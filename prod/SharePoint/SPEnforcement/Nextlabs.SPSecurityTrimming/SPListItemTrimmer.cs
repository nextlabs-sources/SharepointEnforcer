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
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    class SPListItemTrimmer : ITrimmer
    {
        private HttpContext Context;
        private ListViewWebPart WebPart;
        public SPListItemTrimmer(HttpContext context, ListViewWebPart webpart)
        {
            Context = context;
            WebPart = webpart;
        }

        public bool MultipleTrimming()
        {
            try
            {
                bool allow = true;
                SPWeb web = SPControl.GetContextWeb(Context);
                SPList list = web.Lists[new Guid(WebPart.ListName)];

                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                {
                    if (!manager.CheckListTrimming() && !manager.CheckListTrimming(list))
                    {
                        return !allow;
                    }

                }
                if (list.BaseType == SPBaseType.Survey)
                {
                    // Survey List doesn't support list view trimming
                    return !allow;
                }

                SPView view = list.Views[new Guid(WebPart.ViewGuid)];
                SPQuery query = new SPQuery();
                query.Query = view.Query;
                query.RowLimit = (uint)list.ItemCount;

                string rootFolderUrl = Context.Request.QueryString["RootFolder"];

                if (!String.IsNullOrEmpty(rootFolderUrl))
                {
                    string decodeUrl = Globals.UrlDecode(rootFolderUrl);
                    if (decodeUrl.StartsWith(web.Site.Url, StringComparison.OrdinalIgnoreCase))
                        rootFolderUrl = decodeUrl.Substring(web.Site.Url.Length);
                    else
                        rootFolderUrl = decodeUrl;

                    SPFolder folder = web.GetFolder(web.Site.MakeFullUrl(rootFolderUrl));
                    if (folder != null && folder.ParentListId == list.ID)
                    {
                        //set the folder as query only when it belongs to current list
                        query.Folder = folder;
                    }
                }

                SPListItemCollection items;
                items = list.GetItems(query);
                int idRequest = 0;
                List<KeyValuePair<SPListItem, int>> listItemCache = new List<KeyValuePair<SPListItem, int>>();
                EvaluationMultiple multEval = null;
                TrimmingEvaluationMultiple.NewEvalMult(web, ref multEval);
                Array AllowItems = Array.CreateInstance(items.Count.GetType(), items.Count);
                Array DenyItems = Array.CreateInstance(items.Count.GetType(), items.Count);
                int AllowIndex = 0;
                int DenyIndex = 0;
                string remoteAddress = Context.Request.UserHostAddress;
                string userId = web.CurrentUser.LoginName;
                bool bAllow = true;
                string guid = null;
                bool bExisted = false;
                string itemUrl = null;
                string srcName = null;
                string[] srcAttr = null;

                foreach (SPListItem item in items)
                {
                    guid = list.ID.ToString() + item.ID.ToString();
                    bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, guid, ref bAllow, NextLabs.Common.Utilities.GetLastModifiedTime(item));
                    if (bExisted)
                    {
                        if (bAllow)
                        {
                            AllowItems.SetValue(item.ID, AllowIndex++);
                        }
                        else
                        {
                            DenyItems.SetValue(item.ID, DenyIndex++);
                        }
                    }
                    else
                    {
                        itemUrl = web.Url + "/" + item.Url;
                        if (String.IsNullOrEmpty(rootFolderUrl) || itemUrl.IndexOf(rootFolderUrl, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            Globals.GetSrcNameAndSrcAttr(item, itemUrl, Context, ref srcName, ref srcAttr);
                            multEval.SetTrimRequest(item, srcName, srcAttr, out idRequest);
                            KeyValuePair<SPListItem, int> itemId = new KeyValuePair<SPListItem, int>(item, idRequest);
                            listItemCache.Add(itemId);
                        }
                    }
                }

                bool bRun = multEval.run();
                if (!bRun)
                {
                    DateTime evalTime = DateTime.Now;
                    foreach (KeyValuePair<SPListItem, int> cache in listItemCache)
                    {
                        bAllow = multEval.GetTrimEvalResult(cache.Value);
                        guid = list.ID.ToString() + cache.Key.ID.ToString();
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddress, guid, bAllow, evalTime);
                        if (bAllow)
                        {
                            AllowItems.SetValue(cache.Key.ID, AllowIndex++);
                        }
                        else
                        {
                            DenyItems.SetValue(cache.Key.ID, DenyIndex++);
                        }
                    }
                }

                if (items.Count > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.InnerXml = WebPart.ListViewXml;
                    XmlNode node = xmlDoc.DocumentElement;
                    string queryXml = AddIdFilterToQueryString(node["Query"],
                        AllowItems, AllowIndex, DenyItems, DenyIndex);
                    node["Query"].InnerXml = queryXml;

                    WebPart.ListViewXml = xmlDoc.InnerXml;
                }
                multEval.ClearRequest();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer MultipleTrimming:", null, ex);
            }

            return true;
        }

        public bool DoTrimming()
        {
            bool allow = true;
            int AllowIndex = 0;
            int DenyIndex = 0;

            try
            {
                SPWeb web = SPControl.GetContextWeb(Context);
                SPList list = web.Lists[new Guid(WebPart.ListName)];

                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                {
                    if (!manager.CheckListTrimming() && !manager.CheckListTrimming(list))
                    {
                        return !allow;
                    }
                }

                if (list.BaseType == SPBaseType.Survey)
                {
                    // Survey List doesn't support list view trimming
                    return !allow;
                }

                SPView view = list.Views[new Guid(WebPart.ViewGuid)];
                SPQuery query = new SPQuery();
                query.Query = view.Query;
                query.RowLimit = (uint)list.ItemCount;

                string rootFolderUrl = Context.Request.QueryString["RootFolder"];

                if (!String.IsNullOrEmpty(rootFolderUrl))
                {
                    string decodeUrl = Globals.UrlDecode(rootFolderUrl);
                    if (decodeUrl.StartsWith(web.Site.Url, StringComparison.OrdinalIgnoreCase))
                        rootFolderUrl = decodeUrl.Substring(web.Site.Url.Length);
                    else
                        rootFolderUrl = decodeUrl;

                    query.Folder = web.GetFolder(web.Site.MakeFullUrl(rootFolderUrl));
                }

                string remoteAddress = Context.Request.UserHostAddress;
                string userId = Context.User.Identity.Name;

                SPListItemCollection items;
                items = list.GetItems(query);
                Array AllowItems = Array.CreateInstance(items.Count.GetType(), items.Count);
                Array DenyItems = Array.CreateInstance(items.Count.GetType(), items.Count);

                DateTime Start = DateTime.Now;

                foreach (SPListItem item in items)
                {
                    string itemUrl = web.Url + "/" + item.Url;
                    if (String.IsNullOrEmpty(rootFolderUrl)
                        || itemUrl.IndexOf(rootFolderUrl, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        EvaluationBase EvaObj = EvaluationFactory.CreateInstance(item, CETYPE.CEAction.Read,
                            web.Url + "/" + item.Url, remoteAddress, "List Item Trimmer", web.CurrentUser);

                        bool bAllow = EvaObj.Run(); // 1: Allow; 0: Deny

                        if (bAllow)
                        {
                            AllowItems.SetValue(item.ID, AllowIndex++);
                        }
                        else
                        {
                            DenyItems.SetValue(item.ID, DenyIndex++);
                        }
                    }
                }

                DateTime End = DateTime.Now;
                TimeSpan span = End - Start;
                NLLogger.OutputLog(LogLevel.Debug, "SPListItemTrimmer DoTrimming Evaluation " + items.Count.ToString() + " items Span " + span);
                if (items.Count > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.InnerXml = WebPart.ListViewXml;
                    XmlNode node = xmlDoc.DocumentElement;
                    string queryXml = AddIdFilterToQueryString(node["Query"],
                        AllowItems, AllowIndex, DenyItems, DenyIndex);
                    node["Query"].InnerXml = queryXml;

                    WebPart.ListViewXml = xmlDoc.InnerXml;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during  SPListItemTrimmer DoTrimming:", null, ex);
            }

            return (DenyIndex != 0);
        }

        private string AddIdFilterToQueryString(XmlNode parentNode,
            Array allowItems, int allowCount, Array denyItems, int denyCount)
        {
            string resultXml = "";

            if (denyCount == 0)
            {
                resultXml = parentNode.InnerXml;
            }
            else
            {
                resultXml = AddAllowFilterToQueryString(parentNode, allowItems, allowCount);
            }

            return resultXml;
        }

        private string AddAllowFilterToQueryString(XmlNode parentNode,
            Array allowItems, int allowCount)
        {
            string resultXml = "";
            bool bWhereExisted = false;

            int allowFilterCount = allowCount;
            if (allowFilterCount == 0)
                allowFilterCount = 1;

            Array allowFilters = Array.CreateInstance(resultXml.GetType(), allowFilterCount);
            ConvertAllowIdsToAllowFilters(allowItems, allowCount, ref allowFilters, ref allowFilterCount);

            XmlNodeList childNodes = parentNode.ChildNodes;
            string newXml = "";

            foreach (XmlNode child in parentNode.ChildNodes)
            {
                if (child.LocalName.Equals("Where", StringComparison.OrdinalIgnoreCase))
                {
                    string backXml = child.InnerXml;

                    newXml += "<And>";
                    newXml += backXml;

                    for (int i = 0; i < allowFilterCount - 1; i++)
                    {
                        newXml += "<Or>";
                    }

                    newXml += allowFilters.GetValue(0);

                    for (int i = 1; i < allowFilterCount; i++)
                    {
                        newXml += allowFilters.GetValue(i);
                        newXml += "</Or>";
                    }
                    newXml += "</And>";

                    child.InnerXml = newXml;
                    bWhereExisted = true;
                    break;
                }
            }

            if (!bWhereExisted)
            {
                newXml = "<Where>";

                for (int i = 0; i < allowFilterCount - 1; i++)
                {
                    newXml += "<Or>";
                }

                newXml += allowFilters.GetValue(0);

                for (int i = 1; i < allowFilterCount; i++)
                {
                    newXml += allowFilters.GetValue(i);
                    newXml += "</Or>";
                }

                newXml += "</Where>";

                parentNode.InnerXml += newXml;
            }

            resultXml = parentNode.InnerXml;

            return resultXml;
        }

        private void ConvertAllowIdsToAllowFilters(Array allowIds, int allowIdCount,
            ref Array allowFilters, ref int allowFilterCount)
        {
            string xml = "";
            string format = "";

            if (allowIdCount == 0)
            {
                xml = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Counter\">0</Value></Eq>";
                allowFilters.SetValue(xml, 0);

                return;
            }

            int tmp = 0;
            Array tmpAllowIds = Array.CreateInstance(tmp.GetType(), allowIdCount);
            for (int i = 0; i < allowIdCount; i++)
            {
                tmpAllowIds.SetValue(allowIds.GetValue(i), i);
            }

            Array.Sort(tmpAllowIds);

            int smallId = (int)tmpAllowIds.GetValue(0);
            int largeId = smallId;

            if (allowIdCount == 1)
            {
                format = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Eq>";
                xml = String.Format(format, smallId.ToString());
                allowFilters.SetValue(xml, 0);

                return;
            }

            int curId, nextId;
            int j = 0;

            for (int i = 1; i < allowIdCount && j < allowFilterCount; i++)
            {
                curId = (int)tmpAllowIds.GetValue(i);
                nextId = (int)tmpAllowIds.GetValue(i - 1);
                if (curId == (nextId + 1))
                {
                    largeId = curId;
                    continue;
                }
                else
                {
                    if (smallId == largeId)
                    {
                        format = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Eq>";
                        xml = String.Format(format, smallId.ToString());
                    }
                    else
                    {
                        format = "<And>";
                        format += "<Gt><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Gt>";
                        format += "<Lt><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{1}</Value></Lt>";
                        format += "</And>";
                        xml = String.Format(format, (smallId - 1).ToString(), (largeId + 1).ToString());
                    }

                    allowFilters.SetValue(xml, j++);

                    smallId = largeId = curId;
                }
            }

            if (smallId == largeId)
            {
                format = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Eq>";
                xml = String.Format(format, smallId.ToString());
            }
            else
            {
                format = "<And>";
                format += "<Gt><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Gt>";
                format += "<Lt><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{1}</Value></Lt>";
                format += "</And>";
                xml = String.Format(format, (smallId - 1).ToString(), (largeId + 1).ToString());
            }

            allowFilters.SetValue(xml, j++);

            allowFilterCount = j;
        }
    }
}
