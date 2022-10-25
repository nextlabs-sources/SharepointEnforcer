using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.CSCInvoke;
using Microsoft.SharePoint.Utilities;
using System.Web;
using System.Threading;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class LogObligation : IObligation
    {
        private SPWeb m_Web;
        private String m_DocLibUrl = null;
        private String m_Url = null;
        private String m_Location = null;
        private String m_UserName = null;
        private String m_FileName = null;
        public SPWeb Web
        {
            get { return m_Web; }
            set { m_Web = value; }
        }
        public String DocLibUrl
        {
            get { return m_DocLibUrl; }
            set { m_DocLibUrl = value; }
        }
        public String Url
        {
            get { return m_Url; }
            set { m_Url = value; }
        }
        public String Location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }
        public String UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; }
        }
        public String FileName
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        public LogObligation()
        {
            m_Web = null;
        }

        private static void ThreadProc(object state)
        {
            TaskInfo ti = state as TaskInfo;
            CETYPE.CEResult_t call_result;
            call_result = CESDKAPI.CELOGGING_LogObligationData(ti.m_handle, ti.m_logIdentifier, ti.m_assistantName, ref ti.m_attr_value);

        }

        public void Process(List<Obligation> obligations, IntPtr hConnect)
        {
            String _DocLibUrl = null;
            String _Url = null;
            String _Location = null;
            foreach(Obligation obligation in obligations)
            {
                if (obligation.Name.Equals("SPLOGACTIVITY", StringComparison.OrdinalIgnoreCase)
                    || obligation.Name.Equals("SPLOGCONTROL", StringComparison.OrdinalIgnoreCase))
                {
                    string message = obligation.Name + ": ";
                    List<string> attrs = new List<string>();
                    string logIdentifier = "";
                    Dictionary<string, string> dicAttrs = obligation.Attributes;
                    foreach (KeyValuePair<string, string> attrpair in dicAttrs)
                    {
                        if (attrpair.Key.Equals("LogId", StringComparison.OrdinalIgnoreCase))
                        {
                            logIdentifier = attrpair.Value;
                            message += ("LogId=" + logIdentifier + ";");
                        }
                        else
                        {
                            attrs.Add(attrpair.Key);
                            message += attrpair.Key;
                            if (String.IsNullOrEmpty(attrpair.Value))
                            {
                                attrs.Add("N/A");
                                message += "N/A;";
                            }
                            else
                            {
                                attrs.Add(attrpair.Value);
                                message += (attrpair.Value + ";");
                            }
                        }
                    }

                    if (m_Web != null)
                    {
                        attrs.Add("Site");
                        attrs.Add(Globals.UrlToResSig(m_Web.Url).ToLower());
                        message += ("Site" + m_Web.Url);
                    }

                    string[] attr_value = attrs.ToArray();
                    TaskInfo ti = new TaskInfo(hConnect, logIdentifier, "SharePoint Enforcer", attr_value);
                    System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), ti);
                    NLLogger.OutputLog(LogLevel.Debug, message, null);
                }
                else if (obligation.Name.Equals("SPSENDEMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    String _Recipients = null;
                    String _Title = null;
                    String _Body = null;
                    Dictionary<string, string> dicAttrs = obligation.Attributes;
                    foreach (KeyValuePair<string, string> attrpair in dicAttrs)
                    {
                        if (attrpair.Key.Equals("Recipient", StringComparison.OrdinalIgnoreCase))
                            _Recipients = attrpair.Value;
                        else if (attrpair.Key.Equals("Title", StringComparison.OrdinalIgnoreCase))
                            _Title = attrpair.Value;
                        else if (attrpair.Key.Equals("Body", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                _Body = attrpair.Value;
                            }
                            catch
                            {
                                _Body = " ";
                            }
                        }
                    }
                    if (_Body == null)
                        _Body = " ";
                    String _datatime = DateTime.Now.ToString();
                    _DocLibUrl = "<a href=\"" + m_DocLibUrl + "\">" + m_DocLibUrl + "</a>";
                    _Url = "<a href=\"" + m_Url + "\">" + m_Url + "</a>";
                    _Location = "<a href=\"" + m_Location + "\">" + m_Location + "</a>";
                    _Body = _Body.Replace("{doclib}", _DocLibUrl);
                    _Body = _Body.Replace("{url}", _Url);
                    _Body = _Body.Replace("{location}", _Location);
                    _Body = _Body.Replace("{datetime}",_datatime);
                    _Body = _Body.Replace("{username}",m_UserName);
                    _Body = _Body.Replace("{filename}",m_FileName);
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        bool _call = SPUtility.SendEmail(m_Web, true, false, _Recipients, _Title, _Body);
                        NLLogger.OutputLog(LogLevel.Debug, "SPUtility.SendEmail to _Recipients result:" + _call, null);
                    });
                }
            }
        }
    }
}
