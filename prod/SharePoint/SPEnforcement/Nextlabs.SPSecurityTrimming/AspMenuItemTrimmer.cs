using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class AspMenuItemTrimmer : ITrimmer
    {
        private HttpContext Context;
        private AspMenu NavMenu;
        private MenuItem Item;

        public AspMenuItemTrimmer(HttpContext context, AspMenu menu, MenuItem item)
        {
            Context = context;
            NavMenu = menu;
            Item = item;
        }

        public bool MultipleTrimming(EvaluationMultiple multEval, List<MenuItemInfo> menuItemCache)
        {
            try
            {
                int idRequest = 0;
                string srcName = null;
                string[] srcAttr = null;
                SPSite site = SPControl.GetContextSite(Context);
                string url = "";
                if (Item.NavigateUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || Item.NavigateUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = Item.NavigateUrl;
                }
                else
                {
                    url = site.MakeFullUrl(Item.NavigateUrl);
                }

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SPWeb web = SPControl.GetContextWeb(Context);
                    if (web != null)
                    {
                        string guid = url;
                        string remoteAddress = Context.Request.UserHostAddress;
                        string userId = web.CurrentUser.LoginName;
                        bool bAllow = true;
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, guid, ref bAllow);
                        if (bExisted)
                        {
                            if (!bAllow)
                            {
                                // Hide
                                if (Item.Parent != null)
                                    Item.Parent.ChildItems.Remove(Item);
                                else
                                    NavMenu.Items.Remove(Item);
                            }
                        }
                        else
                        {
                            using (SPHttpUrlParser parser = new SPHttpUrlParser(url))
                            {
                                parser.Parse();
                                if (parser.ParsedObject != null)
                                {
                                    Globals.GetSrcNameAndSrcAttr(parser.ParsedObject, url, Context, ref srcName, ref srcAttr);
                                    multEval.SetTrimRequest(parser.ParsedObject, srcName, srcAttr, out idRequest);
                                    MenuItemInfo info = new MenuItemInfo(Item, NavMenu, idRequest, guid);
                                    menuItemCache.Add(info);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AspMenuItemTrimmer::MenuItemTrimming:", null, ex);
            }

            return true;
        }

        public bool DoTrimming()
        {
            bool bAllow = true;

            try
            {
                SPSite site = SPControl.GetContextSite(Context);
                SPWeb web = SPControl.GetContextWeb(Context);
                string url = "";
                if (Item.NavigateUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || Item.NavigateUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = Item.NavigateUrl;
                }
                else
                {
                    url = site.MakeFullUrl(Item.NavigateUrl);
                }
                string remoteAddress = Context.Request.UserHostAddress;
                string userId = web.CurrentUser.LoginName;
                bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, url, ref bAllow);
                if (!bExisted)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPHttpUrlParser parser = new SPHttpUrlParser(url))
                        {
                            parser.Parse();
                            EvaluationBase evaObj = EvaluationFactory.CreateInstance(parser.ParsedObject,
                                CETYPE.CEAction.Read, url, Context.Request.UserHostAddress, "Asp Menu Item Trimmer", web.CurrentUser);
                            if (evaObj != null)
                            {
                                bAllow = evaObj.Run();
                            }
                        }
                    });
                }
                if (!bAllow)
                {
                    // Hide
                    if (Item.Parent != null)
                        Item.Parent.ChildItems.Remove(Item);
                    else
                        NavMenu.Items.Remove(Item);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during DoTrimming:", null, ex);
            }
            return !bAllow;
        }
    }
}
