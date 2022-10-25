using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.CSCInvoke;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPListEvaluation : EvaluationBase
    {
        private SPList m_List;
        private SPWeb m_Web;
        private String m_CacheKey;
        private int m_iTTL;
        private DateTime m_Evaltime;
        private List<Obligation> m_Obligations;
        public SPList List
        {
            get { return m_List; }
            set { m_List = value; }
        }

        public int TTL
        {
            get { return m_iTTL;  }
            set { m_iTTL = value; }
        }

        public SPListEvaluation(SPList list, CETYPE.CEAction action, String url, String host, String module, SPUser user)
            : base(action, url, host, module, user)
        {
            m_List = list;
            m_Web = list.ParentWeb;
            m_iTTL = 0;
        }

        public SPListEvaluation(SPList list, CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
            : base(action, url, host, module, username, userSid)
        {
            m_List = list;
            m_Web = list.ParentWeb;
            m_iTTL = 0;
        }

        public List<Obligation> GetObligations()
        {
            return m_Obligations;
        }

        override public string ReConstructUrl()
        {
            string listUrl = "";

            listUrl = ReConstructListUrl(m_List, m_Web);

            if (m_Url.Contains("?"))
            {
	            string[] splitedUrls = m_Url.Split(new char[] {'?'});

	            if (splitedUrls[0].StartsWith(listUrl, StringComparison.OrdinalIgnoreCase))
	            {
	                if (splitedUrls[0].Length > listUrl.Length)
	                    listUrl = splitedUrls[0];
	            }
	            else
	                listUrl += splitedUrls[0].Substring(splitedUrls[0].LastIndexOf("/"));
			}
            return listUrl;
        }

        override public bool Run()
        {
            bool bResult = true;

            try
            {
                String objTargetUrl = null;
                String objReferrerUrl = null;

                if (m_List != null)
                {
                    // m_CacheKey should be set before call QueryEvaluationResultCache()
                    m_CacheKey = objReferrerUrl = ReConstructUrl();
                    bool bCacheExisted = QueryEvaluationResultCache(ref bResult);
                    if (bCacheExisted && CheckCache)
                        return bResult;

                    EvaluatorContext evaContext = new EvaluatorContext();
                    evaContext.ActionType = m_Action;
                    evaContext.NoiseLevel = NoiseLevel.Application;
                    evaContext.Web = m_Web;
                    evaContext.RemoteAddress = m_HostAddress;
                    evaContext.UserName = m_UserName;
                    evaContext.UserSid = m_UserSid;
                    evaContext.SrcName = objReferrerUrl;
                    evaContext.TargetName = objTargetUrl;

                    EvaluatorProperties evaProperties = new EvaluatorProperties();
                    List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
                    evaProperties.ConstructForList(m_List, ref attributes);
                    evaContext.SrcAttributes = attributes;

                    //Add a page type attribute
                    if (m_Url.IndexOf("/_layouts", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                            EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_APPLICATION);
                        evaContext.SrcAttributes.Add(keyVaule);
                    }
                    else
                    {
                        KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                            EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_NORMAL);
                        evaContext.SrcAttributes.Add(keyVaule);
                    }

                    Evaluator evaluator = new Evaluator();
                    evaluator.EVALOBJECT = m_List;

                    if (Globals.g_JPCParams.bUseJavaPC)
                        bResult = evaluator.CheckPortal_CloudAZ(ref evaContext);
                    else
                        bResult = evaluator.CheckPortal(ref evaContext);

                    m_iTTL = evaContext.CacheHint;
                    m_Evaltime = DateTime.Now;
                    if (bResult)
                    {
	                    m_Obligations = evaluator.GetObligations();
                    }
                    if (CheckCache)
                    {
                        AddEvaluationResultCache(bResult);
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SPListEvaluation:", null, ex);
            }

            return bResult;
        }

        protected override bool QueryEvaluationResultCache(ref bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            bool bExisted = EvaluationCache.Instance.GetValue(m_HostAddress, userId, m_CacheKey,
                ref bAllow, ref savedQuery, new DateTime(1, 1, 1));
            return bExisted;
        }

        protected override void AddEvaluationResultCache(bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            TimeSpan span = new TimeSpan(0, m_iTTL, 0);

            EvaluationCache.Instance.Add(m_HostAddress, userId, m_CacheKey, bAllow, savedQuery, span, new DateTime(1, 1, 1), m_Evaltime);
        }
    }
}
