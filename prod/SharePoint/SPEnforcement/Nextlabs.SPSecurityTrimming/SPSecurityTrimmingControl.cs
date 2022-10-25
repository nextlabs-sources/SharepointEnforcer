using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Serialization;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    [Guid("F3B160CA-D5A5-478c-A535-F13086EAE6A6")]
    public class SPSecurityTrimmingControl : WebControl
    {
        public SPSecurityTrimmingControl()
        {
        }

        protected override void CreateChildControls()
        {
            try
            {
                base.CreateChildControls();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPSecurityTrimmingControl CreateChildControls:", null, ex);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                this.EnsureChildControls();
                // Your code here...
                // Ignore Default Search Indexing User's request
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(this.Context.User.Identity.Name))
                {
                    return;
                }

                var args = new ControlEventArgs(this.Context);
                EventHelper.Instance.OnBeforeEventExecuting(this, args);
                if (args.Cancel)
                {
                    return;
                }

                // Check whether security trimming is enabled for this site collection
                SPSite site = SPControl.GetContextSite(this.Context);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return;
                    }
                    ControlEnumerator enumerator = new ControlEnumerator(this.Context, this.Page);
                    enumerator.SecurityCtl = true;
                    enumerator.EnumerateAll();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPSecurityTrimmingControl OnLoad:", null, ex);
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            try
            {
                base.OnPreRender(e);
                this.EnsureChildControls();
                // Your code here...
                // Ignore Default Search Indexing User's request
                var args = new ControlEventArgs(this.Context);
                EventHelper.Instance.OnBeforeEventExecuting(this, args);
                if (args.Cancel)
                    return;
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(this.Context.User.Identity.Name))
                {
                    return;
                }

                // Check whether security trimming is enabled for this site collection
                SPSite site = SPControl.GetContextSite(this.Context);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return;
                    }

                    ControlEnumerator enumerator = new ControlEnumerator(this.Context, this.Page);
                    enumerator.EnumerateAllWebParts();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPSecurityTrimmingControl OnPreRender:", null, ex);
            }
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }
    }
}
