using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;
using System.Net;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NextLabs.CSCInvoke;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Microsoft.IdentityModel.Claims;
using System.Security.Principal;

using QueryCloudAZSDK;
using QueryCloudAZSDK.CEModel;
using NextLabs.Diagnostic;
using LogLevel = NextLabs.Diagnostic.LogLevel;

namespace NextLabs.Common
{
    public class Global_Utils
    {
        public const string PREFILTER_ACTION_KEY = "ENTTMNT_FILTER";
        public static void SPELogEnforcementResult(string srcName, string action, CETYPE.CEResult_t call_result, CETYPE.CEResponse_t enforcement_result)
        {
            NLLogger.OutputLog(LogLevel.Debug, "SPELogEnforcementResult srcName: " + srcName + ", action: " + action);
            if (call_result == CETYPE.CEResult_t.CE_RESULT_TIMEDOUT)
                {
                NLLogger.OutputLog(LogLevel.Debug, "Enforcement_result: timeout", null);
                }
            else if (call_result == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                if (enforcement_result == CETYPE.CEResponse_t.CEAllow)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Enforcement_result: allow", null);
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Enforcement_result: deny", null);
                }
            }
        }

        public static void SPELogEnforcementInfo(string moduleName, string loginName, string remoteAddr, string srcName, string action, string[] srcAttr, string[] userAttr)
        {
            NLLogger.OutputLog(LogLevel.Debug, moduleName + ": thread ID = " + Thread.CurrentThread.ManagedThreadId + ", srcName = " + srcName + ", action = " + action + ", loginName = " + loginName + ", remoteAddr = " + remoteAddr, null);
            NLLogger.OutputLog(LogLevel.Debug, moduleName + ": User attributes: [" + string.Join(",", userAttr) + "]", null);
            NLLogger.OutputLog(LogLevel.Debug, moduleName + ": Resource attributes: [" + string.Join(",", srcAttr) + "]", null);
        }

        public void SPE_LogObligation(IntPtr localConnectHandle, CETYPE.CENoiseLevel_t NoiseLevel,
                                        CETYPE.CEResult_t call_result,
                                        string[] enforcement_obligation,
                                        CETYPE.CEResponse_t enforcement_result, SPWeb web)
        {
            if (call_result == CETYPE.CEResult_t.CE_RESULT_SUCCESS
                && (NoiseLevel == CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION || NoiseLevel == CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_APPLICATION))
            {
                string sitevalue = null;
                if (web != null)
                {
                    sitevalue = web.Url;
                }
                ReportObligationLog.DoReportLog(localConnectHandle, enforcement_obligation, sitevalue);
            }
        }

        public CETYPE.CEResponse_t SPE_Evaluation_CloudAZ(CETYPE.CEAction action, string remoteAddr, string loginName, string sid, CETYPE.CENoiseLevel_t NoiseLevel,
                                                    string srcName, string origin_srcName, ref string[] srcAttr, string targetName, ref string[] targetAttr,
                                                    ref string policyName, ref string policyMessage, SPWeb web, bool _SPE_LogObligation,
                                                   ref IntPtr _localConnectHandle, ref string[] enforcement_obligation, IPrincipal PrincipalUser)
        {
            String Input_SrcName = origin_srcName;
            String Src_Subtype = "";
            if (action == CETYPE.CEAction.Read)
            {
                for (int i = 0; i < srcAttr.Length; i += 2)
                {
                    if (srcAttr[i] != null && srcAttr[i] == CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE)
                    {
                        if (srcAttr[i + 1] != null)
                        {
                            Src_Subtype = srcAttr[i + 1];
                            break;
                        }
                    }
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            targetName = (targetName == null) ? "" : Globals.UrlToResSig(targetName).ToLower();
            CETYPE.CEUser user;
            string url = web.Url;

            // loginName is not used for matching, hence there is no need to
            // convert it to lower-case.  sid matching is not done as string
            // matching.  Hence it should not be converted to lower-case.
            string strLoginNameBeforeConverted = loginName;
            if (sid != null && sid.Equals(loginName))
            {
                loginName = NextLabs.Common.Utilities.ClaimUserConvertion(loginName);
                sid = loginName;
            }
            else
            {
                loginName = NextLabs.Common.Utilities.ClaimUserConvertion(loginName);
            }
            user = new CETYPE.CEUser(loginName, sid);

            string ActionStr = Globals.SPECommon_ActionConvert(action);
            string[] userAttr = Globals.SPECommon_GetUserAttr(PrincipalUser,web);
            srcAttr = Globals.SPECommon_GetPropBag(web, srcAttr);
            srcAttr = Globals.SetAttrsResourceSignature(srcAttr, srcName); // Set "Resource Signature" to resource attributes.
            remoteAddr = Globals.GetXHeaderIp(user.userName, web.Url, remoteAddr);
            //add additional IPAddress attributes and XHeader for src attrs.
            Globals.AddXHeaderAndIpAttribute(ref srcAttr, user.userName, web.Url, remoteAddr);

            userAttr = Globals.SPECommon_GetUserProfile(web, strLoginNameBeforeConverted, userAttr);
            //check if profile sid exists, if yes, remove
            string profileSid = GetAndRemoveIdAttribute(ref userAttr);
            if (!string.IsNullOrEmpty(profileSid) && UserSid.IsValidSid(user.userID, user.userName) != true && UserSid.IsValidSid(profileSid, user.userName) != false)
            {
                //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                //update user sid with profile sid
                user.userID = profileSid;
            }
            string[] emptyAttributes = new string[0];
            // Cache old source attributes to do tagging.
            string[] oldSrcAttr = new string[srcAttr.Length];
            Array.Copy(srcAttr, oldSrcAttr, srcAttr.Length);
            // Do PreAuthorization beofre Evaluation
            Globals.DoPreAuthorization(web, origin_srcName, srcName, ActionStr, ref userAttr, ref srcAttr, ref targetAttr);
            // Check all attribute before Evaluation
            userAttr = Globals.CheckEvalAttributs(userAttr);

            srcAttr = Globals.CheckEvalAttributs(srcAttr);
            targetAttr = Globals.CheckEvalAttributs(targetAttr);

            List<CEObligation> lstObligation = new List<CEObligation>();
            PolicyResult result = PolicyResult.DontCare;

            // Log information for enforcement.
            SPELogEnforcementInfo("SPE_Evaluation_CloudAZ", loginName, remoteAddr, srcName, ActionStr, srcAttr, userAttr);

            QueryCloudAZSDK.CEModel.CERequest ceRequest = CloudAZQuery.CreateQueryReq(ActionStr, remoteAddr, srcName, srcAttr, user.userID, user.userName, userAttr);
            QueryStatus qs = CloudAZQuery.Instance.QueryColuAZPC(ceRequest, ref lstObligation, ref result);
            NLLogger.OutputLog(LogLevel.Debug, "SPE_Evaluation_CloudAZ QueryStatus:" + qs);
            NLLogger.OutputLog(LogLevel.Debug, "SPE_Evaluation_CloudAZ result:" + result);

            if (qs == QueryStatus.S_OK)
            {
                Globals.GetPolicyNameMessageByObligation(lstObligation, ref policyName, ref policyMessage);
                NLLogger.OutputLog(LogLevel.Debug, "SPE_Evaluation_CloudAZ: enforcement_result:[{0}], loginName:[{1}], remoteAddr:[{2}]", new object[] { result, url, loginName, remoteAddr });                if (result == PolicyResult.Deny)
                {
                    return CETYPE.CEResponse_t.CEDeny;
                }
                else
                {
                    enforcement_obligation = Globals.ConvertObligationListtoArray(lstObligation);
                    Globals.EvalTagging(action, Input_SrcName, Src_Subtype, srcName, web, oldSrcAttr, enforcement_obligation);
                    return CETYPE.CEResponse_t.CEAllow;
                }
            }
            else
            {
                return Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;

            }
        }

        static public CETYPE.CEResponse_t EvaluateForEnforceStatus_CloudAZ(string userName, string sid, string clientAddress, string requestUrl)
        {
            //default attributes
            string[] emptyAttributes = new string[0];
            //construct user
            CETYPE.CEUser user = new CETYPE.CEUser(userName, sid);
            try
            {
                PolicyResult result = PolicyResult.DontCare;
                List<CEObligation> lstObligation = new List<CEObligation>();
                // Log information for enforcement.
                string[] srcAttr = {"Empty Attributes"};
                string[] userAttr = { "Empty Attributes" };
                SPELogEnforcementInfo("EvaluateForEnforceStatus_CloudAZ", userName, clientAddress, requestUrl, PREFILTER_ACTION_KEY, srcAttr, userAttr);

                QueryCloudAZSDK.CEModel.CERequest ceRequest = CloudAZQuery.CreateQueryReq(PREFILTER_ACTION_KEY, clientAddress, requestUrl, srcAttr, user.userID, user.userName, userAttr);
                QueryStatus qs = CloudAZQuery.Instance.QueryColuAZPC(ceRequest, ref lstObligation, ref result);
                if (qs == QueryStatus.S_OK)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "EvaluateForEnforceStatus_CloudAZ: enforcement_result=" + result, null);
                    if (result == PolicyResult.Deny)
                    {
                        return CETYPE.CEResponse_t.CEDeny;
                    }
                    else
                    {
                        return CETYPE.CEResponse_t.CEAllow;
                    }
                }
                else
                {
                    return Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during EvaluateForEnforceStatus_CloudAZ:", null, ex);

                // Allow or Deny depend on the registry key "PolicyDefaultBahavior", modify by George.
                return Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
            }
        }

        public CETYPE.CEResponse_t SPE_Evaluation(CETYPE.CEAction action, string remoteAddr, string loginName, string sid, CETYPE.CENoiseLevel_t NoiseLevel,
                                                    string srcName, string origin_srcName, ref string[] srcAttr, string targetName, ref string[] targetAttr,
                                                    ref string policyName, ref string policyMessage, SPWeb web, bool _SPE_LogObligation,
                                                   ref IntPtr _localConnectHandle, ref string[] enforcement_obligation, IPrincipal PrincipalUser)
        {
            String Input_SrcName = origin_srcName;
            String Src_Subtype = "";
            if (action == CETYPE.CEAction.Read)
            {
                for (int i = 0; i < srcAttr.Length; i += 2)
                {
                    if (srcAttr[i] != null && srcAttr[i] == CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE)
                    {
                        if (srcAttr[i + 1] != null)
                        {
                            Src_Subtype = srcAttr[i + 1];
                            break;
                        }
                    }
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            if (targetName == null)
            {
                targetName = "";
            }
            else
            {
                targetName = Globals.UrlToResSig(targetName).ToLower();
            }
            CETYPE.CEResponse_t enforcement_result;
            //string[] enforcement_obligation;
            CETYPE.CEResult_t call_result;
            IntPtr localConnectHandle;
            CETYPE.CEUser user;
            string url = web.Url;
            bool oldConnectionExisted = (Globals.connectHandle != IntPtr.Zero);

            while (true)
            {
                // (The thread synchronization code here between calling
                // _Initialize and calling _Close doesn't really work.  Need to be
                // fixed later.)
                // Try to connect if it's not already connected.
                lock (typeof(Globals))
                {
                    if (Globals.connectHandle == IntPtr.Zero)
                    {
                        CETYPE.CEApplication app =
                            new CETYPE.CEApplication("SharePoint", null, null);
                        CETYPE.CEResult_t result;

                        // We are passing dummy strings instead of null's or empty
                        // strings for userName and userID.  This is to avoid a
                        // crash problem in cepdpman.exe.
                        //
                        // The crash happens when NL_getUserId() in the SDK gets an
                        // "Access is denied" error from OpenProcessToken(), then
                        // sends null's to cepdpman, which probably de-references
                        // the null's and crashes.  The "Access is denied" error
                        // happens when, after all the w3wp.exe processes have
                        // exited due to idle time, the first remote user to
                        // connect to IIS is "NT AUTHORITY\LOCAL SERVICE" instead
                        // of a real user.
                        //
                        // Passing dummy strings here ensures that NL_getUserId()
                        // is never called.
                        user = new CETYPE.CEUser("dummyName", "dummyId");

                        result = CESDKAPI.CECONN_Initialize(app, user, null,
                                                            out Globals.connectHandle,
                                                            Globals.connectTimeoutMs);

                        if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                        {
                            Globals.connectHandle = IntPtr.Zero;
                            NLLogger.OutputLog(LogLevel.Debug, "CallEval: Can't connect to SDK! Url:[{0}], loginName:[{1}], RemoteAddr:[{2}]", new object[] { url, loginName, remoteAddr });

                            // Always ALLOW when error occurrs.
                            return CETYPE.CEResponse_t.CEAllow;
                        }
                    }

                    localConnectHandle = Globals.connectHandle;
                }

                // loginName is not used for matching, hence there is no need to
                // convert it to lower-case.  sid matching is not done as string
                // matching.  Hence it should not be converted to lower-case.
                string strLoginNameBeforeConverted = loginName;
                if (sid != null && sid.Equals(loginName))
                {
                    loginName = NextLabs.Common.Utilities.ClaimUserConvertion(loginName);
                    sid = loginName;
                }
                else
                {
                    loginName = NextLabs.Common.Utilities.ClaimUserConvertion(loginName);
                }
                user = new CETYPE.CEUser(loginName, sid);

                string ActionStr = Globals.SPECommon_ActionConvert(action);
                string[] userAttr = Globals.SPECommon_GetUserAttr(PrincipalUser,web);
                srcAttr = Globals.SPECommon_GetPropBag(web, srcAttr);
                srcAttr = Globals.SetAttrsResourceSignature(srcAttr, srcName); // Set "Resource Signature" to resource attributes.
                remoteAddr = Globals.GetXHeaderIp(user.userName, web.Url, remoteAddr);
                uint ipNumber = Globals.IPAddressToIPNumber(remoteAddr);
                //add additional IPAddress attributes and XHeader for src attrs.
                Globals.AddXHeaderAndIpAttribute(ref srcAttr, user.userName, web.Url, remoteAddr);
                userAttr = Globals.SPECommon_GetUserProfile(web, strLoginNameBeforeConverted, userAttr);
                //check if profile sid exists, if yes, remove
                string profileSid = GetAndRemoveIdAttribute(ref userAttr);
                if (!string.IsNullOrEmpty(profileSid)
                    && UserSid.IsValidSid(user.userID, user.userName) != true
                    && UserSid.IsValidSid(profileSid, user.userName) != false)
                {
                    //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                    //update user sid with profile sid
                    user.userID = profileSid;
                }
                string[] emptyAttributes = new string[0];
                int evalTimeoutMs = Globals.GetPolicyDefaultTimeout();
                // Cache old source attributes to do tagging.
                string[] oldSrcAttr = new string[srcAttr.Length];
                Array.Copy(srcAttr, oldSrcAttr, srcAttr.Length);
                // Do PreAuthorization beofre Evaluation
                Globals.DoPreAuthorization(web, origin_srcName, srcName, ActionStr, ref userAttr, ref srcAttr, ref targetAttr);
                // Check all attribute before Evaluation
                userAttr = Globals.CheckEvalAttributs(userAttr);
                srcAttr = Globals.CheckEvalAttributs(srcAttr);
                targetAttr = Globals.CheckEvalAttributs(targetAttr);

                // Log information for enforcement.
                Global_Utils.SPELogEnforcementInfo("CheckPortalResource", loginName, remoteAddr, srcName, ActionStr, srcAttr, userAttr);

                call_result = CESDKAPI.CEEVALUATE_CheckResources
                    (localConnectHandle,
                    ActionStr,
                    new CETYPE.CEResource(srcName, "spe"),
                    ref srcAttr,
                    new CETYPE.CEResource(targetName, "spe"),
                    ref targetAttr,
                    user,
                    ref userAttr,
                    new CETYPE.CEApplication("SharePoint", null, null),
                    ref emptyAttributes,
                    ref emptyAttributes,
                    ipNumber,
                    true,
                    NoiseLevel,
                    out enforcement_obligation,
                    out enforcement_result,
                    evalTimeoutMs);
                _localConnectHandle = localConnectHandle;

                // Log enforcement result.
                Global_Utils.SPELogEnforcementResult(srcName, ActionStr, call_result, enforcement_result);

                if (_SPE_LogObligation)
                    SPE_LogObligation(localConnectHandle, NoiseLevel, call_result, enforcement_obligation, enforcement_result, web);
                if (call_result == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                {
                    //Do tagging
                    // Get the policy message.  This is the ONLY delegated
                    // obligation we understand

                    if (enforcement_obligation.Length > 0)
                    {
                        // Dump the key/values into the hashtable for easy manipulation
                        Dictionary<string, string> obligations = new Dictionary<string, string>();

                        for (int i = 0; i < enforcement_obligation.Length; i += 2)
                        {
                            obligations.Add(enforcement_obligation[i], enforcement_obligation[i + 1]);
                        }

                        int obligation_count = 0;
                        try
                        {
                            string count = obligations[CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT];
                            obligation_count = int.Parse(count);
                        }
                        catch
                        {
                        }

                        for (int i = 0; i < obligation_count; i++)
                        {
                            // Make sure we get the notify delegated obligation, which is the only thing we understand
                            string namekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_NAME + ":" + (i + 1);
                            if (obligations.ContainsKey(namekey))
                            {
                                string name = obligations[namekey];
                                if (!name.Equals(CETYPE.CEAttrVal.CE_OBLIGATION_NOTIFY, StringComparison.OrdinalIgnoreCase))
                                    continue;
                            }

                            string policyNamekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_POLICY + ":" + (i + 1);
                            if (obligations.ContainsKey(policyNamekey))
                            {
                                policyName = obligations[policyNamekey];
                            }

                            string policyMessagekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1);
                            if (obligations.ContainsKey(policyMessagekey))
                            {
                                policyMessage = obligations[policyMessagekey];
                            }
                        }
                        if (string.IsNullOrEmpty(policyMessage))
                        {
                            Globals.GetPolicyNameMessageByObligation(enforcement_obligation, ref policyName, ref policyMessage);
                        }
                    }
                    if (enforcement_result.Equals(CETYPE.CEResponse_t.CEAllow))
                    {
                        Globals.EvalTagging(action, Input_SrcName, Src_Subtype, srcName, web, oldSrcAttr, enforcement_obligation);
                    }
                    return enforcement_result;
                }
                else
                {
                    if ((call_result == CETYPE.CEResult_t.CE_RESULT_CONN_FAILED) || (call_result == CETYPE.CEResult_t.CE_RESULT_THREAD_NOT_INITIALIZED))
                    {
                        lock (typeof(Globals))
                        {
                            if (Globals.connectHandle == localConnectHandle)
                            {
                                CESDKAPI.CECONN_Close(Globals.connectHandle,
                                                      Globals.connectTimeoutMs);
                                Globals.connectHandle = IntPtr.Zero;
                            }
                        }

                        if (oldConnectionExisted)
                        {
                            // An old connection existed but died.  It might be the
                            // case where the server has been stopped and then
                            // re-started, so we try to re-connect once.
                            oldConnectionExisted = false;
                        }
                        else
                        {
                            // The new connection gave us error upon calling Eval.
                            // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                            return Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
                        }
                    }
                    else
                    {
                        // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                        return Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
                    }
                }
            } /* while (true) */
        }
        /// <summary>
        /// if the sid exists in array, return it and remove it from array
        /// </summary>
        /// <param name="userAtributes"></param>
        /// <returns></returns>
        public static string GetAndRemoveIdAttribute(ref string[] userAtributes)
        {
            string sid = null;
            if (userAtributes != null)
            {
                var list = new List<string>(userAtributes);
                int profileSidIndex = list.IndexOf("id");
                while (profileSidIndex >= 0)
                {
                    sid = list[profileSidIndex + 1];
                    list.RemoveRange(profileSidIndex, 2);

                    //check if still contains id attribute
                    profileSidIndex = list.IndexOf("id");
                }
                userAtributes = list.ToArray();
            }
            return sid;
        }

        public void SPE_AttrCheck(ref string[] srcAttr, ref string[] targetAttr, string targetName)
        {
            // HACK
            // This is a hack to put the URL into the Attr, which is required by the policy
            ArrayList tmplist = new ArrayList(); // array doesn't grow

            for (int i = 0; i < srcAttr.GetLength(0); i += 2)
            {
                // SDK is restrictive, and it requires passing non-empty attributes
                // So, we will trip any attribute that has zero length
                string k = (string)srcAttr.GetValue(i);
                string v = (string)srcAttr.GetValue(i + 1);

                if ((k != null && k.Length > 0) && (v != null && v.Length > 0))
                {
                    tmplist.Add(k);

                    if (k == CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY ||
                        k == CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY)
                    {
                        tmplist.Add(v);
                    }
                    else
                    {
                        tmplist.Add(v.ToLower());
                    }
                }
            }

            srcAttr = (string[])tmplist.ToArray(typeof(string));

            if (!string.IsNullOrEmpty(targetName))
            {
                ArrayList tmptargetlist = new ArrayList();

                for (int i = 0; i < targetAttr.GetLength(0); i += 2)
                {
                    string k = (string)targetAttr.GetValue(i);
                    string v = (string)targetAttr.GetValue(i + 1);

                    if (k.Length > 0 && v.Length > 0)
                    {
                        tmptargetlist.Add(k);

                        if (k == CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY ||
                            k == CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY)
                        {
                            tmptargetlist.Add(v);
                        }
                        else
                        {
                            tmptargetlist.Add(v.ToLower());
                        }
                    }
                }
                targetAttr = (string[])tmptargetlist.ToArray(typeof(string));
            }

            // End of hack
        }

        public void SPE_AlternateUrlCheck(SPWeb web, ref string srcName, ref string targetName)
        {
			//It is a host named site, no need to check.
            if (web.Site.HostHeaderIsSiteName)	return;

            // For bug #8567, Update come in URL with the default Host in
            // AlternateUrl collection if it exists.
            // Gavin Ye, Feb. 22, 2009
            SPWebApplication spWebApp = web.Site.WebApplication;
            string url = CommonVar.GetSPWebContent(web, "url");
            if (null != spWebApp)
            {
                CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
                if (null != spAUC)
                {
                    srcName = spAUC.UrlUpdate(spWebApp, srcName);
                    targetName = spAUC.UrlUpdate(spWebApp, targetName);
                }
            }
            // End. Gavin Ye, Feb. 22, 2009
        }

        public void SPE_NoiseLevel_Detection(CETYPE.CEAction action,
                                                                    string srcName,
                                                                    string remoteAddr,
                                                                    string loginName,
                                                                    string sid,
                                                                    string before_url,
                                                                    string after_url,
                                                                    ref CETYPE.CENoiseLevel_t NoiseLevel,
                                                                    string ModuleName,
                                                                    SPWeb web)
        {
            //William add this to pass the command if following continuous request or event
            //Such as this: Command A,then in a short time the same url comes command B, then command B
            //shall be evaluated but no log
            try
            {
                string userkey = loginName + " " + sid + " " + remoteAddr;
                string url = web.Url;
                if (userkey != null)
                    userkey = userkey.ToLower();
                bool containkey = Globals.ActionMap.ContainsKey(userkey);

                //We only consider CE_NOISE_LEVEL_USER_ACTION condition
                if (NoiseLevel == CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION)
                {
                    if (containkey)
                    {
                        UInt64 current_time = ((UInt64)
                             ((DateTime.Now.ToUniversalTime() -
                               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                              TotalMilliseconds));
                        int pos = 0;
                        for (int i = 0; i < Globals.ActionMap[userkey].Count; i++)
                        {
                            Globals.UserActionInfo UserAction = Globals.ActionMap[userkey][i];
                            if ((UserAction.url == srcName)
                                || ((UserAction.before_url != null && before_url != null) && (UserAction.before_url == before_url))
                                || ((UserAction.after_url != null && after_url != null) && (UserAction.after_url == after_url)))
                            {
                                //Must be in same module
                                if (UserAction.ModuleName == ModuleName)
                                {
                                    //If in event handler, must the same action type, otherwise not a must
                                    if ((ModuleName == Globals.EventHandlerName && UserAction.action == action)
                                        || ModuleName == Globals.HttpModuleName)
                                    {
                                        UInt64 time = UserAction.time;
                                        UInt64 time_minus = current_time - time;
                                        if (time_minus < Globals.logTimeoutMs)
                                        {
                                            NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_APPLICATION;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        Globals.UserActionInfo NewUserAction;
                        NewUserAction.url = srcName;
                        NewUserAction.time = current_time;
                        NewUserAction.before_url = before_url;
                        NewUserAction.after_url = after_url;
                        NewUserAction.action = action;
                        NewUserAction.ModuleName = ModuleName;
                        lock (Globals.syncActionMapRoot)
                        {
                            Globals.ActionMap[userkey].Add(NewUserAction);
                        }
                        //Remove the actions that is less than the new action in Globals.logTimeoutMs
                        for (int i = 0; i < Globals.ActionMap[userkey].Count; i++)
                        {
                            Globals.UserActionInfo UserAction = Globals.ActionMap[userkey][i];
                            if ((current_time - UserAction.time) < Globals.logTimeoutMs)
                            {
                                pos = i;
                                break;
                            }
                        }
                        if (pos > 0)
                        {
                            lock (Globals.syncActionMapRoot)
                            {
                                Globals.ActionMap[userkey].RemoveRange(0, pos);
                            }
                        }
                    }
                    else
                    {
                        Globals.UserActionInfo NewUserAction;
                        NewUserAction.url = srcName;
                        NewUserAction.before_url = before_url;
                        NewUserAction.after_url = after_url;
                        NewUserAction.action = action;
                        NewUserAction.ModuleName = ModuleName;
                        NewUserAction.time = ((UInt64)
                             ((DateTime.Now.ToUniversalTime() -
                               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                              TotalMilliseconds));
                        List<Globals.UserActionInfo> UserActionInfo_list = new List<Globals.UserActionInfo>();
                        UserActionInfo_list.Add(NewUserAction);
                        lock (Globals.syncActionMapRoot)
                        {
                            Globals.ActionMap[userkey] = UserActionInfo_list;
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
