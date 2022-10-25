using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Data;
using System.Threading;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Portal.WebControls;
using Nextlabs.SPSecurityTrimming;
using Microsoft.SharePoint.Administration;
using NextLabs.Common;
using System.Diagnostics;
namespace NextLabs.SPEnforcer
{
    public class ContentAnalysisPage : LayoutsPageBase
    {
        protected EncodedLiteral DescriptionLiteral;
        protected EncodedLiteral DescriptionLiteral2;
        protected Microsoft.SharePoint.WebControls.InputFormCheckBox CAStatusCheckBox;
        protected Microsoft.SharePoint.WebControls.InputFormCheckBox ClearLastScanTimeCheckBox;
        protected Microsoft.SharePoint.WebControls.InputFormCheckBox ProcessUploadCheckBox;
        protected EncodedLiteral CAStatus;
        protected HyperLink LogLink;
        protected InputFormHyperLink manageScheduleLink;
        protected InputFormDropDownList scheduleFullDropDown;

        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc AllXmlSchedules;
        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc SelectedIndex;

        //List Trimming
        protected CheckBox EnableListTrimming;
        protected CheckBox EnableListPrefilterTrimming;
        protected SPList List;

        public ContentAnalysisPage()
        {
            DescriptionLiteral = new EncodedLiteral();
            DescriptionLiteral2 = new EncodedLiteral();
            CAStatus = new EncodedLiteral();
            LogLink = new HyperLink();
            manageScheduleLink = new InputFormHyperLink();
            scheduleFullDropDown = new InputFormDropDownList();
            AllXmlSchedules = new TextBoxLoc();
            SelectedIndex = new TextBoxLoc();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SPWeb web = this.Web;
            {
                // Validate the Page Request to avoid any malicious posts
                if (Request.HttpMethod == "POST")
                    SPUtility.ValidateFormDigest();

                if (!Page.IsPostBack)
                {
                    String ListGuid = Request.QueryString["List"];
                    SPList List = web.Lists[new Guid(ListGuid)];

                    SPUser user = web.CurrentUser;
                    if (!user.IsSiteAdmin)
                    {
                        LogLink.Enabled = false;
                        LogLink.ForeColor = System.Drawing.Color.Gray; 
                    }

#if SP2013
                    LogLink.NavigateUrl = web.Url + "/_layouts/15/ICLogViewer.aspx?List=" + ListGuid;
#elif SP2016 || SP2019
                    LogLink.NavigateUrl = web.Url + "/_layouts/16/ICLogViewer.aspx?List=" + ListGuid;
#else
                    LogLink.NavigateUrl = web.Url + "/_layouts/ICLogViewer.aspx?List=" + ListGuid;
#endif

                    if (List != null)
                    {
                        //For List Trimming
                        if (Request.HttpMethod == "POST")
                            SPUtility.ValidateFormDigest();

                        if (!Page.IsPostBack)
                        {
                            ListGuid = Request.QueryString["List"];
                            List = web.Lists[new Guid(ListGuid)];

                            if (List != null)
                            {
                                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                                {
                                    if (manager.CheckSecurityTrimming() && manager.CheckListTrimming())
                                    {
                                        EnableListTrimming.Checked = true;
                                        EnableListTrimming.Enabled = false;
                                        EnableListPrefilterTrimming.Enabled = true;
                                        EnableListPrefilterTrimming.Checked = manager.CheckListPrefilterTrimming(List);
                                        if (manager.CheckListPrefilterTrimming())
                                        {
                                            EnableListPrefilterTrimming.Checked = true;
                                            EnableListPrefilterTrimming.Enabled = false;
                                        }

                                    }
                                    else if (!manager.CheckSecurityTrimming())
                                    {
                                        EnableListTrimming.Enabled = false;
                                        EnableListPrefilterTrimming.Enabled = false;
                                    }
                                    else
                                    {
                                        EnableListTrimming.Enabled = true;
                                        if (manager.CheckListTrimming(List))
                                        {
                                            EnableListTrimming.Checked = true;
                                            EnableListPrefilterTrimming.Enabled = true;
                                            if (manager.CheckListPrefilterTrimming(List))
                                            {
                                                EnableListPrefilterTrimming.Checked = true;
                                            }
                                            else
                                            {
                                                EnableListPrefilterTrimming.Checked = false;
                                            }
                                        }
                                        else
                                        {
                                            EnableListTrimming.Checked = false;
                                            EnableListPrefilterTrimming.Enabled = false;
                                        }
                                    }

                                    string strGlobalProcessUploadValue = Globals.GetGlobalSetOfProcessUpload(List);
                                    if (strGlobalProcessUploadValue.Equals(Globals.strGlobalProcessUploadPropValueNone, StringComparison.OrdinalIgnoreCase))
                                    {
                                        ProcessUploadCheckBox.Enabled = true;
                                        ProcessUploadCheckBox.Checked = !Globals.CheckListProperty(List, Globals.strLibraryProcessUploadPropName);
                                    }
                                    else
                                    {
                                        ProcessUploadCheckBox.Checked = strGlobalProcessUploadValue.Equals(Globals.strGlobalProcessUploadPropValueDisable, StringComparison.OrdinalIgnoreCase);
                                        ProcessUploadCheckBox.Enabled = false;
                                    }
                                }
                            }
                        }

                        //For CA
                        if (!List.DoesUserHavePermissions(SPBasePermissions.ManageLists))
                        {
                            String _url = "http://" + Request.Params["HTTP_HOST"] + Request.RawUrl;
                            SPUtility.Redirect("AccessDenied.aspx" + "?Source=" + _url, SPRedirectFlags.RelativeToLayoutsPage, this.Context);
                            return;
                        }

                        InitScheduleDropDownList(List);
                        bool isAdmin = false;
                        //bool isFarmAdmin = false;
                        //SPSecurity.RunWithElevatedPrivileges(delegate ()
                        //{
                        //    isFarmAdmin = SPFarm.Local.CurrentUserIsAdministrator();
                        //});
                        //if (!SPFarm.Local.CurrentUserIsAdministrator() && isFarmAdmin == false)
                        //    isAdmin = false;

                        System.Security.Principal.IIdentity userIdentity = HttpContext.Current.User.Identity;
                        string username = Globals.GetCurrentUser(userIdentity);
                        isAdmin = Globals.IsFarmAdministrator(username);

                        if (isAdmin)
                        {
                            if (!this.ClientScript.IsClientScriptBlockRegistered(this.GetType(), "CustomJS"))
                            {
                                string scrp = "var g_dropdown = document.getElementById('ctl00_PlaceHolderMain_CASchedule_scheduleFullDropDown'); function OnBaseLoad(){var link = document.getElementById('ctl00_PlaceHolderMain_CASchedule_manageScheduleLink'); var link2 = document.getElementById('ctl00_PlaceHolderMain_CASchedule_deleteScheduleLink'); link.onclick = function(){ OpenEditSchedule(); return false; }; link2.onclick = function(){ DeleteSelectedSchedule(); return false; }; ChangeScheduleLinkLabel(); }";
                                scrp += "function OpenEditSchedule(){var index = g_dropdown.selectedIndex; var gotoUrl = 'editcaschedule.aspx?'; if (index > 0) { gotoUrl += 'params=' + g_dropdown.options[index].value; } portal_openModalDialog(gotoUrl);}";
                                scrp += "function EditScheduleCallback(ret, value){ if(ret == 1) { if (g_dropdown.selectedIndex == 0){ var len = g_dropdown.length; g_dropdown.options[len] = new Option(value[0], value[1]); g_dropdown.selectedIndex = len; }else{var index = g_dropdown.selectedIndex; g_dropdown.options[index] = new Option(value[0], value[1]); g_dropdown.selectedIndex = index; } ChangeScheduleLinkLabel(); RecordSchedules(); } }";
                                scrp += "function ChangeScheduleLinkLabel() {var link = document.getElementById('ctl00_PlaceHolderMain_CASchedule_manageScheduleLink'); var link2 = document.getElementById('ctl00_PlaceHolderMain_CASchedule_deleteScheduleLink'); var index = g_dropdown.selectedIndex; document.getElementById('ctl00_PlaceHolderMain_SelectedIndex').value = g_dropdown.selectedIndex; if (index > 0){link.innerText='Edit schedule'; link2.style.display='inline'; }else{link.innerText='Create schedule'; link2.style.display='none'; } }";
                                scrp += "function RecordSchedules() { var xml = '<Schedules>'; document.getElementById('ctl00_PlaceHolderMain_SelectedIndex').value = g_dropdown.selectedIndex; for (var i = 1; i < g_dropdown.length; i++) { xml += g_dropdown.options[i].value; } xml += '</Schedules>'; document.getElementById('ctl00_PlaceHolderMain_AllXmlSchedules').value = xml; }";
                                scrp += "function CheckKeyIsNumber(){ var key = window.event.keyCode; return (key >= 48 && key <= 57); }";
                                scrp += "function DeleteSelectedSchedule() { var i = g_dropdown.selectedIndex; if (i > 0) g_dropdown.remove(i); ChangeScheduleLinkLabel(); RecordSchedules(); }";
                                scrp += "function portal_openModalDialog(url) { var options = SP.UI.$create_DialogOptions(); options.width = 500; options.height = 450; options.url = url; options.dialogReturnValueCallback = Function.createDelegate(null, EditScheduleCallback); SP.UI.ModalDialog.showModalDialog(options); }";
                                scrp += "_spBodyOnLoadFunctionNames.push(\"OnBaseLoad\");";
                                this.ClientScript.RegisterStartupScript(this.GetType(), "CustomJS", scrp, true);
                                scheduleFullDropDown.Attributes.Add("onchange", "ChangeScheduleLinkLabel();");
                            }
                            scheduleFullDropDown.Enabled = true;
                            manageScheduleLink.Enabled = true;
                        }
                        else
                        {
                            scheduleFullDropDown.Enabled = false;
                            manageScheduleLink.Enabled = false;
                        }

                        String ca_state = List.RootFolder.Properties["ca_state"] as String;
                        String curr_ca_itemcount = List.RootFolder.Properties["curr_ca_itemcount"] as String;
                        String need_ca_itemcount = List.RootFolder.Properties["need_ca_itemcount"] as String;
                        //String curr_ca_fileurl = List.RootFolder.Properties["curr_ca_fileurl"] as String;

                        if (ca_state != null && ca_state.Equals("In Progress"))
                        {
                            if (List.BaseType == SPBaseType.DocumentLibrary)
                            {
                                DescriptionLiteral.Text = "Process Content on documents within the document library for classification. ";
                                DescriptionLiteral2.Text = "Check this checkbox and click OK button will stop the Processing of all documents within the document library.";
                            }
                            else
                            {
                                DescriptionLiteral.Text = "Process Content on documents within the list for classification. ";
                                DescriptionLiteral2.Text = "Check this checkbox and click OK button will stop the Processing of all documents within the list.";
                            }

                            CAStatusCheckBox.LabelText = "Stop Processing";
                            CAStatus.Text = "Status: Completed " + curr_ca_itemcount.ToString() +
                                " of " + need_ca_itemcount.ToString() + " Documents";
                        }
                        else
                        {
                            if (List.BaseType == SPBaseType.DocumentLibrary)
                            {
                                DescriptionLiteral.Text = "Process Content on documents within the document library for classification. ";
                                DescriptionLiteral.Text += "Check this checkbox and click OK button will start the Processing of all documents within the document library.";
                            }
                            else
                            {
                                DescriptionLiteral.Text = "Process Content on documents within the list for classification. ";
                                DescriptionLiteral.Text += "Check this checkbox and click OK button will start the Processing of all documents within the list.";
                            }

                            CAStatusCheckBox.LabelText = "Start Processing";
                            CAStatus.Text = "Status: Not Running";
                        }
                    }
                }
            }
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }

        protected void InitScheduleDropDownList(SPList list)
        {
            ListItem item = new ListItem("None", "None");
            scheduleFullDropDown.Items.Add(item);
            scheduleFullDropDown.SelectedIndex = 0;

            String ca_schedules = list.RootFolder.Properties["ca_schedules"] as String;
            String ca_schIndex = list.RootFolder.Properties["ca_schIndex"] as String;
            if (!String.IsNullOrEmpty(ca_schedules))
            {
                AllXmlSchedules.Text = ca_schedules;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = ca_schedules;
                XmlNode node = xmlDoc.DocumentElement;

                XmlNodeList nodes = node.ChildNodes;
                foreach (XmlNode child in nodes)
                {
                    string type = child["Type"].InnerText;
                    string display = "";
                    if (type.Equals("Hourly"))
                    {
                        display = "Every " + child["Hours"].InnerText + " hour(s) from ";
                        display += (child["From"].InnerText + ", ");
                    }
                    else if (type.Equals("Minutely"))
                    {
                        display = "Every " + child["Minutes"].InnerText + " minute(s) from ";
                        display += (child["From"].InnerText + ", ");
                    }
                    else if (type.Equals("Daily"))
                    {
                        display = "At " + child["From"].InnerText;
                        display += (" every " + child["Days"].InnerText + " day(s), ");
                    }
                    else
                    {
                        display = "At " + child["From"].InnerText + " every ";
                        display += child["WeekDays"].InnerText;
                        display += (" of every " + child["Weeks"].InnerText + " week(s), ");
                    }
                    display += ("starting " + child["StartDate"].InnerText);

                    ListItem item2 = new ListItem(display, child.OuterXml);
                    scheduleFullDropDown.Items.Add(item2);
                }
            }

            if (!String.IsNullOrEmpty(ca_schIndex))
            {
                scheduleFullDropDown.SelectedIndex = Int32.Parse(ca_schIndex);
                SelectedIndex.Text = ca_schIndex;
            }
        }

        protected void SaveScheduleDropDownList(SPList list)
        {
            list.RootFolder.Properties["ca_schedules"] = AllXmlSchedules.Text;
            list.RootFolder.Properties["ca_schIndex"] = SelectedIndex.Text;
            list.RootFolder.Update();
        }

        protected void BtnOK_Click(object sender, EventArgs e)
        {
            String ListGuid = Request.QueryString["List"];
            SPWeb web = SPControl.GetContextWeb(this.Context);
            SPList List = web.Lists[new Guid(ListGuid)];

            this.Page.Validate();
            if (this.Page.IsValid && List != null)
            {
                //For List Trimming
                SPSite site = SPControl.GetContextSite(this.Context);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (EnableListTrimming.Enabled)
                    {
                        if (EnableListTrimming.Checked)
                        {
                            manager.EnableListTrimming(List);
                            if (EnableListPrefilterTrimming.Checked)
                            {
                                manager.EnableListPrefilterTrimming(List);
                            }
                            else
                            {
                                manager.DisableListPrefilterTrimming(List);
                            }
                        }
                        else
                        {
                            manager.DisableListTrimming(List);
                            manager.DisableListPrefilterTrimming(List);
                        }
                    }
                    else
                    {
                        if (EnableListTrimming.Checked)
                        {
                            if (EnableListPrefilterTrimming.Checked)
                            {
                                manager.EnableListPrefilterTrimming(List);
                            }
                            else
                            {
                                manager.DisableListPrefilterTrimming(List);
                            }
                        }
                    }

                    if (ProcessUploadCheckBox.Enabled)
                    {
                        if (ProcessUploadCheckBox.Checked)	// batch mode
                        {
                            if (Globals.CheckListProperty(List, Globals.strLibraryProcessUploadPropName))
                            {
                                Globals.DisableListProperty(List, Globals.strLibraryProcessUploadPropName);
                                SPEventReceiverDefinitionCollection AllEventReceivers = List.EventReceivers;
                                string assemblyClassName = "NextLabs.SPEnforcer.ItemHandler";
                                for (int i = 0; i < AllEventReceivers.Count; i++)
                                {
                                    SPEventReceiverDefinition it = AllEventReceivers[i];
                                    if (it.Class.Equals(assemblyClassName, StringComparison.OrdinalIgnoreCase) &&
                                        (it.Type == SPEventReceiverType.ItemAdded || it.Type == SPEventReceiverType.ItemAttachmentAdded))
                                    {
                                        SPSecurity.RunWithElevatedPrivileges(delegate()
                                        {
                                            it.Synchronization = SPEventReceiverSynchronization.Synchronous;
                                            it.Update();
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!Globals.CheckListProperty(List, Globals.strLibraryProcessUploadPropName))
                            {
                                Globals.EnableListProperty(List, Globals.strLibraryProcessUploadPropName);
                                SPEventReceiverDefinitionCollection AllEventReceivers = List.EventReceivers;
                                string assemblyClassName = "NextLabs.SPEnforcer.ItemHandler";
                                for (int i = 0; i < AllEventReceivers.Count; i++)
                                {
                                    SPEventReceiverDefinition it = AllEventReceivers[i];
                                    if (it.Class.Equals(assemblyClassName, StringComparison.OrdinalIgnoreCase) &&
                                        (it.Type == SPEventReceiverType.ItemAdded || it.Type == SPEventReceiverType.ItemAttachmentAdded))
                                    {
                                        SPSecurity.RunWithElevatedPrivileges(delegate()
                                        {
                                            it.Synchronization = SPEventReceiverSynchronization.Asynchronous;
                                            it.Update();
                                        });
                                    }
                                }
                            }
                        }
                    }

                    //For CA
                    if (scheduleFullDropDown.Enabled == true)
                    {
                        SaveScheduleDropDownList(List);

                        CASchedule caSchedule = new CASchedule(web.Url, web.CurrentUser.LoginName, web.CurrentUser.Sid, ListGuid, this.Request.UserHostAddress);
                        caSchedule.UpdateTimer();
                    }


                    if (ClearLastScanTimeCheckBox.Checked)
                    {
                        foreach (SPListItem item in List.Items)
                        {
                            if (List.BaseType == SPBaseType.DocumentLibrary)
                            {
                                try
                                {
                                    if (item.File.Properties.ContainsKey("nextlabs_lastscan"))
                                    {

                                        item.File.Properties.Remove("nextlabs_lastscan");
                                        item.File.Update();
                                    }
                                }
                                catch (Exception exp)
                                {
                                    if (exp.Message.Contains("is not checked out"))
                                    {
                                        item.File.CheckOut();
                                        item.File.Update();
                                        item.File.CheckIn("Reset Property[nextlabs_lastscan] after user select [Clear last scan time] checkbox");
                                    }
                                }
                            }
                            else
                            {
                                SPAttachmentCollection attachments = item.Attachments;
                                SPSecurity.RunWithElevatedPrivileges(delegate()
                                {
                                    foreach (string url in attachments)
                                    {
                                        SPFile _file = null;
                                        try
                                        {
                                            _file = item.ParentList.ParentWeb.GetFile(attachments.UrlPrefix + url);

                                            if (_file.Properties.ContainsKey("nextlabs_lastscan"))
                                            {

                                                _file.Properties.Remove("nextlabs_lastscan");
                                                _file.Update();
                                            }
                                        }
                                        catch (Exception exp)
                                        {
                                            if (exp.Message.Contains("is not checked out"))
                                            {
                                                _file.CheckOut();
                                                _file.Update();
                                                _file.CheckIn("Reset Property[nextlabs_lastscan] after user select [Clear last scan time] checkbox");
                                            }
                                        }

                                    }
                                });
                            }
                        }

                    }

                    // Manual start execute batch mode
                    if (CAStatusCheckBox.Checked)
                    {
                        if (List != null)
                        {
                            String ca_state = List.RootFolder.Properties["ca_state"] as String;

                            if (!CAStatus.Text.Equals("Status: Not Running")
                                && ca_state != null && ca_state.Equals("In Progress"))
                            {
                                // Stop Content Analysis
                                List.RootFolder.Properties["ca_state"] = "Idle";
                                List.RootFolder.Update();
                            }
                            else if (CAStatus.Text.Equals("Status: Not Running")
                                && (ca_state == null || !ca_state.Equals("In Progress")))
                            {
                                // Start Content Analysis
                               // Thread currentThread = Thread.CurrentThread;

                                using (AutoResetEvent autoEvent = new AutoResetEvent(false))
                                {
                                    ListContentAnalysisWorker worker = new ListContentAnalysisWorker(web.Url, web.CurrentUser.UserToken, ListGuid, this.Request.UserHostAddress);
                                    Thread workerThread = new Thread(worker.Run);
                                    workerThread.Start(autoEvent);
                                    autoEvent.WaitOne();
                                }
                            }
                        }
                    }
                }
            }

            Response.Redirect(Request.RawUrl);
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            String ListGuid = Request.QueryString["List"];

            SPUtility.Redirect("listedit.aspx?List=" + ListGuid, SPRedirectFlags.RelativeToLayoutsPage, this.Context);
        }
    }
}
