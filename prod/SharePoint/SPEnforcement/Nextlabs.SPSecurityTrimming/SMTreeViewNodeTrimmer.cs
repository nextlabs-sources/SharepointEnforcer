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
using Microsoft.SharePoint.Publishing.Internal.WebControls;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    class SMTreeViewNodeTrimmer : ITrimmer
    {
        private HttpContext Context;
        private SMTreeView View;
        private TreeNode Node;

        private SPWeb m_Web;
        private SPList m_List;
        private SPFolder m_Folder;

        public SMTreeViewNodeTrimmer(HttpContext context, SMTreeView view, TreeNode node)
        {
            Context = context;
            Node = node;
            View = view;

            m_Web = null;
            m_List = null;
            m_Folder = null;
        }

        public bool DoTrimming()
        {
            bool allow = true;

            try
            {
                SPSite site = SPControl.GetContextSite(Context);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return !allow;
                    }

                    m_Web = SPControl.GetContextWeb(Context);

                    ParseValuePath(Node.ValuePath);

                    if (m_Web != null)
                    {
                        string objUrl = m_Web.Url;
                        object nodeObj = m_Web;

                        if (m_List != null)
                        {
                            objUrl = NextLabs.Common.Utilities.ReConstructListUrl(m_List);
                            nodeObj = m_List;

                            if (m_Folder != null)
                            {
                                objUrl = site.MakeFullUrl(m_Web.ServerRelativeUrl + "/" + m_Folder.Url);
                                nodeObj = Utilities.GetCachedSPContent(m_Web, objUrl, Utilities.SPUrlListItem);
                            }
                        }

                        string remoteAddress = Context.Request.UserHostAddress;
                        {
                            EvaluationBase evaObj = EvaluationFactory.CreateInstance(nodeObj,
                                    CETYPE.CEAction.Read, objUrl, remoteAddress, "SMTreeView Node Trimmer", m_Web.CurrentUser);
                            allow = evaObj.Run();
                        }

                        if (!allow)
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
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SSMTreeViewNodeTrimmer:", null, ex);
            }

            return !allow;
        }

        void ParseValuePath(string valuePath)
        {
            //string prefix = "Area:?";
            string webGuid = "";
            string listGuid = "";
            string folderGuid = "";

            if (valuePath != null)
            {
                string[] parameters = valuePath.Split(new char[]{':', '/', '?'}, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == "SPWeb")
                    {
                        i++;
                        webGuid = parameters[i];
                    }
                    else if (parameters[i] == "SPList")
                    {
                        i++;
                        listGuid = parameters[i];
                    }
                    else if (parameters[i] == "SPFolder")
                    {
                        i++;
                        folderGuid = parameters[i];
                    }
                }
            }

            SPSite site = SPControl.GetContextSite(Context);
            if (!String.IsNullOrEmpty(webGuid))
            {
                SPWeb web = site.OpenWeb(new Guid(webGuid));
                if(web!=null)
                {
                    SPEEvalAttrs.Current().AddDisposeWeb(web);
                    if (!String.IsNullOrEmpty(listGuid))
                    {
                        m_List = web.Lists[new Guid(listGuid)];
                    }

                    if (!String.IsNullOrEmpty(folderGuid))
                    {
                        m_Folder = web.GetFolder(new Guid(folderGuid));
                    }
                    if (m_List == null && m_Folder == null)
                    {
                        m_Web = web;
                    }
                }
            }
        }
    }
}
