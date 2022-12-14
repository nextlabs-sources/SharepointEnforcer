using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using log4net;
using log4net.Core;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using NextLabs.Diagnostic.log4net.Appender;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace NextLabs.Diagnostic
{

    public class SPELoggerShadow
    {
        private static ILog l4nlog;
        private static object syncRoot = new Object();
        private static SPELoggerShadow instance = null;

        protected SPELoggerShadow()
        {
            l4nlog = LogManager.GetLogger("PerfLogger");
        }
        ~SPELoggerShadow()
        {
            Trace.WriteLine("SPELogger delete");

        }

        public static SPELoggerShadow Instance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    // Uses "Lazy initialization"
                    if (instance == null)
                        instance = new SPELoggerShadow();
                }
            }
            return instance;
        }

        public enum LogLevel
        {
            Fatal = 0,
            Error = 1,
            Warn = 2,
            Info = 3,
            Debug = 4,
            Unused = 5
        }


        private void ChangeLog4netLogFileName(ILog iLog, string fileName)
        {
            LogImpl logImpl = iLog as LogImpl;
            if (logImpl != null)
            {
                AppenderCollection ac = ((Logger)logImpl.Logger).Appenders;
                for (int i = 0; i < ac.Count; i++)
                {
                    RollingFileAppender rfa = ac[i] as RollingFileAppender;
                }
            }
        }

        public void log(LogLevel level, string msg, string caller, string request_url, string user_name, string ip_address)
        {
            lock (syncRoot)
            {
                string _msg;
                string _caller = caller;
                ChangeLog4netLogFileName(l4nlog, null);
                if (string.IsNullOrEmpty(caller))
                {
                    _caller = "NextLabs.Diagnostic.SPELogger";
                }
                _msg = _caller;
                if (!string.IsNullOrEmpty(ip_address))
                    _msg = _msg + "[IP:" + ip_address.PadRight(15, ' ') + "]";

                string request_str = request_url + user_name + ip_address;
                if (!string.IsNullOrEmpty(request_str))
                {
                    int request_id = request_str.GetHashCode();
                    _msg = _msg + "[RequestID:0x" + request_id.ToString("X").PadRight(8, ' ') + "]";
                }

                if (!string.IsNullOrEmpty(user_name))
                    _msg = _msg + "[User:" + user_name + "]";

                _msg += msg;

                int tick = System.Environment.TickCount;
                int sec = DateTime.Now.Second;
                int mili = DateTime.Now.Millisecond;
                string current_date = DateTime.Now.ToString();
                _msg = "[" + current_date + "]" + "[sec" + sec + "]" + "[ticks" + tick + "]" + "[thread " + Thread.CurrentThread.ManagedThreadId + "]" + msg;

                _msg = _msg.Replace('%', '#');
                l4nlog.Fatal(_msg);
            }
        }
    }

    public class SPELogger
    {
        private static ILog l4nlog;
        private static object syncRoot = new Object();

        private static SPELogger instance = null;
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern Int64 LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        // Constructor
        protected SPELogger()
        {
            string LogDir = null;
            string BinDir = null;
            string ReglogInstallDir_str = null;
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer", false);                
                object ReglogInstallDir = null;
                if (Software_key != null)
                    ReglogInstallDir = Software_key.GetValue("InstallDir");
                if (ReglogInstallDir != null)
                {
                    ReglogInstallDir_str = Convert.ToString(ReglogInstallDir);
                    string configdir = "Config\\Log.Config";
                    string dlldir = null;
                    if (IntPtr.Size == 4)
                    {
                        dlldir = "bin\\CE_Log_Interface32.dll";
                    }
                    else
                    {
                        dlldir = "bin\\CE_Log_Interface.dll";  
                    }
                    if (ReglogInstallDir_str.EndsWith("\\"))
                    {
                        LogDir = ReglogInstallDir_str + configdir;
                        BinDir = ReglogInstallDir_str + dlldir;
                    }
                    else
                    {
                        LogDir = ReglogInstallDir_str + configdir;
                        BinDir = ReglogInstallDir_str + dlldir;
                    }
                }

            }
            catch(Exception e)
            {
                Trace.WriteLine("SPELogger ctor Exception:" + e.Message);
            }
            Trace.WriteLine(LogDir);
            if (LogDir != null)
            {
                try
                {
                    FileInfo file = new FileInfo(LogDir);
                    XmlConfigurator.ConfigureAndWatch(file);
                }
                catch (UnauthorizedAccessException exp)
                {
                    Trace.WriteLine("Exception:" + exp.Message);
                }
            }
            else
            {
                XmlConfigurator.Configure();
            }
            if (BinDir != null)
            {
                Int64 re = LoadLibrary(BinDir);
                Trace.WriteLine("LoadLibrary result is" + re);

            }
            l4nlog = LogManager.GetLogger("SPELogger");
            SetCELogPath(l4nlog, "SharePointCELogAppender", ReglogInstallDir_str);
          
        }
        ~SPELogger()
        {
            Trace.WriteLine("SPELogger delete");

        }

        public static void SetCELogPath(ILog iLog, string strAppenderName, string strReglogInstallDir)
        {
            try
            {
                bool bFind = false;
                AppenderCollection ac = ((Logger)iLog.Logger).Appenders;
                RollingFileAppender rfa = null;
                System.Diagnostics.Trace.WriteLine(ac); 
                for (int i = 0; i < ac.Count; i++)
                {
                    rfa = ac[i] as RollingFileAppender;

                    if (rfa != null && rfa.Name.Equals(strAppenderName))
                    {
                        bFind = true;
                        break;
                    }
                }
                if (bFind && !string.IsNullOrEmpty(strReglogInstallDir))
                {
                    rfa.File = strReglogInstallDir + "Logs\\SharepointEnforcer.log";
                    rfa.ActivateOptions();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception:" + ex.StackTrace + ex.Message);
            }
        }

        // Methods
        public static SPELogger Instance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    // Uses "Lazy initialization"
                    if (instance == null)
                        instance = new SPELogger();
                }
            }
            return instance;
        }

        public enum LogLevel
        {
            Fatal = 0,
            Error = 1,
            Warn = 2,
            Info = 3,
            Debug = 4,
            Unused = 5
        }

        public void log(LogLevel level, string msg, string caller, string request_url, string user_name, string ip_address)
        {
            lock (syncRoot)
            {
                string _msg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffZ") + " - ";
                string _caller = caller;
                if (string.IsNullOrEmpty(caller))
                {
                    _caller = "NextLabs.Diagnostic.SPELogger";
                }
                _msg += "[Caller:" + _caller + "]";
                if(!string.IsNullOrEmpty(ip_address))
                    _msg = _msg + "[IP:" + ip_address.PadRight(15,' ') +"]";

                string request_str = request_url + user_name + ip_address;
                if (!string.IsNullOrEmpty(request_str))
                {
                    int request_id = request_str.GetHashCode();
                    _msg = _msg+ "[RequestID:0x" + request_id.ToString("X").PadRight(8,' ') + "]" ;
                }

                if (!string.IsNullOrEmpty(user_name))
                    _msg = _msg + "[User:" + user_name + "]";

                _msg += msg;
                
                _msg = _msg.Replace('%', '#');
                switch (level)
                {
                    case LogLevel.Fatal:
                        _msg = "[EMERGENCY] - " + _msg;
                        l4nlog.Fatal(_msg);
                        break;
                    case LogLevel.Error:
                        _msg = "[ERROR] - " + _msg;
                        l4nlog.Error(_msg);
                        break;
                    case LogLevel.Warn:
                        _msg = "[WARN] - " + _msg;
                        l4nlog.Warn(_msg);
                        break;
                    case LogLevel.Info:
                        _msg = "[INFO] - " + _msg;
                        l4nlog.Info(_msg);
                        break;
                    case LogLevel.Debug:
                        _msg = "[DEBUG] - " + _msg;
                        l4nlog.Debug(_msg);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
