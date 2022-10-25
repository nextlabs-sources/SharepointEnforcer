using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueryCloudAZSDK;
using QueryCloudAZSDK.CEModel;


namespace NextLabs.Common
{
    public sealed class CloudAZQuery
    {
        private static volatile CloudAZQuery instance;
        
        private string m_strJPCHost;
        private string m_strOAUTHost;
        private string m_strClientId;
        private string m_strClientSecure;
        private CEQuery m_obCEQuery;

        private CloudAZQuery()        
        {
           InitParams();
        }

        private static object syncRoot = new Object();
        public static CloudAZQuery Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new CloudAZQuery();
                        }
                    }
                }
                return instance;
            }
        }

         
        private void InitParams()
        {
            m_strJPCHost = Globals.g_JPCParams.strPCHostAddress;// "http://win12-jpc-81.qapf1.qalab01.nextlabs.com:58080";///
            m_strOAUTHost = Globals.g_JPCParams.strOAuthHostAddress;//"https://win12-cc-81.qapf1.qalab01.nextlabs.com";////
            m_strClientId = Globals.g_JPCParams.strClientID;////"apiclient";////
            m_strClientSecure = Globals.g_JPCParams.strClientSecureKey;////"123blue!";////

			m_obCEQuery = new CEQuery(m_strJPCHost, m_strOAUTHost, m_strClientId, m_strClientSecure);
        }

        public static QueryCloudAZSDK.CEModel.CEAttres ConvertArraytoAtrrs(string[] attrs)
        {
            if (attrs == null || attrs.Length < 2)
                return null;

            QueryCloudAZSDK.CEModel.CEAttres obAttres = new QueryCloudAZSDK.CEModel.CEAttres();
            for (int i = 0; i < attrs.Length; i += 2)
            {
                obAttres.AddAttribute(new CEAttribute(attrs[i], attrs[i + 1], CEAttributeType.XacmlString));
            }
            return obAttres;
        } 

        public static CERequest CreateQueryReq(string strAction, string remoteAddress, string srcName, string[] srcAttr, string userSid, 
            string userName, string[] userAttr, string destName = null , string[] destAttr = null)
        {
            if (!string.IsNullOrEmpty(strAction) && !string.IsNullOrEmpty(srcName))
            {
                CERequest obReq = new CERequest();
                // Host
                if (!string.IsNullOrEmpty(remoteAddress) && !remoteAddress.Contains(":")) // George: Not support IPV6
                {
                    obReq.SetHost(remoteAddress, remoteAddress, null);
                }

                // Action
                obReq.SetAction(strAction);

                // User
                if (!string.IsNullOrEmpty(userSid) && !string.IsNullOrEmpty(userName))
                {
                    CEAttres ceUserAttr = ConvertArraytoAtrrs(userAttr);
                    obReq.SetUser(userSid, userName, ceUserAttr);
                }

                // Resource
                CEAttres ceSrcAttr = ConvertArraytoAtrrs(srcAttr);
                if (ceSrcAttr == null)
                {
                    ceSrcAttr = new CEAttres();
                }
                ceSrcAttr.AddAttribute(new CEAttribute("url", srcName, CEAttributeType.XacmlString));
                obReq.SetSource(srcName, "spe", ceSrcAttr);

                // Destination
                if (!string.IsNullOrEmpty(destName))
                {
                    CEAttres ceDestAttr = ConvertArraytoAtrrs(destAttr);
                    obReq.SetDest(destName, "spe", ceDestAttr);
                }

                // App
                obReq.SetApp("Sharepoint", null, null, null);

                // Environment: set Dont Care case.
                {
                    CEAttres envAttrs = new CEAttres();
                    envAttrs.AddAttribute(new CEAttribute("dont-care-acceptable", "yes", QueryCloudAZSDK.CEModel.CEAttributeType.XacmlString));
                    obReq.SetEnvAttributes(envAttrs);
                }
                return obReq;
            }
            return null;
        }

        public QueryStatus QueryColuAZPC(CERequest obReq, ref List<CEObligation> lsObligation, ref PolicyResult emPolicyResult)
        {
            QueryStatus emQueryRes = QueryStatus.S_OK;
            emPolicyResult = PolicyResult.DontCare;
            if (obReq != null)
            {
                emQueryRes = m_obCEQuery.CheckResource(obReq, out emPolicyResult, out lsObligation);

                if (emQueryRes == QueryStatus.E_Unauthorized)
                {
                    m_obCEQuery.RefreshToken();
                    emQueryRes = m_obCEQuery.CheckResource(obReq, out emPolicyResult, out lsObligation);
                }

            }
            return emQueryRes;
        }

        public QueryStatus MultipleQueryColuAZPC(List<CERequest> ceRequests, out List<PolicyResult> listPolicyResults, out List<List<CEObligation>> listObligations)
        {
            QueryStatus emQueryRes = QueryStatus.S_OK;
            emQueryRes = m_obCEQuery.CheckMultipleResources(ceRequests, out listPolicyResults, out listObligations);

            if (emQueryRes == QueryStatus.E_Unauthorized)
            {
                m_obCEQuery.RefreshToken();
                emQueryRes = m_obCEQuery.CheckMultipleResources(ceRequests, out listPolicyResults, out listObligations);
            }

            return emQueryRes;
        }
     }
}
