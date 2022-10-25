using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    class PageEventHook
    {
        private HttpApplication Application;
        private SPWeb Web;
        private Page CurPage;
        private ControlEnumerator enumerator;

        public PageEventHook()
        {
        }

        public void Register(object sender)
        {
            Application = sender as HttpApplication;
            if (Application != null)
            {
                Web = SPControl.GetContextWeb(Application.Context);

                if (Web != null)
                {
                    // Check whether security trimming is enabled for this site collection
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(Web.Site))
                    {
                        if (!manager.CheckSecurityTrimming())
                            return;
                        if (Application.Context.CurrentHandler is Page)
                        {
                            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();

                            // Do Trimming in page response.
                            DoTrimingForPageResponse(_SPEEvalAttr);

                            if (_SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.RequestURL_path.IndexOf("inplview.aspx", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                // Add by George, for "turn page trimming" case.
                                SPListItemTrimmer2010 timmer = new SPListItemTrimmer2010(Application.Context, null);
                                timmer.DoTurnPageTrimming();
                                return;
                            }
                            CurPage = Application.Context.CurrentHandler as Page;
                            CurPage.Load += new EventHandler(page_Load);
                            CurPage.PreRender += new EventHandler(page_PreRender);
                            CurPage.PreRenderComplete += new EventHandler(page_PreRenderComplete);
                            CurPage.Unload += new EventHandler(page_Unload);
                        }
                    }
                }
            }
        }

        private void DoTrimingForPageResponse(SPEEvalAttr evalAttr)
        {
            PageFilterType pageType = PageFilterType.Unknown;
            if (-1 != evalAttr.RequestURL_path.IndexOf("/ViewEdit.aspx", StringComparison.OrdinalIgnoreCase))
            {
                pageType = PageFilterType.ViewEdit;
            }
            else if (evalAttr.ListObj != null && evalAttr.ItemObj == null)
            {
                if (evalAttr.ListObj.BaseTemplate == SPListTemplateType.TasksWithTimelineAndHierarchy)
                {
                    pageType = PageFilterType.Tasks;
                }
            }
#if SP2016
            if (pageType != PageFilterType.Unknown)
#endif
            {
                ResponseFilter filter = ResponseFilters.Current(Application.Context.Response);
                filter.AddFilterType(FilterType.PageTrimmer);
                filter.Web = Web;
                filter.PageType = pageType;
                filter.List = evalAttr.ListObj;
                filter.RemoteAddr = Application.Context.Request.UserHostAddress;
            }
        }

        private void page_Load(object sender, EventArgs e)
        {
            try
            {
                if (enumerator == null)
                {
                    enumerator = new ControlEnumerator(Application.Context, CurPage);
                    enumerator.EnumerateAll();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during page_Load:", null, ex);
            }
        }

        private void page_PreRenderComplete(object sender, EventArgs e)
        {
            if (enumerator != null)
            {
                enumerator.RunEnumerateAll();
                enumerator = null;
            }
        }

        private void page_PreRender(object sender, EventArgs e)
        {
            try
            {
                if (enumerator == null)
                {
                    enumerator = new ControlEnumerator(Application.Context, CurPage);
                }
                enumerator.EnumerateAllWebParts();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during page_PreRender:", null, ex);
            }
        }

        private void page_Unload(object sender, EventArgs e)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    CommonVar.Clear();
                });
        }
    }
}
