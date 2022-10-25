using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Administration.Security;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Web;
using Microsoft.SharePoint.WebControls;
using NextLabs.CSCInvoke;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.Administration.Claims;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class CommonVar
    {
        private static IDictionary<int, SPWebCache> SPWebContainers = null;
        private static IDictionary<int, SPListCache> SPListContainers = null;
        private static IDictionary<int, SPListItemCache> SPListItemContainers = null;
        private static IDictionary<int, SPObjCache> SPObjContainers = null;
        private static IDictionary<int, SPObjCache> SPSiteObjContainers = null;
        private static object syncWebRoot = new Object();
        private static object syncListRoot = new Object();
        private static object syncListItemRoot = new Object();
        private static object syncObjRoot = new Object();

        static CommonVar()
        {
            Init();
        }

        public static void Init()
        {
            lock (syncWebRoot)
            {
                if (SPWebContainers == null)
                    SPWebContainers = new SortedList<int, SPWebCache>();
            }
            lock (syncListRoot)
            {
                if (SPListContainers == null)
                    SPListContainers = new SortedList<int, SPListCache>();
            }
            lock (syncListItemRoot)
            {
                if (SPListItemContainers == null)
                    SPListItemContainers = new SortedList<int, SPListItemCache>();
            }
            lock (syncObjRoot)
            {
                if (SPObjContainers == null)
                    SPObjContainers = new SortedList<int, SPObjCache>();
            }
            lock (syncObjRoot)
            {
                if (SPSiteObjContainers == null)
                    SPSiteObjContainers = new SortedList<int, SPObjCache>();
            }
        }

        public static void Clear()
        {
            try
            {
                int ssid = Thread.CurrentThread.ManagedThreadId;
                if (SPWebContainers != null && SPWebContainers.ContainsKey(ssid))
                {
                    if (SPWebContainers[ssid] != null)
                        SPWebContainers[ssid].Close();
                    lock (syncWebRoot)
                    {
                        SPWebContainers.Remove(ssid);
                    }
                }
                if (SPListContainers != null && SPListContainers.ContainsKey(ssid))
                {
                    if (SPListContainers[ssid] != null)
                        SPListContainers[ssid].Close();
                    lock (syncListRoot)
                    {
                        SPListContainers.Remove(ssid);
                    }
                }
                if (SPListItemContainers != null && SPListItemContainers.ContainsKey(ssid))
                {
                    if (SPListItemContainers[ssid] != null)
                        SPListItemContainers[ssid].Close();
                    lock (syncListItemRoot)
                    {
                        SPListItemContainers.Remove(ssid);
                    }
                }
                if (SPObjContainers != null && SPObjContainers.ContainsKey(ssid))
                {
                    lock (syncObjRoot)
                    {
                        if (SPObjContainers[ssid] != null)
                            SPObjContainers[ssid].Close();
                        SPObjContainers.Remove(ssid);
                    }
                }
                if (SPSiteObjContainers != null && SPSiteObjContainers.ContainsKey(ssid))
                {
                    lock (syncObjRoot)
                    {
                        if (SPSiteObjContainers[ssid] != null)
                            SPSiteObjContainers[ssid].Close();
                        SPSiteObjContainers.Remove(ssid);
                    }
                }
            }
            catch
            {
            }
        }

        public static Object GetSPObjectContent(Object _spobject, string _url, string _type)
        {
            //for SP2013, if url is /_layouts/15
            //it's not valid to get SP object
            if (string.IsNullOrEmpty(_url) || _url.StartsWith("/_layouts"))
                return null;

            SPObjCache _SPObjCache = null;
            SPObjCache _SPSiteObjCache = null;
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (SPObjContainers.ContainsKey(ssid))
            {
                _SPObjCache = SPObjContainers[ssid];
            }
            else
            {
                _SPObjCache = new SPObjCache();
                lock (syncObjRoot)
                {
                    SPObjContainers[ssid] = _SPObjCache;
                }
            }
            if (SPSiteObjContainers.ContainsKey(ssid))
            {
                _SPSiteObjCache = SPSiteObjContainers[ssid];
            }
            else
            {
                _SPSiteObjCache = new SPObjCache();
                lock (syncObjRoot)
                {
                    SPSiteObjContainers[ssid] = _SPSiteObjCache;
                }
            }
            Object _target = null;
            SPSite _Site = null;
            string _key = _url + _type;
            if (_SPObjCache.SPObjContainer.ContainsKey(_key))
            {
                _target = _SPObjCache.SPObjContainer[_key];
            }
            else
            {
                try
                {
                    if (_type.Equals(Utilities.SPUrlWeb, StringComparison.OrdinalIgnoreCase))
                    {
                        _Site = new SPSite(_url);
                        if (_Site != null)
                        {
                            SPEEvalAttrs.Current().AddDisposeSite(_Site);
                            SPWeb web = _Site.OpenWeb();
                            if (web != null)
                            {
                                SPEEvalAttrs.Current().AddDisposeWeb(web);
                                _target = web;
                            }
                        }
                    }
                    else if (_type.Equals(Utilities.SPUrlList, StringComparison.OrdinalIgnoreCase))
                    {
                        SPWeb _web = (SPWeb)_spobject;
                        _target = (Object)_web.GetList(_url);
                    }
                    else if (_type.Equals(Utilities.SPUrlListID, StringComparison.OrdinalIgnoreCase))
                    {
                        SPWeb _web = (SPWeb)_spobject;
                        _target = (Object)_web.Lists[new Guid(_url)];

                    }
                    else if (_type.Equals(Utilities.SPUrlListItem, StringComparison.OrdinalIgnoreCase))
                    {
                        SPWeb _web = (SPWeb)_spobject;
                        _target = (Object)_web.GetListItem(_url);
                    }
                }
                catch
                {
                }
                if (_Site != null)
                {
                    lock (syncObjRoot)
                    {
                        _SPSiteObjCache.SPObjContainer.Add(_key, (Object)_Site);
                    }
                }
                if (_target != null)
                {
                    lock (syncObjRoot)
                    {
                        _SPObjCache.SPObjContainer.Add(_key, _target);
                    }
                }
            }
            return _target;
        }

        public static string GetSPWebContent(SPWeb SPObject, string content)
        {
            {
                int ssid = Thread.CurrentThread.ManagedThreadId;
                string spcontent = null;
                if (SPObject == null || content == null)
                {
                    return spcontent;
                }
                try
                {
                    SPWebCache _SPWebCache = null;
                    if (SPWebContainers == null)
                    {
                        return spcontent;
                    }
                    if (SPWebContainers.ContainsKey(ssid))
                    {
                        _SPWebCache = SPWebContainers[ssid];
                    }
                    else
                    {
                        _SPWebCache = new SPWebCache();
                        lock (syncWebRoot)
                        {
                            SPWebContainers[ssid] = _SPWebCache;
                        }
                    }
                    if (_SPWebCache == null)
                    {
                        return spcontent;
                    }
                    if (_SPWebCache.SPWebContainer == null)
                    {
                        return spcontent;
                    }
                    if (_SPWebCache.SPWebContainer.ContainsKey(content))
                    {
                        spcontent = _SPWebCache.SPWebContainer[content];
                        return spcontent;
                    }
                    if (content.Equals("url", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Url;
                    else if (content.Equals("siteurl", StringComparison.OrdinalIgnoreCase))
                    {
                        if (SPObject.Site != null)
                        {
                            spcontent = SPObject.Site.Url;
                        }
                        else
                        {
                            return spcontent;
                        }
                    }
                    else if (content.Equals("loginname", StringComparison.OrdinalIgnoreCase))
                    {
                        if (SPObject.CurrentUser != null)
                        {
                            spcontent = SPObject.CurrentUser.LoginName;
                        }
                        else
                        {
                            return spcontent;
                        }
                    }
                    else if (content.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        if (SPObject.ID != null)
                        {
                            spcontent = SPObject.ID.ToString();
                        }
                        else
                        {
                            return spcontent;
                        }
                    }
                    else if (content.Equals("description", StringComparison.OrdinalIgnoreCase))
                    {
                        spcontent = SPObject.Description;
                    }
                    else if (content.Equals("name", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Name;
                    else if (content.Equals("title", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Title;
                    lock (syncWebRoot)
                    {
                        _SPWebCache.SPWebContainer[content] = spcontent;
                    }
                }
                catch
                {
                }
                return spcontent;
            }
        }


        public static string GetSPListContent(SPList SPObject, string content)
        {
            {
                int ssid = Thread.CurrentThread.ManagedThreadId;
                string spcontent = null;
                if (SPObject == null || content == null)
                {
                    return spcontent;
                }
                try
                {
                    SPListCache _SPListCache = null;
                    if (SPListContainers == null)
                    {
                        return spcontent;
                    }
                    if (SPListContainers.ContainsKey(ssid))
                    {
                        _SPListCache = SPListContainers[ssid];
                    }
                    else
                    {
                        _SPListCache = new SPListCache();
                        lock (syncListRoot)
                        {
                            SPListContainers[ssid] = _SPListCache;
                        }
                    }
                    if (_SPListCache.SPListContainer == null)
                    {
                        return spcontent;
                    }
                    if (_SPListCache.SPListContainer.ContainsKey(content))
                    {
                        spcontent = _SPListCache.SPListContainer[content];
                        return spcontent;
                    }
                    if (content.Equals("url", StringComparison.OrdinalIgnoreCase))
                    {
                        spcontent = SPObject.DefaultViewUrl;
                    }
                    else if (content.Equals("title", StringComparison.OrdinalIgnoreCase))
                    {
                        spcontent = SPObject.Title;
                    }
                    else if (content.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        if (SPObject.ID != null)
                        {
                            spcontent = SPObject.ID.ToString();
                        }
                        else
                        {
                            return spcontent;
                        }
                    }
                    else if (content.Equals("description", StringComparison.OrdinalIgnoreCase))
                    {
                        spcontent = SPObject.Description;
                    }
                    else if (content.Equals("basetype", StringComparison.OrdinalIgnoreCase))
                    {
                        spcontent = SPObject.BaseType.ToString();
                    }
                    lock (syncListRoot)
                    {
                        _SPListCache.SPListContainer[content] = spcontent;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during GetSPListContent:", null, ex);
                }
                return spcontent;
            }
        }

        public static string GetSPListItemContent(SPListItem SPObject, string content)
        {
            {
                int ssid = Thread.CurrentThread.ManagedThreadId;
                string spcontent = null;
                try
                {
                    SPListItemCache _SPListItemCache = null;
                    if (SPListItemContainers.ContainsKey(ssid))
                        _SPListItemCache = SPListItemContainers[ssid];
                    else
                    {
                        _SPListItemCache = new SPListItemCache();
                        lock (syncListItemRoot)
                        {
                            SPListItemContainers[ssid] = _SPListItemCache;
                        }
                    }

                    if (_SPListItemCache.SPListItemContainer.ContainsKey(content))
                    {
                        spcontent = _SPListItemCache.SPListItemContainer[content];
                        return spcontent;
                    }
                    if (content.Equals("url", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Url;
                    else if (content.Equals("name", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Name;
                    else if (content.Equals("displayname", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.DisplayName;
                    else if (content.Equals("title", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.Title;
                    else if (content.Equals("id", StringComparison.OrdinalIgnoreCase))
                        spcontent = SPObject.ID.ToString();
                    lock (syncListItemRoot)
                    {
                        _SPListItemCache.SPListItemContainer[content] = spcontent;
                    }
                }
                catch
                {
                }
                return spcontent;
            }
        }
    }

    class SPObjCache
    {
        private Hashtable m_SPObjContainer = null;

        public Hashtable SPObjContainer
        {
            get { return m_SPObjContainer; }
        }

        public SPObjCache()
        {
            if (m_SPObjContainer == null)
                m_SPObjContainer = new Hashtable();
        }

        public void Close()
        {
            if (m_SPObjContainer != null)
            {
                m_SPObjContainer.Clear();
            }
        }
    }

    class SPWebCache
    {
        private IDictionary<string, string> m_SPWebContainer = null;

        public IDictionary<string, string> SPWebContainer
        {
            get { return m_SPWebContainer; }
        }
        public SPWebCache()
        {
            if (m_SPWebContainer == null)
                m_SPWebContainer = new SortedList<string, string>();
        }

        public void Close()
        {
            if (m_SPWebContainer != null)
                m_SPWebContainer.Clear();
        }
    }

    class SPListCache
    {
        private IDictionary<string, string> m_SPListContainer = null;

        public IDictionary<string, string> SPListContainer
        {
            get { return m_SPListContainer; }
        }

        public SPListCache()
        {
            if (m_SPListContainer == null)
                m_SPListContainer = new SortedList<string, string>();
        }

        public void Close()
        {
            if (m_SPListContainer != null)
                m_SPListContainer.Clear();
        }
    }

    class SPListItemCache
    {
        private IDictionary<string, string> m_SPListItemContainer = null;

        public IDictionary<string, string> SPListItemContainer
        {
            get { return m_SPListItemContainer; }
        }

        public SPListItemCache()
        {
            if (m_SPListItemContainer == null)
                m_SPListItemContainer = new SortedList<string, string>();
        }

        public void Close()
        {
            if (m_SPListItemContainer != null)
                m_SPListItemContainer.Clear();
        }
    }

    public class Utilities
    {
        [DllImport("kernel32.dll", EntryPoint = "OpenFileMapping", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenFileMapping(int dwDesiredAccess, bool bInheritHandle, String lpName);
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern void CloseHandle(IntPtr hptr);
        public const int FILE_MAP_WRITE = 0x0002;
        private static String _sSearchApp = "";
        private static bool _sbSearchApp = false;

        private static Hashtable _sSpurltable = null;
        public static string SPUrlWeb = "SPURLWEB";
        public static string SPUrlList = "SPURLLIST";
        public static string SPUrlListID = "SPURLLISTID";
        public static string SPUrlListItem = "SPURLLISTITEM";
        private static object syncUrlRoot = new Object();
        private static void InitUrlTable()
        {
            lock (syncUrlRoot)
            {
                if (_sSpurltable == null)
                {
                    _sSpurltable = new Hashtable();
                }
            }
        }


        public static bool CheckCacheContent(Object _spobject, string _url, string _type)
        {
            InitUrlTable();
            if (string.IsNullOrEmpty(_url))
                return false;
            string _key = _url + _type;
            if (_sSpurltable.ContainsKey(_key))
            {
                string value = (string)_sSpurltable[_key];
                if (value != null && value.Equals("cached"))
                    return true;
            }
            else
            {
                string value = "cached";
                string targettype = null;
                targettype = CheckObjectType(_spobject, _url, _type);
                if (targettype != null)
                {
                    //Get from a list id is actually getting a list
                    if (_type == SPUrlListID)
                        _type = SPUrlList;
                    if (_type.Equals(targettype))
                    {
                        lock (syncUrlRoot)
                        {
                            _sSpurltable.Add(_key, value);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private static string CheckObjectType(Object _spobject, string _url, string _type)
        {
            SPSite _Site = null;
            SPWeb _web = null;
            try
            {
                SPList _list = null;
                SPListItem _listitem = null;
                if (_type.Equals(Utilities.SPUrlWeb, StringComparison.OrdinalIgnoreCase))
                {
                    _Site = new SPSite(_url);
                    if (_Site != null)
                        _web = _Site.OpenWeb();
                    if (_web == null)
                    {
                        SPWeb _web1 = (SPWeb)_spobject;
                        _list = _web1.GetList(_url);
                        if (_list == null)
                        {
                            _listitem = _web1.GetListItem(_url);
                            if (_listitem != null)
                                return Utilities.SPUrlListItem;
                        }
                        else
                            return Utilities.SPUrlList;
                    }
                    else
                        return Utilities.SPUrlWeb;
                }
                else if (_type.Equals(Utilities.SPUrlList, StringComparison.OrdinalIgnoreCase))
                {
                    SPWeb _web1 = (SPWeb)_spobject;
                    _list = _web1.GetList(_url);
                    if (_list == null)
                    {
                        _listitem = _web1.GetListItem(_url);
                        if (_listitem == null)
                        {
                            _Site = new SPSite(_url);
                            if (_Site != null)
                            {
                                _web = _Site.OpenWeb();
                                if (_web != null)
                                    return Utilities.SPUrlWeb;
                            }
                        }
                        else
                            return Utilities.SPUrlListItem;
                    }
                    else
                        return Utilities.SPUrlList;
                }
                else if (_type.Equals(Utilities.SPUrlListID, StringComparison.OrdinalIgnoreCase))
                {
                    SPWeb _web1 = (SPWeb)_spobject;
                    _list = _web1.Lists[new Guid(_url)];
                    if (_list != null)
                        return Utilities.SPUrlList;

                }
                else if (_type.Equals(Utilities.SPUrlListItem, StringComparison.OrdinalIgnoreCase))
                {
                    SPWeb _web1 = (SPWeb)_spobject;
                    _listitem = _web1.GetListItem(_url);
                    if (_listitem == null)
                    {
                        _list = _web1.GetList(_url);
                        if (_list == null)
                        {
                            _Site = new SPSite(_url);
                            if (_Site != null)
                            {
                                _web = _Site.OpenWeb();
                                if (_web != null)
                                    return Utilities.SPUrlWeb;
                            }
                        }
                        else
                            return Utilities.SPUrlList;
                    }
                    else
                        return Utilities.SPUrlListItem;
                }
                return null;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CheckObjectType, SPObject:[{0}], Url:[{1}], Type:[{2}]",  new object[] { _spobject,  _url, _type }, ex);
                return null;
            }
            finally
            {
                if (_web != null)
                {
                    _web.Dispose();
                    _web = null;
                }
                if (_Site != null)
                {
                    _Site.Dispose();
                    _Site = null;
                }

            }

        }


        //this function is get a list object,and if the list url is not in cache, cache it
        public static Object GetCachedSPContent(Object _spobject, string _url, string _type)
        {
            return CommonVar.GetSPObjectContent(_spobject, _url, _type);
        }

        private static void InitSAPPConfig()
        {
            if (_sbSearchApp)
                return;
            try
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegCEInstallDir = null;
                string CEBinDir = null;
                if (CE_key != null)
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                if (RegCEInstallDir != null)
                {
                    String RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (RegCEInstallDir_str.EndsWith("\\"))
                        CEBinDir = RegCEInstallDir_str + "config\\SearchService.cfg";
                    else
                        CEBinDir = RegCEInstallDir_str + "\\config\\SearchService.cfg";
                }
                if (CEBinDir == null)
                {
                    return;
                }
                using (FileStream fs = new FileStream(CEBinDir, FileMode.Open, FileAccess.Read))
                {
                    if (fs != null)
                    {
                        byte[] _filecontent = new byte[fs.Length];
                        fs.Read(_filecontent, 0, (int)fs.Length);
                        String _spFilecontent = System.Text.Encoding.ASCII.GetString(_filecontent);
                        _sSearchApp = _spFilecontent;
                        fs.Close();
                        _sbSearchApp = true;
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during InitSAPPConfig:", null, ex);
            }
            return;
        }

        //this function is used to check whether cepc is already running
        public static bool SPECommon_Isup()
        {
            bool result = true;
            try
            {
                if(Globals.g_JPCParams.bUseJavaPC)
                {
                    //in cepc code: 
                    //HANDLE hPIDFileMapping = CreateFileMapping(**, **, **, **, **, b67546e2-6dc7-4d07-aa8a-e1647d29d4d7);
                    //if(hPIDFileMapping == NULL || GetLastError() == ERROR_ALREADY_EXISTS)
                    //{
                    //  TRACE(CELOG_WARNING, _T(Policy Controller already running\n));
                    //}
                    NLLogger.OutputLog(LogLevel.Info, "use jpc so need query pc and do not need check cepc is running");
                    return result;
                }
                IntPtr hPIDFileMapping = OpenFileMapping(FILE_MAP_WRITE, false, "Global\\b67546e2-6dc7-4d07-aa8a-e1647d29d4d7");
                if (hPIDFileMapping == IntPtr.Zero)
                    result = false;
                else
                    CloseHandle(hPIDFileMapping);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during SPECommon_Isup, open file mapping failed\n", null, ex);
            }
            return result;
        }

        public static string ClaimUserConvertion(string _inputuser)
        {
            string _outputuser = _inputuser;
            if (!string.IsNullOrEmpty(_outputuser))
            {
                if (_outputuser.Contains("|"))
                {
                    int iPos = _outputuser.IndexOf("|");
                    _outputuser = _outputuser.Substring(iPos + 1);
                    _outputuser = _outputuser.Replace("|", ":");
                }
                else
                {
                    try
                    {
                        SPClaim _user = SPClaimProviderManager.Local.DecodeClaim(_outputuser);
                        _outputuser = _user.Value;
                    }
                    catch
                    {
                    }
                }
            }
            return _outputuser;
        }


        public static bool IsSearchingRequest(HttpRequest _request)
        {
            try
            {
                if (_request != null)
                {
                    if (_request.UserAgent != null && _request.UserAgent.IndexOf("MS Search") != -1)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during IsSearchingRequest:", null, ex);
            }
            return false;
        }

        public static bool IsDefaultIndexingAccount(string account)
        {
            // George: Don't filter the user in search crawler.
            return false;
            /*
            bool bRet = false;
            bool bError = false;
            try
            {
                int ssid = Thread.CurrentThread.ManagedThreadId;
                List<String> GatheringAccounts = new List<String>();
                if (EvaluationUserCache.Instance.IfCacheTimeOut(ssid))
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        SPFarm localFarm = SPFarm.Local;
#if SP2016 || SP2019
                        SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch16");
#elif SP2013
                        SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch15");
#elif SP2010
                        SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch14");
#endif
                        foreach (SearchServiceApplication app in searchService.SearchApplications)
                        {
                            Content content = new Content(app);
                            String GatheringAccount = content.DefaultGatheringAccount;
                            GatheringAccounts.Add(GatheringAccount);
                        }
                        if (GatheringAccounts.Count <= 0)
                            GatheringAccounts.Add("Dummy");
                    });
                    EvaluationUserCache.Instance.Add(ssid, GatheringAccounts.ToArray());
                }
                bRet = EvaluationUserCache.Instance.CompareValue(ssid, account);
            }
            catch (Exception e)
            {
                bError = true;
                CE_Log.LOG(LogLevel.Info, "Exception IsDefaultIndexingAccount:", e);
            }
            if (bError)
                bRet = IsSearchingRequest(HttpContext.Current.Request);
            return bRet;*/
        }

        public static string ConstructSPObjectUrl(Object obj)
        {
            if (obj is SPWeb)
            {
                SPWeb web = obj as SPWeb;
                return web.Url;
            }
            else if (obj is SPList)
            {
                SPList list = obj as SPList;
                return ReConstructListUrl(list);
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;

                return (view.ParentList.ParentWeb.Url + "/" + view.Url);
            }
            else if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;

                string itemUrl;
                SPList list = item.ParentList;

                if (list.BaseType == SPBaseType.DocumentLibrary)
                {
                    itemUrl = list.ParentWeb.Url + "/" + item.Url;
                }
                else
                {
                    string listUrl = ReConstructListUrl(list);
                    string itemName;
                    if (list.BaseType == SPBaseType.Survey)
                        itemName = item.DisplayName;
                    else
                        itemName = item.Name;

                    itemUrl = listUrl + "/" + itemName;
                }

                return itemUrl;
            }
            else
                return "";
        }

        public static string ReConstructListUrl(SPList list)
        {
            return Globals.ConstructListUrl(list.ParentWeb, list);
        }

        public static DateTime GetLastModifiedTime(object obj)
        {
            DateTime time = new DateTime(1, 1, 1);

            if (obj is SPList)
            {
                SPList list = obj as SPList;
                time = list.LastItemModifiedDate;
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                time = view.ParentList.LastItemModifiedDate;
            }
            else if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                time = (DateTime)item["Modified"];
            }

            return time;
        }

        public static IPAddress GetIP4Address(string address)
        {
            IPAddress ipv4Address = IPAddress.None;
            string IP4Address = String.Empty;

            if (!String.IsNullOrEmpty(address))
            {
                foreach (IPAddress IPA in Dns.GetHostAddresses(address))
                {
                    if (IPA.AddressFamily.ToString() == "InterNetwork")
                    {
                        ipv4Address = IPA;
                        break;
                    }
                }

                if (ipv4Address != IPAddress.None)
                {
                    return ipv4Address;
                }
            }

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    ipv4Address = IPA;
                    break;
                }
            }

            return ipv4Address;
        }

        public static uint IPAddressToIPNumber(string IPaddress)
        {
            uint num = 0;
            string[] arrDec;

            IPaddress = GetIP4Address(IPaddress).ToString();

            if (string.IsNullOrEmpty(IPaddress))
            {
                return 0;
            }
            else
            {
                arrDec = IPaddress.Split('.');
                for (int i = 0; i < arrDec.Length; i++)
                {
                    num = num * 256 + uint.Parse(arrDec[i]) % 256;
                }
            }

            return num;
        }

        public static string GetLocalIPv4Address()
        {
            IPHostEntry host;
            string localIp = "";

            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }

            return localIp;
        }

        public static string ConvertToXmlString(string normalStr)
        {
            if (String.IsNullOrEmpty(normalStr)) return normalStr;

            string xmlStr = normalStr.Replace("&", "&amp;");
            xmlStr = xmlStr.Replace("\"", "&quot;");
            xmlStr = xmlStr.Replace("'", "&apos;");
            xmlStr = xmlStr.Replace("<", "&lt;");
            xmlStr = xmlStr.Replace(">", "&gt;");

            return xmlStr;
        }

        public static string ParseFromXmlString(string xmlStr)
        {
            if (String.IsNullOrEmpty(xmlStr)) return xmlStr;

            string normalStr = xmlStr.Replace("&amp;", "&");
            normalStr = normalStr.Replace("&quot;", "\"");
            normalStr = normalStr.Replace("&apos;", "'");
            normalStr = normalStr.Replace("&lt;", "<");
            normalStr = normalStr.Replace("&gt;", ">");

            return normalStr;
        }

        static public bool IsValidIP(string ip)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(ip, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}"))
            {
                string[] ips = ip.Split('.');
                if (ips.Length == 4 || ips.Length == 6)
                {
                    if (System.Int32.Parse(ips[0]) < 256 && System.Int32.Parse(ips[1]) < 256 & System.Int32.Parse(ips[2]) < 256 & System.Int32.Parse(ips[3]) < 256)
                        return true;
                    else
                        return false;
                }
                else
                    return false;

            }
            else
                return false;
        }

        public static void GenerateBackUrl(HttpRequest Request, String policyName, String policyMessage, ref String backurl, ref String httpserver, ref String msg)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            if (_SPEEvalAttr.WebObj != null)
            {
                string site_url = _SPEEvalAttr.WebUrl;
                bool home_url = false;
                try
                {
                    using (SPSite site = Globals.GetValidSPSite(_SPEEvalAttr.ObjEvalUrl, HttpContext.Current))
                    {
                        // George: check the request UrlReferrer is not null before using it.
                        if ((site != null && site.Url == site_url) || (Request.UrlReferrer != null && Request.UrlReferrer.AbsoluteUri.Contains(site_url)))
                        {
                            home_url = true;
                        }
                    }
                }
                catch
                {

                }
                backurl = site_url;
                String url = _SPEEvalAttr.ObjEvalUrl;
                if (url != null && url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);
                if (site_url != null && site_url.EndsWith("/"))
                    site_url = site_url.Substring(0, site_url.Length - 1);
                if (site_url == url && !home_url)
                {
                    int index1 = backurl.LastIndexOf("/");
                    if (index1 > 0)
                    {
                        backurl = site_url.Remove(index1);
                    }
                }
                else
                {
                    backurl = site_url;
                }
            }
            else
            {
                backurl = _SPEEvalAttr.ObjEvalUrl;
                int index1 = backurl.IndexOf("_layouts");

                if (index1 > 0)
                {

                    backurl = backurl.Remove(index1);

                }
            }

            String serverurl = _SPEEvalAttr.ObjEvalUrl;
            if (string.IsNullOrEmpty(serverurl))
            {
                // if the "_SPEEvalAttr.ObjEvalUrl" is null,
                serverurl = backurl;
            }
            int index = serverurl.IndexOf("_layouts");

            if (index > 0)
            {

                serverurl = serverurl.Remove(index);

            }

            httpserver = serverurl;
            bool _https = false;
            index = httpserver.IndexOf("http://");
            if (index >= 0)
            {
                httpserver = httpserver.Remove(index, 7);
            }
            index = httpserver.IndexOf("https://");
            if (index >= 0)
            {
                httpserver = httpserver.Remove(index, 8);
                _https = true;
            }
            index = httpserver.IndexOf("/");
            if (index > 0)
            {
                httpserver = httpserver.Remove(index);
            }
            if (!_https)
                httpserver = "http://" + httpserver;
            else
                httpserver = "https://" + httpserver;

            msg = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
        }
        public static string GetUserSid(SPWeb web,string userName)
        {
            string sid = "";
            try
            {
                sid = Globals.getADUserSid(userName);
                if (String.IsNullOrEmpty(sid))
                {
                    sid = Globals.GetSidFromUserProfile(web, userName);
                    if (String.IsNullOrEmpty(sid))
                        sid = Utilities.ClaimUserConvertion(userName);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetUserSid:", null, ex);
            }
            return sid;
        }

        public static string GetUserSid(string userName)
        {
            string sid = "";
            try
            {
                sid = Globals.getADUserSid(userName);
                if (String.IsNullOrEmpty(sid))
                {
                    sid = Utilities.ClaimUserConvertion(userName);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetUserSid:", null, ex);
            }
            return sid;
        }

        public static string GetWebsiteUrl()
        {
            try
            {
                using (SPSite site = new SPSite(HttpContext.Current.Request.Url.AbsoluteUri))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                       return web.Url;
                    }
                }
            }
            catch
            {
                return  "";
            }

        }

        public static void SetDenyRequestHeader(HttpRequest Request, string policyName, string policyMessage)
        {
            Request.Headers["X_Result"] = "1";
            String backurl = "";
            String httpserver = "";
            String msg = "";
            GenerateBackUrl(HttpContext.Current.Request, policyName, policyMessage, ref backurl, ref httpserver, ref msg);
            if (Request.Headers["X_Message"] == null)
            {
                Request.Headers["X_Message"] = msg;
            }
            if (Request.Headers["X_BackUrl"] == null)
            {
                Request.Headers["X_BackUrl"] = backurl;
            }
            if (Request.Headers["X_HttpServer"] == null)
            {
                Request.Headers["X_HttpServer"] = httpserver;
            }
        }

        public static string GetDenyString(string policyName, string policyMessage)
        {
            string errorMsg = Globals.EnforcementMessage;
            if ((policyName == null) && (policyMessage == null))
            {
                //Fix bug 9369. When there is not the policy name, change the message to "This operation has been enforced by Compliant Enterprise."
                errorMsg = Globals.NoPolicyEnforceMessage;
            }
            else
            {
                if (policyName != null)
                {
                    errorMsg = errorMsg + " " + policyName;
                }
                if (policyMessage != null)
                {
                    errorMsg = errorMsg + ": " + policyMessage;
                }
            }
            return errorMsg;
        }

        public static string GetFedAuthFromRequest(HttpRequest Request)
        {
            string strToken = null;
            HttpCookieCollection colCookie = Request.Cookies;

            for (int i = 0; i < colCookie.Count; i++)
            {
                if (colCookie.Get(i).Name.Equals("FedAuth", StringComparison.OrdinalIgnoreCase))
                {
                    strToken = colCookie.Get(i).Value;
                    break;
                }
            }
            return strToken;
        }

        public static bool IsADFSEnv(SPSite site)
        {
            SPWebApplication wa = site.WebApplication;
            SPUrlZone uz = site.Zone;
            SPIisSettings iss = wa.GetIisSettingsWithFallback(uz);
            if (iss != null && !iss.UseTrustedClaimsAuthenticationProvider)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
    }

    public struct AddressItem
    {
        public AddressItem(string UserName, string WebName, string RemoteAddr, IPrincipal PrincipalUser, List<string> strHeaders)
        {
            remoteAddr = RemoteAddr;
            userName = UserName;
            webName = WebName;
            lastTime = DateTime.Now;
			principalUser = PrincipalUser;
            headers = strHeaders;
        }
        public string remoteAddr;
        public string userName;
        public string webName;
        public DateTime lastTime;
        public IPrincipal principalUser;
        public List<string> headers;
    }

    public class TaskInfo
    {
        public IntPtr m_handle;
        public string m_logIdentifier;
        public string m_assistantName;
        public string[] m_attr_value;

        public TaskInfo(IntPtr handle, string logIdentifier, string assistantName, string[] attr_value)
        {
            m_handle = handle;
            m_logIdentifier = logIdentifier;
            m_assistantName = assistantName;
            m_attr_value = attr_value;
        }
    }


    public class ReportObligationLog
    {
        //private static int i;
        static ReportObligationLog()
        {

        }
        ~ReportObligationLog()
        {

        }

        private static void ThreadProc(object state)
        {
            TaskInfo ti = state as TaskInfo;
            CETYPE.CEResult_t call_result;
            call_result = CESDKAPI.CELOGGING_LogObligationData(ti.m_handle, ti.m_logIdentifier, ti.m_assistantName, ref ti.m_attr_value);
        }


        public static void DoReportLog(IntPtr handler, string[] obligation, string sitevalue)
        {
            if (obligation == null || obligation.Length == 0)
                return;
            int count = 0;
            string logIdentifier = null;
            string assistantName = "SharePoint Enforcer";
            int ob_start = 0;
            List<string> attr = null;
            try
            {
                for (count = 0; count < obligation.Length; count += 2)
                {
                    if (obligation[count] != null && obligation[count].IndexOf("CE_ATTR_OBLIGATION_NAME") != -1)
                    {
                        if (obligation[count + 1] != null && (obligation[count + 1].Equals("SPLOGACTIVITY") || obligation[count + 1].Equals("SPLOGCONTROL")))
                        {
                            //Fix bug 8399, added by William 20090203
                            assistantName = obligation[count + 1];
                            ob_start = count + 4;
                            attr = new List<string>();
                            for (; ob_start < obligation.Length; ob_start += 2)
                            {
                                {
                                    if (obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_VALUE") != -1)
                                    {
                                        if (obligation[ob_start + 1] != null && obligation[ob_start + 1].Equals("LogId"))
                                        {
                                            ob_start += 2;
                                            if (ob_start < obligation.Length && obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_VALUE") != -1)
                                            {
                                                logIdentifier = obligation[ob_start + 1];
                                            }
                                        }
                                        //for bug 8879, add null deetction, Addeb by William 20090306
                                        else if(string.IsNullOrEmpty(obligation[ob_start + 1]))
                                        {
                                            attr.Add("N/A");
                                        }
                                        else if (obligation[ob_start + 1] != null)
                                        {
                                            attr.Add(obligation[ob_start + 1]);
                                        }
                                    }
                                    else if (obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_NUMVALUES") != -1)
                                    {
                                        if (sitevalue != null)
                                        {
                                            attr.Add("Site");
                                            //Fix bug 8354, added by William 20090203
                                            attr.Add(Globals.UrlToResSig(sitevalue).ToLower());
                                        }
                                        string[] attr_value = attr.ToArray();
                                        TaskInfo ti = new TaskInfo(handler, logIdentifier, assistantName, attr_value);
                                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), ti);
                                        ob_start += 2;
                                        count = ob_start - 2;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DoReportLog:", null, ex);
            }
        }
    }


    public class WebRemoteAddressMap
    {
        private static IList<AddressItem> webAddrList;
        private static int CurrentMaxItem;
        private const int MAXITEM = 5000;
        private static object syncRoot = new Object();
        static WebRemoteAddressMap()
        {
            webAddrList = new List<AddressItem>();
            CurrentMaxItem = MAXITEM;
        }
        ~WebRemoteAddressMap()
        {
            webAddrList.Clear();
        }

        // TRUE: we can record it! Allow the request continue..
        // FALSE: we can not recoret it! Deny the request.
        public static bool TrytoAddNewRemoteAddress(string UserName, string WebName, string RemoteAddr, IPrincipal PrincipalUser, HttpRequest Req)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(WebName) || string.IsNullOrEmpty(RemoteAddr))
                return false;
            List<string> strHeaders = new List<string>();
            string strIpFromHeader = Globals.ParseXHeaderProperties(Req, strHeaders);
            if (!string.IsNullOrEmpty(strIpFromHeader))
                RemoteAddr = strIpFromHeader;
            UserName = CheckUserName(UserName);
            const int MinimumExpiredSeconds = 10;
            bool Return = true;
            bool Update = false;
            for (int Index = 0; Index < webAddrList.Count; ++Index)
            {
                AddressItem Item = webAddrList[Index];
                bool SameUser = UserName.Equals(Item.userName);
                bool SameWeb = WebName.Equals(Item.webName);
                bool SameAddr = RemoteAddr.Equals(Item.remoteAddr);
                if (SameUser && SameWeb && SameAddr)
                {
                    Update = true;
                }
                else if (SameUser && SameWeb && !SameAddr)
                {
                    // Check the timestamp!
                    DateTime PreviousTime = webAddrList[Index].lastTime;
                    TimeSpan Interval = DateTime.Now - PreviousTime;
                    if (Interval.TotalSeconds > MinimumExpiredSeconds)
                    {
                        Update = true;
                        //Fix bug 8676, update the IPAddress if same user log to different remote.Added by William 20090211
                        lock (syncRoot)
                        {
                            webAddrList[Index] = new AddressItem(UserName, WebName, RemoteAddr, PrincipalUser, strHeaders);
                        }
                    }
                    else
                    {
                        // Refuse the request!!!
                        Return = false;
                    }

                    break;
                }
                else if (SameUser && !SameWeb && SameAddr)
                {
                    if ((WebName.StartsWith("http:", StringComparison.OrdinalIgnoreCase) && Item.webName.StartsWith("https:")) ||
                        (WebName.StartsWith("https:", StringComparison.OrdinalIgnoreCase) && Item.webName.StartsWith("http:"))
                        )
                        Update = false;
                    else
                        Update = true;
                }
                else
                {
                    // do nothing
                }
                if (Update)
                {
                    // Update the timestamp
                    lock (syncRoot)
                    {
                        webAddrList[Index] = new AddressItem(UserName, WebName, RemoteAddr, PrincipalUser,strHeaders);
                    }
                    break;
                }
            }
            if (Return && !Update)
            {
                lock (syncRoot)
                {
                    webAddrList.Add(new AddressItem(UserName, WebName, RemoteAddr, PrincipalUser, strHeaders));
                }
            }

            if (webAddrList.Count > CurrentMaxItem)
            {
                // Keep the map smaller. Such as:
                // Remove the Item whose AddressTimeStamp's DataTime is expired!
                List<AddressItem> NeedToRemove = new List<AddressItem>();
                foreach (AddressItem s in webAddrList)
                {
                    // Check the timestamp!
                    DateTime PreviousTime = s.lastTime;
                    TimeSpan Interval = DateTime.Now - PreviousTime;
                    if (Interval.TotalSeconds > MinimumExpiredSeconds * 60)
                    {
                        // Those Items that havenot updated in 600s( 10 minutes) will be removed.
                        NeedToRemove.Add(s);
                    }
                }
                foreach (AddressItem s in NeedToRemove)
                {
                    lock (syncRoot)
                    {
                        webAddrList.Remove(s);
                    }
                }

                // Adjust the MAXITEM number to avoid sacnning the expire too constantly.
                if (webAddrList.Count > CurrentMaxItem) CurrentMaxItem = webAddrList.Count + 100;
                else if (webAddrList.Count < (MAXITEM - 100)) CurrentMaxItem = MAXITEM;
            }
            return Return;
        }

        public static string GetRemoteAddress(string UserName, string WebName, ref IPrincipal PrincipalUser)
        {
            string remoteAddress = "";
            AddressItem Item = new AddressItem();
            if (GetAddressItem(UserName, WebName, ref Item))
            {
                PrincipalUser = Item.principalUser;
                remoteAddress = Item.remoteAddr;
            }
            return remoteAddress;
        }

        public static bool GetAddressItem(string UserName, string WebName, ref AddressItem selectItem)
        {
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(WebName))
            {
                for (int Index = 0; Index < webAddrList.Count; ++Index)
                {
                    AddressItem Item = webAddrList[Index];
                    UserName = CheckUserName(UserName);
                    bool SameUser = UserName.Equals(Item.userName, StringComparison.OrdinalIgnoreCase);
                    bool SameWeb = false;
                    int index1 = WebName.ToLower().IndexOf(Item.webName.ToLower());
                    int index2 = Item.webName.ToLower().IndexOf(WebName.ToLower());
                    if (index1 != -1 || index2 != -1)
                        SameWeb = true;
                    if (SameUser && SameWeb)
                    {
                        selectItem = Item;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool GetXHeaderAttributes(string UserName, string WebName, ref List<string> strListXHeader)
        {
            AddressItem Item = new AddressItem();
            if (GetAddressItem(UserName, WebName, ref Item))
            {
                strListXHeader = Item.headers;
                return true;
            }
            return false;
        }

        // "i:05.t|saml provider|abraham.lincoln@adfs.com" OR "saml provider:abraham.lincoln@adfs.com"
        private static string CheckUserName(string userName)
        {
            string endUserName = userName;
            int index = userName.LastIndexOf(":");
            if (index != -1)
            {
                endUserName = userName.Substring(index + 1);
            }
            index = userName.LastIndexOf("|");
            if (index != -1)
            {
                endUserName = userName.Substring(index + 1);
            }

            return endUserName;
        }

    }

    public class PLEManager
    {
        //private SPSite SiteCollection;

        //private SPWeb RootWeb;

        HttpApplication _HttpApplication;
        HttpRequest _HttpRequest;
        private HttpContext Context;

        /// <summary>
        ///
        /// </summary>
        /// <param name="site">get instance through SPControl,if not,it cause native memory leak</param>
        public PLEManager(HttpApplication httpApplication)
        {
            _HttpApplication = httpApplication;
            _HttpRequest = _HttpApplication.Context.Request;
            Context = _HttpApplication.Context;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <param name="containsKey"></param>
        /// <returns></returns>
        public string getPropertyValue(string propertyKey)
        {
            SPSite SiteCollection;
            SPPropertyBag props = null;
            String returnValue = null;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                try
                {
                    SiteCollection = SPControl.GetContextSite(Context);
                    SPWeb RootWeb = SiteCollection.RootWeb;
                    props = RootWeb.Properties;
                    returnValue = props[propertyKey];
                }
                catch
                {
                    string object_url = Globals.UrlDecode(_HttpRequest.Url.GetLeftPart(UriPartial.Path));
                    using (SiteCollection = new SPSite(object_url))
                    {
                        SPWeb RootWeb = SiteCollection.RootWeb;
                        props = RootWeb.Properties;
                        returnValue = props[propertyKey];
                    }
                }
            });
            return returnValue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool setPropertyValue(string propertyKey, string propertyValue)
        {
            SPSite SiteCollection;
            SPWeb RootWeb;
            bool returnValue = false;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                try
                {
                    SiteCollection = SPControl.GetContextSite(Context);
                    RootWeb = SiteCollection.RootWeb;
                    RootWeb.Properties[propertyKey] = propertyValue;
                    RootWeb.Properties.Update();
                    returnValue = true;
                }
                catch
                {
                    string object_url = Globals.UrlDecode(_HttpRequest.Url.GetLeftPart(UriPartial.Path));
                    using (SiteCollection = new SPSite(object_url))
                    {
                        using (RootWeb = SiteCollection.RootWeb)
                        {
                            RootWeb.Properties[propertyKey] = propertyValue;
                            RootWeb.Properties.Update();
                            returnValue = true;
                        }
                    }
                }
            });
            return returnValue;
        }

        public bool IsPLEEnabled()
        {
            #region add by roy
            try
            {
                string propertyValue = this.getPropertyValue("spepleswitch");
                if (propertyValue != null)
                {
                    if (propertyValue.Trim().ToLower().Equals("disable"))
                    {
                        return false;
                    }
                }
                else//turn on the ple filter function by default
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during IsPLEEnabled:", null, ex);
            }
            return true;
            #endregion

        }
    }

    public static class UserSid
    {
        private static PropertiesCacheManage<string> userSidCache = PropertiesCacheManage<string>.getSingleInstance("UserSid");
        public static string GetUserSid(HttpContext context)
        {
            string sid = "";
            if (context != null)
            {
                string userName = context.User.Identity.Name;
                if (!String.IsNullOrEmpty(userName))
                {
                    object cache = new object();
                    if (userSidCache.getCacheItem(userName, out cache))
                    {
                        return cache.ToString();
                    }

                    sid = Utilities.GetUserSid(userName); //this will get SID via querying AD
                    string postfixUserName=userName,postfixSid=sid;
                    int iPos=userName.LastIndexOf("|");
                    if(iPos>0)
                        postfixUserName=userName.Substring(iPos+1);
                    iPos=sid.LastIndexOf(":");
                    if(iPos>0)
                        postfixSid=sid.Substring(iPos+1);

                    string requestUrl = context.Request.Url.AbsoluteUri;
                    if ((!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(sid) && !string.IsNullOrEmpty(requestUrl) &&userName.EndsWith(sid, StringComparison.OrdinalIgnoreCase))||
                        (!string.IsNullOrEmpty(postfixUserName)&&!string.IsNullOrEmpty(postfixSid)&&!string.IsNullOrEmpty(requestUrl)&&postfixUserName.EndsWith(postfixSid,StringComparison.OrdinalIgnoreCase)))
                    {
                        SPWeb web = null;
                        SPSite site = null;
                        try
                        {
                            site = new SPSite(requestUrl);
                            if (site != null)
                                web = site.OpenWeb();
                            if (web != null)
                                sid = Utilities.GetUserSid(web, userName); //this will get SID via query user profile
                        }
                        finally
                        {
                            if (web != null)
                                web.Dispose();
                            if (site != null)
                                site.Dispose();
                        }
                    }
                    if (!string.IsNullOrEmpty(sid))
                    {
                        userSidCache.setCacheItem(userName, sid);
                    }
                }
            }
            return sid;
        }

        public static string GetUserSid(SPWeb web)
        {
            string sid = "";
            if (web != null)
            {
                sid = web.CurrentUser.Sid;
                string userName = web.CurrentUser.LoginName;
                if (!String.IsNullOrEmpty(sid))
                {
                    userSidCache.setCacheItem(userName, sid);
                }
                else
                {
                    sid = GetUserSid(userName);
                }
            }
            return sid;
        }
        public static string GetUserSid(string webUrl, string userName)
        {
            SPWeb web = null;
            SPSite site = null;
            try
            {
                site= new SPSite(webUrl);
                if (site != null)
                    web = site.OpenWeb();
                if (web != null)
                    return GetUserSid(web, userName);
            }
            finally
            {
                if (web != null)
                    web.Dispose();
                if (site != null)
                    site.Dispose();
            }
            return string.Empty;
        }
        public static string GetUserSid(SPWeb web, string userName)
        {
            string sid = string.Empty;
            if (!string.IsNullOrEmpty(userName))
            {
                object cache = new object();
                if (userSidCache.getCacheItem(userName, out cache))
                {
                    return cache.ToString();
                }
                sid = Utilities.GetUserSid(web, userName);
                if (!string.IsNullOrEmpty(sid))
                {
                    userSidCache.setCacheItem(userName, sid);
                }
            }
            return sid;
        }

        public static string GetUserSid(string userName)
        {
            string sid = string.Empty;
            if (!string.IsNullOrEmpty(userName))
            {
                object cache = new object();
                if (userSidCache.getCacheItem(userName, out cache))
                {
                    return cache.ToString();
                }
                sid = Utilities.GetUserSid(userName);
                if (!string.IsNullOrEmpty(sid))
                {
                    userSidCache.setCacheItem(userName, sid);
                }
            }
            return sid;
        }
        //http://msdn.microsoft.com/en-us/library/cc246018.aspx
        public static Regex SidFormation = new Regex(@"^S-\d-\d+-(\d+-){1,14}\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        ///
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="userName"></param>
        /// <returns>if the input sid is valid
        /// TRUE - BEST - the sid is standard sid
        /// NULL - BETTER - the sid may be claim sid, better than no sid
        /// FALSE - INVLAID - the sid is invalid
        /// </returns>
        public static bool? IsValidSid(string sid, string userName)
        {
            if (!string.IsNullOrEmpty(sid) && !sid.Contains(@"\"))
            {
                if (SidFormation.IsMatch(sid))
                {
                    //this sid is in formation "S-1-XXXX-XX-XX-XXX"
                    return true;
                }
                int colonIndex = sid.IndexOf(":");
                if (colonIndex > 0)
                {
                    //this may from fba or adfs
                    string name = sid.Substring(colonIndex + 1);
                    if (userName.EndsWith(name))
                    {
                        return null;
                    }
                }
                int pipeIndex = sid.LastIndexOf("|");
                if (pipeIndex > 0)
                {
                    string name = sid.Substring(pipeIndex);
                    if (userName.EndsWith(name))
                    {
                        return null;
                    }
                }

                if (colonIndex < 0 && pipeIndex < 0 //if the sid doesn't contain the domain prefix
                    && sid.IndexOf("@") > 0 //and the sid is an email
                    && userName.Length > sid.Length && userName.EndsWith(sid))  //and it's part of the user name
                {
                    return null;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Add By Roy
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class UrlFilterManage<T,K>
    {
       private static object _objSyncIndexLock = new object();
       private static IDictionary<T, K> urlsTable = new System.Collections.Generic.Dictionary<T, K>();


       public static bool ContainsKey(T key)
       {
           lock (_objSyncIndexLock)
           {
               return urlsTable.ContainsKey(key);
           }
       }

       public static void Add(T key,K value)
       {
           lock (_objSyncIndexLock)
           {
               urlsTable.Add(key, value);
           }
       }

       public static bool Remove(T key)
       {
           bool value;
           lock (_objSyncIndexLock)
           {
             value =  urlsTable.Remove(key);
           }
           return value;
       }

       public static K getValueByKey(T key)
       {
           lock (_objSyncIndexLock)
           {
               if (urlsTable.ContainsKey(key))
               {
                   return urlsTable[key];
               }
               else
               {
                   return default(K);
               }
           }

       }
    }


    /// <summary>
    /// Add By Roy
    /// </summary>
    public class PropertiesCacheManage<T>
    {
        static IDictionary<string, PropertiesCacheManage<T>> cacheModuleTable = new System.Collections.Generic.Dictionary<string, PropertiesCacheManage<T>>();
        IDictionary<T, object> propertiesTable = new System.Collections.Generic.Dictionary<T, object>();
        DateTime _expiredTime;
        int _interval;

        static object objSyncIndexLock = new object();

        private PropertiesCacheManage(int mins)
        {
            _interval = mins;
            _expiredTime = DateTime.Now.AddMinutes(_interval);
        }

        public static PropertiesCacheManage<T> getSingleInstance(string key)
        {
           return getSingleInstance(key, int.MaxValue);
        }

        public static PropertiesCacheManage<T> getSingleInstance(string key, int mins)
        {
            PropertiesCacheManage<T> intance;
            if (key == null || mins < 0)
                return null;

              bool isExist = cacheModuleTable.TryGetValue(key, out intance);
              if (isExist)
              {
                  return intance;
              }
              else
              {
                  lock (objSyncIndexLock)
                  {
                      if (!cacheModuleTable.TryGetValue(key, out intance))
                      {
                          intance = new PropertiesCacheManage<T>(mins);
                          cacheModuleTable.Add(key, intance);
                      }
                  }
              }

            return intance;
        }

        public bool getCacheItem(T guid, out object propertyValue)
        {
            propertyValue = null;
            bool getCacheItem = false;

            if (_expiredTime < DateTime.Now)
            {
                ClearCache();
                return false;
            }

            if (guid != null)
            {
               getCacheItem = propertiesTable.TryGetValue(guid, out propertyValue);
            }

            return getCacheItem;
        }

        public bool setCacheItem(T guid, object propertyValue)
        {
            try
            {
                lock (this)
                {
                    if (propertiesTable.ContainsKey(guid))
                    {
                        propertiesTable[guid] = propertyValue;
                    }
                    else
                    {
                        propertiesTable.Add(guid, propertyValue);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during setCacheItem:", null, ex);
            }

            return false;
        }

        private void ClearCache()
        {
            lock (this)
            {
                propertiesTable.Clear();
                _expiredTime = DateTime.Now.AddMinutes(_interval);
            }
        }
    }


    public class CustomDenyPageSwitch
    {
        private const string CUSTOMDENYPAGE_ENABLED_REGISTRY_KEY = "CustomDenyPageEnabled";
        private const int PREFILTER_ENABLED_RESET_MINUTES = 10;
        private static bool? _enabled;
        private static DateTime _resetTime;
        public static bool IsEnabled()
        {
            if (!_enabled.HasValue || DateTime.Now >= _resetTime)
            {
                try
                {
                    using (var ceKey = Registry.LocalMachine.OpenSubKey(@"Software\NextLabs\Compliant Enterprise\Sharepoint Enforcer\", false))
                    {
                        if (ceKey != null)
                        {
                            object regValue = ceKey.GetValue(CUSTOMDENYPAGE_ENABLED_REGISTRY_KEY, string.Empty);
                            bool enabled = false;
                            if (regValue != null && bool.TryParse(regValue.ToString(), out enabled))
                            {
                                _enabled = enabled;
                            }
                        }
                    }
                    _resetTime = DateTime.Now.AddMinutes(PREFILTER_ENABLED_RESET_MINUTES);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during CustomDenyPageSwitch:", null, ex);
                }
            }
            return _enabled.HasValue ? _enabled.Value : false;

        }
    }
}
