using System;
using System.Diagnostics;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.ApplicationPages;
using NextLabs.Common;
using System.Collections.Generic;
using NextLabs.Diagnostic;
namespace NextLabs.Deployment
{
    public class FeatureController : LayoutsPageBase
    {
        //querystring key for selected WebApplication.
        private const string QUERYSTRING_SELECTED_KEY = "wa";
        #region VARIABLES

        protected DropDownList WebAppDropDown;
        protected TreeView FeatureTree;
        protected InputFormSection CentralAdminSection;
        protected InputFormSection OptionsSection;
        protected ImageButton RootSiteImageButton;
        protected LinkButton RootSiteLinkButton;
        protected Button UpdateButton;
        protected RadioButton OptionCheckBox;
        protected RadioButton DeactivateCheckBox;
        protected InputFormCheckBox NewSiteCheckBox;
        protected Literal PageDescription;

        protected Literal ResetRealtimeModeText;
        protected RadioButton GlobalProcessUploadNotset;
        protected RadioButton GlobalProcessUploadBatchMode;
        protected RadioButton GlobalProcessUploadRealTime;

        protected CheckBox InputFormCheckBoxUseJpc;
        protected InputFormTextBox InputFormTextBoxJavaPcHost;
        protected InputFormTextBox InputFormTextBoxClientID;
        protected InputFormTextBox InputFormTextBoxClientSecureKey;
        protected InputFormTextBox InputFormTextBoxOAUTHHost;
        protected InputFormTextBox InputFormTextBoxOAUTHHostUserName;
        protected InputFormTextBox InputFormTextBoxOAUTHHostPassword;
        protected InputFormRequiredFieldValidator InputFormRequiredFieldClientSecureKey;



        private ArrayList expandedNodes = new ArrayList();
        public string webAppName = string.Empty;
        public string barValue = string.Empty;
        public string barStr = string.Empty;
        public string visibility = string.Empty;
        private const string SiteIdSplit = ";";
        #endregion

        #region LOAD

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                this.ClientScript.RegisterStartupScript(this.GetType(), "StartFeatureStyling", "StyleFeatureList();", true);
                SPWeb currentWeb = SPContext.Current.Web;
                PageDescription.Text = "Administration of Nextlabs Entitlement in SharePoint";
                if (!Page.IsPostBack)
                {
                    if (currentWeb != null)
                    {
                        LoadWebAppDropDown();

                        //Try to get default selection from cookie
                        var selectedValue = Request.QueryString[QUERYSTRING_SELECTED_KEY];
                        if (selectedValue != null)
                        {
                            //try set the selected value of the dropdown list as the value from cookie.
                            var listItem = WebAppDropDown.Items.FindByValue(selectedValue);
                            if (listItem != null)
                            {
                                listItem.Selected = true;
                            }
                        }

                        //get the default value of "ProcessUpload"

                        if (!string.IsNullOrEmpty(WebAppDropDown.SelectedValue))
                        {
                            InitDefaultMode();
                            LoadTreeView();
                            InitProgressBar();
                        }
                        else
                        {
                            NLLogger.OutputLog(LogLevel.Error, "FeatureController Page_Load Error: WebAppDropDown.SelectedValue:" + WebAppDropDown.SelectedValue);
                            this.UpdateButton.Enabled = false;
                        }

                        SPWebApplication spAdminWebApp = null;
                        if (Globals.GetAdministrationWebApplication(ref spAdminWebApp) && spAdminWebApp != null)
                        {
                            // Set JavaPC enable or disable.
                            string strJavaPCEnabled = string.Empty;
                            try
                            {
                                strJavaPCEnabled = spAdminWebApp.Properties[Globals.strGlobalJavaPCPropertyName] as string;
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Debug, "Exception during Get admin app:", null, ex);
                            }



                            if (!string.IsNullOrEmpty(strJavaPCEnabled) && strJavaPCEnabled.Equals(Globals.strGlobalEnabled))
                            {
                                //JavaPc is enabled.
                                InputFormCheckBoxUseJpc.Checked = true;
                                InputFormTextBoxJavaPcHost.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCHost] as string;
                                InputFormTextBoxClientID.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCClientID] as string;
                                InputFormTextBoxClientSecureKey.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCClientSecureKey] as string;
                            }
                            else
                            {
                                InputFormCheckBoxUseJpc.Checked = false;
                                ResetJavaPCSettings();
                            }

                            // add cc info for prefilter
                            if (spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHUserName] != null)
                            {
                                InputFormTextBoxOAUTHHostUserName.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHUserName] as string;
                            }
                            if (spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHPwd] != null)
                            {
                                InputFormTextBoxOAUTHHostPassword.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHPwd] as string;
                            }
                            InputFormTextBoxOAUTHHost.Text = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHHost] as string;
                        }
                        else
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "Get admin app failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during Page_Load:", null, ex);
            }
        }

		private void InitDefaultMode()
        {
            SPFarm farm = SPFarm.Local;
            SPWebService spws = farm.Services.GetValue<SPWebService>("");
            SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
            string strEnableRealTimeMode = selWebApp.Properties[Globals.strGlobalProcessUploadPropName] as string;
            if ((strEnableRealTimeMode == null) || (strEnableRealTimeMode.Equals(Globals.strGlobalProcessUploadPropValueNone, StringComparison.OrdinalIgnoreCase)))
            {
                GlobalProcessUploadNotset.Checked = true;
                GlobalProcessUploadBatchMode.Checked = false;
                GlobalProcessUploadRealTime.Checked = false;
            }
            else if (strEnableRealTimeMode.Equals(Globals.strGlobalProcessUploadPropValueEnable, StringComparison.OrdinalIgnoreCase))
            {
                GlobalProcessUploadRealTime.Checked = true;
                GlobalProcessUploadBatchMode.Checked = false;
                GlobalProcessUploadNotset.Checked = false;
            }
            else
            {
                GlobalProcessUploadBatchMode.Checked = true;
                GlobalProcessUploadRealTime.Checked = false;
                GlobalProcessUploadNotset.Checked = false;
            }
        }

        private void ResetJavaPCSettings()
        {
            InputFormTextBoxJavaPcHost.Enabled = InputFormCheckBoxUseJpc.Checked;
            InputFormTextBoxClientID.Enabled = InputFormCheckBoxUseJpc.Checked;
            InputFormTextBoxClientSecureKey.Enabled = InputFormCheckBoxUseJpc.Checked;
            InputFormRequiredFieldClientSecureKey.Enabled = InputFormCheckBoxUseJpc.Checked;
            InputFormRequiredFieldClientSecureKey.IsValid = InputFormCheckBoxUseJpc.Checked;
        }
        protected void UseJavaPC_CheckedChanged(object sender, EventArgs e)
        {
            //CommonLib utility = new CommonLib();
            //if(utility.IsSPEInstalledWithoutCEPCOnlyUseJPC())
            //{ 
            //    CheckBox _InputFormCheckBoxUseJpc = sender as CheckBox;
            //    _InputFormCheckBoxUseJpc.Visible = false;
            //    _InputFormCheckBoxUseJpc.Enabled = false;
            //    InputFormTextBoxJavaPcHost.Enabled = false;
            //    InputFormTextBoxClientID.Enabled = false;
            //    InputFormTextBoxClientSecureKey.Enabled = false;
            //    InputFormRequiredFieldClientSecureKey.Enabled = false;
            //    InputFormRequiredFieldClientSecureKey.IsValid = false;
            //    return;
            //}
            ResetJavaPCSettings();
        }

        private void InitProgressBar()
        {
            try
            {
                barValue = Progress.GetLastLog(webAppName).Split('|')[0];
                barStr = Progress.GetLastLog(webAppName).Split('|')[1];
            }
            catch
            {
                barValue = "0";
                barStr = "";
            }
            if (barValue == "0")
            {
                visibility = "0";
            }
            else
            {
                visibility = "100";
            }
        }
        private void LoadWebAppDropDown()
        {
            WebAppDropDown.Items.Clear();

            SPSolution installedSolution = SPFarm.Local.Solutions[new Guid(CommonLib.NEXTLABSWSPGUID)];
            foreach (SPWebApplication webApp in installedSolution.DeployedWebApplications)
            {
                if (!webApp.IsAdministrationWebApplication)
                {
                    WebAppDropDown.Items.Add(new ListItem(webApp.Name + " - " + webApp.GetResponseUri(SPUrlZone.Default).ToString(), webApp.Id.ToString()));
                }
            }
        }

        private void LoadTreeView()
        {
            SaveExpandedNodes();

            if (IsCentralAdmin())
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    FeatureTree.Nodes.Clear();
                    if (!string.IsNullOrEmpty(WebAppDropDown.SelectedValue))
                    {
                        SPFarm farm = SPFarm.Local;
                        SPWebService spws = farm.Services.GetValue<SPWebService>("");
                        SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];

                        //Set the Checkbox.
                        bool bWebappStatus = selWebApp.GetPEDefaultStatus();
                        OptionCheckBox.Checked = bWebappStatus;
                        DeactivateCheckBox.Checked = !bWebappStatus;
                        if (bWebappStatus)
                        {
                            string value = Globals.GetNewSitePEDefault(selWebApp);
                            NewSiteCheckBox.Visible = true;
                            NewSiteCheckBox.Checked = !value.Equals(Globals.strGlobalDisabled);
                        }
                        else
                        {
                            NewSiteCheckBox.Visible = false;
                        }
                        //To update the Treeview.
                        TreeNode webAppNode = new TreeNode(selWebApp.Name + " - " + selWebApp.GetResponseUri(SPUrlZone.Default).ToString(), selWebApp.Id.ToString());
                        webAppName = selWebApp.Name;
                        webAppNode.NavigateUrl = "javascript:CheckUncheckTree();";
                        webAppNode.ToolTip = "Click to select/unselect Site Collections (including SubSites)";
                        webAppNode.ShowCheckBox = false;
                        if (GetWebAppEMIsActive(selWebApp))
                        {
                            webAppNode.ImageUrl = CommonLib.GIFACTIVEURL;
                        }
                        else
                        {
                            webAppNode.ImageUrl = CommonLib.GIFDEACTIVEURL;
                        }
                        webAppNode.Expand();
                        string strSiteIds = Globals.GetActivatedSiteIds(selWebApp);
                        foreach (SPSite site in selWebApp.Sites)
                        {
                            using (site)
                            {
                                try
                                {
                                    //site.AllowUnsafeUpdates = true;
                                    TreeNode siteRootNode = new TreeNode();
                                    siteRootNode.ShowCheckBox = true;
                                    siteRootNode.Checked = site.GetPEStatus();
                                    siteRootNode.Text = site.RootWeb.Title + " (Site Collection)";
                                    siteRootNode.Value = site.ID.ToString();
                                    siteRootNode.ImageUrl = strSiteIds.Contains(site.ID.ToString()) ? CommonLib.GIFACTIVEURL : CommonLib.GIFDEACTIVEURL;
                                    //fix bug 60040,readonly site and new created failed site have not checkbox
                                    if (site.ReadOnly || site == null)
                                    {
                                        siteRootNode.ShowCheckBox = false;
                                        if(site == null)
                                        {
                                            siteRootNode.Text = site.RootWeb.Title + " (Invalid Site Collection)";
                                        }
                                        else if (site.ReadOnly)
                                        {
                                            siteRootNode.Text = site.RootWeb.Title + " (Read-only Site Collection)";
                                        }
                                    }

                                    // for new created site collection(not include readonly site and new created failed site)
                                    if (!site.ReadOnly && site != null && bWebappStatus && site.GetPEStatus() && !strSiteIds.Contains(site.ID.ToString()))
                                    {
                                        if (NewSiteCheckBox.Visible && NewSiteCheckBox.Checked)
                                        {
                                            CommonLib utility = new CommonLib();
                                            System.Guid guidEventReceiver = new System.Guid(CommonLib.NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID);
                                            utility.AddOrRemoveFeatureForWebs(selWebApp, site, guidEventReceiver, true);
                                            if (CommonLib.CheckFeatureForWebs(selWebApp, site, true))
                                            {
                                                bool bOrgAllowUnsafeUpdatesFlag = site.AllowUnsafeUpdates;
                                                site.AllowUnsafeUpdates = true;
                                                siteRootNode.ImageUrl = CommonLib.GIFACTIVEURL;
                                                Globals.SetActivatedSiteIds(selWebApp, strSiteIds + SiteIdSplit + site.ID.ToString());
                                                site.AllowUnsafeUpdates = bOrgAllowUnsafeUpdatesFlag;
                                            }
                                        }
                                        else
                                        {
                                            // Remove the selected for this site.
                                            siteRootNode.Checked = false;
                                            site.SetPEStatus(false);
                                        }
                                    }
                                    webAppNode.ChildNodes.Add(siteRootNode);
                                }
                                catch (Exception ex)
                                {
                                    NLLogger.OutputLog(LogLevel.Error, "Exception during Get Site:", null, ex);
                                }
                            }
                        }
                        FeatureTree.Nodes.Add(webAppNode);
                        if (!Page.IsPostBack)
                        {
                            webAppNode.CollapseAll();
                            webAppNode.Expand();
                        }
                    }

                });
            }
        }

        protected void WebAppDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTreeView();
            InitProgressBar();
            InitDefaultMode();
            WebAppDropDown.Focus();
        }

#endregion

#region UTILITIES

        private bool IsCentralAdmin()
        {
            return SPContext.Current.Site.WebApplication.IsAdministrationWebApplication;
        }

        private bool GetWebAppEMIsActive(SPWebApplication webApp)
        {
            bool active = false;
            try
            {
                if (webApp != null)
                {
                    Guid basicGuid = new Guid(CommonLib.NEXTLABSENTITLEMENTBASICFEATUREID);
                    SPFeature activeFeature = webApp.Features[basicGuid];
                    if (activeFeature != null) active = true;
                    else active = false;
                }
            }
            catch
            {
            }
            return active;
        }

        private bool SetWebAppEM(SPWebApplication webApp, List<string> selectedSiteIDs, bool bActiveFeatureIn)
        {
            bool setResult = false;
            try
            {
                string strActiveOrDeActive = bActiveFeatureIn ? "Actived" : "Deactived";
                CommonLib utility = new CommonLib();
                System.Guid guidEventReceiver = new System.Guid(CommonLib.NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID);
                Progress.WriteLog(webApp.Name, "5|Start to " + strActiveOrDeActive.Substring(0, strActiveOrDeActive.Length - 1) + " feature NextLabs.Entitlement.Basic");
                if (webApp == null)
                {
                    NLLogger.OutputLog(LogLevel.Info, "The web app is null, no need active\n");
                }
                else
                {
                    try
                    {
                        Guid basicGuid = new Guid(CommonLib.NEXTLABSENTITLEMENTBASICFEATUREID);
                        bool formDigestSetting = webApp.FormDigestSettings.Enabled;
                        webApp.FormDigestSettings.Enabled = false;
                        Progress.WriteLog(webApp.Name, "Flag:RunFormUpdateButton");
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            bool bWebAppActiveFlag = GetWebAppEMIsActive(webApp);
                            if (bActiveFeatureIn)
                            {
                                if (bWebAppActiveFlag)
                                {
                                    NLLogger.OutputLog(LogLevel.Info, "WebApp:[{0}] already actived:", new object[] { webApp.Name });
                                }
                                else
                                {
                                    Progress.WriteLog(webApp.Name, "5|Start to " + strActiveOrDeActive.Substring(0, strActiveOrDeActive.Length - 1) + " feature NextLabs.Entitlement.Basic");
                                    Progress.WriteLog(webApp.Name, "Flag:RunFormUpdateButton");
                                    webApp.Features.Add(basicGuid);
                                    Progress.WriteLog(webApp.Name, "91|" + webApp.Name + " " + strActiveOrDeActive + "feature NextLabs.Entitlement.Basic finish");
                                }
                                string webconfigFilePath = utility.GetWebconfigFilePath(ref webApp);
                                List<string> spservers = new List<string>();
                                bool result = utility.GetInvalidFarmSPServers(ref spservers, webApp.Name);
                                if(result == true)
                                {
                                    foreach(string server in spservers)
                                    {
                                        utility.EditWebconfig(server, Operations.Edit, webconfigFilePath, webApp.Name);
                                    }
                                }

                                utility.ActivateWebScopeFeature(webApp, guidEventReceiver, selectedSiteIDs);
                            }
                            else
                            {
                                if (bWebAppActiveFlag)
                                {
                                    Progress.WriteLog(webApp.Name, "5|Start to " + strActiveOrDeActive.Substring(0, strActiveOrDeActive.Length - 1) + " feature NextLabs.Entitlement.Basic");
                                    Progress.WriteLog(webApp.Name, "Flag:RunFormUpdateButton");
                                    webApp.Features.Remove(basicGuid);
                                    Progress.WriteLog(webApp.Name, "91|" + webApp.Name + " " + strActiveOrDeActive + "feature NextLabs.Entitlement.Basic finish");
                                }
                                else
                                {
                                    NLLogger.OutputLog(LogLevel.Info, string.Format("WebApp:[{0}] already deactived:", webApp.Name));
                                }
                                string webconfigFilePath = utility.GetWebconfigFilePath(ref webApp);
                                List<string> spservers = new List<string>();
                                bool result = utility.GetInvalidFarmSPServers(ref spservers, webApp.Name);
                                if (result == true)
                                {
                                    foreach (string server in spservers)
                                    {
                                        utility.EditWebconfig(server, Operations.Delete, webconfigFilePath, webApp.Name);
                                    }
                                }
                                utility.DeactivateWebScopeFeature(webApp, guidEventReceiver, selectedSiteIDs);
                            }
                        });

                        webApp.FormDigestSettings.Enabled = formDigestSetting;
                        webApp.Update();
                        Progress.WriteLog(webApp.Name, "91|" + webApp.Name + " " + strActiveOrDeActive + "feature NextLabs.Entitlement.Basic finish");
                        Progress.WriteLog(webApp.Name, "92|" + strActiveOrDeActive + " policy enforcement for site collections ");
                        setResult = true;
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Error, "Exception during SetWebAppEM(inner):", null, ex);
                        if (ex.Message.Contains("An update conflict has occurred"))
                        {
                            setResult = true;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SetWebAppEM:", null, ex);
            }
            return setResult;
        }

#endregion

#region NODE STATE METHODS

        private void SaveExpandedNodes()
        {
            expandedNodes.Clear();

            foreach (TreeNode node in FeatureTree.Nodes)
            {
                SaveNodeExpandState(node);
            }
        }

        private void SaveNodeExpandState(TreeNode node)
        {
            if (node.Expanded == true)
            {
                expandedNodes.Add(node.Value);
            }

            foreach (TreeNode subNode in node.ChildNodes)
            {
                SaveNodeExpandState(subNode);
            }
        }

#endregion

#region EVENT METHODS
        private void UpdateEnforce(string strSelWebAppId, bool bActive, List<string> selectedSiteIDs)
        {
            SPFarm farm = SPFarm.Local;
            SPWebService spws = farm.Services.GetValue<SPWebService>("");
            SPWebApplication selWebApp = spws.WebApplications[new Guid(strSelWebAppId)];
            string strAction = bActive ? "Activate Policy Enforcement" : "Disable Policy Enforcement";
            string cellAction = bActive ? "activate" : "deactivate";
            List<string> activedSiteIDs = new List<string>();
            try
            {
                Progress.CreateLog(selWebApp.Name);
                Progress.WriteLog(selWebApp.Name, "BEGIN");
                Progress.WriteLog(selWebApp.Name, "0|Start Update ...  at " + DateTime.Now.ToString());
                if (bActive)
                {
                    //To active the selected webapp
                    if (SetWebAppEM(selWebApp, selectedSiteIDs, true))
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "CentralAdmin to active Nextlabs Sharepoint Entilement Management on WebAPP---" + selWebApp.Name + "---successfully");
                    }
                    else
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "CentralAdmin to active Nextlabs Sharepoint Entilement Management on WebAPP---" + selWebApp.Name + "---faild");
                    }
                }
                else
                {
                    //To deactive the selected webapp
                    if (SetWebAppEM(selWebApp, selectedSiteIDs, false))
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "CentralAdmin to deactive Nextlabs Sharepoint Entilement Management on WebAPP---" + selWebApp.Name + "---successfully");
                    }
                    else
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "CentralAdmin to deactive Nextlabs Sharepoint Entilement Management on WebAPP---" + selWebApp.Name + "---faild");
                    }
                }
                bool bRet = CommonLib.CheckFeaturesAndEvents(selWebApp, selectedSiteIDs, bActive, activedSiteIDs);
                Progress.WriteLog(selWebApp.Name, "92|Actived site collections IDs:" + string.Join(",", activedSiteIDs.ToArray()));
                //update default and activated site collection in batch mode;
                string strByDefault = bActive ? "Set Activate Policy Enforcement:" : "Set Deactivate Policy Enforcement:";
                Progress.WriteLog(selWebApp.Name, "93|" + strByDefault);
                selWebApp.UpdateSiteCollectionsStatus(selectedSiteIDs, bActive);
                Progress.WriteLog(selWebApp.Name, "95|Updated site collections status");

                // Set activated site ids to webapp property.
                Globals.SetActivatedSiteIds(selWebApp, string.Join(SiteIdSplit, activedSiteIDs));
                if (bRet)
                {
                    Progress.WriteLog(selWebApp.Name, "100|" + strAction + " successfully at " + DateTime.Now.ToString());
                }
                else
                {
                    Progress.WriteLog(selWebApp.Name, "100|" + strAction + " failed on some sites at " + DateTime.Now.ToString() + ". Please click “Update” button to " + cellAction + " failed sites again. Click here to view detail information.");
                }
            }
            catch (Exception ex)
            {
                Progress.WriteLog(selWebApp.Name, "Exception:" + ex.Message);
                Progress.WriteLog(selWebApp.Name, "Exception:" + ex.StackTrace);
                Progress.WriteLog(selWebApp.Name, "100|Update to " + strAction + " failed at " + DateTime.Now.ToString());
            }
            finally
            {
                Progress.WriteLog(selWebApp.Name, "END");
            }
        }

        protected void SetJavaPCParams()
        {
            try
            {
                //javapc checkbox
                SPWebApplication spAdminWebApp = null;
                if (Globals.GetAdministrationWebApplication(ref spAdminWebApp) && spAdminWebApp != null)
                {
                    if (InputFormCheckBoxUseJpc.Checked)
                    {
                        spAdminWebApp.Properties[Globals.strGlobalJavaPCPropertyName] = Globals.strGlobalEnabled;
                        //Javapc params
                        {
                            spAdminWebApp.Properties[Globals.strGlobalJavaPCHost] = InputFormTextBoxJavaPcHost.Text;
                            spAdminWebApp.Properties[Globals.strGlobalJavaPCClientID] = InputFormTextBoxClientID.Text;
                            spAdminWebApp.Properties[Globals.strGlobalJavaPCClientSecureKey] = InputFormTextBoxClientSecureKey.Text;
                        }
                    }
                    else
                    {
                        spAdminWebApp.Properties[Globals.strGlobalJavaPCPropertyName] = Globals.strGlobalDisabled;
                    }
                    spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHHost] = InputFormTextBoxOAUTHHost.Text;
                    if (spAdminWebApp.Properties.ContainsKey(Globals.strGlobalJavaPCAUTHUserName))
                    {
                        spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHUserName] = InputFormTextBoxOAUTHHostUserName.Text;
                    }
                    else
                    {
                        spAdminWebApp.Properties.Add(Globals.strGlobalJavaPCAUTHUserName, InputFormTextBoxOAUTHHostUserName.Text);
                    }
                    if (spAdminWebApp.Properties.ContainsKey(Globals.strGlobalJavaPCAUTHPwd))
                    {
                        spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHPwd] = InputFormTextBoxOAUTHHostPassword.Text;
                    }
                    else
                    {
                        spAdminWebApp.Properties.Add(Globals.strGlobalJavaPCAUTHPwd, InputFormTextBoxOAUTHHostPassword.Text);
                    }
                    spAdminWebApp.Update();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SetJavaPCParams:", null, ex);
            }
        }

        protected void UpdateButton_Click(object sender, EventArgs e)
        {
            //save selected value in query string(remove current query string when building the new url)
            string newUrl = string.Format("{0}?{1}={2}", Context.Request.Url.GetLeftPart(UriPartial.Path), QUERYSTRING_SELECTED_KEY, WebAppDropDown.SelectedValue);
            if (String.IsNullOrEmpty(WebAppDropDown.SelectedValue))
            {
                Response.Redirect(this.Context.Request.Url.ToString());
                return;
            }
            List<string> selectedSiteIDs = new List<string>();
            //To set the selected site colleciton EM's switch
            foreach (TreeNode webAppNode in FeatureTree.Nodes)
            {
                if (webAppNode.Value.Equals(WebAppDropDown.SelectedValue))
                {
                    foreach (TreeNode sitecollectionNode in webAppNode.ChildNodes)
                    {
                        if (String.IsNullOrEmpty(sitecollectionNode.Value))
                        {
                            Response.Redirect(this.Context.Request.Url.ToString());
                            return;
                        }
                        if (sitecollectionNode.Checked)
                        {
                            selectedSiteIDs.Add(sitecollectionNode.Value);
                        }
                    }
                    break;
                }
            }
            UpdateEnforce(WebAppDropDown.SelectedValue, OptionCheckBox.Checked, selectedSiteIDs);

            //set Global set of ProcessUpload property
            SetGlobalValueOfProcessUpload();

            SetJavaPCParams();

            //To avoid the submit due to a refresh of the page
            Response.Redirect(newUrl);

        }

        protected void SetGlobalValueOfProcessUpload()
        {
            try
            {
                //get value
                string strGloalValueOfProcessUpload = GlobalProcessUploadBatchMode.Checked ? Globals.strGlobalProcessUploadPropValueDisable: (GlobalProcessUploadRealTime.Checked ? Globals.strGlobalProcessUploadPropValueEnable : Globals.strGlobalProcessUploadPropValueNone);

                //set value
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];

                //Set "NewSiteCheckBox" status after update
                if (NewSiteCheckBox.Visible)
                {
                    string value = NewSiteCheckBox.Checked ? Globals.strGlobalEnabled : Globals.strGlobalDisabled;
                    Globals.SetNewSitePEDefault(selWebApp, value);
                }

                selWebApp.Properties[Globals.strGlobalProcessUploadPropName] = strGloalValueOfProcessUpload;
                selWebApp.Update();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SetGlobalValueOfProcessUpload:", null, ex);
            }

        }

        protected void ReturnButton_Click(object sender, EventArgs e)
        {
            string CAURL = this.Context.Request.Url.ToString().Replace(this.Context.Request.Url.AbsolutePath, "");
            SPUtility.Redirect(CAURL, SPRedirectFlags.Trusted, this.Context);
        }

        protected void OptionCheckBoxClick(object sender, EventArgs e)
        {
            try
            {
                NewSiteCheckBox.Visible = true;
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                string value = Globals.GetActivatedSiteIds(selWebApp);
                NewSiteCheckBox.Checked = !value.Equals(Globals.strGlobalDisabled);
            }
            catch
            { }
        }

        protected void DeactivateCheckBoxClick(object sender, EventArgs e)
        {
            if (DeactivateCheckBox.Checked)
            {
                NewSiteCheckBox.Visible = false;
            }
        }

#endregion
    }

}