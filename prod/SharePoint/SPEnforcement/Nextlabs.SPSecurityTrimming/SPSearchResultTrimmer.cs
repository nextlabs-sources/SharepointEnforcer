using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Security.Principal;
using System.Diagnostics;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Query;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    class MultipleDataCache
    {
        public MultipleDataCache()
        {
            Guid = null;
            IdRequest = -1;
            ModifyTime = new DateTime(1, 1, 1);
            Allow = true;
            Id = 0;
        }
        public string Guid;
        public int IdRequest;
        public DateTime ModifyTime;
        public bool Allow;
        public int Id;
    }

#if SP2010
    class SPSearchResultTrimmer : ISecurityTrimmer2
#else
    class SPSearchResultTrimmer : ISecurityTrimmerPost
#endif
    {
        public SPSearchResultTrimmer()
        {
        }
        public void Initialize(NameValueCollection staticProperties, SearchServiceApplication searchApp)
        {
        }
#if SP2013 || SP2016 || SP2019
        public BitArray CheckAccess(IList<String> documentCrawlUrls, IList<string> dummy,IDictionary<String, Object> sessionProperties, IIdentity userIdentity)
#else
        public BitArray CheckAccess(IList<String> documentCrawlUrls, IDictionary<String, Object> sessionProperties, IIdentity userIdentity)
#endif
        {
            CommonVar.Init();
            BitArray retArray = new BitArray(documentCrawlUrls.Count);

            DateTime Start = DateTime.Now;

            string remoteAddress = NextLabs.Common.Utilities.GetLocalIPv4Address();
            string curUser = userIdentity.Name.Substring(userIdentity.Name.LastIndexOf("|") + 1);
            string curSid = null;
            try
            {
                NTAccount account = new NTAccount(curUser);
                SecurityIdentifier identifier = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                curSid = identifier.ToString();
            }
            catch
            {
                curSid = curUser;
            }
            SPWeb web = null;
            EvaluationMultiple evaluator = null;
            List<MultipleDataCache> evalCache = new List<MultipleDataCache>();
            bool bPCConnect = TrimmingEvaluationMultiple.IsPCConnected();
            bool bDefault = Globals.GetPolicyDefaultBehavior();
            for (int x = 0; x < documentCrawlUrls.Count; x++)
            {
                Uri url = new Uri(documentCrawlUrls[x]);
                try
                {

                    if (url.AbsoluteUri.StartsWith("sts3", StringComparison.OrdinalIgnoreCase)
                        || url.AbsoluteUri.StartsWith("sts4", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bPCConnect)
                        {
                            string guid = url.AbsoluteUri;
                            bool bAllow = true;
                            bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(curSid, remoteAddress, guid, ref bAllow);
                            if (bExisted)
                            {
                                retArray[x] = bAllow;
                            }
                            else
                            {
                                using (SPSts3UrlParser UrlParser = new SPSts3UrlParser(url.AbsoluteUri))
                                {
                                    UrlParser.Parse();
                                    object obj = UrlParser.ParsedObject;
                                    MultipleDataCache cache = new MultipleDataCache();
                                    cache.Id = x;
                                    bool bExised = SetTrimRequest(ref web, ref evaluator, obj, curUser, curSid, remoteAddress, guid, cache);
                                    evalCache.Add(cache);
                                }
                            }
                        }
                        else
                        {
                            retArray[x] = bDefault;
                        }
                    }
                    else if (url.AbsoluteUri.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                        || url.AbsoluteUri.StartsWith("\\\\"))
                    {
                        FileShareEvaluation EvaObj = new FileShareEvaluation(CETYPE.CEAction.Read, url.AbsoluteUri,
                            remoteAddress, curUser, curSid, "Search Result Trimmer");
                        retArray[x] = EvaObj.Run();
                    }
                    else
                    {
                        NLLogger.OutputLog(LogLevel.Debug , "Search URL " + url.AbsoluteUri + " is not supported yet.");
                        retArray[x] = true;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, $"Exception CheckAccess (URL={url.AbsoluteUri}):", null, ex);
                }
            }

            if (evalCache.Count > 0 && evaluator != null)
            {
                DateTime evalTime = DateTime.Now;
                bool bRun = evaluator.run();
                if (bRun)
                {
                    foreach (MultipleDataCache cache in evalCache)
                    {
                        bool bAllow = evaluator.GetTrimEvalResult(cache.IdRequest);
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(curSid, remoteAddress, cache.Guid, bAllow, evalTime, cache.ModifyTime);
                        retArray[cache.Id] = bAllow;
                    }
                    evaluator.ClearRequest();
                }
            }
            DateTime End = DateTime.Now;
            TimeSpan span = End - Start;

            NLLogger.OutputLog(LogLevel.Debug, "Evaluation " + documentCrawlUrls.Count.ToString() + " items Span " + span);

            return retArray;
        }

        public bool SetTrimRequest(ref SPWeb web, ref EvaluationMultiple evaluator, object obj, string userName, string userId, string remoteAddr, string guid, MultipleDataCache cache)
        {
            bool bExisted = true;
            try
            {
                int idRequest = -1;
                if (obj != null)
                {
                    System.DateTime modifyTime = GetLastModifiedTime(obj);
                    if (evaluator == null && web != null)
                    {
                        SPSite site = web.Site;
                        SPWeb rootWeb = site.RootWeb;
                        TrimmingEvaluationMultiple.NewEvalMult(web, ref evaluator, CETYPE.CEAction.Read, userName, userId);
                    }

                    string srcName = null;
                    string[] srcAttr = null;
                    string url = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                    if (web != null && evaluator != null)
                    {
                        Globals.GetSrcNameAndSrcAttr(web, obj, url, remoteAddr, ref srcName, ref srcAttr);
                        evaluator.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                        cache.Guid = guid;
                        cache.IdRequest = idRequest;
                        cache.ModifyTime = modifyTime;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during  SPSearchResultTrimmer SetTrimRequest:", null, ex);
            }

            return bExisted;
        }

        private DateTime GetLastModifiedTime(object obj)
        {
            DateTime time = new DateTime(1, 1, 1);

            if (obj is SPList)
            {
                SPList list = obj as SPList;
                time = list.LastItemModifiedDate;
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                time = view.ParentList.LastItemModifiedDate;
            }
            else if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                time = (DateTime)item["Modified"];
            }

            return time;
        }
    }
}
