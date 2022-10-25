using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.ApplicationPages;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;

namespace Nextlabs.SPSecurityTrimming
{
    public class SPListTrimmingPage : WebAdminPageBase
    {
        public SPListTrimmingPage()
        {
        }

        protected CheckBox EnableListTrimming;
        protected String ListGuid;
        protected SPList List;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            using (SPWeb web = this.Web)
            {
                // Validate the Page Request to avoid any malicious posts
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
                            }
                            else if (!manager.CheckSecurityTrimming())
                            {
                                EnableListTrimming.Enabled = false;
                            }
                            else
                            {
                                EnableListTrimming.Enabled = true;
                                if (manager.CheckListTrimming(List))
                                {
                                    EnableListTrimming.Checked = true;
                                }
                                else
                                {
                                    EnableListTrimming.Checked = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void BtnOK_Click(object sender, EventArgs e)
        {
            ListGuid = Request.QueryString["List"];
            
            this.Page.Validate();
            if (this.Page.IsValid)
            {
                using (SPWeb web = this.Web)
                {
                    List = web.Lists[new Guid(ListGuid)];

                    if (List != null)
                    {
                        SPSite site = SPControl.GetContextSite(this.Context);
                        using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                        {
                            if (EnableListTrimming.Enabled)
                            {
                                if (EnableListTrimming.Checked)
                                {
                                    manager.EnableListTrimming(List);
                                }
                                else
                                {
                                    manager.DisableListTrimming(List);
                                }
                            }
                        }
                    }
                }
            }

            SPUtility.Redirect("listedit.aspx?List=" + ListGuid, SPRedirectFlags.UseSource | SPRedirectFlags.RelativeToLayoutsPage, this.Context);
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            ListGuid = Request.QueryString["List"];

            SPUtility.Redirect("listedit.aspx?List=" + ListGuid, SPRedirectFlags.UseSource | SPRedirectFlags.RelativeToLayoutsPage, this.Context);
        }
    }
}
