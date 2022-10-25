using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.SharePoint;
using System.Threading;
using Microsoft.SharePoint.Administration;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class PreAuthorization
    {
        private const string IniFileName = "PreAuthPluginConfig.ini";
        // Load setting
        private const string LoadSetting = "LoadSetting";
        private const string SitePreAuthZKey = "SitePreAuthZ";
        private const string ListPreAuthZKey = "LibPreAuthZ";
        private const string ItemPreAuthZKey = "ItemPreAuthZ";

        private const string NoneValue = "NONE";
        private const string AllValue = "ALL";
        private const string SelectedValue = "SELECTED";

        // SitePreAuthZSelected
        private const string SitePreAuthZSelectedKey = "SitePreAuthZSelected";
        private const string UrlsKey = "Urls";
        // ListPreAuthZSelected
        private const string ListPreAuthZSelectedKey = "LibPreAuthZSelected";
        // ItemPreAuthZSelected
        private const string ItemPreAuthZSelectedKey = "ItemPreAuthZSelected";
        // Load Sequence
        private const string LoadSequenceKey = "LoadSequence";
        private const string SitePlugInsKey = "SitePlugIns";
        private const string ListPlugInsKey = "LibPlugIns";
        private const string ItemPlugInsKey = "ItemPlugIns";
        // Timeout
        private const string TimeoutKey = "Timeout";
        private const string GlobalTimeOutKey = "GlobalTimeOut";
        // Attribute conflict
        private const string AttributeConflictKey = "AttributeConflict";
        private const string UseSPAttributeKey = "UseSPAttribute";
        private const string UseLastOneKey = "UseLastOne";

        private const string YesValue = "YES";
        private const string NoValue = "NO";
        private const string OnValue = "ON";
        private const string OffValue = "OFF";
        private const string MergeValue = "MERGE";


        // Supported Action
        private const string SupportedActionKey = "SupportedAction";
        private const string SiteActionsKey = "SiteActions";
        private const string ListActionsKey = "LibActions";
        private const string ItemActionsKey = "ItemActions";
        // Update Config Info time
        private const string UpdateConfigInfotimeKey = "UpdateConfigInfotime";
        private const string IntervalTimeKey = "IntervalTime";

        // Check if need merge attrs when do trmming
        private const string TrimmingSeletcedKey = "TrimmingSeletced";
        private const string TrimmingPreAuthZKey = "TrimmingPreAuthZ";

        // Split
        private const char semSplit = ';';
        public static string tagSplit = "||";
        public static string customerSplit = "|";
        public static string[] urlSplitArr = { "**" };
        public static string urlSplitStr = "**";

        //Cache Name
        private const string CacheName = "NextlabsConfigCacheKey";
        private const string CacheValue = "NextlabsConfigCacheValue";

        // Limit length
        private const int ReadLenth = 2048;

        // Global
        private static PreAuthorization g_PreAuthorization = null;
        private static object g_objLock = new object();

        // Member
        private string m_strSitePlugins;
        private string m_strListPlugins;
        private string m_strItemPlugins;

        private string m_strSelectedSiteUrls;
        private string m_strSelectedListUrls;
        private string m_strSelectedItemUrls;

        private bool m_bSiteAll;    // match all the SPWeb.
        private bool m_bListAll;    // match all the SPList.
        private bool m_bItemAll;    // match all the SPListItem.

        private List<PreAuthAttributes.IGetPreAuthAttributes> m_listVaildSitePlugin;
        private List<PreAuthAttributes.IGetPreAuthAttributes> m_listVaildListPlugin;
        private List<PreAuthAttributes.IGetPreAuthAttributes> m_listVaildItemPlugin;
        private ReaderWriterLock m_rwlock;
        private string m_strConfigFilePath;

        private int m_updateTime;
        private int m_nTimeOut;

        private string m_strAttrConflict;
        private bool m_bAttrLastOne;

        private string m_strSiteActions;
        private string m_strListActions;
        private string m_strItemActions;

        private bool m_bTrimmingPreAuthZ;

        public static PreAuthorization GetInstance()
        {
            lock (g_objLock)
            {
                if (g_PreAuthorization == null)
                {
                    g_PreAuthorization = new PreAuthorization();
                }
                return g_PreAuthorization;
            }
        }

        /// <summary>
        /// conclassed function
        /// </summary>
        /// <param name="_strConfigPath">config file path</param>
        private PreAuthorization()
        {
            try
            {
                m_strConfigFilePath = Globals.GetSPEPath() + @"config\" + IniFileName;
                m_strSitePlugins = "";
                m_strListPlugins = "";
                m_strItemPlugins = "";

                m_strSelectedSiteUrls = "";
                m_strSelectedListUrls = "";
                m_strSelectedItemUrls = "";

                m_bSiteAll = false;
                m_bListAll = false;
                m_bItemAll = false;

                m_listVaildSitePlugin = new List<PreAuthAttributes.IGetPreAuthAttributes>();
                m_listVaildListPlugin = new List<PreAuthAttributes.IGetPreAuthAttributes>();
                m_listVaildItemPlugin = new List<PreAuthAttributes.IGetPreAuthAttributes>();

                m_updateTime = 3600; // Update time (second)
                m_nTimeOut = 1000; // Time out (second)

                m_strAttrConflict = "";
                m_bAttrLastOne = false;

                m_strSiteActions = "";
                m_strListActions = "";
                m_strItemActions = "";

                m_bTrimmingPreAuthZ = false;

                m_rwlock = new ReaderWriterLock();
                LoadConfig();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during PreAuthorization ctor:", null, ex);
            }
        }

        private void LoadPreAuthZ(string strPreAuthZKey, string strPluginKey, string strPreAuthZSelectedKey, ref bool bPreAuthAll,
            ref string strPlugins, ref string strSelectedUrl, List<PreAuthAttributes.IGetPreAuthAttributes> listPlugin)
        {
            string strPreAuthZ = IniReadValue(m_strConfigFilePath, LoadSetting, strPreAuthZKey);
            if (!string.IsNullOrEmpty(strPreAuthZ))
            {
                if (strPreAuthZ.Equals(AllValue, StringComparison.OrdinalIgnoreCase))
                {
                    bPreAuthAll = true;
                }

                if (strPreAuthZ.Equals(SelectedValue, StringComparison.OrdinalIgnoreCase) || strPreAuthZ.Equals(AllValue, StringComparison.OrdinalIgnoreCase))
                {
                    string strPluginValue = IniReadValue(m_strConfigFilePath, LoadSequenceKey, strPluginKey);
                    if (!string.IsNullOrEmpty(strPluginValue))
                    {
                        // Update Site Plugin.
                        strPlugins = strPluginValue.Trim();
                        string[] arryPlugins = strPlugins.Split(semSplit);
                        List<string> assePlugin = arryPlugins.ToList<string>();
                        LoadAssemble(assePlugin, listPlugin);
                    }
                }

                if (strPreAuthZ.Equals(SelectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    strSelectedUrl = IniReadValue(m_strConfigFilePath, strPreAuthZSelectedKey, UrlsKey);
                }
            }
        }

        public bool LoadConfig()
        {
            NLLogger.OutputLog(LogLevel.Info, "PreAuthorization LoadConfig:" + m_strConfigFilePath, null);
            bool bresult=false;
            try
            {
                if (File.Exists(m_strConfigFilePath) && m_strConfigFilePath.EndsWith(".ini"))
                {
                    //load setting
                    string strSitePreAuthZ = IniReadValue(m_strConfigFilePath, LoadSetting, SitePreAuthZKey);
                    string strListPreAuthZ = IniReadValue(m_strConfigFilePath, LoadSetting, ListPreAuthZKey);
                    string strItemPreAuthZ = IniReadValue(m_strConfigFilePath, LoadSetting, ItemPreAuthZKey);

                    //SitePreAuthZ
                    LoadPreAuthZ(SitePreAuthZKey, SitePlugInsKey, SitePreAuthZSelectedKey, ref m_bSiteAll, ref m_strSitePlugins, ref m_strSelectedSiteUrls, m_listVaildSitePlugin);

                    //ListPreAuthZ
                    LoadPreAuthZ(ListPreAuthZKey, ListPlugInsKey, ListPreAuthZSelectedKey, ref m_bListAll, ref m_strListPlugins, ref m_strSelectedListUrls, m_listVaildListPlugin);

                    //ItemPreAuthZ
                    LoadPreAuthZ(ItemPreAuthZKey, ItemPlugInsKey, ItemPreAuthZSelectedKey, ref m_bItemAll, ref m_strItemPlugins, ref m_strSelectedItemUrls, m_listVaildItemPlugin);

                    //Attribute conflict
                    m_strAttrConflict = IniReadValue(m_strConfigFilePath, AttributeConflictKey, UseSPAttributeKey);
                    string strUseLastOne = IniReadValue(m_strConfigFilePath, AttributeConflictKey, UseLastOneKey);
                    m_bAttrLastOne = strUseLastOne == YesValue ? true : false;

                    //Supported Action
                    m_strSiteActions = IniReadValue(m_strConfigFilePath, SupportedActionKey, SiteActionsKey);
                    m_strListActions = IniReadValue(m_strConfigFilePath, SupportedActionKey, ListActionsKey);
                    m_strItemActions = IniReadValue(m_strConfigFilePath, SupportedActionKey, ItemActionsKey);


                    //Update Config Info time
                    string strUpdateTime = IniReadValue(m_strConfigFilePath, UpdateConfigInfotimeKey, IntervalTimeKey);
                    if (!string.IsNullOrEmpty(strUpdateTime))
                    {
                        int iUpdateTime = 3600;
                        if (Int32.TryParse(strUpdateTime, out iUpdateTime))
                        {
                            m_updateTime = iUpdateTime;
                        }
                    }

                    //Timeout
                    string strTimeout = IniReadValue(m_strConfigFilePath, TimeoutKey, GlobalTimeOutKey);
                    if (!string.IsNullOrEmpty(strTimeout))
                    {
                        int iTimeOut = 1000;
                        if (Int32.TryParse(strTimeout, out iTimeOut))
                        {
                            m_nTimeOut = iTimeOut;
                        }
                    }

                    //Check if need merge attrs when do trmming
                    string strTrimMerge = IniReadValue(m_strConfigFilePath, TrimmingSeletcedKey, TrimmingPreAuthZKey);
                    if (!string.IsNullOrEmpty(strTrimMerge))
                    {
                        m_bTrimmingPreAuthZ = strTrimMerge == OnValue ? true : false;
                    }
                    bresult = true;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Info, "Exception during LoadConfig:", null, ex);
            }
            return bresult;
        }

        public bool IfNeedTrimmingPreAthuZ()
        {
            return m_bTrimmingPreAuthZ;
        }

        private T GetInstance<T>(string _strInterfaceName, Assembly _ass)
        {
            T t = default(T);
            Type[] ts = _ass.GetTypes();
            for (int i = 0; i < ts.Length; i++)
            {
                if (ts[i].GetInterface(_strInterfaceName, true) != null)
                {
                    t = (T)Activator.CreateInstance(ts[i]);
                    break;
                }
            }
            return t;
        }

        private void LoadAssemble(List<string> listPath, List<PreAuthAttributes.IGetPreAuthAttributes> listPlugin)
        {
            if (listPath != null && listPlugin != null)
            {
                foreach (string strPlugIn in listPath)
                {
                    try
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "LoadAssemble:"+strPlugIn, null);

                        //safer load: Assembly.Load("ListDllA, Version=1.0.0.0, Culture=neutral, PublicKeyToken=9cddd5d1b2b106b0")
                        Assembly ass = Assembly.Load(strPlugIn);
                        if (ass != null)
                        {
                            PreAuthAttributes.IGetPreAuthAttributes tempType = GetInstance<PreAuthAttributes.IGetPreAuthAttributes>
                                (typeof(PreAuthAttributes.IGetPreAuthAttributes).FullName, ass);
                            if (tempType != null)
                            {
                                listPlugin.Add(tempType);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Error, $"Exception,strPlugIn:{strPlugIn} ,during LoadAssemble:", null, ex);
                    }
                }
            }
        }

        public bool CheckPluginExisted()
        {
            if (m_listVaildSitePlugin.Count == 0 && m_listVaildListPlugin.Count == 0 && m_listVaildItemPlugin.Count == 0)
            {
                return false;
            }
            return true;
        }

        public void GetAttributesAttributes(object _SPUser, object _SPObject, object _SPFile, string _strAction, string url, string strSPType, Dictionary<string, string> _userPair,
            Dictionary<string, string> _srcPair, Dictionary<string, string> _dstPair)
        {
            try
            {
                m_rwlock.AcquireReaderLock(Int32.MaxValue);
                bool bSelectedAll = false;
                List<string> listActions = null;
                List<string> listSelectedUrls = null;
                int nObject = 0;
                List<PreAuthAttributes.IGetPreAuthAttributes> lisMthods = null;
                if (strSPType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE)
                {
                    bSelectedAll = m_bSiteAll;
                    nObject = 1;
                    listActions = m_strSiteActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedSiteUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildSitePlugin;
                }
                else if (strSPType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET)
                {
                    bSelectedAll = m_bListAll;
                    nObject = 2;
                    listActions = m_strListActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedListUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildListPlugin;
                }
                else if (strSPType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM)
                {
                    bSelectedAll = m_bItemAll;
                    nObject = 3;
                    listActions = m_strItemActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedItemUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildItemPlugin;
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Info, "SPType is invalid", null);
                    return;
                }

                if (bSelectedAll)
                {
                    if (CheckAction(_strAction, listActions))
                    {
                        InvokeMethod(lisMthods, _SPUser, _SPObject, _SPFile, _strAction, _userPair, _srcPair, _dstPair, nObject);
                    }
                }
                else
                {
                    if (CheckSource(url, listSelectedUrls))
                    {

                        if (CheckAction(_strAction, listActions))
                        {
                            InvokeMethod(lisMthods, _SPUser, _SPObject, _SPFile, _strAction, _userPair, _srcPair, _dstPair, nObject);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during GetAttributesAttributes", null, ex);
            }
            finally
            {
                m_rwlock.ReleaseReaderLock();
            }
        }

        public void GetFileAttributesDuringUpload(SPItemEventProperties spItemEventProperties, string filePath, string type, string action, string url, ref List<KeyValuePair<string, string>> lsPrepareExtralAttributesRef)
        {
            NLLogger.OutputLog(LogLevel.Debug, "GetAttributes enter", null);
            try
            {
                bool bSelectedAll = false;
                List<string> listActions = null;
                List<string> listSelectedUrls = null;
                List<PreAuthAttributes.IGetPreAuthAttributes> lisMthods = null;
                if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE)
                {
                    bSelectedAll = m_bSiteAll;
                    listActions = m_strSiteActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedSiteUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildSitePlugin;
                }
                else if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET)
                {
                    bSelectedAll = m_bListAll;
                    listActions = m_strListActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedListUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildListPlugin;
                }
                else if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM)
                {
                    bSelectedAll = m_bItemAll;
                    listActions = m_strItemActions.Split(semSplit).ToList<string>();
                    listSelectedUrls = m_strSelectedItemUrls.Split(semSplit).ToList<string>();
                    lisMthods = m_listVaildItemPlugin;
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Info, "SPType is invalid");
                    return;
                }

                NLLogger.OutputLog(LogLevel.Debug, "bSelectedAll:" + bSelectedAll);
                if (bSelectedAll)
                {
                    if (CheckAction(action, listActions))
                    {
                        InvokeMethod(lisMthods, spItemEventProperties, filePath, ref lsPrepareExtralAttributesRef);
                    }
                }
                else
                {
                    foreach (var url1 in listSelectedUrls)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "url:" + url1);
                    }
                    NLLogger.OutputLog(LogLevel.Debug, "url:" + url);
                    if (CheckSource(url, listSelectedUrls))
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "CheckSource is true");
                        foreach (var act in listActions)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "action:" + act);
                        }
                        NLLogger.OutputLog(LogLevel.Debug, "_strAction:" + action);

                        if (CheckAction(action, listActions))
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "CheckAction is true");
                            InvokeMethod(lisMthods, spItemEventProperties, filePath, ref lsPrepareExtralAttributesRef);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "GetAttributesAttributes error:", null, ex);
            }
        }
        //call preauth dll to read josn from server file
        private void InvokeMethod(List<PreAuthAttributes.IGetPreAuthAttributes> lisMthods, SPItemEventProperties spItemEventProperties, string filePath, ref List<KeyValuePair<string, string>> lsPrepareExtralAttributesRef)
        {
            try
            {
                NLLogger.OutputLog(LogLevel.Info, "InvokeMethod Start:"+ lisMthods.Count);

                if (lisMthods != null)
                {
                    foreach (PreAuthAttributes.IGetPreAuthAttributes method in lisMthods)
                    {
                        bool bGetFileContent = method.PrepareFileAttributesDuringUpload(spItemEventProperties, filePath, ref lsPrepareExtralAttributesRef);

                        NLLogger.OutputLog(LogLevel.Debug, String.Format("Get header info from file:[{0}] with result:[{1}]\n", filePath, bGetFileContent));
                    }
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Warn, "lisMthods is null");
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during InvokeMethod:", null, ex);
            }
        }
        private void InvokeMethod(List<PreAuthAttributes.IGetPreAuthAttributes> lisMthods, object _SPUser, object _SPObject, object _SPFile,
            string _strAction, Dictionary<string, string> userPair, Dictionary<string, string> srcPair, Dictionary<string, string> dstPair, int nObjectType)
        {
            try
            {
                NLLogger.OutputLog(LogLevel.Info, "InvokeMethod Start");
                if (lisMthods != null)
                {
                    foreach (PreAuthAttributes.IGetPreAuthAttributes method in lisMthods)
                    {
                        Dictionary<string, string> lisTempUserPair = new Dictionary<string, string>();
                        Dictionary<string, string> lisTempSrcPair = new Dictionary<string, string>();
                        Dictionary<string, string> lisTempDstPair = new Dictionary<string, string>();
                        int iTempResult = 0;
                        iTempResult = method.GetCustomAttributes(_SPUser, _SPObject, _SPFile, _strAction, lisTempUserPair, lisTempSrcPair, lisTempDstPair, m_nTimeOut, nObjectType);
                        MergeAttribute(userPair, lisTempUserPair);
                        MergeAttribute(srcPair, lisTempSrcPair);
                        MergeAttribute(dstPair, lisTempDstPair);
                    }
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Warn, "lisMthods is null");
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during InvokeMethod", null, ex);
            }
        }

        private void MergeAttribute(Dictionary<string, string> dstPair, Dictionary<string, string> srcPair)
        {
            if (dstPair != null)
            {
                string key = null;
                foreach (string tempGetKey in srcPair.Keys)
                {
                    bool bExisted = false;
                    foreach (string userKey in dstPair.Keys)
                    {
                        if (userKey.Equals(tempGetKey, StringComparison.OrdinalIgnoreCase))
                        {
                            bExisted = true;
                            key = userKey;
                            break;
                        }
                    }

                    if (bExisted)
                    {
                        if (m_strAttrConflict.Equals(MergeValue, StringComparison.OrdinalIgnoreCase))
                        {
                            dstPair[key] = dstPair[key] + tagSplit + srcPair[tempGetKey].Replace(customerSplit, tagSplit);
                        }
                        else if (m_bAttrLastOne)
                        {
                            dstPair[key] = srcPair[tempGetKey].Replace(customerSplit, tagSplit);
                        }
                    }
                    else
                    {
                        dstPair.Add(tempGetKey, srcPair[tempGetKey].Replace(customerSplit, tagSplit));
                    }
                }
            }
        }

        private bool CheckSource(string url, List<string> _lisUrls)
        {
            bool bresult = false;

            if (!string.IsNullOrEmpty(url) && _lisUrls != null && _lisUrls.Count > 0)
            {
				//URL_SPLIT and check the string match
				foreach (string strConfigUrl in _lisUrls)
                {
                    if (!string.IsNullOrEmpty(strConfigUrl))
                    {
                        string subUrl = url;
                        string[] splits = strConfigUrl.Split(urlSplitArr, StringSplitOptions.RemoveEmptyEntries);
                        //Check the head and tail of string.
                        if ((!strConfigUrl.StartsWith(urlSplitStr) && !url.StartsWith(splits[0], StringComparison.OrdinalIgnoreCase))
                            || (!strConfigUrl.EndsWith(urlSplitStr) && !url.EndsWith(splits[splits.Length - 1], StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                        for (int i = 0; i < splits.Length; i++)
                        {
                            int ind = subUrl.IndexOf(splits[i], StringComparison.OrdinalIgnoreCase);
                            if (-1 != ind)
                            {
                                if (i == splits.Length - 1)
                                {
                                    return true;
                                }
                                subUrl = subUrl.Substring(ind + splits[i].Length);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return bresult;
        }

        private bool CheckAction(string _strAction, List<string> _lisActions)
        {
            bool bresult = false;
            if (_lisActions != null)
            {
                bresult = _lisActions.Exists(dir => { return dir.Equals(_strAction, StringComparison.OrdinalIgnoreCase); });
            }
            return bresult;
        }

        public string[] AssemblyAttributs(string[] arryAttributs, Dictionary<string, string> pairAttributs)
        {
            if (arryAttributs != null && arryAttributs.Length > 0 && pairAttributs.Keys.Count > 0)
            {
                try
                {
                    if (m_strAttrConflict.Equals(OffValue, StringComparison.OrdinalIgnoreCase))
                    {
                        List<string> attrList = new List<string>();
                        List<string> keys = new List<string>();
                        string key = null;
                        foreach (var keyValue in pairAttributs)
                        {
                            keys.Add(keyValue.Key.ToLower());
                            attrList.Add(keyValue.Key);
                            attrList.Add(keyValue.Value);
                        }

                        for (int i = 0; i < arryAttributs.Length; i = i + 2)
                        {
                            key = arryAttributs[i];
                            if (!keys.Contains(key.ToLower()))
                            {
                                attrList.Add(key);
                                attrList.Add(arryAttributs[i + 1]);
                            }
                        }

                        return attrList.ToArray();
                    }
                    else if (m_strAttrConflict.Equals(MergeValue, StringComparison.OrdinalIgnoreCase))
                    {
                        List<string> attrList = arryAttributs.ToList<string>();
                        foreach (var keyValue in pairAttributs)
                        {
                            attrList.Add(keyValue.Key);
                            attrList.Add(keyValue.Value);
                        }
                        return attrList.ToArray();
                    }
                    else
                    {
                        List<string> attrList = new List<string>();
                        List<string> keys = new List<string>();
                        string key = null;
                        for (int i = 0; i < arryAttributs.Length; i = i + 2)
                        {
                            key = arryAttributs[i];
                            if (!keys.Contains(key.ToLower()))
                            {
                                keys.Add(key.ToLower());
                            }
                            attrList.Add(key);
                            attrList.Add(arryAttributs[i + 1]);
                        }

                        foreach (var keyValue in pairAttributs)
                        {
                            if (!keys.Contains(keyValue.Key.ToLower()))
                            {
                                attrList.Add(keyValue.Key);
                                attrList.Add(keyValue.Value);
                            }
                        }

                        return attrList.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception in  AssemblyAttributs", null, ex);
                    return arryAttributs;
                }
            }
            else if ((arryAttributs == null || arryAttributs.Length == 0) && pairAttributs.Keys.Count > 0)
            {
                List<string> attrList = new List<string>();
                foreach (var keyValue in pairAttributs)
                {
                    attrList.Add(keyValue.Key);
                    attrList.Add(keyValue.Value);
                }
                return attrList.ToArray();
            }
            else
            {
                NLLogger.OutputLog(LogLevel.Info, "AssemblyAttributs arryAttributs is null or pairAttributs is empty", null);
                return arryAttributs;
            }
        }

        private string IniReadValue(string path, string section, string key)
        {
            string value = "";
            try
            {
                StringBuilder temp = new StringBuilder(ReadLenth);
                int i = GetPrivateProfileString(section, key, "", temp, ReadLenth, path);
                int errorid = Marshal.GetLastWin32Error();
                value =  temp.ToString().Trim();
            }
            catch
            {
            }
            return value;
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
    }
}

namespace NextLabs.PreAuthAttributes
{
    public interface IGetPreAuthAttributes
    {
        /*! GetCustomAttributes
        *
        * \brief: Get additional attribute from plugin for Pre-AuthZ.
        * \return value:             0 means success, 1 means timeout, others means unknow error happens.
        *
        * \param spUser (in)         it can be convert to SPUser, it can't be NULL.
        * \param spSource (in)       it is the object that current user access to, it can
        *                               be an Site/List/Item object, it was exactly figure
        *                               outed by nObjectType, detal info ,refer to nObjectType,
         *                              if new Site/List/Item, use url instead.
        * \param spFile(in)          retention parameter.
        * \param strAction (in)      it can be one of them.OPEN/DELETE/UPLOAD/EDIT
        * \param userPair (in/out)   it contains all information that SPE retrieve for user and SPE will pass this List to PC,
        *                               plugin can  append/modify the attributes for user into this List pair
        * \param srcPair (in/out)    it contains all information that SPE retrieve for source and SPE will pass this List to PC,
        *                               plugin can  append/modify the attributes for source into this List pair
        * \param dstPair (in/out)    it contains all information that SPE retrieve for target and SPE will pass this List to PC,
        *                               plugin can  append/modify the attributes for target into this List pair
        * \param nObjectType (in)    if spSource is: Site object , it is 1; List object ,it is 2; ListItem object, it is 3.
        * \param nTimeout(in)        it is the timeout setting for current plugin
        *
        */
        int GetCustomAttributes(
             Object spUser,
             Object spSource,
             Object SPFile,
             string strAction,
             Dictionary<string, string> userPair,
             Dictionary<string, string> srcPair,
             Dictionary<string, string> dstPair,
             int nObjectType,
             int nTimeout = 3000
         );
        bool PrepareFileAttributesDuringUpload(SPItemEventProperties spItemEventProperties, string filePath, ref List<KeyValuePair<string, string>> lsPrepareExtralAttributesRef);
    }

}
