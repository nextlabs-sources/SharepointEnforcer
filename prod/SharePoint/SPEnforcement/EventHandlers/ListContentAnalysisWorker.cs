using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.SharePoint;
using NextLabs.Common;
using System.Web;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public class ListContentAnalysisWorker
    {
        private string m_WebURL;
        private string m_UserName;
        private string m_UserSid;
        private SPUserToken m_UserToken;
        private string m_ListGuid;
        private SPList m_List;
        private string m_ClientIP;

        private List<ContentAnalysisResult> m_ResultList;

        public List<ContentAnalysisResult> Results
        {
            get { return m_ResultList; }
        }

        public PreContentAnalysisEventDelegate PreContentAnalysisFunc;
        public PostContentAnalysisEventDelegate PostContentAnalysisFunc;

        public ListContentAnalysisWorker(string webUrl, SPUserToken user, string listGuid, string ip)
        {
            m_WebURL = webUrl;
            m_UserToken = user;
            m_ListGuid = listGuid;
            m_List = null;
            m_ClientIP = ip;
            m_ResultList = new List<ContentAnalysisResult>();
        }

        public ListContentAnalysisWorker(string webUrl, string userName, string userSid, string listGuid, string ip)
        {
            m_WebURL = webUrl;
            m_UserName = userName;
            m_UserSid = userSid;
            m_UserToken = null;
            m_ListGuid = listGuid;
            m_List = null;
            m_ClientIP = ip;
            m_ResultList = new List<ContentAnalysisResult>();
        }

        public void Run(object obj)
        {
            using (SPSite site = Globals.GetValidSPSite(m_WebURL, m_UserToken, HttpContext.Current))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    m_List = web.Lists[new Guid(m_ListGuid)];
                    SPListItemCollection items = m_List.GetItems(new SPQuery());
                    AutoResetEvent autoEvent = obj as AutoResetEvent;
                    NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker start with Site=" + m_WebURL + " List=" + m_List.Title);

                    int count = items.Count;
                    if (count < m_List.ItemCount)
                    {
                        count = m_List.ItemCount;
                        items = m_List.Items;
                    }

                    count = 0;
                    bool bExclude = false;
                    foreach (SPListItem item in items)
                    {
                        try
                        {
                            bExclude = false;
                            if (item.Fields.ContainsField("Exclude Content Analysis"))
                            {
                                bExclude = (bool)item["Exclude Content Analysis"];
                            }
                        }
                        catch (Exception)
                        {
                            bExclude = false;
                        }

                        if (!(m_List.BaseType == SPBaseType.DocumentLibrary && item.Folder != null) && !bExclude)
                        {
                            count++;
                        }
                    }

                    if (count == 0)
                    {
                        autoEvent.Set();
                        NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker end with no list item.");
                        return;
                    }

                    Int32 curr_ca_itemcount = 0;
                    try
                    {
                        m_List.RootFolder.Properties["ca_state"] = "In Progress";
                        m_List.RootFolder.Properties["curr_ca_itemcount"] = curr_ca_itemcount.ToString();
                        m_List.RootFolder.Properties["need_ca_itemcount"] = count.ToString();
                        m_List.RootFolder.Update();
                    }
                    catch (Exception ex)
                    {
                        autoEvent.Set();
                        NLLogger.OutputLog(LogLevel.Error, "Exception during ListContentAnalysisWorker:", null, ex);
                        return;
                    }
                    autoEvent.Set();
                    HandleEventFiring eventCtrl = new HandleEventFiring();
                    eventCtrl.CustomDisableEventFiring();

                    foreach (SPListItem item in items)
                    {
                        try
                        {
							bExclude = false;
                            if (item.Fields.ContainsField("Exclude Content Analysis"))
                            {
                                bExclude = (bool)item["Exclude Content Analysis"];
                            }
                        }
                        catch (Exception)
                        {
                            bExclude = false;
                        }
                        // Skip document library folder item and excluded items
                        if ((m_List.BaseType == SPBaseType.DocumentLibrary && item.Folder != null) || bExclude)
                        {
                            continue;
                        }
                        String ca_state = m_List.RootFolder.Properties["ca_state"] as String;
                        if (ca_state.Equals("In Progress"))
                        {
                            ListItemContentAnalysis listItemCA = new ListItemContentAnalysis(item, m_UserName, m_UserSid, m_ClientIP, null, true);
                            listItemCA.PreContentAnalysisFunc = PreContentAnalysisEventHandler;
                            listItemCA.PostContentAnalysisFunc = PostContentAnalysisEventHandler;
                            listItemCA.Run();
                            curr_ca_itemcount++;

                            m_List.RootFolder.Properties["curr_ca_itemcount"] = curr_ca_itemcount.ToString();
                            m_List.RootFolder.Update();
                            m_ResultList.AddRange(listItemCA.Results);

                            NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker: Processed " + curr_ca_itemcount.ToString() + " of " + m_List.ItemCount.ToString() + " documents.");
                        }
                        else
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker: Stopped by user.");
                            break;
                        }
                    }
                    eventCtrl.CustomEnableEventFiring();

                    m_List.RootFolder.Properties["ca_state"] = "Idle";
                    m_List.RootFolder.Properties["curr_ca_itemcount"] = "0";
                    m_List.RootFolder.Properties["need_ca_itemcount"] = "0";
                    m_List.RootFolder.Properties["curr_ca_fileurl"] = "";
                    m_List.RootFolder.Update();

                    NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker: end...");
                }
            }
        }

        protected void PreContentAnalysisEventHandler(object sender, EventArgs e)
        {
            FileContentAnalysis fileCA = sender as FileContentAnalysis;

            NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker: Processing " + fileCA.Result.FileUrl);

            m_List.RootFolder.Properties["curr_ca_fileurl"] = fileCA.Result.FileUrl;
            m_List.RootFolder.Update();
        }

        protected void PostContentAnalysisEventHandler(object sender, EventArgs e)
        {
            FileContentAnalysis fileCA = sender as FileContentAnalysis;
            if (fileCA.Result.FailedReason.Count != 0 || fileCA.Result.ModifiedFields.Count != 0)
            {
                string xmlData = "<CAResult>";
                xmlData += "<User>";
                xmlData += Utilities.ConvertToXmlString(m_List.ParentWeb.CurrentUser.LoginName);
                xmlData += "</User>";
                xmlData += "<ItemName>";
                if (String.IsNullOrEmpty(fileCA.Result.ListItem.Name))
                    xmlData += Utilities.ConvertToXmlString(fileCA.Result.ListItem.Title);
                else
                    xmlData += Utilities.ConvertToXmlString(fileCA.Result.ListItem.Name);
                xmlData += "</ItemName>";
                xmlData += "<FileUrl>";
                xmlData += Utilities.ConvertToXmlString(fileCA.Result.FileUrl);
                xmlData += "</FileUrl>";

                xmlData += "<Modified>";
                if (fileCA.Result.ModifiedFields.Count > 0)
                {
                    xmlData += "Succeeded to set ";
                    int count = 0;
                    foreach (KeyValuePair<string, string> keyValue in fileCA.Result.ModifiedFields)
                    {
                        xmlData += Utilities.ConvertToXmlString(keyValue.Key);
                        xmlData += "=";
                        xmlData += Utilities.ConvertToXmlString(keyValue.Value);
                        count++;
                        if (count != fileCA.Result.ModifiedFields.Count)
                            xmlData += ",";
                    }
                    xmlData += ". ";
                }
                xmlData += "</Modified>";

                xmlData += "<Failed>";
                if (fileCA.Result.FailedReason.Count > 0)
                {
                    foreach (string failed in fileCA.Result.FailedReason)
                    {
                        Int32 count = 1;
                        xmlData += count.ToString() + ". ";
                        xmlData += Utilities.ConvertToXmlString(failed);
                        xmlData += " ";
                        count++;
                    }
                }
                xmlData += "</Failed>";
                xmlData += "</CAResult>";

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    m_List.Audit.WriteAuditEvent(SPAuditEventType.Custom, "ContentAnalysis", xmlData);
                });

                NLLogger.OutputLog(LogLevel.Debug, "ListContentAnalysisWorker: Audit XML Data=" + xmlData);
            }
        }
    }
}
