using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using NextLabs.CSCInvoke;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    class EvaluatorApplication
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern Int64 LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(int HModule);
        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        public static extern int GetLastError();

        private static string[] librarynames = { "CEBRAIN.dll", "CECEM.dll", "CEMARSHAL50.dll", "CETRANSPORT.dll", "CEPEPMAN.dll", "CECONN.dll", "CEEVAL.dll", "CELOG.dll" };

        private static EvaluatorApplication Instance = null;
        private CETYPE.CEApplication m_Application;

        public CETYPE.CEApplication Application
        {
            get { return m_Application; }
        }

        public EvaluatorApplication()
        {
            m_Application = new CETYPE.CEApplication("SharePoint", null, null);
        }

        public static EvaluatorApplication CreateInstance()
        {
            if (Instance == null)
            {
                LoadSDKLibrary();
                Instance = new EvaluatorApplication();
            }

            return Instance;
        }

        private static bool LoadSDKLibrary()
        {
            try
            {
                Int64 re = 0L;
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegPPCInstallDir = null;
                string PPCBinDir = null;
                if (Software_key != null)
                    RegPPCInstallDir = Software_key.GetValue("PolicyControllerDir");
                if (RegPPCInstallDir != null)
                {
                    String RegPPCInstallDir_str = Convert.ToString(RegPPCInstallDir);
                    if (RegPPCInstallDir_str.EndsWith("\\"))
                        PPCBinDir = RegPPCInstallDir_str + "bin\\";
                    else
                        PPCBinDir = RegPPCInstallDir_str + "\\bin\\";
                }
                if (PPCBinDir == null)
                {
                    return false;
                }
                for (int i = 0; i < librarynames.Length; i++)
                {
                    String LoadDir = PPCBinDir + librarynames[i];
                    re = LoadLibrary(LoadDir);
                    if (re <= 0L)
                    {
                        int iError = GetLastError();
                        NLLogger.OutputLog(LogLevel.Debug, "EvaluatorApplication call _LoadSDKLibrary Load " + LoadDir + " failed. Error=" + iError.ToString());
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during LoadSDKLibrary:", null, ex);
            }
            return false;

        }
    }
}
