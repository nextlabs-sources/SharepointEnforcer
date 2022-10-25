using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class SPTHttpModule
    {
        public SPTHttpModule()
        {
        }

        public void PreRequest(Object source, EventArgs e)
        {
            HttpApplication application = (HttpApplication)source;
            HttpContext context = application.Context;
            // Pass through FBA login page.
            if (context == null || application.Request == null) return;

            try
            {
                string object_url = Globals.UrlDecode(context.Request.Url.GetLeftPart(UriPartial.Path));
                if (object_url.IndexOf("/_layouts/login.aspx", StringComparison.OrdinalIgnoreCase) > 0
                    || object_url.IndexOf("/_layouts/1033/", StringComparison.OrdinalIgnoreCase) > 0
                    || object_url.IndexOf("/_vti_bin/shtml.dll", StringComparison.OrdinalIgnoreCase) > 0
                    || object_url.IndexOf("/_layouts/AccessDenied.aspx", StringComparison.OrdinalIgnoreCase) > 0
                    || object_url.IndexOf("/_layouts/closeConnection.aspx", StringComparison.OrdinalIgnoreCase) > 0
                    || object_url.IndexOf("/download.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    return;
                }

                // George add trimming for authour.dll case.
                AuthorTrimming authorTrim = new AuthorTrimming();
                authorTrim.DoTrimming(application.Request, context);

                // George Add mobile trimming.
                SPMobileTrimming mobileTrim = new SPMobileTrimming();
                mobileTrim.DoTrimming(application.Request, context);

                if (object_url.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                // Ignore Default Search Indexing User's request
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(context.User.Identity.Name))
                {
                    return;
                }

                PageEventHook eventHook = new PageEventHook();
                eventHook.Register(application);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during PreRequestHandlerExecute:", null, ex);
            }
        }

        public void EndRequest(Object source, EventArgs e)
        {
            // Clear cache after "EndRequest".
            SPItemTrimCaches.ClearCurrent();
            ResponseFilters.ClearCurrent();
        }
    }
}
