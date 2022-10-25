using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.CSCInvoke;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPListItemEvaluation : EvaluationBase
    {
        private SPListItem m_Item;
        private SPList m_List;
        private SPWeb m_Web;
        private int m_iTTL;
        private DateTime m_Evaltime;

        public SPListItem ListItem
        {
            get { return m_Item; }
            set { m_Item = value; }
        }

        public int TTL
        {
            get { return m_iTTL; }
            set { m_iTTL = value; }
        }

        public SPListItemEvaluation(SPListItem item, CETYPE.CEAction action, String url, String host, String module, SPUser user)
            : base(action, url, host, module, user)
        {
            m_Item = item;
            m_List = m_Item.ParentList;
            m_Web = m_List.ParentWeb;
            m_iTTL = 0;
        }

        public SPListItemEvaluation(SPListItem item, CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
            : base(action, url, host, module, username, userSid)
        {
            m_Item = item;
            m_List = m_Item.ParentList;
            m_Web = m_List.ParentWeb;
            m_iTTL = 0;
        }

        override public string ReConstructUrl()
        {
            string itemUrl = "";

            if (m_List.BaseType == SPBaseType.DocumentLibrary)
            {
                itemUrl = m_Web.Url + "/" + m_Item.Url;
            }
            else
            {
                string listUrl = ReConstructListUrl(m_List, m_Web);
                string itemName;
                if (m_List.BaseType == SPBaseType.Survey)
                    itemName = m_Item.DisplayName;
                else
                    itemName = m_Item.Name;

                itemUrl = listUrl + "/" + itemName;
            }

            string[] splitedUrls = m_Url.Split(new char[] { '?' });

            string ListItemKey = "/Attachments/" + m_Item.ID.ToString();
            int trimPos = splitedUrls[0].IndexOf(ListItemKey, StringComparison.OrdinalIgnoreCase);
            if (trimPos > 0)
            {
                if (splitedUrls[0].Length > (trimPos + ListItemKey.Length + 1))
                    itemUrl += splitedUrls[0].Substring(trimPos + ListItemKey.Length); ;
            }
            else
            {
                string fakedItemUrl = Utilities.ConstructSPObjectUrl(m_Item); //m_Web.Url + "/" + m_Item.Url;
                if (!fakedItemUrl.Equals(splitedUrls[0], StringComparison.OrdinalIgnoreCase))
                    itemUrl += splitedUrls[0].Substring(splitedUrls[0].LastIndexOf("/"));
            }

            return itemUrl;
        }

        override public bool Run()
        {
            bool bResult = true;

            try
            {
                String objTargetUrl = null;
                String objReferrerUrl = null;
                if (m_Item != null)
                {
                    bool bCacheExisted = QueryEvaluationResultCache(ref bResult);
                    if (bCacheExisted && CheckCache)
                        return bResult;

                    objReferrerUrl = ReConstructUrl();

                    EvaluatorContext evaContext = new EvaluatorContext();
                    evaContext.ActionType = m_Action;
                    evaContext.NoiseLevel = NoiseLevel.Application;
                    evaContext.Web = m_Web;
                    evaContext.RemoteAddress = m_HostAddress;
                    evaContext.UserName = m_UserName;
                    evaContext.UserSid = m_UserSid;
                    evaContext.SrcName = objReferrerUrl;
                    evaContext.TargetName = objTargetUrl;

                    EvaluatorProperties evaProperties = new EvaluatorProperties();
                    List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
                    evaProperties.ConstructForItem(m_Item, ref attributes);
                    evaContext.SrcAttributes = attributes;

                    //Add a page type attribute
                    if (m_Url.IndexOf("/_layouts", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                            EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_APPLICATION);
                        evaContext.SrcAttributes.Add(keyVaule);
                    }
                    else
                    {
                        KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                            EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_NORMAL);
                        evaContext.SrcAttributes.Add(keyVaule);
                    }

                    Evaluator evaluator = new Evaluator();
                    evaluator.EVALOBJECT = m_Item;
                    if (Globals.g_JPCParams.bUseJavaPC)
                        bResult = evaluator.CheckPortal_CloudAZ(ref evaContext);
                    else
                        bResult = evaluator.CheckPortal(ref evaContext);
                    m_iTTL = evaContext.CacheHint;
                    m_Evaltime = DateTime.Now;
                    if (CheckCache)
                    {
                        AddEvaluationResultCache(bResult);
                    }
                }
            }

            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SPListItemEvaluation:", null, ex);
            }

            return bResult;
        }

        protected override bool QueryEvaluationResultCache(ref bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            string guid = m_List.ID.ToString() + m_Item.ID.ToString();

            bool bExisted = EvaluationCache.Instance.GetValue(m_HostAddress, userId, guid,
                ref bAllow, ref savedQuery, Utilities.GetLastModifiedTime(m_Item));

            return bExisted;
        }

        protected override void AddEvaluationResultCache(bool bAllow)
        {
            string userId = m_UserName;
            string savedQuery = "";
            string guid = m_List.ID.ToString() + m_Item.ID.ToString();
            TimeSpan span = new TimeSpan(0, m_iTTL, 0);

            EvaluationCache.Instance.Add(m_HostAddress, userId, guid, bAllow, savedQuery, span, Utilities.GetLastModifiedTime(m_Item), m_Evaltime);
        }
    }
}
