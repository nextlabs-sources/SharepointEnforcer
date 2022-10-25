using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextLabs.Diagnostic
{
   public class DumpManager
    {
        private delegate Int32 CallBackUnhandledExceptionFilter();

        [DllImport("kernel32")]
        private static extern Int32 SetUnhandledExceptionFilter(CallBackUnhandledExceptionFilter pFuncCB);

        private static Int32 dumpExceptionfilter()
        {
            MiniDump.TryDump(MiniDump.MiniDumpType.WithFullMemory);
            return 0;
        }

        public static void DumpInitalize()
        {
            SetUnhandledExceptionFilter(new CallBackUnhandledExceptionFilter(dumpExceptionfilter));
        }
    }
    static class MiniDump
    {
        [DllImport("DbgHelp.dll", SetLastError = true)]
        private static extern Boolean MiniDumpWriteDump(
                                    IntPtr hProcess,
                                    Int32 processId,
                                    IntPtr fileHandle,
                                    MiniDumpType dumpType,
                                    ref MinidumpExceptionInfo excepInfo,
                                    IntPtr userInfo,
                                    IntPtr extInfo);

        /*
         *  MINIDUMP_EXCEPTION_INFORMATION
         */
        struct MinidumpExceptionInfo
        {
            public Int32 ThreadId;
            public IntPtr ExceptionPointers;
            public Boolean ClientPointers;
        }

        public enum MiniDumpType
        {
            None = 0x00010000,
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            FilterMemory = 0x00000008,
            ScanMemory = 0x00000010,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            FilterModulePaths = 0x00000080,
            WithProcessThreadData = 0x00000100,
            WithPrivateReadWriteMemory = 0x00000200,
            WithoutOptionalData = 0x00000400,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00001000,
            WithCodeSegs = 0x00002000
        }

        private const string g_kstrRegister_Key_SPERoot = "Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer";
        private const string g_kstrRegister_ItemKey_InstallDir = "InstallDir";
        private const string g_kstrRegister_ItemKey_NeedRecordDumpFile = "NeedRecordDumpFile";

        private static readonly string m_strDumpDir = "";
        private static readonly bool m_bNeedRecordDumpFile = false;
        static MiniDump()
        {
            m_strDumpDir = "";
            m_bNeedRecordDumpFile = false;

            string ReglogInstallDir_str = null;
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey(g_kstrRegister_Key_SPERoot, false);
                if (null == Software_key)
                {
                    NLLogger.OutputLog(LogLevel.Info, "Cannot open SPE register key:[{0}]", new object[] { g_kstrRegister_Key_SPERoot });
                }
                else
                {
                    object ReglogInstallDir = Software_key.GetValue(g_kstrRegister_ItemKey_InstallDir);
                    if (null == ReglogInstallDir)
                    {
                        m_strDumpDir = "";
                    }
                    else
                    {
                        ReglogInstallDir_str = Convert.ToString(ReglogInstallDir);
                        string configdir = "Logs\\DumpFiles\\";
                        if (ReglogInstallDir_str.EndsWith("\\"))
                        {
                            m_strDumpDir = ReglogInstallDir_str + configdir;
                        }
                        else
                        {
                            m_strDumpDir = ReglogInstallDir_str + "\\" + configdir;
                        }
                    }

                    object obRegValue_NeedRecordDumpFile = Software_key.GetValue(g_kstrRegister_ItemKey_NeedRecordDumpFile);
                    if (null == obRegValue_NeedRecordDumpFile)
                    {
                        // In release build this flag is false by default
                        m_bNeedRecordDumpFile = true;
                    }
                    else
                    {
                        string strNeedRecordDumpFile = Convert.ToString(obRegValue_NeedRecordDumpFile);
                        if (String.Equals("true", strNeedRecordDumpFile) || String.Equals("1", strNeedRecordDumpFile))
                        {
                            m_bNeedRecordDumpFile = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SPELogger ctor:", null, ex);
            }
            NLLogger.OutputLog(LogLevel.Info, "Dump record flag:[{0}], folder:[{1}]", new object[] { m_bNeedRecordDumpFile, m_strDumpDir });
        }

        //try to create dump file
        public static Boolean TryDump(MiniDumpType dmpType)
        {
            Boolean bRet = false;
            try
            {
                Process process = Process.GetCurrentProcess();
                if (m_bNeedRecordDumpFile && (!string.IsNullOrEmpty(m_strDumpDir)))
                {
                    CommonTools.TrimSpecifyTopFolderFiles(m_strDumpDir, 5, "*.dmp");

                    string fileName = GetProcessDumpFileName(process, ".dmp");
                    string dmpPath = m_strDumpDir + @"\" + fileName;
                    if (!string.IsNullOrEmpty(m_strDumpDir) && !Directory.Exists(m_strDumpDir))
                    {
                        Directory.CreateDirectory(m_strDumpDir);
                    }
                    NLLogger.OutputLog(LogLevel.Debug, "dmpPath:" + dmpPath, null);

                    using (FileStream stream = new FileStream(dmpPath, FileMode.Create))
                    {
                        // MINIDUMP_EXCEPTION_INFORMATION init
                        MinidumpExceptionInfo mei = new MinidumpExceptionInfo();
                        mei.ThreadId = Thread.CurrentThread.ManagedThreadId;
                        mei.ExceptionPointers = Marshal.GetExceptionPointers();
                        var code = Marshal.GetExceptionCode();
                        //https://docs.microsoft.com/en-us/archive/blogs/dondu/writing-minidumps-in-c , set ClientPointers to false,confusable variable
                        mei.ClientPointers = false;

                        //call Win32 API
                        bRet = MiniDumpWriteDump(
                                            process.Handle,
                                            process.Id,
                                            stream.SafeFileHandle.DangerousGetHandle(),
                                            dmpType,
                                            ref mei,
                                            IntPtr.Zero,
                                            IntPtr.Zero);
                        if (!bRet)
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            NLLogger.OutputLog(LogLevel.Debug, "MiniDumpWriteDump error Code:{0}", new object[] { errorCode });
                        }
                        stream.Flush();
                        stream.Close();
                    }
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Crash happened in process:[{0}:{1}], but no need record dump by dump flag:[{2}], DumpFolder:[{3}]", new object[] { process.Id, process.ProcessName, m_bNeedRecordDumpFile, m_strDumpDir });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during TryDump:", null, ex);
                bRet = false;
            }
            return bRet;
        }

        private static string GetProcessDumpFileName(Process obProcess, string strExtensionWithStartPoint)
        {
            return obProcess.ProcessName + "-" + obProcess.Id + "-" + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + strExtensionWithStartPoint;
        }
        // Only trim top folder files, do not care sub folders
    }
}
