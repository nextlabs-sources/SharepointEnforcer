using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Timers;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Reflection;
using NextLabs.PluginInterface;
using System.Threading;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    struct RequestPluginStruct
    {
        public bool Continue;
        public int Sequence;
        public Assembly asm;
        public NextLabs.PluginInterface.IProcessHttpRequest InterfaceObj;
    }

    struct ObligationPluginStruct
    {
        public string ObligationName;
        public bool Continue;
        public int Sequence;
        public Assembly asm;
        public NextLabs.PluginInterface.IExecuteObligation InterfaceObj;
    }
    struct PluginFrameGlobalParam
    {
        public long ExecuteTimeOut;
        public string Pluginfolder;
    }

    enum PluginType
    {
        RequestPlugin,
        ObligationPlugin
    }
    struct PluginParam
    {
        public string PluginName;
        public PluginType Type;
        public int Sequece;
        public long PluginTimeOut;
        public bool NeedContinue;
        public bool Pre_Execution;
        public string ObligationName;
    }

    public sealed class PluginFrame
    {
        private static volatile PluginFrame instance;
#region const
        private const string PLUGIN_ENABLED_REGISTRY_KEY = "PluginEnabled";
        //interfaces
        private const string INTERFACE_REQUEST = "IProcessHttpRequest";
        private const string INTERFACE_OBLIGATION = "IExecuteObligation";

        //config const pararmeters
        private const string CONFIG_GLOBAL_SECTION = "Global";
        private const string CONFIG_FILE_NAME = "PluginFrame.ini";
        private const string CONFIG_REFRESH_INTERVAL = "RefreshInterval";
        private const string CONFIG_TIMEOUT_GLOBAL = "TimeOut";
        private const string CONFIG_PLUGIN_FOLDER = "PluginFolder";
        private const string CONFIG_PLUGIN_SECTION = "Plugin";
        private const string CONFIG_PLUGIN_NAME = "Name";
        private const string CONFIG_PLUGIN_TYPE = "Type";
        private const string CONFIG_PLUGIN_TYPE_REQUEST = "Request";
        private const string CONFIG_PLUGIN_TYPE_OBLIGATION = "Obligation";
        private const string CONFIG_PLUGIN_SEQUENCE = "Sequence";
        private const string CONFIG_PLUGIN_TIMEOUT = "TimeOut";
        private const string CONFIG_PLUGIN_CONTINUE = "Continue";
        private const string CONFIG_PLUGIN_CONTINUE_YES = "Yes";
        private const string CONFIG_PLUGIN_CONTINUE_NO = "No";
        private const string CONFIG_PLUGIN_PREEXECUTION = "Pre_Execution";
        private const string CONFIG_PLUGIN_PREEXECUTION_YES = "Yes";
        private const string CONFIG_PLUGIN_PREEXECUTION_NO = "No";
        private const string CONFIG_PLUGIN_OBLIGATION_NAME = "ObligationName";

        private const int ReadLenth = 2048;
        private const int MaxPlugins = 100;


        private static long g_nInterval = 1200;//in minute
#endregion

        private PluginFrameGlobalParam m_GlobalParams;
        private List<PluginParam> m_PluginFiles;
        private List<RequestPluginStruct> m_RequestPlugins;
        private List<ObligationPluginStruct> m_ObligationPrePlugins;
        private List<ObligationPluginStruct> m_ObligationAfterPlugins;

        private PluginFrameGlobalParam GlobalParams
        {
            get { return m_GlobalParams; }
            set { m_GlobalParams = value; }
        }

        private List<PluginParam> PluginFiles
        {
            get { return m_PluginFiles; }
            set { m_PluginFiles = value; }
        }

        private List<ObligationPluginStruct> ObligationPrePlugins
        {
            get { return m_ObligationPrePlugins; }
            set { m_ObligationPrePlugins = value; }
        }

        private List<ObligationPluginStruct> ObligationAfterPlugins
        {
            get { return m_ObligationAfterPlugins; }
            set { m_ObligationAfterPlugins = value; }
        }

        private List<RequestPluginStruct> RequestPlugins
        {
            get { return m_RequestPlugins; }
            set { m_RequestPlugins = value; }
        }

        private static DateTime? PluginReloadTime;
        public static bool IsPluginEnabled()
        {
            bool PluginEnabled = false;
            try
            {
                using (var ceKey = Registry.LocalMachine.OpenSubKey(@"Software\NextLabs\Compliant Enterprise\Sharepoint Enforcer\", false))
                {
                    if (ceKey != null)
                    {
                        object regValue = ceKey.GetValue(PLUGIN_ENABLED_REGISTRY_KEY, string.Empty);
                        bool enabled = false;
                        if (regValue != null && bool.TryParse(regValue.ToString(), out enabled))
                        {
                            PluginEnabled = enabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception in IsPluginEnabled: ", null, ex);
            }

            if (PluginReloadTime.HasValue && PluginReloadTime <= DateTime.Now)
                instance.LoadPluginFrameConfig();

            return PluginEnabled;
        }

        private PluginFrame()
        {
            InitPluginFrame();
        }

        private static object syncRoot = new Object();

        public static PluginFrame Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new PluginFrame();
                        }
                    }
                }
                return instance;
            }
        }

        private void InitPluginFrame()
        {
            PluginFiles = new List<PluginParam>();
            RequestPlugins = new List<RequestPluginStruct>();
            ObligationPrePlugins = new List<ObligationPluginStruct>();
            ObligationAfterPlugins = new List<ObligationPluginStruct>();
            GlobalParams = new PluginFrameGlobalParam();

            LoadPluginFrameConfig(true);
        }
        private bool LoadPluginFrameConfig(bool bForce = false)
        {
            try
            {
                string ConfigFilePath = Globals.GetSPEPath() + "config\\" + CONFIG_FILE_NAME;

                if (!bForce && !NeedReloadConfigFile(ConfigFilePath))
                    return false;

                if (PluginFiles != null)
                    PluginFiles.Clear();

                int ErrorCode;

                //read global parameters
                long RefreshInterval = IniReadValueinLong(ConfigFilePath, CONFIG_GLOBAL_SECTION, CONFIG_REFRESH_INTERVAL, out ErrorCode);
                g_nInterval = RefreshInterval;

                m_GlobalParams.ExecuteTimeOut = IniReadValueinLong(ConfigFilePath, CONFIG_GLOBAL_SECTION, CONFIG_TIMEOUT_GLOBAL, out ErrorCode);
                m_GlobalParams.Pluginfolder = IniReadValue(ConfigFilePath, CONFIG_GLOBAL_SECTION, CONFIG_PLUGIN_FOLDER, out ErrorCode);

                StringBuilder PluginSec = new StringBuilder(CONFIG_PLUGIN_SECTION);
                string PluginName = CONFIG_PLUGIN_SECTION;

                for (int i = 1; i < MaxPlugins; ++i)
                {
                    PluginName = PluginSec.ToString() + i.ToString();
                    PluginParam Plugin = new PluginParam();
                    Plugin.PluginName = IniReadValue(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_NAME, out ErrorCode);
                    if (ErrorCode == 2)
                    {
                        break; // this seciton is not exist, stop
                    }

                    Plugin.PluginTimeOut = IniReadValueinLong(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_TIMEOUT, out ErrorCode);
                    if (ErrorCode != 0 || Plugin.PluginTimeOut <= 0)
                        Plugin.PluginTimeOut = GlobalParams.ExecuteTimeOut;


                    Plugin.Type = PluginType.ObligationPlugin;
                    string Value = IniReadValue(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_TYPE, out ErrorCode);
                    if (Value.Equals(CONFIG_PLUGIN_TYPE_REQUEST))
                    {
                        Plugin.Type = PluginType.RequestPlugin;
                    }
                    Plugin.Sequece = IniReadValueinInt(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_SEQUENCE, out ErrorCode);

                    Plugin.NeedContinue = true;
                    Value = IniReadValue(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_CONTINUE, out ErrorCode);
                    if (Value.Equals(CONFIG_PLUGIN_CONTINUE_NO))
                    {
                        Plugin.NeedContinue = false;
                    }

                    Plugin.ObligationName = IniReadValue(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_OBLIGATION_NAME, out ErrorCode);

                    Plugin.Pre_Execution = true;
                    Value = IniReadValue(ConfigFilePath, PluginName.ToString(), CONFIG_PLUGIN_PREEXECUTION, out ErrorCode);
                    if (Value.Equals(CONFIG_PLUGIN_PREEXECUTION_NO))
                    {
                        Plugin.Pre_Execution = false;
                    }

                    PluginFiles.Add(Plugin);
                }

                LoadAssembles();
                PluginReloadTime = DateTime.Now.AddMinutes(RefreshInterval);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception in LoadPluginFrameConfig: ", null, ex);
            }

            return true;
        }
        private bool NeedReloadConfigFile(string FilePath)
        {
            bool bRet = true;

            DateTime FileLastModified = File.GetLastWriteTime(FilePath);

            if (FileLastModified.AddMinutes(g_nInterval).CompareTo(PluginReloadTime) < 0)
            {
                bRet = false;
            }
            return bRet;
        }


        private bool LoadAssembles()
        {
            if (RequestPlugins != null)
                RequestPlugins.Clear();

            if (ObligationPrePlugins != null)
                ObligationPrePlugins.Clear();

            if (ObligationAfterPlugins != null)
                ObligationAfterPlugins.Clear();

            bool Ret = true;
            string RequestInterfaceName = typeof(NextLabs.PluginInterface.IProcessHttpRequest).FullName;
            string ObligationInterfaceName = typeof(NextLabs.PluginInterface.IExecuteObligation).FullName;

            foreach (PluginParam plugin in PluginFiles)
            {
                try
                {
                    Assembly asm = Assembly.LoadFile(GlobalParams.Pluginfolder + "\\" + plugin.PluginName);
                    Type[] t = asm.GetExportedTypes();

                    foreach (Type type in t)
                    {
                        if (type.GetInterface(INTERFACE_REQUEST) != null)
                        {
                            NextLabs.PluginInterface.IProcessHttpRequest ProcessHttpRequest = (NextLabs.PluginInterface.IProcessHttpRequest)Activator.CreateInstance(type);
                            RequestPluginStruct RequestPlugin = new RequestPluginStruct();
                            RequestPlugin.asm = asm;
                            RequestPlugin.InterfaceObj = ProcessHttpRequest;
                            RequestPlugin.Sequence = plugin.Sequece;
                            RequestPlugin.Continue = plugin.NeedContinue;
                            RequestPlugins.Add(RequestPlugin);
                            NLLogger.OutputLog(LogLevel.Debug, "LoadAssembles, RequestPlugin added: " + plugin.PluginName);
                        }
                        else if (type.GetInterface(INTERFACE_OBLIGATION) != null)
                        {
                            NextLabs.PluginInterface.IExecuteObligation ProcessHttpRequest = (NextLabs.PluginInterface.IExecuteObligation)Activator.CreateInstance(type);
                            ObligationPluginStruct ObligationPlugin = new ObligationPluginStruct();
                            ObligationPlugin.asm = asm;
                            ObligationPlugin.InterfaceObj = ProcessHttpRequest;
                            ObligationPlugin.Sequence = plugin.Sequece;
                            ObligationPlugin.Continue = plugin.NeedContinue;
                            ObligationPlugin.ObligationName = plugin.ObligationName;
                            if (plugin.Pre_Execution)
                            {
                                ObligationPrePlugins.Add(ObligationPlugin);
                            }
                            else
                            {
                                ObligationAfterPlugins.Add(ObligationPlugin);
                            }
                            NLLogger.OutputLog(LogLevel.Debug, "LoadAssembles, ObligationPlugin added: " + plugin.PluginName + " pre-execute: " + plugin.Pre_Execution.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception in LoadAssembles,Export:", null, ex);
                }
            }
            try
            {
                //sort all plugins with sequences.
                RequestPlugins.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
                ObligationPrePlugins.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
                ObligationAfterPlugins.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception in LoadAssembles,sorts:", null, ex);
            }

            return Ret;
        }

        private bool IsRequestPluginResultRight(EvalObjectType eot, string SiteUrl, string ListUrl, string ItemUrl)
        {
            bool Ret = true;
            switch (eot)
            {
                case EvalObjectType.EOTItem:
                    if (string.IsNullOrEmpty(ItemUrl) || string.IsNullOrEmpty(ListUrl) || string.IsNullOrEmpty(SiteUrl))
                        Ret = false;
                    break;
                case EvalObjectType.EOTList:
                    if (string.IsNullOrEmpty(ListUrl) || string.IsNullOrEmpty(SiteUrl))
                        Ret = false;
                    break;

                case EvalObjectType.EOTSite:
                    if (string.IsNullOrEmpty(SiteUrl))
                        Ret = false;
                    break;
                case EvalObjectType.EOTUnknown:
                    Ret = false;
                    break;
            }
            return Ret;
        }

        public bool RunRequestPlugins(HttpRequest request, HttpResponse response,ref EvalObjectType eot, ref EvalAction ea, ref string SiteUrl, ref string ListUrl, ref string ItemUrl, int Timeout = 3000)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Enter RunRequestPlugins. Parameters: Site url: " + SiteUrl + ". List url:" + ListUrl + " . Item url: " + ItemUrl);
            string Logs = string.Empty;
            bool bValid = false;
            try
            {
                foreach(RequestPluginStruct plugin in RequestPlugins)
                {
                    Logs = string.Empty;
                    plugin.InterfaceObj.ProcessHttpRequest(request, response, ref eot,ref ea, ref SiteUrl, ref ListUrl, ref ItemUrl, ref Logs, Timeout);
                    NLLogger.OutputLog(LogLevel.Debug, "RunRequestPlugins: Log from plugin[" + plugin.Sequence.ToString() + "]" + Logs);
                    bValid = IsRequestPluginResultRight(eot, SiteUrl, ListUrl, ItemUrl);
                    if (bValid)
                        break;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception in RunRequestPlugins: ", null, ex);
            }
            return !bValid;
        }

        public bool RunObligationplugins(string WebUrl, Guid ListID, int ItemId, List<string> Obligations, bool bPre, int Timeout = 3000)
        {
            bool Ret = true;
            try
            {
                List<ObligationPluginStruct> ObligationPlugins;
                if (bPre)
                    ObligationPlugins = ObligationPrePlugins;
                else
                    ObligationPlugins = ObligationAfterPlugins;

                if (ObligationPlugins.Count <= 0)
                    return false;
                string Logs = string.Empty;

                List<string> strArrOb = new List<string>();
                foreach(string st in Obligations)
                {
                    strArrOb.Add(st.ToLower());
                }

                foreach (ObligationPluginStruct plugin in ObligationPlugins)
                {
                    Logs = string.Empty;
                    if (!strArrOb.Contains(plugin.ObligationName.ToLower()))//the obligation is not configured
                        continue;
                    plugin.InterfaceObj.ExecuteObligation(WebUrl, ListID, ItemId, ref Logs, Timeout);
                    NLLogger.OutputLog(LogLevel.Debug, "RunObligationplugins: Log from plugin[" + plugin.Sequence.ToString() + "]" + Logs);
                    if (!plugin.Continue)
                        break;
                    Ret = plugin.Continue;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception in RunObligationplugins: ", null, ex);
            }

            return Ret;
        }

        private string IniReadValue(string path, string section, string key, out int ErrorCode)
        {
            StringBuilder temp = new StringBuilder(ReadLenth);
            int i = GetPrivateProfileString(section, key, "", temp, ReadLenth, path);
            ErrorCode = Marshal.GetLastWin32Error();
            return temp.ToString().Trim();
        }

        private int IniReadValueinInt(string path, string section, string key, out int ErrorCode)
        {
            int RetValue = 0;
            string ReadValue = IniReadValue(path, section, key, out ErrorCode);
            if (!string.IsNullOrEmpty(ReadValue))
            {
                int.TryParse(ReadValue, out RetValue);
            }

            return RetValue;
        }

        private long IniReadValueinLong(string path, string section, string key, out int ErrorCode)
        {
            long RetValue = 0;
            string ReadValue = IniReadValue(path, section, key, out ErrorCode);
            if (!string.IsNullOrEmpty(ReadValue))
            {
                long.TryParse(ReadValue, out RetValue);
            }
            return RetValue;
        }

        private void IniWriteValue(string section, string key, string iValue, string path)
        {
            WritePrivateProfileString(section, key, iValue, path);
        }

        #region p/invoke
        [DllImport("kernel32", SetLastError = true)]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        #endregion
    }
}


namespace NextLabs.PluginInterface
{
    public enum EvalObjectType
    {
        EOTUnknown,
        EOTSite,
        EOTList,
        EOTItem
    }
    public enum EvalAction
    {
        EAUknown,
        EARead,
        EAWrite,
        EADelete
    }
    public interface IProcessHttpRequest
    {
        int ProcessHttpRequest(
            HttpRequest request,
            HttpResponse response,
            ref EvalObjectType eot,
            ref EvalAction ea,
            ref string SiteUrl,
            ref string ListUrl,
            ref string ItemUrl,
            ref string Logs,
            int Timeout = 3000);
    }
    public interface IExecuteObligation
    {
        int ExecuteObligation(
            string WebUrl,
            Guid ListGuid,
            int ItemId,
            ref string Logs,
            int Timeout = 3000);
    }


}
