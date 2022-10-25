using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPSts3UrlParser : SPUrlParserBase
    {
        public SPSts3UrlParser(String url)
            : base(url)
        {
        }

        ~SPSts3UrlParser()
        {
        }

        public override String ParsedUrl
        {
            get
            {
                switch (m_Type)
                {
                    case SPUrlType.SPUrlTypeWeb:
                        return m_Web.Url;
                    case SPUrlType.SPUrlTypeList:
                        return ReConstructListUrl(m_List);
                    case SPUrlType.SPUrlTypeListView:
                        return (m_Web.Url + "/" + m_View.Url);
                    case SPUrlType.SPUrlTypeListItem:
                        {
                            string itemUrl;
                            SPList list = m_Item.ParentList;

                            if (list.BaseType == SPBaseType.DocumentLibrary)
                            {
                                itemUrl = list.ParentWeb.Url + "/" + m_Item.Url;
                            }
                            else
                            {
                                string listUrl = ReConstructListUrl(list);
                                string itemName;
                                if (list.BaseType == SPBaseType.Survey)
                                    itemName = m_Item.DisplayName;
                                else
                                    itemName = m_Item.Name;

                                itemUrl = listUrl + "/" + itemName;
                            }
                            return itemUrl;
                        }
                    default:
                        return null;
                }
            }
        }

        protected override bool ParseWeb(string url)
        {
            string[] splitedUrl = url.Split(new string[] { "/", "=" }, StringSplitOptions.None);
            string SiteId = null;
            string WebId = null;
            bool bRet = false;

            try
            {
                for (int i = 0; i < splitedUrl.Length; )
                {
                    if (splitedUrl[i++].Equals("siteid", StringComparison.OrdinalIgnoreCase))
                    {
                        SiteId = HttpUtility.UrlDecode(splitedUrl[i++]);
                    }

                    if (splitedUrl[i].Equals("webid", StringComparison.OrdinalIgnoreCase))
                        WebId = HttpUtility.UrlDecode(splitedUrl[++i]);

                    if (SiteId != null && WebId != null)
                        break;
                }

                if (SiteId != null && WebId != null)
                {
                    m_Site = new SPSite(new Guid(SiteId));
                    if (m_Site != null)
                    {
                        SPEEvalAttrs.Current().AddDisposeSite(m_Site);
                        m_Web = m_Site.OpenWeb(new Guid(WebId));
                    }
                    if (m_Web != null)
                    {
                        SPEEvalAttrs.Current().AddDisposeWeb(m_Web);
                        m_NeedDispose = true;
                        m_Type = SPUrlType.SPUrlTypeWeb;
                        bRet = true;
                    }
                }
            }
            catch (Exception)
            {
            }

            return bRet;
        }

        protected override bool ParseListItem(string url)
        {
            bool bRet = false;
            string[] splitedUrl = url.Split(new string[] { "/", "=" }, StringSplitOptions.None);
            string ListId = null;
            string ItemId = null;

            try
            {
                if (m_Web != null)
                {
                    for (int i = 0; i < splitedUrl.Length; )
                    {
                        if (splitedUrl[i++].Equals("listid", StringComparison.OrdinalIgnoreCase))
                        {
                            ListId = HttpUtility.UrlDecode(splitedUrl[i++]);
                        }

                        if (splitedUrl[i].Equals("itemid", StringComparison.OrdinalIgnoreCase))
                            ItemId = HttpUtility.UrlDecode(splitedUrl[++i]);

                        if (ListId != null && ItemId != null)
                            break;
                    }

                    if (ListId != null && ItemId != null)
                    {
                        m_List = (SPList)Utilities.GetCachedSPContent(m_Web, ListId, Utilities.SPUrlListID);
                        m_Item = GetItemById(m_List, int.Parse(ItemId));
                        if (m_Item != null)
                        {
                            m_Type = SPUrlType.SPUrlTypeListItem;
                            bRet = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ParseListItem: ", null, ex);
            }

            return bRet;
        }


        protected override bool ParseList(string url)
        {
            bool bRet = false;
            string[] splitedUrl = url.Split(new string[] { "/", "=" }, StringSplitOptions.None);
            string ListId = null;

            try
            {
                if (m_Web != null)
                {
                    for (int i = 0; i < splitedUrl.Length; )
                    {
                        if (splitedUrl[i++].Equals("listid", StringComparison.OrdinalIgnoreCase))
                            ListId = HttpUtility.UrlDecode(splitedUrl[i++]);

                        if (ListId != null)
                            break;
                    }

                    if (ListId != null)
                    {
                        m_List = (SPList)Utilities.GetCachedSPContent(m_Web, ListId, Utilities.SPUrlListID);
                        if (m_List != null)
                        {
                            m_Type = SPUrlType.SPUrlTypeList;
                            bRet = true;
                        }
                    }
                }
            }
            catch
            {
            }

            return bRet;
        }
    }
}
