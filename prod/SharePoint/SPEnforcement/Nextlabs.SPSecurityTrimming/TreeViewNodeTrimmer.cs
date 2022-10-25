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
    public class TreeViewNodeTrimmer : ITrimmer
    {
        private HttpContext Context;
        private TreeView View;
        private TreeNode Node;

        public TreeViewNodeTrimmer(HttpContext context, TreeView view, TreeNode node)
        {
            Context = context;
            Node = node;
            View = view;
        }

        public bool DoTrimming()
        {
            bool bAllow = true;

            try
            {
                SPSite site = SPControl.GetContextSite(Context);
                SPWeb web = SPControl.GetContextWeb(Context);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return !bAllow;
                    }

                    string navUrl = Node.NavigateUrl;
                    string objType = "";

                    string navFun = "javascript:_spNavigateHierarchy(";
                    if (navUrl.StartsWith(navFun))
                    {
                        string[] navParams = navUrl.Substring(navFun.Length).Split(new char[] { ',', '\'', ')' }, StringSplitOptions.RemoveEmptyEntries);
                        if (navParams.Length != 6)
                        {
                            return !bAllow;
                        }
                        else
                        {
                            string tmpUrl = navParams.GetValue(2) as string;
                            objType = navParams.GetValue(4) as string;
                            if (!tmpUrl.Contains("\\u002f"))
                            {
                                tmpUrl = navParams.GetValue(3) as string;
                                objType = navParams.GetValue(5) as string;
                            }
                            navUrl = tmpUrl.Replace("\\u002f", "/");
                        }

                        string url = site.MakeFullUrl(navUrl);
                        string remoteAddress = Context.Request.UserHostAddress;
                        string userId = web.CurrentUser.LoginName;
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, url, ref bAllow);
                        if (!bExisted)
                        {
                            using (SPHttpUrlParser parser = new SPHttpUrlParser(url))
                            {
                                parser.Parse();
                                EvaluationBase evaObj = EvaluationFactory.CreateInstance(parser.ParsedObject,
                                CETYPE.CEAction.Read, url, remoteAddress, "Tree View Node Trimmer", web.CurrentUser);
                                bAllow = evaObj.Run();
                            }
                        }

                        if (!bAllow)
                        {
                            // Hide
                            if (Node.Parent != null)
                                Node.Parent.ChildNodes.Remove(Node);
                            else
                                View.Nodes.Remove(Node);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during TreeViewNodeTrimmer DoTrimming:", null, ex);
            }

            return !bAllow;
        }
    }
}
