using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NextLabs.Common;
using Microsoft.SharePoint;
using System.Web;
using NextLabs.CSCInvoke;
using Microsoft.Win32;
using NextLabs.Diagnostic;
using System.Threading;

namespace NextLabs.SPEnforcer
{
    public struct PreFilterSource
    {
        public string userName;
        public string userSid;
        public string clientAddress;
        public string requestUrl;
    }

    class PreFilterModule
    {
        private const string PREFILTER_ENABLED_REGISTRY_KEY = "PreFilterEnabled";
        private const int PREFILTER_ENABLED_RESET_MINUTES = 10;

        public static PreFilterSource prefilterSource = new PreFilterSource();
        private static int _initializing = 0;//0=false;1=true
        public static void Initialize()
        {
            //use Interlocked instead of lock() to reduce performance overhead
            if (Interlocked.CompareExchange(ref _initializing, 1, 0) == 0)
            {
                if (IsEnabled())
                {
                    EventHelper.Instance.BeforeEventExecuting += PreFilterModule.BeforeEventExecutingHandler;
                }
                else
                {
                    EventHelper.Instance.BeforeEventExecuting -= PreFilterModule.BeforeEventExecutingHandler;
                }
                _initializing = 0;
            }
        }

        public static void setSource(string userName, string userSid, string clientAddress, string requestUrl)
        {
            prefilterSource.userName = userName;
            prefilterSource.userSid = userSid;
            prefilterSource.clientAddress = clientAddress;
            prefilterSource.requestUrl = requestUrl;
        }

        public static void releaseSource()
        {
            prefilterSource.userName = String.Empty;
            prefilterSource.userSid = String.Empty;
            prefilterSource.clientAddress = String.Empty;
            prefilterSource.requestUrl = string.Empty;
        }

        public static void BeforeEventExecutingHandler(object sender, CancelEventArgs args)
        {
            try
            {
                HttpContext httpContext = null;
                string userName = string.Empty;
                string userSid = string.Empty;
                string clientAddress = string.Empty;
                string requestUrl = string.Empty;
                if (args is EventHandlerEventArgs)
                {
                    var eventArgs = (EventHandlerEventArgs)args;
                    if (eventArgs != null)
                    {
                        httpContext = eventArgs.Context;
                        var properties = eventArgs.EventProperties;
                        //create new SPWeb to fix the dispose issue
                        using (SPSite site = new SPSite(properties.SiteId))
                        {
                            SPWeb web = null;
                            try
                            {
                                if (properties is SPItemEventProperties)
                                {
                                    web = site.OpenWeb(((SPItemEventProperties)properties).RelativeWebUrl);
                                }
                                else if (properties is SPListEventProperties)
                                {
                                    web = site.OpenWeb(((SPListEventProperties)properties).WebId);
                                }
                                else if (properties is SPWebEventProperties)
                                {
                                    web = site.OpenWeb(((SPWebEventProperties)properties).WebId);
                                }
                                if (web != null)
                                {
                                    userName = web.CurrentUser.LoginName;
                                    userSid = UserSid.GetUserSid(web);
                                }
                            }
                            finally
                            {
                                if (web != null)
                                {
                                    web.Dispose();
                                }
                            }
                        }
                    }
                }
                else if (args is HttpModuleEventArgs)
                {
                    //this event is from a http module
                    var httpModuleEventArgs = args as HttpModuleEventArgs;

                    switch (httpModuleEventArgs.EventType)
                    {
                        case HttpModuleEvents.BeginRequest:
                        case HttpModuleEvents.EndRequest:
                            break;
                        default:
                            {
                                httpContext = httpModuleEventArgs.Application.Context;
                                SPEEvalAttr attrs = SPEEvalAttrs.Current();
                                if (attrs != null)
                                {
                                    userName = attrs.LogonUser;
                                    requestUrl = attrs.RequestURL;
                                    clientAddress = attrs.RemoteAddr;
                                }
                            }
                            break;
                    }
                }
                else if (args is ControlEventArgs)
                {
                    var controlEventArgs = args as ControlEventArgs;
                    httpContext = controlEventArgs.Context;
                }

                if (httpContext == null)
                {
                    clientAddress = prefilterSource.clientAddress;
                    if (!string.IsNullOrEmpty(prefilterSource.userName)
                        && !string.IsNullOrEmpty(prefilterSource.userSid)
                        && !string.IsNullOrEmpty(prefilterSource.requestUrl))
                    {
                        userName = prefilterSource.userName;
                        userSid = prefilterSource.userSid;
                        requestUrl = prefilterSource.requestUrl;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(userName))
                    {
                        userName = httpContext.User.Identity.Name;
                    }
                    if (string.IsNullOrEmpty(userSid))
                    {
                        userSid = UserSid.GetUserSid(httpContext);
                    }
                    if (string.IsNullOrEmpty(requestUrl))
                    {
                        requestUrl = httpContext.Request.Url.AbsoluteUri;
                    }
                    if (string.IsNullOrEmpty(clientAddress))
                    {
                        clientAddress = httpContext.Request.UserHostAddress;
                    }
                }

                if (userSid != null && userSid.Equals(userName))
                {
                    userName = NextLabs.Common.Utilities.ClaimUserConvertion(userName);
                    userSid = userName;
                }
                else
                {
                    userName = NextLabs.Common.Utilities.ClaimUserConvertion(userName);
                }

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userSid) || string.IsNullOrEmpty(requestUrl))
                {
                    // Don't do evaluation when exist empty parameter.
                    return;
                }

                //do evaluation
                CETYPE.CEResponse_t resp = CETYPE.CEResponse_t.CEAllow;
                if (Globals.g_JPCParams.bUseJavaPC)
                    resp = Global_Utils.EvaluateForEnforceStatus_CloudAZ(userName, userSid, clientAddress, requestUrl);
                else
                    resp = EvaluateForEnforceStatus(userName, userSid, clientAddress, requestUrl);


                if (resp == CETYPE.CEResponse_t.CEAllow)
                {
                    //Deny=Enforce it
                    //Allow=Skip it
                    //if evaluation result is allowed, cancel this event executing
                    args.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during BeforeEventExecutingHandler:", null, ex);
            }
        }

        private static CETYPE.CEResponse_t EvaluateForEnforceStatus(string userName, string sid, string clientAddress, string requestUrl)
        {
            //check if PC is up
            if (!Utilities.SPECommon_Isup())
            {
                //PC is not UP, get default behavior
                return Globals.GetPolicyDefaultBehavior()
                    ? CETYPE.CEResponse_t.CEAllow
                    : CETYPE.CEResponse_t.CEDeny;
            }
            //
            uint ipNumber = Utilities.IPAddressToIPNumber(clientAddress);
            CETYPE.CENoiseLevel_t noiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
            //enforcement result
            CETYPE.CEResponse_t enforcementResult;
            //default attributes
            string[] emptyAttributes = new string[0];
            //attributes include "url"
            string[] urlAttrs = new string[2];
            urlAttrs[0] = "url";
            urlAttrs[1] = requestUrl;

            //construct user
            CETYPE.CEUser user = new CETYPE.CEUser(userName, sid);
            CETYPE.CEApplication ceApp = new CETYPE.CEApplication("SharePoint", null, null);
            CETYPE.CEResponse_t evalDefault = Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
            int connectionFailed = 0;
            while (true)
            {
                IntPtr connectionHandle;
                try
                {
                    if (!TryGetGlobalConnection(ceApp, user, out connectionHandle))
                    {
                        // Always ALLOW when error occurrs.
                        return CETYPE.CEResponse_t.CEAllow;
                    }
                    //Policy Evaluation
                    int evalTimeoutMs = Globals.GetPolicyDefaultTimeout();

                    NLLogger.OutputLog(LogLevel.Debug, "EvaluateForEnforceStatus: srcName = " + requestUrl + ", userName = " + userName);
                    System.Diagnostics.Trace.WriteLine("EvaluateForEnforceStatus: srcName = " + requestUrl + ", userName = " + userName);
                    var callResult = CESDKAPI.CEEVALUATE_CheckResources(connectionHandle,
                        Global_Utils.PREFILTER_ACTION_KEY,
                        new CETYPE.CEResource(requestUrl, "spe"),
                        ref urlAttrs,
                        new CETYPE.CEResource(string.Empty, "spe"),
                        ref emptyAttributes,
                        user,
                        ref emptyAttributes,
                        ceApp,
                        ref emptyAttributes,
                        ref emptyAttributes,
                        (uint)ipNumber,
                        true,
                        (CETYPE.CENoiseLevel_t)noiseLevel,
                        out emptyAttributes,
                        out enforcementResult,
                        evalTimeoutMs);

                    // Log enforcement result.
                    Global_Utils.SPELogEnforcementResult(requestUrl, Global_Utils.PREFILTER_ACTION_KEY, callResult, enforcementResult);
                    if (callResult != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                    {
                        if (callResult == CETYPE.CEResult_t.CE_RESULT_CONN_FAILED)
                        {
                            connectionFailed++;
                            //close current handler
                            CloseGlobalConnection(connectionHandle);
                            //Retry one time
                            if (connectionFailed <= 1)
                            {
                                continue;
                            }
                        }
                        // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                        return evalDefault;
                    }
                    return enforcementResult;
                }
                catch (Exception ex)
                {
                    //make sure not fall into dead loop
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during PreFilterModule EvaluateForEnforceStatus:", null, ex);

                    // Allow or Deny depend on the registry key "PolicyDefaultBehavior", modify by George.
                    return evalDefault;
                }
                finally
                {
                    connectionHandle = IntPtr.Zero;
                }
            }//while(true)
        }

        #region connection handler utilities
        private static void CloseGlobalConnection(IntPtr connectionHandler)
        {
            lock (typeof(Globals))
            {
                if (connectionHandler == Globals.connectHandle)
                {
                    CESDKAPI.CECONN_Close(Globals.connectHandle,
                                          Globals.connectTimeoutMs);
                    Globals.connectHandle = IntPtr.Zero;
                }
            }
        }
        private static bool TryGetGlobalConnection(CETYPE.CEApplication ceApp, CETYPE.CEUser user, out IntPtr connectionHandler)
        {
            if (Globals.connectHandle == IntPtr.Zero)
            {
                lock (typeof(Globals))
                {
                    //initial connection handle if it doesn't exist
                    if (Globals.connectHandle == IntPtr.Zero)
                    {
                        var result = CESDKAPI.CECONN_Initialize(ceApp, user, null,
                                                                        out Globals.connectHandle,
                                                                        Globals.connectTimeoutMs);
                        if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                        {
                            Globals.connectHandle = IntPtr.Zero;
                        }
                    }
                }
            }
            connectionHandler = Globals.connectHandle;
            return connectionHandler != IntPtr.Zero;
        }
        #endregion

        private static bool? _enabled;
        private static DateTime _resetTime;
        public static bool IsEnabled()
        {
            if (!_enabled.HasValue||DateTime.Now >= _resetTime)
            {
                try
                {
                    using (var ceKey = Registry.LocalMachine.OpenSubKey(@"Software\NextLabs\Compliant Enterprise\Sharepoint Enforcer\", false))
                    {
                        if (ceKey != null)
                        {
                            object regValue = ceKey.GetValue(PREFILTER_ENABLED_REGISTRY_KEY, string.Empty);
                            bool enabled = false;
                            if (regValue != null && bool.TryParse(regValue.ToString(), out enabled))
                            {
                                _enabled = enabled;
                            }
                        }
                    }
                    _resetTime = DateTime.Now.AddMinutes(PREFILTER_ENABLED_RESET_MINUTES);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during PreFilterModule IsEnabled when reading registry key:", null, ex);
                }
            }
            return _enabled.HasValue ? _enabled.Value : false;
        }
    }
}
