using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server.Search.Administration;
using NextLabs.Common;


namespace NextLabs.Deployment
{
    public class SearchResultTrimmingPage : LayoutsPageBase
    {
        protected DropDownList SSA_Name;
        protected DropDownList CR_URL;
        protected Microsoft.SharePoint.WebControls.InputFormTextBox Identifier;
        protected EncodedLiteral ExcuteStatus;
#if SP2016 || SP2019
        const string SEARCHSERVICENAME = "OSearch16";
#elif SP2010
        const string SEARCHSERVICENAME = "OSearch14";
#else
        const string SEARCHSERVICENAME = "OSearch15";
#endif

        SPTrimming searchResultTrimming;

        private void LoadSSADropDown()
        {
            SSA_Name.Items.Clear();
            if (IsCentralAdmin())
            {
                SPFarm farm = SPFarm.Local;

                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPFarm localFarm = SPFarm.Local;
                SearchService searchService = localFarm.Services.GetValue<SearchService>(SEARCHSERVICENAME);

#if SP2013 || SP2016 || SP2019
                foreach (SearchServiceApplication searchserviceApp in searchService.SearchServiceApplications)
                {
                    SSA_Name.Items.Add(new ListItem(searchserviceApp.Name, searchserviceApp.Name));
                }
#else
               foreach (SearchServiceApplication searchserviceApp in searchService.SearchApplications)
               {
                   SSA_Name.Items.Add(new ListItem(searchserviceApp.Name, searchserviceApp.Name));
               }
#endif

            }
            else
            {
                SSA_Name.Items.Add(new ListItem("", ""));
            }
        }

        private void LoadCRUDropDown()
        {
            CR_URL.Items.Clear();
            if (IsCentralAdmin() && !String.IsNullOrEmpty(SSA_Name.SelectedValue))
            {
                SPFarm farm = SPFarm.Local;
                SPWebService spws = farm.Services.GetValue<SPWebService>("");
                SPFarm localFarm = SPFarm.Local;
                SearchService searchService = localFarm.Services.GetValue<SearchService>(SEARCHSERVICENAME);
                SearchServiceApplication searchApp = null;
#if SP2013 || SP2016 || SP2019
                foreach (SearchServiceApplication ssa in searchService.SearchServiceApplications)
                {
                    if (ssa.Name.Equals(SSA_Name.SelectedValue))
                    {
                        searchApp = ssa;
                    }
                }
#else
                searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>(SSA_Name.SelectedValue);
#endif
                Microsoft.Office.Server.Search.Administration.Content content = new Microsoft.Office.Server.Search.Administration.Content(searchApp);
                CrawlRuleCollection rules = content.CrawlRules;
                foreach (CrawlRule crawRule in rules)
                {
                    CR_URL.Items.Add(new ListItem(crawRule.Path, crawRule.Path));
                }

                bool isInstalled = false;
                int trimID = -1;
                isInstalled = searchResultTrimming.IsInstalledOnURL(SSA_Name.Text, ref trimID, CR_URL.Text);
                if (isInstalled)
                {
                    ExcuteStatus.Text = string.Format("This crawl rule path has been mapped to a security trimmer with ID {0:D}, if to use the same id the previous trimmer will be overwritten.", trimID);
                    Identifier.Text = trimID.ToString();
                }
            }
            else
            {
                CR_URL.Items.Add(new ListItem("", ""));
            }
        }


        private bool IsCentralAdmin()
        {
            return SPContext.Current.Site.WebApplication.IsAdministrationWebApplication;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                if (IsCentralAdmin())
                {
                    LoadSSADropDown();
                    LoadCRUDropDown();
                }
                else
                {
                    ExcuteStatus.Text = "Do not have the permission!";
                }
            }
        }

        public SearchResultTrimmingPage()
        {
            searchResultTrimming = new SPTrimming();
            ExcuteStatus = new EncodedLiteral();
            Identifier = new InputFormTextBox();

        }

        protected void BtnOK_Click(object sender, EventArgs e)
        {
            if (!IsCentralAdmin())
            {
                ExcuteStatus.Text = "Do not have the permission!";
                return;
            }
            int id = 0;
            if (string.IsNullOrEmpty(Identifier.Text))
            {
                id = 100;
            }
            else
            {
                id = Convert.ToInt32(Identifier.Text);
            }

            if (String.IsNullOrEmpty(SSA_Name.SelectedValue))
            {
                ExcuteStatus.Text = "Error the SSA name is null.";
                return;
            }

            int ret = 0;
            searchResultTrimming.InstallSearchResultTrimming(SSA_Name.Text, id, CR_URL.Text);
            if (ret != 0)
            {
                ExcuteStatus.Text = "Install fail!";
            }
#if SP2013 || SP2016 || SP2019
            ExcuteStatus.Text = "Install complete, please use commond line 'net stop SPSearchHostController' and 'net start SPSearchHostController' to restart the Search Service Host.";
#else
            ExcuteStatus.Text = "Install complete, please use commond line 'net stop OSearch14' and 'net start OSearch14' to restart the Search Service.";
#endif
        }

        protected void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (!IsCentralAdmin())
            {
                ExcuteStatus.Text = "Do not have the permission!";
                return;
            }
            int id = 0;
            if (String.IsNullOrEmpty(SSA_Name.SelectedValue))
            {
                ExcuteStatus.Text = "Error the SSA name is null.";
                return;
            }
            if (string.IsNullOrEmpty(Identifier.Text))
            {
                ExcuteStatus.Text = "Please input the ID.";
                return;
            }
            id = Convert.ToInt32(Identifier.Text);

            int ret = 0;
            ret = searchResultTrimming.UninstallSearchResultTrimming(SSA_Name.Text, id, CR_URL.Text);
            if (ret != 0)
            {
                ExcuteStatus.Text = "Uninstall fail.";
            }

            ExcuteStatus.Text = "Uninstall success.";

        }

        protected void SSA_Name_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCRUDropDown();
            SSA_Name.Focus();
        }

        protected void CR_URL_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isInstalled = false;
            int trimID = -1;
            ExcuteStatus.Text = "";
            Identifier.Text = "";
            isInstalled = searchResultTrimming.IsInstalledOnURL(SSA_Name.Text, ref trimID, CR_URL.Text);
            if (isInstalled)
            {
                ExcuteStatus.Text = string.Format("This crawl rule path has been mapped to a security trimmer with ID {0:D}, if to use the same id the previous trimmer will be overwritten.", trimID);
                Identifier.Text = trimID.ToString();
            }
            CR_URL.Focus();
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            SPUtility.Redirect(this.Context.Request.Url.ToString(), SPRedirectFlags.Trusted, this.Context);
        }

    }
}
