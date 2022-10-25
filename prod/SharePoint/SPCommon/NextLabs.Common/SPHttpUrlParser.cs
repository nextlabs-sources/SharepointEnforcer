using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPHttpUrlParser : SPUrlParserBase
    {
        public SPHttpUrlParser(String url)
            : base(url)
        {
        }

        public SPHttpUrlParser(String url, SPWeb web)
            : base(url, web)
        {
        }

        ~SPHttpUrlParser()
        {
        }

        protected override bool ParseWeb(string url)
        {
            bool bRet = false;

            try
            {
                bool _inCache = Utilities.CheckCacheContent(null, url, Utilities.SPUrlWeb);
                if (_inCache)
                {
                    m_Web = (SPWeb)Utilities.GetCachedSPContent(null, url, Utilities.SPUrlWeb);
                }

                if (_inCache && m_Web != null)
                {
                    m_NeedDispose = true;
                    m_Type = SPUrlType.SPUrlTypeWeb;
                    bRet = true;
                }
            }
            catch
            {
            }

            return bRet;
        }

        protected override bool ParseListItem(string url)
        {
            bool bRet = false;
            try
            {
                bool _inCache = Utilities.CheckCacheContent(m_Web, url, Utilities.SPUrlListItem);
                if (_inCache)
                {
                    m_Item = (SPListItem)Utilities.GetCachedSPContent(m_Web, url, Utilities.SPUrlListItem);
                }
                if (_inCache && m_Item != null)
                {
                    m_Type = SPUrlType.SPUrlTypeListItem;
                    bRet = true;
                }
                else
                {
                    string[] QueryStringCol = url.Split(new string[] { "?", "=", "&" }, StringSplitOptions.None);
                    string rootFolder = null;
                    for (int i = 1; i < QueryStringCol.Length; i++)
                    {
                        // For example: http://lab01-w08-sps13/team%20site/Lists/discussionboard1/AllItems.aspx?RootFolder=%2Fteam%20site%2FLists%2Fdiscussionboard1%2Fbbbb&FolderCTID=0x01200200022DAE43C13B7B4EA35CC19F8DE270B5
                        if (QueryStringCol[i].Equals("RootFolder", StringComparison.OrdinalIgnoreCase))
                        {
                            rootFolder = QueryStringCol[i + 1];
                            break;
                        }
                    }
                    if (rootFolder != null)
                    {
                        string itemUrl = m_Web.Site.MakeFullUrl(Globals.UrlDecode(rootFolder));
                        try
                        {
                            m_Item = (SPListItem)Utilities.GetCachedSPContent(m_Web, itemUrl, Utilities.SPUrlListItem);
                            if (m_Item != null)
                            {
                                m_Type = SPUrlType.SPUrlTypeListItem;
                                bRet = true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            return bRet;
        }

        protected override bool ParseList(string url)
        {
            bool bRet = false;
            string ListId = null;
            bool _inCache = false;
            try
            {
                //m_List = m_Web.GetList(url);
                if (url.IndexOf("ListId", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    string[] splitedUrl = url.Split(new string[] { "/", "=", "&" }, StringSplitOptions.None);
                    for (int i = 0; i < splitedUrl.Length; )
                    {
                        if (splitedUrl[i++].Equals("listid", StringComparison.OrdinalIgnoreCase))
                            ListId = Globals.UrlDecode(splitedUrl[i++]);
                    }
                }
                if (ListId != null)
                    _inCache = Utilities.CheckCacheContent(m_Web, ListId, Utilities.SPUrlListID);
                else
                    _inCache = Utilities.CheckCacheContent(m_Web, url, Utilities.SPUrlList);
                if (_inCache)
                {
                    if (ListId != null)
                        m_List = (SPList)Utilities.GetCachedSPContent(m_Web, ListId, Utilities.SPUrlListID);
                    else
                        m_List = (SPList)Utilities.GetCachedSPContent(m_Web, url, Utilities.SPUrlList);
                }
                if (_inCache && m_List != null)
                {
                    if (url.IndexOf("/Pages/Default.aspx", StringComparison.OrdinalIgnoreCase) > 0
                        || url.IndexOf("/SitePages/Home.aspx", StringComparison.OrdinalIgnoreCase) > 0
                        || url.IndexOf("Pages/Home.aspx", StringComparison.OrdinalIgnoreCase) > 0
                        || url.IndexOf("Pages/category.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        // For example, http://lab01-sps06/Pages/Default.aspx should be Web URL, not List Item URL.
                        return bRet;
                    }

                    m_Type = SPUrlType.SPUrlTypeList;
                    bRet = true;

                    int Id = -1;
                    string[] QueryStringCol = url.Split(new string[] { "?", "=", "&" }, StringSplitOptions.None);
                    for (int i = 1; i < QueryStringCol.Length; i++ )
                    {
                        // For example: http://lab01-sps06/Docs/Lists/Announcements/DispForm.aspx?ID=3
                        if (QueryStringCol[i].Equals("ID", StringComparison.OrdinalIgnoreCase))
                        {
                            Id = int.Parse(QueryStringCol[i+1]);
                            break;
                        }
                    }

                    if (Id < 0)
                    {
                        string[] urlItems = url.Split(new string[] { "/" }, StringSplitOptions.None);
                        for (int i = 0; i < urlItems.Length; i++)
                        {
                            // For example: http://lab01-sps06/Docs/Lists/Announcements/Attachments/3/sts3.txt
                            if (urlItems[i].Equals("Attachments", StringComparison.OrdinalIgnoreCase))
                            {
                                Id = int.Parse(urlItems[i + 1]);
                                break;
                            }
                        }
                    }

                    if (Id > 0)
                    {
                        m_Item = GetItemById(m_List, Id);
                        if (m_Item != null)
                        {
                            m_Type = SPUrlType.SPUrlTypeListItem;
                        }
                    }
                    else if (url.IndexOf("/Flat.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        string rootFolder = null;
                        for (int i = 1; i < QueryStringCol.Length; i++)
                        {
                            // For example: http://lab01-w08-sps13/team%20site/Lists/discussionboard1/Flat.aspx?RootFolder=%2Fteam%20site%2FLists%2Fdiscussionboard1%2Fbbbb&FolderCTID=0x01200200022DAE43C13B7B4EA35CC19F8DE270B5
                            if (QueryStringCol[i].Equals("RootFolder", StringComparison.OrdinalIgnoreCase))
                            {
                                rootFolder = QueryStringCol[i + 1];
                                break;
                            }
                        }
                        if (rootFolder != null)
                        {
                            string itemUrl = m_Web.Site.MakeFullUrl(Globals.UrlDecode(rootFolder));
                            if (ParseListItem(itemUrl))
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ParseList:", null, ex);
            }

            return bRet;
        }

        protected override bool ParseApplicationPage(string url)
        {
            try
            {
                NLLogger.OutputLog(LogLevel.Debug, "ParseApplicationPage: URL=" + url, null);

                int intQuery = url.IndexOf("?");
                if (intQuery > 0)
                {
                    string QueryString = url.Substring(intQuery + 1);

                    string[] QueryItems = QueryString.Split(new string[] { "=", "&" }, StringSplitOptions.None);
                    NameValueCollection QueryCols = new NameValueCollection();
                    for (int i = 0; i < QueryItems.Length; )
                    {
                        QueryCols.Add(QueryItems[i], Globals.UrlDecode(QueryItems[i + 1]));
                        i += 2;
                    }

                    // Parse FileName=xxx or ItemUrl=xxx
                    {
                        string strItemUrl = QueryCols["FileName"];
                        string strListItemUrl = null;

                        if (strItemUrl == null)
                            strItemUrl = QueryCols["ItemUrl"];

                        if (strItemUrl != null)
                        {
                            if (strItemUrl.StartsWith(m_Web.Site.Url, StringComparison.OrdinalIgnoreCase))
                                strListItemUrl = strItemUrl;
                            else
                                strListItemUrl = m_Web.Site.Url + strItemUrl;
                        }

                        if (strListItemUrl != null)
                        {
                            Dispose();
                            ParseWeb(strListItemUrl);

                            if (ParseListItem(strListItemUrl))
                                return true;
                        }
                    }

                    if (m_List == null)
                    {
                        string strListId = QueryCols["List"];
                        if (strListId != null)
                            m_List = m_Web.Lists[new Guid(strListId)];
                    }

                    if (m_List == null)
                    {
                        string strListId = QueryCols["ListId"];
                        if (strListId != null)
                            m_List = m_Web.Lists[new Guid(strListId)];
                    }

                    if (m_List == null)
                    {
                        string strListUrl = QueryCols["CancelSource"];
                        if (strListUrl != null)
                            m_List = m_Web.GetList(strListUrl);
                    }

                    // Parse List=xxx&ID=xxx
                    if (m_List != null)
                    {
                        string strListItemId = QueryCols["ID"];
                        if (strListItemId != null)
                        {
                            int Id = int.Parse(strListItemId);
                            if (Id >= 0)
                                m_Item = GetItemById(m_List, Id);
                        }

                        // Parse List=xxx&obj=xxx
                        if (m_Item == null)
                        {
                            // FileName=xxx or obj=xxx
                            string strObj = QueryCols["obj"];
                            if (strObj != null)
                            {
                                int index1 = strObj.IndexOf(",");
                                int index2 = strObj.LastIndexOf(",");
                                if (index1 > 0 && index2 > index1)
                                {
                                    string strId = strObj.Substring(index1 + 1, index2 - index1 - 1);
                                    int intId = Convert.ToInt32(strId);
                                    m_Item = GetItemById(m_List, intId);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Parse Source=xxx
                        string strSource = QueryCols["Source"];
                        if (strSource != null)
                        {
                            Dispose();
                            ParseWeb(strSource);

                            if (ParseListItem(strSource))
                                return true;

                            ParseList(strSource);
                        }
                    }
                }
            }
            catch
            {
            }

            if (m_Item != null)
                m_Type = SPUrlType.SPUrlTypeListItem;
            else if (m_List != null)
                m_Type = SPUrlType.SPUrlTypeList;

            return false;
        }
    }
}
