using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.SharePoint;
using System.Web;
using Microsoft.SharePoint.WebControls;
using System.Threading;
using System.Collections;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPEEvalAttrHepler
    {
        public static void SetObjEvalAttr(object obj, SPEEvalAttr evalAttr)
        {
            if (obj != null && evalAttr != null)
            {
                if (obj is SPWeb)
                {
                    SetWebEvalAttr((SPWeb)obj, evalAttr);
                }
                else if (obj is SPList)
                {
                    SetListEvalAttr((SPList)obj, evalAttr);
                }
                else if (obj is SPListItem)
                {
                    SetItemEvalAttr((SPListItem)obj, evalAttr);
                }
                else if (obj is SPSite)
                {
                    SPSite site = obj as SPSite;
                    SetWebEvalAttr(site.RootWeb, evalAttr);
                }
            }
        }


        private static void SetWebEvalAttr(SPWeb web, SPEEvalAttr evalAttr)
        {
            if (web != null && evalAttr != null)
            {
                evalAttr.WebObj = web;
                evalAttr.ObjEvalUrl = web.Url;
                evalAttr.ObjTitle = web.Title;
                evalAttr.ObjName = web.Name;
                evalAttr.ObjDesc = web.Description;
                evalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                evalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
            }
        }

        private static void SetListEvalAttr(SPList list, SPEEvalAttr evalAttr)
        {
            if (list != null && evalAttr != null)
            {
                SPWeb web = list.ParentWeb;
                evalAttr.WebObj = web;
                evalAttr.ListObj = list;
                evalAttr.ObjEvalUrl = Globals.ConstructListUrl(web, list);
                evalAttr.ObjTitle = list.Title;
                evalAttr.ObjName = list.Title;
                evalAttr.ObjDesc = list.Description;

                evalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                if (list.BaseType == SPBaseType.DocumentLibrary)
                {
                    evalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                }
                else
                {
                    evalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                }
            }
        }

        private static void SetItemEvalAttr(SPListItem item, SPEEvalAttr evalAttr)
        {
            if (item != null && evalAttr != null)
            {
                SPList list = item.ParentList;
                SPWeb web = list.ParentWeb;
                string itemName = list.BaseType == SPBaseType.Survey ? item.DisplayName : item.Name;

                evalAttr.WebObj = web;
                evalAttr.ListObj = list;
                evalAttr.ItemObj = item;
                evalAttr.ObjTitle = item.Title;
                evalAttr.ObjName = itemName;
                evalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                if (list.BaseType == SPBaseType.DocumentLibrary)
                {
                    evalAttr.ObjEvalUrl = item.ParentList.ParentWeb.Url + "/" + item.Url; // library item
                    evalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                }
                else
                {
                    evalAttr.ObjEvalUrl = Globals.ConstructListUrl(web, list) + "/" + itemName; // list item
                    evalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                }
            }
        }
    }

    public class SPEEvalAttrs
    {
        private static IDictionary<int, SPEEvalAttr> SPEEvalAttrPool = new SortedList<int, SPEEvalAttr>();
        private static object syncRoot = new Object();
        private static object prefilterLock = new object();
        public static List<string> prefilterSubList = null;
        public static List<string> prefilterResList = null;

        public static void Init()
        {
            SPEEvalAttr _SPEEvalAttr = Current();
            _SPEEvalAttr.Init();
        }
        public static SPEEvalAttr Current()
        {
            if (SPEEvalAttrPool == null)
                SPEEvalAttrPool = new SortedList<int, SPEEvalAttr>();
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (SPEEvalAttrPool.ContainsKey(ssid))
                return SPEEvalAttrPool[ssid];
            else
            {
                SPEEvalAttr newSPEEvalAttr = new SPEEvalAttr();
                lock (syncRoot)
                {
                    SPEEvalAttrPool[ssid] = newSPEEvalAttr;
                    return newSPEEvalAttr;
                }
            }
        }
        public static void prefilterAnalyze(List<string> subList, List<string> resList)
        {
            lock (prefilterLock)
            {
                prefilterSubList = subList;
                prefilterResList = resList;
            }
        }
    }

    public class SPEEvalAttr
    {
        // variables
        private int m_BeginTicks;
        private String m_remoteAddr;
        private String m_httpMethod;
        private String m_requestURL;
        private String m_requestURL_path;
        private String m_logonUser;
        private String m_action = "UNKNOWN_ACTION";
        private String m_obj_name = "";
        private String m_obj_title = "";
        private String m_obj_type = "";
        private String m_obj_subtype = "";
        private String m_obj_description = "";
        private String m_obj_referrerurl = null;
        private String m_obj_targeturl = null;
        private String m_loginname = null;
        private String m_web_url = null;
        private String m_before_url = null;
        private String m_after_url = null;
        //############################################
        private CETYPE.CENoiseLevel_t m_noiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
        private CETYPE.CEAction m_policy_action = (CETYPE.CEAction)(-1);
        //############################################
        private SPList m_listObj = null;
        private SPListItem m_itemObj = null;
        private SPWeb m_webObj = null;
        private SPSite m_siteObject = null;
        //############################################
        private bool m_isPost;
        //When OWA enable, To get SPListItem's properties somehow  will call "File not found" exception.
        private string[] m_Params4OWA = new string[5 * 2];
        private bool m_isOWA = false;
        //############################################
        private String m_fileVersion = null;
        private ArrayList m_alUserAttrs;
        private List<SPWeb> m_webDisposeList;
        private List<SPSite> m_siteDisposeList;

        public SPEEvalAttr()
        {
            m_webDisposeList = new List<SPWeb>();
            m_siteDisposeList = new List<SPSite>();
        }

        public void Init()
        {
            m_BeginTicks = 0;
            m_remoteAddr = null;
            m_httpMethod = null;
            m_requestURL = null;
            m_requestURL_path = null;
            m_logonUser = null;
            m_action = "UNKNOWN_ACTION";
            m_obj_name = "";
            m_obj_title = "";
            m_obj_type = "";
            m_obj_subtype = "";
            m_obj_description = "";
            m_obj_referrerurl = null;
            m_obj_targeturl = null;
            m_loginname = null;
            m_web_url = null;
            m_before_url = null;
            m_after_url = null;
            //############################################
            m_noiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
            m_policy_action = (CETYPE.CEAction)(-1);
            //############################################
            m_listObj = null;
            m_itemObj = null;
            m_webObj = null;
            m_siteObject = null;
            //############################################
            m_isPost = false;
            m_isOWA = false;
            m_Params4OWA = new string[5 * 2];
            //############################################
            m_fileVersion = null;
            m_alUserAttrs = new ArrayList();
        }

        public void GenerateSPWeb(HttpRequest Request)
        {
            try
            {
                try
                {
                    m_webObj = SPControl.GetContextWeb(HttpContext.Current); // This method will happen exception, and we don't care this.
                    m_siteObject = m_webObj.Site;
                }
                catch (Exception /*ex*/)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Failed to get web object from current HTTP content. No need care\n");
                }
                if (m_webObj == null)
                {
                    string siteUrl = null;
                    if (Request.RawUrl.IndexOf("/_vti_bin/", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        if (Request.UrlReferrer != null)
                        {
                            siteUrl = Request.UrlReferrer.AbsoluteUri;
                        }
                    }
                    else
                    {
                        int _index = Request.RawUrl.IndexOf("?");
                        String _RelativeUrl = (_index > -1) ? (Request.RawUrl.Substring(0, _index)) : Request.RawUrl;
                        String _Host = Request.ServerVariables["HTTP_HOST"];
                        String _Http = Request.ServerVariables["HTTPS"];
                        String _Url = "";
                        if (string.IsNullOrEmpty(_Host))
                            _Host = Request.Url.Host;
                        if (_Http != null && _Http.Equals("on", StringComparison.OrdinalIgnoreCase))
                            _Url = "https://" + _Host + _RelativeUrl;
                        else
                            _Url = "http://" + _Host + _RelativeUrl;
                        siteUrl = _Url;
                    }
                    if (!string.IsNullOrEmpty(siteUrl))
                    {
                        m_siteObject = new SPSite(siteUrl);
                        if (m_siteObject != null)
                        {
                            m_webObj = m_siteObject.OpenWeb();
                            m_webDisposeList.Add(m_webObj);
                            m_siteDisposeList.Add(m_siteObject);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SPDispose();
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GenerateSPWeb\n", null, ex);
            }
        }

        public void SPDispose()
        {
            // Dispose all the SPWeb in list.
            if (m_webDisposeList.Count > 0)
            {
                foreach (SPWeb web in m_webDisposeList)
                {
                    try
                    {
                        if (web != null)
                        {
                            web.Dispose();
                        }
                    }
                    catch
                    {
                    }
                }
                m_webDisposeList.Clear();
            }

            // Dispose all the SPSite in list.
            if (m_siteDisposeList.Count > 0)
            {
                foreach (SPSite site in m_siteDisposeList)
                {
                    try
                    {
                        if (site != null)
                        {
                            site.Dispose();
                        }
                    }
                    catch
                    {
                    }
                }
                m_siteDisposeList.Clear();
            }
        }

        public int BeginTicks
        {
            get { return m_BeginTicks; }
            set { m_BeginTicks = value; }
        }
        public string[] Params4OWA
        {
            get { return m_Params4OWA; }
            set { m_Params4OWA = value; }
        }
        public bool IsOWA
        {
            get { return m_isOWA; }
            set { m_isOWA = value; }
        }
        public String RemoteAddr
        {
            get { return m_remoteAddr; }
            set { m_remoteAddr = value; }
        }
        public String HttpMethod
        {
            get { return m_httpMethod; }
            set { m_httpMethod = value; }
        }
        public String RequestURL
        {
            get { return m_requestURL; }
            set { m_requestURL = value; }
        }
        public String RequestURL_path
        {
            get { return m_requestURL_path; }
            set { m_requestURL_path = value; }
        }
        public String LogonUser
        {
            get { return m_logonUser; }
            set { m_logonUser = value; }
        }
        public String Action
        {
            get { return m_action; }
            set { m_action = value; }
        }
        public String ObjName
        {
            get { return m_obj_name; }
            set { m_obj_name = value; }
        }
        public String ObjTitle
        {
            get { return m_obj_title; }
            set { m_obj_title = value; }
        }
        public String ObjType
        {
            get { return m_obj_type; }
            set { m_obj_type = value; }
        }
        public String ObjSubtype
        {
            get { return m_obj_subtype; }
            set { m_obj_subtype = value; }
        }
        public String ObjDesc
        {
            get { return m_obj_description; }
            set { m_obj_description = value; }
        }
        public String ObjEvalUrl
        {
            get { return m_obj_referrerurl; }
            set { m_obj_referrerurl = value; }
        }
        public String ObjTargetUrl
        {
            get { return m_obj_targeturl; }
            set { m_obj_targeturl = value; }
        }
        public String LoginName
        {
            get { return m_loginname; }
            set { m_loginname = value; }
        }
        public String WebUrl
        {
            get { return m_web_url; }
            set { m_web_url = value; }
        }
        public String BeforeUrl
        {
            get { return m_before_url; }
            set { m_before_url = value; }
        }
        public String AfterUrl
        {
            get { return m_after_url; }
            set { m_after_url = value; }
        }
        //############################################
        public CETYPE.CENoiseLevel_t NoiseLevel
        {
            get { return m_noiseLevel; }
            set { m_noiseLevel = value; }
        }
        public CETYPE.CEAction PolicyAction
        {
            get { return m_policy_action; }
            set { m_policy_action = value; }
        }
        //############################################
        public SPList ListObj
        {
            get { return m_listObj; }
            set { m_listObj = value; }
        }
        public SPListItem ItemObj
        {
            get { return m_itemObj; }
            set { m_itemObj = value; }
        }
        public SPWeb WebObj
        {
            get { return m_webObj; }
            set { m_webObj = value; }
        }
        public SPSite SiteObj
        {
            get { return m_siteObject; }
            set { m_siteObject = value; }
        }
        //############################################
        public bool IsPost
        {
            get { return m_isPost; }
            set { m_isPost = value; }
        }
        public string FileVersion
        {
            get { return m_fileVersion; }
            set { m_fileVersion = value; }
        }
        public ArrayList UserAttrs
        {
            get { return m_alUserAttrs; }
            set { m_alUserAttrs = value; }
        }
        public void AddDisposeSite(SPSite site)
        {
            m_siteDisposeList.Add(site);
        }
        public void AddDisposeWeb(SPWeb web)
        {
            m_webDisposeList.Add(web);
        }
    }
}