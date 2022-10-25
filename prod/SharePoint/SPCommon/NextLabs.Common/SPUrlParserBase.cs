using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPUrlParserBase : IUrlParser, IDisposable
    {
        protected String m_Url;
        protected SPSite m_Site;
        protected SPWeb m_Web;
        protected SPList m_List;
        protected SPListItem m_Item;
        protected SPView m_View;

        protected SPUrlType m_Type;
        protected bool m_NeedDispose;


        public virtual SPUrlType UrlType
        {
            get { return m_Type; }
        }

        public virtual String ParsedUrl
        {
            get { return m_Url; }
        }

        public virtual Object ParsedObject
        {
            get
            {
                switch (m_Type)
                {
                    case SPUrlType.SPUrlTypeWeb:
                        return m_Web;
                    case SPUrlType.SPUrlTypeList:
                        return m_List;
                    case SPUrlType.SPUrlTypeListView:
                        return m_View;
                    case SPUrlType.SPUrlTypeListItem:
                        return m_Item;
                    default:
                        return null;
                }
            }
        }

        public SPUrlParserBase(String url)
        {
            m_Url = url;
            m_Type = SPUrlType.SPUrlTypeUnknown;
            m_NeedDispose = true;
        }

        public SPUrlParserBase(String url, SPWeb web)
        {
            m_Url = url;
            m_Web = web;
            m_Type = SPUrlType.SPUrlTypeWeb;
            m_NeedDispose = false;
        }

        ~SPUrlParserBase()
        {
            Dispose();
        }

        #region IDisposable Members
        // Summary:
        //     Performs application-defined tasks associated with freeing, releasing, or
        //     resetting unmanaged resources.
        public virtual void Dispose()
        {
        }
        #endregion

        public virtual void Parse()
        {
            if (m_Web == null)
            {
                if (ParseWeb(m_Url) == false)
                    return;

                m_NeedDispose = true;
            }

            if (ParseList(m_Url))
            {
                if (m_Type != SPUrlType.SPUrlTypeListItem)
                {
                    ParseListItem(m_Url);
                }
            }
            else
            {
                if (m_Url.IndexOf("/_layouts/", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    ParseApplicationPage(m_Url);
                }
            }
        }

        protected virtual bool ParseWeb(string url)
        {
            return false;
        }

        protected virtual bool ParseListItem(string url)
        {
            return false;
        }

        protected virtual bool ParseList(string url)
        {
            return false;
        }

        protected virtual bool ParseApplicationPage(string url)
        {
            return false;
        }

        protected string ReConstructListUrl(SPList list)
        {
            return Globals.ConstructListUrl(list.ParentWeb, list);
        }

        protected SPListItem GetItemById(SPList list, int id)
        {
            SPListItem item = null;
            try
            {
                SPQuery query = new SPQuery();
                query.ItemIdQuery = true;
                query.ViewAttributes = "Scope=\"Recursive\"";
                string format = "<Where><Eq><FieldRef Name=\"ID\" /><Value Type=\"Counter\">{0}</Value></Eq></Where>";
                query.Query = String.Format(format, id.ToString());
                SPListItemCollection items = list.GetItems(query);
                if (list.ItemCount > 0)
                    item = items[0];
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "GetItemById failed:" + ex.Message);
            }
            if (item == null)
                item = list.GetItemById(id);
            return item;
        }
    }
}
