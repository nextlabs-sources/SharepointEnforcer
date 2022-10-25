using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.SharePoint.WebControls;
namespace NextLabs.Deployment
{
    public class nlprogress : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
    public interface IProgressLog
    {
        bool CreateLog(string WebName);
        void WriteLog(string WebName, string message);
        string GetLastLog(string WebName);
        string GetLastLine(string WebName);
        List<string> GetAllLog(string webName);
    }


    public class ProgressLogInFile : IProgressLog
    {
        static CommonLib utility = new CommonLib();
        string BasePath = utility.GetSPEIntalledPath() + @"Logs\";
        static FileStream logFileStream;
        static StreamWriter streamWriter;

        public bool CreateLog(string WebName)
        {
            bool result = false;
            string LogPath = Path.Combine(BasePath, WebName + ".log");
            if (File.Exists(LogPath))
            {
                File.Move(LogPath, Path.Combine(BasePath, WebName + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".log"));
            }
            FileStream LogFile = File.Create(LogPath);
            if (LogFile != null)
            {
                LogFile.Close();
                result = true;
            }
            return result;
        }

        public List<string> GetAllLog(string webName)
        {
            List<string> allLog = new List<string>();
            string LogPath = Path.Combine(BasePath, webName + ".log");
            if (!File.Exists(LogPath))
            {
                return allLog;
            }
            allLog = File.ReadAllLines(LogPath, Encoding.Unicode).ToList<string>();
            return allLog;
        }
        public void WriteLog(string WebName, string message)
        {
            string LogPath = Path.Combine(BasePath, WebName + ".log");
            using (logFileStream = new FileStream(LogPath, FileMode.Append, FileAccess.Write))
            {
                lock (logFileStream)
                {
                    using (streamWriter = new StreamWriter(logFileStream, Encoding.Unicode))
                    {
                        streamWriter.WriteLine(message);
                        streamWriter.Close();
                        logFileStream.Close();
                    }
                }
            }
        }

        public string GetLastLog(string WebName)
        {
            string LogPath = Path.Combine(BasePath, WebName + ".log");
            string result = string.Empty;
            if (!File.Exists(LogPath))
            {
                return result;
            }
            string[] ArryLines = File.ReadAllLines(LogPath, Encoding.Unicode);
            if (ArryLines.Length > 0)
            {
                for (int i = ArryLines.Length - 1; i > 0; i--)
                {
                    if (ArryLines[i].Split('|').Length == 2)
                    {
                        result = ArryLines[i];
                        break;
                    }
                }
            }
            return result;
        }
        public string GetLastLine(string WebName)
        {
            string LogPath = Path.Combine(BasePath, WebName + ".log");
            string result = string.Empty;
            if (!File.Exists(LogPath))
            {
                return result;
            }
            string[] ArryLines = File.ReadAllLines(LogPath, Encoding.Unicode);
            if (ArryLines.Length > 0)
            {
                result = ArryLines[ArryLines.Length - 1];
            }
            return result;
        }
    }

    public static class Progress
    {
        static IProgressLog ProgressLog = new ProgressLogInFile();

        public static bool CreateLog(string WebName)
        {
            return ProgressLog.CreateLog(WebName);
        }

        public static void WriteLog(string WebName, string message)
        {
            ProgressLog.WriteLog(WebName, message);
        }
        public static string GetLastLog(string WebName)
        {
            return ProgressLog.GetLastLog(WebName);
            //return "";
        }
        public static string GetLastLine(string WebName)
        {
            return ProgressLog.GetLastLine(WebName);
        }
        public static List<string> GetAllLog(string WebName)
        {
            return ProgressLog.GetAllLog(WebName);
            // return new List<string>();
        }
    }
}
