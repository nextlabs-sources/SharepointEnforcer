using System;
using System.IO;
using System.Web;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using NextLabs.Common;
using System.Text.RegularExpressions;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class AuthorNodeData
    {
        public string trimId;
        public string node;
        public int evalId;
        public bool bAllow;
        public string guid;
        public System.DateTime time;
        public AuthorType AuthorType;
        public AuthorNodeData()
        {
            trimId = null;
            node = null;
            evalId = -1;
            bAllow = true;
            guid = null;
            time = new DateTime();
            AuthorType = AuthorType.Unknown;
        }
    }

    public enum AuthorType
    {
        SPWebs,
        SPLists,
        SPItems,
        Unknown
    }

    public class AuthorTrimming
    {
        private static string strAuthour = "/_vti_bin/_vti_aut/author.dll";
        public void DoTrimming(HttpRequest Request, HttpContext context)
        {
            try
            {
                string url = Globals.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
                if (url.EndsWith(strAuthour, StringComparison.OrdinalIgnoreCase))
                {
                    SPWeb web = SPControl.GetContextWeb(context);
                    bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(context.Request);
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                    {
                        if (bIgnoreTrimControl || manager.CheckSecurityTrimming())
                        {
                            string webUrl = web.Url;
                            string remoteAddr = Request.UserHostAddress;
                            AuthorEvaluation authorEval = NewAuthorEval(web, webUrl, remoteAddr);
                            HttpResponse response = context.Response;
                            ReplaceResponseFilter(response, authorEval);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AuthorTrimmer DoTrimming:", null, ex);
            }
        }

        public void ReplaceResponseFilter(HttpResponse response, AuthorEvaluation authorEval)
        {
            ResponseFilter filter = ResponseFilters.Current(response);
            filter.AuthorEval = authorEval;
            filter.AddFilterType(FilterType.AuthorTrimmer);
        }

        public AuthorEvaluation NewAuthorEval(SPWeb web, string url, string remoteAddr)
        {
            EvaluationMultiple mulEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(web, ref mulEval);
            AuthorEvaluation authorEval = new AuthorEvaluation(mulEval, web, remoteAddr);
            return authorEval;
        }
    }

    public class AuthorTrimmer
    {
        public static string[] decodeId = { "document_name=", "url=" };
        private HttpResponse m_response;
        private AuthorEvaluation m_authorEval;

        public AuthorTrimmer(HttpResponse response, AuthorEvaluation authorEval)
        {
            m_response = response;
            m_authorEval = authorEval;
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            try
            {
                if (-1 != strInput.IndexOf("</html>", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = HtmlTrimming(strInput);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AuthorTrimmer Run:", null, ex);
            }
            return strFinal;
        }

        private string HtmlTrimming(string responseHtml)
        {
            string strFinal = responseHtml;
            try
            {
                List<AuthorNodeData> listNodeData = new List<AuthorNodeData>();

                //get process string
                string[] regexArr = { @"<ul>\s<li>document_name=.*\s</ul>\s",
                                     @"<ul>\s<li>document_name=.+\s<li>meta_info=\s<ul>\s(<li>.*\s){1,}</ul>\s</ul>\s",
                                    @"<ul>\s<li>url=.+\s<li>meta_info=\s<ul>\s(<li>.*\s){1,}</ul>\s</ul>\s" };
                foreach(string regex in regexArr)
                {
                    Regex re = new Regex(regex);
                    MatchCollection matches = re.Matches(responseHtml);
                    string strNode = null;
                    string id = null;
                    AuthorType type = AuthorType.Unknown;
                    System.Collections.IEnumerator enu = matches.GetEnumerator();
                    while (enu.MoveNext() && enu.Current != null)
                    {
                        Match match = (Match)(enu.Current);
                        strNode = match.Value;
                        if (strNode != null)
                        {
                            bool bDecode = DecodeNode(strNode, ref id, ref type);
                            if (bDecode && id != null && !type.Equals(AuthorType.Unknown))
                            {
                                AuthorNodeData nodeData = new AuthorNodeData();
                                nodeData.node = strNode;
                                nodeData.trimId = id;
                                nodeData.AuthorType = type;
                                listNodeData.Add(nodeData);
                            }
                        }
                    }
                }

                if (listNodeData.Count != 0)
                {
                    m_authorEval.Run(listNodeData);
                    foreach (AuthorNodeData nodeData in listNodeData)
                    {
                        if (!nodeData.bAllow && nodeData.node != null)
                        {
                            strFinal = strFinal.Replace(nodeData.node, "");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AuthorTrimmer HtmlTrimming:", null, ex);
            }
            return strFinal;
        }

        private bool DecodeNode(string node, ref string id, ref AuthorType type)
        {
            bool bDecode = false;
            int ind = 0;
            string tail = null;
            if (-1 != (ind = node.IndexOf(decodeId[0])))
            {
                tail = node.Substring(ind + decodeId[0].Length);
                if (-1 != (ind = tail.IndexOf("<")))
                {
                    id = tail.Substring(0, ind - 1);
                }
                type = AuthorType.SPItems;
                bDecode = true;
            }
            else if (-1 != (ind = node.IndexOf(decodeId[1])))
            {
                tail = node.Substring(ind + decodeId[1].Length);
                if(-1 != (ind = tail.IndexOf("<")))
                {
                    id = tail.Substring(0, ind - 1);
                }
                if (-1 != tail.IndexOf("vti_ischildweb", StringComparison.OrdinalIgnoreCase))
                {
                    type = AuthorType.SPWebs;
                }
                else if (-1 != tail.IndexOf("vti_listtitle", StringComparison.OrdinalIgnoreCase))
                {
                    type = AuthorType.SPLists;
                }
                else
                {
                    type = AuthorType.SPItems;
                }
                bDecode = true;
            }
            return bDecode;
        }

    }

    public class AuthorEvaluation
    {
        private EvaluationMultiple m_mulEval;
        private SPWeb m_web;
        private string m_remoteAddr;
        private string m_userId;
        public AuthorEvaluation(EvaluationMultiple mulEval, SPWeb web, string remoteAddr)
        {
            m_mulEval = mulEval;
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
        }

        public bool Run(List<AuthorNodeData> listNodeData)
        {
            try
            {
                foreach (AuthorNodeData nodeData in listNodeData)
                {
                    SetEvalAttrBytrimId(nodeData);
                }
                m_mulEval.run();
                foreach (AuthorNodeData nodeData in listNodeData)
                {
                    int evalId = nodeData.evalId;
                    if (evalId != -1)
                    {
                        bool bAllow = m_mulEval.GetTrimEvalResult(evalId);
                        nodeData.bAllow = bAllow;
                        DateTime evalTime = DateTime.Now;
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(m_userId, m_remoteAddr, nodeData.guid, bAllow, evalTime, nodeData.time);
                    }
                }
                m_mulEval.ClearRequest();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AuthorEvaluation run:", null, ex);
            }
            return true;
        }

        public void SetEvalAttrBytrimId(AuthorNodeData nodeData)
        {
            try
            {
                AuthorType type = nodeData.AuthorType;
                string trimId = nodeData.trimId;
                trimId = trimId.StartsWith("/") ? trimId : "/" + trimId;
                string url = m_web.Url + trimId;
                url = HttpUtility.HtmlDecode(url);
                object obj = null;
                if (url != null)
                {
                    if (type.Equals(AuthorType.SPItems))
                    {
                        SPListItem item = (SPListItem)NextLabs.Common.Utilities.GetCachedSPContent(m_web, url, NextLabs.Common.Utilities.SPUrlListItem);
                        if (item == null && -1 != url.IndexOf("Attachments", StringComparison.OrdinalIgnoreCase))
                        {
                            item = Globals.ParseItemFromAttachmentURL(m_web, url);
                        }
                        obj = item;
                    }
                    else if (type.Equals(AuthorType.SPWebs))
                    {
                        SPWeb web = (SPWeb)NextLabs.Common.Utilities.GetCachedSPContent(null, url, NextLabs.Common.Utilities.SPUrlWeb);
                        obj = web;
                    }
                    else if (type.Equals(AuthorType.SPLists))
                    {
                        SPList list = (SPList)NextLabs.Common.Utilities.GetCachedSPContent(m_web, url, NextLabs.Common.Utilities.SPUrlList);
                        obj = list;
                    }
                }

                CheckEvalCacheAndSetTrim(obj, nodeData, url);

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AuthorEvaluation SetEvalAttrBytrimId:", null, ex);
            }
        }

        public void CheckEvalCacheAndSetTrim(object obj, AuthorNodeData nodeData, string url)
        {
            string guid = null;
            bool bExisted = false;
            bool bAllow = true;
            System.DateTime modifyTime = new DateTime(1, 1, 1);
            bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
            {
                bool bTrimLists = bIgnoreTrimControl || manager.CheckListTrimming();
                string srcName = null;
                string[] srcAttr = null;
                int idRequest = -1;
                bool bObj = false;
                if (obj != null)
                {
                    bObj = true;
                    if (obj is SPWeb)
                    {
                        SPWeb evalWeb = obj as SPWeb;
                        guid = evalWeb.Url;
                    }
                    else if (obj is SPList)
                    {
                        if (bTrimLists)
                        {
                            SPList evalList = obj as SPList;
                            guid = NextLabs.Common.Utilities.ReConstructListUrl(evalList);
                        }
                    }
                    else if (obj is SPListItem)
                    {
                        SPListItem evalItem = obj as SPListItem;
                        if (bTrimLists || manager.CheckListTrimming(evalItem.ParentList))
                        {
                            guid = evalItem.ParentList.ID.ToString() + evalItem.ID.ToString();
                            modifyTime = NextLabs.Common.Utilities.GetLastModifiedTime(evalItem);
                        }
                    }
                }
                else if (url != null)
                {
                    guid = url;
                }

                if (guid != null)
                {
                    bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(m_userId, m_remoteAddr, guid, ref bAllow, modifyTime);
                    if (bExisted)
                    {
                        nodeData.bAllow = bAllow;
                    }
                    else
                    {
                        if (bObj)
                        {
                            HttpContext Context = HttpContext.Current;
                            string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                            Globals.GetSrcNameAndSrcAttr(obj, objUrl, Context, ref srcName, ref srcAttr);
                            m_mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                        }
                        else
                        {
                            string name = url.Substring(url.LastIndexOf("/") + 1);
                            string[] attr = { "name", name };
                            m_mulEval.SetTrimRequest(obj, url, attr, out idRequest);
                        }
                        nodeData.guid = guid;
                        nodeData.evalId = idRequest;
                        nodeData.time = modifyTime;
                    }
                }
            }
        }
    }
}
