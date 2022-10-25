using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using Microsoft.SharePoint.ApplicationPages;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Portal.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using NextLabs.Common;
using System.Data.OleDb;
using System.Threading;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public class NxlClassificationComparisonPage : LayoutsPageBase
    {
        protected InputFormDropDownList scheduleFullDropDown;
        protected InputFormHyperLink manageScheduleLink;
        protected Microsoft.SharePoint.WebControls.InputFormCheckBox StartCheckBox;
        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc AllXmlSchedules;
        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc SelectedIndex;

        public NxlClassificationComparisonPage()
        {
            manageScheduleLink = new InputFormHyperLink();
            scheduleFullDropDown = new InputFormDropDownList();
            StartCheckBox = new Microsoft.SharePoint.WebControls.InputFormCheckBox();
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
                if (web.EffectiveBasePermissions == SPBasePermissions.ManagePermissions
                    || web.EffectiveBasePermissions == SPBasePermissions.FullMask)
                {
                    if (!Page.IsPostBack)
                    {
                        InitScheduleDropDownList(web);
                        if (!this.ClientScript.IsClientScriptBlockRegistered(this.GetType(), "CustomJS"))
                        {
                            string scrp = "var g_dropdown = document.getElementById('ctl00_PlaceHolderMain_WPTSection3_scheduleFullDropDown'); function OnBaseLoad(){var link = document.getElementById('ctl00_PlaceHolderMain_WPTSection3_manageScheduleLink'); var link2 = document.getElementById('ctl00_PlaceHolderMain_WPTSection3_deleteScheduleLink'); link.onclick = function(){ OpenEditSchedule(); return false; }; link2.onclick = function(){ DeleteSelectedSchedule(); return false; }; ChangeScheduleLinkLabel(); }";
                            scrp += "function OpenEditSchedule(){var index = g_dropdown.selectedIndex; var gotoUrl = 'editcaschedule.aspx?'; if (index > 0) { gotoUrl += 'params=' + g_dropdown.options[index].value; } portal_openModalDialog(gotoUrl);}";
                            scrp += "function EditScheduleCallback(ret, value){ if(ret == 1) { if (g_dropdown.selectedIndex == 0){ var len = g_dropdown.length; g_dropdown.options[len] = new Option(value[0], value[1]); g_dropdown.selectedIndex = len; }else{var index = g_dropdown.selectedIndex; g_dropdown.options[index] = new Option(value[0], value[1]); g_dropdown.selectedIndex = index; } ChangeScheduleLinkLabel(); RecordSchedules(); } }";
                            scrp += "function ChangeScheduleLinkLabel() {var link = document.getElementById('ctl00_PlaceHolderMain_WPTSection3_manageScheduleLink'); var link2 = document.getElementById('ctl00_PlaceHolderMain_WPTSection3_deleteScheduleLink'); var index = g_dropdown.selectedIndex; document.getElementById('ctl00_PlaceHolderMain_SelectedIndex').value = g_dropdown.selectedIndex; if (index > 0){link.innerText='Edit schedule'; link2.style.display='inline'; }else{link.innerText='Create schedule'; link2.style.display='none'; } }";
                            scrp += "function RecordSchedules() { var xml = '<Schedules>'; document.getElementById('ctl00_PlaceHolderMain_SelectedIndex').value = g_dropdown.selectedIndex; for (var i = 1; i < g_dropdown.length; i++) { xml += g_dropdown.options[i].value; } xml += '</Schedules>'; document.getElementById('ctl00_PlaceHolderMain_AllXmlSchedules').value = xml; }";
                            scrp += "function DeleteSelectedSchedule() { var i = g_dropdown.selectedIndex; if (i > 0) g_dropdown.remove(i); ChangeScheduleLinkLabel(); RecordSchedules(); }";
                            scrp += "function portal_openModalDialog(url) { var options = SP.UI.$create_DialogOptions(); options.width = 500; options.height = 450; options.url = url; options.dialogReturnValueCallback = Function.createDelegate(null, EditScheduleCallback); SP.UI.ModalDialog.showModalDialog(options); }";
                            scrp += "_spBodyOnLoadFunctionNames.push(\"OnBaseLoad\");";
                            this.ClientScript.RegisterStartupScript(this.GetType(), "CustomJS", scrp, true);
                            scheduleFullDropDown.Attributes.Add("onchange", "ChangeScheduleLinkLabel();");
                        }
                        scheduleFullDropDown.Enabled = true;
                        manageScheduleLink.Enabled = true;
                    }
                }
                else
                {
                    String _url = "http://" + Request.Params["HTTP_HOST"] + Request.RawUrl;
                    SPUtility.Redirect("AccessDenied.aspx" + "?Source=" + _url, SPRedirectFlags.RelativeToLayoutsPage, this.Context);
                    return;
                }
            }
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }

        protected void InitScheduleDropDownList(SPWeb web)
        {
            ListItem item = new ListItem("None", "None");
            scheduleFullDropDown.Items.Add(item);
            scheduleFullDropDown.SelectedIndex = 0;

            String ca_schedules = Globals.GetSiteProperty(web, Globals.strSiteSchedulesPropName);
            String ca_schIndex = Globals.GetSiteProperty(web, Globals.strSiteSchIndexPropName);
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


        protected void SaveScheduleDropDownList()
        {
            Globals.SetSiteProperty(this.Web, Globals.strSiteSchedulesPropName, AllXmlSchedules.Text);
            Globals.SetSiteProperty(this.Web, Globals.strSiteSchIndexPropName, SelectedIndex.Text);
        }

        private void SetClassificationCompSchedule()
        {
            SPWeb web = SPControl.GetContextWeb(this.Context);
          //  SPSite curSite = SPControl.GetContextSite(HttpContext.Current);
            if (web != null)
            {
                RunLevel level = RunLevel.RLSiteLevel;
                string strID = web.ID.ToString();
                CASchedule caSchedule = new CASchedule(web.Url, web.CurrentUser.LoginName, web.CurrentUser.Sid, strID, this.Request.UserHostAddress, level, web.CurrentUser.ID);
                caSchedule.UpdateTimerInSiteLevel();
            }

        }


        protected void BtnOK_Click(object sender, EventArgs e)
        {
           SPWeb web = this.Web;
            if (StartCheckBox.Checked)//start immediately
            {
                string strState = Globals.GetSiteProperty(web, Globals.strSiteProcessStatePropName);

                if (string.IsNullOrEmpty(strState) || !strState.Equals(Globals.strSiteCNMStatePropValue_Processing))
                {
                    //can start the cnm, run worker thread...
                    try
                    {
                        using (AutoResetEvent autoEvent = new AutoResetEvent(false))
                        {
                            CNMWorker worker = new CNMWorker(web.Url, web.CurrentUser.UserToken, web.ID.ToString(), this.Request.UserHostAddress, true);
                            Thread workerThread = new Thread(worker.WorkerRun);
                            workerThread.Start(autoEvent);
                            autoEvent.WaitOne();
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during BtnOK_Click:", null, ex);
                    }
                }
            }
            SaveScheduleDropDownList();
            SetClassificationCompSchedule();
            SPUtility.Redirect("settings.aspx", SPRedirectFlags.UseSource | SPRedirectFlags.RelativeToLayoutsPage, this.Context);
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Page.Validate();
            if (this.Page.IsValid)
            {
                SPUtility.Redirect("settings.aspx", SPRedirectFlags.UseSource | SPRedirectFlags.RelativeToLayoutsPage, this.Context);
            }
        }

    }
}
