using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.CSCInvoke;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPWebEvaluation : EvaluationBase
    {
        private SPWeb m_Web;
        private String m_CacheKey;
        private int m_iTTL;
        private DateTime m_Evaltime;
        private List<Obligation> m_Obligations;

        public SPWeb Web
        {
            get { return m_Web; }
            set { m_Web = value; }
        }

        public int TTL
        {
            get { return m_iTTL; }
            set { m_iTTL = value; }
        }

        public SPWebEvaluation(SPWeb web, CETYPE.CEAction action, String url, String host, String module, SPUser user)
            : base(action, url, host, module, user)
        {
            m_Web = web;
            m_iTTL = 0;
        }

        public SPWebEvaluation(SPWeb web, CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
            : base(action, url, host, module, username, userSid)
        {
            m_Web = web;
            m_iTTL = 0;
        }


        public List<Obligation> GetObligations()
        {
            return m_Obligations;
        }

        override public string ReConstructUrl()
        {
            string webUrl = m_Web.Url;

            string[] splitedUrls = m_Url.Split(new char[] { '?' });
            if (splitedUrls[0].StartsWith(webUrl + "/_layouts/", StringComparison.OrdinalIgnoreCase))
            {
                webUrl += splitedUrls[0].Substring(splitedUrls[0].LastIndexOf("/"));
            }
            else
            {
                if (splitedUrls[0].Length > webUrl.Length)
                    webUrl = splitedUrls[0];
            }

            return webUrl;
        }

        override public bool Run()
        {
            bool bResult = true;

            try
            {
                String objTargetUrl = null;
                String objReferrerUrl = null;

                if (m_Web != null)
                {
                    // m_CacheKey should be set before call QueryEvaluationResultCache()
                    m_CacheKey = objReferrerUrl = ReConstructUrl();
                    bool bCacheExisted = QueryEvaluationResultCache(ref bResult);
                    if (bCacheExisted && CheckCache)
                        return bResult;

                    if (objReferrerUrl != null
                        && !objReferrerUrl.EndsWith("RedirectPage.aspx", StringComparison.OrdinalIgnoreCase))
                    {
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
                        evaProperties.ConstructForWeb(m_Web, ref attributes);
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
                        evaluator.EVALOBJECT = m_Web;
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
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SPWebEvaluation:" + ex.Message);
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

            EvaluationCache.Instance.Add(m_HostAddress, userId, m_CacheKey, bAllow, savedQuery, span, new DateTime(1, 1, 1),m_Evaltime);
        }
    }
}
