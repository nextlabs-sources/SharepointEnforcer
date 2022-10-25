using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using NextLabs.Common;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public enum SPItemTrimType
    {
        RootTrim,
        ListTrim,
        Unknow
    }

    public class SPItemTrimCaches
    {
        private static Dictionary<int, Dictionary<string, SPItemTrimCache>> SPItemTrimCachePool = new Dictionary<int, Dictionary<string, SPItemTrimCache>>();
        private static object syncRoot = new Object();

        public SPItemTrimCaches()
        {

        }

        public static SPItemTrimCache Current(string guid)
        {
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (SPItemTrimCachePool.ContainsKey(ssid))
            {
                if (SPItemTrimCachePool[ssid].ContainsKey(guid))
                {
                    return SPItemTrimCachePool[ssid][guid];
                }
                else
                {
                    lock (syncRoot)
                    {
                        SPItemTrimCache newSPItemTrimCache = new SPItemTrimCache();
                        SPItemTrimCachePool[ssid][guid] = newSPItemTrimCache;
                        return newSPItemTrimCache;
                    }
                }
            }
            else
            {
                SPItemTrimCache newSPItemTrimCache = new SPItemTrimCache();
                Dictionary<string, SPItemTrimCache> newDicSPItem = new Dictionary<string, SPItemTrimCache>();
                newDicSPItem[guid] = newSPItemTrimCache;
                lock (syncRoot)
                {
                    SPItemTrimCachePool[ssid] = newDicSPItem;
                    return newSPItemTrimCache;
                }
            }
        }

        public static void ClearCurrent()
        {
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (SPItemTrimCachePool.ContainsKey(ssid))
            {
                SPItemTrimCachePool.Remove(ssid);
            }
        }
    }

    public class SPItemTrimCache
    {
        public SPItemTrimCache()
        {
            m_result = null;
            m_type = SPItemTrimType.Unknow;
        }

        private string m_result;
        public string CacheResult
        {
            set { m_result = value; }
            get { return m_result; }
        }

        private SPItemTrimType m_type;
        public SPItemTrimType TrimType
        {
            set { m_type = value; }
            get { return m_type; }
        }
    }

    class SPListItemTrimmer2010 : ITrimmer
    {
        private HttpContext m_context;
        private XsltListViewWebPart m_webPart;
        static uint DefaultRowLimit = 70; // Row limited in one page in list trimming .
        private SPItemTrimCache m_trimCache;

        public SPListItemTrimmer2010(HttpContext context, XsltListViewWebPart webPart)
        {
            m_context = context;
            m_webPart = webPart;

            m_trimCache = null;
        }

        public bool DoTrimming()
        {
            try
            {
                SPWeb web = SPControl.GetContextWeb(m_context);
                SPList list = web.Lists[new Guid(m_webPart.ListName)];
                // Do triming for tasks template list in PageTrimmer.
                if (!IfListNeedTrimming(web, list) || list.BaseTemplate == SPListTemplateType.TasksWithTimelineAndHierarchy)
                {
                    return false;
                }

                // Check the cache result before do trimming. (just cache in one request.)
                SPView view = list.Views[new Guid(m_webPart.ViewGuid)];
                m_trimCache = SPItemTrimCaches.Current(list.ID.ToString() + view.ID.ToString());
                if (m_trimCache.TrimType.Equals(SPItemTrimType.RootTrim))
                {
                    if (!string.IsNullOrEmpty(m_trimCache.CacheResult))
                    {
                        m_webPart.XmlDefinition = m_trimCache.CacheResult;
                        // Set the auto refresh to long time.(Delay refresh to solve splash screen)
                        m_webPart.AutoRefresh = true;
                        m_webPart.AutoRefreshInterval = 86400;
                    }
                    return true;
                }

                // Change the row limited if it larger than 70.
                uint rowLimit = ChangeViewRowLimit(web, list, view);

                List<string> idItems = new List<string>();
                string filterXml = "";
                bool bAllowFlag = false; // Mark "Neq" or "Eq" in CAML.
#if SP2010
                HttpRequest Request = m_context.Request;
                bool bTurnPgae = EvalTurnPage(Request, list, view, idItems, ref filterXml);
                if(!bTurnPgae)
                {
                    EvalListItems(list, view, rowLimit, idItems, bAllowFlag, ref filterXml);
                }
                bAllowFlag = bTurnPgae; // if turn page trimming, the flag is true.
#else
                EvalListItems(list, view, rowLimit, idItems, bAllowFlag, ref filterXml);
#endif
                m_trimCache.TrimType = SPItemTrimType.RootTrim; // Set trim cache type.
                if (idItems.Count > 0 || !string.IsNullOrEmpty(filterXml))
                {
                    // Modify the Webpart Xml if need.
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.InnerXml = string.IsNullOrEmpty(m_webPart.XmlDefinition) ? view.GetViewXml() : m_webPart.XmlDefinition;
                    XmlNode node = xmlDoc.DocumentElement;
                    if (rowLimit != view.RowLimit && node["RowLimit"] != null)
                    {
                        node["RowLimit"].InnerText = rowLimit.ToString();
                    }
                    AddWebPartDenyFilter(list, node, idItems, filterXml, bAllowFlag);

                    if (string.IsNullOrEmpty(m_webPart.XmlDefinition) || !m_webPart.XmlDefinition.Equals(xmlDoc.InnerXml))
                    {
                        m_webPart.XmlDefinition = xmlDoc.InnerXml;
                        // Set the auto refresh to long time.(Delay refresh to solve splash screen)
                        m_webPart.AutoRefresh = true;
                        m_webPart.AutoRefreshInterval = 86400;
                        m_trimCache.CacheResult = xmlDoc.InnerXml; // Sect trim cache result.
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer2010 DoTrimming:", null, ex);
            }
            return false;
        }

        public bool DoListTrimming(ListViewWebPart webpart)
        {
            try
            {
                SPWeb web = SPControl.GetContextWeb(m_context);
                SPList list = web.Lists[new Guid(webpart.ListName)];
                if (!IfListNeedTrimming(web, list))
                {
                    return false;
                }

                // Check the cache result before do trimming. (just cache in one request.)
                SPView view = list.Views[new Guid(m_webPart.ViewGuid)];
                m_trimCache = SPItemTrimCaches.Current(list.ID.ToString() + view.ID.ToString());
                if (m_trimCache.TrimType.Equals(SPItemTrimType.ListTrim))
                {
                    if (!string.IsNullOrEmpty(m_trimCache.CacheResult))
                    {
                        webpart.ListViewXml = m_trimCache.CacheResult;
                        return true;
                    }
                }

                // Change the row limited if it larger than 70.
                uint rowLimit = ChangeViewRowLimit(web, list, view);

                List<string> denyItems = new List<string>();
                string filterXml = "";
                EvalListItems(list, view, rowLimit, denyItems, false, ref filterXml);
                m_trimCache.TrimType = SPItemTrimType.ListTrim; // Set trim cache type.

                if (denyItems.Count > 0)
                {
                    // Modify the Webpart Xml if need.
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.InnerXml = webpart.ListViewXml;
                    XmlNode node = xmlDoc.DocumentElement;
                    if (rowLimit != view.RowLimit && node["RowLimit"] != null)
                    {
                        node["RowLimit"].InnerText = rowLimit.ToString();
                    }
                    AddWebPartDenyFilter(list, node, denyItems, filterXml, false);

                    if (!webpart.ListViewXml.Equals(xmlDoc.InnerXml))
                    {
                        webpart.ListViewXml = xmlDoc.InnerXml;
                        m_trimCache.CacheResult = xmlDoc.InnerXml; // Sect trim cache result.
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer2010 DoListTrimming:", null, ex);
            }
            return false;
        }

        public bool DoTurnPageTrimming()
        {
            try
            {
                HttpRequest Request = m_context.Request;
                SPWeb web = SPControl.GetContextWeb(m_context);

                string listId = Request.QueryString["List"];
                string viewId = Request.QueryString["View"];
                if (!string.IsNullOrEmpty(listId) && !string.IsNullOrEmpty(viewId))
                {
                    SPList list = web.Lists[new Guid(listId)];
                    SPView view = list.Views[new Guid(viewId)];

                    bool bNeed = CheckInplviewNeedTrimming(Request);
                    if (list != null && view != null && bNeed)
                    {
                        if (!IfListNeedTrimming(web, list))
                        {
                            return false;
                        }

                        uint position = 0;
                        string rowPos = Request.QueryString["PageFirstRow"];
                        if (!string.IsNullOrEmpty(rowPos))
                        {
                            position = uint.Parse(rowPos);
                        }
                        // Check if the request is modified by myself before do turn page trimming.
                        if (null == Request.Headers["TURN_TRIM"] || Request.Headers["TURN_TRIM"] != position.ToString())
                        {
                            Request.Headers["TURN_TRIM"] = position.ToString();
                            List<string> allowItems = new List<string>();
                            string filterXml = "";
                            EvalTurnPage(Request, list, view, allowItems, ref filterXml);
                            TranslateRequest(Request, (int)view.RowLimit, allowItems);
                            return true;
                        }
                        else
                        {
                            // Change the response to remove our added "ID" filters.
                            string key = Request.Headers["SPEFliterIdKey"];
                            if (!string.IsNullOrEmpty(key))
                            {
                                ResponseFilter filter = ResponseFilters.Current(m_context.Response);
                                filter.AddFilterType(FilterType.FieldIdTrimmer);
                                filter.Key = key;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer2010 DoTurnPageTrimming:", null, ex);
            }
            return false;
        }

        public bool DoFolderTrimming()
        {
            bool bRet = false;
            try
            {
                SPWeb web = SPControl.GetContextWeb(m_context);
                HttpRequest Request = m_context.Request;
                SPFolder folder = null;
                // Get the folder and view in this case.
                string rootFolderUrl = Request.QueryString["RootFolder"];
                string ViewId = Request.QueryString["View"];
                if (!string.IsNullOrEmpty(rootFolderUrl))
                {
                    folder = web.GetFolder(web.Site.MakeFullUrl(rootFolderUrl));
                }
                if (folder != null && !string.IsNullOrEmpty(ViewId))
                {
                    SPList list = web.Lists[folder.ParentListId];
                    if (!IfListNeedTrimming(web, list))
                    {
                        return false;
                    }
                    string folderName = folder.Name;

                    // Check if the request is modified by myself before do folder trimming.
                    if (null == Request.Headers["FORLDER_TRIM"] || Request.Headers["FORLDER_TRIM"] != folderName)
                    {
                        Request.Headers["FORLDER_TRIM"] = folderName;
                        SPView view = list.Views[new Guid(ViewId)];
                        List<string> allowItems = new List<string>();
                        string filterXml = "";

                        // Change the row limited if it larger than 70.
                        uint rowLimit = ChangeViewRowLimit(web, list, view);

                        EvalListItems(list, view, rowLimit, allowItems, true, ref filterXml, folder);
                        TranslateRequest(Request, (int)rowLimit, allowItems);
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer2010 DoFolderTrimming:", null, ex);
            }

            return bRet;
        }

        public bool IfListNeedTrimming(SPWeb web, SPList list)
        {
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
            {
                if (!manager.CheckListTrimming() && !manager.CheckListTrimming(list))
                {
                    // List trimming is not opened by customer.
                    return false;
                }
            }
            if (list.BaseType == SPBaseType.Survey)
            {
                // Survey List doesn't support list view trimming
                return false;
            }
            return true;
        }

        public bool EvalTurnPage(HttpRequest Request, SPList list, SPView view, List<string> allowItems, ref string filterXml)
        {
            NameValueCollection nameValue = new NameValueCollection(Request.QueryString);
            string prePage = Request.QueryString["PagedPrev"];
            string preFile = Request.QueryString["p_FileLeafRef"];
            string preId = Request.QueryString["p_ID"];
#if SP2013 || SP2016 || SP2019
            string rootFolderUrl = Request.Form["InplRootFolder"];
#elif SP2010
            string rootFolderUrl = Request.QueryString["RootFolder"];
#endif
            bool bPrePage = false;
            if (!string.IsNullOrEmpty(prePage) && prePage.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                bPrePage = true;
            }

            uint rowLimit = view.RowLimit;
            uint position = 0;
            string rowPos = Request.QueryString["PageFirstRow"];
            if (!string.IsNullOrEmpty(rowPos))
            {
                position = uint.Parse(rowPos);
            }
#if SP2010
            if(string.IsNullOrEmpty(preFile) && string.IsNullOrEmpty(preId) && string.IsNullOrEmpty(rowPos))
            {
                return false; // This is first page, we should not do anything.
            }
#endif
            SPQuery query = new SPQuery();
            string queryStr = view.Query;

            //Add customer filter to query.
            ConvertCustomFilter(list, ref queryStr, nameValue);
            ConvertCustomOrderBy(Request, ref queryStr);
            if (CheckPreFilterListTrimming(list))
            {
                filterXml = Globals.DoPreFilterForCaml(list, Request.UserHostAddress, CETYPE.CEAction.View, ref queryStr);
            }
            query.Query = queryStr;

            if (!string.IsNullOrEmpty(rootFolderUrl))
            {
                SPFolder folder = list.ParentWeb.GetFolder(list.ParentWeb.Site.MakeFullUrl(rootFolderUrl));
                query.Folder = folder;
            }
            else
            {
                query.Folder = list.RootFolder;
            }
            if (string.IsNullOrEmpty(preFile) && string.IsNullOrEmpty(preId))
            {
                // This is jump page, don't turn from pre file.
                bPrePage = false;
            }
#if SP2010
            // "position = 1" means first page.
            if (position == 1)
            {
                bPrePage = false;
                preFile = null; // pre file is disable.
                preId = null; // pre ID is disable.
            }
#endif
            if (bPrePage)
            {
                bool bFind = DoTurnPrePage(list, query, position, rowLimit, preFile, preId, allowItems);
                if (!bFind)
                {
                    allowItems.Clear();
                    DoTurnPrePage(list, query, 0, rowLimit, preFile, preId, allowItems);
                }
            }
            else
            {
                bool bBegin = DoTurnNextPage(list, query, position, rowLimit, preFile, preId, allowItems);
                if (!bBegin)
                {
                    allowItems.Clear();
                    DoTurnNextPage(list, query, 0, rowLimit, preFile, preId, allowItems);
                }
            }
            return true;
        }

        public void CheckListItemEval(SPWeb web, SPList list, SPListItemCollection items, List<string> itemsId, bool allowFlag, ref int allowCount)
        {
            List<SPListItem> itemsList = new List<SPListItem>();
            // Convert SPListItemCollection to List.
            foreach (SPListItem item in items)
            {
                itemsList.Add(item);
            }
            CheckListItemEval(web, list, itemsList, itemsId, allowFlag, ref allowCount);
        }

        // "Multiple query" function for list items .
        public void CheckListItemEval(SPWeb web, SPList list, List<SPListItem> items, List<string> itemsId, bool allowFlag, ref int allowCount)
        {
            if (web == null || list == null || items == null || items.Count == 0)
            {
                return;
            }

            string remoteAddress = m_context.Request.UserHostAddress;
            string userId = web.CurrentUser.LoginName;
            bool bAllow = true;
            string itemUrl = null;
            string srcName = null;
            string[] srcAttr = null;
            int idRequest = 0;
            string guid = null;
            bool bExisted = false;
            EvaluationMultiple multEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(web, ref multEval);
            List<KeyValuePair<SPListItem, int>> listItemCache = new List<KeyValuePair<SPListItem, int>>();
            foreach (SPListItem item in items)
            {
                guid = list.ID.ToString() + item.ID.ToString();
                // Check evaluation cache before.
                bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, guid, ref bAllow, NextLabs.Common.Utilities.GetLastModifiedTime(item));
                if (!bExisted)
                {
                    itemUrl = web.Url + "/" + item.Url;
                    {
                        Globals.GetSrcNameAndSrcAttr(item, itemUrl, m_context, ref srcName, ref srcAttr);
                        multEval.SetTrimRequest(item, srcName, srcAttr, out idRequest);
                        KeyValuePair<SPListItem, int> itemId = new KeyValuePair<SPListItem, int>(item, idRequest);
                        listItemCache.Add(itemId);
                    }
                }
                else
                {
                    if (bAllow)
                    {
                        allowCount++;
                    }
                    if (bAllow.Equals(allowFlag))
                    {
                        itemsId.Add(item.ID.ToString());
                    }
                }
            }

            if (listItemCache.Count > 0)
            {
                // "Multuiple query" to get evaluation result.
                bool bRun = multEval.run();
                if (bRun)
                {
                    DateTime evalTime = DateTime.Now;
                    foreach (KeyValuePair<SPListItem, int> cache in listItemCache)
                    {
                        bAllow = multEval.GetTrimEvalResult(cache.Value);
                        guid = list.ID.ToString() + cache.Key.ID.ToString();
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddress, guid, bAllow, evalTime, NextLabs.Common.Utilities.GetLastModifiedTime(cache.Key));
                        if (bAllow)
                        {
                            allowCount++;
                        }
                        if (bAllow.Equals(allowFlag))
                        {
                            itemsId.Add(cache.Key.ID.ToString());
                        }
                    }
                    multEval.ClearRequest();
                }
            }
        }

        public bool ConvertCustomFilter(SPList list, ref string queryStr, NameValueCollection keyValue)
        {
            if (keyValue == null)
            {
                return false;
            }
            Dictionary<string, string> customFilter = new Dictionary<string, string>();
            string filterKey = "FilterField";
            string filterValue = "FilterValue";
            string value = "";

            foreach (string key in keyValue.Keys)
            {
                if (!string.IsNullOrEmpty(key) && -1 != key.IndexOf(filterKey))
                {
                    value = key.Replace(filterKey, filterValue);
                    if (keyValue[key] != null && !keyValue[key].Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        if (keyValue[value] == null)
                        {
                            customFilter.Add(keyValue[key], "");
                        }
                        else
                        {
                            customFilter.Add(keyValue[key], keyValue[value]);
                        }
                    }
                }
            }
            if (customFilter.Count < 1)
            {
                return false;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = "<Query>" + queryStr + "</Query>"; ;
            XmlNode node = xmlDoc.DocumentElement;
            if (node["Where"] == null)
            {
                node.InnerXml += "<Where></Where>";
            }
            XmlNode wheNode = node["Where"];
            string field = "";
            string[] split = { ";#" };

            foreach (var pair in customFilter)
            {
                field = pair.Key;
                string[] splits = pair.Value.Split(split, StringSplitOptions.None);
                if (splits != null)
                {
                    Globals.ConvertFieldFiltersXml(list, wheNode, field, splits.ToList<string>(), "Eq");
                }
            }

            queryStr = node.InnerXml;
            return true;
        }

        public void ConvertCustomOrderBy(HttpRequest Request, ref string queryStr)
        {
            // Add customer "OrderBy" case.
            string sortField = Request.QueryString["SortField"];
            string sortDir = Request.QueryString["SortDir"];
            if (!string.IsNullOrEmpty(sortField) && !string.IsNullOrEmpty(sortDir))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = "<Query>" + queryStr + "</Query>"; ;
                XmlNode node = xmlDoc.DocumentElement;
                if (node["OrderBy"] == null)
                {
                    node.InnerXml += "<OrderBy></OrderBy>";
                }
                string ascending = "TRUE";
                if(sortDir.Equals("Desc", StringComparison.OrdinalIgnoreCase))
                {
                    ascending = "FALSE";
                }
                string oldOrderby = node["OrderBy"].InnerXml;
                string fieldOderBy = string.Format("<FieldRef Name=\"{0}\" Ascending=\"{1}\" />", sortField, ascending);
                if (!string.IsNullOrEmpty(oldOrderby))
                {
                    string fieldName = string.Format("Name=\"{0}\"", sortField);
                    int ind = oldOrderby.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase);
                    if (-1 != ind)
                    {
                        // we need remove the "<FieldRef ** />" when existed same field.
                        string head = oldOrderby.Substring(0, ind);
                        string tail = oldOrderby.Substring(ind);
                        ind = head.LastIndexOf("<FieldRef");
                        if (ind != -1)
                        {
                            head = head.Substring(0, ind);
                        }
                        ind = tail.IndexOf("/>");
                        if (ind != -1)
                        {
                            tail = tail.Substring(ind + 2);
                        }
                        node["OrderBy"].InnerXml = fieldOderBy + head + tail;
                    }
                    else
                    {
                        node["OrderBy"].InnerXml = fieldOderBy + oldOrderby;
                    }
                }
                else
                {
                    node["OrderBy"].InnerXml = fieldOderBy;
                }
                queryStr = node.InnerXml;
            }
        }

        // Change the row limited when "rowLimit > DefaultRowLimit";
        private uint ChangeViewRowLimit(SPWeb web, SPList list, SPView view)
        {
            uint rowLimit = view.RowLimit;
            try
            {
                if (rowLimit > DefaultRowLimit)
                {
                    rowLimit = DefaultRowLimit;
                    SPSite site = web.Site;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite _site = new SPSite(site.Url))
                        {
                            using (SPWeb _web = _site.OpenWeb(web.ServerRelativeUrl))
                            {
                                bool bWebSafe = _web.AllowUnsafeUpdates;
                                if (!bWebSafe)
                                {
                                    _web.AllowUnsafeUpdates = true;
                                }
                                SPList _list = _web.Lists[list.ID];
                                SPView _view = _list.Views[view.ID];

                                // Set the row limited in view.
                                _view.RowLimit = DefaultRowLimit;
                                _view.Update();
                                if (!bWebSafe)
                                {
                                    _web.AllowUnsafeUpdates = bWebSafe;
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during ChangeViewRowLimit:", null, ex);
            }
            return rowLimit;
        }

        private bool CheckInplviewNeedTrimming(HttpRequest Request)
        {
            // Check the request inplview need do trimming.
            bool bNeed = false;
            string listId = Request.QueryString["List"];
            string viewId = Request.QueryString["View"];
            string cmd = Request.QueryString["Cmd"];

            // fix bug 34112, we all do trimming except "Cmd=Ctx".
            if (!string.IsNullOrEmpty(listId) && !string.IsNullOrEmpty(viewId))
            {
                if (string.IsNullOrEmpty(cmd) || !cmd.Equals("Ctx", StringComparison.OrdinalIgnoreCase))
                {
                    bNeed = true;
                }
            }
            return bNeed;
        }

        private bool EvalListItems(SPList list, SPView view, uint rowLimit, List<string> idItems, bool bAllowFlag, ref string filterXml, SPFolder folder = null)
        {
            try
            {
                int allowCount = 0;
                SPQuery query = new SPQuery();
                string queryStr = view.Query;
                NameValueCollection nameValue = new NameValueCollection(m_context.Request.QueryString);

                // Add customer filter to query.
                ConvertCustomFilter(list, ref queryStr, nameValue);
                ConvertCustomOrderBy(m_context.Request, ref queryStr);
                if (CheckPreFilterListTrimming(list))
                {
                    filterXml = Globals.DoPreFilterForCaml(list, m_context.Request.UserHostAddress, CETYPE.CEAction.View, ref queryStr);
                }
                query.Query = queryStr;

                if (folder == null)
                {
                    GetRootFolderUrl(list.ParentWeb, ref query, list);
                }
                else
                {
                    query.Folder = folder;
                }

                // query enough items filled first page.
                query.RowLimit = rowLimit + 1;
                do
                {
                    SPListItemCollection items = list.GetItems(query);
                    CheckListItemEval(list.ParentWeb, list, items, idItems, bAllowFlag, ref allowCount);
                    query.ListItemCollectionPosition = items.ListItemCollectionPosition;
                }// ListItemCollectionPosition is null for the last batch.
                while (query.ListItemCollectionPosition != null && allowCount <= rowLimit);
            }

            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SPListItemTrimmer2010 EvalListItems:", null, ex);
            }

            return true;
        }

        private void GetRootFolderUrl(SPWeb web, ref SPQuery query, SPList list)
        {
            string rootFolderUrl = null;
            string webpartViewId = m_context.Request.QueryString["View"];

            if (!string.IsNullOrEmpty(webpartViewId))
            {
                if (webpartViewId != m_webPart.ViewGuid)
                {
                    //if the root folder url does not belong to current list, try get root folder from WebPart.ParameterValues
                    rootFolderUrl = m_webPart.ParameterValues.Collection["RootFolder"] as string;
                }
            }

            if (string.IsNullOrEmpty(rootFolderUrl))
            {
                rootFolderUrl = m_context.Request.QueryString["RootFolder"];
            }

            if (!string.IsNullOrEmpty(rootFolderUrl))
            {
                string decodeUrl = Globals.UrlDecode(rootFolderUrl);
                if (decodeUrl.StartsWith(web.Site.Url, StringComparison.OrdinalIgnoreCase))
                    rootFolderUrl = decodeUrl.Substring(web.Site.Url.Length);
                else
                    rootFolderUrl = decodeUrl;

                var folder = web.GetFolder(web.Site.MakeFullUrl(rootFolderUrl));

                if (folder != null && folder.ParentListId == list.ID)
                {
                    //set the folder as query only when it belongs to current list
                    query.Folder = folder;
                }
            }
        }

        private void AddWebPartDenyFilter(SPList list, XmlNode node, List<string> idItems, string filterXml, bool bAllowFlag)
        {
            if (node["Query"] == null)
            {
                node.InnerXml += "<Query></Query>";
            }
            XmlNode queryNode = node["Query"];
            bool bWhereExisted = false;
            foreach (XmlNode child in queryNode.ChildNodes)
            {
                if (child.Name.Equals("GroupBy", StringComparison.OrdinalIgnoreCase))
                {
                    var attr = child.Attributes["Collapse"];
                    if (attr != null)
                    {
                        attr.Value = "FALSE";
                    }
                    break;
                }
            }
            foreach (XmlNode child in queryNode.ChildNodes)
            {
                if (child.Name.Equals("Where", StringComparison.OrdinalIgnoreCase))
                {
                    bWhereExisted = true;
                    break;
                }
            }

            if (!bWhereExisted)
            {
                queryNode.InnerXml += "<Where></Where>";
            }
            if (!string.IsNullOrEmpty(filterXml))
            {
                string innerXml = queryNode["Where"].InnerXml;
                if (string.IsNullOrEmpty(innerXml))
                {
                    innerXml = filterXml;
                }
                else if (!innerXml.Contains(filterXml))
                {
                    innerXml = "<And>" + innerXml + filterXml + "</And>";
                }
                queryNode["Where"].InnerXml = innerXml;
            }
            string camlOp = bAllowFlag ? "Eq" : "Neq";
            Globals.ConvertFieldFiltersXml(list, queryNode["Where"], "ID", idItems, camlOp);
        }

        private bool DoTurnPrePage(SPList list, SPQuery query, uint position, uint rowLimit, string preFile, string preId, List<string> idItems, bool bAllowFlag = true)
        {
            bool bFind = false;
            List<SPListItem> itemsList = new List<SPListItem>();
            uint queryPos = 1;
            query.ListItemCollectionPosition = null; // Clear the position before query.
            // query enough items each time.("currect page items" + "pre item" + "next item")
            query.RowLimit = rowLimit + 2;
            do
            {
                SPListItemCollection items = list.GetItems(query);
                query.ListItemCollectionPosition = items.ListItemCollectionPosition;
                if (position > rowLimit && queryPos < position - rowLimit)
                {
                    // Don't goto pre page before position, we don't need begin to find file.
                    queryPos += rowLimit;
                    continue;
                }

                foreach (SPListItem item in items)
                {
                    itemsList.Add(item);
                    if ((!string.IsNullOrEmpty(preFile) && item.Name.Equals(preFile))
                        || (!string.IsNullOrEmpty(preId) && item.ID.ToString().Equals(preId)))
                    {
                        // Find the pre file, from it begin.
                        bFind = true;
                        break;
                    }
                }
                if (bFind)
                {
                    break;
                }
            }// ListItemCollectionPosition is null for the last batch.
            while (query.ListItemCollectionPosition != null);
            int allowCount = 0;
            if (bFind)
            {
                List<SPListItem> evalItems = new List<SPListItem>();
                for (int i = itemsList.Count - 1; i >= 0; i--)
                {
                    if (allowCount >= rowLimit + 2)
                    {
                        break;
                    }
                    else
                    {
                        evalItems.Add(itemsList[i]);
                        if (evalItems.Count >= rowLimit + 2)
                        {
                            CheckListItemEval(list.ParentWeb, list, evalItems, idItems, bAllowFlag, ref allowCount);
                            evalItems.Clear();
                        }
                    }
                }

                if (allowCount < rowLimit + 2 && evalItems.Count > 0)
                {
                    CheckListItemEval(list.ParentWeb, list, evalItems, idItems, bAllowFlag, ref allowCount);
                }
            }
            if (allowCount < rowLimit + 2)
            {
                // Not get enough item.
                return false;
            }
            return bFind;
        }

        private bool DoTurnNextPage(SPList list, SPQuery query, uint position, uint rowLimit, string preFile, string preId, List<string> idItems, bool bAllowFlag = true)
        {
            bool bBegin = false; // Find the specifical file "preFile", we begin from it.
            int allowCount = 0;
            uint queryPos = 1;
            if (string.IsNullOrEmpty(preFile) && string.IsNullOrEmpty(preId))
            {
                bBegin = true;
            }
            query.ListItemCollectionPosition = null; // Clear the position before query.
            // query enough items each time.("currect page items" + "pre item" + "next item")
            query.RowLimit = rowLimit + 2;
            do
            {
                SPListItemCollection items = list.GetItems(query);
                query.ListItemCollectionPosition = items.ListItemCollectionPosition;
                if (position > rowLimit && queryPos < position - rowLimit)
                {
                    // Don't goto positon, we don't need begin to find file.
                    queryPos += rowLimit;
                    continue;
                }
                List<SPListItem> evalItems = new List<SPListItem>();
                foreach (SPListItem item in items)
                {
                    if (!bBegin)
                    {
                        if ((!string.IsNullOrEmpty(preFile) && item.Name.Equals(preFile))
                            || (!string.IsNullOrEmpty(preId) && item.ID.ToString().Equals(preId)))
                        {
                            bBegin = true;
                        }
                    }

                    if (bBegin)
                    {
                        evalItems.Add(item);
                    }
                }
                if (bBegin && evalItems.Count > 0)
                {
                    CheckListItemEval(list.ParentWeb, list, evalItems, idItems, bAllowFlag, ref allowCount);
                    evalItems.Clear();
                }
                if (allowCount >= rowLimit + 2)
                {
                    break;
                }
            }// ListItemCollectionPosition is null for the last batch.
            while (query.ListItemCollectionPosition != null);
            return bBegin;
        }

        private void TranslateRequest(HttpRequest Request, int rowLimit, List<string> itemIds)
        {
            NameValueCollection nameValue = new NameValueCollection(Request.QueryString);
            string newUrl = Request.RawUrl;
            int ind = 0;
            string filterKey = "FilterField";
            string filterKeys = "FilterFields";
            string filterValues = "FilterValues";
            bool bExisted = false;
            string value = "";
            string values = "";
            string filterIdKey = ""; // record the filter key for "ID";
            // Convert the items ID to field filter in URL.
            StringBuilder newQuery = new StringBuilder();
            bool bExistItems = true;
            // If all the items are trimed, we need add an empty item to the list.
            if (itemIds.Count == 0)
            {
                bExistItems = false;
                itemIds.Add("");
            }

            int num = itemIds.Count > (rowLimit + 2) ? (rowLimit + 2) : itemIds.Count;
            for (int i = 0; i < num; i++)
            {
                string itemId = itemIds[i];
                newQuery.AppendFormat("{0}%3B%23", itemId);
            }
            values = newQuery.ToString();

            List<string> keys = new List<string>();
            foreach (string key in nameValue.Keys)
            {
                if (!string.IsNullOrEmpty(key) && (-1 != key.IndexOf(filterKey) || -1 != key.IndexOf(filterKeys)))
                {
                    keys.Add(key);
                    if(-1 != key.IndexOf(filterKeys) && nameValue[key].Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        bExisted = true;
                        filterIdKey = key;
                        value = key.Replace(filterKeys, filterValues);
                        break;
                    }
                }
            }

            if (bExisted && newUrl.Contains(value))
            {
                ind = newUrl.IndexOf(value);
                string tail = newUrl.Substring(ind + value.Length + 1);
                ind = tail.IndexOf("&");
                if (-1 != ind)
                {
                    tail = tail.Substring(0, ind);
                }

                newUrl = newUrl.Replace(tail, values);
            }
            else
            {
                string newFilterkey = "";
                string newFilterValue = "";
                for (int i = 1; i < 10000; i++)
                {
                    newFilterkey = filterKeys + i.ToString();
                    if (!keys.Contains(newFilterkey) && !keys.Contains(newFilterkey.Replace(filterKeys, filterKey)))
                    {
                        newFilterValue =  newFilterkey.Replace(filterKeys, filterValues);
                        break;
                    }
                }
                filterIdKey = newFilterkey;
                newUrl += string.Format("&{0}=ID&{1}={2}", newFilterkey, newFilterValue, values);
            }

            if (!string.IsNullOrEmpty(filterIdKey) && bExistItems)
            {
                // Add the "ID" filter key to header, in order to change the response by it.
                Request.Headers["SPEFliterIdKey"] = filterIdKey;
            }
            HttpContext.Current.Server.TransferRequest(newUrl, true, Request.HttpMethod, Request.Headers);
        }

        public bool CheckPreFilterListTrimming(SPList list)
        {
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(list.ParentWeb.Site))
            {
                if (!manager.CheckListPrefilterTrimming() && !manager.CheckListPrefilterTrimming(list))
                {
                    // List pre-filter trimming is not opened by customer.
                    return false;
                }
            }
            return true;
        }
    }
}
