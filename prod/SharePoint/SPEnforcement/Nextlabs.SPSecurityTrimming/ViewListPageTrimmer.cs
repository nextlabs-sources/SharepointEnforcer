using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class ViewListPageTrimmer
    {
        public static bool TrimList(HttpContext context, SPList list)
        {
            bool allow = true;
            try
            {
                Object obj = new Object();
                var args = new ControlEventArgs(context);
                EventHelper.Instance.OnBeforeEventExecuting(obj, args);
                if (args.Cancel)
                    return allow;

                SPWeb web = SPControl.GetContextWeb(context);

                // Check whether security trimming is enabled for this site collection
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                {
                    if (!manager.CheckSecurityTrimming() || !manager.CheckPageTrimming())
                        return allow;
                }

                // Ignore Default Search Indexing User's request
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(context.User.Identity.Name))
                {
                    return allow;
                }

                string defaultViewUrl = "";
                string url = "";

                try
                {
                    defaultViewUrl = list.DefaultViewUrl;
                }
                catch
                {
                    defaultViewUrl = "";
                }

                if (String.IsNullOrEmpty(defaultViewUrl))
                {
                    defaultViewUrl = list.RootFolder.ServerRelativeUrl;
                }

                url = web.Site.MakeFullUrl(defaultViewUrl);

                string remoteAddress = context.Request.UserHostAddress;
                {
                    EvaluationBase EvaObj = EvaluationFactory.CreateInstance(list, CETYPE.CEAction.Read, url,
                        remoteAddress, "View List Page Trimmer", web.CurrentUser);
                    allow = EvaObj.Run();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during ViewListPageTrimmer TrimList:", null, ex);
            }

            return allow;
        }

        public static bool TrimWeb(HttpContext context, SPWeb web)
        {
            bool allow = true;
            try
            {
                Object obj = new Object();
                var args = new ControlEventArgs(context);
                EventHelper.Instance.OnBeforeEventExecuting(obj, args);
                if (args.Cancel)
                    return allow;

                // Check whether security trimming is enabled for this site collection
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                {
                    if (!manager.CheckSecurityTrimming() || !manager.CheckPageTrimming())
                        return allow;

                }
                // Ignore Default Search Indexing User's request
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(context.User.Identity.Name))
                {
                    return allow;
                }

                string url = web.Url;

                string remoteAddress = context.Request.UserHostAddress;
                {
                    EvaluationBase EvaObj = EvaluationFactory.CreateInstance(web, CETYPE.CEAction.Read, url,
                        remoteAddress, "View List Page Trimmer", web.CurrentUser);
                    allow = EvaObj.Run();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during ViewListPageTrimmer TrimWeb:", null, ex);
            }

            return allow;
        }
    }
}
