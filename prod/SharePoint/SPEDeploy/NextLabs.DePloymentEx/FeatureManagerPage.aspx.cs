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
using NextLabs.Diagnostic;

namespace NextLabs.Deployment
{
    public class FeatureManager : LayoutsPageBase
    {
        #region VARIABLES

        protected DropDownList WebAppDropDown;
        protected DropDownList WebFeatureDropDown;
        protected Label FeatureTitleLabel;
        protected Label FeatureStatusLabel;
        protected Label FeatureDescriptionLabel;
        protected Label Warning;
        protected Image FeatureImage;
        protected Image WarningImage;
        protected TreeView FeatureTree;
        protected Table StatusTable;
        protected InputFormSection CentralAdminSection;
        protected InputFormSection ScopeSection;
        protected InputFormSection UpdateSection;
        protected InputFormSection OptionsSection;
        protected InputFormSection FeatureSection;
        protected InputFormControl NavigationControl;
        protected ImageButton ParentSiteImageButton;
        protected LinkButton ParentSiteLinkButton;
        protected ImageButton RootSiteImageButton;
        protected LinkButton RootSiteLinkButton;
        protected Button EnableButton;
        protected Button DisableButton;
        protected Button ClearButton;
        protected CheckBox ShowHiddenFeaturesCB;
        protected InputFormRadioButton WebScopeRadioButton;
        protected InputFormRadioButton SiteScopeRadioButton;
        protected InputFormRadioButton WebAppRadioButton;
        protected Literal PageDescription;
        protected HyperLink ExpandCollapseTreeHyperlink;
        private ArrayList expandedNodes = new ArrayList();
        private Guid EventReceiverID = new Guid(CommonLib.NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID);
        private Guid EntitlementBasicID = new Guid(CommonLib.NEXTLABSENTITLEMENTBASICFEATUREID);
        private Guid EntitlementAdministrationID = new Guid(CommonLib.NEXTLABSENTITLEMENTADMINISTRATIONFEATUREID);

        #endregion

        #region LOAD

        protected void Page_Load(object sender, EventArgs e)
        {
            this.ClientScript.RegisterStartupScript(this.GetType(), "StartFeatureStyling", "StyleFeatureList();", true);
            this.ExpandCollapseTreeHyperlink.NavigateUrl = "javascript:ToggleExpandCollapse('" + this.FeatureTree.ClientID + "');";
            this.ExpandCollapseTreeHyperlink.ToolTip = "Click to Expand/Collapse all nodes in Tree";

            SPWeb currentWeb = SPContext.Current.Web;
            PageDescription.Text = "Administration of Nextlabs Features for Export/Backup";
            Warning = new Label();
            WarningImage = new Image();
            if (!Page.IsPostBack)
            {
                if (currentWeb != null)
                {
                    LoadTreeView();
                    UpdateSection.Visible = false;
                }
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

        private void CheckCurrentStatus()
        {
            if (IsCentralAdmin())
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SPFarm farm = SPFarm.Local;
                    SPWebService spws = farm.Services.GetValue<SPWebService>("");
                    bool hasBeenDisable = false;
                    foreach (SPWebApplication webApp in spws.WebApplications)
                    {
                        //To check if is there  save the nextlabs feature status before.
                        if (webApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
                        {
                            //Set the scop
                            WebAppRadioButton.Checked = true;
                            SiteScopeRadioButton.Enabled = false;
                            WebScopeRadioButton.Enabled = false;
                            DisableButton.Enabled = false;
                            hasBeenDisable = true;
                        }
                        else if (webApp.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
                        {
                            CentralAdminSection.Visible = true;
                            LoadWebAppDropDown();
                            //Set the scop
                            SiteScopeRadioButton.Checked = true;
                            WebAppRadioButton.Enabled = false;
                            WebScopeRadioButton.Enabled = false;
                            //Set the webapp
                            WebAppDropDown.Items.FindByValue(webApp.Id.ToString()).Selected = true;
                            WebAppDropDown.Enabled = false;
                            DisableButton.Enabled = false;
                            hasBeenDisable = true;
                        }
                        else if (webApp.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
                        {
                            CentralAdminSection.Visible = true;
                            LoadWebAppDropDown();
                            //Set the scop
                            WebScopeRadioButton.Checked = true;
                            SiteScopeRadioButton.Enabled = false;
                            WebAppRadioButton.Enabled = false;
                            //Set the webapp
                            WebAppDropDown.Items.FindByValue(webApp.Id.ToString()).Selected = true;
                            WebAppDropDown.Enabled = false;
                            DisableButton.Enabled = false;
                            hasBeenDisable = true;
                        }
                        else
                        { }
                    }
                    if (!hasBeenDisable)
                    {
                        EnableButton.Enabled = false;
                    }
                });
            }
        }

        private void LoadFeatureDropDown()
        {
            WebFeatureDropDown.Items.Clear();
            SPFeatureScope currentScope = GetCurrentFeatureScope();
            //There is no site scope Nextlabs feature now, just skip it.
            if (currentScope == SPFeatureScope.Site)
            {
                return;
            }
            ArrayList features = new ArrayList();
            ArrayList Nextlabsfeatures = new ArrayList();

            //NextLabs.Entitlement.EventReceiver
            SPFeatureDefinition def = SPFarm.Local.FeatureDefinitions[EventReceiverID];
            Nextlabsfeatures.Add(def);
            //NextLabs.Entitlement.Basic
            def = SPFarm.Local.FeatureDefinitions[EntitlementBasicID];
            Nextlabsfeatures.Add(def);
            //NextLabs.Entitlement.Administration
            def = SPFarm.Local.FeatureDefinitions[EntitlementAdministrationID];
            Nextlabsfeatures.Add(def);
            foreach (SPFeatureDefinition featureDef in Nextlabsfeatures)
            {
                if (featureDef.Scope == currentScope && (!featureDef.Hidden || ShowHiddenFeaturesCB.Checked))
                {
                    features.Add(featureDef);
                }
            }

            foreach (SPFeatureDefinition featureDef in features)
            {
                string title = featureDef.GetTitle(System.Globalization.CultureInfo.CurrentCulture);
                if(featureDef.Hidden) title += " *";
                string id = featureDef.Id.ToString();
                WebFeatureDropDown.Items.Add(new ListItem(title, id));
            }

            string selFeatureID = Request.QueryString["FeatureID"] != null ? Request.QueryString["FeatureID"].ToString() : string.Empty;

            if (!string.IsNullOrEmpty(selFeatureID))
            {
                ListItem selItem = WebFeatureDropDown.Items.FindByValue(selFeatureID);
                if (selItem != null) selItem.Selected = true;
            }
        }

        private void LoadFeatureInfo()
        {
            Guid selGuid = new Guid(WebFeatureDropDown.SelectedValue.ToString());
            SPFeatureDefinition fDef = SPFarm.Local.FeatureDefinitions[selGuid];

            if (fDef != null)
            {
                string title = fDef.GetTitle(System.Globalization.CultureInfo.CurrentCulture);
                string desc = fDef.GetDescription(System.Globalization.CultureInfo.CurrentCulture);
                string imgUrl = fDef.GetImageUrl(System.Globalization.CultureInfo.CurrentCulture);
                FeatureTitleLabel.Text = title;
                FeatureDescriptionLabel.Text = desc;

                if (imgUrl != null)
                {
                    FeatureImage.ImageUrl = "/_layouts/images/" + imgUrl;
                }
                else
                {
                    FeatureImage.ImageUrl = "/_layouts/images/GenericFeature.gif";
                }
            }
        }

        private bool CheckWebAppIsSelected(SPWebApplication webApp)
        {
            if (webApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
            {
                return true;
            }
            return false;
        }

        private bool CheckSiteCollectionIsSelected(SPSite site)
        {
            if (site.WebApplication.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
            {
                string siteCollectionStr = site.WebApplication.Properties["NextlabsFeature_SiteCollection_Status"].ToString();
                string[] sites = siteCollectionStr.Split(new char[] { '#' });
                for (int i = 0; i < sites.Length - 1; ++i)
                {
                    string siteStr = sites[i];
                    string[] webs = siteStr.Split(new char[] { '$' });
                    string siteID = webs[0];
                    if (siteID.Equals(site.ID.ToString()))
                    {
                       return true;
                    }
                }
            }
            return false;
        }

        private bool CheckWebIsSelected(SPWeb web)
        {
            bool ret = false;
            if (web.Site.WebApplication.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    string webStr = "";
                    webStr = web.Site.WebApplication.Properties["NextlabsFeature_Webs_Status"].ToString();
                    string[] counts = webStr.Split(new char[] { '#' });
                    for (int i = 0; i < counts.Length - 1; ++i)
                    {
                        string webstr = counts[i];
                        string[] webs = webstr.Split(new char[] { '$' });
                        string WebID = webs[1];
                        if (WebID.EndsWith("Active"))
                        {
                            string[] webid = WebID.Split(new char[] { '&' });
                            WebID = webid[0];
                        }
                        if (WebID.Equals(web.ID.ToString()))
                        {
                            ret = true;
                            break;
                        }
                    }
                });
            }
            return ret;
        }

        private void LoadTreeView()
        {
            CheckCurrentStatus();
            SaveExpandedNodes();

            if (IsCentralAdmin())
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    FeatureTree.Nodes.Clear();
                    SPFarm farm = SPFarm.Local;
                    SPWebService spws = farm.Services.GetValue<SPWebService>("");
                    if (GetCurrentFeatureScope() == SPFeatureScope.WebApplication)
                    {
                        SPSolution installedSolution = farm.Solutions[new Guid(CommonLib.NEXTLABSWSPGUID)];
                        foreach (SPWebApplication webApp in installedSolution.DeployedWebApplications)
                        {
                            if (!webApp.IsAdministrationWebApplication)
                            {
                                TreeNode node = new TreeNode(webApp.Name + " - " + webApp.GetResponseUri(SPUrlZone.Default).ToString(), webApp.Id.ToString());
                                node.Value = webApp.Id.ToString();
                                node.ShowCheckBox = true;
                                Warning.Visible = false;
                                WarningImage.Visible = false;
                                if (!DisableButton.Enabled)
                                {
                                    node.ShowCheckBox = false;
                                    if (CheckWebAppIsSelected(webApp))
                                    {
                                        node.ImageUrl = CommonLib.GIFACTIVEURL;
                                    }
                                    Warning.Visible = true;
                                    WarningImage.Visible = true;
                                }
                                FeatureTree.Nodes.Add(node);
                            }
                        }
                        return;
                    }
                    else
                    {
                        SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                        TreeNode webAppNode = new TreeNode(selWebApp.Name + " - " + selWebApp.GetResponseUri(SPUrlZone.Default).ToString(), "WEBAPP#" + selWebApp.Id.ToString());
                        webAppNode.NavigateUrl = "javascript:CheckUncheckTree();";
                        webAppNode.ToolTip = "Click to select/unselect Site Collections (including SubSites)";
                        webAppNode.ShowCheckBox = false;
                        webAppNode.Expand();
                        foreach (SPSite site in selWebApp.Sites)
                        {
                            using (site)
                            {
                                SPFeatureScope scope = GetCurrentFeatureScope();
                                TreeNode siteRootNode = AddWebs(scope, site.RootWeb);
                                siteRootNode.ShowCheckBox = true;
                                Warning.Visible = false;
                                WarningImage.Visible = false;
                                if (!DisableButton.Enabled)
                                {
                                    siteRootNode.ShowCheckBox = false;
                                    if (CheckSiteCollectionIsSelected(site))
                                    {
                                        siteRootNode.ImageUrl = CommonLib.GIFACTIVEURL;
                                    }
                                    Warning.Visible = true;
                                    WarningImage.Visible = true;
                                }
                                webAppNode.ChildNodes.Add(siteRootNode);
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

        private TreeNode AddWebs(SPFeatureScope scope, SPWeb web)
        {
            string nodeID = GetFullNodeID(web);
            bool centralAdmin = IsCentralAdmin();
            TreeNode currentWebNode = new TreeNode();
            if (web.IsRootWeb && centralAdmin)
            {
                currentWebNode.Text = web.Title + " (Site Collection)";
            }
            else
            {
                currentWebNode.Text = web.Title;
            }

            currentWebNode.Value = nodeID;
            if (centralAdmin)
            {
                currentWebNode.Target = "_blank";
            }
            currentWebNode.ShowCheckBox = true;
            Warning.Visible = false;
            WarningImage.Visible = false;
            if (!DisableButton.Enabled)
            {
                currentWebNode.ShowCheckBox = false;
                if (CheckWebIsSelected(web))
                {
                    currentWebNode.ImageUrl = CommonLib.GIFACTIVEURL;
                }
                Warning.Visible = true;
                WarningImage.Visible = true;
            }

            if (scope == SPFeatureScope.Web)
            {
                foreach (SPWeb subWeb in web.Webs)
                {
                    try
                    {
                        currentWebNode.ChildNodes.Add(AddWebs(scope, subWeb));
                    }
                    finally
                    {
                        if (subWeb != null) subWeb.Dispose();
                    }
                }
            }

            if (expandedNodes.Contains(nodeID))
            {
                currentWebNode.Expand();
            }
            currentWebNode.ImageToolTip = currentWebNode.ToolTip;

            return currentWebNode;
        }

        #endregion

        #region UTILITIES

        private bool IsCentralAdmin()
        {
            return SPContext.Current.Site.WebApplication.IsAdministrationWebApplication;
        }

        private SPFeatureScope GetCurrentFeatureScope()
        {
            if(WebScopeRadioButton.Checked)
            {
                return  SPFeatureScope.Web;
            }
            else if (SiteScopeRadioButton.Checked)
            {
                return SPFeatureScope.Site;
            }
            else
            {
                return SPFeatureScope.WebApplication;
            }
        }

        private string GetFullNodeID(SPWeb web)
        {
            return web.Site.ID.ToString() + "|" + web.ID.ToString();
        }

        private bool IsMissingDependantFeatures(SPWeb web, Guid featureDefID, out ArrayList missingFeatures)
        {
            SPFeatureDefinition featureDef = SPFarm.Local.FeatureDefinitions[featureDefID];

            bool missing = false;
            missingFeatures = new ArrayList();

            SPFeatureDependencyCollection activationDependencies = featureDef.ActivationDependencies;

            foreach (SPFeatureDependency featDep in activationDependencies)
            {
                SPFeatureDefinition dependantFeatureDefinition = SPFarm.Local.FeatureDefinitions[featDep.FeatureId];

                if (!dependantFeatureDefinition.Hidden)
                {
                    SPFeature feature = null;

                    switch (dependantFeatureDefinition.Scope)
                    {
                        case SPFeatureScope.Web: feature = web.Features[dependantFeatureDefinition.Id]; break;
                        case SPFeatureScope.Site: feature = web.Site.Features[dependantFeatureDefinition.Id]; break;
                        default: break;
                    }

                    if (feature == null)
                    {
                        missingFeatures.Add(dependantFeatureDefinition);
                        missing = true;
                    }
                }
            }

            return missing;
        }

        private bool IsValidWeb(SPWeb web)
        {
            return true;
        }

        private void SaveWebNextlabsFeatureStatus(SPWeb selWeb)
        {
            string siteStr = "";
            siteStr += selWeb.Site.ID.ToString();
            siteStr += "$";

            siteStr += selWeb.ID.ToString();
            if (IsFeatureActive(selWeb, EventReceiverID))
            {
                siteStr += "&Active";
            }
            siteStr += "#";

            if (!selWeb.Site.WebApplication.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
            {
                    selWeb.Site.WebApplication.Properties.Add("NextlabsFeature_Webs_Status", siteStr);
            }
            else
            {
                    selWeb.Site.WebApplication.Properties["NextlabsFeature_Webs_Status"] += siteStr;
            }
        }

        private void SaveSiteCollectionNextlabsFeatureStatus(SPSite selSite)
        {
            string siteStr = "";
            string webStr = "";

            siteStr += selSite.ID.ToString();
            siteStr += "$";
            foreach (SPWeb web in selSite.AllWebs)
            {
                using (web)
                {
                    webStr += web.ID.ToString();
                    if (IsFeatureActive(web, EventReceiverID))
                    {
                        webStr += "&Active";
                    }
                    webStr += "$";
                }
            }
            siteStr += webStr;
            siteStr += "#";

            if (!selSite.WebApplication.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
            {
                selSite.WebApplication.Properties.Add("NextlabsFeature_SiteCollection_Status", siteStr);
            }
            else
            {
                selSite.WebApplication.Properties["NextlabsFeature_SiteCollection_Status"] += siteStr;
            }
        }

        private void SaveWebAppNextlabsFeatureStatus(SPWebApplication selWebApp)
        {
            string appStr = "";
            string siteStr = "";
            string webStr = "";
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                foreach (SPSite site in selWebApp.Sites)
                {
                    using (site)
                    {
                        siteStr = "";
                        siteStr += site.ID.ToString();
                        siteStr += "$";
                        webStr = "";
                        try
                        {
                            foreach (SPWeb web in site.AllWebs)
                            {
                                using (web)
                                {
                                    webStr += web.ID.ToString();
                                    // Nextlabs Entitlement EventReceiver feature
                                    if (IsFeatureActive(web, EventReceiverID))
                                    {
                                        webStr += "&Active";
                                    }
                                    webStr += "$";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Error, "Exception during SaveWebAppNextlabsFeatureStatus:", null, ex);
                        }
                        siteStr += webStr;
                        siteStr += "#";
                        appStr += siteStr;
                    }
                }
            });

            if (!selWebApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
            {
                selWebApp.Properties.Add("NextlabsFeature_WebAPP_Status", appStr);
            }
            else
            {
                selWebApp.Properties["NextlabsFeature_WebAPP_Status"] = appStr;
            }
        }

        private void RestoreWebAppNextlabsFeatureStatus(SPWebApplication selWebApp)
        {
            try
            {
                string appStr = "";
                if (!selWebApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
                {
                    return;
                }
                else
                {
                    appStr = selWebApp.Properties["NextlabsFeature_WebAPP_Status"].ToString();
                }

                string[] sites = appStr.Split(new char[] { '#' }); //The last one is "", so should skip it.

                for (int i = 0; i < sites.Length - 1; ++i)
                {
                    try
                    {
                        string siteStr = sites[i];
                        string[] webs = siteStr.Split(new char[] { '$' });
                        Guid siteID = new Guid(webs[0]);

                        if (siteID != Guid.Empty && webs.Length > 1)
                        {
                            SPSecurity.RunWithElevatedPrivileges(delegate()
                            {
                                using (SPSite site = new SPSite(siteID))
                                {
                                    for (int j = 1; j < webs.Length - 1; ++j)
                                    {
                                        try
                                        {
                                            string webStr = webs[j];
                                            //TO deactivate the feature
                                            if (!webStr.EndsWith("Active"))
                                            {
                                                Guid webID = new Guid(webStr);
                                                using (SPWeb web = site.OpenWeb(webID))
                                                {
                                                    web.AllowUnsafeUpdates = true;
                                                    SPFeature activeWebFeature = web.Features[EventReceiverID];
                                                    if (activeWebFeature != null)
                                                    {
                                                        try
                                                        {
                                                            web.Features.Remove(EventReceiverID);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            NLLogger.OutputLog(LogLevel.Debug, "Exception during RestoreWebAppNextlabsFeatureStatus:", null, ex);
                                                        }
                                                    }
                                                    web.AllowUnsafeUpdates = false;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            });
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private void RestoreSiteCollectionNextlabsFeatureStatus(SPWebApplication selWebApp)
        {
            try
            {
                string siteCollectionStr = "";
                if (!selWebApp.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
                {
                    return;
                }
                else
                {
                    siteCollectionStr = selWebApp.Properties["NextlabsFeature_SiteCollection_Status"].ToString();
                }

                string[] sites = siteCollectionStr.Split(new char[] { '#' });

                // Nextlabs Entitlement EventReceiver feature
                for (int i = 0; i < sites.Length - 1; ++i)
                {
                    try
                    {
                        string siteStr = sites[i];
                        string[] webs = siteStr.Split(new char[] { '$' });
                        Guid siteID = new Guid(webs[0]);

                        if (siteID != Guid.Empty && webs.Length > 1)
                        {
                            SPSecurity.RunWithElevatedPrivileges(delegate()
                            {
                                using (SPSite site = new SPSite(siteID))
                                {
                                    for (int j = 1; j < webs.Length - 1; ++j)
                                    {
                                        try
                                        {
                                            string webStr = webs[j];
                                            if (webStr.EndsWith("Active"))
                                            {
                                                string[] webid = webStr.Split(new char[] { '&' });
                                                Guid webID = new Guid(webid[0]);
                                                using (SPWeb web = site.OpenWeb(webID))
                                                {
                                                    web.AllowUnsafeUpdates = true;
                                                    SPFeature activeWebFeature = web.Features[EventReceiverID];
                                                    if (activeWebFeature == null)
                                                    {
                                                        try
                                                        {
                                                            web.Features.Add(EventReceiverID);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            NLLogger.OutputLog(LogLevel.Debug, "Exception during RestoreSiteCollectionNextlabsFeatureStatus:", null, ex);
                                                        }
                                                    }
                                                    web.AllowUnsafeUpdates = false;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            });
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private void RestoreWebsNextlabsFeatureStatus(SPWebApplication selWebApp)
        {
            try
            {
                string webStr = "";
                webStr = selWebApp.Properties["NextlabsFeature_Webs_Status"].ToString();
                string[] counts = webStr.Split(new char[] { '#' });
                for (int i = 0; i < counts.Length - 1; ++i)
                {
                    string webstr = counts[i];
                    string[] webs = webstr.Split(new char[] { '$' });
                    Guid siteID = new Guid(webs[0]);

                    if (siteID != Guid.Empty)
                    {
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            using (SPSite site = new SPSite(siteID))
                            {
                                string web1 = webs[1];
                                if (web1.EndsWith("Active"))
                                {
                                    string[] webid = web1.Split(new char[] { '&' });
                                    Guid webID = new Guid(webid[0]);
                                    using (SPWeb web = site.OpenWeb(webID))
                                    {
                                        web.AllowUnsafeUpdates = true;
                                        SPFeature activeWebFeature = web.Features[EventReceiverID];
                                        if (activeWebFeature == null)
                                        {
                                            try
                                            {
                                                web.Features.Add(EventReceiverID);
                                            }
                                            catch (Exception ex)
                                            {
                                                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestoreWebsNextlabsFeatureStatus:", null, ex);
                                            }
                                        }
                                        web.AllowUnsafeUpdates = false;
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch
            {
            }
        }

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

#region WEB METHODS

        private bool IsFeatureActive(SPWeb web, Guid featureID)
        {
            bool active = false;

            try
            {
                if (web != null && !featureID.Equals(Guid.Empty))
                {
                    SPFeature activeFeature = web.Features[featureID];

                    if (activeFeature != null)
                        active = true;
                    else
                        active = false;
                }
            }
            catch
            {
            }

            return active;
        }

        private void ActivateFeature(SPWeb web, Guid featureID)
        {
            if (web != null)
            {
                try
                {
                    SPFeature activeFeature = web.Features[featureID];

                    if (activeFeature == null)
                    {
                        web.Features.Add(featureID);
                    }
                }
                catch
                {
                }
            }
        }

        private void DeActivateFeature(SPWeb web, Guid featureID)
        {
            if (web != null)
            {
                try
                {
                    SPFeature activeFeature = web.Features[featureID];

                    if (activeFeature != null)
                    {
                        web.Features.Remove(activeFeature.DefinitionId);
                    }
                }
                catch
                {
                }
            }
        }

#endregion

#region SITE METHODS

        private bool IsFeatureActive(SPSite site, Guid featureID)
        {
            bool active = false;

            try
            {
                if (site != null && !featureID.Equals(Guid.Empty))
                {
                    SPFeature activeFeature = site.Features[featureID];

                    if (activeFeature != null) active = true;
                    else active = false;
                }
            }
            catch
            {
            }

            return active;
        }

        private void DeActivateFeature(SPSite site, Guid featureID)
        {
            try
            {
                if (site != null)
                {
                    foreach (SPWeb web in site.AllWebs)
                    {
                        try
                        {
                            using (web)
                            {
                                SPFeature activeFeature = web.Features[featureID];
                                if (activeFeature != null)
                                {
                                    web.Features.Remove(activeFeature.DefinitionId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Error, "Exception during DeActivateFeature:", null, ex);
                        }
                    }
                }
            }
            catch
            {
            }
        }

#endregion

#region WEB APPLICATION METHODS

        private bool IsFeatureActive(SPWebApplication webApp, Guid featureID)
        {
            bool active = false;

            try
            {
                if (webApp != null && !featureID.Equals(Guid.Empty))
                {
                    SPFeature activeFeature = webApp.Features[featureID];

                    if (activeFeature != null)
                        active = true;
                    else
                        active = false;
                }
            }
            catch
            {
            }

            return active;
        }

        private void ActivateFeature(SPWebApplication webApp, Guid featureID)
        {
            if (webApp != null)
            {
                SPFeature activeFeature = webApp.Features[featureID];

                if (activeFeature == null)
                {
                    webApp.Features.Add(featureID);
                }
            }
        }

        private void DeActivateFeature(SPWebApplication webApp, Guid featureID)
        {
            if (webApp != null)
            {
                SPFeature activeFeature = webApp.Features[featureID];

                if (activeFeature != null)
                {
                    webApp.Features.Remove(activeFeature.DefinitionId);
                }
            }
        }

#endregion

#region COMMON METHODS

        private void UpdateFeature(TreeNode node, Guid featureID)
        {
            if (!node.Value.Contains("WEBAPP"))
            {
                SPFeatureScope currentScope = GetCurrentFeatureScope();

                string[] splittedIDs = node.Value.Split(new char[] { '|' });

                Guid siteID = new Guid(splittedIDs[0]);
                Guid webID = new Guid(splittedIDs[1]);

                if (siteID != Guid.Empty && webID != Guid.Empty)
                {
                    using (SPSite site = new SPSite(siteID))
                    {
                        switch (currentScope)
                        {
                            case SPFeatureScope.Site:

                                site.AllowUnsafeUpdates = true;

                                SPFeature activeSiteFeature = site.Features[featureID];
                                if (activeSiteFeature == null && node.Checked)
                                {
                                    try
                                    {
                                        site.Features.Add(featureID);
                                        AddStatusRow(site.RootWeb.Title + " (Site Collection)", "Activated");
                                    }
                                    catch (Exception ex)
                                    {
                                        AddStatusRow(site.RootWeb.Title + " (Site Collection)", ex.Message);
                                    }
                                }
                                else if (activeSiteFeature != null && !node.Checked)
                                {
                                    site.Features.Remove(featureID);
                                    AddStatusRow(site.RootWeb.Title + " (Site Collection)", "Deactivated");
                                }

                                site.AllowUnsafeUpdates = false;

                                break;
                            case SPFeatureScope.Web:

                                using (SPWeb web = site.OpenWeb(webID))
                                {
                                    web.AllowUnsafeUpdates = true;

                                    SPFeature activeWebFeature = web.Features[featureID];
                                    if (activeWebFeature == null && node.Checked)
                                    {
                                        try
                                        {
                                            web.Features.Add(featureID);
                                            AddStatusRow(web.Title, "Activated");
                                        }
                                        catch (Exception ex)
                                        {
                                            AddStatusRow(web.Title, ex.Message);
                                        }
                                    }
                                    else if (activeWebFeature != null && !node.Checked)
                                    {
                                        web.Features.Remove(featureID);
                                        AddStatusRow(web.Title, "Deactivated");
                                    }

                                    web.AllowUnsafeUpdates = false;
                                }

                                break;
                            default:
                                break;
                        }
                    }
                }
            }


            foreach (TreeNode subNode in node.ChildNodes)
            {
                UpdateFeature(subNode, featureID);
            }
        }

#endregion

        private void AddStatusRow(string site, string change)
        {
            TableRow statusRow = new TableRow();
            TableCell siteCell = new TableCell();
            siteCell.CssClass = "ms-authoringcontrols";
            siteCell.Style.Add("vertical-align", "top");
            siteCell.Text = site;
            TableCell statusCell = new TableCell();
            statusCell.CssClass = "ms-authoringcontrols";
            statusCell.Style.Add("padding-left", "15px");
            statusCell.Text = change;
            statusRow.Cells.Add(siteCell);
            statusRow.Cells.Add(statusCell);
            StatusTable.Rows.Add(statusRow);
        }

        private void Redirect()
        {
            SPUtility.Redirect("settings.aspx", SPRedirectFlags.RelativeToLayoutsPage, this.Context);
        }

#endregion

#region EVENTS

        protected void RootSiteImageButton_Click(object sender, EventArgs e)
        {
            SPWeb currentWeb = SPContext.Current.Web;
            if (!currentWeb.IsRootWeb)
            {
                Response.Redirect(currentWeb.Site.RootWeb.Url + "/_layouts/FeatureManager/FeatureManager.aspx");
            }
        }

        protected void RootSiteLinkButton_Click(object sender, EventArgs e)
        {
            SPWeb currentWeb = SPContext.Current.Web;
            if (!currentWeb.IsRootWeb)
            {
                Response.Redirect(currentWeb.Site.RootWeb.Url + "/_layouts/FeatureManager/FeatureManager.aspx");
            }
        }

        protected void ParentSiteImageButton_Click(object sender, EventArgs e)
        {
            SPWeb currentWeb = SPContext.Current.Web;
            if (!currentWeb.IsRootWeb)
            {
                Response.Redirect(currentWeb.ParentWeb.Url + "/_layouts/FeatureManager/FeatureManager.aspx");
            }
        }

        protected void ParentSiteLinkButton_Click(object sender, EventArgs e)
        {
            SPWeb currentWeb = SPContext.Current.Web;
            if (!currentWeb.IsRootWeb)
            {
                Response.Redirect(currentWeb.ParentWeb.Url + "/_layouts/FeatureManager/FeatureManager.aspx");
            }
        }

        protected void EnableButton_Click(object sender, EventArgs e)
        {
            if (IsCentralAdmin())
            {
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPFeatureScope currentScope = GetCurrentFeatureScope();

                switch (currentScope)
                {
                    case SPFeatureScope.WebApplication:
                        foreach (TreeNode node in FeatureTree.Nodes)
                        {
                            SPWebApplication webApp = spws.WebApplications[new Guid(node.Value)];
                            if (webApp != null)
                            {
                                if (webApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
                                {
                                    try
                                    {
                                        bool formDigestSetting = webApp.FormDigestSettings.Enabled;
                                        webApp.FormDigestSettings.Enabled = false;

                                        //Active NextLabs.Entitlement.Basic.feature
                                        SPFeature activeSiteFeature = webApp.Features[EntitlementBasicID];
                                        if (activeSiteFeature == null)
                                        {
                                            webApp.Features.Add(EntitlementBasicID);
                                        }

                                        //Restore the previous status
                                        RestoreWebAppNextlabsFeatureStatus(webApp);
                                        //Remove the property
                                        webApp.Properties.Remove("NextlabsFeature_WebAPP_Status");
                                        webApp.FormDigestSettings.Enabled = formDigestSetting;
                                        webApp.Update();
                                        DisableButton.Enabled = true;
                                        EnableButton.Enabled = false;
                                    }
                                    catch(Exception ex)
                                    {
                                        NLLogger.OutputLog(LogLevel.Error, "Exception during EnableButton_Click:", null, ex);
                                    }
                                }
                            }
                        }
                        break;
                    case SPFeatureScope.Site:
                        SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                        if (selWebApp != null)
                        {
                            //To check if the web application is disabled before.
                            if (selWebApp.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
                            {
                                try
                                {
                                    bool tempFormDigestSetting = selWebApp.FormDigestSettings.Enabled;
                                    selWebApp.FormDigestSettings.Enabled = false;

                                    //Restore the previous status
                                    RestoreSiteCollectionNextlabsFeatureStatus(selWebApp);
                                    //Remove the propterty
                                    selWebApp.Properties.Remove("NextlabsFeature_SiteCollection_Status");
                                    selWebApp.FormDigestSettings.Enabled = tempFormDigestSetting;
                                    selWebApp.Update();
                                    DisableButton.Enabled = true;
                                    EnableButton.Enabled = false;
                                }
                                catch (Exception ex)
                                {
                                    NLLogger.OutputLog(LogLevel.Error, "Exception during EnableButton_Click:", null, ex);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        break;
                    case SPFeatureScope.Web:
                        SPWebApplication seltWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                        if (seltWebApp != null)
                        {
                            //To check if the web application is disabled before.
                            if (seltWebApp.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
                            {
                                try
                                {
                                    bool tempFormDigestSetting = seltWebApp.FormDigestSettings.Enabled;
                                    seltWebApp.FormDigestSettings.Enabled = false;

                                    //Restore the previous status
                                    RestoreWebsNextlabsFeatureStatus(seltWebApp);
                                    //Remove the propterty
                                    seltWebApp.Properties.Remove("NextlabsFeature_Webs_Status");
                                    seltWebApp.FormDigestSettings.Enabled = tempFormDigestSetting;
                                    seltWebApp.Update();
                                    DisableButton.Enabled = true;
                                    EnableButton.Enabled = false;
                                }
                                catch (Exception ex)
                                {
                                    NLLogger.OutputLog(LogLevel.Error, "Exception during EnableButton_Click:", null, ex);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        break;
                }
            }
            SiteScopeRadioButton.Enabled = true;
            WebAppRadioButton.Enabled = true;
            WebScopeRadioButton.Enabled = true;
            WebAppDropDown.Enabled = true;
            //TO avoid the submit due to a refresh of the page
            Response.Redirect(this.Context.Request.Url.ToString());
        }

        protected void DisableButton_Click(object sender, EventArgs e)
        {
            if (IsCentralAdmin())
            {
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPFeatureScope currentScope = GetCurrentFeatureScope();

                switch (currentScope)
                {
                    case SPFeatureScope.WebApplication:
                        foreach (TreeNode node in FeatureTree.Nodes)
                        {
                            if (node.Checked)
                            {
                                SPWebApplication webApp = spws.WebApplications[new Guid(node.Value)];
                                if (webApp != null)
                                {
                                    try
                                    {
                                        bool formDigestSetting = webApp.FormDigestSettings.Enabled;
                                        webApp.FormDigestSettings.Enabled = false;
                                        SaveWebAppNextlabsFeatureStatus(webApp);

                                        //Deactive NextLabs.Entitlement.Basic.feature
                                        SPFeature activeSiteFeature = webApp.Features[EntitlementBasicID];
                                        if (activeSiteFeature != null && node.Checked)
                                        {
                                            webApp.Features.Remove(EntitlementBasicID);
                                        }
                                        webApp.FormDigestSettings.Enabled = formDigestSetting;
                                        webApp.Update();
                                    }
                                    catch (Exception ex)
                                    {
                                        NLLogger.OutputLog(LogLevel.Error, "Exception during DisableButton_Click:", null, ex);
                                    }
                                }
                                DisableButton.Enabled = false;
                                EnableButton.Enabled = true;
                            }
                        }
                        break;
                    case SPFeatureScope.Site:
                        SPWebApplication selWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                        if (selWebApp != null)
                        {
                            try
                            {
                                bool tempFormDigestSetting = selWebApp.FormDigestSettings.Enabled;
                                selWebApp.FormDigestSettings.Enabled = false;
                                foreach (TreeNode node in FeatureTree.Nodes)
                                {
                                    foreach (TreeNode n in node.ChildNodes)
                                    {
                                        if (n.Checked)
                                        {
                                            string[] splittedIDs = n.Value.Split(new char[] { '|' });
                                            Guid siteID = new Guid(splittedIDs[0]);
                                            SPSecurity.RunWithElevatedPrivileges(delegate()
                                            {
                                                using (SPSite site = new SPSite(siteID))
                                                {
                                                    SaveSiteCollectionNextlabsFeatureStatus(site);
                                                    //Then deactive this sitcollection all nextlabs feauture.
                                                    DeActivateFeature(site, EventReceiverID);
                                                }
                                            });
                                            DisableButton.Enabled = false;
                                            EnableButton.Enabled = true;
                                        }
                                    }
                                }
                                selWebApp.FormDigestSettings.Enabled = tempFormDigestSetting;
                                selWebApp.Update();
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Error, "Exception during DisableButton_Click:", null, ex);
                            }
                        }
                        break;
                    case SPFeatureScope.Web:
                        SPWebApplication seltWebApp = spws.WebApplications[new Guid(WebAppDropDown.SelectedValue)];
                        if (seltWebApp != null)
                        {
                            try
                            {
                                bool tempFormDigestSetting = seltWebApp.FormDigestSettings.Enabled;
                                seltWebApp.FormDigestSettings.Enabled = false;
                                foreach (TreeNode node in FeatureTree.Nodes)
                                {
                                    if (node.Value.Contains(seltWebApp.Id.ToString()))
                                    {
                                        foreach (TreeNode childnode in node.ChildNodes)
                                        {
                                            TravelWeb(childnode);
                                        }
                                    }
                                }
                                seltWebApp.FormDigestSettings.Enabled = tempFormDigestSetting;
                                seltWebApp.Update();
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Error, "Exception during DisableButton_Click:", null, ex);
                            }
                        }
                        break;
                }
            }
            //TO avoid the submit due to a refresh of the page
            Response.Redirect(this.Context.Request.Url.ToString());
        }

        protected void ClearButton_Click(object sender, EventArgs e)
        {
            if (IsCentralAdmin())
            {
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                foreach (SPWebApplication webApp in spws.WebApplications)
                {
                     SPSecurity.RunWithElevatedPrivileges(delegate()
                     {
                         bool ishas = false;
                         //To check if is there  save the nextlabs feature status before.
                         if (webApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
                         {
                             webApp.Properties.Remove("NextlabsFeature_WebAPP_Status");
                             ishas = true;
                         }
                         if (webApp.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
                         {
                             webApp.Properties.Remove("NextlabsFeature_SiteCollection_Status");
                             ishas = true;
                         }
                         if (webApp.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
                         {
                             webApp.Properties.Remove("NextlabsFeature_Webs_Status");
                             ishas = true;
                         }
                         if (ishas)
                         {
                             webApp.Update();
                         }
                     });
                }
                DisableButton.Enabled = true;
                EnableButton.Enabled = true;
            }
        }

        protected void ShowHiddenFeaturesCB_CheckedChanged(object sender, EventArgs e)
        {
            LoadFeatureDropDown();
            LoadTreeView();
            LoadFeatureInfo();
            UpdateSection.Visible = false;
            ShowHiddenFeaturesCB.Focus();
        }

        protected void WebAppDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTreeView();
            UpdateSection.Visible = false;
            WebAppDropDown.Focus();
        }

        protected void WebFeatureDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTreeView();
            LoadFeatureInfo();
            UpdateSection.Visible = false;
            WebFeatureDropDown.Focus();
        }

        protected void WebScopeRadioButton_CheckedChanged(object sendter, EventArgs e)
        {
            CentralAdminSection.Visible = true;
            LoadWebAppDropDown();
            LoadTreeView();
            WebScopeRadioButton.Focus();
        }

        protected void SiteScopeRadioButton_CheckedChanged(object sendter, EventArgs e)
        {
            CentralAdminSection.Visible = true;
            LoadWebAppDropDown();
            LoadTreeView();
            SiteScopeRadioButton.Focus();
        }

        protected void WebAppRadioButton_CheckedChanged(object sendter, EventArgs e)
        {
            CentralAdminSection.Visible = false;
            LoadTreeView();
            WebAppRadioButton.Focus();
        }

#endregion

        private void TravelWeb(TreeNode treeNode)
        {
            if (treeNode == null)
            {
                return;
            }

            string[] splittedIDs = treeNode.Value.Split(new char[] { '|' });
            Guid siteID = new Guid(splittedIDs[0]);
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(siteID))
                {
                    if (treeNode.Checked)
                    {
                        string[] splittedID = treeNode.Value.Split(new char[] { '|' });
                        Guid webID = new Guid(splittedID[1]);
                        try
                        {
                            using (SPWeb web = site.OpenWeb(webID))
                            {
                                SaveWebNextlabsFeatureStatus(web);
                                //Then deactive nextlabs feauture.
                                DeActivateFeature(web, EventReceiverID);
                                DisableButton.Enabled = false;
                                EnableButton.Enabled = true;
                            }
                        }
                        catch(Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Error, "Exception during TravelWeb:", null, ex);
                        }
                    }
                }
            });

            foreach (TreeNode childnode in treeNode.ChildNodes)
            {
                TravelWeb(childnode);
            }
        }
    }

}