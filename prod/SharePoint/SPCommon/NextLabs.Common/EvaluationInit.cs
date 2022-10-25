using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPEEvalInit
    {
        public enum ProcessType
        {
            CE_X86,
            CE_X64,
        }
        public static String[] _sAclinvPage_PostWords = { "" };
        public static String[] _sNewgrpPage_PostWords = { "" };
        public static String[] _sPeoplePage_PostWords = { "" };
        public static String[] _sEditgrpPage_PostWords = { "" };
        public static String[] _sPermsetupPage_PostWords = { "" };
        public static String[] _sMngsiteadminPage_PostWords = { "" };
        public static String[] _sEditprmsPage_PostWords = { "" };
        public static String[] _sUserPage_PostWords = { "" };
        public static String[] _sAddrolePage_PostWords = { "" };
        public static String[] _sRolePage_PostWords = { "" };
        public static String[] _sEditRolePage_PostWords = { "" };

        public static String[] _sAclinvPage_PostWords_2010 = { "" };
        public static String[] _sNewgrpPage_PostWords_2010 = { "" };
        public static String[] _sPeoplePage_PostWords_2010 = { "" };
        public static String[] _sEditgrpPage_PostWords_2010 = { "" };
        public static String[] _sPermsetupPage_PostWords_2010 = { "" };
        public static String[] _sMngsiteadminPage_PostWords_2010 = { "" };
        public static String[] _sEditprmsPage_PostWords_2010 = { "" };
        public static String[] _sUserPage_PostWords_2010 = { "" };
        public static String[] _sAddrolePage_PostWords_2010 = { "" };
        public static String[] _sRolePage_PostWords_2010 = { "" };
        public static String[] _sEditRolePage_PostWords_2010 = { "" };


        private static bool m_Taglibrary_loaded = false;
        private static string[] m_Taglibrary = {
                                                 "TagDocProtector.dll"
                                             };
        private static string[] m_Taglibrary32 = {
                                                 "TagDocProtector32.dll"
                                             };
        private static string[] m_Tagdeplibrary = {
                                                 "cesdk.dll",
                                                 "RESATTRLIB.DLL",
                                                 "celog.dll",
                                                 "zlibwapi.dll",
                                                 "nl_sysenc_lib.dll",
                                                 "PODOFOLIB.DLL",
                                                 "RESATTRMGR.DLL",
                                             };
        private static string[] m_Tagdeplibrary32 = {
                                                 "cesdk32.dll",
                                                 "RESATTRLIB32.DLL",
                                                 "celog32.dll",
                                                 "zlibwapi32.dll",
                                                 "nl_sysenc_lib32.dll",
                                                 "zlib1.DLL",
                                                 "freetype6.dll",
                                                 "PODOFOLIB.DLL",
                                                 "libtiff.DLL",
                                                 "pdflib32.dll",
                                                 "RESATTRMGR32.DLL",
                                             };
        private static string m_Soapconfig = "soap_define.cfg";

        private static string m_gPCDir = null;
        private static string m_gSPEDir = null;
        static public IDictionary<string, XmlDocument> SoapActionMap = null;
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern Int64 LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(int HModule);
        private static string GetPCPath()
        {
            if (m_gPCDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegPPCInstallDir = null;
                string PCDir = null;
                if (CE_key != null)
                    RegPPCInstallDir = CE_key.GetValue("PolicyControllerDir");
                if (RegPPCInstallDir != null)
                {
                    PCDir = Convert.ToString(RegPPCInstallDir);
                    if (PCDir != null && !PCDir.EndsWith("\\"))
                        PCDir = PCDir + "\\";
                }
                m_gPCDir = PCDir;
            }
            return m_gPCDir;
        }
        private static string GetInstallPath()
        {
            if (m_gPCDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegPPCInstallDir = null;
                string PCDir = null;
                if (CE_key != null)
                    RegPPCInstallDir = CE_key.GetValue("InstallDir");
                if (RegPPCInstallDir != null)
                {
                    PCDir = Convert.ToString(RegPPCInstallDir);
                    if (PCDir != null && !PCDir.EndsWith("\\"))
                        PCDir = PCDir + "\\";
                }
                m_gPCDir = PCDir;
            }
            return m_gPCDir;
        }

        private static string GetSPEPath()
        {
            if (m_gSPEDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegSPEInstallDir = null;
                string SPEDir = null;
                if (CE_key != null)
                    RegSPEInstallDir = CE_key.GetValue("InstallDir");
                if (RegSPEInstallDir != null)
                {
                    SPEDir = Convert.ToString(RegSPEInstallDir);
                    if (SPEDir != null && !SPEDir.EndsWith("\\"))
                        SPEDir = SPEDir + "\\";
                }
                m_gSPEDir = SPEDir;
            }
            return m_gSPEDir;
        }

        public static bool InitAdminLogConfig()
        {

#if SP2013
            //If it's sp2013 should also init 2010 tyle
            SPEEvalInit.InitAdminLogConfig2010();
#endif
            try
            {
                String CEBinDir = GetSPEPath() + "config\\Page_PostWord.cfg";
                if (CEBinDir == null)
                {
                    return false;
                }
                using (FileStream fs = new FileStream(CEBinDir, FileMode.Open, FileAccess.Read))
                {
                    if (fs != null)
                    {
                        byte[] _filecontent = new byte[fs.Length];
                        fs.Read(_filecontent, 0, (int)fs.Length);
                        String _spFilecontent = System.Text.Encoding.ASCII.GetString(_filecontent);
                        ParseConfigContent(_spFilecontent);
                        fs.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during InitAdminLogConfig:", null, ex);
            }
            return false;
        }

        public static bool InitAdminLogConfig2010()
        {
            try
            {
                String CEBinDir = GetSPEPath() + "config\\Page_PostWord2010.cfg";
                if (CEBinDir == null)
                {
                    return false;
                }
                using (FileStream fs = new FileStream(CEBinDir, FileMode.Open, FileAccess.Read))
                {
                    if (fs != null)
                    {
                        byte[] _filecontent = new byte[fs.Length];
                        fs.Read(_filecontent, 0, (int)fs.Length);
                        String _spFilecontent = System.Text.Encoding.ASCII.GetString(_filecontent);
                        ParseConfigContent2010(_spFilecontent);
                        fs.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during InitAdminLogConfig2010:", null, ex);
            }
            return false;
        }

        private static void ParseConfigContent(String _spFilecontent)
        {
            String[] _Words = _spFilecontent.Split(new String[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < _Words.Length; i++)
            {
                if (_Words[i] != null && _Words[i].EndsWith("\r"))
                {
                    _Words[i] = _Words[i].Substring(0, _Words[i].Length - 1);
                }
                if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Aclinv.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sAclinvPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Newgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sNewgrpPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("People.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sPeoplePage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Editgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditgrpPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Permsetup.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sPermsetupPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Mngsiteadmin.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sMngsiteadminPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Editprms.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditprmsPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("user.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sUserPage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Addrole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sAddrolePage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Role.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sRolePage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("EditRole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditRolePage_PostWords = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
            }
        }

        private static void ParseConfigContent2010(String _spFilecontent)
        {
            String[] _Words = _spFilecontent.Split(new String[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < _Words.Length; i++)
            {
                if (_Words[i] != null && _Words[i].EndsWith("\r"))
                {
                    _Words[i] = _Words[i].Substring(0, _Words[i].Length - 1);
                }
                if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Aclinv.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sAclinvPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Newgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sNewgrpPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("People.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sPeoplePage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Editgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditgrpPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Permsetup.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sPermsetupPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Mngsiteadmin.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sMngsiteadminPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Editprms.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditprmsPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("user.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sUserPage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Addrole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sAddrolePage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("Role.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sRolePage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
                else if (!string.IsNullOrEmpty(_Words[i]) && _Words[i].StartsWith("EditRole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _sEditRolePage_PostWords_2010 = _Words[i].Split(new String[] { "," }, StringSplitOptions.None);
                }
            }
        }

        private static ProcessType CheckProcessType()
        {
            if (IntPtr.Size == 4)
                return ProcessType.CE_X86;
            else
                return ProcessType.CE_X64;
        }

        public static bool LoadCETAGDEPLibrary()
        {
            Int64 re = 0;
            try
            {
                String PPCBinDir = GetInstallPath() + "Common\\";
                ProcessType _ProcessType = CheckProcessType();
                if (_ProcessType == ProcessType.CE_X64)
                    PPCBinDir += "bin64\\";
                else
                    PPCBinDir += "bin32\\";
                if (PPCBinDir == null)
                {
                    return false;
                }
                int _length = 0;
                if (_ProcessType == ProcessType.CE_X64)
                    _length = m_Tagdeplibrary.Length;
                else
                    _length = m_Tagdeplibrary32.Length;

                for (int i = 0; i < _length; i++)
                {
                    String LoadDir = null;
                    if (_ProcessType == ProcessType.CE_X64)
                        LoadDir = PPCBinDir + m_Tagdeplibrary[i];
                    else
                        LoadDir = PPCBinDir + m_Tagdeplibrary32[i];
                    re = LoadLibrary(LoadDir);
                    if (re <= 0)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "LoadCETAGDEPLibrary call Load:[{0}] failed", new object[] { LoadDir });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during LoadCETAGDEPLibrary:", null, ex);
            }
            return false;
        }



        public static bool LoadCETAGLibrary()
        {
            if (m_Taglibrary_loaded)
                return true;
            Int64 re = 0;
            try
            {
                string CEBinDir = GetSPEPath() + "bin\\"; ;
                ProcessType _ProcessType = CheckProcessType();
                if (CEBinDir == null)
                {
                    return false;
                }
                for (int i = 0; i < m_Taglibrary.Length; i++)
                {
                    String LoadDir = null;
                    if (_ProcessType == ProcessType.CE_X64)
                        LoadDir = CEBinDir + m_Taglibrary[i];
                    else
                        LoadDir = CEBinDir + m_Taglibrary32[i];
                    re = LoadLibrary(LoadDir);
                    if (re <= 0)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "LoadCETAGLibrary call Load:[{0}] failed", new object[] { LoadDir });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during LoadCETAGLibrary:", null, ex);
            }
            return false;
        }

        public static bool _LoadSoapProtocolConfig()
        {
            try
            {
                string CEBinDir = GetSPEPath() + "config\\" + m_Soapconfig; ;
                if (CEBinDir != null)
                {
                    using(FileStream fs = new FileStream(CEBinDir, FileMode.Open, FileAccess.Read))
                    {
                        if (fs != null && fs.Length > 0)
                        {
                            if (SoapActionMap == null)
                            {
                                SoapActionMap = new SortedList<string, XmlDocument>();
                            }
                            byte[] _filecontent = new byte[fs.Length];
                            fs.Read(_filecontent, 0, (int)fs.Length);
                            using (Stream InputStream = new MemoryStream(_filecontent))
                            {
                                XmlDocument doc = new XmlDocument();
                                doc.Load(InputStream);
                                XmlNodeList nodes = doc.DocumentElement.SelectNodes("SoapAction");
                                for (int i = 0; i < nodes.Count; i++)
                                {
                                    if (nodes[0].ChildNodes[0].NodeType == XmlNodeType.Text)
                                    {
                                        String _xmltxt = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
                                        _xmltxt += nodes[i].OuterXml;
                                        byte[] InputBuffer = Encoding.Default.GetBytes(_xmltxt);
                                        using (Stream _InputStream = new MemoryStream(InputBuffer))
                                        {
                                            XmlDocument _doc = new XmlDocument();
                                            _doc.Load(_InputStream);
                                            SoapActionMap[nodes[i].ChildNodes[0].Value] = _doc;
                                        }
                                    }
                                }
                                fs.Close();
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during _LoadSoapProtocolConfig:", null, ex);
            }
            return false;
        }
    }
}
