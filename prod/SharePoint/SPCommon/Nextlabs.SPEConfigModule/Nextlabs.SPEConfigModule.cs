using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
namespace Nextlabs.SPEConfigModule
{     
    public class SPEConfig
    {
        public enum SPEType
        {
            SPE_2007,
            SPE_2010,
        }
        private SPEType m_GlobalSPEType;
        private static SPEConfig instance = null;
        private static object syncRoot_ = new Object();
        protected SPEConfig()
        {
            m_GlobalSPEType = SPEType.SPE_2010;
            _LoadSPEConfig();
        }

        public bool _LoadSPEConfig()
        {
            try
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegCEInstallDir = null;
                string CEBinDir = null;
                if (CE_key != null)
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                if (RegCEInstallDir != null)
                {
                    String RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (RegCEInstallDir_str.EndsWith("\\"))
                        CEBinDir = RegCEInstallDir_str + "config\\" + "SPEConfig.cfg";
                    else
                        CEBinDir = RegCEInstallDir_str + "\\config\\" + "SPEConfig.cfg";
                }
                if (CEBinDir != null)
                {
                    using (FileStream fs = new FileStream(CEBinDir, FileMode.Open, FileAccess.Read))
                    {
                        if (fs != null && fs.Length > 0)
                        {
                            byte[] _filecontent = new byte[fs.Length];
                            fs.Read(_filecontent, 0, (int)fs.Length);
                            String _content = System.Text.Encoding.ASCII.GetString(_filecontent);
                            if (_content.IndexOf("SPE2007", StringComparison.OrdinalIgnoreCase) != -1)
                                m_GlobalSPEType = SPEType.SPE_2007;
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                 Trace.WriteLine("_LoadSPEConfig  failed:" + e.Message);
            }
            return false;
        }


        ~SPEConfig()
        {
        }
        public static SPEConfig Instance()
        {
            lock (syncRoot_)
            {
                // Uses "Lazy initialization"
                if (instance == null)
                    instance = new SPEConfig();
            }
            return instance;
        }      

        public SPEType GlobalSPEType
        {
            get { return m_GlobalSPEType; }
            set { m_GlobalSPEType = value; }
        }
    }
}
