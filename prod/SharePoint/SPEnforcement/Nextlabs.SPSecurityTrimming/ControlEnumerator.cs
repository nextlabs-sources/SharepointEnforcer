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
using Microsoft.SharePoint.Publishing.Internal.WebControls;
using Microsoft.SharePoint.Administration;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class WebPartInfo
    {
        public WebPartInfo(System.Web.UI.WebControls.WebParts.WebPart WebPart, int id)
        {
            webPart = WebPart;
            ID = id;
        }
        public System.Web.UI.WebControls.WebParts.WebPart webPart;
        public int ID;
    }

    public class MenuItemInfo
    {
        public MenuItemInfo(System.Web.UI.WebControls.MenuItem MenuItem, AspMenu NavMenu, int id, string menuGuid)
        {
            menuItem = MenuItem;
            navMenu = NavMenu;
            ID = id;
            guid = menuGuid;
        }
        public System.Web.UI.WebControls.MenuItem menuItem;
        public AspMenu navMenu;
        public int ID;
        public string guid;
    }

    public class SmtGridViewRowInfo
    {
        public SmtGridViewRowInfo(GridViewRow RowControl, int id, string smtGuid)
        {
            rowControl = RowControl;
            ID = id;
            guid = smtGuid;
        }
        public GridViewRow rowControl;
        public int ID;
        public string guid;
    }

    public class ControlEnumerator
    {
        private HttpContext Context;
        private Page CurPage;
        private WebPartManager WpManager;
        private bool m_bSecrityTrimming;
        private bool m_bTabTrimming;
        private bool m_bWebPartTrimming;
        private bool m_bPageTrimming;
        private bool m_bConnected;
        private List<WebPartInfo> m_WebPartCache;
        private List<MenuItemInfo> m_MenuItemCache;
        private EvaluationMultiple m_mulEvalContrl;
        private EvaluationMultiple m_mulEvalWebPart;
        private bool m_bEnumAllCtls;
        private bool m_bSecurityCtl;
        public bool SecurityCtl
        {
            get{ return m_bSecurityCtl; }
            set{ m_bSecurityCtl = value; }
        }

        public ControlEnumerator(HttpContext context, Page page)
        {
            Context = context;
            CurPage = page;
            WpManager = WebPartManager.GetCurrentWebPartManager(CurPage);
            m_bEnumAllCtls = false;
            m_bSecurityCtl = false;


            m_WebPartCache = new List<WebPartInfo>();
            m_MenuItemCache = new List<MenuItemInfo>();

            //check trimming options
            SPSite site = SPControl.GetContextSite(Context);
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
            {
                m_bSecrityTrimming = manager.CheckSecurityTrimming();
                if (m_bSecrityTrimming)
                {
                    m_bTabTrimming = manager.CheckTabTrimming();
                    m_bWebPartTrimming = manager.CheckWebpartTrimming();
                    m_bPageTrimming = manager.CheckPageTrimming();
                }
                m_bConnected = TrimmingEvaluationMultiple.IsPCConnected();
            }
        }

        public void RunEnumerateAll()
        {
            if (m_bConnected && m_mulEvalContrl != null)
            {
                bool bRun = m_mulEvalContrl.run();
                if(bRun)
                {
                    MenuItemTrimming();
                    m_MenuItemCache.Clear();
                }
                m_mulEvalContrl.ClearRequest();
                m_mulEvalContrl = null;
            }
            m_bEnumAllCtls = false;
        }

        public void EnumerateAll()
        {
            if (m_bConnected && m_bSecrityTrimming && !m_bEnumAllCtls)
            {
                m_bEnumAllCtls = true;
                SPWeb web = SPControl.GetContextWeb(Context);
                TrimmingEvaluationMultiple.NewEvalMult(web, ref m_mulEvalContrl);
                EnumerateAllControls(CurPage);
            }
        }

        public void EnumerateAllWebParts()
        {
            if (m_bConnected && m_bSecrityTrimming && m_bWebPartTrimming)
            {
                SPWeb web = SPControl.GetContextWeb(Context);
                TrimmingEvaluationMultiple.NewEvalMult(web, ref m_mulEvalWebPart);
                EnumerateAllWebParts(CurPage);
            }
            if (m_mulEvalWebPart != null && m_WebPartCache.Count > 0)
            {
                bool bRun = m_mulEvalWebPart.run();
                if (bRun)
                {
                    WebPartTrimming();
                    m_WebPartCache.Clear();
                }
                m_mulEvalWebPart.ClearRequest();
                m_mulEvalWebPart = null;
            }
        }

        private void EnumerateAllWebParts(Control Root)
        {
            foreach (Control ctl in Root.Controls)
            {
                if (ctl is System.Web.UI.WebControls.WebParts.WebPart)
                {
                    System.Web.UI.WebControls.WebParts.WebPart webpart = ctl as System.Web.UI.WebControls.WebParts.WebPart;

                    WebPartTrimmer trimmer = new WebPartTrimmer(Context, WpManager, webpart);
                    if (m_mulEvalWebPart != null)
                    {
                        trimmer.MultipleTrimming(m_mulEvalWebPart, m_WebPartCache);
                    }
                    else
                    {
                        trimmer.DoTrimming();
                    }
                }
                else
                    EnumerateAllWebParts(ctl);
            }
        }

        private void EnumerateAllControls(Control Root)
        {
            foreach (Control ctl in Root.Controls)
            {
                if (ctl is XsltListViewWebPart)
                {
                    XsltListViewWebPart listWebPart = ctl as XsltListViewWebPart;

                    if (m_bSecrityTrimming && m_bWebPartTrimming)
                    {
                        WebPartTrimmer wpTrimmer = new WebPartTrimmer(Context, WpManager, listWebPart);
                        wpTrimmer.DoTrimming();
                    }

                    if (listWebPart.Visible && !listWebPart.IsClosed)
                    {
#if SP2013 || SP2016 || SP2019
                        HttpRequest request=HttpContext.Current.Request;
                        HttpResponse response = HttpContext.Current.Response;
                        SPWeb web = SPControl.GetContextWeb(Context);
                        bool bRunSPListTrimmer2010 = false;
                        if (web.UIVersion == 15)
                        {
                            string wikiPageMode = request["_wikiPageMode"];
                            string callBackPram = request.Form["__CALLBACKPARAM"];
                            string wikiEditPram = request.Form["ctl00$PlaceHolderMain$btnWikiEdit"];
                            if (Context.Request.HttpMethod != "POST" || (wikiPageMode != null && wikiPageMode == "Edit")
                            || (wikiEditPram != null && wikiEditPram.Contains("edit"))
                            || (callBackPram != null && (callBackPram.Contains("Read") || callBackPram.Contains("Edit"))))
                                bRunSPListTrimmer2010 = true;
                        }
                        else
                        {
                            string view = request.QueryString["View"];
                            string rootFolder=request.QueryString["RootFolder"];
                            string __EVENTTARGET = request.Form["__EVENTTARGET"];
                            if(Context.Request.HttpMethod != "POST")
                                bRunSPListTrimmer2010 = true;
                            else if (Context.Request.HttpMethod == "POST" && ( (string.IsNullOrEmpty(view) == false && string.IsNullOrEmpty(rootFolder) == false) ||
                                                                               (string.IsNullOrEmpty(__EVENTTARGET))
                                                                             )
                                     )
                                bRunSPListTrimmer2010 = true;
                        }

						SPListItemTrimmer2010 trimmer = new SPListItemTrimmer2010(Context, listWebPart);
                        if (bRunSPListTrimmer2010==true)
                        {
                            bool bTrimmed = true;
                            bTrimmed = trimmer.DoTrimming();
                            if (bTrimmed)
                            {
                                listWebPart.PreRender += new EventHandler(XsltListViewWebPart_PreRender);
                            }
                        }
                        else
                        {
                            string rootFolder = request.QueryString["RootFolder"];
                            SPList list = web.Lists[new Guid(listWebPart.ListName)];
                            string leftUrl = request.Url.GetLeftPart(UriPartial.Path);
                            SPList trimList = (SPList)Utilities.GetCachedSPContent(web, leftUrl, Utilities.SPUrlList);
                            if (!string.IsNullOrEmpty(rootFolder) && request.HttpMethod == "POST" && list != null && trimList != null && trimList.ID == list.ID)
                            {
                                trimmer.DoFolderTrimming();
                            }
                            else if (list != null && request.HttpMethod == "POST")
                            {
                                ResponseFilter filter = ResponseFilters.Current(response);
                                filter.AddFilterType(FilterType.PostTrimmer);
                                filter.Web = web;
                            }
                        }
#else
                        SPListItemTrimmer2010 trimmer = new SPListItemTrimmer2010(Context, listWebPart);
                        bool bTrimmed = true;
                        bTrimmed = trimmer.DoTrimming();
                        if (bTrimmed)
                        {
                            listWebPart.PreRender += new EventHandler(XsltListViewWebPart_PreRender);
                        }
#endif
                    }
                }
                else if (ctl is ListViewWebPart)
                {
                    ListViewWebPart webpart = ctl as ListViewWebPart;
                    if (m_bSecrityTrimming && m_bWebPartTrimming)
                    {
                        WebPartTrimmer wpTrimmer = new WebPartTrimmer(Context, WpManager, webpart);
                        wpTrimmer.DoTrimming();
                    }

                    if (webpart.Visible && !webpart.IsClosed)
                    {
                        SPListItemTrimmer2010 trimmer = new SPListItemTrimmer2010(Context, null);
                        bool bTrimmed = true;
                        bTrimmed = trimmer.DoListTrimming(webpart);
                        if (bTrimmed)
                        {
                            webpart.PreRender += new EventHandler(ListViewWebPart_PreRender);
                        }
                    }
                }
                else if (ctl is SMTreeView)
                {
                    if (m_bSecrityTrimming && m_bPageTrimming)
                    {

                        SMTreeView treeView = ctl as SMTreeView;

                        treeView.TreeNodeExpanded += new TreeNodeEventHandler(SMTreeView_TreeNodeExpanded);

                        TreeNodeCollection childs = treeView.Nodes;
                        for (int index = 0; index < childs.Count; )
                        {
                            bool allow = EnumerateAllExpandedTreeNode(treeView, childs[index]);
                            if (allow) index++;
                        }
                    }
                }
                else if (ctl is SmtGridView)
                {
                    if (m_bSecrityTrimming && m_bPageTrimming)
                    {
                        SmtGridView gridView = ctl as SmtGridView;
                        gridView.DataBound += new EventHandler(SmtGridView_DataBound);
                    }
                }
                else if (ctl is AspMenu)
                {
                    if (m_bSecrityTrimming && m_bTabTrimming)
                    {
                        AspMenu menu = ctl as AspMenu;
                        menu.MenuItemDataBound += new MenuEventHandler(AspMenu_MenuItemDataBound);
                        menu.Unload += new EventHandler(Control_Unload);
                    }
                }
                else if (ctl is TreeView)
                {
                    if (m_bSecrityTrimming && m_bPageTrimming)
                    {
                        TreeView tree = ctl as TreeView;
                        tree.TreeNodeDataBound += new TreeNodeEventHandler(TreeView_TreeNodeDataBound);
                        tree.Unload += new EventHandler(Control_Unload);
                    }
                }
                else if (ctl is System.Web.UI.WebControls.WebParts.WebPart)
                {
                    // Don't deep into WebPart children.
                    // Or else it will cause no search result when using advanced search sometimes.
                    if (this.CurPage.Request.Url.ToString().Contains("/Document Set/docsethomepage.aspx"))
                    {
                        foreach (Control col in ctl.Controls)
                        {
                            if (col is XsltListViewWebPart)
                            {
                                EnumerateAllControls(col.Parent);
                                break;
                            }
                        }

                    }
                }
                else
                    EnumerateAllControls(ctl);
            }
        }

        void XsltListViewWebPart_PreRender(object sender, EventArgs e)
        {
            try
            {
                if (sender is XsltListViewWebPart)
                {

                    XsltListViewWebPart webpart = sender as XsltListViewWebPart;
                    TrimFeatureMenuTemplate(webpart.ListName, webpart);
                }
            }
            catch
            {
            }
        }

        void ListViewWebPart_PreRender(object sender, EventArgs e)
        {
            try
            {
                ListViewWebPart webpart = sender as ListViewWebPart;
                TrimFeatureMenuTemplate(webpart.ListName, webpart);
            }
            catch
            {
            }
        }

        private void AspMenu_MenuItemDataBound(object sender, MenuEventArgs e)
        {
            try
            {
                AspMenu menu = sender as AspMenu;
                System.Web.UI.WebControls.MenuItem item = e.Item;
                AspMenuItemTrimmer trimmer = new AspMenuItemTrimmer(Context, menu, item);
                if (m_mulEvalContrl != null && !m_bSecurityCtl)
                {
                    trimmer.MultipleTrimming(m_mulEvalContrl, m_MenuItemCache);
                }
                else
                {
                    trimmer.DoTrimming();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  AspMenu_MenuItemDataBound:", null, ex);
            }
        }

        private void TreeView_TreeNodeDataBound(object sender, TreeNodeEventArgs e)
        {
            try
            {
                TreeView tree = sender as TreeView;
                TreeNode node = e.Node;
                TreeViewNodeTrimmer trimmer = new TreeViewNodeTrimmer(Context, tree, node);
                trimmer.DoTrimming();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  TreeView_TreeNodeDataBound:", null, ex);
            }
        }

        private void Control_Unload(object sender, EventArgs e)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                CommonVar.Clear();
            });
        }

        private void SMTreeView_TreeNodeExpanded(object sender, TreeNodeEventArgs e)
        {
            try
            {
                SMTreeView tree = sender as SMTreeView;
                TreeNode node = e.Node;
                EnumerateAllExpandedTreeNode(tree, node);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SMTreeView_TreeNodeExpanded:", null, ex);
            }
        }

        private void SmtGridView_DataBound(object sender, EventArgs e)
        {
            try
            {
                SmtGridView gridView = sender as SmtGridView;
                gridView.Caption = "";
                GridViewRowCollection rows = gridView.Rows;
                SmtGridViewRowTrimmer trimmer = new SmtGridViewRowTrimmer(Context, rows);
               trimmer.DoTrimming();
               if (!trimmer.GetAnyAllowItem())
               {
                   gridView.Caption = "All the items on this page are trimmed by NextLabs Entitlement Manager";
               }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SmtGridView_DataBound:", null, ex);
            }
        }

        private bool EnumerateAllExpandedTreeNode(SMTreeView treeview, TreeNode node)
        {
            // To do evaluation
            SMTreeViewNodeTrimmer trimmer = new SMTreeViewNodeTrimmer(Context, treeview, node);
            bool trimmed = trimmer.DoTrimming();
            if (trimmed)
                return !trimmed;

            if (node.ChildNodes.Count == 0 || node.Expanded == false)
            {
                return true;
            }

            TreeNodeCollection childs = node.ChildNodes;
            for (int index = 0; index < childs.Count;)
            {
                bool allow = EnumerateAllExpandedTreeNode(treeview, childs[index]);
                if (allow) index++;
            }

            return true;
        }

        private void TrimFeatureMenuTemplate(string ListName, Control ctl)
        {
            SPWeb web = SPControl.GetContextWeb(Context);
            SPList list = web.Lists[new Guid(ListName)];
            foreach (Control child in ctl.Controls)
            {
                if (child is FeatureMenuTemplate)
                {
                    FeatureMenuTemplate menuTemplate = child as FeatureMenuTemplate;
                    if (menuTemplate.GroupId.Equals("ActionsMenu"))
                    {
                        foreach (Control itemCtl in menuTemplate.Controls)
                        {
                            if (itemCtl is MenuItemTemplate)
                            {
                                MenuItemTemplate itemTemplate = itemCtl as MenuItemTemplate;
                                if (itemTemplate.Text.Equals("Edit In Datasheet", StringComparison.OrdinalIgnoreCase)
                                    || itemTemplate.Text.Equals("View RSS Feed", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Hide "Edit in DataSheet", and "View RSS Feed"
                                    itemTemplate.Visible = false;
                                }
                            }
                        }
                    }
                    else if (menuTemplate.GroupId.Equals("ViewSelectorMenu"))
                    {
                        foreach (Control itemCtl in menuTemplate.Controls)
                        {
                            if (itemCtl is MenuItemTemplate)
                            {
                                MenuItemTemplate itemTemplate = itemCtl as MenuItemTemplate;
                            }
                        }
                    }
                    else if (menuTemplate.GroupId.Equals("SettingsMenu"))
                    {
                        foreach (Control itemCtl in menuTemplate.Controls)
                        {
                            if (itemCtl is MenuItemTemplate)
                            {
                                MenuItemTemplate itemTemplate = itemCtl as MenuItemTemplate;
                            }
                        }
                    }
                }
                else if (child is Microsoft.SharePoint.WebControls.Menu)
                {
                    Microsoft.SharePoint.WebControls.Menu menu = child as Microsoft.SharePoint.WebControls.Menu;
                }
                else
                    TrimFeatureMenuTemplate(ListName, child);
            }
        }

        private void WebPartTrimming()
        {
            System.Web.UI.WebControls.WebParts.WebPart CurWebPart = null;
            bool bAllow = true;
            SPWeb web = SPControl.GetContextWeb(Context);
            string remoteAddress = Context.Request.UserHostAddress;
            string userId = web.CurrentUser.LoginName;
            DateTime evalTime = DateTime.Now;
            string guid = null;
            foreach (WebPartInfo cache in m_WebPartCache)
            {
                CurWebPart = cache.webPart;
                bAllow = m_mulEvalWebPart.GetTrimEvalResult(cache.ID);
                guid = String.IsNullOrEmpty(CurWebPart.ID) ? CurWebPart.GetType().ToString() : CurWebPart.ID + CurWebPart.Title;
                TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddress, guid, bAllow, evalTime);
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
            }
        }

        private void MenuItemTrimming()
        {
            System.Web.UI.WebControls.MenuItem Item = null;
            AspMenu NavMenu = null;
            SPWeb web = SPControl.GetContextWeb(Context);
            string remoteAddress = Context.Request.UserHostAddress;
            string userId = web.CurrentUser.LoginName;
            DateTime evalTime = DateTime.Now;
            string guid = null;
            bool bAllow = true;
            foreach (MenuItemInfo cache in m_MenuItemCache)
            {
                bAllow = m_mulEvalContrl.GetTrimEvalResult(cache.ID);
                guid = cache.guid;
                TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddress, guid, bAllow, evalTime);
                if (!bAllow)
                {
                    Item = cache.menuItem;
                    NavMenu = cache.navMenu;
                    // Hide
                    if (Item.Parent != null)
                        Item.Parent.ChildItems.Remove(Item);
                    else
                        NavMenu.Items.Remove(Item);
                }
            }
        }
    }
}
