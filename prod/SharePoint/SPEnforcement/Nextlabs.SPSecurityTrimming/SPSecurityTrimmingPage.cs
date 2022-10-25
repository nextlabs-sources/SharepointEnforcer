using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.ApplicationPages;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using NextLabs.Common;
namespace Nextlabs.SPSecurityTrimming
{
    public class SPSecurityTrimmingPage : WebAdminPageBase
    {
        public SPSecurityTrimmingPage()
        {
        }

        protected CheckBox EnablePLE;
        protected CheckBox EnableSearchPrefilterTrimming;
        protected CheckBox EnableSecurityTrimming;
        protected CheckBox EnableAllListTrimming;
        protected CheckBox EnableAllListPrefilterTrimming;
        protected CheckBox EnableTabTrimming;
        protected CheckBox EnableWebpartTrimming;
        protected CheckBox EnablePageTrimming;
        protected CheckBox EnableFastSearchTrimming;
        protected CheckBox ClearTrimmingCache;
        

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SPWeb web = this.Web;
            {
                // Validate the Page Request to avoid any malicious posts
                if (Request.HttpMethod == "POST")
                    SPUtility.ValidateFormDigest();

                if (web.UserIsSiteAdmin)
                {
                    if (!Page.IsPostBack)
                    {
                        using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                        {
                            //check if SPE is activated on this site collection
                            bool isPEactive = SCLSwitchChecker.GetPEStatus(web.Site);

                            //update enabled status of 
                            EnableSearchPrefilterTrimming.Enabled = isPEactive;
                            EnableSecurityTrimming.Enabled = isPEactive;
                            EnablePLE.Enabled = isPEactive;

                            //check if PLE is on
                            PLEManager pleManager = new PLEManager(this.Context.ApplicationInstance);
                            EnablePLE.Checked = pleManager.IsPLEEnabled();

                            //check if pre-filter trimming for search is on
                            EnableSearchPrefilterTrimming.Checked = manager.CheckSearchPrefilterTrimming();

                            //check if Security Trimming is on
                            if (isPEactive && manager.CheckSecurityTrimming())
                            {
                                EnableSecurityTrimming.Checked = true;
                                EnableAllListTrimming.Enabled = true;
                                EnableAllListTrimming.Checked = manager.CheckListTrimming();
                                EnableTabTrimming.Enabled = true;
                                EnableWebpartTrimming.Enabled = true;
                                EnablePageTrimming.Enabled = true;
                                EnableFastSearchTrimming.Enabled = true;
                                EnableTabTrimming.Checked = manager.CheckTabTrimming();
                                EnableWebpartTrimming.Checked = manager.CheckWebpartTrimming();
                                EnablePageTrimming.Checked = manager.CheckPageTrimming();
                                EnableFastSearchTrimming.Checked = manager.CheckFastSearchTrimming();
                            }
                            else
                            {
                                EnableSecurityTrimming.Checked = false;
                                EnableAllListTrimming.Enabled = false;
                                EnableAllListTrimming.Checked = manager.CheckListTrimming();
                                EnableTabTrimming.Enabled = false;
                                EnableWebpartTrimming.Enabled = false;
                                EnablePageTrimming.Enabled = false;
                                EnableFastSearchTrimming.Enabled = false;
                                EnableTabTrimming.Checked = manager.CheckTabTrimming();
                                EnableWebpartTrimming.Checked = manager.CheckWebpartTrimming();
                                EnablePageTrimming.Checked = manager.CheckPageTrimming();
                                EnableFastSearchTrimming.Checked = manager.CheckFastSearchTrimming();
                            }
                            if (isPEactive && manager.CheckListTrimming())
                            {
                                EnableAllListPrefilterTrimming.Enabled = true;
                                EnableAllListPrefilterTrimming.Checked = manager.CheckListPrefilterTrimming();
                            }
                            else
                            {
                                EnableAllListPrefilterTrimming.Enabled = false;
                                EnableAllListPrefilterTrimming.Checked = manager.CheckListPrefilterTrimming();
                            }
                            //bear fix bug 24282
                            ClearTrimmingCache.Enabled = EnableSecurityTrimming.Checked ? true : false;
                        }
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

        protected void BtnOK_Click(object sender, EventArgs e)
        {
            this.Page.Validate();
            if (this.Page.IsValid)
            {
                SPSite site = SPControl.GetContextSite(this.Context);
         
                #region
                    PLEManager pleManager = new PLEManager(this.Context.ApplicationInstance);
                    if (EnablePLE.Checked)
                    {
                        pleManager.setPropertyValue("spepleswitch", "enable");
                    }
                    else
                    {
                        pleManager.setPropertyValue("spepleswitch", "disable");
                    }

                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                    {
                        if (EnableSearchPrefilterTrimming.Checked)
                        {
                            manager.ActivateSearchPrefilterTrimming();
                        }
                        else
                        {
                            manager.DeactivateSearchPrefilterTrimming();
                        }

                        if (EnableSecurityTrimming.Checked)
                        {
                            manager.EnableInPublishingConsole();
                            if (EnableAllListTrimming.Checked)
                            {
                                manager.ActivateListTrimming();
                                if (EnableAllListPrefilterTrimming.Checked)
                                {
                                    manager.ActivateListPrefilterTrimming();
                                }
                                else
                                {
                                    manager.DeactivateListPrefilterTrimming();
                                }
                            }
                            else
                            {
                                manager.DeactivateListTrimming();
                                manager.DeactivateListPrefilterTrimming();
                            }
                            if (EnableTabTrimming.Checked)
                            {
                                manager.ActivateTabTrimming();
                            }
                            else
                            {
                                manager.DeactivateTabTrimming();
                            }
                            if (EnableWebpartTrimming.Checked)
                            {
                                manager.ActivateWebpartTrimming();
                            }
                            else
                            {
                                manager.DeactivateWebpartTrimming();
                            }
                            if (EnablePageTrimming.Checked)
                            {
                                manager.ActivatePageTrimming();
                            }
                            else
                            {
                                manager.DeactivatePageTrimming();
                            }
                            if (EnableFastSearchTrimming.Checked)
                            {
                                manager.ActivateFastSearchTrimming();
                            }
                            else
                            {
                                manager.DeactivateFastSearchTrimming();
                            }
                            if (ClearTrimmingCache.Checked)
                            {
                                manager.ClearCache();
                            }
                        }
                        else
                        {
                            manager.Disable();
                        }
                    }
                    //bear fix bug 24324
                    SCLSwitchChecker.SetPEStatus(site, true);
                    #endregion
         
                SPUtility.Redirect("settings.aspx", SPRedirectFlags.UseSource | SPRedirectFlags.RelativeToLayoutsPage, this.Context);
            }
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
