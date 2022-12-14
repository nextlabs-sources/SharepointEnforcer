using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using log4net;
using log4net.Core;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NextLabs.Diagnostic
{
    public enum LogLevel
    {
        Fatal = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Debug = 4,
        Unused = 5
    }

    public class NLLogger
    {
        #region Sigeton
        static private object s_obLockForInstance = new object();
        static private NLLogger s_obSPELoggerIns = null;
        static public NLLogger GetInstance()
        {
            if (null == s_obSPELoggerIns)
            {
                lock (s_obLockForInstance)
                {
                    if (null == s_obSPELoggerIns)
                    {
                        s_obSPELoggerIns = new NLLogger();
                    }
                }
            }
            return s_obSPELoggerIns;
        }
        private NLLogger()
        {
            try
            {
                string strInstallerDir = CommonTools.GetRegStringValue("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer", "InstallDir", "");
                if (String.IsNullOrEmpty(strInstallerDir))
				{
                    // Failed
                    XmlConfigurator.Configure();
                }
                else
				{
                    CommonTools.MakeStandardFolderPath(ref strInstallerDir);
            		
                    
                    m_strLogConfigFilePath = strInstallerDir + "Config\\Log.Config";
                    m_strLogStandardDir = strInstallerDir + "Logs\\";

                    // Init log config file and no need care exceptions
					try
					{
						FileInfo obLogConfigFileInfo = new FileInfo(m_strLogConfigFilePath);
						XmlConfigurator.ConfigureAndWatch(obLogConfigFileInfo);
					}
					catch (UnauthorizedAccessException ex)
					{
						MyOutputDebugString("Exception during SPELogger ctor:{0}", ex.Message + ex.StackTrace);
					}

                    m_obl4nlog = LogManager.GetLogger("SPELogger");
                    InitLogInfo(m_obl4nlog, "SharePointCELogAppender", m_strLogStandardDir);
                    
                    Task obTaskForCleanLogFiles = new Task(ThreadCleanLogFiles);
					obTaskForCleanLogFiles.Start();

                    ForceTrimLogFiles();
                }            
            }
            catch (Exception ex)
            {
                MyOutputDebugString("Exception during SPELogger ctor:{0}", ex.Message + ex.StackTrace);
            }
        }
        #endregion

        #region Public log methods       
        public static void OutputLog(LogLevel emLevel, string strFormat, object[] szArgs = null, Exception exInfo = null, bool bOutputStackTrace = true, bool bOutputInnerExceptionInfo = true, [CallerFilePath] string strCallerFileName = null, [CallerLineNumber] int nCallerFileNumber = 0, [CallerMemberName] string strCallerName = null)
        {
            NLLogger obSPELoggerIns = GetInstance();
            try
			{
				if (obSPELoggerIns.IsLogLevelSupport(emLevel))
				{
					string strLogerInfo = obSPELoggerIns.EstablishLogInfo(strFormat, szArgs, exInfo, bOutputStackTrace, bOutputInnerExceptionInfo, strCallerFileName, nCallerFileNumber, strCallerName);

                    obSPELoggerIns.InnerLog(emLevel, strLogerInfo);
				}
				else
				{
					// ignore
				}
			}
            catch (Exception ex)
			{
                obSPELoggerIns.InnerLog(LogLevel.Fatal, String.Format("Output log exception in:[{0},{1},{2}], info:[{3}], {4}]\n", new object[] { strCallerFileName, nCallerFileNumber, strCallerName, ex.Message, ex.StackTrace }));
            }
		}
        #endregion

        #region Inner log methods
        private bool IsLogLevelSupport(LogLevel emLevel)
		{
            bool bRet = false;
            if (null == m_obl4nlog)
            {
                // do not support
            }
            else
			{
				switch (emLevel)
				{
				case LogLevel.Fatal:
                {
                    bRet = m_obl4nlog.IsFatalEnabled;
                    break;
                }
				case LogLevel.Error:
                {
                    bRet = m_obl4nlog.IsErrorEnabled;
                    break;
                }
				case LogLevel.Warn:
                {
                    bRet = m_obl4nlog.IsWarnEnabled;
                    break;
                }
				case LogLevel.Info:
                {
                    bRet = m_obl4nlog.IsInfoEnabled;
                    break;
                }
				case LogLevel.Debug:
                {
                    bRet = m_obl4nlog.IsDebugEnabled;
                    break;
                }
				default:
                {
                    break;
                }
				}
			}
            return bRet;
		}
        private void InnerLog(LogLevel emLevel, string strMsg)
        {
            try
			{
				if (null == m_obl4nlog)
				{
					// ignore
				}
				else
				{
					switch (emLevel)
					{
					case LogLevel.Fatal:
					{
						m_obl4nlog.Fatal(strMsg);
						break;
					}
					case LogLevel.Error:
					{
						m_obl4nlog.Error(strMsg);
						break;
					}
					case LogLevel.Warn:
					{
						m_obl4nlog.Warn(strMsg);
						break;
					}
					case LogLevel.Info:
					{
						m_obl4nlog.Info(strMsg);
						break;
					}
					case LogLevel.Debug:
					{
						m_obl4nlog.Debug(strMsg);
						break;
					}
					default:
					{
						break;
					}
					}
				}
			}
            catch (Exception ex)
			{
                MyOutputDebugString("Inner log exception:[{0}]", ex.Message);
            }
        }
		private string EstablishLogInfo(string strFormat, object[] szArgs, Exception exInfo, bool bOutputStackTrace, bool bOutputInnerExceptionInfo, string strCallerFileName, int nCallerFileNumber, string strCallerName)
		{
			string strCallerInfo = "";
			if (String.IsNullOrEmpty(strCallerFileName))
			{
				// Stack frame: index 0, current
				// Stack frame: index 1, caller
				StackFrame obCallerStackFrame = GetCallerStackFrame(new StackTrace(true), 2);
				strCallerInfo = GetCallerInfo(obCallerStackFrame, "[", "] ");
			}
			else
			{
				strCallerInfo = String.Format("[{0}:{1}:{2}] ", Path.GetFileName(strCallerFileName), nCallerFileNumber, strCallerName);
			}

			string strMessageInfo = "";
			if (null == szArgs)
			{
				strMessageInfo = (null == strFormat) ? "" : strFormat;
			}
			else
			{
				strMessageInfo = String.Format(strFormat, szArgs);
			}

			string strExceptionInfo = "";
			if (null == exInfo)
			{
				// Empty
			}
			else
			{
				if (bOutputStackTrace)
				{
					strExceptionInfo = String.Format("\nExceptionMessage:[{0}]\n\tStackTrace:[{1}]\n", exInfo.Message, exInfo.StackTrace);
				}
				else
				{
					strExceptionInfo = String.Format("\nExceptionMessage:[{0}]\n", exInfo.Message);
				}

				if (bOutputInnerExceptionInfo)
				{
					if (null == exInfo.InnerException)
					{
						// Empty
					}
					else
					{
						if (bOutputStackTrace)
						{
							strExceptionInfo += String.Format("InnerExceptionMessage:[{0}]\n\tInnerExceptionStackTrace:[{1}]\n", exInfo.InnerException.Message, exInfo.InnerException.StackTrace);
						}
						else
						{
							strExceptionInfo += String.Format("InnerExceptionMessage:[{0}]\n", exInfo.InnerException.Message);
						}
					}
				}
			}

			return strCallerInfo + strMessageInfo + strExceptionInfo;
		}

		private void ForceTrimLogFiles()
		{
			m_obEventForCleanLogFiles.Set();
		}
		private void ThreadCleanLogFiles()
		{
            const int knCleanIntervalMs = 12 * 60 * 60 * 1000;
			const int knMaxLogFiles = 10;
			const string kstrLogFilePatten = "*.log*";  // *.log, *.log.1

			NLLogger obSPELoggerIns = NLLogger.GetInstance();
            NLLogger.OutputLog(LogLevel.Info, "The log file clean thread start");
            bool bContinue = true;
           
            do
            {
                try
				{
                    NLLogger.OutputLog(LogLevel.Info, "Begin wait clean log file event, timeout setting:[{0}]", new object[] { knCleanIntervalMs });
                    bool bWaitRet = obSPELoggerIns.m_obEventForCleanLogFiles.WaitOne(knCleanIntervalMs);
                    NLLogger.OutputLog(LogLevel.Info, "End wait clean log file event with result:[{0}] and begin to do clean", new object[]{ bWaitRet ? "Singled" : "Timeout" });

                    CommonTools.TrimSpecifyTopFolderFiles(obSPELoggerIns.m_strLogStandardDir, knMaxLogFiles, kstrLogFilePatten);
                    NLLogger.OutputLog(LogLevel.Info, "End clean log folder:[{0}] with max files:[{1}] in patten:[{2}]", new object[] { obSPELoggerIns.m_strLogStandardDir, knMaxLogFiles, kstrLogFilePatten });

                    bContinue = true;
				}
                catch (Exception)
				{
                    // Exception, exit
                    bContinue = false;
				}
            } while (bContinue);
            NLLogger.OutputLog(LogLevel.Info, "The log file clean thread stop");
        }
        #endregion

        #region Inner independence tools
        private static void InitLogInfo(ILog iLog, string strAppenderName, string strStandardLogFolderPath)
        {
            try
            {
                bool bFind = false;
                AppenderCollection ac = ((Logger)iLog.Logger).Appenders;
                RollingFileAppender rfa = null;
                for (int i = 0; i < ac.Count; i++)
                {
                    rfa = ac[i] as RollingFileAppender;

                    if (rfa != null && rfa.Name.Equals(strAppenderName))
                    {
                        bFind = true;
                        break;
                    }
                }

                if (bFind && !string.IsNullOrEmpty(strStandardLogFolderPath))
                {                   
                    Process obCurProcess = Process.GetCurrentProcess();
                    rfa.File = strStandardLogFolderPath + "SharepointEnforcer_" + obCurProcess.ProcessName + "_" + obCurProcess.Id.ToString() + ".log";
                    rfa.ActivateOptions();
                }
            }
            catch (Exception ex)
            {
                MyOutputDebugString("Exception during SetCELogPath:{0}", ex.Message + ex.StackTrace);
            }
        }
        private static string GetCallerInfo(StackFrame obCallerStackFrame, string strPrefix, string strPostfix)
        {
            string strCallerInfoRet = "";
            if (null == obCallerStackFrame)
            {
                strCallerInfoRet = "";
            }
            else
            {
                string strFileFullName = obCallerStackFrame.GetFileName();
                if (String.IsNullOrEmpty(strFileFullName))
                {
                    // Crash module invoke, the file info will be empty
                    strCallerInfoRet = obCallerStackFrame.GetMethod().Name;
                }
                else
                {
                    string strFileName = Path.GetFileName(strFileFullName);
                    strCallerInfoRet = String.Format("{0}{1}:{2}:{3}{4}", strPrefix, strFileName, obCallerStackFrame.GetFileLineNumber(), obCallerStackFrame.GetMethod().Name, strPostfix);
                }
            }
            return strCallerInfoRet;
        }
		private static StackFrame GetCallerStackFrame(StackTrace obStackTrace, int nCallerIndex)
		{
			StackFrame obStackFrameRet = null;
			try
			{
                if ((null != obStackFrameRet) && (0 <= nCallerIndex))
				{
					if (nCallerIndex < obStackTrace.FrameCount)
					{
						obStackFrameRet = obStackTrace.GetFrame(nCallerIndex);
					}
					else
					{
						obStackFrameRet = null;
					}
				}
                else
				{
                    // Parameters error
                    MyOutputDebugString("Parameters error when we try to get caller stack frame, coding error, please check\n");
                }
			}
			catch (Exception ex)
			{
                MyOutputDebugString("Exception during GetCallerStackFrame:{0}, {1}\n", ex.Message, ex.StackTrace);
			}
			return obStackFrameRet;
		}
		private static void MyOutputDebugString(string message)
		{
            MyOutputDebugString("{0}", message);
		}
		private static void MyOutputDebugString(string format, params string[] szArgs)
		{
			try
			{
				Trace.TraceInformation(format, szArgs);
			}
			catch (Exception)
			{
			}
		}
        #endregion

        #region Members
        private readonly string m_strLogStandardDir = "";
        private readonly string m_strLogConfigFilePath = "";
        private AutoResetEvent m_obEventForCleanLogFiles = new AutoResetEvent(false);
		private ILog m_obl4nlog = null;
        #endregion    
    }
}
