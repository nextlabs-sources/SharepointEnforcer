using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Web.UI.WebControls.WebParts;
using NextLabs.CSCInvoke;

namespace NextLabs.Common
{
    public class SPWebPartEvaluation : EvaluationBase
    {
        private SPWeb m_Web;
        private System.Web.UI.WebControls.WebParts.WebPart m_WebPart;
        private int m_iTTL;
        private DateTime m_Evaltime;
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

        public System.Web.UI.WebControls.WebParts.WebPart WebPart
        {
            get { return m_WebPart; }
            set { m_WebPart = value; }
        }

        public SPWebPartEvaluation(System.Web.UI.WebControls.WebParts.WebPart webpart, SPWeb web,
            CETYPE.CEAction action, String url, String host, String module, SPUser user)
            : base(action, url, host, module, user)
        {
            m_WebPart = webpart;
            m_Web = web;
            m_iTTL = 0;
        }

        public SPWebPartEvaluation(System.Web.UI.WebControls.WebParts.WebPart webpart, SPWeb web,
            CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
            : base(action, url, host, module, username, userSid)
        {
            m_WebPart = webpart;
            m_Web = web;
            m_iTTL = 0;
        }

        override public string ReConstructUrl()
        {
            string[] splitedUrls = m_Url.Split(new char[] { '?' });

            string wpUrl = splitedUrls[0];
            if (m_WebPart is ListViewWebPart)
            {
                ListViewWebPart lvwp = m_WebPart as ListViewWebPart;
                SPList list;
                string listUrl;
                try
                {
                    list = m_Web.Lists[new Guid(lvwp.ListName)];
                    listUrl = ReConstructListUrl(list, m_Web);
                    wpUrl = listUrl;
                }
                catch (Exception)
                {
                }
            }
            else if (m_WebPart is XsltListViewWebPart)
            {
                XsltListViewWebPart xslvwp = m_WebPart as XsltListViewWebPart;
                SPList list;
                string listUrl;
                try
                {
                    list = m_Web.Lists[new Guid(xslvwp.ListName)];
                    listUrl = ReConstructListUrl(list, m_Web);
                    wpUrl = listUrl;
                }
                catch (Exception)
                {
                }
            }
            return wpUrl;
        }

        override public bool Run()
        {
            bool bResult = true;

            bool bCacheExisted = QueryEvaluationResultCache(ref bResult);
            if (bCacheExisted && CheckCache)
                return bResult;

            string wpurl = ReConstructUrl();
            EvaluatorContext evaContext = new EvaluatorContext();
            evaContext.ActionType = m_Action;
            evaContext.NoiseLevel = NoiseLevel.Application;
            evaContext.Web = m_Web;
            evaContext.RemoteAddress = m_HostAddress;
            evaContext.UserName = m_UserName;
            evaContext.UserSid = m_UserSid;
            evaContext.SrcName = wpurl;
            evaContext.TargetName = "";

            EvaluatorProperties evaProperties = new EvaluatorProperties();
            List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
            evaProperties.ConstructForWebPart(m_WebPart, m_Web, ref attributes);
            evaContext.SrcAttributes = attributes;

            Evaluator evaluator = new Evaluator();
            evaluator.EVALOBJECT = m_WebPart;
            if (Globals.g_JPCParams.bUseJavaPC)
                bResult = evaluator.CheckPortal_CloudAZ(ref evaContext);
            else
                bResult = evaluator.CheckPortal(ref evaContext);
            m_iTTL = evaContext.CacheHint;
            m_Evaltime = DateTime.Now;

            AddEvaluationResultCache(bResult);

            return bResult;
        }

        private string GetSubType()
        {
            string strbasetype = "webpart";
            if (m_WebPart is ListViewWebPart)
            {
                ListViewWebPart lvwp = (ListViewWebPart)m_WebPart;
                try
                {
                    SPList list = m_Web.Lists[new Guid(lvwp.ListName)];
                    if (list.BaseType == SPBaseType.DocumentLibrary)
                    {
                        strbasetype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    }
                    else
                    {
                        strbasetype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                    }
                }
                catch (Exception)
                {
                }
            }
            return strbasetype;
        }

        protected override bool QueryEvaluationResultCache(ref bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            string guid = "";
            if (String.IsNullOrEmpty(m_WebPart.ID))
                guid = m_WebPart.GetType().ToString();
            else
                guid = m_WebPart.ID + m_WebPart.Title;

            bool bExisted = EvaluationCache.Instance.GetValue(m_HostAddress, userId, guid,
                ref bAllow, ref savedQuery, new DateTime(1, 1, 1));
            
            return bExisted;
        }

        protected override void AddEvaluationResultCache(bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            string guid = "";
            if (String.IsNullOrEmpty(m_WebPart.ID))
                guid = m_WebPart.GetType().ToString();
            else
                guid = m_WebPart.ID + m_WebPart.Title;
            TimeSpan span = new TimeSpan(0, m_iTTL, 0);

            EvaluationCache.Instance.Add(m_HostAddress, userId, guid, bAllow, savedQuery, span, new DateTime(1, 1, 1),m_Evaltime);
        }
    }
}
