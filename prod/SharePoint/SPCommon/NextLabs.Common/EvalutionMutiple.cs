using System;
using Microsoft.SharePoint;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SDKWrapperLib;
using NextLabs.CSCInvoke;
using System.Security.Principal;
using Microsoft.SharePoint.Administration;
using QueryCloudAZSDK;
using NextLabs.Diagnostic;
using LogLevel = NextLabs.Diagnostic.LogLevel;

namespace NextLabs.Common
{
    public class TrimmingEvaluationMultiple
    {
        public static bool IsPCConnected()
        {
            EvaluatorApplication app = EvaluatorApplication.CreateInstance();
            CETYPE.CEResult_t result;
            CETYPE.CEUser user = new CETYPE.CEUser("dummyName", "dummyId");
            IntPtr m_ConnectHandle = IntPtr.Zero;
            int ConnectTimeoutMs = 5 * 1000;
            result = CESDKAPI.CECONN_Initialize(app.Application, user, null,
                                                out m_ConnectHandle, ConnectTimeoutMs);
            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                return false;
            }
            return true;
        }

        public static void NewEvalMult(SPWeb web, ref EvaluationMultiple mulEval, CETYPE.CEAction userAction = CETYPE.CEAction.Read)
        {
            try
            {
                string UserName = web.CurrentUser.LoginName;
                UserName = NextLabs.Common.Utilities.ClaimUserConvertion(UserName);
                string userSid = web.CurrentUser.Sid;
                if (String.IsNullOrEmpty(userSid))
                {
                    userSid = UserSid.GetUserSid(web, web.CurrentUser.LoginName);
                    if (String.IsNullOrEmpty(userSid))
                    {
                        userSid = UserName;
                    }
                }
                NewEvalMult(web, ref mulEval, userAction, UserName, userSid);
            }

            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during NewEvalMult :", null, ex);
            }
        }

        // Add this function for "Open" and "Search" action.
        public static void NewEvalMult(SPWeb web, ref EvaluationMultiple mulEval, CETYPE.CEAction userAction, string userName, string userSid)
        {
            try
            {
                IPrincipal principalUser = null;
                string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(userName, web.Url, ref principalUser);
                string action = Globals.SPECommon_ActionConvert(userAction);
                string[] userAttr = Globals.SPECommon_GetUserAttr(principalUser,web);
                userAttr = Globals.SPECommon_GetUserProfile(web, userName, userAttr);
                //check if profile sid exists, if yes, remove
                string profileSid = Global_Utils.GetAndRemoveIdAttribute(ref userAttr);
                if (!string.IsNullOrEmpty(profileSid)
                && UserSid.IsValidSid(userSid, userName) != true
                && UserSid.IsValidSid(profileSid, userName) != false)
                {
                    //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                    //update user sid with profile sid
                    userSid = profileSid;
                }
                CETYPE.CENoiseLevel_t noiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_APPLICATION;
                mulEval = new EvaluationMultiple(web, action, userName, userSid, noiseLevel, userAttr);////
            }

            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during NewEvalMult :", null, ex);
            }
        }

        public static bool QueryEvaluationResultCache(string userId, string hostAddress, string guid, ref bool bAllow)
        {
            string savedQuery = "";
            return EvaluationCache.Instance.GetValue(hostAddress, userId, guid,
                ref bAllow, ref savedQuery, new DateTime(1, 1, 1));
        }

        public static bool QueryEvaluationResultCache(string userId, string hostAddress, string guid, ref bool bAllow, DateTime modifyTime)
        {
            string savedQuery = "";
            return EvaluationCache.Instance.GetValue(hostAddress, userId, guid,
                ref bAllow, ref savedQuery, modifyTime);
        }

        public static void AddEvaluationResultCache(string userId, string hostAddress, string guid, bool bAllow, DateTime evalTime)
        {
            string savedQuery = "";
            TimeSpan span = new TimeSpan(0, 0, 0);

            EvaluationCache.Instance.Add(hostAddress, userId, guid, bAllow, savedQuery, span, new DateTime(1, 1, 1), evalTime);
        }

        public static void AddEvaluationResultCache(string userId, string hostAddress, string guid, bool bAllow, DateTime evalTime, DateTime modifyTime)
        {
            string savedQuery = "";
            TimeSpan span = new TimeSpan(0, 0, 0);

            EvaluationCache.Instance.Add(hostAddress, userId, guid, bAllow, savedQuery, span, modifyTime, evalTime);
        }
    }

    public class EvaluationMultiple
    {
        private QueryPC m_thePc;
        private int m_iCookie;
        private string m_action;
        private string m_userName;
        private string m_sid;
        private CETYPE.CENoiseLevel_t m_noiseLevel;
        private List<string> m_userAttr;
        private int m_reqCount;
        private bool m_bRun;
        private SPWeb m_web;
        private bool bTrimPreAuthZ;
        private bool m_bDefault;
		private string m_strIPAddr;
        // Use for Java PC.
        private bool m_bJpc;
        private List<QueryCloudAZSDK.CEModel.CERequest> m_listRequests;
        private List<QueryCloudAZSDK.CEModel.PolicyResult> m_listResults;
        private List<List<QueryCloudAZSDK.CEModel.CEObligation>> m_listObligations;
        private CETYPE.CEUser m_ceUser;
        private List<string> m_strArrXHeader;

        public EvaluationMultiple(SPWeb web, string action, string userName, string sid, CETYPE.CENoiseLevel_t NoiseLevel, string[] userAttr)
        {
            m_reqCount = 0;
            m_thePc = new QueryPC();
            m_thePc.get_cookie(out m_iCookie);

            m_bRun = false;
            m_action = action;
            m_userName = userName;
            m_sid = sid;
            m_userAttr = userAttr == null ? new List<string>() : userAttr.ToList<string>();
            m_noiseLevel = NoiseLevel;
            m_web = web;
            PreAuthorization preAuthorization = PreAuthorization.GetInstance();
            bTrimPreAuthZ = preAuthorization.IfNeedTrimmingPreAthuZ();
            m_bDefault = Globals.GetPolicyDefaultBehavior();
            m_strArrXHeader = null;
            try
            {
                m_strIPAddr = HttpContext.Current.Request.UserHostAddress;
                IPrincipal PrincipalUser = null;
                string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(userName, web.Url, ref PrincipalUser);
                if (!string.IsNullOrEmpty(clientIpAddr))
                {
                    m_strIPAddr = clientIpAddr;
                }
                WebRemoteAddressMap.GetXHeaderAttributes(userName, web.Url, ref m_strArrXHeader);
            }
            catch
            {
                m_strIPAddr = "";
            }
            // use for Java PC.
            m_bJpc = Globals.g_JPCParams.bUseJavaPC;
            m_listRequests = new List<QueryCloudAZSDK.CEModel.CERequest>();
            m_listResults = new List<QueryCloudAZSDK.CEModel.PolicyResult>();
            m_listObligations = new List<List<QueryCloudAZSDK.CEModel.CEObligation>>();
            m_ceUser = new CETYPE.CEUser(m_userName, m_sid);
            int ipNumber = (int)Utilities.IPAddressToIPNumber(m_strIPAddr);
            m_thePc.set_ip_number(ipNumber);
        }

        public void ClearRequest()
        {
            m_thePc.release_request(m_iCookie);
            m_reqCount = 0;
        }

        public void SetTrimRequest(object obj, string srcName, string[] srcAttr, out int idRequest, int iCareOb = 0)
        {
            idRequest = -1;
            if (string.IsNullOrEmpty(srcName) || srcAttr == null || srcAttr.Length == 0)
            {
                return;
            }
            string[] userAttr = m_userAttr.ToArray();

            // Converting the URL to Resource Signature.
            SPWebApplication spWebApp = m_web.Site.WebApplication;
            CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
            if (spWebApp != null && spAUC != null)
            {
                srcName = spAUC.UrlUpdate(spWebApp, srcName);
            }
            srcName = Globals.UrlToResSig(srcName).ToLower();
            //add additional IPAddress attributes for src attrs.
            Globals.AddIPAddrAttribute(ref srcAttr, m_strIPAddr);
            if (m_strArrXHeader != null && m_strArrXHeader.Count > 0)
            {
                srcAttr = srcAttr.Concat(m_strArrXHeader).ToArray();
            }
            srcAttr = Globals.SetAttrsResourceSignature(srcAttr, srcName); // Set "Resource Signature" to resource attributes.

            // check PreAuthz-Trimming.
            if (bTrimPreAuthZ && obj != null)
            {
                // Do PreAuthorization beofre Evaluation
                Globals.DoPreAuthorizationForTrim(m_web, srcName, m_action, ref userAttr, ref srcAttr, obj);
                // Check all attribute before Evaluation
                userAttr = Globals.CheckEvalAttributs(userAttr);
                srcAttr = Globals.CheckEvalAttributs(srcAttr);
            }

            if (m_bJpc)
            {
                SetJPCTrimRequest(obj, srcName, srcAttr, userAttr, out idRequest);
            }
            else
            {
                SetLPCTrimRequest(obj, srcName, srcAttr, userAttr, out idRequest, iCareOb);
            }
        }

        public void SetJPCTrimRequest(object obj, string srcName, string[] srcAttr, string[] userAttr, out int idRequest)
        {
            idRequest = -1;
            QueryCloudAZSDK.CEModel.CERequest ceRequest = CloudAZQuery.CreateQueryReq(m_action, m_strIPAddr, srcName, srcAttr, m_sid, m_userName, userAttr);
            if(ceRequest != null)
            {
                idRequest = m_listRequests.Count;
                m_listRequests.Add(ceRequest);
            }
        }

        public void SetLPCTrimRequest(object obj, string srcName, string[] srcAttr, string[] userAttr, out int idRequest, int iCareOb)
        {
            idRequest = -1;
            try
            {
                SDKWrapperLib.Request pReq = new SDKWrapperLib.Request();
                pReq.set_action(m_action);
                SDKWrapperLib.CEAttres appAttrs = new SDKWrapperLib.CEAttres();
                pReq.set_app("SharePoint", "", "", appAttrs);
                SDKWrapperLib.CEAttres userAttrs = new SDKWrapperLib.CEAttres();
                for (int i = 0; i + 1 < userAttr.Length; i = i + 2)
                {
                    userAttrs.add_attre(userAttr[i], userAttr[i + 1]);
                }
                pReq.set_user(m_sid, m_userName, userAttrs);
                pReq.set_noiseLevel(Convert.ToInt32(m_noiseLevel));
                pReq.set_performObligation(iCareOb); // iCareOb is 0 means don't care obligation, other value is care obligation.
                SDKWrapperLib.CEAttres sourceAttrs = new SDKWrapperLib.CEAttres();
                if (srcAttr != null)
                {
                    for (int i = 0; i + 1 < srcAttr.Length; i = i + 2)
                    {
                        sourceAttrs.add_attre(srcAttr[i], srcAttr[i + 1]);
                    }
                }
                pReq.set_param(srcName, "spe", sourceAttrs, 0);

                // Set this attribute to get "dont-care-acceptable" evaluation result.
                SDKWrapperLib.CEAttres AddtionalAttrs = new SDKWrapperLib.CEAttres();
                AddtionalAttrs.add_attre("dont-care-acceptable", "yes");
                pReq.set_param("environment", "", AddtionalAttrs, 2);

                m_thePc.set_request(pReq, m_iCookie);
                idRequest = m_reqCount;
                m_reqCount++;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SetLPCTrimRequest :", null, ex);
            }
        }

        public bool run(int iCareOb = 0)
        {
            m_bRun = false;
            try
            {
                if (m_bJpc)
                {
                    if (m_listRequests.Count > 0)
                    {
                        CloudAZQuery cloudAzQuery = CloudAZQuery.Instance;
                        QueryCloudAZSDK.QueryStatus queryStatus = QueryStatus.S_OK;
                        if (m_listRequests.Count == 1)
                        {
                            // Fix javapc issue to get "PolicyResult.DontCare" in pre-filter trimming.
                            List<QueryCloudAZSDK.CEModel.CEObligation> lsObligation = new List<QueryCloudAZSDK.CEModel.CEObligation>();
                            QueryCloudAZSDK.CEModel.PolicyResult emPolicyResult = QueryCloudAZSDK.CEModel.PolicyResult.DontCare;
                            cloudAzQuery.QueryColuAZPC(m_listRequests[0], ref lsObligation, ref emPolicyResult);
                            if (queryStatus.Equals(QueryCloudAZSDK.QueryStatus.S_OK))
                            {
                                m_listResults.Add(emPolicyResult);
                                m_listObligations.Add(lsObligation);
                            }
                        }
                        else
                        {
                            queryStatus = cloudAzQuery.MultipleQueryColuAZPC(m_listRequests, out m_listResults, out m_listObligations);
                        }
                        if (queryStatus.Equals(QueryCloudAZSDK.QueryStatus.S_OK))
                        {
                            m_bRun = true;
                        }
                    }
                }
                else if (m_reqCount > 0)
                {
                    int callResult = 0;
                    int iTimeout = Globals.GetPolicyDefaultTimeout() * m_reqCount;
                    m_thePc.check_resourceex(m_iCookie, "", 0, iTimeout, iCareOb, ref callResult);
                    if (callResult == (int)CETYPE.CEResult_t.CE_RESULT_SUCCESS) // evaluation success
                    {
                        m_bRun = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during run :", null, ex);
            }
            return m_bRun;
        }

        public bool GetTrimEvalResult(int idRequest)
        {
            QueryCloudAZSDK.CEModel.PolicyResult policyResult = QueryCloudAZSDK.CEModel.PolicyResult.Allow;
            return GetEvalResult(idRequest, ref policyResult);
        }

        public bool GetEvalResult(int idRequest, ref QueryCloudAZSDK.CEModel.PolicyResult policyResult)
        {
            try
            {
                if (!m_bRun)
                {
                    return m_bDefault;
                }

                if (m_bJpc)
                {
                    if (idRequest >= 0 && idRequest < m_listResults.Count)
                    {
                        policyResult = m_listResults[idRequest];
                    }
                }
                else
                {
                    int iObNum = 0;
                    int lResult = 0;
                    m_thePc.get_result(m_iCookie, idRequest, out lResult, out iObNum);
                    policyResult = (QueryCloudAZSDK.CEModel.PolicyResult)lResult;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetEvalResult :", null, ex);
            }
            return policyResult.Equals(QueryCloudAZSDK.CEModel.PolicyResult.Deny) ? false : true;
        }

        public bool GetObligations(int idRequest, List<Obligation> listObligations, string obName = null)
        {
            if (m_bRun && listObligations != null)
            {
                try
                {
                    if (m_bJpc)
                    {
                        if (idRequest >= 0 && idRequest < m_listObligations.Count)
                        {
                            List<QueryCloudAZSDK.CEModel.CEObligation>  jpcObligations = m_listObligations[idRequest];
                            foreach (QueryCloudAZSDK.CEModel.CEObligation jpcOb in jpcObligations)
                            {
                                string jpcObName = jpcOb.GetName();
                                if(!string.IsNullOrEmpty(obName) && !obName.Equals(jpcObName))
                                {
                                    continue; // Don't match the "obName".
                                }
                                Obligation ob = new Obligation();
                                ob.Name = jpcObName;
                                ob.Policy = jpcOb.GetPolicyName();
                                QueryCloudAZSDK.CEModel.CEAttres ceAttrs = jpcOb.GetCEAttres();
                                int count = ceAttrs.Count;
                                for (int i = 0; i < count; i++)
                                {
                                    QueryCloudAZSDK.CEModel.CEAttribute ceAttr = ceAttrs[i];
                                    if (!string.IsNullOrEmpty(ceAttr.Name) && !string.IsNullOrEmpty(ceAttr.Value))
                                    {
                                        ob.AddAttribute(ceAttr.Name, ceAttr.Value);
                                    }
                                }
                                listObligations.Add(ob);
                            }
                            return true;
                        }
                    }
                    else
                    {
                        int iObNum = 0;
                        int lResult = 0;
                        m_thePc.get_result(m_iCookie, idRequest, out lResult, out iObNum);
                        SDKWrapperLib.Obligation pcOb = new SDKWrapperLib.Obligation();
                        for (int i = 0; i < iObNum; i++)
                        {
                            m_thePc.get_obligation(m_iCookie, obName, idRequest, i, out pcOb);
                            if (pcOb != null)
                            {
                                string pcObName = string.Empty;
                                pcOb.get_name(ref pcObName);
                                if (!string.IsNullOrEmpty(obName) && !obName.Equals(pcObName))
                                {
                                    continue; // Don't match the "obName".
                                }
                                Obligation ob = new Obligation();
                                ob.Name = pcObName;
                                string pcObPolicyName = string.Empty;
                                pcOb.get_policyname(out pcObPolicyName);
                                ob.Policy = pcObPolicyName;
                                SDKWrapperLib.CEAttres attrs = null;
                                pcOb.get_attres(out attrs);
                                int count = 0;
                                attrs.get_count(out count);
                                for (int j = 0; j < count; j++)
                                {
                                    string key = "";
                                    string value = "";
                                    attrs.get_attre(j, out key, out value);
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        ob.AddAttribute(key, value);
                                    }
                                }
                                listObligations.Add(ob);
                            }
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during GetObligations :", null, ex);
                }

            }
            return false;
        }
    }
}
