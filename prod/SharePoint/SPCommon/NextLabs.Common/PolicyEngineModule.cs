using Microsoft.IdentityModel.Claims;
using Microsoft.SharePoint;
using NextLabs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Timers;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
   public class PolicyEngineModule
    {
        private enum PolicyEngineHandleType
        {
            PE_STRING_LIST,
            PE_SUBJECT,
            PE_HOST,
            PE_APPLICATION,
            PE_RESOURCE
        }
        public class PrefilterMatchResult
        {
            public string retKey;
            public string retValue;
            public PrefilterMatchResult(string key, string value)
            {
                retKey = key;
                retValue = value;
            }
        }
        /// <summary>
        /// result of load dll,if true do prefilter init,otherwise not
        /// </summary>
        public static bool bDllLoaded = false;

        private static List<string> skippedUser = new List<string>();

        private static object userClaimCacheLock = new Object();

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_module_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern int policyEngineModuleInit(string cchost, string ccport, string ccuser, string ccpwd, string tag, int sync_interval_seconds);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_module_exit", CallingConvention = CallingConvention.Cdecl)]
        public static extern int policyEngineModuleExit();

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_analyze", CallingConvention = CallingConvention.Cdecl)]
        private static extern int policyEngineAnalyze(ref IntPtr sublist, ref IntPtr actlist, ref IntPtr resourcelist, ref IntPtr host, ref IntPtr app);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_string_list_next", CallingConvention = CallingConvention.Cdecl)]
        private static extern int policyEngineStringListNext(IntPtr pstring_list, ref IntPtr next);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_string_list_current", CallingConvention = CallingConvention.Cdecl)]
        private  static extern int policyEngineStringListCurrent(IntPtr pstring_list, ref IntPtr str);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_destroy_string_list", CallingConvention = CallingConvention.Cdecl)]
        private static extern int policyEngineDestroyStringList(IntPtr pstring_list);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_create_dictionary_handle", CallingConvention = CallingConvention.Cdecl)]
        private  static extern int policyEngineCreateDictionaryHandle(PolicyEngineHandleType dictionary_type, ref IntPtr pdictionary);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_destroy_dictionary", CallingConvention = CallingConvention.Cdecl)]
        private  static extern int policyEngineDestroyDictionary(IntPtr dictionary);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_insert_into_dictionary", CallingConvention = CallingConvention.Cdecl)]
        private  static extern int policyEngineInsertIntoDictionary(IntPtr dictionary, string attribute_name, string attribute_value);

        [DllImport("policy_engine.dll", EntryPoint = "policy_engine_match", CallingConvention = CallingConvention.Cdecl)]
        private  static extern int policyEngineMatch(IntPtr subject, string action, IntPtr resource, IntPtr host, IntPtr app, ref int result);

        static PolicyEngineModule()
        {
            var installDir = Globals.GetSPEPath();
            if (installDir != null)
            {
                var ret = LoadDll(installDir + "bin\\boost_date_time-vc140-mt-x64-1_67.dll");
                if (ret == -1)
                    return;
                ret = LoadDll(installDir + "bin\\jsoncpp.dll");
                if (ret == -1)
                    return;
                ret = LoadDll(installDir + "bin\\LIBEAY32.dll");
                if (ret == -1)
                    return;
                ret = LoadDll(installDir + "bin\\SSLEAY32.dll");
                if (ret == -1)
                    return;
                ret = LoadDll(installDir + "bin\\policy_engine.dll");
                if (ret == -1)
                    return;
                bDllLoaded = true;
            }
            //add for user cache before prefilter
            Timer timer = new Timer();
            timer.Enabled = true;
            timer.Interval = Globals.strGlobalPolicyEngineIntervalSec * 1000;//in milliseconds
            timer.Start();
            timer.Elapsed += new ElapsedEventHandler(ClearUSerCache);
        }

        private static void ClearUSerCache(object sender, ElapsedEventArgs e)
        {
            lock (userClaimCacheLock)
            {
                skippedUser.Clear();
            }
        }
        private static void UpdateUserCache(string loginName)
        {
            lock (userClaimCacheLock)
            {
                if (!skippedUser.Contains(loginName))
                {
                    skippedUser.Add(loginName);
                }
            }
        }
        private static bool CheckSkippedUser(string loginName)
        {
            lock (userClaimCacheLock)
            {
                if (skippedUser.Contains(loginName))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool PrefilterEngineMatch(SPWeb web, IPrincipal PrincipalUser, string loginName, string action)
        {
            if (loginName.Equals(@"SHAREPOINT\system", StringComparison.OrdinalIgnoreCase))
            {
                NLLogger.OutputLog(LogLevel.Debug, @"prefilter don't support user(SHAREPOINT\system)");
                return false;
            }
            if (CheckSkippedUser(loginName))
            {
                NLLogger.OutputLog(LogLevel.Debug, loginName + "is in cache,we skip this");
                return true;
            }
            bool noMatch = false;
            List<string> subList = new List<string>();
            List<string> actList = new List<string>();
            List<string> resList = new List<string>();
            List<string> hostList = new List<string>();
            List<string> appList = new List<string>();

            var ret = PolicyEngineAnalyze(ref subList, ref actList, ref resList, ref hostList, ref appList);
            if (ret == 0)
            {
                SPEEvalAttrs.prefilterAnalyze(subList, resList);
                if (actList.Contains(action) || (string.IsNullOrEmpty(action) && actList.Count > 0))
                {
                    var userAttrs = new List<PrefilterMatchResult>();
                    var hostAttrs = new List<PrefilterMatchResult>();
                    var appAttrs = new List<PrefilterMatchResult>();
                    // gather user info
                    if (subList.Count != 0)
                    {
                        Globals.GetUserClaim(PrincipalUser, loginName, userAttrs, subList);
                        Globals.GetUserProfile(web, loginName, userAttrs, subList);
                    }
                    //gather host info
                    if (hostList.Count != 0)
                    {
                    }
                    //gather app info
                    if (appList.Count != 0)
                    {
                    }
                    IntPtr subHandler = InsertIntoDicHandler(PolicyEngineHandleType.PE_SUBJECT, userAttrs);
                    IntPtr hostHandler = InsertIntoDicHandler(PolicyEngineHandleType.PE_HOST, hostAttrs);
                    IntPtr appHandler = InsertIntoDicHandler(PolicyEngineHandleType.PE_APPLICATION, appAttrs);
                    var result = -1;
                    ret = PolicyEngineMatch(subHandler, action, IntPtr.Zero, hostHandler, appHandler, ref result);
                    NLLogger.OutputLog(LogLevel.Debug, "prefilter result:" + result + ",ret:" + ret);
                    if (ret == 0 && result == 0)
                    {
                        noMatch = true;
                        UpdateUserCache(loginName);
                    }
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Debug, "this action is not included");
                    noMatch = true;
                }
            }
            else
            {
                NLLogger.OutputLog(LogLevel.Debug, "PolicyEngineAnalyze failed");
            }
            return noMatch;
        }

        private static int LoadDll(string dllPath)
        {
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
            {
                NLLogger.OutputLog(LogLevel.Debug, "has't find dll" + dllPath);
                return -1;
            }
            return 0;
        }

        public static int PolicyEngineAnalyze(ref List<string> subList, ref List<string> actList, ref List<string> resList, ref List<string> hostList, ref List<string> appList)
        {
            IntPtr sublistHandler = IntPtr.Zero;
            IntPtr actlistHandler = IntPtr.Zero;
            IntPtr resourcelistHandler = IntPtr.Zero;
            IntPtr hostHandler = IntPtr.Zero;
            IntPtr appHandler = IntPtr.Zero;
            var ret = PolicyEngineModule.policyEngineAnalyze(ref sublistHandler, ref actlistHandler, ref resourcelistHandler, ref hostHandler, ref appHandler);
            if (ret != 0)
            {
                return ret;
            }
            //convert list
            GetEnumatorFromList(subList, sublistHandler);
            GetEnumatorFromList(actList, actlistHandler);
            GetEnumatorFromList(resList, resourcelistHandler);
            GetEnumatorFromList(hostList, hostHandler);
            GetEnumatorFromList(appList, appHandler);

            // destroy handler
            policyEngineDestroyStringList(sublistHandler);
            policyEngineDestroyStringList(actlistHandler);
            policyEngineDestroyStringList(resourcelistHandler);
            policyEngineDestroyStringList(hostHandler);
            policyEngineDestroyStringList(appHandler);

            return ret;
        }
        private static IntPtr InsertIntoDicHandler(PolicyEngineHandleType policyEngineHandleType, List<PrefilterMatchResult> attrs)
        {
            IntPtr dicHandler = IntPtr.Zero;
            var ret = policyEngineCreateDictionaryHandle(policyEngineHandleType, ref dicHandler);
            if (ret == 0)
            {
                foreach (var matchResult in attrs)
                {
                    ret = policyEngineInsertIntoDictionary(dicHandler, matchResult.retKey, matchResult.retValue);
                    if (ret != 0)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "InsertIntoDicHandler insert dictionary failed");
                    }
                }
            }
            else
            {
                dicHandler = IntPtr.Zero;
                NLLogger.OutputLog(LogLevel.Debug, "InsertIntoDicHandler create dictionary failed:" + policyEngineHandleType.ToString());
            }
            return dicHandler;
        }
        private static void GetEnumatorFromList(List<string> list, IntPtr handler)
        {
            for (IntPtr it = handler; it != IntPtr.Zero; policyEngineStringListNext(it, ref it))
            {
                IntPtr sub = IntPtr.Zero;
                var ret = policyEngineStringListCurrent(it, ref sub);
                if (ret == 0)
                {
                    list.Add(Marshal.PtrToStringAnsi(sub));
                }
            }
        }
        private static int PolicyEngineMatch(IntPtr subject, string action, IntPtr resource, IntPtr host, IntPtr app, ref int result)
        {
            var ret = policyEngineMatch(subject, action, resource, host, app, ref result);
            policyEngineDestroyDictionary(subject);
            policyEngineDestroyDictionary(host);
            policyEngineDestroyDictionary(app);
            return ret;
        }
    }
}
