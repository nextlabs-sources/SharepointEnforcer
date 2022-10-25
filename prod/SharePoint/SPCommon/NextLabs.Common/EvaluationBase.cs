using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.SharePoint;
using NextLabs.CSCInvoke;

namespace NextLabs.Common
{
    // do evaluation for object "SPWeb", "SPList" and "SPLIstItem".
    public class SPObjectEvaluation
    {
        private HttpRequest m_request;
        private SPWeb m_web;
        private object m_evalObj;
        private CETYPE.CEAction m_action;
        private string m_denyMsg;
        public SPObjectEvaluation(HttpRequest request, SPWeb web, object evalObj, CETYPE.CEAction action)
        {
            m_request = request;
            m_web = web;
            m_evalObj = evalObj;
            m_action = action;
            m_denyMsg = null;
        }

        public bool Run()
        {
            bool bAllow = Globals.GetPolicyDefaultBehavior(); // use "default behavior" as default value.
            if (m_evalObj != null && m_web != null && m_request != null)
            {
                string url = Globals.ConstructObjectUrl(m_evalObj);
                EvaluationBase evalBase = EvaluationFactory.CreateInstance(m_evalObj, m_action, url, m_request.UserHostAddress, "CSOM Evaluation", m_web.CurrentUser);
                evalBase.CheckCache = false;

                if (evalBase != null)
                {
                    bAllow = evalBase.Run();
                }
                if (!bAllow)
                {
                    string backUrl = null;
                    string httpServer = null;
                    string msg = null;

                    Utilities.GenerateBackUrl(m_request, evalBase.PolicyName, evalBase.PolicyMessage, ref backUrl, ref httpServer, ref msg);
                    m_denyMsg = Globals.GetDenyPageHtml(httpServer, backUrl, msg);
                }
            }
            return bAllow;
        }

        public string GetDenyMessage()
        {
            return m_denyMsg;
        }
    }

    public abstract class EvaluationBase : IEvaluation
    {
        public const string SP_PAGE_TYPE = "page_type";
        public const string SP_PAGE_TYPE_APPLICATION = "application page";//Page that have _layouts format
        public const string SP_PAGE_TYPE_NORMAL = "normal page";

        protected CETYPE.CEAction m_Action;
        protected String m_HostAddress;
        protected String m_ModuleName;
        protected String m_Url;
        protected String m_UserName;
        protected String m_UserSid;

        protected String m_PolicyName;
        protected String m_PolicyMessage;

        private bool m_bCheckCache;
        public bool CheckCache
        {
            get { return m_bCheckCache; }
            set { m_bCheckCache = value; }
        }
		
        public CETYPE.CEAction Action
        {
            get { return m_Action; }
            set { m_Action = value;  }
        }

        public String HostAddress
        {
            get { return m_HostAddress; }
            set { m_HostAddress = value; }
        }

        public String ModuleName
        {
            get { return m_ModuleName; }
            set { m_ModuleName = value; }
        }

        public String Url
        {
            get { return m_Url; }
            set { m_Url = value; }
        }

        public String PolicyName
        {
            get { return m_PolicyName; }
        }

        public String PolicyMessage
        {
            get { return m_PolicyMessage; }
        }

        public EvaluationBase()
        {
        }

        public EvaluationBase(CETYPE.CEAction action, String url, String host, String module, SPUser user)
        {
            m_Action = action;
            m_Url = url;
            m_HostAddress = host;
            m_ModuleName = module;
            m_UserName = user.LoginName;
            m_UserSid = user.Sid;
            CheckCache = true;
        }

        public EvaluationBase(CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
        {
            m_Action = action;
            m_Url = url;
            m_HostAddress = host;
            m_ModuleName = module;
            m_UserName = username;
            m_UserSid = userSid;
            CheckCache = true;
        }

        abstract public string ReConstructUrl();
        abstract public bool Run();

        protected virtual bool QueryEvaluationResultCache(ref bool bAllow)
        {
            return true;
        }

        protected virtual void AddEvaluationResultCache(bool bAllow)
        {
        }

        protected string ReConstructListUrl(SPList list, SPWeb web)
        {
            return Globals.ConstructListUrl(web, list);
        }
    }
}
