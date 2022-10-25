using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Data;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public class CALogViewerPage : LayoutsPageBase
    {
        protected SPGridView SPGridView1;

        public CALogViewerPage()
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            SPSite siteColl = SPContext.Current.Site;
            SPWeb site = SPContext.Current.Web;
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                SPBoundField boundField = new SPBoundField();
                boundField.HeaderText = "Time";
                boundField.DataField = "Time";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "User";
                boundField.DataField = "User";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "Item Name";
                boundField.DataField = "Item Name";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "File Url";
                boundField.DataField = "File Url";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "Result";
                boundField.DataField = "Result";
                SPGridView1.Columns.Add(boundField);

                SPGridView1.AllowSorting = true;
                SPGridView1.HeaderStyle.Font.Bold = true;

                String ListGuid = Request.QueryString["List"];
                SPList List = site.Lists[new Guid(ListGuid)];

                if (List != null)
                {
                    if (!List.DoesUserHavePermissions(SPBasePermissions.ManageLists))
                    {
                        String _url = "http://" + Request.Params["HTTP_HOST"] + Request.RawUrl;
                        SPUtility.Redirect("AccessDenied.aspx" + "?Source=" + _url, SPRedirectFlags.RelativeToLayoutsPage, this.Context);
                        return;
                    }

                    SPAuditQuery wssQuery = new SPAuditQuery(siteColl);
                    wssQuery.AddEventRestriction(SPAuditEventType.Custom);
                    wssQuery.RestrictToList(List);
                    SPAuditEntryCollection auditCol = null;

                    try
                    {
                        auditCol = List.Audit.GetEntries(wssQuery);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        String _url = "http://" + Request.Params["HTTP_HOST"] + Request.RawUrl;
                        SPUtility.Redirect("AccessDenied.aspx" + "?Source=" + _url, SPRedirectFlags.RelativeToLayoutsPage, this.Context);
                        return;
                    }

                    using (DataTable table = new DataTable())
                    {
                        table.Columns.Add("Time", typeof(DateTime));
                        table.Columns.Add("User", typeof(string));
                        table.Columns.Add("Item Name", typeof(string));
                        table.Columns.Add("File Url", typeof(string));
                        table.Columns.Add("Result", typeof(string));

                        DataRow newRow;

                        foreach (SPAuditEntry entry in auditCol)
                        {
                            if (entry.SourceName.Equals("ContentAnalysis"))
                            {
                                string eventData = entry.EventData;

                                try
                                {
                                    XmlDocument xmlDoc = new XmlDocument();
                                    xmlDoc.InnerXml = eventData;
                                    XmlNode node = xmlDoc.DocumentElement;

                                    //string fileUrl = Utilities.ParseFromXmlString(node["FileUrl"].InnerText);
                                    string result = Utilities.ParseFromXmlString(node["Modified"].InnerText + node["Failed"].InnerText).ToString();
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        continue;
                                    }
                                    newRow = table.Rows.Add();
                                    String _user = Utilities.ParseFromXmlString(node["User"].InnerText); //GetUserNameById(entry.UserId, site);
                                    _user = Utilities.ClaimUserConvertion(_user);
                                    newRow["User"] = _user;
                                    newRow["Time"] = entry.Occurred.ToLocalTime();
                                    newRow["Item Name"] = Utilities.ParseFromXmlString(node["ItemName"].InnerText);
                                    newRow["File Url"] = Utilities.ParseFromXmlString(node["FileUrl"].InnerText);
                                    newRow["Result"] = Utilities.ParseFromXmlString(node["Modified"].InnerText + node["Failed"].InnerText);
                                }
                                catch (Exception ex)
                                {
                                    NLLogger.OutputLog(LogLevel.Error, "Exception during CALogViewerPage OnLoad:", null, ex);
                                }
                            }
                        }

                        SPGridView1.AutoGenerateColumns = false;
                        SPGridView1.DataSource = table.DefaultView;
                        SPGridView1.DataBind();
                    }
                }
            }
        }

        string GetUserNameById(int UserId, SPWeb site)
        {
            try
            {
                return site.SiteUsers.GetByID(UserId).LoginName;
            }
            catch
            {
                return UserId.ToString();
            }
        }


        protected void cmdDeleteAllEntires_Click(object sender, EventArgs e)
        {
            SPSite siteColl = SPContext.Current.Site;
            siteColl.Audit.DeleteEntries(DateTime.Now.ToLocalTime().AddDays(1));
            siteColl.Audit.Update();
            Response.Redirect(Request.RawUrl);
        }

        protected void cmdRefreshPage_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.RawUrl);
        }

        protected void cmdOK_Click(object sender, EventArgs e)
        {
            String ListGuid = Request.QueryString["List"];
            SPWeb web = SPControl.GetContextWeb(this.Context);
            String caUrl = web.Url + "/_layouts/ICSetting.aspx?List=" + ListGuid;

            Response.Redirect(caUrl);
        }
    }
}
