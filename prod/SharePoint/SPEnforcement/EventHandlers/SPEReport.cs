using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public class SPEReport
    {
        private string reportFilePath;
        private SPWeb web;
        private StreamWriter fileWriter;
        private string strRecipients;


        public SPEReport(SPWeb _web)
        {
            web = _web;
        }

        public void OpenOrCreateReport()
        {
            try
            {
                reportFilePath = GetReportFilePath(web);
                fileWriter = new StreamWriter(reportFilePath, true, Encoding.Default);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during OpenOrCreateReport:", null, ex);
            }
        }

        public void InitReportHeader(string strClassName, string strClassValues, string strCompWith, bool bSetParentSite, string _strRecipients)
        {
            try
            {
                fileWriter.WriteLine("");
                fileWriter.WriteLine("Start Time: " + DateTime.Now.ToString());
                fileWriter.WriteLine("Classification Name: " + strClassName);
                fileWriter.WriteLine("Classification Values: " + strClassValues);
                fileWriter.WriteLine("Compare With: " + strCompWith);
                fileWriter.WriteLine("Set Parent Site: " + (bSetParentSite ? "Yes": "No"));
                fileWriter.WriteLine("");
                strRecipients = _strRecipients;
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during InitReportHeader:", null, ex);
            }
        }

        private string GetReportFilePath(SPWeb web)
        {
            string _filepath = Globals.GetSPEPath();
            _filepath += "Logs\\Reports\\";
            if (!Directory.Exists(_filepath))
            {
                Directory.CreateDirectory(_filepath);
            }
            string webUrl = web.Url;
            int pos = webUrl.IndexOf("//");
            string tail = "";
            if (pos != -1)
            {
                tail = webUrl.Substring(pos + 2);
            }
            string fileName = _filepath + tail.Replace('/', '.').Replace(':', '_') + "." + DateTime.Now.ToString("yyyyMMddHHmm");
            while(File.Exists(fileName + ".csv"))
            {
                fileName += "_2";
            }
            return fileName+".csv";
        }

        public void WriteToCSVFile(List<string> reporter)
        {
            fileWriter.WriteLine(String.Join(",", reporter.ToArray()));
        }

        public void FinishReport(string strSub, string strBody)
        {
            try
            {
                fileWriter.Flush();
                fileWriter.Close();
                Globals.SPESendEmail(web, strRecipients, strSub, strBody, reportFilePath);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FinishReport:", null, ex);
            }
        }
    }
}
