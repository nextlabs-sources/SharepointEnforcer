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
    public class WebPartTrimmer : ITrimmer
    {
        private HttpContext Context;
        private WebPartManager WpManager;
        private System.Web.UI.WebControls.WebParts.WebPart CurWebPart;

        public WebPartTrimmer(HttpContext context, WebPartManager wpMgr, System.Web.UI.WebControls.WebParts.WebPart webpart)
        {
            Context = context;
            WpManager = wpMgr;
            CurWebPart = webpart;
        }

        public bool MultipleTrimming(EvaluationMultiple multEval, List<WebPartInfo> webPartCache)
        {
            try
            {
                SPWeb web = SPControl.GetContextWeb(Context);
                string remoteAddress = Context.Request.UserHostAddress;
                string userId = web.CurrentUser.LoginName;
                bool bAllow = true;
                string guid = String.IsNullOrEmpty(CurWebPart.ID) ? CurWebPart.GetType().ToString() : CurWebPart.ID + CurWebPart.Title;
                bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, guid, ref bAllow);
                if (bExisted)
                {
                    if (!bAllow)
                    {
                        if (!(CurWebPart.IsStandalone || CurWebPart.IsStatic))
                        {
                            // Dynamic WebPart
                            if (WpManager != null && !CurWebPart.IsClosed)
                            {
                                WpManager.CloseWebPart(CurWebPart);
                            }
                        }
                        else
                        {
                            CurWebPart.Visible = false;
                        }
                    }
                    return false;
                }

                int idRequest = 0;
                string url = Context.Request.Url.GetLeftPart(UriPartial.Path);
                string srcName = null;
                string[] srcAttr = null;
                Globals.GetSrcNameAndSrcAttr(CurWebPart, url, Context, ref srcName, ref srcAttr);
                multEval.SetTrimRequest(CurWebPart, srcName, srcAttr, out idRequest);
                WebPartInfo info = new WebPartInfo(CurWebPart, idRequest);
                webPartCache.Add(info);
            }

            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during WebPartTrimmer MultipleTrimming:", null, ex);
            }

            return true;
        }

        public bool DoTrimming()
        {
            bool allow = true;

            try
            {
                SPSite site = SPControl.GetContextSite(Context);
                string url = Context.Request.Url.GetLeftPart(UriPartial.Path);

                string remoteAddress = Context.Request.UserHostAddress;
                {
                    SPWeb web = SPControl.GetContextWeb(Context);
                    SPWebPartEvaluation EvaObj = new SPWebPartEvaluation(CurWebPart, web, CETYPE.CEAction.Read,
                        url, Context.Request.UserHostAddress, "Web Part Trimmer", web.CurrentUser);

                    allow = EvaObj.Run();
                }

                if (!allow)
                {
                    if (!(CurWebPart.IsStandalone || CurWebPart.IsStatic))
                    {
                        // Dynamic WebPart
                        if (WpManager != null && !CurWebPart.IsClosed)
                        {
                            WpManager.CloseWebPart(CurWebPart);
                        }
                    }
                    else
                    {
                        CurWebPart.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AspMenuItemTrimmer DoTrimming:", null, ex);
            }

            return !allow;
        }
    }
}
