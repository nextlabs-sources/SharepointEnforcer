using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NextLabs.CSCInvoke;
using System.Security.Principal;
using System.Web;
using System.Diagnostics;
using QueryCloudAZSDK;
using QueryCloudAZSDK.CEModel;
using NextLabs.Diagnostic;
using LogLevel = NextLabs.Diagnostic.LogLevel;

namespace NextLabs.Common
{
    public class Evaluator
    {
        public const int ConnectTimeoutMs = 5 * 1000;
        public const string SPSchemeName = "sharepoint";
        public const string CEATTR_NUMVALUES_KEY = "CE_ATTR_OBLIGATION_NUMVALUES";
        public const string CacheHintKey = "CE_CACHE_HINT";

        private List<IObligation> m_IObligationList;
        private List<Obligation> m_Obligations;
        private IntPtr m_ConnectHandle;
        private LogObligation m_LogObligation;

        private object m_evalObj;
        public object EVALOBJECT
        {
            get
            {
                return m_evalObj;
            }
            set
            {
                m_evalObj = value;
            }
        }

        public Evaluator()
        {
            m_ConnectHandle = IntPtr.Zero;
            m_IObligationList = new List<IObligation>();
            m_Obligations = new List<Obligation>();

            m_LogObligation = new LogObligation();
            m_IObligationList.Add(m_LogObligation);
        }

        public List<Obligation> GetObligations()
        {
            return m_Obligations;
        }

        public void RegisterIObligation(IObligation iObligation)
        {
            m_IObligationList.Add(iObligation);
        }

        public bool CheckFile(ref EvaluatorContext context)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Evaluator CheckFile Enter.", null);
            if (!Utilities.SPECommon_Isup())
            {
                context.Allow = true;
                context.Allow = Globals.GetPolicyDefaultBehavior();
                return context.Allow;
            }
            long ipNumber = Utilities.IPAddressToIPNumber(context.RemoteAddress);
            NoiseLevel noiseLevel = context.NoiseLevel;
            CETYPE.CEUser user;
            string[] enforcementObligation;
            CETYPE.CEResponse_t enforcementResult;
            CETYPE.CEResult_t callResult;
            string srcName = context.SrcName;
            string targetName = context.TargetName;
            string[] srcAttributes = null;
            string[] targetAttributes = null;
            if (String.IsNullOrEmpty(context.UserSid))
            {
                context.UserSid = UserSid.GetUserSid(context.Web, context.UserName);
                if (String.IsNullOrEmpty(context.UserSid))
                    context.UserSid = context.UserName;
            }

            Initialise();
            if (m_ConnectHandle == IntPtr.Zero)
            {
                context.FailedReason = "Warning - Failed to connect to Policy Controller.";
                context.Allow = true;
                return true;
            }
            if (context.UserSid != null && context.UserSid.Equals(context.UserName))
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
                context.UserSid = context.UserName;
            }
            else
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
            }
            user = new CETYPE.CEUser(context.UserName, context.UserSid);

            srcName = srcName.ToLower();
            targetName = targetName.ToLower();

            if (context.UserName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                || context.UserName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                noiseLevel = (NoiseLevel)CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
            }

            ConvertAttributeListToArray(context.SrcAttributes, ref srcAttributes);
            ConvertAttributeListToArray(context.TargetAttributes, ref targetAttributes);

            int EvalTimeoutMs = Globals.GetPolicyDefaultTimeout();
            EvaluatorApplication app = EvaluatorApplication.CreateInstance();
            callResult = CESDKAPI.CEEVALUATE_CheckFile(m_ConnectHandle,
                (CETYPE.CEAction)context.ActionType, srcName, ref srcAttributes,
                targetName, ref targetAttributes, (uint)ipNumber, user, app.Application,
                true, (CETYPE.CENoiseLevel_t)noiseLevel,
                out enforcementObligation, out enforcementResult, EvalTimeoutMs);

            if (callResult == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                ParseObligations(ref context, ref enforcementObligation);

                // Run all registered IObligation
                foreach (IObligation iOb in m_IObligationList)
                {
                    iOb.Process(m_Obligations, m_ConnectHandle);
                }

                context.Allow = (enforcementResult == CETYPE.CEResponse_t.CEAllow) ? true : false;
            }
            else
            {
                // TODO add fail log
                context.FailedReason = "Warning - Failed to evaluate to Policy Controller.";
                context.Allow = true;
            }

            Release();

            return context.Allow;
        }

        public bool CheckPortal(ref EvaluatorContext context)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Evaluator CheckPortal Enter.", null);
            if (!Utilities.SPECommon_Isup())
            {
                context.Allow = true;
                context.Allow = Globals.GetPolicyDefaultBehavior();
                return context.Allow;
            }
            NoiseLevel noiseLevel = context.NoiseLevel;
            CETYPE.CEUser user;
            string[] enforcementObligation;
            CETYPE.CEResponse_t enforcementResult;
            CETYPE.CEResult_t callResult;
            string srcName = context.SrcName;
            string targetName = context.TargetName;
            string[] srcAttributes = null;
            string[] targetAttributes = null;

            if (String.IsNullOrEmpty(context.UserSid))
            {
                context.UserSid=UserSid.GetUserSid(context.Web, context.UserName);
                if (String.IsNullOrEmpty(context.UserSid))
                    context.UserSid = context.UserName;
            }

            Initialise();
            if (m_ConnectHandle == IntPtr.Zero)
            {
                context.FailedReason = "Warning - Failed to connect to Policy Controller.";
                return true;
            }

            m_LogObligation.Web = context.Web;

            string strLoginNameBeforeConverted = context.UserName;
            if (context.UserSid != null && context.UserSid.Equals(context.UserName))
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
                context.UserSid = context.UserName;
            }
            else
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
            }
            user = new CETYPE.CEUser(context.UserName, context.UserSid);

            if (context.Web != null)
            {
                SPWebApplication spWebApp = context.Web.Site.WebApplication;
                CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
                if (spWebApp != null && spAUC != null)
                {
                    srcName = spAUC.UrlUpdate(spWebApp, context.SrcName);
                    targetName = spAUC.UrlUpdate(spWebApp, context.TargetName);
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            if (!String.IsNullOrEmpty(targetName))
                targetName = Globals.UrlToResSig(targetName).ToLower();

            if (context.UserName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                || context.UserName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                noiseLevel = (NoiseLevel)CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
            }

            ConvertAttributeListToArray(context.SrcAttributes, ref srcAttributes);

            context.RemoteAddress = Globals.GetXHeaderIp(context.UserName, context.Web.Url, context.RemoteAddress);
            uint ipNumber = Utilities.IPAddressToIPNumber(context.RemoteAddress);
            //add additional IPAddress attributes and XHeader for src attrs.
            Globals.AddXHeaderAndIpAttribute(ref srcAttributes, context.UserName, context.Web.Url, context.RemoteAddress);

            ConvertAttributeListToArray(context.TargetAttributes, ref targetAttributes);

            string ActionStr = Globals.SPECommon_ActionConvert(context.ActionType);
            string[] userAttr = Globals.SPECommon_GetUserAttr(context.PrincipalUser,context.Web);
            srcAttributes = Globals.SPECommon_GetPropBag(context.Web, srcAttributes);
            srcAttributes = Globals.SetAttrsResourceSignature(srcAttributes, srcName); // Set "Resource Signature" to resource attributes.
            userAttr = Globals.SPECommon_GetUserProfile(context.Web, strLoginNameBeforeConverted, userAttr);

            //check if profile sid exists, if yes, remove
            string profileSid = Global_Utils.GetAndRemoveIdAttribute(ref userAttr);
            if (!string.IsNullOrEmpty(profileSid)
                && UserSid.IsValidSid(user.userID, user.userName) != true
                && UserSid.IsValidSid(profileSid, user.userName) != false)
            {
                //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                //update user sid with profile sid
                user.userID = profileSid;
            }
            string[] emptyAttributes = new string[0];
            int EvalTimeoutMs = Globals.GetPolicyDefaultTimeout();
            // Do PreAuthorization beofre Evaluation
            Globals.DoPreAuthorizationForTrim(context.Web, srcName, ActionStr, ref userAttr, ref srcAttributes, EVALOBJECT);
            // Check all attribute before Evaluation
            userAttr = Globals.CheckEvalAttributs(userAttr);
            srcAttributes = Globals.CheckEvalAttributs(srcAttributes);
            EvaluatorApplication app = EvaluatorApplication.CreateInstance();

            // Log information for enforcement.
            Global_Utils.SPELogEnforcementInfo("CheckPortal", context.UserName, context.RemoteAddress, srcName, ActionStr, srcAttributes, userAttr);

            callResult = CESDKAPI.CEEVALUATE_CheckResources(m_ConnectHandle, ActionStr,
                new CETYPE.CEResource(srcName, "spe"), ref srcAttributes,
                new CETYPE.CEResource(targetName, "spe"), ref targetAttributes, user, ref userAttr,
                app.Application, ref emptyAttributes, ref emptyAttributes, ipNumber,
                true, (CETYPE.CENoiseLevel_t)noiseLevel,
                out enforcementObligation, out enforcementResult, EvalTimeoutMs);

            // Log enforcement result.
            Global_Utils.SPELogEnforcementResult(srcName, ActionStr, callResult, enforcementResult);

            if (callResult == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                ParseObligations(ref context, ref enforcementObligation);

                // Run all registered IObligation
                foreach (IObligation iOb in m_IObligationList)
                {
                    iOb.Process(m_Obligations, m_ConnectHandle);
                }
                context.Allow = (enforcementResult == CETYPE.CEResponse_t.CEAllow) ? true : false;
            }
            else
            {
                // TODO add fail log
                context.FailedReason = "Warning - Failed to evaluate to Policy Controller.";

                // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                context.Allow = Globals.GetPolicyDefaultBehavior();
            }

            Release();

            return context.Allow;
        }

        public bool CheckPortal_CloudAZ(ref EvaluatorContext context)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Evaluator CheckPortal_CloudAZ Enter.");
            NoiseLevel noiseLevel = context.NoiseLevel;
            CETYPE.CEUser user;
            string[] enforcementObligation;

            string srcName = context.SrcName;
            string targetName = context.TargetName;
            string[] srcAttributes = null;
            string[] targetAttributes = null;

            if (String.IsNullOrEmpty(context.UserSid))
            {
                context.UserSid=UserSid.GetUserSid(context.Web, context.UserName);
                if (String.IsNullOrEmpty(context.UserSid))
                    context.UserSid = context.UserName;
            }

            m_LogObligation.Web = context.Web;
            string strLoginNameBeforeConverted = context.UserName;
            if (context.UserSid != null && context.UserSid.Equals(context.UserName))
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
                context.UserSid = context.UserName;
            }
            else
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
            }
            user = new CETYPE.CEUser(context.UserName, context.UserSid);

            if (context.Web != null)
            {
                SPWebApplication spWebApp = context.Web.Site.WebApplication;
                CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
                if (spWebApp != null && spAUC != null)
                {
                    srcName = spAUC.UrlUpdate(spWebApp, context.SrcName);
                    targetName = spAUC.UrlUpdate(spWebApp, context.TargetName);
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            if (!String.IsNullOrEmpty(targetName))
                targetName = Globals.UrlToResSig(targetName).ToLower();

            if (context.UserName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                || context.UserName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                noiseLevel = (NoiseLevel)CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
            }

            ConvertAttributeListToArray(context.SrcAttributes, ref srcAttributes);
            context.RemoteAddress = Globals.GetXHeaderIp(context.UserName, context.Web.Url, context.RemoteAddress);
            uint ipNumber = Utilities.IPAddressToIPNumber(context.RemoteAddress);
            //add additional IPAddress attributes and XHeader for src attrs.
            Globals.AddXHeaderAndIpAttribute(ref srcAttributes, context.UserName, context.Web.Url, context.RemoteAddress);

            ConvertAttributeListToArray(context.TargetAttributes, ref targetAttributes);
            string ActionStr = Globals.SPECommon_ActionConvert(context.ActionType);
            string[] userAttr = Globals.SPECommon_GetUserAttr(context.PrincipalUser,context.Web);
            srcAttributes = Globals.SPECommon_GetPropBag(context.Web, srcAttributes);
            srcAttributes = Globals.SetAttrsResourceSignature(srcAttributes, srcName); // Set "Resource Signature" to resource attributes.
            userAttr = Globals.SPECommon_GetUserProfile(context.Web, strLoginNameBeforeConverted, userAttr);

            //check if profile sid exists, if yes, remove
            string profileSid = Global_Utils.GetAndRemoveIdAttribute(ref userAttr);
            if (!string.IsNullOrEmpty(profileSid)
                && UserSid.IsValidSid(user.userID, user.userName) != true
                && UserSid.IsValidSid(profileSid, user.userName) != false)
            {
                //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                //update user sid with profile sid
                user.userID = profileSid;
            }
            string[] emptyAttributes = new string[0];
            int EvalTimeoutMs = Globals.GetPolicyDefaultTimeout();
            // Do PreAuthorization beofre Evaluation
            Globals.DoPreAuthorizationForTrim(context.Web, srcName, ActionStr, ref userAttr, ref srcAttributes, EVALOBJECT);
            // Check all attribute before Evaluation
            userAttr = Globals.CheckEvalAttributs(userAttr);
            srcAttributes = Globals.CheckEvalAttributs(srcAttributes);

            EvaluatorApplication app = EvaluatorApplication.CreateInstance();
            PolicyResult result = PolicyResult.DontCare;
            List<CEObligation> lstObligation = new List<CEObligation>();

            // Log information for enforcement.
            Global_Utils.SPELogEnforcementInfo("CheckPortal_CloudAZ", context.UserName, context.RemoteAddress, srcName, ActionStr, srcAttributes, userAttr);

            QueryCloudAZSDK.CEModel.CERequest ceRequest = CloudAZQuery.CreateQueryReq(ActionStr, context.RemoteAddress, srcName, srcAttributes, context.UserSid, context.UserName, userAttr);
            QueryStatus qs = CloudAZQuery.Instance.QueryColuAZPC(ceRequest, ref lstObligation, ref result);
            NLLogger.OutputLog(LogLevel.Debug, "CheckPortal_CloudAZ: enforcement_result=" + result);

            if (qs == QueryStatus.S_OK)
            {
                enforcementObligation = Globals.ConvertObligationListtoArray(lstObligation);
                ParseObligations(ref context, ref enforcementObligation);

                // Run all registered IObligation
                foreach (IObligation iOb in m_IObligationList)
                {
                    iOb.Process(m_Obligations, m_ConnectHandle);
                }

                context.Allow = (result == PolicyResult.Deny) ? false : true;
            }
            else
            {
                context.FailedReason = "Warning - Failed to evaluate to Policy Controller.";

                // Allow or Deny depend on the registry key "PolicyDefaultBahavior", modify by George.
                context.Allow = Globals.GetPolicyDefaultBehavior();
            }

          return context.Allow;
        }

        private string ConstructListUrl(SPWeb web, SPList list)
        {
            return Globals.ConstructListUrl(web, list);
        }

        public bool CheckPortalResource(ref EvaluatorContext context)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Evaluator CheckPortalResource Enter.");
            if (!Utilities.SPECommon_Isup())
            {
                context.Allow = true;
                context.Allow = Globals.GetPolicyDefaultBehavior();
                return context.Allow;
            }
            if (String.IsNullOrEmpty(context.ActionStr))
            {
                if (Globals.g_JPCParams.bUseJavaPC)
                    return CheckPortal_CloudAZ(ref context);
                else
                    return CheckPortal(ref context);
            }

            NoiseLevel noiseLevel = context.NoiseLevel;
            CETYPE.CEUser user;
            string[] enforcementObligation;
            CETYPE.CEResponse_t enforcementResult;
            CETYPE.CEResult_t callResult;
            string srcName = context.SrcName;
            string targetName = context.TargetName;
            string[] srcAttributes = null;
            string[] targetAttributes = null;
            string[] emptyAttributes = new string[0];
            if (String.IsNullOrEmpty(context.UserSid))
            {
                context.UserSid = UserSid.GetUserSid(context.Web, context.UserName);
                if (String.IsNullOrEmpty(context.UserSid))
                    context.UserSid = context.UserName;
            }

            Initialise();
            if (m_ConnectHandle == IntPtr.Zero)
            {
                context.FailedReason = "Warning - Failed to connect to Policy Controller.";
                return true;
            }

            m_LogObligation.Web = context.Web;
            string strLoginNameBeforeConverted = context.UserName;
            if (context.UserSid != null && context.UserSid.Equals(context.UserName))
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
                context.UserSid = context.UserName;
            }
            else
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
            }
            user = new CETYPE.CEUser(context.UserName, context.UserSid);
            {
                Object _obj = null;
                SPListItem _listitem = null;
                SPFile _file = null;
                SPList _list = null;
                SPFolder _folder = null;
                try
                {
                    _obj = m_LogObligation.Web.GetObject(srcName);
                }
                catch
                {
                }
                if (_obj != null)
                {
                    if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFile)))
                    {
                        _file = (SPFile)_obj;
                        if (_file != null)
                            _folder = _file.ParentFolder;
                        if (_folder != null)
                        {
                            Guid _guid = _folder.ParentListId;
                            _list = m_LogObligation.Web.Lists.GetList(_guid, true);
                        }
                    }
                    else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPListItem)))
                    {
                        _listitem = (SPListItem)_obj;
                    }
                }
                if (_listitem != null)
                {
                    _list = _listitem.ParentList;
                    m_LogObligation.Url = m_LogObligation.Web.Url + "/" + _listitem.Url;
                    int _pos = m_LogObligation.Url.LastIndexOf("/");
                    if (_pos != -1)
                    {
                        m_LogObligation.Location = m_LogObligation.Url.Substring(0, _pos);
                    }
                }
                if (_list != null)
                    m_LogObligation.DocLibUrl = ConstructListUrl(m_LogObligation.Web, _list);
            }

            if (m_LogObligation.DocLibUrl == null)
                m_LogObligation.DocLibUrl = srcName.Substring(0, srcName.LastIndexOf("/"));
            m_LogObligation.UserName = user.userName;
            m_LogObligation.FileName = srcName.Substring(srcName.LastIndexOf("/")+1);

            if (context.Web != null)
            {
                SPWebApplication spWebApp = context.Web.Site.WebApplication;
                CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
                if (spWebApp != null && spAUC != null)
                {
                    srcName = spAUC.UrlUpdate(spWebApp, context.SrcName);
                    targetName = spAUC.UrlUpdate(spWebApp, context.TargetName);
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            if (!String.IsNullOrEmpty(targetName))
                targetName = Globals.UrlToResSig(targetName).ToLower();

            // if specify the noise level as CE_NOISE_LEVEL_SYSTEM, no obligation returned and cannot get match policy info in agent log
            /*
            if (!context.ActionStr.Equals("UPLOAD"))
            {
                if (context.UserName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                    || context.UserName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    noiseLevel = (NoiseLevel)CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
                }
            }
            */

            ConvertAttributeListToArray(context.SrcAttributes, ref srcAttributes);
            ConvertAttributeListToArray(context.TargetAttributes, ref targetAttributes);
            context.RemoteAddress = Globals.GetXHeaderIp(context.UserName, context.Web.Url, context.RemoteAddress);
            uint ipNumber = Utilities.IPAddressToIPNumber(context.RemoteAddress);
            //add additional IPAddress attributes and XHeader for src attrs.
            Globals.AddXHeaderAndIpAttribute(ref srcAttributes, context.UserName, context.Web.Url, context.RemoteAddress);

            string[] userAttr = Globals.SPECommon_GetUserAttr(context.PrincipalUser,context.Web);
            srcAttributes = Globals.SPECommon_GetPropBag(context.Web, srcAttributes);
            srcAttributes = Globals.SetAttrsResourceSignature(srcAttributes, srcName); // Set "Resource Signature" to resource attributes.
            userAttr = Globals.SPECommon_GetUserProfile(context.Web, strLoginNameBeforeConverted, userAttr);

            //check if profile sid exists, if yes, remove
            string profileSid = Global_Utils.GetAndRemoveIdAttribute(ref userAttr);
            if (!string.IsNullOrEmpty(profileSid)
                && UserSid.IsValidSid(user.userID, user.userName) != true
                && UserSid.IsValidSid(profileSid, user.userName) != false)
            {
                //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                //update user sid with profile sid
                user.userID = profileSid;
            }
            int EvalTimeoutMs = Globals.GetPolicyDefaultTimeout();

            // Do PreAuthorization beofre Evaluation
            Globals.DoPreAuthorization(context.Web, context.SrcName, srcName, context.ActionStr, ref userAttr, ref srcAttributes, ref targetAttributes);
            // Check all attribute before Evaluation
            userAttr = Globals.CheckEvalAttributs(userAttr);
            srcAttributes = Globals.CheckEvalAttributs(srcAttributes);
            targetAttributes = Globals.CheckEvalAttributs(targetAttributes);
            EvaluatorApplication app = EvaluatorApplication.CreateInstance();

            // Log information for enforcement.
            Global_Utils.SPELogEnforcementInfo("CheckPortalResource", context.UserName, context.RemoteAddress, srcName, context.ActionStr, srcAttributes, userAttr);

            callResult = CESDKAPI.CEEVALUATE_CheckResources(m_ConnectHandle, context.ActionStr,
                new CETYPE.CEResource(srcName, "spe"), ref srcAttributes,
                new CETYPE.CEResource(targetName, "spe"), ref targetAttributes, user, ref userAttr,
                app.Application, ref emptyAttributes, ref emptyAttributes, ipNumber,
                true, (CETYPE.CENoiseLevel_t)noiseLevel,
                out enforcementObligation, out enforcementResult, EvalTimeoutMs);

            // Log enforcement result.
            Global_Utils.SPELogEnforcementResult(srcName, context.ActionStr, callResult, enforcementResult);

            if (callResult == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                context.Allow = (enforcementResult == CETYPE.CEResponse_t.CEAllow) ? true : false;
                if (context.Allow)
                {
                    ParseObligations(ref context, ref enforcementObligation);

                    // Run all registered IObligation
                    foreach (IObligation iOb in m_IObligationList)
                    {
                        iOb.Process(m_Obligations, m_ConnectHandle);
                    }
                }
            }
            else
            {
                // TODO add fail log
                context.FailedReason = "Warning - Failed to evaluate to Policy Controller.(" + callResult.ToString() + ")";

                // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                context.Allow = Globals.GetPolicyDefaultBehavior();
            }

            Release();

            return context.Allow;
        }


        public bool CheckPortalResource_CloudAZ(ref EvaluatorContext context)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Evaluator CheckPortalResource_CloudAZ Enter.");
            if (String.IsNullOrEmpty(context.ActionStr))
            {
                if (Globals.g_JPCParams.bUseJavaPC)
                    return CheckPortal_CloudAZ(ref context);
                else
                    return CheckPortal(ref context);
            }

            NoiseLevel noiseLevel = context.NoiseLevel;
            CETYPE.CEUser user;
            string[] enforcementObligation;

            string srcName = context.SrcName;
            string targetName = context.TargetName;
            string[] srcAttributes = null;
            string[] targetAttributes = null;
            string[] emptyAttributes = new string[0];
            if (String.IsNullOrEmpty(context.UserSid))
            {
                context.UserSid = UserSid.GetUserSid(context.Web, context.UserName);
                if (String.IsNullOrEmpty(context.UserSid))
                    context.UserSid = context.UserName;
            }
            Initialise();
            if (m_ConnectHandle == IntPtr.Zero)
            {
                context.FailedReason = "Warning - Failed to connect to Policy Controller.";
                return true;
            }

            m_LogObligation.Web = context.Web;
            string strLoginNameBeforeConverted = context.UserName;
            if (context.UserSid != null && context.UserSid.Equals(context.UserName))
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
                context.UserSid = context.UserName;
            }
            else
            {
                context.UserName = NextLabs.Common.Utilities.ClaimUserConvertion(context.UserName);
            }
            user = new CETYPE.CEUser(context.UserName, context.UserSid);
            {
                Object _obj = null;
                SPListItem _listitem = null;
                SPFile _file = null;
                SPList _list = null;
                SPFolder _folder = null;
                try
                {
                    _obj = m_LogObligation.Web.GetObject(srcName);
                }
                catch
                {
                }
                if (_obj != null)
                {
                    if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFile)))
                    {
                        _file = (SPFile)_obj;
                        if (_file != null)
                            _folder = _file.ParentFolder;
                        if (_folder != null)
                        {
                            Guid _guid = _folder.ParentListId;
                            _list = m_LogObligation.Web.Lists.GetList(_guid, true);
                        }
                    }
                    else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPListItem)))
                    {
                        _listitem = (SPListItem)_obj;
                    }
                }
                if (_listitem != null)
                {
                    _list = _listitem.ParentList;
                    m_LogObligation.Url = m_LogObligation.Web.Url + "/" + _listitem.Url;
                    int _pos = m_LogObligation.Url.LastIndexOf("/");
                    if (_pos != -1)
                    {
                        m_LogObligation.Location = m_LogObligation.Url.Substring(0, _pos);
                    }
                }
                if (_list != null)
                    m_LogObligation.DocLibUrl = ConstructListUrl(m_LogObligation.Web, _list);
            }

            if (m_LogObligation.DocLibUrl == null)
                m_LogObligation.DocLibUrl = srcName.Substring(0, srcName.LastIndexOf("/"));
            m_LogObligation.UserName = user.userName;
            m_LogObligation.FileName = srcName.Substring(srcName.LastIndexOf("/")+1);

            if (context.Web != null)
            {
                SPWebApplication spWebApp = context.Web.Site.WebApplication;
                CAlternateUrlCheck spAUC = new CAlternateUrlCheck();
                if (spWebApp != null && spAUC != null)
                {
                    srcName = spAUC.UrlUpdate(spWebApp, context.SrcName);
                    targetName = spAUC.UrlUpdate(spWebApp, context.TargetName);
                }
            }

            srcName = Globals.UrlToResSig(srcName).ToLower();
            if (!String.IsNullOrEmpty(targetName))
                targetName = Globals.UrlToResSig(targetName).ToLower();

            if (!context.ActionStr.Equals("UPLOAD"))
            {
                if (context.UserName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                    || context.UserName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    noiseLevel = (NoiseLevel)CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
                }
            }

            ConvertAttributeListToArray(context.SrcAttributes, ref srcAttributes);
            ConvertAttributeListToArray(context.TargetAttributes, ref targetAttributes);
            context.RemoteAddress = Globals.GetXHeaderIp(context.UserName, context.Web.Url, context.RemoteAddress);
            uint ipNumber = Utilities.IPAddressToIPNumber(context.RemoteAddress);
            //add additional IPAddress attributes and XHeader for src attrs.
            Globals.AddXHeaderAndIpAttribute(ref srcAttributes, context.UserName, context.Web.Url, context.RemoteAddress);

            string[] userAttr = Globals.SPECommon_GetUserAttr(context.PrincipalUser,context.Web);
            srcAttributes = Globals.SPECommon_GetPropBag(context.Web, srcAttributes);
            srcAttributes = Globals.SetAttrsResourceSignature(srcAttributes, srcName); // Set "Resource Signature" to resource attributes.
            userAttr = Globals.SPECommon_GetUserProfile(context.Web, strLoginNameBeforeConverted, userAttr);

            //check if profile sid exists, if yes, remove
            string profileSid = Global_Utils.GetAndRemoveIdAttribute(ref userAttr);
            if (!string.IsNullOrEmpty(profileSid)
                && UserSid.IsValidSid(user.userID, user.userName) != true
                && UserSid.IsValidSid(profileSid, user.userName) != false)
            {
                //if the current sid IS NOT BEST(true) and the profile sid IS NOT INVALID(false)
                //update user sid with profile sid
                user.userID = profileSid;
            }
            int EvalTimeoutMs = Globals.GetPolicyDefaultTimeout();

            // Do PreAuthorization beofre Evaluation
            Globals.DoPreAuthorization(context.Web, context.SrcName, srcName, context.ActionStr, ref userAttr, ref srcAttributes, ref targetAttributes);
            // Check all attribute before Evaluation
            userAttr = Globals.CheckEvalAttributs(userAttr);
            srcAttributes = Globals.CheckEvalAttributs(srcAttributes);
            targetAttributes = Globals.CheckEvalAttributs(targetAttributes);

            EvaluatorApplication app = EvaluatorApplication.CreateInstance();

            List<CEObligation> lstObligation = new List<CEObligation>();
            PolicyResult result = PolicyResult.DontCare;

            // Log information for enforcement.
            Global_Utils.SPELogEnforcementInfo("CheckPortalResource_CloudAZ", context.UserName, context.RemoteAddress, srcName, context.ActionStr, srcAttributes, userAttr);

            QueryCloudAZSDK.CEModel.CERequest ceRequest = CloudAZQuery.CreateQueryReq(context.ActionStr, context.RemoteAddress, srcName, srcAttributes, context.UserSid, context.UserName, userAttr);
            QueryStatus qs = CloudAZQuery.Instance.QueryColuAZPC(ceRequest, ref lstObligation, ref result);
            NLLogger.OutputLog(LogLevel.Debug, "CheckPortalResource_CloudAZ: enforcement_result=" + result);

            if (qs == QueryStatus.S_OK)
            {
                enforcementObligation = Globals.ConvertObligationListtoArray(lstObligation);
                ParseObligations(ref context, ref enforcementObligation);

                // Run all registered IObligation
                foreach (IObligation iOb in m_IObligationList)
                {
                    iOb.Process(m_Obligations, m_ConnectHandle);
                }
                context.Allow = (result == PolicyResult.Deny) ? false : true;
            }
            else
            {

                context.FailedReason = "Warning - Failed to evaluate to Policy Controller.(" + qs.ToString() + ")";

                // Allow or Deny depend on the registry key "PolicyDefaultBahavior", modify by George.
                context.Allow = Globals.GetPolicyDefaultBehavior();
            }


       //     Release();

            return context.Allow;
        }

        private void ParseObligations(ref EvaluatorContext context, ref string[] enforcerObligations)
        {
            // Parse enforcerObligations to m_Obligations
            if (enforcerObligations.Length > 0)
            {
                Dictionary<string, string> obligations = new Dictionary<string, string>();
                for (int i = 0; i < enforcerObligations.Length; i += 2)
                {
                    obligations.Add(enforcerObligations[i], enforcerObligations[i + 1]);
                }

                try
                {
                    string hintValue = obligations.ContainsKey(CacheHintKey) ? obligations[CacheHintKey] : null;
                    if (hintValue != null)
                    {
                        context.CacheHint = Int32.Parse(hintValue);
                    }
                }
                catch
                {
                }

                int obligation_count = 0;
                try
                {
                    string count = obligations.ContainsKey(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT) ? obligations[CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT] : null;
                    if (!string.IsNullOrEmpty(count))
                    {
                        obligation_count = int.Parse(count);
                    }
                }
                catch
                {
                }

                for (int i = 0; i < obligation_count; i++)
                {
                    Obligation ob = new Obligation();
                    string nameKey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_NAME + ":" + (i + 1);
                   string policyKey = string.Empty;
                    if (!Globals.g_JPCParams.bUseJavaPC)
                        policyKey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_POLICY + ":" + (i + 1);
                     string attrNumKey = CEATTR_NUMVALUES_KEY + ":" + (i + 1);

                    ob.Name = obligations[nameKey];

                    if (!Globals.g_JPCParams.bUseJavaPC)
                        ob.Policy = obligations[policyKey];

                    if (ob.Name != CETYPE.CEAttrVal.CE_OBLIGATION_NOTIFY)
                    {
                        int attrNum = Int32.Parse(obligations[attrNumKey]);
                        for (int j = 0; j < attrNum; j += 2)
                        {
                            string attrValueName = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1) + ":" + (j + 1);
                            string attrKey = obligations[attrValueName];
                            attrValueName = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1) + ":" + (j + 2);
                            string attrValue = obligations[attrValueName];

                            if (ob.Name.Equals("SP_Security_Filter_Criteria") && attrValue == null)
                            {
                                attrValue = ""; // cover null value for obligation "SP_Security_Filter_Criteria".
                            }
                            if (!String.IsNullOrEmpty(attrKey) && attrValue != null)
                            {
                                ob.AddAttribute(attrKey, attrValue);
                            }
                        }
                        m_Obligations.Add(ob);
                    }
                    else
                    {
                        string attrValueName = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1);
                        context.PolicyName = ob.Policy;
                        context.PolicyMessage = obligations[attrValueName];
                    }
                }
                if (string.IsNullOrEmpty(context.PolicyMessage))
                {
                    string policyName = null;
                    string policyMsg = null;
                    Globals.GetPolicyNameMessageByObligation(enforcerObligations, ref policyName, ref policyMsg);
                    context.PolicyName = policyName;
                    context.PolicyMessage = policyMsg;
                }
            }
        }

        private void Initialise()
        {
            EvaluatorApplication app = EvaluatorApplication.CreateInstance();
            CETYPE.CEResult_t result;
            CETYPE.CEUser user = new CETYPE.CEUser("dummyName", "dummyId");

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

            result = CESDKAPI.CECONN_Initialize(app.Application, user, null,
                                                out m_ConnectHandle, ConnectTimeoutMs);
            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                // log failed message
                m_ConnectHandle = IntPtr.Zero;
            }
        }

        private void Release()
        {
            if (m_ConnectHandle != IntPtr.Zero)
            {
                CESDKAPI.CECONN_Close(m_ConnectHandle, ConnectTimeoutMs);
                m_ConnectHandle = IntPtr.Zero;
            }
        }

        public void ConvertPropertyListToArray(ref List<KeyValuePair<string, string>> attrlist, ref string[] attrs)
        {
            ConvertAttributeListToArray(attrlist, ref attrs);
        }

        private void ConvertAttributeListToArray(List<KeyValuePair<string, string>> attrlist, ref string[] attrs)
        {
            List<string> tmpList = new List<string>();

            foreach (KeyValuePair<string, string> property in attrlist)
            {
                if (!String.IsNullOrEmpty(property.Key) && !String.IsNullOrEmpty(property.Value))
                {
                    // Remove replicated keys
                    if ((property.Key == CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED ||
                        property.Key == CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED) &&
                        tmpList.Contains(property.Key))
                        continue;

                    int value = 0;
                    // Remove replicated integer keys
                    if (Int32.TryParse(property.Value, out value) && tmpList.Contains(property.Key))
                        continue;

                    tmpList.Add(property.Key);
                    if (property.Key == CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY ||
                        property.Key == CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY)
                    {
                        tmpList.Add(property.Value);
                    }
                    else
                    {
                        tmpList.Add(property.Value.ToLower());
                    }
                }
            }

            attrs = tmpList.ToArray();
        }
    }
}
