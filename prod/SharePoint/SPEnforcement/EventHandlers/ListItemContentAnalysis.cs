using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using System.Diagnostics;
using System.Security.Principal;
namespace NextLabs.SPEnforcer
{
    public class ListItemContentAnalysis
    {
        private SPListItem m_ListItem;
        private IPrincipal m_PrincipalUser;
        private string m_ClientIP;
        private bool m_Batch;
        private string m_UserName;
        private string m_UserSid;

        private List<ContentAnalysisResult> m_ResultList;
        public List<ContentAnalysisResult> Results
        {
            get { return m_ResultList; }
        }

        private bool m_bCADenied;
        public bool CADenied
        {
            get { return m_bCADenied; }
            set { m_bCADenied = value; }
        }

        public PreContentAnalysisEventDelegate PreContentAnalysisFunc;
        public PostContentAnalysisEventDelegate PostContentAnalysisFunc;

        public ListItemContentAnalysis(SPListItem item, string ip, IPrincipal PrincipalUser, bool bBatch)
        {
            m_ListItem = item;
            m_ClientIP = ip;
            m_ResultList = new List<ContentAnalysisResult>();
            m_Batch = bBatch;
            m_UserName = null;
            m_UserSid = null;
            m_PrincipalUser = PrincipalUser;
            m_bCADenied = false;
        }

        public ListItemContentAnalysis(SPListItem item, string userName, string userSid, string ip, IPrincipal PrincipalUser, bool bBatch)
        {
            m_ListItem = item;
            m_ClientIP = ip;
            m_ResultList = new List<ContentAnalysisResult>();
            m_Batch = bBatch;
            m_UserName = userName;
            m_UserSid = userSid;
            m_PrincipalUser = PrincipalUser;
            m_bCADenied = false;
        }

        public void Run()
        {
            if (m_Batch)
            {
                RunByTimerJob();
            }
            else
            {
                RunByUpload();
            }
        }

        public void RunByUpload()
        {
            SPList list = m_ListItem.ParentList;
            SPWeb web = list.ParentWeb;
             
            if (list.BaseType == SPBaseType.DocumentLibrary)
            {
                string fileUrl = web.Url + "/" + m_ListItem.Url;
                FileContentAnalysis fileCA = new FileContentAnalysis(m_ListItem, fileUrl, m_UserName, m_UserSid, m_ClientIP, m_PrincipalUser, m_Batch);
                if (PreContentAnalysisFunc != null)
                    fileCA.PreContentAnalysisEventHandler += new PreContentAnalysisEventDelegate(PreContentAnalysisFunc);
                if (PostContentAnalysisFunc != null)
                    fileCA.PostContentAnalysisEventHandler += new PostContentAnalysisEventDelegate(PostContentAnalysisFunc);
                fileCA.Run();
                m_ResultList.Add(fileCA.Result);
                m_bCADenied = fileCA.CADenied;
            }
            else
            {
                SPAttachmentCollection attachments = m_ListItem.Attachments;
                foreach(string url in attachments)
                {
                    FileContentAnalysis fileCA = new FileContentAnalysis(m_ListItem, attachments.UrlPrefix + url, m_UserName, m_UserSid, m_ClientIP, m_PrincipalUser, m_Batch);
                    if (PreContentAnalysisFunc != null)
                        fileCA.PreContentAnalysisEventHandler += new PreContentAnalysisEventDelegate(PreContentAnalysisFunc);
                    if (PostContentAnalysisFunc != null)
                        fileCA.PostContentAnalysisEventHandler += new PostContentAnalysisEventDelegate(PostContentAnalysisFunc);
                    fileCA.Run();
                    m_ResultList.Add(fileCA.Result);
                    if (fileCA.CADenied)
                    {
                        m_bCADenied = true;
                        break;
                    }
                }
            }
        }

        public void RunByTimerJob()
        {
            SPList list = m_ListItem.ParentList;
            SPWeb web = list.ParentWeb;

            if (list.BaseType == SPBaseType.DocumentLibrary)
            {
                string _modifytime = (string)m_ListItem["Last_x0020_Modified"];

                string _lastscantime = null;
                if (!string.IsNullOrEmpty(_modifytime))
                {
                    SPFile _file = null;
                    _file = m_ListItem.File;

                    if (_file.Properties.ContainsKey("nextlabs_lastscan"))
                    {
                        _lastscantime = (string)m_ListItem.File.Properties["nextlabs_lastscan"];
                    }
                    else
                    {
                        _lastscantime = "01/01/2001 01:01:01 AM";
                    }

                    if (!_lastscantime.Equals("01/01/2001 01:01:01 AM"))
                    {
                        //Donot make encrypte if file has been moidified.
                        return;
                    }
                }

                string fileUrl = web.Url + "/" + m_ListItem.Url;
                FileContentAnalysis fileCA = new FileContentAnalysis(m_ListItem, fileUrl, m_UserName, m_UserSid, m_ClientIP, m_PrincipalUser, m_Batch);
                if (PreContentAnalysisFunc != null)
                    fileCA.PreContentAnalysisEventHandler += new PreContentAnalysisEventDelegate(PreContentAnalysisFunc);
                if (PostContentAnalysisFunc != null)
                    fileCA.PostContentAnalysisEventHandler += new PostContentAnalysisEventDelegate(PostContentAnalysisFunc);
                fileCA.Run();
                m_ResultList.Add(fileCA.Result);
            }
            else
            {
                SPAttachmentCollection attachments = m_ListItem.Attachments;
                foreach (string url in attachments)
                {
                    SPFile _file = null;
                    _file = m_ListItem.ParentList.ParentWeb.GetFile(attachments.UrlPrefix + url);
                    string _modifytime = _file.Properties["vti_timelastmodified"].ToString();
                    string _lastscantime = null;
                    if (!string.IsNullOrEmpty(_modifytime))
                    {
                        if (_file.Properties.ContainsKey("nextlabs_lastscan"))
                        {
                            _lastscantime = (string)_file.Properties["nextlabs_lastscan"];
                        }
                        else
                        {
                            _lastscantime = "01/01/2001 01:01:01 AM";
                        }

                        if (!_lastscantime.Equals("01/01/2001 01:01:01 AM"))
                        {
                            continue;
                        }
                    }

                    FileContentAnalysis fileCA = new FileContentAnalysis(m_ListItem, attachments.UrlPrefix + url, m_UserName, m_UserSid, m_ClientIP, m_PrincipalUser, m_Batch);
                    if (PreContentAnalysisFunc != null)
                        fileCA.PreContentAnalysisEventHandler += new PreContentAnalysisEventDelegate(PreContentAnalysisFunc);
                    if (PostContentAnalysisFunc != null)
                        fileCA.PostContentAnalysisEventHandler += new PostContentAnalysisEventDelegate(PostContentAnalysisFunc);
                    fileCA.Run();
                    m_ResultList.Add(fileCA.Result);
                }
            }
        }
    }
}
