using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Web;
using System.Net;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NextLabs.CSCInvoke;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using Microsoft.SharePoint.WebControls;
using System.DirectoryServices;
using System.Security.Principal;
using System.Xml.Serialization;
using Microsoft.IdentityModel.Claims;
using Microsoft.Office.Server.UserProfiles;
using Microsoft.Office.Server;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint.WebPartPages;
using System.Linq;
using Microsoft.SharePoint.Utilities;
using Microsoft.Office.Server.Search.Administration;
using SDKWrapperLib;
using QueryCloudAZSDK;
using QueryCloudAZSDK.CEModel;
using System.Net.Mail;
using static NextLabs.Common.PolicyEngineModule;
using NextLabs.Diagnostic;
using LogLevel = NextLabs.Diagnostic.LogLevel;
using Microsoft.SharePoint.Administration.Claims;
namespace NextLabs.Common
{
    public class Globals
    {
        static readonly Dictionary<CETYPE.CEAction, string> dicActionMaping = new Dictionary<CETYPE.CEAction, string>
        {
            { CETYPE.CEAction.Unknown, "" },
            { CETYPE.CEAction.Read, "OPEN" },
            { CETYPE.CEAction.Delete, "DELETE" },
            { CETYPE.CEAction.Move, "MOVE" },
            { CETYPE.CEAction.Copy, "COPY" },
            { CETYPE.CEAction.Write, "EDIT" },
            { CETYPE.CEAction.Rename, "RENAME" },
            { CETYPE.CEAction.ChangeAttrFile, "CHANGE_ATTRIBUTES" },
            { CETYPE.CEAction.ChangeSecFile, "CHANGE_SECURITY" },
            { CETYPE.CEAction.PrintFile, "PRINT" },
            { CETYPE.CEAction.PasteFile, "PASTE" },
            { CETYPE.CEAction.EmailFile, "EMAIL" },
            { CETYPE.CEAction.ImFile, "IM" },
            { CETYPE.CEAction.Export, "EXPORT" },
            { CETYPE.CEAction.Import, "IMPORT" },
            { CETYPE.CEAction.CheckIn, "CHECKIN" },
            { CETYPE.CEAction.CheckOut, "CHECKOUT" },
            { CETYPE.CEAction.Attach, "EDIT" },
            { CETYPE.CEAction.Run, "RUN" },
            { CETYPE.CEAction.Reply, "REPLY" },
            { CETYPE.CEAction.Forward, "FORWARD" },
            { CETYPE.CEAction.NewEmail, "NEW_EMAIL" },
            { CETYPE.CEAction.AVD, "AVDCALL" },
            { CETYPE.CEAction.Meeting, "MEETING" },
            { CETYPE.CEAction.ProcessTerminate, "PROC_TERMINATE" },
            { CETYPE.CEAction.WmShare, "SHARE" },
            { CETYPE.CEAction.WmRecord, "RECORD" },
            { CETYPE.CEAction.WmQuestion, "QUESTION" },
            { CETYPE.CEAction.WmVoice, "VOICE" },
            { CETYPE.CEAction.WmVideo, "VIDEO" },
            { CETYPE.CEAction.WmJoin, "JOIN" },
            { CETYPE.CEAction.View, "VIEW" },
            { CETYPE.CEAction.Upload, "EDIT" }
        };
        public const int defaultTimeout = 5000;
        public const bool bSPTrimAllow = true;
        public const string SPUserAlert = "DISPLAYUSERALERT";
        public const string SPPolicyName = "policyname";
        public const string SPPolicyMessage = "message";

        public enum CETagType
        {
            CE_TAG_ALLCOLUMN,
            CE_TAG_SYSCOLUMN,
            CE_TAG_SPECCOLUMN,
            CE_TAG_SYSSPECCOLUMN,
            CE_TAG_OTHER
        }

        [DllImport("TagDocProtector32.dll", EntryPoint = "TagProtector_AddTagParam", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void _TagProtector_AddTagParam32(byte[] URL, int dwUrlLen,
                                                            byte[] RemoteUser, int dwRemoteUserLen,
                                                            byte[] KeyName, int dwKeyNameLen,
                                                            byte[] KeyValue, int dwKeyValueLen,
                                                            bool bLastAttribute,
                                                            int nMillisecond, int nSecond,
                                                            int nMinute, int nHour,
                                                            int nDay, int nMonth, int nYear);
        [DllImport("TagDocProtector.dll", EntryPoint = "TagProtector_AddTagParam", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void _TagProtector_AddTagParam64(byte[] URL, int dwUrlLen,
                                                            byte[] RemoteUser, int dwRemoteUserLen,
                                                            byte[] KeyName, int dwKeyNameLen,
                                                            byte[] KeyValue, int dwKeyValueLen,
                                                            bool bLastAttribute,
                                                            int nMillisecond, int nSecond,
                                                            int nMinute, int nHour,
                                                            int nDay, int nMonth, int nYear);

        [DllImport("TagDocProtector32.dll", EntryPoint = "TagProtector_AddFileEncryptIgnore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int _TagProtector_AddFileEncryptIgnore32(string strFullUrl, string strRemoteUser, int nTicks);

        [DllImport("TagDocProtector32.dll", EntryPoint = "TagProtector_RemoveFileEncryptIgnore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int _TagProtector_RemoveFileEncryptIgnore32(string strFullUrl, string strRemoteUser, int nTicks);

        [DllImport("TagDocProtector.dll", EntryPoint = "TagProtector_AddFileEncryptIgnore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int _TagProtector_AddFileEncryptIgnore64(string strFullUrl, string strRemoteUser, int nTicks);

        [DllImport("TagDocProtector.dll", EntryPoint = "TagProtector_RemoveFileEncryptIgnore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int _TagProtector_RemoveFileEncryptIgnore64(string strFullUrl, string strRemoteUser, int nTicks);

        // Get the normal file tags(pdf and office file): the file tag name is lower, multiple file tag value use pointed separator to separate ("|" or ";").
        private static bool GetNormalFileTags(string filePath, Dictionary<string, string> dicTags, string separator, ref int errCode)
        {
            if (string.IsNullOrEmpty(filePath) || dicTags == null)
            {
                return false;
            }
            IFileTagManager fileTagManager = new FileTagManager();
            int nCount = 0;
            int iRet = fileTagManager.GetTagsCount(filePath, out nCount);
            if (iRet == (int)CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                string tagName = null;
                string tagValue = null;
                string tagLowerName = null;
                for (int i = 0; i < nCount; i++)
                {
                    iRet = fileTagManager.GetTagByIndex(filePath, i, out tagName, out tagValue);
                    if (iRet == (int)CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                    {
                        if (!string.IsNullOrEmpty(tagName))
                        {
                            tagLowerName = tagName.ToLower();
                            if (dicTags.ContainsKey(tagLowerName))
                            {
                                dicTags[tagLowerName] += (separator + tagValue); // mutiple value, use separator to separate.
                            }
                            else
                            {
                                dicTags[tagLowerName] = tagValue;
                            }
                        }
                    }
                    else
                    {
                        break; // Get tag failed.
                    }
                }
            }
            if (iRet == (int)CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                return true;
            }
            else
            {
                errCode = iRet;
                return false;
            }
        }

        // Get the file tags: the file tag name is lower, multiple file tag value use pointed separator to separate ("|" or ";").
        // "separator" default value is ";".
        public static bool GetFileTags(string filePath, Dictionary<string, string> dicTags, ref int errCode, string separator = ";")
        {
            if (string.IsNullOrEmpty(filePath) || dicTags == null)
            {
                return false;
            }

            GetNormalFileTags(filePath, dicTags, separator, ref errCode);

            if (errCode == (int)CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Remove the same values in tag value.
        private static string CheckSameTagValues(string tagValue, string separator)
        {
            if (!tagValue.Contains(separator))
            {
                return tagValue;
            }
            StringBuilder destValueBuilder = new StringBuilder();
            string[] splits = new string[1];
            splits[0] = separator;
            string[] arrValues = tagValue.Split(splits, StringSplitOptions.RemoveEmptyEntries);
            List<string> listvalues = new List<string>();
            foreach (string value in arrValues)
            {
                if (listvalues.Contains(value.ToLower()))
                {
                    //Remove the same value.
                }
                else
                {
                    listvalues.Add(value.ToLower()); // Record the "lower value".
                    if(destValueBuilder.Length == 0)
                    {
                        destValueBuilder.Append(value);
                    }
                    else
                    {
                        destValueBuilder.Append(separator + value);
                    }
                }
            }
            return destValueBuilder.ToString();
        }

        #region added for prefilter
        /// <summary>
        /// get current user profile
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="web"></param>
        /// <returns></returns>
        private static UserProfile GetUserProfile(string loginName, SPWeb web)
        {
            HttpContext context = HttpContext.Current;
            UserProfile profile = null;
            if (context == null)
            {
                SPServiceContext serverContext = SPServiceContext.GetContext(web.Site);
                UserProfileManager profileManager = new UserProfileManager(serverContext);
                if (profileManager.UserExists(loginName))
                {
                    profile = profileManager.GetUserProfile(loginName);
                }
                else
                {
                    string userName = web.CurrentUser.LoginName;
                    profile = GetClaimsUserProfile(profileManager, userName);
                }
            }
            else
            {
                var httpHandlerSPWeb = HttpContext.Current.Items["HttpHandlerSPWeb"];
                SPServiceContext serverContext = SPServiceContext.GetContext(web.Site);
                UserProfileManager profileManager = new UserProfileManager(serverContext);
                //TO fix the Exception"Operation is not valid due to the current state of the object."
                if (httpHandlerSPWeb == null)
                {
                    HttpContext.Current.Items["HttpHandlerSPWeb"] = web;
                }
                if (profileManager.UserExists(loginName))
                {
                    profile = profileManager.GetUserProfile(loginName);
                }
                else
                {
                    string userName = context.User.Identity.Name;
                    profile = GetClaimsUserProfile(profileManager, userName);
                }
            }
            return profile;
        }
        /// <summary>
        /// insert selected user claim info to dictionary
        /// </summary>
        /// <param name="claimValue"></param>
        /// <param name="claim"></param>
        /// <param name="selectedAttr"></param>
        /// <param name="userAttrs"></param>
        private static void InsertClaimIntoList(string claimValue, SPEClaim claim, List<string> selectedAttr, List<PrefilterMatchResult> userAttrs)
        {
            string attributeName = claim.attributename;
            string prefix = claim.prefix;
            if (!string.IsNullOrEmpty(attributeName) && (selectedAttr == null || selectedAttr.Contains(attributeName.ToLower())))
            {
                userAttrs.Add(new PrefilterMatchResult(attributeName, claimValue));
            }
            else if (!string.IsNullOrEmpty(prefix))
            {
                string claimName = claim.prefix + claim.claimtype.Substring(claim.claimtype.LastIndexOf("/") + 1);
                if (selectedAttr == null || selectedAttr.Contains(claimName.ToLower()))
                {
                    userAttrs.Add(new PrefilterMatchResult(claimName, claimValue));
                }
            }
        }
        /// <summary>
        /// insert selected user profile info to dictionary
        /// </summary>
        /// <param name="profileValue"></param>
        /// <param name="profile"></param>
        /// <param name="selectedAttr"></param>
        /// <param name="userAttrs"></param>
        private static void InsertProfileIntoList(string profileValue, SPEProperty profile, List<string> selectedAttr, List<PrefilterMatchResult> userAttrs)
        {
            string attributeName = profile.attributename;
            if (!string.IsNullOrEmpty(attributeName) && (selectedAttr == null || selectedAttr.Contains(attributeName.ToLower())))
            {
                userAttrs.Add(new PrefilterMatchResult(attributeName, profileValue));
            }
            else
            {
                string name = profile.name;
                if (!string.IsNullOrEmpty(name) && (selectedAttr == null || selectedAttr.Contains(name.ToLower())))
                {
                    userAttrs.Add(new PrefilterMatchResult(name, profileValue));
                }
            }
        }
        private static void GetUserClaimFromCache(string loginName, double timeout, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr, SPEClaim[] claimGroup, ref bool bExistedCache)
        {
            double current_time = ((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            double claimCacheTime = 0;
            lock (g_UserClaimCacheLock)
            {
                try
                {
                    if (g_UserClaimCacheTimeDic.ContainsKey(loginName))
                    {
                        claimCacheTime = g_UserClaimCacheTimeDic[loginName];
                    }
                    double timeMinus = current_time - claimCacheTime;
                    if (timeMinus <= timeout)
                    {
                        for (int i = 0; i < claimGroup.Length; i++)
                        {
                            var claimCachelist = g_UserClaimCacheList.Where(p => p.retKey == loginName + claimGroup[i].claimtype).ToList();
                            foreach(var claimCache in claimCachelist)
                            {
                                bExistedCache = true;
                                string claimValue = claimCache.retValue;
                                InsertClaimIntoList(claimValue, claimGroup[i], selectedAttr, userAttrs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during GetUserClaimFromCache:", null, ex);
                }
            }
        }
        private static void GetUserClaimRealTime(string loginName, IPrincipal principalUser, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr, SPEClaim[] claimGroup)
        {
            if (principalUser == null)
            {
                if (HttpContext.Current != null && HttpContext.Current.User != null)
                {
                    principalUser = HttpContext.Current.User;
                }
            }
            if (principalUser != null)
            {
                try
                {
                    IClaimsPrincipal icp = (principalUser) as IClaimsPrincipal;
                    if (icp != null)
                    {
                        IClaimsIdentity claimsIdentity = (IClaimsIdentity)icp.Identity;
                        if (claimsIdentity != null)
                        {
                            List<PrefilterMatchResult> claimCacheTemp = new List<PrefilterMatchResult>();
                            foreach (Claim claim in claimsIdentity.Claims)
                            {
                                if (string.IsNullOrEmpty(claim.Value))
                                {
                                    continue;
                                }
                                for (int i = 0; i < claimGroup.Length; i++)
                                {
                                    if (!claimGroup[i].disabled && claim.ClaimType.Equals(claimGroup[i].claimtype, StringComparison.OrdinalIgnoreCase))
                                    {
                                        InsertClaimIntoList(claim.Value, claimGroup[i], selectedAttr, userAttrs);
                                        claimCacheTemp.Add(new PrefilterMatchResult(loginName + claimGroup[i].claimtype, claim.Value));
                                    }
                                }
                            }
                            // update claim cache
                            UpdateUserClaimCacheList(claimCacheTemp);
                            //update current user claim cache time
                            UpdateUserClaimCacheTime(loginName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during GetUserClaimRealTime:", null, ex);
                }
            }
            else
            {
                NLLogger.OutputLog(LogLevel.Debug, "GetUserClaimRealTime principalUser is null");
            }
        }
        private static void GetUserProfileFromCache(string loginName, double timeout, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr, SPEProperty[] profileGroup,ref bool bExistedCache)
        {
            double current_time = (((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds));
            double profileCacheTime = 0;
            lock (g_UserProfileCacheLock)
            {
                try
                {
                    if (g_UserProfileCacheTimeDic.ContainsKey(loginName))
                    {
                        profileCacheTime = g_UserProfileCacheTimeDic[loginName];
                    }
                    double timeMinus = current_time - profileCacheTime;
                    if (timeMinus <= timeout)
                    {
                        for (int i = 0; i < profileGroup.Length; i++)
                        {
                            string key = loginName + profileGroup[i].name;
                            var profileCacheList = g_UserProfileCacheList.Where(p => p.retKey == key).ToList();
                            foreach(var profileCache in profileCacheList)
                            {
                                bExistedCache = true;
                                string profileValue = profileCache.retValue;
                                InsertProfileIntoList(profileValue, profileGroup[i], selectedAttr, userAttrs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during get user profile from cache:" , null, ex);
                }
            }
        }
        private static void GetUserProfileRealTime(string loginName, SPWeb web, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr, SPEProperty[] profileGroup)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                try
                {
                    UserProfile profile = GetUserProfile(loginName, web);
                    if (profile != null)
                    {
                        List<PrefilterMatchResult> profileCacheTemp = new List<PrefilterMatchResult>();
                        for (int i = 0; i < profileGroup.Length; i++)
                        {
                            var profileName = profileGroup[i].name;
                            if (profile[profileName].Value == null || string.IsNullOrEmpty(profile[profileName].Value.ToString()) || profileGroup[i].disabled)
                            {
                                continue;
                            }
                            string profileValue = profile[profileName].Value.ToString();
                            InsertProfileIntoList(profileValue, profileGroup[i], selectedAttr, userAttrs);
                            profileCacheTemp.Add(new PrefilterMatchResult(loginName + profileGroup[i].name, profileValue));
                        }
                        // update profile cache
                        UpdateUserProfileCacheList(profileCacheTemp);
                        //update current user profile cache time
                        UpdateUserProfileCacheTime(loginName);
                    }
                    else
                    {
                        NLLogger.OutputLog(LogLevel.Debug, string.Format("GetUserProfileRealTime:No profile for {0} is found in profilemanager", loginName));
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception duiring GetUserProfileRealTime:", null, ex);
                }
            });
        }
        private static void UpdateUserClaimCacheList(List<PrefilterMatchResult> claimCacheList)
        {
            lock (g_UserClaimCacheLock)
            {
                foreach (var claimCache in claimCacheList)
                {
                    g_UserClaimCacheList.Add(new PrefilterMatchResult(claimCache.retKey,claimCache.retValue));
                }
            }
        }
        private static void UpdateUserClaimCacheTime(string loginName)
        {
            var cacheTime = (DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            lock (g_UserClaimCacheLock)
            {
                g_UserClaimCacheTimeDic[loginName] = cacheTime;
            }
        }
        private static void UpdateUserProfileCacheList(List<PrefilterMatchResult> profileCacheTempList)
        {
            lock (g_UserProfileCacheLock)
            {
                foreach(var profileCache in profileCacheTempList)
                {
                    g_UserProfileCacheList.Add(new PrefilterMatchResult(profileCache.retKey,profileCache.retValue));
                }
            }
        }
        private static void UpdateUserProfileCacheTime(string loginName)
        {
            var cacheTime = ((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
            lock (g_UserProfileCacheLock)
            {
                g_UserProfileCacheTimeDic[loginName] = cacheTime;
            }
        }
        /// <summary>
        /// get user attr for prefilter
        /// </summary>
        /// <param name="principalUser"></param>
        /// <param name="userAttrs"></param>
        /// <param name="selectedAttr"></param>
        public static void GetUserClaim(IPrincipal principalUser, string loginName, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr)
        {
            if (principalUser == null)
            {
                if (HttpContext.Current != null && HttpContext.Current.User != null)
                {
                    principalUser = HttpContext.Current.User;
                }
            }
            if(principalUser == null)
            {
                return;
            }
            bool bExistedCache = false;
            Configuration claimConf = Common.Globals.Claimconf;
            if (claimConf != null && claimConf.SPEConfiguration != null
                && claimConf.SPEConfiguration.UserAttribute != null && claimConf.SPEConfiguration.UserAttribute.Claims != null
                && claimConf.SPEConfiguration.UserAttribute.Claims.disabled != true)
            {
                var claimGroup = claimConf.SPEConfiguration.UserAttribute.Claims.Claim;
                double timeOut = double.Parse(claimConf.SPEConfiguration.UserAttribute.Claims.cachetimeout);

                //gather user claim from cache
                GetUserClaimFromCache(loginName, timeOut, userAttrs, selectedAttr, claimGroup, ref bExistedCache);

                //gather user claim real time
                if (!bExistedCache)
                {
                    GetUserClaimRealTime(loginName, principalUser, userAttrs, selectedAttr, claimGroup);
                }
            }
        }
        /// <summary>
        /// get user profile for prefilter
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="userAttrs"></param>
        public static void GetUserProfile(SPWeb web, string loginName, List<PrefilterMatchResult> userAttrs, List<string> selectedAttr)
        {
            Configuration claimConf = Common.Globals.Claimconf;
            bool bExistedCache = false;
            if (claimConf != null && claimConf.SPEConfiguration != null && claimConf.SPEConfiguration.UserAttribute != null
               && claimConf.SPEConfiguration.UserAttribute.UserProfile != null && claimConf.SPEConfiguration.UserAttribute.UserProfile.disabled != true
               && claimConf.SPEConfiguration.UserAttribute.UserProfile.Property.Length > 0)
            {
                var profileGroup = claimConf.SPEConfiguration.UserAttribute.UserProfile.Property;
                double timeout = double.Parse(claimConf.SPEConfiguration.UserAttribute.UserProfile.cachetimeout);

                //get user profile from cache
                GetUserProfileFromCache(loginName, timeout, userAttrs, selectedAttr, profileGroup,ref bExistedCache);

                // get user profile real time
                if (!bExistedCache)
                {
                    GetUserProfileRealTime(loginName, web, userAttrs, selectedAttr, profileGroup);
                }
            }
        }
        #endregion

        public static void _GetRMSErrorMsg(int errcode, StringBuilder errorMsg)
        {
            CETYPE.CEResult_t result = (CETYPE.CEResult_t)errcode;
            errorMsg.Append(result.ToString());
        }

        public static void _TagProtector_AddTagParam(byte[] URL, int dwUrlLen,
                                                            byte[] RemoteUser, int dwRemoteUserLen,
                                                            byte[] KeyName, int dwKeyNameLen,
                                                            byte[] KeyValue, int dwKeyValueLen,
                                                            bool bLastAttribute,
                                                            int nMillisecond, int nSecond,
                                                            int nMinute, int nHour,
                                                            int nDay, int nMonth, int nYear)
        {
            if (IntPtr.Size == 4)
            {
                _TagProtector_AddTagParam32(URL, dwUrlLen,
                                            RemoteUser, dwRemoteUserLen,
                                            KeyName, dwKeyNameLen,
                                            KeyValue, dwKeyValueLen,
                                            bLastAttribute,
                                            nMillisecond, nSecond,
                                            nMinute, nHour,
                                            nDay, nMonth, nYear);
            }
            else
            {
                _TagProtector_AddTagParam64(URL, dwUrlLen,
                                            RemoteUser, dwRemoteUserLen,
                                            KeyName, dwKeyNameLen,
                                            KeyValue, dwKeyValueLen,
                                            bLastAttribute,
                                            nMillisecond, nSecond,
                                            nMinute, nHour,
                                            nDay, nMonth, nYear);
            }
        }

        public static int _TagProtector_AddFileEncryptIgnore(string strFullUrl, string strRemoteUser, int nTicks)
        {
            if(IntPtr.Size == 4)
            {
               return _TagProtector_AddFileEncryptIgnore32(strFullUrl, strRemoteUser, nTicks);
            }
            else
            {
               return _TagProtector_AddFileEncryptIgnore64(strFullUrl, strRemoteUser, nTicks);
            }
        }

        public static int _TagProtector_RemoveFileEncryptIgnore(string strFullUrl, string strRemoteUser, int nTicks)
        {
            if(IntPtr.Size == 4)
            {
                return _TagProtector_RemoveFileEncryptIgnore32(strFullUrl, strRemoteUser, nTicks);
            }
            else
            {
                return _TagProtector_RemoveFileEncryptIgnore64(strFullUrl, strRemoteUser, nTicks);
            }
        }

        public struct JavaPCParams
        {
            public bool bUseJavaPC;
            public string strPCHostAddress;
            public string strOAuthHostAddress;
            public string strClientID;
            public string strClientSecureKey;
            public string strTokenExpTime;
        }


        public struct UserActionInfo
        {
            public string url;
            public string ModuleName;
            public string before_url;
            public string after_url;
            public CETYPE.CEAction action;
            public UInt64 time;
        }

        public struct IPADDRCache
        {
            public IPHostEntry _IPHostEntry;
            public UInt64 time;
        }

        const string spSchemeName = "sharepoint";
        public const string HttpModuleName = "HttpModule";
        public const string EventHandlerName = "EventHandler";

        // I have a feeling that we will need to change it every time instead of a hard coded message...
        public const string EnforcementMessage = "You are not authorized to perform this action due to a policy enforced by NextLabs Entitlement Manager.";
        public const string NoPolicyEnforceMessage = "You are not authorized to perform this action due to a policy enforced by NextLabs Entitlement Manager.";
        public const string CE_SP_SEPERATOR = "";
        static public string CE_TAG_Extensions = "";
        public const int connectTimeoutMs = 5 * 1000;

        public const string strGlobalProcessUploadPropName = "ProcessUploadGlobalValue";
        public const string strGlobalProcessUploadPropValueNone = "uselibset";
        public const string strGlobalProcessUploadPropValueEnable = "enable";
        public const string strGlobalProcessUploadPropValueDisable = "disable";

        public const string strLibraryProcessUploadPropName = "ProcessUpload";
        public const string strLibraryProcessUploadPropValueEnable = "enable";
        public const string strLibraryProcessUploadPropValueDisable = "disable";

        // Flag for history versions RMS
        public const string strLibraryVersionsRMS = "VersionsRMS";

        //for classificaiton notification mistch
        public const string strSiteSchedulesPropName = "nxlsiteschedules";
        public const string strSiteSchIndexPropName = "nxlsiteschindex";
        public const string strSiteProcessStatePropName = "Nxl_CNM_Site_State";
        public const string strSiteCNMStatePropValue_Processing = "Nxl_CNM_Site_Processing";
        public const string strSiteCNMStatePropValue_Idle = "Nxl_CNM_Site_Idle";
		public const string strGlobalIPAddrName = "IPAddr";

        public const string strGlobalJavaPCPropertyName = "JavaPC";
        public const string strGlobalEnabled = "enabled";
        public const string strGlobalDisabled = "disabled";

        public const string strGlobalJavaPCHost = "JavaPCHost";
        public const string strGlobalJavaPCClientID = "JavaPCClientID";
        public const string strGlobalJavaPCClientSecureKey = "JavaPCClientSecureKey";
        public const string strGlobalJavaPCAUTHHost = "JavaPCAUTHHost";
        public const string strGlobalJavaPCAUTHUserName = "JavaPCAUTHUserName";
        public const string strGlobalJavaPCAUTHPwd = "JavaPCAUTHPwd";
        public const string strGlobalJavaPCExpireInMin = "JavaPCExpireInMin";
        public const string strGlobalPolicyEngineTag = "SPEEnforcer";
        public const int strGlobalPolicyEngineIntervalSec = 3600;

        public const string ActivatedSiteIds = "ActivatedSiteIds";
        public const string NewSitePEDefault = "NewSitePEDefault";

        public static UInt64 logTimeoutMs;
        const UInt64 default_logTimeoutMs = 5000;
        static public IntPtr connectHandle = IntPtr.Zero;
        static public Int32 sleepTimeWhenUpload = 1000;
        static public UInt32 succeedCountForUpload = 0;
		static private Configuration g_Claimconf;
        static private string g_ConfigDir = null;
        static private FileSystemWatcher g_Claimconfwatcher = null;
        static private object g_UserProfileCacheLock = new Object();
        static private object g_UserClaimCacheLock = new Object();
        static private List<PrefilterMatchResult> g_UserProfileCacheList = new List<PrefilterMatchResult>();
        static private List<PrefilterMatchResult> g_UserClaimCacheList = new List<PrefilterMatchResult>();
        static private Dictionary<string, double> g_UserProfileCacheTimeDic = new Dictionary<string, double>();
        static private Dictionary<string, double> g_UserClaimCacheTimeDic = new Dictionary<string, double>();
        static private int SidEvLength = 28;
        static public JavaPCParams g_JPCParams;
		static public List<KeyValuePair<string, string>> g_lstrXHeaders;

        public static Configuration Claimconf
        {
            get { return g_Claimconf; }
        }


        static private FileSystemWatcher g_Bundlewatcher = null;
        static private string g_PCDir = null;
		static private string g_SPEDir = null;
        static public DateTime g_BundleTime;


        static IDictionary<string, string> fixedAttrNameMap;
        static public IDictionary<string, List<UserActionInfo>> ActionMap = null;
        public static object syncActionMapRoot = new Object();
        static public IDictionary<string, IPADDRCache> IPAddressMap = null;
        public static object syncIPAddressMapRoot = new Object();
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern Int64 LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(int HModule);

        static Globals()
        {
			LoadClaimConfig();
            AttrNameMapInit();
            RegistersionInit();
            SPEEvalInit.LoadCETAGDEPLibrary();
            SPEEvalInit.LoadCETAGLibrary();
            ReadTagConfig();
            TrySoapConfigFile();
            FileWatchList();
            ReadJavaPCParams();
			ReadXHeaderParams();
        }

        ~Globals()
        {
            if (connectHandle != IntPtr.Zero)
            {
                CESDKAPI.CECONN_Close(connectHandle, connectTimeoutMs);
            }
        }
        public static bool ReadJavaPCParams()
        {
            g_JPCParams = new JavaPCParams();
            g_JPCParams.bUseJavaPC = false;
            SPWebApplication spAdminApp = null;
            if (GetAdministrationWebApplication(ref spAdminApp) && spAdminApp != null)
            {
                string strJavaPCEnabled = string.Empty;
                try
                {
                    strJavaPCEnabled = spAdminApp.Properties[strGlobalJavaPCPropertyName] as string;
                    if (!string.IsNullOrEmpty(strJavaPCEnabled) && strJavaPCEnabled.Equals(Globals.strGlobalEnabled))
                    {
                        g_JPCParams.bUseJavaPC = true;
                        g_JPCParams.strPCHostAddress= spAdminApp.Properties[Globals.strGlobalJavaPCHost] as string;
                        g_JPCParams.strOAuthHostAddress = spAdminApp.Properties[Globals.strGlobalJavaPCAUTHHost] as string;
                        g_JPCParams.strClientID = spAdminApp.Properties[Globals.strGlobalJavaPCClientID] as string;
                        g_JPCParams.strClientSecureKey = spAdminApp.Properties[Globals.strGlobalJavaPCClientSecureKey] as string;
                        g_JPCParams.strTokenExpTime = spAdminApp.Properties[Globals.strGlobalJavaPCExpireInMin] as string;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception is caught while loading JavaPC settings:", null, ex);
                }
            }
            return g_JPCParams.bUseJavaPC;
        }

        public static string GetPCPath()
        {
            if (g_PCDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegPPCInstallDir = null;
                string PCDir = null;
                if (CE_key != null)
                    RegPPCInstallDir = CE_key.GetValue("PolicyControllerDir");
                if (RegPPCInstallDir != null)
                {
                    String RegPPCInstallDir_str = Convert.ToString(RegPPCInstallDir);
                    if (RegPPCInstallDir_str.EndsWith("\\"))
                        PCDir = RegPPCInstallDir_str + "bin\\";
                    else
                        PCDir = RegPPCInstallDir_str + "\\bin\\";
                }
                g_PCDir = PCDir;
            }
            return g_PCDir;
        }
   		public static int ReadXHeaderParams()
        {
            if (g_lstrXHeaders == null)
            {
                g_lstrXHeaders = new List<KeyValuePair<string, string>>();
            }
            else
            {
                g_lstrXHeaders.Clear();
            }
            string strConfigPath = GetConfigPath() + "XHeaderConfig.xml";
            XmlDocument xdConfig = new XmlDocument();
            string strHeader = null;
            try
            {
                xdConfig.Load(strConfigPath);
                XmlNodeList nodelst = xdConfig.GetElementsByTagName("IP-Special");
                if (nodelst != null && nodelst.Item(0) != null)
                {
                    strHeader = nodelst.Item(0).InnerText;
                    g_lstrXHeaders.Add(new KeyValuePair<string, string>(strHeader, strHeader)); //only allow one IP-Special
                }
                nodelst = xdConfig.GetElementsByTagName("Item");
                string strItem = null;
                if (nodelst != null)
                {
                    foreach(XmlElement ele in nodelst)
                    {
                        strHeader = ele.GetAttribute("name");
                        if (ele.HasChildNodes)
                        {
                            strItem = ele.FirstChild.InnerText;
                        }
                        else
                        {
                            strItem = strHeader;
                        }
                        if (!string.IsNullOrEmpty(strHeader) && !string.IsNullOrEmpty(strItem))
                        {
                            g_lstrXHeaders.Add(new KeyValuePair<string, string>(strHeader, strItem));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception is caught while load xheader config file:", null, ex);
            }
            return g_lstrXHeaders.Count;
        }
        public static bool AddIPAddrAttribute(ref string[] srcAttr, string strIPAddress)
        {
            bool bRet = true;
            try
            {
  				//for IPV6, remove the charactors after %
                int nPos = strIPAddress.IndexOf('%');
                if (nPos != -1)
                    strIPAddress = strIPAddress.Substring(0, nPos);
                List<string> lstAttr = new List<string>(srcAttr);
                lstAttr.Add(strGlobalIPAddrName);
                lstAttr.Add(strIPAddress);
                srcAttr = lstAttr.ToArray();
            }
            catch
            {
                bRet = false;
            }
            return bRet;
        }

        public static string GetXHeaderIp(string userName, string webUrl, string strIPAddress)
        {
            string strEndIpAddr = strIPAddress;
            try
            {
                IPrincipal PrincipalUser = null;
                string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(userName, webUrl, ref PrincipalUser);
                if (!string.IsNullOrEmpty(clientIpAddr))
                {
                    strEndIpAddr = clientIpAddr;
                }
            }
            catch
            {
            }
            return strEndIpAddr;
        }

        public static void AddXHeaderAndIpAttribute(ref string[] srcAttr, string userName, string webUrl, string strIPAddress)
        {
            try
            {
                AddIPAddrAttribute(ref srcAttr, strIPAddress);
                List<string> strHeaderArr = null;
                WebRemoteAddressMap.GetXHeaderAttributes(userName, webUrl, ref strHeaderArr);
                if (strHeaderArr != null && strHeaderArr.Count > 0)
                {
                    srcAttr = srcAttr.Concat(strHeaderArr).ToArray();
                }
            }
            catch
            { }
        }

		public static string GetConfigPath()
        {
            if (g_ConfigDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegCEInstallDir = null;
                string CEConfigDir = null;
                if (CE_key != null)
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                if (RegCEInstallDir != null)
                {
                    String RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (RegCEInstallDir_str.EndsWith("\\"))
                        CEConfigDir = RegCEInstallDir_str + "config\\";
                    else
                        CEConfigDir = RegCEInstallDir_str + "\\config\\";
                }
                g_ConfigDir = CEConfigDir;
            }
            return g_ConfigDir;
        }

        public static string GetSPEPath()
        {
            if (g_SPEDir == null)
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegCEInstallDir = null;
                string CEBinDir = null;
                if (CE_key != null)
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                if (RegCEInstallDir != null)
                {
                    String RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (!RegCEInstallDir_str.EndsWith("\\"))
                        CEBinDir = RegCEInstallDir_str + "\\";
                    else
                        CEBinDir = RegCEInstallDir_str;
                }
                g_SPEDir = CEBinDir;
            }
            return g_SPEDir;
        }

        private static void LoadBundleTime()
        {
            String _filepath = GetPCPath();
            _filepath = _filepath.Replace("bin\\", "");
            _filepath += "bundle.bin";
            g_BundleTime = File.GetLastWriteTime(_filepath);
        }

        private static void Bundlewatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if ((e.ChangeType == WatcherChangeTypes.Changed
                || e.ChangeType == WatcherChangeTypes.Created)
                && e.Name.Equals("bundle.bin", StringComparison.OrdinalIgnoreCase))
                LoadBundleTime();
        }

        private static void FileWatchList()
        {
            g_Claimconfwatcher = new FileSystemWatcher();
            g_Claimconfwatcher.Path = GetConfigPath();

            g_Claimconfwatcher.Filter = "Configuration.xml";
            g_Claimconfwatcher.Changed += new FileSystemEventHandler(Claimwatcher_Changed);
            g_Claimconfwatcher.EnableRaisingEvents = true;
            g_Claimconfwatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastAccess
                                  | NotifyFilters.LastWrite | NotifyFilters.Size;

            if (g_Bundlewatcher == null)
                g_Bundlewatcher = new FileSystemWatcher();
            String _filepath = GetPCPath();
            _filepath = _filepath.Replace("bin\\", "");
            g_Bundlewatcher.Path = _filepath;

            g_Bundlewatcher.Filter = "*.bin";
            g_Bundlewatcher.Changed += new FileSystemEventHandler(Bundlewatcher_Changed);
            g_Bundlewatcher.Created += new FileSystemEventHandler(Bundlewatcher_Changed);
            g_Bundlewatcher.EnableRaisingEvents = true;
            g_Bundlewatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastAccess
                                  | NotifyFilters.LastWrite | NotifyFilters.Size;
            LoadBundleTime();
        }

        static void Claimwatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
                LoadClaimConfig();
        }

        public static bool LoadClaimConfig()
        {
            string _configfile = null;
            try
            {
                if (g_UserProfileCacheLock == null)
                    g_UserProfileCacheLock = new SortedList<string, string>();
                _configfile = GetConfigPath() + "Configuration.xml";
                NLLogger.OutputLog(LogLevel.Debug, "LoadClaimConfig:Loading configuration file from :" + _configfile);
                using (TextReader reader = new StreamReader(_configfile))
                {
                    XmlSerializer xmlSl = new XmlSerializer(typeof(Configuration));
                    g_Claimconf = (Configuration)xmlSl.Deserialize(reader);
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during LoadClaimConfig:", null, ex);
            }
            return false;
        }

        static public void ReadTagConfig()
        {
            try
            {
                RegistryKey Tag_key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\TagDocProtector\\", false);
                object RegTagConfig = null;
                if (Tag_key != null)
                    RegTagConfig = Tag_key.GetValue("Extensions");
                if (RegTagConfig != null)
                {
                    CE_TAG_Extensions = Convert.ToString(RegTagConfig);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during ReadTagConfig:", null, ex);
            }
        }

        public static void TrySoapConfigFile()
        {
            for (int i = 0; i <= 5; i++)
            {
                bool _re = SPEEvalInit._LoadSoapProtocolConfig();
                if (_re)
                {
                    break;
                }
            }
        }
        public static bool IsLibraryTemplateID(string templateID)
        {
            if (templateID.Equals("101")
                         || templateID.Equals("115")
                         || templateID.Equals("119")
                         || templateID.Equals("109")
                         || templateID.Equals("506")
                         || templateID.Equals("2100")
                         || templateID.Equals("1300")
                         || templateID.Equals("433")
                         || templateID.Equals("470")
                         || templateID.Equals("701")
                         || templateID.Equals("130")
                         || templateID.Equals("851")
                         || templateID.Equals("1302")
                         || templateID.Equals("3100")
                )
                return true;
            return false;
        }
        static public string GetDenyByExceptionMsg()
        {
            string strMsg = "Please input message here!";
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software", false);
                RegistryKey NextLabs_key = Software_key.OpenSubKey("NextLabs", false);
                if (NextLabs_key == null)
                {
                    NextLabs_key = Software_key.CreateSubKey("NextLabs", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey CE_key = NextLabs_key.OpenSubKey("Compliant Enterprise", false);
                if (CE_key == null)
                {
                    CE_key = NextLabs_key.CreateSubKey("Compliant Enterprise", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey SPE_key = CE_key.OpenSubKey("Sharepoint Enforcer", false);
                if (SPE_key == null)
                {
                    SPE_key = CE_key.CreateSubKey("Sharepoint Enforcer", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                object RegDenyByExceptionMsg = SPE_key.GetValue("DenyByExceptionMsg");
                if (RegDenyByExceptionMsg != null)
                {
                    strMsg = Convert.ToString(RegDenyByExceptionMsg);
                }
                else
                {
                    SPE_key.SetValue("DenyByExceptionMsg", strMsg);
                }

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception caught for rechieving DenyByExceptionMsg:", null, ex);
            }

            return strMsg;
        }

        static public bool GetPolicyDefaultBehavior()
        {
            //return value: true allow | false deny
            bool _DefaultBehavior = true;
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software", false);
                RegistryKey NextLabs_key = Software_key.OpenSubKey("NextLabs", false);
                if (NextLabs_key == null)
                {
                    NextLabs_key = Software_key.CreateSubKey("NextLabs", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey CE_key = NextLabs_key.OpenSubKey("Compliant Enterprise", false);
                if (CE_key == null)
                {
                    CE_key = NextLabs_key.CreateSubKey("Compliant Enterprise", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey SPE_key = CE_key.OpenSubKey("Sharepoint Enforcer", false);
                if (SPE_key == null)
                {
                    SPE_key = CE_key.CreateSubKey("Sharepoint Enforcer", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                object RegDefaultBehavior = SPE_key.GetValue("PolicyDefaultBehavior");
                if (RegDefaultBehavior != null)
                {
                    string _strDefaultBehavior = Convert.ToString(RegDefaultBehavior);
                    if (_strDefaultBehavior.Equals("Deny", StringComparison.OrdinalIgnoreCase))
                        _DefaultBehavior = false;
                    else if (_strDefaultBehavior.Equals("Allow", StringComparison.OrdinalIgnoreCase))
                        _DefaultBehavior = true;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during GetPolicyDefaultBehavior:", null, ex);
            }
            return _DefaultBehavior;
        }

        static public int GetPolicyDefaultTimeout()
        {
            int _DefaultTimeout = defaultTimeout;
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software", false);
                RegistryKey NextLabs_key = Software_key.OpenSubKey("NextLabs", false);
                if (NextLabs_key == null)
                {
                    NextLabs_key = Software_key.CreateSubKey("NextLabs", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey CE_key = NextLabs_key.OpenSubKey("Compliant Enterprise", false);
                if (CE_key == null)
                {
                    CE_key = NextLabs_key.CreateSubKey("Compliant Enterprise", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey SPE_key = CE_key.OpenSubKey("Sharepoint Enforcer", false);
                if (SPE_key == null)
                {
                    SPE_key = CE_key.CreateSubKey("Sharepoint Enforcer", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                object RegDefaultBehavior = SPE_key.GetValue("PolicyDefaultTimeout(ms)");
                if (RegDefaultBehavior != null)
                {
                    _DefaultTimeout = Convert.ToInt32(RegDefaultBehavior);
                }
                return _DefaultTimeout;
            }
            catch
            {
                return _DefaultTimeout;
            }
        }

        static void AttrNameMapInit()
        {
            fixedAttrNameMap = new SortedList<string, string>();
            ActionMap = new SortedList<string, List<UserActionInfo>>();
            IPAddressMap = new SortedList<string, IPADDRCache>();
            // We don't map vti_name and vti_description, because it seems
            // like they are not used in SharePoint.  I have never seen these
            // two keys appearing in any properties.
            fixedAttrNameMap["vti_title"] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            fixedAttrNameMap["Title"] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
        }


        static public void RegistersionInit()
        {
            try
            {
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey NextLabs_key = Software_key.OpenSubKey("NextLabs", true);
                if (NextLabs_key == null)
                {
                    NextLabs_key = Software_key.CreateSubKey("NextLabs", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey CE_key = NextLabs_key.OpenSubKey("Compliant Enterprise", true);
                if (CE_key == null)
                {
                    CE_key = NextLabs_key.CreateSubKey("Compliant Enterprise", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                RegistryKey SPE_key = CE_key.OpenSubKey("Sharepoint Enforcer", true);
                if (SPE_key == null)
                {
                    SPE_key = CE_key.CreateSubKey("Sharepoint Enforcer", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                object ReglogTimeoutMs = SPE_key.GetValue("logTimeoutMs");
                if (ReglogTimeoutMs == null)
                {
                    SPE_key.SetValue("logTimeoutMs", default_logTimeoutMs);
                    logTimeoutMs = default_logTimeoutMs;
                }
                else
                {
                    logTimeoutMs = Convert.ToUInt64(ReglogTimeoutMs);
                }

            }
            catch (Exception)
            {
                logTimeoutMs = default_logTimeoutMs;
            }
        }

        static public string AttrNameMapConvert(string srcAttrName,
                                                SPFieldCollection fields)
        {
            if (fixedAttrNameMap.ContainsKey(srcAttrName))
            {
                // This attr is a fixed attrs.  Use the hard-coded name
                // supplied by the SDK.
                return fixedAttrNameMap[srcAttrName];
            }
            else
            {
                // This attr is a custom attr.  Since SPFieldConnection can
                // only be indexed by display name but not internal name, and
                // we want to convert internal name to display name, we have
                // to do a brute-force search through the fields using the
                // internal name.
                foreach (SPField f in fields)
                {
                    if (f.InternalName == srcAttrName)
                    {
                        // A field with matching internal name is found.
                        return f.Title;
                    }
                }

                return srcAttrName;
            }
        }

        //William add this, to detect if a string is in a list,added in 20090224
        static private int IfInlist(string t, List<string> CompList)
        {
            int i = 0;
            for (i = 0; i < CompList.Count; i += 2)
            {
                if (t != null && CompList[i] != null)
                {
                    if (t.Equals(CompList[i], StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1;
        }

        // Re-construct value for some field types like "Calculated", "Lookup", "URL" and "Managed Metadata".
        static public void ReConstructByFieldType(SPField field, ref string fieldValue)
        {
            if (field.Type == SPFieldType.Calculated || field.Type == SPFieldType.Lookup)
            {
                string split = ";#";
                int index = fieldValue.IndexOf(split);
                if (index != -1)
                {
                    fieldValue = fieldValue.Substring(index + split.Length, fieldValue.Length - index - split.Length);
                }
            }
            else if(field.Type == SPFieldType.URL)
            {
                string split = ", ";
                int index = fieldValue.IndexOf(split);
                if (index != -1)
                {
                    fieldValue = fieldValue.Substring(index + split.Length, fieldValue.Length - index - split.Length);
                }
            }
            else if (field.Type == SPFieldType.Invalid && field.TypeDisplayName.Equals("Managed Metadata", StringComparison.OrdinalIgnoreCase))
            {
                if (fieldValue.Contains(";")) // multiple value
                {
                    string[] values = fieldValue.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < values.Length; i++)
                    {
                        int index = values[i].LastIndexOf("|");
                        if (index != -1)
                        {
                            values[i] = values[i].Substring(0, index);
                        }
                    }
                    fieldValue = String.Join(";", values);
                }
                else
                {
                    int index = fieldValue.LastIndexOf("|");
                    if (index != -1)
                    {
                        fieldValue = fieldValue.Substring(0, index);
                    }
                }
            }
        }

        static public void SetListPairInfoIntoArray(ref string[] szArrayInfoRef, List<KeyValuePair<string, string>> lsNewInfo)
        {
            if ((null != lsNewInfo) && (0 < lsNewInfo.Count))
            {
                int nLastIndex = szArrayInfoRef.Length;
                Array.Resize(ref szArrayInfoRef, szArrayInfoRef.Length + (lsNewInfo.Count * 2));
                foreach (KeyValuePair<string, string> pairItem in lsNewInfo)
                {
                    szArrayInfoRef[nLastIndex] = pairItem.Key;
                    szArrayInfoRef[nLastIndex + 1] = pairItem.Value;
                }
            }
        }

        static public bool MyDeleteFile(string strFilePath)
        {
            bool bRet = false;
            try
            {
                if (String.IsNullOrEmpty(strFilePath))
                {
                    bRet = true;
                }
                else
                {
                    if (File.Exists(strFilePath))
                    {
                        File.Delete(strFilePath);
                    }
                    else
                    {
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                NLLogger.OutputLog(LogLevel.Debug, $"Exception during delte local file:[{strFilePath}]:", null, ex);
            }
            return bRet;
        }

        //William add this, this is to get the spfile properties except "Created" and "Modified"
        static public string[] BuildAttrArray2FromSPField
        (SPWeb web, SPList list, SPListItem item, string[] extraAttrs)
        {
            if (web == null || item == null)
                return extraAttrs;
            //To avoid list comparing time problem, use a hashtable. Peformance issue
            Hashtable hs = new Hashtable();
            int _additionalcnt = 0;
            string created = "";
            string modified = "";
            string created_time = "";
            string modified_time = "";
            if (extraAttrs != null)
            {
                for (int i = 0; i < extraAttrs.Length / 2; i++)
                {
                    if (!string.IsNullOrEmpty(extraAttrs[i * 2 + 1]))
                    {
                        bool found = false;
                        if (hs.Contains(extraAttrs[i * 2]))
                        {
                            foreach (string _value in (List<string>)hs[extraAttrs[i * 2]])
                            {
                                if (_value.Equals(extraAttrs[i * 2 + 1], StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                ((List<string>)hs[extraAttrs[i * 2]]).Add(extraAttrs[i * 2 + 1]);
                                _additionalcnt++;
                            }
                        }
                        else
                        {
                            List<string> _list = new List<string>();
                            _list.Add(extraAttrs[i * 2 + 1]);
                            hs.Add(extraAttrs[i * 2], _list);
                        }
                    }
                }
            }
            SPFieldCollection fieldCollection = item.Fields;
            foreach (SPField f in fieldCollection)
            {
                //if prefilterResList !=null,means prefilter is enable
                if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(f.Title.ToLower()))
                {
                    continue;
                }
                object attr_obj = null;
                string attr_value = null;
                string attr_name = f.ToString();
                if (attr_name == null)
                    continue;
                try
                {
                    attr_obj = item[f.InternalName];
                }
                catch
                {
                }

                if (attr_obj != null)
                    attr_value = attr_obj.ToString();
                if (!string.IsNullOrEmpty(attr_value))
                {
                    ReConstructByFieldType(f, ref attr_value); // Re-construct value for some field types.

                    //add this code to replace ReplaceHashTime()
                    if (attr_name.Equals("created", StringComparison.OrdinalIgnoreCase))
                    {
                        created=attr_value;
                        created_time = Globals.ConvertTime(created);
                    }
                    else if (attr_name.Equals("modified", StringComparison.OrdinalIgnoreCase))
                    {
                        modified = attr_value;
                        modified_time = Globals.ConvertTime(modified);
                    }
                    if (!(attr_name.Equals("created", StringComparison.OrdinalIgnoreCase)) && !(attr_name.Equals("modified", StringComparison.OrdinalIgnoreCase)))
                    {
                        //Fix bug 8968, convert all "description" to "desc", Addeb by William 20090323
                        if (attr_name.Equals("description", StringComparison.OrdinalIgnoreCase))
                            attr_name = "desc";
                        //added by chellee for the bug 8998
                        if ((f.FieldTypeDefinition != null) && (f.FieldTypeDefinition.TypeDisplayName.ToString() == "Person or Group"))
                        {
                            if (attr_value.IndexOf(";#") != -1)
                            {
                                string[] arr = attr_value.Split('#');
                                attr_value = arr[1];
                            }

                        }

                        //To fix bugs 8729 and 9013, we skip adding the
                        //attribute only if the name/value pair already exists
                        //case-insensitively.
                        //(This code is O(n^2).  Using a hash map will improve
                        //performance here.)
                        //To avoid list comparing time problem, use a hashtable. Peformance issue
                        bool contain = false;
                        bool found = false;
                        if (hs.Contains(attr_name))
                        {
                            contain = true;
                            foreach (string _value in (List<string>)hs[attr_name])
                            {
                                if (_value.Equals(attr_value, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (!found)
                        {
                            if (contain)
                            {
                                ((List<string>)hs[attr_name]).Add(attr_value);
                                _additionalcnt++;
                            }
                            else
                            {
                                List<string> _list = new List<string>();
                                _list.Add(attr_value);
                                hs.Add(attr_name, _list);
                            }
                        }
                    }
                }
            }
            //To avoid list comparing time problem, use a hashtable. Peformance issue
            string[] returnArray = new string[hs.Count * 2 + _additionalcnt * 2];
            int _index = 0;
            string createdBy = null, docCreatedBy = null, modifiedBy = null, docModifiedBy = null;
            int iCreatedBy = -1, iDocCreatedBy = -1, iModifiedBy = -1, iDocModifiedBy = -1;
            foreach (DictionaryEntry de in hs)
            {
                string _attrname = de.Key.ToString();
                foreach (string _value in (List<string>)de.Value)
                {
                    returnArray[_index] = _attrname;
                    if (_attrname.Equals("created", StringComparison.OrdinalIgnoreCase))
                    {
                        if (created_time != null && created_time.Length != 0)
                        {

                            returnArray[_index + 1] = created_time;
                            _index += 2;
                        }
                    }
                    else if (_attrname.Equals("modified", StringComparison.OrdinalIgnoreCase))
                    {
                        if (modified_time != null && modified_time.Length != 0)
                        {
                            returnArray[_index + 1] = modified_time;
                            _index += 2;
                        }
                    }
                    else
                    {
                        returnArray[_index + 1] = _value;
                        _index += 2;
                        {
                            if (_attrname.Equals("created_by", StringComparison.OrdinalIgnoreCase))
                            {
                                createdBy = _value;
                                iCreatedBy = _index - 2;
                            }
                            else if (_attrname.Equals("modified_by", StringComparison.OrdinalIgnoreCase))
                            {
                                modifiedBy = _value;
                                iModifiedBy = _index - 2;
                            }
                            else if (_attrname.Equals("document created by", StringComparison.OrdinalIgnoreCase))
                            {
                                docCreatedBy = _value;
                                iDocCreatedBy = _index - 2;
                            }
                            else if (_attrname.Equals("document modified by", StringComparison.OrdinalIgnoreCase))
                            {
                                docModifiedBy = _value;
                                iDocModifiedBy = _index - 2;
                            }
                        }
                    }
                }
            }
            if (iCreatedBy != -1 && iDocCreatedBy != -1)
            {
                if (!String.IsNullOrEmpty(docCreatedBy))
                {
                    if (docCreatedBy.EndsWith(createdBy ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        returnArray[iCreatedBy] = UserSid.GetUserSid(web, docCreatedBy);
                    }
                }
            }
            if (iModifiedBy != -1 && iDocModifiedBy != -1)
            {
                if (!String.IsNullOrEmpty(docModifiedBy))
                {
                    if (docModifiedBy.EndsWith(modifiedBy ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        returnArray[iModifiedBy] = UserSid.GetUserSid(web, docModifiedBy);
                    }
                }
            }
            return returnArray;
        }

        //William add this, this is the new function to get item propties diffferent from spe1.0's method
        static public string[] BuildAttrArrayFromSPField(SPWeb web, SPList list, SPListItem item)
        {
            List<string> ls = new List<string>();
            SPFieldCollection fieldCollection = item.Fields;
            foreach (SPField f in fieldCollection)
            {
                //change the f.ToString() to f.InternalName, william changed
                try
                {
                    object attr_obj = item[f.InternalName];
                    string attr_value = null;
                    if (attr_obj != null)
                        attr_value = attr_obj.ToString();
                    if (!string.IsNullOrEmpty(attr_value))
                    {
                        ls.Add(f.ToString());
                        ls.Add(attr_value);
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception catched for BuildAttr:", null, ex);
                }
            }
            return ls.ToArray();
        }

        // fix bug 8873 by derek
        static public string[] BuildAttrArrayFromHashTable
            (Hashtable properties, string[] extraAttrs)
        {
            List<string> ls = new List<string>();

            if (extraAttrs != null)
            {
                for (int i = 0; i < extraAttrs.Length / 2; i++)
                {
                    if (!string.IsNullOrEmpty(extraAttrs[i * 2 + 1]))
                    {
                        ls.Add(extraAttrs[i * 2]);
                        ls.Add(extraAttrs[i * 2 + 1]);
                    }
                }
            }
            foreach (DictionaryEntry de in properties)
            {
                //if prefilterResList !=null,means prefilter is enable
                if (de.Key != null && !string.IsNullOrEmpty(de.Key.ToString()) && SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(de.Key.ToString().ToLower()))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(de.Value.ToString()))
                {
                    ls.Add(de.Key.ToString());
                    ls.Add(de.Value.ToString());
                }
            }

            return ls.ToArray();
        }

        static public string ConvertTime(string orignal)
        {
            DateTime past = DateTime.Now;
            if (DateTime.TryParse(orignal,out past) == false)
                return "0";

            DateTime time = past.ToUniversalTime();
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc);

            double span = (time - refTime).TotalMilliseconds;

            if (span >= 0.0)
            {
                // Round down to the millisecond.
                return ((UInt64)span).ToString();
            }
            else
            {
                // Return the earliest time that we can.
                return "0";
            }
        }

        public static string UrlDecode(string inputUrl)
        {
            return string.IsNullOrEmpty(inputUrl)
                ? string.Empty
                : HttpUtility.UrlDecode(inputUrl.Replace("+", "%2b"));
        }
        public static String HttpModule_ReBuildURL(String _FullURL, String _PathURL, String _AddOnPath)
        {
            if (_FullURL != null)
            {
                _FullURL = UrlDecode(_FullURL);
                if (_FullURL != null && _PathURL != null && _AddOnPath != null)
                {
                    if (_FullURL.EndsWith(_PathURL, StringComparison.OrdinalIgnoreCase))//That's ok, no tail fix
                    {
                        return _FullURL;
                    }
                    else //That is not good, it means the url has a tail
                    {
                        int index = _FullURL.LastIndexOf(_PathURL, StringComparison.OrdinalIgnoreCase);
                        if (index != -1)
                        {
                            int length = _PathURL.Length;
                            String _ReBuildURL = _FullURL.Substring(0, index + length);
                            String _QueryString = _FullURL.Substring(index + _AddOnPath.Length, _FullURL.Length - index - _AddOnPath.Length);
                            return _ReBuildURL + _QueryString;
                        }
                        else
                        {
                            return _FullURL;
                        }

                    }
                }
            }
            return _FullURL;
        }

        static public string[] ReplaceHashTime(SPWeb web, SPList list, SPListItem item, string[] propertyArray)
        {
            return propertyArray;
        }


        static private string[] BuildAttrArrayAddExtraAttrs
        (List<string> ls, string[] extraAttrs, SPBaseType baseType,
         SPFieldCollection fields)
        {
            if (extraAttrs != null)
            {
                for (int i = 0; i < extraAttrs.Length / 2; i++)
                {
                    if (!string.IsNullOrEmpty(extraAttrs[i * 2 + 1]))
                    {
                        ls.Add(extraAttrs[i * 2]);
                        ls.Add(extraAttrs[i * 2 + 1]);
                    }
                }
            }

            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE);
            ls.Add(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM);
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE);
            ls.Add(baseType == SPBaseType.DocumentLibrary ?
                   CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM :
                   CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM);

            return ls.ToArray();
        }

        static public string[] BuildAttrArrayFromItemEventProperties
        (SPItemEventDataCollection properties, string[] extraAttrs,
         SPBaseType baseType, SPFieldCollection fields)
        {
            List<string> ls = new List<string>();
            string key = "";
            string value = "";
            if (properties != null)
            {
                foreach (DictionaryEntry de in properties)
                {
                    if (de.Key != null && !string.IsNullOrEmpty(de.Key.ToString()) && de.Value != null && !string.IsNullOrEmpty(de.Value.ToString()))
                    {
                        key = AttrNameMapConvert(de.Key.ToString(), fields);
                        //if prefilterResList !=null,means prefilter is enable
                        if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(key.ToLower()))
                        {
                            continue;
                        }
                        value = de.Value.ToString();
                        if (key.Equals("Created", StringComparison.OrdinalIgnoreCase)
                            || key.Equals("Modified", StringComparison.OrdinalIgnoreCase))
                        {
                            value = ConvertTime(value);
                        }

                        if (key.Equals("file size", StringComparison.OrdinalIgnoreCase)
                            && value.Equals("0"))
                        {
                            value = "1";
                        }
                        ls.Add(key);
                        ls.Add(value);
                    }
                }
            }
            return BuildAttrArrayAddExtraAttrs(ls, extraAttrs, baseType, fields);
        }

        static public string[] BuildAttrArrayFromItemProperties
        (Hashtable properties, string[] extraAttrs, SPBaseType baseType,
         SPFieldCollection fields)
        {
            List<string> ls = new List<string>();
            string key = "";
            string value = "";
            if (properties != null)
            {
                foreach (DictionaryEntry de in properties)
                {
                    if (de.Key != null && !string.IsNullOrEmpty(de.Key.ToString()) && de.Value != null && !string.IsNullOrEmpty(de.Value.ToString()))
                    {
                        key = AttrNameMapConvert(de.Key.ToString(), fields);
                        //if prefilterResList !=null,means prefilter is enable
                        if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(key.ToLower()))
                        {
                            continue;
                        }
                        value = de.Value.ToString();
                        if (key.Equals("Created", StringComparison.OrdinalIgnoreCase)
                            || key.Equals("Modified", StringComparison.OrdinalIgnoreCase))
                        {
                            value = ConvertTime(value);
                        }

                        if (key.Equals("file size", StringComparison.OrdinalIgnoreCase)
                            && value.Equals("0"))
                        {
                            value = "1";
                        }
                        ls.Add(key);
                        ls.Add(value);
                    }
                }
            }
            return BuildAttrArrayAddExtraAttrs(ls, extraAttrs, baseType, fields);
        }

        static public string TrimEndUrlSegments(string url, int n)
        {
            int index = url.Length;

            for (; n > 0; n--)
            {
                index = url.LastIndexOf('/', index - 1, index);
            }

            return url.Remove(index);
        }

        // Construct the "URL" for "SPWeb", "SPList" and "SPListItem";
        static public string ConstructObjectUrl(object obj)
        {
            string url = null;
            if(obj == null)
            {
                return url;
            }

            if (obj is SPWeb)
            {
                SPWeb web = obj as SPWeb;
                url = web.Url;
            }
            else if(obj is SPList)
            {
                SPList list = obj as SPList;
                url = ConstructListUrl(list.ParentWeb, list);
            }
            else if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                url = ConstructListItemUrl(item);
            }
            return url;
        }

        // Construct "URL" for "SPListItem";
        static public string ConstructListItemUrl(SPListItem item)
        {
            string url = null;
            if (item == null)
            {
                return url;
            }
            SPList list = item.ParentList;
            if (list.BaseType == SPBaseType.DocumentLibrary)
            {
                url = item.ParentList.ParentWeb.Url + "/" + item.Url; // library item
            }
            else
            {
                string itemName = list.BaseType == SPBaseType.Survey ? item.DisplayName : item.Name;
                url = Globals.ConstructListUrl(list.ParentWeb, list) + "/" + itemName; // list item
            }
            return url;
        }

        static public string ConstructListUrl(SPWeb web, SPList list)
        {
            string listUrl = "";
            try
            {
                if (list != null)
                {
                    if (list.RootFolder != null)
                    {
                        SPWeb parWeb = list.ParentWeb;
                        listUrl = parWeb.Site.MakeFullUrl(list.RootFolder.ServerRelativeUrl);
                    }
                    else
                    {
                        listUrl = OldConstructListUrl(web, list);
                    }
                }

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during ConstructListUrl:", null, ex);
            }
            return listUrl;
        }

        static private string OldConstructListUrl(SPWeb web, SPList list)
        {
            string defaultViewUrl, combinedUrl;
            int lastSlashIndex, secondLastSlashIndex;
            defaultViewUrl = CommonVar.GetSPListContent(list, "url");
            if (string.IsNullOrEmpty(defaultViewUrl))
            {
                defaultViewUrl = list.DefaultViewUrl;
            }
            lastSlashIndex = defaultViewUrl.LastIndexOf('/');

            if (lastSlashIndex > 0)
            {
                // There may be a second last '/'.  Try to find it.
                secondLastSlashIndex = defaultViewUrl.
                    LastIndexOf('/', lastSlashIndex - 1, lastSlashIndex);
            }
            else
            {
                // There is no second last '/'.
                secondLastSlashIndex = -1;
            }
            SPBaseType _SPBaseType = list.BaseType;
            if ((_SPBaseType == SPBaseType.GenericList
                || _SPBaseType == SPBaseType.Survey
                || _SPBaseType == SPBaseType.DiscussionBoard
                || _SPBaseType == SPBaseType.Issue) &&
                lastSlashIndex != -1)
            {
                combinedUrl = web.Site.MakeFullUrl
                    (defaultViewUrl.Remove(lastSlashIndex));
            }
            else if (_SPBaseType == SPBaseType.DocumentLibrary &&
                     secondLastSlashIndex != -1)
            {
                combinedUrl = web.Site.MakeFullUrl
                    (defaultViewUrl.Remove(secondLastSlashIndex));
            }
            else
            {
                return "";
            }

            return combinedUrl;
        }

        static public SPListItem ParseItemFromAttachmentURL(SPWeb web, String url)
        {
            SPList list = null;
            SPListItem item = null;

            try
            {
                list = web.GetList(url);
                string tmpUrl = "/attachments/";
                int index = url.IndexOf(tmpUrl, StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    string strID = url.Substring(index + tmpUrl.Length);
                    index = strID.IndexOf('/');
                    if (index > 0)
                    {
                        strID = strID.Substring(0, index);
                    }
                    int listId = Int32.Parse(strID);
                    item = list.GetItemById(listId);
                }
            }
            catch
            {
            }

            return item;
        }

        //Added by herbert
        static public string ConstructFolderUrl(SPWeb web, SPFolder folder)
        {
            string defaultViewUrl = "", combinedUrl;

            defaultViewUrl += folder.ServerRelativeUrl;

            combinedUrl = web.Site.MakeFullUrl(defaultViewUrl);

            return combinedUrl;
        }
        //end added

        // Converting the URL to Resource Signature, the format that
        // make CE happy.
        //  1. Make the URL comforms with FQDN first
        //  2. Convert the http:// to sharepoint://
        public static string UrlToResSig(string url)
        {
            string result = "";
            if (!String.IsNullOrEmpty(url))
            {
                string qualifiedURL = url;
                try
                {
                    UriBuilder tmp_uri = new UriBuilder(url);

                    if (!String.IsNullOrEmpty(tmp_uri.Uri.DnsSafeHost))
                    {
                        IPHostEntry ipentry = Dns.GetHostEntry(tmp_uri.Uri.DnsSafeHost);
                        tmp_uri.Host = ipentry.HostName;
                    }
                    qualifiedURL = Uri.UnescapeDataString(tmp_uri.Uri.AbsoluteUri);
                }
                catch(Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during UrlToResSig:", null, ex);
                }

                int firstColonIndex = qualifiedURL.IndexOf(':');

                if (firstColonIndex == -1)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "UrlToResSig: invalid url:" + qualifiedURL);
                }
                else
                {
                    result = spSchemeName + qualifiedURL.Substring(firstColonIndex);
                }
            }
            return result;
        }

        public static string GetIP4Address(string IPaddress)
        {
            string IP4Address = String.Empty;

            if (!String.IsNullOrEmpty(IPaddress))
            {
                foreach (IPAddress IPA in Dns.GetHostAddresses(IPaddress))
                {
                    if (IPA.AddressFamily.ToString() == "InterNetwork")
                    {
                        IP4Address = IPA.ToString();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(IP4Address))
                {
                    return IP4Address;
                }
            }

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            return IP4Address;
        }

        static public uint IPAddressToIPNumber(string IPaddress)
        {
            uint num = 0;
            string[] arrDec;

            IPaddress = GetIP4Address(IPaddress);

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

        static public string GetItemUserSidFromUserString(SPListItem item, string userString)
        {
            int memberID = int.Parse(userString.Remove(userString.IndexOf(";#")));
            SPUser user = null;
            // From observation, sometimes some user exists in SiteUsers but
            // not in AllUsers.  So if we can't find the user in AllUsers, we
            // try SiteUsers.  Then, if we can't find ther use in SiteUsers
            // either, we might as well try Users also before we give up.
            try
            {
                // Try AllUsers.
                user = item.Web.AllUsers.GetByID(memberID);
            }
            catch (SPException)
            {
                // memberID not found in AllUsers.  Try SiteUsers.
                try
                {
                    user = item.Web.SiteUsers.GetByID(memberID);
                }
                catch (SPException)
                {
                    // memberID not found in SiteUsers either.  Try Users.
                    try
                    {
                        user = item.Web.Users.GetByID(memberID);
                    }
                    catch
                    {
                        // memberID not found in Users either.  Give up.
                        return string.Empty;
                    }
                }
            }
            if (user != null)
            {
                return user.UserId.NameId.ToUpper();
            }
            return string.Empty;
        }

        static public string GetItemCreatedBySid(SPListItem item)
        {
            // According to MSDN, item[] should be indexed by the display name
            // of the field (default is "Created By").  However, since the
            // display name can be changed by the user and might not be unique,
            // it's not reliable.  So here we use the internal name ("Author")
            // instead, which happens to also work.
            String userString = (String)item["Author"];
            return GetItemUserSidFromUserString(item, userString);
        }

        static public string GetItemModifiedBySid(SPListItem item)
        {
            // According to MSDN, item[] should be indexed by the display name
            // of the field (default is "Modified By").  However, since the
            // display name can be changed by the user and might not be unique,
            // it's not reliable.  So here we use the internal name ("Editor")
            // instead, which happens to also work.
            String userString = (String)item["Editor"];
            return GetItemUserSidFromUserString(item, userString);
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static public string GetCurrentTimeStr()
        {
            // Round down to the millisecond.
            return (((UInt64)
                     ((DateTime.Now.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                      TotalMilliseconds)).
                    ToString());
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static public string ConvertDataTime2Str(DateTime date)
        {
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc);

            double span = (date - refTime).TotalMilliseconds;

            if (span >= 0.0)
            {
                // Round down to the millisecond.
                return ((UInt64)span).ToString();
            }
            else
            {
                // Return the earliest time that we can.
                return "0";
            }
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static string GetItemTimeStrFromField(SPListItem item,
                                              string fieldInternalName)
        {
            DateTime time = ((DateTime)item[fieldInternalName]).
                ToUniversalTime();
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc);

            double span = (time - refTime).TotalMilliseconds;

            if (span >= 0.0)
            {
                // Round down to the millisecond.
                return ((UInt64)span).ToString();
            }
            else
            {
                // Return the earliest time that we can.
                return "0";
            }
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static public string GetItemCreatedStr(SPListItem item)
        {
            // According to MSDN, item[] should be indexed by the display name
            // of the field (default is "Created").  However, since the display
            // name can be changed by the user and might not be unique, it's
            // not reliable.  So here we use the internal name (also "Created")
            // instead, which happens to also work.
            return GetItemTimeStrFromField(item, "Created");
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static public string GetItemModifiedStr(SPListItem item)
        {
            // According to MSDN, item[] should be indexed by the display name
            // of the field (default is "Modified").  However, since the
            // display name can be changed by the user and might not be unique,
            // it's not reliable.  So here we use the internal name (also
            // "Modified") instead, which happens to also work.
            return GetItemTimeStrFromField(item, "Modified");
        }

        /* Returns a string representing the number of bytes in the file */
        static public string GetItemFileSizeStr(SPListItem item)
        {
            // According to MSDN, item[] should be indexed by the display name
            // of the field (default is "File Size").  However, since the
            // display name can be changed by the user and might not be unique,
            // it's not reliable.  So here we use the internal name
            // ("FileSizeDisplay") instead, which happens to also work.
            try
            {
                string FileSizeDisplay = "";
                // George: check the item fields contain the "FileSizeDisplay" field beofore using it.
                string field = "FileSizeDisplay";
                if (item != null && item.Fields.ContainsField(field) && item[field] != null)
                {
                    FileSizeDisplay = item[field].ToString();
                }
                return FileSizeDisplay;
            }
            catch (Exception ex)
            {
                // Either item is not a doc lib item, or there is an error.
                NLLogger.OutputLog(LogLevel.Info, "Exception catched for GetItemFileSizeStr:", null, ex);
                return "";
            }
        }

        // Check the field to do tagging.(it is not hidden and supported type.)
        static private bool CheckTaggingField(SPField field)
        {
            //only support four types for tagging,added by William 20091113
            if (!field.Hidden && (field.Type == SPFieldType.Text || field.Type == SPFieldType.Number || field.Type == SPFieldType.Choice || field.Type == SPFieldType.MultiChoice
                || field.Type == SPFieldType.Currency || field.Type == SPFieldType.Note || field.Type == SPFieldType.URL
                || (field.Type == SPFieldType.Invalid && field.TypeDisplayName.Equals("Managed Metadata", StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
            return false;
        }

        static private void SetDestSrcAttrsFromField(SPListItem item, SPField field,string key, Dictionary<string, string> allSPEColumns, Dictionary<string, KeyValuePair<string, string>> destSrcAttrs)
        {
            object fieldValue = item[field.InternalName];
            if (fieldValue == null || string.IsNullOrEmpty(fieldValue.ToString()))
            {
                return;
            }
            string strfieldValue = fieldValue.ToString();
            if (strfieldValue.Contains(";")) // multiple value
            {
                string[] values = strfieldValue.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < values.Length; i++)
                {
                    int index = values[i].LastIndexOf("|");
                    if (index != -1)
                    {
                        values[i] = values[i].Substring(0, index);
                    }
                }
                strfieldValue = String.Join(";", values);
            }
            else//signle value
            {
                int index = strfieldValue.LastIndexOf("|");
                if (index != -1)
                {
                    strfieldValue = strfieldValue.Substring(0, index);
                }
            }
            allSPEColumns[key.ToLower()] = key;
            SetDestSrcAttrs(destSrcAttrs, key, strfieldValue);
        }
        //George: Filter the source attributes from library columns, site columns and system columns.
        static private void SPE_FilterSrcAttrs(String[] srcAttr, SPListItem item, List<string> sysColumns, List<string> libLowerClomuns, List<string> siteColumns, List<string> portalSiteColumns,
            Dictionary<string, string> defineColumns, Dictionary<string, KeyValuePair<string, string>> destSrcAttrs, bool bAllColumn)
        {
		    // Invaild parameters.
            if (srcAttr == null && item == null || libLowerClomuns == null || siteColumns == null || defineColumns == null || destSrcAttrs == null)
            {
                return;
            }
            SPWeb parWeb = item.ParentList.ParentWeb;
            SPWeb portalWeb = parWeb.Site.RootWeb;
            Dictionary<string, string> allSPEColumns = new Dictionary<string, string>();
            Dictionary<string, string> allSysColumns = new Dictionary<string, string>();

            foreach (string key in sysColumns)
            {
                allSysColumns[key.ToLower()] = key;
            }
            if (bAllColumn || libLowerClomuns.Count > 0)
            {
                foreach (SPField field in item.Fields)
                {
                    if (CheckTaggingField(field))
                    {
                        string key = field.Title;
                        if (!bAllColumn)
                        {
                            if (libLowerClomuns.Contains(key.ToLower()))
                            {
                                //fix bug 59800,after add prefilter,we need add tag to destSrcAttrs.
                                //reason:in 8.3 we query all resource attr in srcAttr,now we only care selected attrs in cc.
                                //so in line 2335,we wont get column tag and value.we need add these in here.
                                SetDestSrcAttrsFromField(item,field,key,allSPEColumns,destSrcAttrs);
                            }
                        }
                        else
                        {
                            allSPEColumns[key.ToLower()] = key;
                            SetDestSrcAttrsFromField(item, field, key, allSPEColumns, destSrcAttrs);
                        }
                    }
                }
            }

            if (portalSiteColumns.Count > 0)
            {
                foreach (string key in portalSiteColumns)
                {
                    if (!allSPEColumns.ContainsKey(key.ToLower()))
                    {
                        string value = GetSiteProperty(portalWeb, key);
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            SetDestSrcAttrs(destSrcAttrs, key, value);
                        }
                    }
                }
            }

            if (siteColumns.Count > 0)
            {
                foreach (string key in siteColumns)
                {
                    if (!allSPEColumns.ContainsKey(key.ToLower()))
                    {
                        string value = GetSiteProperty(parWeb, key);
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            SetDestSrcAttrs(destSrcAttrs, key, value);
                        }
                    }
                }
            }

            for (int i = 0; i < srcAttr.Length; i += 2)
            {
                string lowerKey = srcAttr[i].ToLower();
                if (allSysColumns.ContainsKey(lowerKey))
                {
                    SetDestSrcAttrs(destSrcAttrs, allSysColumns[lowerKey], srcAttr[i + 1]);
                }
                else if (allSPEColumns.ContainsKey(lowerKey))
                {
                    SetDestSrcAttrs(destSrcAttrs, allSPEColumns[lowerKey], srcAttr[i + 1]);
                }
            }
            if (defineColumns.Count > 0)
            {
                foreach (string key in defineColumns.Keys)
                {
                    SetDestSrcAttrs(destSrcAttrs, key, defineColumns[key]);
                }
            }
        }

        static private void SetDestSrcAttrs(Dictionary<string, KeyValuePair<string, string>> destSrcAttrs, string key, string value)
        {
            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(key, value);
            destSrcAttrs[key.ToLower()] = keyValue;
        }

        static public void EnableListProperty(SPList list, string _property)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties[_property] = "enable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch
            {
            }
        }

        static public void DisableListProperty(SPList list, string _property)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties[_property] = "disable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch
            {
            }
        }

        static public string GetGlobalSetOfProcessUpload(SPList list)
        {
            string strGlobalSet = strGlobalProcessUploadPropValueNone;

            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        string strProp = list.ParentWeb.Site.WebApplication.Properties[strGlobalProcessUploadPropName] as string;
                        if (strProp != null)
                        {
                            strGlobalSet = strProp;
                        }
                    });
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetGlobalSetOfProcessUpload:", null, ex);
            }
            return strGlobalSet;
        }
        static public string GetCurrentUser(System.Security.Principal.IIdentity userIdentity)
        {
            string userCurrentName = string.Empty;
            string userLoginName = SPClaimProviderManager.Local.GetUserIdentifierEncodedClaim(userIdentity);
            SPClaimProviderManager mgr = SPClaimProviderManager.Local;
            if (mgr != null)
            {
                if (SPClaimProviderManager.IsEncodedClaim(userLoginName))
                {
                    SPClaim decodedLogin = mgr.DecodeClaim(userLoginName);
                    if (decodedLogin != null)
                    {
                        userCurrentName = mgr.DecodeClaim(userLoginName).Value;
                    }
                }
            }
            return userCurrentName;
        }
        //SPFarm.Local.CurrentUserIsAdministrator return false even login with administrator
        //user which is not farm administrator, get true use SPFarm.Local.CurrentUserIsAdministrator(true)
        //get SPUser from web.CurrentUser, but then login name is SHAREPOINT\system which is aacount of webapp pool, not QAPF1\administator, so need use SPClaimProviderManager
        static public bool IsFarmAdministrator(string loginName)
        {
            bool isFarmAdmin = false;
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                SPGroup adminGroup = SPAdministrationWebApplication.Local.Sites[0].AllWebs[0].SiteGroups["Farm Administrators"];
                foreach (SPUser user in adminGroup.Users)
                {
                    if (string.Compare(user.LoginName, loginName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        isFarmAdmin = true;
                        break;
                    }
                }
            });
            return isFarmAdmin;
        }
        static public bool CheckListProperty(SPList list, string _property)
        {
            bool bRet = false;


            if(_property.Equals(strLibraryProcessUploadPropName, StringComparison.OrdinalIgnoreCase))
            {
                string strGlobaSetOfProcessUpload = GetGlobalSetOfProcessUpload(list);
                if(!strGlobaSetOfProcessUpload.Equals(strGlobalProcessUploadPropValueNone,StringComparison.OrdinalIgnoreCase))
                {
                    return strGlobaSetOfProcessUpload.Equals(strGlobalProcessUploadPropValueEnable, StringComparison.OrdinalIgnoreCase);
                }
            }

            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        string status = list.RootFolder.Properties[_property] as string;
                        if (status != null)
                        {
                            bRet = status.Equals("enable");
                        }
                    });
                }
            }
            catch
            {
            }
            return bRet;
        }

        // Converting the Web/List to the Resource type in the Policy
        public static String ConvertSPType2PolicyType(String type)
        {
            switch (type)
            {
                case "Microsoft.SharePoint.SPWeb":
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;

                case "Microsoft.SharePoint.SPDocumentLibrary":
                case "Microsoft.SharePoint.SPList":
                default:
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            }
        }

        // Converting the SPBaseType to the Resource sub type in Policy
        public static String ConvertSPBaseType2PolicySubtype(String basetype)
        {
            switch (basetype)
            {
                case "DocumentLibrary":
                    return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;

                case "DiscussionBoard":
                case "GenericList":
                case "Issue":
                case "Survey":
                case "UnspecifiedBaseType":
                case "Unused":
                default:
                    return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
            }
        }

        private static void AddEvalTags(string fileUrl, string remoteUser, Dictionary<string, KeyValuePair<string, string>> tags)
        {
            DateTime nowTime = DateTime.Now;
            byte[] url = System.Text.Encoding.Unicode.GetBytes(fileUrl);
            int urlLen = url.Length;
            byte[] user = System.Text.Encoding.Unicode.GetBytes(remoteUser);
            int userLen = user.Length;
            bool bEnd = false;
            int i = tags.Count;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                foreach (string key in tags.Keys)
                {
                    i--;
                    if (i == 0)
                    {
                        bEnd = true;
                    }
                    KeyValuePair<string, string> keyValue = tags[key];
                    string tagKey = keyValue.Key;
                    string tagValue = keyValue.Value;
                    if (!string.IsNullOrEmpty(tagKey) && !string.IsNullOrEmpty(tagValue))
                    {
                        byte[] keyArr = System.Text.Encoding.Unicode.GetBytes(tagKey);
                        int keyLen = keyArr.Length;
                        byte[] valueArr = System.Text.Encoding.Unicode.GetBytes(tagValue);
                        int valueLen = valueArr.Length;
                        _TagProtector_AddTagParam(url, urlLen, user, urlLen, keyArr, keyLen, valueArr, valueLen, bEnd,
                                nowTime.Millisecond, nowTime.Second, nowTime.Minute, nowTime.Hour, nowTime.Day, nowTime.Month, nowTime.Year);
                    }
                }
            });
        }

        public static void EvalTagging(CETYPE.CEAction action,
                                String inputName,
                                String subType,
                                String srcName,
                                SPWeb web,
                                String[] srcAttr,
                                String[] enforcement_obligation)
        {
            const string resSignature = "Resource Signature";
            try
            {
                // George: Use policy key instead of index to parse obligations.
                NLLogger.OutputLog(LogLevel.Debug, "EvalTagging: Entering:source=" + inputName);

                if ((subType == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM || subType == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM)
                    && action == CETYPE.CEAction.Read && !inputName.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                {
                    // Care with appointed extensions.
                    int lastpost = inputName.LastIndexOf(".");
                    if (!string.IsNullOrEmpty(CE_TAG_Extensions) && lastpost != -1)
                    {
                        string extension = inputName.Substring(lastpost + 1);
                        if (CE_TAG_Extensions.IndexOf(extension, StringComparison.OrdinalIgnoreCase) == -1)
                            return;
                    }

                    // Parse obligations.
                    List<Obligation> obligations = new List<Obligation>();
                    ParseObligations(enforcement_obligation, obligations);

                    SPListItem item = null;
                    String fileUrl = null;

                    if (subType == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM)
                    {
                        item = web.GetListItem(inputName);
                        fileUrl = ConstructListUrl(web, item.ParentList) + "/" + item.Name; // to make the folder file working, use "listUrl + item.Name" to replace real url.
                    }
                    else if (subType == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM)
                    {
                        item = ParseItemFromAttachmentURL(web, inputName);
                        SPFile file = web.GetFile(inputName);
                        fileUrl = ConstructListUrl(web, item.ParentList) + "/" + file.Name;
                    }

                    // Do obligations.
                    if (obligations.Count > 0 && item != null)
                    {
                        Dictionary<string, string> allSrcAttrs = new Dictionary<string, string>();
                        Dictionary<string, string> UserDefineColumns = new Dictionary<string, string>();
                        List<string> UserLibColumns = new List<string>();
                        List<string> UserSiteColumns = new List<string>();
                        List<string> UserPortalSiteColumns = new List<string>();
                        bool bAllColumn = false;
                        bool bSysColumn = false;
                        List<string> sysColumns = new List<string>();
                        foreach (Obligation ob in obligations)
                        {
                            if (ob.Name.Equals("SPALLCOLTAGGING"))
                            {
                                bAllColumn = true;
                            }
                            else if (ob.Name.Equals("SPSYSCOLTAGGING"))
                            {
                                bSysColumn = true;
                            }
                            else if (ob.Name.Equals("SPSPECCOLTAGGING"))
                            {
                                string cloumnName = ob.GetAttribute("Column");
                                if (!sysColumns.Contains(cloumnName))
                                {
                                    sysColumns.Add(cloumnName);
                                }
                            }
                            else if (ob.Name.Equals("SPUSERSPECTAGGING"))
                            {
                                string tagName = ob.GetAttribute("Tag Name");
                                string tagValue = ob.GetAttribute("Tag Value");

                                if (tagValue.Equals("$SPLib.Column"))
                                {
                                    UserLibColumns.Add(tagName.ToLower());
                                }
                                else if (tagValue.Equals("$SPSite.Property"))
                                {
                                    UserSiteColumns.Add(tagName);
                                }
                                else if (tagValue.Equals("$SPPortalSite.Property"))
                                {
                                    UserPortalSiteColumns.Add(tagName);
                                }
                                else if (!string.IsNullOrEmpty(tagName))
                                {
                                    UserDefineColumns[tagName] = tagValue;
                                }
                            }
                        }
                        if (bSysColumn)
                        {
                            sysColumns.Add(resSignature);
                            sysColumns.Add("Title");
                            sysColumns.Add("Name");
                            sysColumns.Add("Created By");
                            sysColumns.Add("Modified By");
                            sysColumns.Add("Created");
                            sysColumns.Add("Modified");
                            sysColumns.Add("File size");
                            sysColumns.Add("Type");
                            sysColumns.Add("Sub_type");
                        }
                        // Get the dest tags, then add them to file.
                        Dictionary<string, KeyValuePair<string, string>> destTags = new Dictionary<string, KeyValuePair<string, string>>();
                        SPE_FilterSrcAttrs(srcAttr, item, sysColumns, UserLibColumns, UserSiteColumns, UserPortalSiteColumns, UserDefineColumns, destTags, bAllColumn);
                        if (sysColumns.Contains(resSignature)) // Converting the URL to Resource Signature.
                        {
                            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(resSignature, srcName);
                            destTags[resSignature.ToLower()] = keyValue;
                        }
                        if (string.IsNullOrEmpty(web.CurrentUser.Email))
                        {
                            AddEvalTags(fileUrl, web.CurrentUser.Name, destTags);
                        }
                        else
                        {
                            AddEvalTags(fileUrl, web.CurrentUser.Email, destTags);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during EvalTagging:", null, ex);
            }
        }

        static public CETYPE.CEResponse_t CheckFile(CETYPE.CEAction action,
                                                        string srcName,
                                                        string targetName,
                                                        ref string[] srcAttr,
                                                        ref string[] targetAttr,
                                                        string remoteAddr,
                                                        string loginName,
                                                        string sid,
                                                        ref string policyName,
                                                        ref string policyMessage,
                                                        string ModuleName,
                                                        CETYPE.CENoiseLevel_t NoiseLevel)
        {
            CETYPE.CEResponse_t enforcement_result = CETYPE.CEResponse_t.CEAllow;
            if (!Utilities.SPECommon_Isup())
			{
                bool _bresponse = Globals.GetPolicyDefaultBehavior();
                if (_bresponse == false)
                    enforcement_result = CETYPE.CEResponse_t.CEDeny;
                return enforcement_result;
			}
            IntPtr localConnectHandle;
            uint ipNumber = IPAddressToIPNumber(remoteAddr);
            CETYPE.CEUser user;
            string[] enforcement_obligation;
            CETYPE.CEResult_t call_result;
            CETYPE.CEApplication app = new CETYPE.CEApplication("SharePoint", null, null);
            if (string.IsNullOrEmpty(sid))
                sid = UserSid.GetUserSid(loginName);
            if (string.IsNullOrEmpty(sid))
                sid = loginName;
            if (srcName == null)
            {
                srcName = "";
            }

            if (targetName == null)
            {
                targetName = "";
            }

            srcName = srcName.ToLower();
            targetName = targetName.ToLower();

            // fix bug 8746 & 8783 by derek
            if (loginName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                || loginName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase)) // fix bug 8894 by derek
            {
                NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
            }

            bool oldConnectionExisted = (connectHandle != IntPtr.Zero);

            while (true)
            {
                // (The thread synchronization code here between calling
                // _Initialize and calling _Close doesn't really work.  Need to be
                // fixed later.)
                // Try to connect if it's not already connected.
                lock (typeof(Globals))
                {
                    if (connectHandle == IntPtr.Zero)
                    {
                        CETYPE.CEResult_t result;

                        // We are passing dummy strings instead of null's or empty
                        // strings for userName and userID.  This is to avoid a
                        // crash problem in cepdpman.exe.
                        //
                        // The crash happens when NL_getUserId() in the SDK gets an
                        // "Access is denied" error from OpenProcessToken(), then
                        // sends null's to cepdpman, which probably de-references
                        // the null's and crashes.  The "Access is denied" error
                        // happens when, after all the w3wp.exe processes have
                        // exited due to idle time, the first remote user to
                        // connect to IIS is "NT AUTHORITY\LOCAL SERVICE" instead
                        // of a real user.
                        //
                        // Passing dummy strings here ensures that NL_getUserId()
                        // is never called.
                        user = new CETYPE.CEUser("dummyName", "dummyId");
                        result = CESDKAPI.CECONN_Initialize(app, user, null,
                                                            out connectHandle,
                                                            connectTimeoutMs);

                        if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                        {
                            connectHandle = IntPtr.Zero;

                            NLLogger.OutputLog(LogLevel.Debug, "CheckFile: Can't connect to SDK! srcName:[{0}], loginName:[{1}], remoteAddr:[{2}]", new object[] { srcName, loginName, remoteAddr });

                            // Always ALLOW when error occurrs.
                            return CETYPE.CEResponse_t.CEAllow;
                        }
                    }

                    localConnectHandle = connectHandle;
                }

                // loginName is not used for matching, hence there is no need to
                // convert it to lower-case.  sid matching is not done as string
                // matching.  Hence it should not be converted to lower-case.
                user = new CETYPE.CEUser(loginName, sid);

                NLLogger.OutputLog(LogLevel.Debug, "CheckFile: loginName:[{0}], sid:[{1}], srcName:[{2}], remoteAddr:[{3}]", new object[] { loginName, sid, srcName, remoteAddr });

                int evalTimeoutMs = GetPolicyDefaultTimeout();

                call_result = CESDKAPI.CEEVALUATE_CheckFile(
                    localConnectHandle,
                    action, srcName, ref srcAttr, targetName, ref targetAttr, ipNumber,
                    user, app, true, NoiseLevel, out enforcement_obligation,
                    out enforcement_result, evalTimeoutMs);

                if (call_result == CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                {
                    // Get the policy message.  This is the ONLY delegated
                    // obligation we understand
                    if (enforcement_obligation.Length > 0)
                    {
                        // Dump the key/values into the hashtable for easy manipulation
                        Dictionary<string, string> obligations = new Dictionary<string, string>();

                        for (int i = 0; i < enforcement_obligation.Length; i += 2)
                        {
                            obligations.Add(enforcement_obligation[i], enforcement_obligation[i + 1]);
                        }

                        int obligation_count = 0;
                        try
                        {
                            string count = obligations[CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT];
                            obligation_count = int.Parse(count);
                        }
                        catch
                        {
                        }

                        for (int i = 0; i < obligation_count; i++)
                        {
                            // Make sure we get the notify delegated obligation, which is the only thing we understand
                            string namekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_NAME + ":" + (i + 1);
                            if (obligations.ContainsKey(namekey))
                            {
                                string name = obligations[namekey];
                                if (!name.Equals(CETYPE.CEAttrVal.CE_OBLIGATION_NOTIFY, StringComparison.OrdinalIgnoreCase))
                                    continue;
                            }

                            string policyNamekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_POLICY + ":" + (i + 1);
                            if (obligations.ContainsKey(policyNamekey))
                            {
                                policyName = obligations[policyNamekey];
                            }

                            string policyMessagekey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1);
                            if (obligations.ContainsKey(policyMessagekey))
                            {
                                policyMessage = obligations[policyMessagekey];
                            }
                        }
                        if (string.IsNullOrEmpty(policyMessage))
                        {
                            GetPolicyNameMessageByObligation(enforcement_obligation, ref policyName, ref policyMessage);
                        }
                    }

                    NLLogger.OutputLog(LogLevel.Debug, "CheckFile: enforcement_result:[{0}], srcName:[{1}], loginName:[{2}], remoteAddr:[{3}]", new object[] { enforcement_result, srcName, loginName, remoteAddr });
                    return enforcement_result;
                }
                else
                {
                    if (call_result == CETYPE.CEResult_t.CE_RESULT_CONN_FAILED)
                    {
                        lock (typeof(Globals))
                        {
                            if (connectHandle == localConnectHandle)
                            {
                                CESDKAPI.CECONN_Close(connectHandle,
                                                      connectTimeoutMs);
                                connectHandle = IntPtr.Zero;
                            }
                        }

                        if (oldConnectionExisted)
                        {
                            // An old connection existed but died.  It might be the
                            // case where the server has been stopped and then
                            // re-started, so we try to re-connect once.
                            oldConnectionExisted = false;
                        }
                        else
                        {
                            // The new connection gave us error upon calling Eval.
                            // Give up.
                            return CETYPE.CEResponse_t.CEAllow;
                        }
                    }
                    else
                    {
                        // Always ALLOW when some other error occurrs.
                        return CETYPE.CEResponse_t.CEAllow;
                    }
                }
            } /* while (true) */
        }

        static public CETYPE.CEResponse_t Custom_CallEval(CETYPE.CEAction action,
                                                 string srcName,
                                                 string targetName,
                                                 ref string[] srcAttr,
                                                 ref string[] targetAttr,
                                                 string remoteAddr,
                                                 string loginName,
                                                 string sid,
                                                 ref string policyName,
                                                 ref string policyMessage,
                                                 string before_url,
            //It is the real before url, it means no matter how it happens, it refer to it logic before url
                                                 string after_url,
            //It is the real before url, it means no matter how it happens, it refer to it logic before url
                                                 string ModuleName,
            //Pass the module name, we will divide the HttpModule and Event handler to different
            //Comparison,Added by William 20090224
                                                 CETYPE.CENoiseLevel_t NoiseLevel,
                                                 SPWeb web,
                                                 bool _SPE_LogObligation,
                                                 ref IntPtr localConnectHandle,
                                                 ref string[] enforcement_obligation,
												 IPrincipal Principaluser)
        {
            //Var defination
            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
            if (!Utilities.SPECommon_Isup())
            {
                bool _bresponse = Globals.GetPolicyDefaultBehavior();
                if (_bresponse == false)
                    response = CETYPE.CEResponse_t.CEDeny;
                return response;
            }
            //we must reord the original srcname
            String origin_srcName = srcName;
            Global_Utils _Global_Utils = new Global_Utils();
            SPWeb opWeb = null;
            try
            {
                //to fix bug 1100, this should be used to open a spsite object
                using (SPSite site = new SPSite(web.Url))
                {
                    opWeb = site.OpenWeb();
                }
                if (opWeb != null)
                {
                    web = opWeb;
                }

                // fix bug 8746 & 8783 by derek
                if (loginName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                    || loginName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase)) // fix bug 8894 by derek
                {
                    NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
                }
                //Added by William 20090420,to let FBA also have a sid to pass the evaluation
                if (string.IsNullOrEmpty(sid))
                {
                    sid = UserSid.GetUserSid(web, loginName);
                    if (String.IsNullOrEmpty(sid))
                        sid = loginName;
                }

                //added by chellee for the exception,when the srcName is NULL ;
                if (srcName == null)
                {
                    srcName = "";
                }

                _Global_Utils.SPE_AlternateUrlCheck(web, ref srcName, ref targetName);

                _Global_Utils.SPE_NoiseLevel_Detection(action, srcName, remoteAddr, loginName, sid, before_url, after_url, ref NoiseLevel, ModuleName, web);

                _Global_Utils.SPE_AttrCheck(ref srcAttr, ref srcAttr, targetName);
                if (g_JPCParams.bUseJavaPC)
                {
                    response = _Global_Utils.SPE_Evaluation_CloudAZ(action, remoteAddr, loginName, sid, NoiseLevel, srcName, origin_srcName,
                        ref srcAttr, targetName, ref targetAttr, ref policyName, ref policyMessage, web, _SPE_LogObligation, ref localConnectHandle, ref enforcement_obligation, Principaluser);
                }
                else
                {
                    response = _Global_Utils.SPE_Evaluation(action, remoteAddr, loginName, sid, NoiseLevel, srcName, origin_srcName,
                        ref srcAttr, targetName, ref targetAttr, ref policyName, ref policyMessage, web, _SPE_LogObligation, ref localConnectHandle, ref enforcement_obligation, Principaluser);
                }
            }
            catch
            {
            }
            finally
            {
                if (opWeb != null)
                {
                    opWeb.Dispose();
                }
            }
            return response;
        }

        public static SPSite GetValidSPSite(string requestUrl, SPUserToken UserToken, HttpContext context)
        {
            SPSite _site = null;
            try
            {
                // George: check the "requestUrl" is not null or empty before using it.
                if (!string.IsNullOrEmpty(requestUrl))
                {
                    if (UserToken != null)
                        _site = new SPSite(requestUrl, UserToken);
                    else
                        _site = new SPSite(requestUrl);
                    SPEEvalAttrs.Current().AddDisposeSite(_site);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetValidSPSite,RequestUrl:" + requestUrl, null, ex);
            }
            return _site;
        }

        public static SPSite GetValidSPSite(string requestUrl, HttpContext context)
        {
            SPSite _site = null;
            try
            {
                // George: check the "requestUrl" is not null or empty before using it.
                if (!string.IsNullOrEmpty(requestUrl))
                {
                    _site = new SPSite(requestUrl);
                    SPEEvalAttrs.Current().AddDisposeSite(_site);
                }
            }
            catch
            {
            }
            return _site;
        }

        public static SPSite GetValidSPSite(string requestUrl, HttpContext context, ref string expMsg)
        {
            SPSite _site = null;
            try
            {
                // George: check the "requestUrl" is not null or empty before using it.
                if (!string.IsNullOrEmpty(requestUrl))
                {
                    _site = new SPSite(requestUrl);
                    SPEEvalAttrs.Current().AddDisposeSite(_site);
                }
            }
            catch
            {
            }
            return _site;
        }


        public static string IPAddrToHostUrl(string url)
        {
            string ip = url;
            ip = ip.Replace("http://", "");
            int lastpos = ip.IndexOf("/");
            if (lastpos != -1)
            {
                ip = ip.Substring(0, lastpos);
            }
            if (IsValidIP(ip))
            {
                string hostname = TryRemoteAddressCache(ip);
                url = url.Replace(ip, hostname);
            }
            return url;

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

        static string TryRemoteAddressCache(string ip)
        {
            string _hostname = "";
            bool containkey = Globals.IPAddressMap.ContainsKey(ip);
            if (containkey)
            {
                IPADDRCache _IPADDRCache = Globals.IPAddressMap[ip];
                UInt64 current_time = ((UInt64)
                     ((DateTime.Now.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                      TotalMilliseconds));
                UInt64 time_minus = current_time - _IPADDRCache.time;
                if (time_minus < 60000)
                {
                    _hostname = _IPADDRCache._IPHostEntry.HostName;
                }
                else
                {
                    IPHostEntry _IPHostEntry = Dns.GetHostEntry(ip);
                    _IPADDRCache.time = ((UInt64)
                         ((DateTime.Now.ToUniversalTime() -
                           new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                          TotalMilliseconds));
                    _IPADDRCache._IPHostEntry = _IPHostEntry;
                    lock (syncIPAddressMapRoot)
                    {
                        Globals.IPAddressMap[ip] = _IPADDRCache;
                    }
                    _hostname = _IPHostEntry.HostName;
                }
            }
            else
            {
                IPHostEntry _IPHostEntry = Dns.GetHostEntry(ip);
                IPADDRCache _IPADDRCache;
                _IPADDRCache.time = ((UInt64)
                     ((DateTime.Now.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                      TotalMilliseconds));
                _IPADDRCache._IPHostEntry = _IPHostEntry;
                lock (syncIPAddressMapRoot)
                {
                    Globals.IPAddressMap[ip] = _IPADDRCache;
                }
                _hostname = _IPHostEntry.HostName;
            }
            return _hostname;
        }

        /*************************************************/
        private static string ConvertByteToStringSid(Byte[] sidBytes)
        {
            StringBuilder strSid = new StringBuilder();
            strSid.Append("S-");
            try
            {
                // Add SID revision.
                strSid.Append(sidBytes[0].ToString());
                // Next six bytes are SID authority value.
                if (sidBytes[6] != 0 || sidBytes[5] != 0)
                {
                    string strAuth = String.Format
                        ("0x{0:2x}{1:2x}{2:2x}{3:2x}{4:2x}{5:2x}",
                        (Int16)sidBytes[1],
                        (Int16)sidBytes[2],
                        (Int16)sidBytes[3],
                        (Int16)sidBytes[4],
                        (Int16)sidBytes[5],
                        (Int16)sidBytes[6]);
                    strSid.Append("-");
                    strSid.Append(strAuth);
                }
                else
                {
                    Int64 iVal = (Int32)(sidBytes[1]) +
                        (Int32)(sidBytes[2] << 8) +
                        (Int32)(sidBytes[3] << 16) +
                        (Int32)(sidBytes[4] << 24);
                    strSid.Append("-");
                    strSid.Append(iVal.ToString());
                }

                // Get sub authority count...
                int iSubCount = Convert.ToInt32(sidBytes[7]);
                int idxAuth = 0;
                for (int i = 0; i < iSubCount; i++)
                {
                    idxAuth = 8 + i * 4;
                    UInt32 iSubAuth = BitConverter.ToUInt32(sidBytes, idxAuth);
                    strSid.Append("-");
                    strSid.Append(iSubAuth.ToString());
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception catched for ConvertByteToString:", null, ex);
                return "";
            }
            return strSid.ToString();
        }

        static public string getADUserSid(string logonuser)
        {
            string sid = "";
            string name = "";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                if (HttpContext.Current.Request.LogonUserIdentity != null)
                {
                    //That is fba case
                    if (string.IsNullOrEmpty(HttpContext.Current.Request.LogonUserIdentity.AuthenticationType))
                    {
                        return sid;
                    }
                }
                if (HttpContext.Current.Request.LogonUserIdentity != null && HttpContext.Current.Request.LogonUserIdentity.User != null)
                {
                    sid = HttpContext.Current.Request.LogonUserIdentity.User.ToString();
                    name = HttpContext.Current.Request.LogonUserIdentity.Name;
                }
                if (!string.IsNullOrEmpty(sid) && logonuser.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return sid;
                }
            }
            sid = null;
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    string username = logonuser;
                    string domainName = String.Empty;
                    int beginpos = logonuser.IndexOf("\\");
                    if (beginpos != -1)
                    {
                        username = logonuser.Substring(beginpos + 1);
                        int pos = logonuser.LastIndexOf("|");
                        if (beginpos != -1)
                        {
                            domainName = logonuser.Substring(pos + 1, beginpos - pos - 1);
                        }
                    }
                    System.DirectoryServices.ActiveDirectory.Domain domain = null;
                    if (string.IsNullOrEmpty(domainName))
                    {
                        domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
                    }
                    else
                    {
                        try
                        {
                            System.DirectoryServices.ActiveDirectory.DirectoryContext domainContext =
                            new System.DirectoryServices.ActiveDirectory.DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType.Domain, domainName);
                            domain = System.DirectoryServices.ActiveDirectory.Domain.GetDomain(domainContext);
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "getADUserSid :get Domain from userName Exception:", null, ex);
                            domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
                        }
                    }
                    using (System.DirectoryServices.DirectoryEntry de = new System.DirectoryServices.DirectoryEntry(string.Format("LDAP://{0}", domain)))
                    {
                        using (System.DirectoryServices.DirectorySearcher search = new System.DirectoryServices.DirectorySearcher(de))
                        {
                            search.Filter = string.Format("(SAMAccountName={0})", username);
                            search.PropertiesToLoad.Add("objectSid");
                            System.DirectoryServices.SearchResult result = search.FindOne();
                            if (result != null)
                            {
                                foreach (Object propValue in result.Properties["objectSid"])
                                {
                                    SecurityIdentifier _sid = new SecurityIdentifier((Byte[])propValue, 0);
                                    sid = _sid.ToString();
                                }
                            }
                            else
                            {
                                if (username.Contains("|"))
                                {
                                    int iPos = username.LastIndexOf("|");
                                    string trimmedName = username.Substring(iPos + 1);
                                    search.Filter = string.Format("(SAMAccountName={0})", trimmedName);
                                    result = search.FindOne();
                                    if (result != null)
                                    {
                                        foreach (Object propValue in result.Properties["objectSid"])
                                        {
                                            SecurityIdentifier _sid = new SecurityIdentifier((Byte[])propValue, 0);
                                            sid = _sid.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during getADUserSid:", null, ex);
            }
            return sid;
        }

        static public string SPECommon_ActionConvert(CETYPE.CEAction action)
        {
            string strAction;
            bool success = dicActionMaping.TryGetValue(action,out strAction);
            if(success)
            {
                return strAction;
            }
            else
            {
                return "";
            }
        }

        //static public string SPECommon_ActionConvert(CETYPE.CEAction action)
        //{
        //    return SPECommon_ActionConvert((int)action);
        //}

        static public string SPECommon_ActionConvert(int action)
        {


            string[] action_table = {"OPEN","DELETE","MOVE","COPY","EDIT","RENAME","CHANGE_ATTRIBUTES",
                              "CHANGE_SECURITY","PRINT","PASTE","EMAIL","IM","EXPORT","IMPORT",
                              "CHECKIN","CHECKOUT","ATTACH","RUN","REPLY",
                              "FORWARD","NEW_EMAIL","AVDCALL","MEETING","PROC_TERMINATE",
                              "SHARE","RECORD","QUESTION","VOICE","VIDEO","JOIN", "VIEW", "UPLOAD"}; // "VIEW" means "Query" action for trimming.
            int index = action - 1;
            string _action = action_table[index];
            return _action;
        }

        static public string[] SPECommon_GetPropBag(SPWeb _web, string[] extraAttrs)
        {
            List<string> ls = new List<string>();
            Configuration _Claimconf = Globals.Claimconf;
            if (_Claimconf != null && _Claimconf.SPEConfiguration != null
                && _Claimconf.SPEConfiguration.PropertyBag != null && _Claimconf.SPEConfiguration.PropertyBag.Length > 0)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    try
                    {
                        //bear fix bug 24482
                        using (SPSite _site = new SPSite(_web.Url))
                        {
                            using (SPWeb _RootWeb = _site.OpenWeb())
                            {
                                for (int i = 0; i < _Claimconf.SPEConfiguration.PropertyBag.Length; i++)
                                {
                                    if (_Claimconf.SPEConfiguration.PropertyBag[i].disabled != true)
                                    {
                                        if (_Claimconf.SPEConfiguration.PropertyBag[i].level == SPEPropertyBagLevel.SiteCollection)
                                        {
                                            for (int j = 0; j < _Claimconf.SPEConfiguration.PropertyBag[i].Property.Length; j++)
                                            {
                                                if (_Claimconf.SPEConfiguration.PropertyBag[i].Property[j].disabled != true)
                                                {
                                                    string pfn = _Claimconf.SPEConfiguration.PropertyBag[i].Property[j].name;
                                                    string attributeName = _Claimconf.SPEConfiguration.PropertyBag[i].Property[j].attributename;
                                                    if (_RootWeb.Site.RootWeb.AllProperties.Contains(pfn))
                                                    {
                                                        string pfv = (string)_RootWeb.Site.RootWeb.AllProperties[pfn];
                                                        if (pfv == null)
                                                        {
                                                            continue;
                                                        }
                                                        ls = InsertPropertyIntoList(attributeName, pfn, pfv, ls);
                                                    }
                                                }
                                            }
                                        }
                                        else if (_Claimconf.SPEConfiguration.PropertyBag[i].level == SPEPropertyBagLevel.SubSite)
                                        {
                                            for (int j = 0; j < _Claimconf.SPEConfiguration.PropertyBag[i].Property.Length; j++)
                                            {
                                                if (_Claimconf.SPEConfiguration.PropertyBag[i].Property[j].disabled != true)
                                                {
                                                    string pfn = _Claimconf.SPEConfiguration.PropertyBag[i].Property[j].name;

                                                    string attributeName = _Claimconf.SPEConfiguration.PropertyBag[i].Property[j].attributename;
                                                    if (_RootWeb.AllProperties.Contains(pfn))
                                                    {
                                                        string pfv = (string)_RootWeb.AllProperties[pfn];
                                                        if (pfv == null)
                                                        {
                                                            continue;
                                                        }
                                                        ls = InsertPropertyIntoList(attributeName,pfn,pfv,ls);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during SPECommon_GetPropBag:", null, ex);
                    }
                });
            }
            if (ls.Count > 0)
            {
                for (int i = 0; i < extraAttrs.Length; i++)
                {
                    ls.Add(extraAttrs[i]);
                }
                return ls.ToArray();
            }
            else
                return extraAttrs;
        }

        static private List<string> InsertPropertyIntoList(string attributeName,string name,string strValue,List<string> ls)
        {
            if (attributeName != null)
            {
                if (SPEEvalAttrs.prefilterResList == null)
                {
                    ls.Add(attributeName);
                    ls.Add(strValue);
                }
                else if (SPEEvalAttrs.prefilterResList != null && SPEEvalAttrs.prefilterResList.Contains(attributeName.ToLower()))
                {
                    ls.Add(attributeName);
                    ls.Add(strValue);
                }
            }
            else if (name != null)
            {
                if (SPEEvalAttrs.prefilterResList == null)
                {
                    ls.Add(name);
                    ls.Add(strValue);
                }
                else if (SPEEvalAttrs.prefilterResList != null && SPEEvalAttrs.prefilterResList.Contains(name.ToLower()))
                {
                    ls.Add(name);
                    ls.Add(strValue);
                }
            }
            return ls;
        }

        static public string GetSidFromUserProfile(SPWeb web,string loginname)
        {
            String value=null;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                HttpContext _context = HttpContext.Current;
                UserProfile profile = null;

                int iPos=loginname.LastIndexOf("|");
                string trimedName = null;
                if (iPos >= 1)
                    trimedName = loginname.Substring(iPos + 1);
                try
                {
                    if (_context == null)
                    {
                        SPServiceContext serverContext = SPServiceContext.GetContext(web.Site);
                        UserProfileManager profileManager = new UserProfileManager(serverContext);

                        if(profileManager.UserExists(loginname))
                            profile = profileManager.GetUserProfile(loginname);
                        else
                        {
                            if (!string.IsNullOrEmpty(trimedName) && profileManager.UserExists(trimedName))
                            {
                                profile = profileManager.GetUserProfile(trimedName);
                            }
                        }
                    }
                    else
                    {
                        SPServiceContext serverContext = SPServiceContext.GetContext(_context);
                        UserProfileManager profileManager = new UserProfileManager(serverContext);
                        if (profileManager.UserExists(loginname))
                            profile = profileManager.GetUserProfile(loginname);
                        else
                        {
                            if (!string.IsNullOrEmpty(trimedName) && profileManager.UserExists(trimedName))
                            {
                                profile = profileManager.GetUserProfile(trimedName);
                            }
                        }
                    }
                    if (profile != null)
                    {
                        value = (String)profile["sid"].Value;
                    }

                }
                catch(Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, $"Exception during user:{loginname} ,GetSidFromUserProfile:", null, ex);
                }
            });
            return value;
        }

        static public string[] SPECommon_GetUserProfile(SPWeb web, string loginName, string[] extraAttrs)
        {
            Configuration claimConf = Common.Globals.Claimconf;
            List<string> userProfile = new List<string>();
            if (claimConf != null && claimConf.SPEConfiguration != null && claimConf.SPEConfiguration.UserAttribute != null
               && claimConf.SPEConfiguration.UserAttribute.UserProfile != null && claimConf.SPEConfiguration.UserAttribute.UserProfile.disabled != true
               && claimConf.SPEConfiguration.UserAttribute.UserProfile.Property.Length > 0)
            {
                var profileGroup = claimConf.SPEConfiguration.UserAttribute.UserProfile.Property;

                List<PrefilterMatchResult> userAttrs = new List<PrefilterMatchResult>();
                List<string> subList = SPEEvalAttrs.prefilterSubList;

                if (subList != null)
                {
                    // prefilter success condition
                    bool bExistedCache = false;
                    double timeout = double.Parse(claimConf.SPEConfiguration.UserAttribute.UserProfile.cachetimeout);
                    //get user profile from cache
                    GetUserProfileFromCache(loginName, timeout, userAttrs, subList, profileGroup, ref bExistedCache);
                    // get user profile real time
                    if (!bExistedCache)
                    {
                        GetUserProfileRealTime(loginName, web, userAttrs, subList, profileGroup);
                    }
                }
                else
                {
                    // prefilter failed condition
                    GetUserProfileRealTime(loginName, web, userAttrs, subList, profileGroup);
                }
                foreach (var userAttr in userAttrs)
                {
                    userProfile.Add(userAttr.retKey);
                    userProfile.Add(userAttr.retValue);
                }
            }
            if (userProfile.Count > 0)
            {
                for (int i = 0; i < extraAttrs.Length; i++)
                {
                    userProfile.Add(extraAttrs[i]);
                }
                return userProfile.ToArray();
            }
            else
            {
                return extraAttrs;
            }
        }
        private static UserProfile GetClaimsUserProfile(UserProfileManager profileManager, string userName)
        {
            if (profileManager == null || string.IsNullOrEmpty(userName))
                return null;
            UserProfile profile = null;
            var cases = new List<string>();
            cases.Add(userName);
            if (userName.StartsWith("0"))
            {
                //NOTE: i and c are the default prefix of sharepoint, they can be configed in web.config file
                //the input user name is start with 0, so it may be an claim user
                cases.Add(string.Format("{0}:{1}", "i", userName));
                cases.Add(string.Format("{0}:{1}", "c", userName));
            }
            foreach (var u in cases)
            {
                if (profileManager.UserExists(u))
                {
                    profile = profileManager.GetUserProfile(u);
                    break;
                }
            }

            return profile;
        }
        static public string[] SPECommon_GetUserAttr(IPrincipal principalUser,SPWeb web)
        {
            List<string> userClaims = new List<string>();
            if (principalUser == null)
            {
                if (HttpContext.Current != null && HttpContext.Current.User != null)
                {
                    principalUser = HttpContext.Current.User;
                }
            }
            if (principalUser == null)
            {
                return userClaims.ToArray();
            }
            Configuration claimConf = Common.Globals.Claimconf;
            if (claimConf != null && claimConf.SPEConfiguration != null
                && claimConf.SPEConfiguration.UserAttribute != null && claimConf.SPEConfiguration.UserAttribute.Claims != null
                && claimConf.SPEConfiguration.UserAttribute.Claims.disabled != true)
            {
                var claimGroup = claimConf.SPEConfiguration.UserAttribute.Claims.Claim;
                double timeout = double.Parse(claimConf.SPEConfiguration.UserAttribute.Claims.cachetimeout);
                var loginName = web.CurrentUser.LoginName;

                List<PrefilterMatchResult> userAttrs = new List<PrefilterMatchResult>();
                List<string> subList = SPEEvalAttrs.prefilterSubList;

                if (subList != null)
                {
                    // prefilter success condition
                    bool bExistedCache = false;
                    //gather user claim from cache
                    GetUserClaimFromCache(loginName, timeout, userAttrs, subList, claimGroup, ref bExistedCache);
                    //gather user claim real time
                    if (!bExistedCache)
                    {
                        GetUserClaimRealTime(loginName, principalUser, userAttrs, subList, claimGroup);
                    }
                }
                else
                {
                    // prefilter failed condition
                    GetUserClaimRealTime(loginName, principalUser, userAttrs, subList, claimGroup);
                }
                foreach (var userAttr in userAttrs)
                {
                    userClaims.Add(userAttr.retKey);
                    userClaims.Add(userAttr.retValue);
                }
            }
            return userClaims.ToArray();
        }

        static public CETYPE.CEResponse_t CallEval(CETYPE.CEAction action,
                                               string srcName,
                                               string targetName,
                                               ref string[] srcAttr,
                                               ref string[] targetAttr,
                                               string remoteAddr,
                                               string loginName,
                                               string sid,
                                               ref string policyName,
                                               ref string policyMessage,
                                               string before_url,
            //It is the real before url, it means no matter how it happens, it refer to it logic before url
                                               string after_url,
            //It is the real before url, it means no matter how it happens, it refer to it logic before url
                                               string ModuleName,
            //Pass the module name, we will divide the HttpModule and Event handler to different
            //Comparison,Added by William 20090224
                                               CETYPE.CENoiseLevel_t NoiseLevel,
                                               SPWeb web,
											   IPrincipal Principaluser)
        {
            //Var defination
            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
            if (!Utilities.SPECommon_Isup())
			{
                bool _bresponse = Globals.GetPolicyDefaultBehavior();
                if (_bresponse == false)
                    response = CETYPE.CEResponse_t.CEDeny;
                return response;
			}
            //we must reord the original srcname
            String origin_srcName = srcName;
            IntPtr localConnectHandle = IntPtr.Zero;
            Global_Utils _Global_Utils = new Global_Utils();
            SPWeb opWeb = null;
            //to fix bug 1100, this should be used to open a spsite object
            try
            {
                using (SPSite site = new SPSite(web.Url))
                {
                    opWeb = site.OpenWeb();
                }
                if (opWeb != null)
                {
                    web = opWeb;
                }
                string[] enforcement_obligation = { "" };
                // fix bug 8746 & 8783 by derek
                if (loginName.Equals("SHAREPOINT\\system", StringComparison.OrdinalIgnoreCase)
                    || loginName.Equals("NT AUTHORITY\\LOCAL SERVICE", StringComparison.OrdinalIgnoreCase)) // fix bug 8894 by derek
                {
                    NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_SYSTEM;
                }
                //Added by William 20090420,to let FBA also have a sid to pass the evaluation

                if (string.IsNullOrEmpty(sid))
                {
                    sid = UserSid.GetUserSid(web, loginName);
                    if (String.IsNullOrEmpty(sid))
                        sid = loginName;
                }
                else
                {
                    if (sid.Length < SidEvLength)
                    {
                        string tempSid = UserSid.GetUserSid(HttpContext.Current);
                        if (tempSid.Length > sid.Length)
                            sid = tempSid;
                    }
                }

                //added by chellee for the exception,when the srcName is NULL ;
                if (srcName == null)
                {
                    srcName = "";
                }
                _Global_Utils.SPE_AlternateUrlCheck(web, ref srcName, ref targetName);
                _Global_Utils.SPE_NoiseLevel_Detection(action, srcName, remoteAddr, loginName, sid, before_url, after_url, ref NoiseLevel, ModuleName, web);
                _Global_Utils.SPE_AttrCheck(ref srcAttr, ref srcAttr, targetName);


                if (g_JPCParams.bUseJavaPC)
                {
                    response = _Global_Utils.SPE_Evaluation_CloudAZ(action, remoteAddr, loginName, sid, NoiseLevel,
                                                    srcName, origin_srcName, ref srcAttr, targetName, ref targetAttr, ref policyName, ref policyMessage, web, true, ref localConnectHandle, ref enforcement_obligation, Principaluser);
                }
                else
                {
                    response = _Global_Utils.SPE_Evaluation(action, remoteAddr, loginName, sid, NoiseLevel,
                                                    srcName, origin_srcName, ref srcAttr, targetName, ref targetAttr, ref policyName, ref policyMessage, web, true, ref localConnectHandle, ref enforcement_obligation, Principaluser);

                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CallEval:", null, ex);
            }
            finally
            {
                if (opWeb != null)
                {
                    opWeb.Dispose();
                }
            }
            NLLogger.OutputLog(LogLevel.Info, "Query policy for action:[{0}] in [{1}] by user:[{2}] with result:[{3}]\n", new object[] { action, srcName, loginName, response });
            return response;
        }


        static public string[] CheckEvalAttributs(string[] attr)
        {
            if (attr != null && attr.Length > 0)
            {
                List<string> attrList = new List<string>();
                List<KeyValuePair<string, string>> keyValueList = new List<KeyValuePair<string, string>>();
                string key = null;
                string value = null;
                string[] splitValue = null;
                string[] split = { PreAuthorization.tagSplit };

                for (int i = 0; i < attr.Length; i = i + 2)
                {
                    key = attr[i];
                    value = attr[i + 1];
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    {
                        continue; // To empty the invalid attributes.
                    }
                    if (-1 != value.IndexOf(PreAuthorization.tagSplit))
                    {
                        splitValue = value.Split(split, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string subValue in splitValue)
                        {
                            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(key.ToLower(), subValue.ToLower());
                            if (!keyValueList.Contains(keyValue))
                            {
                                attrList.Add(key);
                                attrList.Add(subValue);
                                keyValueList.Add(keyValue);
                            }
                        }
                    }
                    else
                    {
                        KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(key.ToLower(), value.ToLower());
                        if (!keyValueList.Contains(keyValue))
                        {
                            attrList.Add(key);
                            attrList.Add(value);
                            keyValueList.Add(keyValue);
                        }
                    }
                }

                return attrList.ToArray();
            }
            else
            {
                return attr;
            }
        }

        static private object GetObjectByURLAndAttrs(SPWeb web, string url, string[] srcAttr, ref string type)
        {
            string itemRelativeUrl = null;
            object obj = null;
            for (int i = 0; i < srcAttr.Length; i = i + 2)
            {
                if (srcAttr[i].Equals("type", StringComparison.OrdinalIgnoreCase)
                    && srcAttr[i + 1].Equals(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE, StringComparison.OrdinalIgnoreCase))
                {
                    type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                    break;
                }
                else if (srcAttr[i].Equals("type", StringComparison.OrdinalIgnoreCase)
                    && srcAttr[i + 1].Equals(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET, StringComparison.OrdinalIgnoreCase))
                {
                    type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                    break;
                }
                else if (srcAttr[i].Equals("type", StringComparison.OrdinalIgnoreCase)
                    && srcAttr[i + 1].Equals(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM, StringComparison.OrdinalIgnoreCase))
                {
                    type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                }
                else if (srcAttr[i].Equals("Server Relative URL", StringComparison.OrdinalIgnoreCase))
                {
                    itemRelativeUrl = srcAttr[i + 1];
                }

                if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM && itemRelativeUrl != null)
                {
                    break;
                }
            }


            if (url.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                int end = 0;
                if (-1 != (end = url.IndexOf("/sitepages/home.aspx", StringComparison.OrdinalIgnoreCase)))
                {
                    url = url.Substring(0, end);
                }
                else
                {
                    end = url.LastIndexOf("/");
                    url = url.Substring(0, end);
                }
            }

            if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE)
            {
                // if creat subsite case, it will get parent web.
                SPWeb spweb = (SPWeb)Utilities.GetCachedSPContent(null, url, Utilities.SPUrlWeb);
				obj = spweb;
            }
            else if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET)
            {
                obj = Utilities.GetCachedSPContent(web, url, Utilities.SPUrlList);
            }
            else if (type == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM)
            {
                obj = Utilities.GetCachedSPContent(web, url, Utilities.SPUrlListItem);

                // fix bug 31314, for list item and attachment.
                if (obj == null && itemRelativeUrl != null)
                {
                    obj = Utilities.GetCachedSPContent(web, itemRelativeUrl, Utilities.SPUrlListItem);
                }
            }

            return obj;
        }

        static public void DoPreAuthorization(SPWeb web, string orgUrl, string srcName, string action,
            ref string[] userAttr, ref string[] srcAttr, ref string[] targetAttr)
        {
            PreAuthorization preAuthorization = PreAuthorization.GetInstance();
            if (!preAuthorization.CheckPluginExisted())
            {
                return;
            }
            NLLogger.OutputLog(LogLevel.Info, "Enter DoPreAuthorization srcName:" + srcName + ", action:" + action);

            string type = null;
            object spObject = GetObjectByURLAndAttrs(web, orgUrl, srcAttr, ref type);
            // new site/list/item, object is null, use url instead.
            if (spObject == null)
            {
                spObject = orgUrl;
            }
            object spUser = web.CurrentUser;
            if (type != null && spUser != null && spObject != null)
            {
                Dictionary<string, string> userPair = new Dictionary<string, string>();
                Dictionary<string, string> srcPair = new Dictionary<string, string>();
                Dictionary<string, string> dstPair = new Dictionary<string, string>();
                preAuthorization.GetAttributesAttributes(spUser, spObject, null, action, srcName, type, userPair, srcPair, dstPair);
                userAttr = preAuthorization.AssemblyAttributs(userAttr, userPair);
                srcAttr = preAuthorization.AssemblyAttributs(srcAttr, srcPair);
                targetAttr = preAuthorization.AssemblyAttributs(targetAttr, dstPair);
            }
        }

        static private string GetTypeForTrimObject(SPWeb web, ref object obj)
        {
            string type = null;
            if (obj is SPWeb)
            {
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
            }
            else if (obj is SPList)
            {
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            }
            else if (obj is SPListItem)
            {
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                obj = view.ParentList;
            }
            else if (obj is ListViewWebPart)
            {
                ListViewWebPart lvwp = obj as ListViewWebPart;
                SPList list = web.Lists[new Guid(lvwp.ListName)];
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                obj = list;
            }
            else if (obj is XsltListViewWebPart)
            {
                XsltListViewWebPart xsltlvWP = obj as XsltListViewWebPart;
                SPList list = web.Lists[new Guid(xsltlvWP.ListName)];
                type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                obj = list;
            }
            return type;
        }
        static public void DoPreAuthorizationForTrim(SPWeb web, string srcName, string action,
            ref string[] userAttr, ref string[] srcAttr, object trimObj)
        {
            PreAuthorization preAuthorization = PreAuthorization.GetInstance();
            if (!preAuthorization.CheckPluginExisted() || (!preAuthorization.IfNeedTrimmingPreAthuZ()) || trimObj == null)
            {
                return;
            }
            string type = GetTypeForTrimObject(web, ref trimObj);
            object spUser = web.CurrentUser;
            if (type != null && spUser != null && trimObj != null)
            {
                Dictionary<string, string> userPair = new Dictionary<string, string>();
                Dictionary<string, string> srcPair = new Dictionary<string, string>();
                Dictionary<string, string> dstPair = new Dictionary<string, string>();
                preAuthorization.GetAttributesAttributes(spUser, trimObj, null, action, srcName, type, userPair, srcPair, dstPair);
                userAttr = preAuthorization.AssemblyAttributs(userAttr, userPair);
                srcAttr = preAuthorization.AssemblyAttributs(srcAttr, srcPair);
            }
        }

        // add pre-auth for itemAdding, itemUpdating, attachmentAdding, event
        public static void DoPreAuthorizationForUpload(SPItemEventProperties spItemEventProperties, string localFilePath, string srcName, string action, ref List<KeyValuePair<string, string>> lsPrepareExtralAttributesRef)
        {
            try
            {
                PreAuthorization preAuthorization = PreAuthorization.GetInstance();
                if (preAuthorization.CheckPluginExisted())
                {
                    NLLogger.OutputLog(LogLevel.Info, "Enter DoPreAuthorization srcName:" + srcName + ", action:" + action);
                    string type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                    preAuthorization.GetFileAttributesDuringUpload(spItemEventProperties, localFilePath, type, action, srcName, ref lsPrepareExtralAttributesRef);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during do pre-authorization for uploading", null, ex);
            }
        }

        static public bool GetPreFilterEval(SPWeb web, object obj, CETYPE.CEAction action, string obName, List<Obligation> obList)
        {
            bool bAllow = true; //Default is allow, we will not deny anything.
            if(obj == null || string.IsNullOrEmpty(obName) || obList == null)
            {
                return bAllow;
            }
            string srcName = "";
            string[] srcAttr = null;
            int idRequest = -1;
            EvaluationMultiple mulEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(web, ref mulEval, action);

            // Use multiple evalution that can return "dont-care-acceptable" result.
            HttpContext Context = HttpContext.Current;
            string objUrl = "";
            // fix bug 37472, get correct url for list when "DefaultViewUrl" is null, empty or "/".
            if (obj is SPList)
            {
                SPList list = obj as SPList;
                if (string.IsNullOrEmpty(list.DefaultViewUrl) || list.DefaultViewUrl == "/")
                {
                    objUrl = web.Site.MakeFullUrl(list.RootFolder.ServerRelativeUrl);
                }
                else
                {
                    objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                }
            }
            else
            {
                objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
            }

            GetSrcNameAndSrcAttr(obj, objUrl, Context, ref srcName, ref srcAttr);
            int iCareOb = 1; // care the obligation
            mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest, iCareOb);
            bool bRun = mulEval.run(iCareOb);
            if (bRun)
            {
                PolicyResult policyResult = PolicyResult.Allow;
                bAllow = mulEval.GetEvalResult(idRequest, ref policyResult);
                if (policyResult == PolicyResult.DontCare)
                {
                    // "dont-care-acceptable" evaluation result, we will deny all.
                    bAllow = false;
                }
                if (bAllow)
                {
                    mulEval.GetObligations(idRequest, obList, obName);
                }
            }
            mulEval.ClearRequest();
            return bAllow;
        }

        // Do pre-filter for search trimming(KQL).
        static public void DoPreFilterForKql(HttpRequest Request, SPWeb web, CETYPE.CEAction action, ref string filterValue)
        {
            try
            {
                List<Obligation> obList = new List<Obligation>();
                bool bAllow = GetPreFilterEval(web, web, action, "SP_Security_Filter_Criteria", obList);
                if (!bAllow)
                {
                    // we will deny all when have deny policy or have not any allow policy.
                    filterValue = "guid=5BBBFD63-8F04-490B-AE7C-E59F69E8318D"; // make the search result is empty.
                    return;
                }

                // Get the SearchService to check managed properties.
                SPFarm localFarm = SPFarm.Local;
#if SP2013
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch15");
#elif SP2010
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch14");
#else
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch16");
#endif
                if (obList.Count == 0 || searchService == null)
                {
                    // Not exist matched obligation or not get search service, we will not do anything for pre-filter.
                    return;
                }

                // Convert all obligations to correct KQL condition.
                string obLinkOp = bSPTrimAllow ? " OR " : " AND ";
                string conditLinkOp = bSPTrimAllow ? " AND " : " OR ";
                List<string> obFilterList = new List<string>();
                bool bValidOb = false;
                foreach (Obligation ob in obList)
                {
                    bool bCheck = true;
                    List<KeyValuePair<string, string>> listAttrs = new List<KeyValuePair<string, string>>();
                    Dictionary<string, string> obAttrs = ob.Attributes;
                    listAttrs = obAttrs.ToList<KeyValuePair<string, string>>();
                    List<string> conditList = new List<string>();
                    for (int i = 0; i < listAttrs.Count - 2; i += 3)
                    {
                        string condition = "";
                        string column = listAttrs[i].Value;
                        string op = listAttrs[i + 1].Value;
                        string value = listAttrs[i + 2].Value;
                        bCheck = ConvertSearchCondition(searchService, column, op, value, ref condition);
                        if (bCheck)
                        {
                            if(!string.IsNullOrEmpty(condition))
                            {
                                conditList.Add(condition);
                            }
                        }
                        else
                        {
                            break; //this obligation is invalid, don't need check next condition.
                        }
                    }
                    if (bCheck)
                    {
                        bValidOb = true;
                        if (conditList.Count > 0)
                        {
                            // Combine all conditions in one obligation.
                            string obFilterStr = CombSearchConditions(conditList, conditLinkOp);
                            obFilterList.Add(obFilterStr);
                        }
                    }
                }

                if (bValidOb)
                {
                    // Combine all obligations conditions to filterValue.
                    if (obFilterList.Count > 0)
                    {
                        filterValue = CombSearchConditions(obFilterList, obLinkOp);
                    }
                }
                else
                {
                    filterValue = "guid=5BBBFD63-8F04-490B-AE7C-E59F69E8318D"; // make the search result is empty.
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DoPreFilterForKql:", null, ex);
            }
        }

        // Combine the all serach condition using "AND" or "OR" operator.
        static public string CombSearchConditions(List<string> conditions, string logicOp)
        {
            string finalXml = "";
            foreach (string condition in conditions)
            {
                if (!string.IsNullOrEmpty(condition))
                {
                    if (string.IsNullOrEmpty(finalXml))
                    {
                        finalXml = condition;
                    }
                    else
                    {
                        finalXml = finalXml + logicOp + condition;
                    }
                }
            }
            if (conditions.Count > 1)
            {
                finalXml = "(" + finalXml + ")";
            }
            return finalXml;
        }

        // check if the search properities contains the "fieldRef" or not.
        static private bool CheckSearchField(SearchService searchService, string fieldRef)
        {
            try
            {
                foreach (SearchServiceApplication ssa in searchService.SearchApplications)
                {
                    Schema schema = new Schema(ssa);
                    if (schema.AllManagedProperties.Contains(fieldRef))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        // Convert the obligation to KQL condition.
        static private bool ConvertSearchCondition(SearchService searchService, string column, string op, string value, ref string condition)
        {
            if (!string.IsNullOrEmpty(column) && string.IsNullOrEmpty(value))
            {
                return false; // if column name is not null and column value is null, this condition is invalid.
            }
            else if (searchService == null || string.IsNullOrEmpty(column) || string.IsNullOrEmpty(op) || string.IsNullOrEmpty(value))
            {
                return true; // this condition is ignored;
            }
            string valuesLink = bSPTrimAllow ? " AND " : " OR ";

            bool bFind = CheckSearchField(searchService, column);
            if (bFind)
            {
                string format = "";
                switch (op)
                {
                    case "Equal to":
                        {
                            format = bSPTrimAllow ? "{0}={1}" : "(NOT {0}={1})";
                            valuesLink = bSPTrimAllow ? " OR " : " AND ";
                        }
                        break;
                    case "Not Equal to":
                        format = bSPTrimAllow ? "(NOT {0}={1})" : "{0}={1}";
                        break;
                    case "Greater than":
                        format = bSPTrimAllow ? "{0}>{1}" : "{0}<={1}";
                        break;
                    case "Greater than or Equal to":
                        format = bSPTrimAllow ? "{0}>={1}" : "{0}<{1}";
                        break;
                    case "Less than":
                        format = bSPTrimAllow ? "{0}<{1}" : "{0}>={1}";
                        break;
                    case "Less than or Equal to":
                        format = bSPTrimAllow ? "{0}<={1}" : "{0}>{1}";
                        break;
                }
                string[] split = { ";" };
                string[] values = value.Split(split, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 1)
                {
                    condition = string.Format(format, column, value);
                }
                else
                {
                    foreach (string columnValue in values)
                    {
                        if (string.IsNullOrEmpty(condition))
                        {
                            condition = string.Format(format, column, columnValue);
                        }
                        else
                        {
                            condition = condition + valuesLink + string.Format(format, column, columnValue);
                        }
                    }
                    condition = "(" + condition + ")"; // multiple values, need use "()" to include.
                }
            }

            return bFind; // if bFind is false, this condition is invalid.
        }

        // Convert the condition to CAML condition.
        static private bool ConvertListTrimCondition(SPList list, string column, string op, string value, ref string condition)
        {
            if (!string.IsNullOrEmpty(column) && string.IsNullOrEmpty(value))
            {
                return false; // if column name is not null and column value is null, this condition is invalid.
            }
            else if (list == null || string.IsNullOrEmpty(column) || string.IsNullOrEmpty(op) || string.IsNullOrEmpty(value))
            {
                return true; // this condition is ignored;
            }
            SPFieldType type = SPFieldType.Text;
            string fieldRef = column;

            bool bFind = CheckListField(list, ref fieldRef, ref type);
            if (bFind)
            {
                string camlOp = "";
                switch (op)
                {
                    case "Equal to":
                        camlOp = bSPTrimAllow ? "Eq" : "Neq";
                        break;
                    case "Not Equal to":
                        camlOp = bSPTrimAllow ? "Neq" : "Eq";
                        break;
                    case "Greater than":
                        camlOp = bSPTrimAllow ? "Gt" : "Leq";
                        break;
                    case "Greater than or Equal to":
                        camlOp = bSPTrimAllow ? "Geq" : "Lt";
                        break;
                    case "Less than":
                        camlOp = bSPTrimAllow ? "Lt" : "Geq";
                        break;
                    case "Less than or Equal to":
                        camlOp = bSPTrimAllow ? "Leq" : "Gt";
                        break;
                }
                if (!string.IsNullOrEmpty(camlOp))
                {
                    string[] split = { ";" };
                    string[] values = value.Split(split, StringSplitOptions.RemoveEmptyEntries);
                    condition = ConvertFieldFiltersXml(fieldRef, type.ToString(), values.ToList<string>(), camlOp);
                }
            }

            return bFind; // if bFind is false, this obligation is invalid.
        }

        // check the list field is valid or not.
        static private bool CheckListField(SPList list, ref string fieldRef, ref SPFieldType type)
        {
            try
            {
                foreach (SPField field in list.Fields)
                {
                    // Fix bug 34082, Use the "Title" instead of "InternalName". "Title" is display name for user, "InternalName" is the first name when column is created.
                    if (field.Title.Equals(fieldRef, StringComparison.OrdinalIgnoreCase))
                    {
                        // A field with matching internal name is found.
                        fieldRef = field.InternalName;
                        type = field.Type;
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        // Do pre-filter for list trimming.
        static public string DoPreFilterObligation(SPList list, string userAddr, CETYPE.CEAction action)
        {
            string valueStr = "";
            List<Obligation> obList = new List<Obligation>();
            bool bAllow = GetPreFilterEval(list.ParentWeb, list, action, "SP_Security_Filter_Criteria", obList);
            if (!bAllow)
            {
                valueStr = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Text\">0</Value></Eq>";  // make the list trimming result is empty("ID=0").
                return valueStr;
            }
            if (obList.Count == 0)
            {
                return valueStr; // Not exist matched obligation, we will not do anything for pre-filter.
            }

            // Convert all obligations to correct CAML condition.
            List<string> conditXmls = new List<string>();
            string ObLink = bSPTrimAllow ? "Or" : "And";
            string conditLinkOp = bSPTrimAllow ? "And" : "Or";
            bool bValidOb = false;
            foreach (Obligation ob in obList)
            {
                List<string> conditions = new List<string>();
                List<KeyValuePair<string, string>> listAttrs = new List<KeyValuePair<string, string>>();
                Dictionary<string, string> obAttrs = ob.Attributes;
                listAttrs = obAttrs.ToList<KeyValuePair<string, string>>();
                bool bCheck = true;
                for (int i = 0; i < listAttrs.Count - 2; i += 3)
                {
                    string condition = "";

                    string column = listAttrs[i].Value;
                    string op = listAttrs[i + 1].Value;
                    string value = listAttrs[i + 2].Value;

                    bCheck = ConvertListTrimCondition(list, column, op, value, ref condition);
                    if (bCheck)
                    {
                        if (!string.IsNullOrEmpty(condition))
                        {
                            conditions.Add(condition);
                        }
                    }
                    else
                    {
                        break; // this obligation is invalid, don't need check next condition.
                    }
                }
                if (bCheck)
                {
                    bValidOb = true;
                    if (conditions.Count > 0)
                    {
                        string conditXml = CombCamlConditions(conditions, conditLinkOp);
                        conditXmls.Add(conditXml);
                    }
                }
            }
            if (bValidOb)
            {
                if (conditXmls.Count > 0)
                {
                    valueStr = CombCamlConditions(conditXmls, ObLink);
                }
            }
            else
            {
                valueStr = "<Eq><FieldRef Name=\"ID\" /><Value Type=\"Text\">0</Value></Eq>";  // make the list trimming result is empty("ID=0").
            }
            return valueStr;
        }

        // Combine the all CAML condition using "And" or "Or" operator.
        static public string CombCamlConditions(List<string> conditions, string logicOp)
        {
            string finalXml = "";
            string format = "<{0}>{1}{2}</{0}>";
            foreach (string condition in conditions)
            {
                if (!string.IsNullOrEmpty(condition))
                {
                    if (string.IsNullOrEmpty(finalXml))
                    {
                        finalXml = condition;
                    }
                    else
                    {
                        finalXml = string.Format(format, logicOp, finalXml, condition);
                    }
                }
            }
            return finalXml;
        }

        // Do pre-filter for list trimming using CAML.
        static public string DoPreFilterForCaml(SPList list, string userAddr, CETYPE.CEAction action, ref string queryStr)
        {
            string filterXml = "";
            if (list == null || string.IsNullOrEmpty(userAddr))
            {
                return filterXml;
            }
            try
            {
                filterXml = DoPreFilterObligation(list, userAddr, action);
                if (!string.IsNullOrEmpty(filterXml) && !queryStr.Contains(filterXml))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.InnerXml = "<Query>" + queryStr + "</Query>";
                    XmlNode node = xmlDoc.DocumentElement;
                    if (node["Where"] == null)
                    {
                        node.InnerXml += "<Where>" + filterXml + "</Where>";
                    }
                    else
                    {
                        XmlNode wheNode = node["Where"];
                        wheNode.InnerXml = "<And>" + wheNode.InnerXml + filterXml + "</And>";
                    }
                    queryStr = node.InnerXml;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DoPreFilterForCaml:", null, ex);
            }
            return filterXml;
        }

        // Use CAML to convert query string.
        static public void ConvertFieldFiltersXml(SPList list, XmlNode wheNode, string fieldRef, List<string> values, string operate)
        {
            if (list != null)
            {
                bool bFind = false;
                SPFieldType type = SPFieldType.Text;
                foreach (SPField field in list.Fields)
                {
                    if (field.InternalName.Equals(fieldRef, StringComparison.OrdinalIgnoreCase))
                    {
                        // A field with matching internal name is found.
                        fieldRef = field.InternalName;
                        type = field.Type;
                        bFind = true;
                        break;
                    }
                }
                if (!bFind)
                {
                    return;
                }
                ConvertFieldFiltersXml(wheNode, fieldRef, type.ToString(), values, operate);
            }
        }

        // Use CAML to convert query string.
        static public void ConvertFieldFiltersXml(XmlNode wheNode, string fieldRef, string fieldType, List<string> values, string operate)
        {
            string fieldXml = ConvertFieldFiltersXml(fieldRef, fieldType, values, operate);
            if (!string.IsNullOrEmpty(fieldXml))
            {
                string innerXml = wheNode.InnerXml;
                if (string.IsNullOrEmpty(innerXml))
                {
                    wheNode.InnerXml = fieldXml;
                }
                else if (!innerXml.Contains(fieldXml))
                {
                    wheNode.InnerXml = "<And>" + innerXml + fieldXml + "</And>";
                }
            }
        }

        // Use CAML to convert query string.
        static public string ConvertFieldFiltersXml(string fieldRef, string fieldType, List<string> values, string operate)
        {
            if (string.IsNullOrEmpty(fieldRef) || string.IsNullOrEmpty(fieldType) || values == null || values.Count < 1 || string.IsNullOrEmpty(operate))
            {
                return "";
            }
            string format = "";
            string valuesStr = "";
            string linkOp = "And";
            switch (operate)
            {
                case "Eq":
                    {
                        if (values.Count > 1)
                        {
                            operate = "In";
                        }
                        linkOp = "Or";
                    }
                    break;
                default:
                    break;
            }
            string fieldXml = "";
            bool bExistEmpty = false;
            if (values.Contains(""))
            {
                // If the values include empty string, we need use "IsNull" to solve it.
                bExistEmpty = true;
                values.Remove("");
            }

            // Use correct oprator to combine correct CAML XML.
            if (operate.Equals("In"))
            {
                format = "<{0}><FieldRef Name=\"{1}\" /><Values>{2}</Values></{0}>";
                string valueFormat = "<Value Type=\"{0}\">{1}</Value>";
                foreach (string value in values)
                {
                    valuesStr += string.Format(valueFormat, fieldType, value);
                }
                fieldXml = string.Format(format, operate, fieldRef, valuesStr);
            }
            else
            {
                format = "<{0}><FieldRef Name=\"{1}\" /><Value Type=\"{2}\">{3}</Value></{0}>";
                string combFormat = "<{0}>{1}{2}</{0}>";
                string value = "";
                string xml = "";
                for (int i = 0; i < values.Count; i++)
                {
                    value = values[i];
                    xml = String.Format(format, operate, fieldRef, fieldType, value);
                    if (string.IsNullOrEmpty(fieldXml))
                    {
                        fieldXml = xml;
                    }
                    else
                    {
                        fieldXml = string.Format(combFormat, linkOp, fieldXml, xml);
                    }
                }
            }

            // Add empty value to CAML XML.
            if (bExistEmpty)
            {
                string emptyOp = linkOp.Equals("Or") ? "IsNull" : "IsNotNull";
                string emptyFormat = "<{0}><FieldRef Name=\"{1}\" /></{0}>";
                string emptyXml = string.Format(emptyFormat, emptyOp, fieldRef); ;
                if (string.IsNullOrEmpty(fieldXml))
                {
                    fieldXml = emptyXml;
                }
                else
                {
                    format = "<{0}>{1}{2}</{0}>";
                    fieldXml = string.Format(format, linkOp, emptyXml, fieldXml);
                }
            }
            return fieldXml;
        }

        // Get srcName and srcAttrublutes for trimming .
        public static void GetSrcNameAndSrcAttr(object obj, string url, HttpContext context, ref string srcName, ref string[] srcAttr)
        {
            if (context != null)
            {
                try
                {
                    SPWeb web = SPControl.GetContextWeb(context);
                    string remoteAddress = context.Request.UserHostAddress;
                    GetSrcNameAndSrcAttr(web, obj, url, remoteAddress, ref srcName, ref srcAttr);
                }
                catch
                {
                }
            }
        }

        public static bool GetAdministrationWebApplication(ref SPWebApplication WebApplication)
        {
            bool bRet = false;
            try
            {
                foreach (SPService service in SPFarm.Local.Services)
                {
                    if (service is SPWebService)
                    {
                        SPWebService webService = (SPWebService)service;
                        foreach (SPWebApplication webapp in webService.WebApplications)
                        {
                            if (webapp.IsAdministrationWebApplication)
                            {
                                WebApplication = webapp;
                                bRet = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetAdministrationWebApplication:", null, ex);
            }

            return bRet;
        }


        // Get srcName and srcAttrublutes for trimming .
        public static void GetSrcNameAndSrcAttr(SPWeb web, object obj, string url, string remoteAddress, ref string srcName, ref string[] srcAttr)
        {
            GetSrcNameAndSrcAttr(web, obj, url, remoteAddress, ref srcName, ref srcAttr, CETYPE.CEAction.Read);
        }

        public static void GetSrcNameAndSrcAttr(SPWeb web, object obj, string url, string remoteAddress, ref string srcName, ref string[] srcAttr, CETYPE.CEAction userAction)
        {
            string itemUrl = url;

            EvaluatorProperties evaProperties = new EvaluatorProperties();
            List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();

            // get different type item source attributes.
            if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                evaProperties.ConstructForItem(item, ref attributes);
            }
            else if (obj is SPList)
            {
                SPList list = obj as SPList;
                evaProperties.ConstructForList(list, ref attributes);
            }
            else if (obj is SPWeb)
            {
                SPWeb webType = obj as SPWeb;
                evaProperties.ConstructForWeb(webType, ref attributes);
            }
            else if (obj is System.Web.UI.WebControls.WebParts.WebPart)
            {
                System.Web.UI.WebControls.WebParts.WebPart webPart = obj as System.Web.UI.WebControls.WebParts.WebPart;
                evaProperties.ConstructForWebPart(webPart, web, ref attributes);
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                evaProperties.ConstructForList(view.ParentList, ref attributes);
            }

            // get different type item source name.
            if (obj is System.Web.UI.WebControls.WebParts.WebPart)
            {
                System.Web.UI.WebControls.WebParts.WebPart webPart = obj as System.Web.UI.WebControls.WebParts.WebPart;
                SPWebPartEvaluation evaObj = new SPWebPartEvaluation(webPart, web, userAction,
                    itemUrl, remoteAddress, "Web Part Trimmer", web.CurrentUser);
                itemUrl = evaObj.ReConstructUrl();
            }
            else
            {
                EvaluationBase evaObj = EvaluationFactory.CreateInstance(obj, userAction, itemUrl, remoteAddress, "Trimmer", web.CurrentUser);
                itemUrl = evaObj.ReConstructUrl();
            }

            srcName = itemUrl;
            //Add a page type attribute
            if (itemUrl.IndexOf("/_layouts", StringComparison.OrdinalIgnoreCase) > 0)
            {
                KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                    EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_APPLICATION);
                attributes.Add(keyVaule);
            }
            else
            {
                KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                    EvaluationBase.SP_PAGE_TYPE, EvaluationBase.SP_PAGE_TYPE_NORMAL);
                attributes.Add(keyVaule);
            }

            Evaluator evaluator = new Evaluator();
            string[] arrAttr = null;
            evaluator.ConvertPropertyListToArray(ref attributes, ref arrAttr);
            srcAttr = Globals.SPECommon_GetPropBag(web, arrAttr);
        }

        static public String GetDenyPageHtml(String httpserver, String backurl, String message)
        {
            String StatusDescription = "";
            {
                StatusDescription = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\"\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">"
                + "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" lang=\"en-us\" dir=\"ltr\">"
                + "<head><meta name=\"GENERATOR\" content=\"Microsoft SharePoint\" /><meta name=\"progid\" content=\"SharePoint.WebPartPage.Document\" /><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /><meta http-equiv=\"Expires\" content=\"0\" /><meta name=\"ROBOTS\" content=\"NOHTMLINDEX\" /><title>"
                + "Access Denied"
                + "</title><link rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/1033/styles/Themable/corev4.css?rev=iIikGkMuXBs8CWzKDAyjsQ%3D%3D\"/>"
                + "<script type=\"text/javascript\">"
                + "// <![CDATA["
                + "document.write('<script type=\"text/javascript\" src=\"/_layouts/1033/init.js?rev=BJDmyeIV5jS04CPkRq4Ldg%3D%3D\"></' + 'script>');"
                + "document.write('<script type=\"text/javascript\" src=\"/ScriptResource.axd?d=6H6ZQK1Kpi1e3Lzs7is0GqLlCY_0cfPFnWMovoG7fQKk7x6bcakfbkvX5ZdroGD-jUtuNdAe_0RaTLbOPUsrhe3GoX2TVUoxBTpCuQQssxc1&amp;t=ffffffffec2d9970\"></' + 'script>');"
                + "document.write('<script type=\"text/javascript\" src=\"/_layouts/blank.js?rev=QGOYAJlouiWgFRlhHVlMKA%3D%3D\"></' + 'script>');"
                + "document.write('<script type=\"text/javascript\" src=\"/ScriptResource.axd?d=6H6ZQK1Kpi1e3Lzs7is0GqLlCY_0cfPFnWMovoG7fQKk7x6bcakfbkvX5ZdroGD-Cb9TOwIMsHPH4DbFaSP9FvP3RD5wj6J_ElwuTB4JaFI1&amp;t=ffffffffec2d9970\"></' + 'script>');"
                + "// ]]>"
                + "</script>"
                + "<meta name=\"Robots\" content=\"NOINDEX \" />"
                + "<meta name=\"SharePointError\" content=\"0\" />"
                + "<link rel=\"shortcut icon\" href=\"/_layouts/images/favicon.ico\" type=\"image/vnd.microsoft.icon\" /></head>"
                + "<body onload=\"javascript:if (typeof(_spBodyOnLoadWrapper) != 'undefined') _spBodyOnLoadWrapper();\">"
                + "<form name=\"aspnetForm\" method=\"post\" action=\"error.aspx\" id=\"aspnetForm\" onsubmit=\"if (typeof(_spFormOnSubmitWrapper) != 'undefined') {return _spFormOnSubmitWrapper();} else {return true;}\">"
                + "<div>"
                + "<input type=\"hidden\" name=\"__EVENTTARGET\" id=\"__EVENTTARGET\" value=\"\" />"
                + "<input type=\"hidden\" name=\"__EVENTARGUMENT\" id=\"__EVENTARGUMENT\" value=\"\" />"
                + "<input type=\"hidden\" name=\"__VIEWSTATE\" id=\"__VIEWSTATE\" value=\"/wEPDwULLTEyODI3MDA2MDcPZBYCZg9kFgICAQ9kFgICAw9kFgQCCw9kFgQCBQ8PFgIeBFRleHQFNENvcnJlbGF0aW9uIElEOiAyZWM1MjFhMi00NzRlLTQ1N2QtYWQ3YS0wYTAzNTk4NTEyMWNkZAIGDw8WAh8ABSNEYXRlIGFuZCBUaW1lOiAxMi81LzIwMTAgNjoxOToxMCBQTWRkAg0PZBYCAgEPDxYCHghJbWFnZVVybAUhL19sYXlvdXRzLzEwMzMvaW1hZ2VzL2NhbHByZXYucG5nZGRkTofWlHSlILBFpZDAGaupJfxeYZ0=\" />"
                + "</div>"
                + "<script type=\"text/javascript\"> "
                + "//<![CDATA["
                + "var theForm = document.forms['aspnetForm'];"
                + "if (!theForm) {"
                + "theForm = document.aspnetForm;"
                + "}"
                + "function __doPostBack(eventTarget, eventArgument) {"
                + "if (!theForm.onsubmit || (theForm.onsubmit() != false)) {"
                + "theForm.__EVENTTARGET.value = eventTarget;"
                + "theForm.__EVENTARGUMENT.value = eventArgument;"
                + "theForm.submit();"
                + "}"
                + "}"
                + "//]]>"
                + "</script>"
                + "<script src=\"/WebResource.axd?d=SiEvSg2na9D88ERIo5WCxg2&amp;t=633802380069218315\" type=\"text/javascript\"></script>"
                + "<script type=\"text/javascript\">"
                + "//<![CDATA["
                + "var g_presenceEnabled = true;var _fV4UI=true;var _spPageContextInfo = {webServerRelativeUrl: \"\u002f\", webLanguage: 1033, currentLanguage: 1033, webUIVersion:4,userId:1, alertsEnabled:false, siteServerRelativeUrl: \"\u002f\", allowSilverlightPrompt:'True'};//]]>"
                + "</script>"
                + "<script src=\"/_layouts/blank.js?rev=QGOYAJlouiWgFRlhHVlMKA%3D%3D\" type=\"text/javascript\"></script>"
                + "<script type=\"text/javascript\">"
                + "//<![CDATA["
                + "if (typeof(DeferWebFormInitCallback) == 'function') DeferWebFormInitCallback();//]]>"
                + "</script>"
                + "<script type=\"text/javascript\"> "
                + "//<![CDATA["
                + "Sys.WebForms.PageRequestManager._initialize('ctl00$ScriptManager', document.getElementById('aspnetForm'));"
                + "Sys.WebForms.PageRequestManager.getInstance()._updateControls([], [], [], 90);"
                + "//]]>"
                + "</script>"
                + "<div id=\"s4-simple-header\" class=\"s4-pr\">"
                + "<div class=\"s4-lpi\">"
                + "<span style=\"height:17px;width:17px;position:relative;display:inline-block;overflow:hidden;\" class=\"s4-clust\"><a href=\"#\" id=\"ctl00_PlaceHolderHelpButton_TopHelpLink\" style=\"height:17px;width:17px;display:inline-block;\" onclick=\"TopHelpButtonClick('NavBarHelpHome');return false\" accesskey=\"6\" title=\"Help (new window)\"><img src=\"/_layouts/images/fgimg.png\" style=\"left:-0px !important;top:-309px !important;position:absolute;\" align=\"absmiddle\" border=\"0\" alt=\"Help (new window)\" /></a></span>"
                + "</div>"
                + "</div>"
                + "<div id=\"s4-simple-card\">"
                + "<div id=\"s4-simple-card-top\">"
                + "</div>"
                + "<div id=\"s4-simple-card-content\">"
                + "<div class=\"s4-simple-iconcont\">"
                + "<img src=\"/_layouts/images/warning32by32.gif\" alt=\"Warn\" />"
                + "</div>"
                + "<div id=\"s4-simple-content\">"
                + "<h1>"
                + "<span id=\"errorPageTitleSpan\" tabindex=\"0\">Access Denied</span>"
                + "</h1>"
                + "<div id=\"s4-simple-error-content\">"
                + "<span id=\"ctl00_PlaceHolderMain_LabelMessage\">";
                StatusDescription += message;
                StatusDescription += "</span>"
                + "<p>"
                + "<span class=\"ms-descriptiontext\">"
                + "</span>"
                + "</p>"
                + "<p>"
                + "<span class=\"ms-descriptiontext\">"
                + "<span id=\"ctl00_PlaceHolderMain_helptopic_WSSEndUser_troubleshooting\"><a title=\"Troubleshoot issues with Microsoft SharePoint Foundation. - Opens in new window\" href=\"javascript:HelpWindowKey('WSSEndUser_troubleshooting')\">Troubleshoot issues with Microsoft SharePoint Foundation.</a></span>"
                + "</span>"
                + "</p>"
                //+ "<p>"
                //+ "<span id=\"ctl00_PlaceHolderMain_RequestGuidText\">Correlation ID: 2ec521a2-474e-457d-ad7a-0a035985121c</span>"
                //+ "</p>"
                + "<p>"
                + "<span id=\"ctl00_PlaceHolderMain_DateTimeText\">Date and Time: ";
                StatusDescription += DateTime.Now.ToString();
                StatusDescription += "</span>"
                + "</p>"
                + "<script type=\"text/javascript\" language=\"JavaScript\">"
                + "// <![CDATA["
                + "function ULSvam(){var o=new Object;o.ULSTeamName=\"Microsoft SharePoint Foundation\";o.ULSFileName=\"error.aspx\";return o;}"
                + "var gearPage = document.getElementById('GearPage');"
                + "if(null != gearPage)"
                + "{"
                + "gearPage.parentNode.removeChild(gearPage);"
                + "document.title = \"Access Denied\";"
                + "}"
                + "function _spBodyOnLoad()"
                + "{ULSvam:;"
                + "var intialFocus = document.getElementById(\"errorPageTitleSpan\");"
                + "try"
                + "{"
                + "intialFocus.focus();"
                + "}"
                + "catch(ex)"
                + "{"
                + "}"
                + "}"
                + "// ]]>"
                + "</script>"
                + "</div>"
                + "<div id=\"s4-simple-gobackcont\">"
                + "<img src=\"/_layouts/1033/images/calprev.png\" alt=\"Go back to site\" style=\"border-width:0px;\" />"
                + "<a href=\"";
                StatusDescription += backurl;
                StatusDescription += "\" id=\"ctl00_PlaceHolderGoBackLink_idSimpleGoBackToHome\" target=\"_parent\">Go back to site</a>"
                + "</div>"
                + "</div>"
                + "</div>"
                + "</div>"
                + "<div class=\"s4-die\">"
                + "</div>"
                + "<script type=\"text/javascript\"> "
                + "// <![CDATA["
                + "// ]]>"
                + "</script>"
                + "<script type=\"text/javascript\">RegisterSod(\"sp.core.js\", \"\u002f_layouts\u002fsp.core.js?rev=7ByNlH\u00252BvcgRJg\u00252BRCctdC0w\u00253D\u00253D\");</script>"
                + "<script type=\"text/javascript\">RegisterSod(\"sp.res.resx\", \"\u002f_layouts\u002fScriptResx.ashx?culture=en\u00252Dus\u0026name=SP\u00252ERes\u0026rev=b6\u00252FcRx1a6orhAQ\u00252FcF\u00252B0ytQ\u00253D\u00253D\");</script>"
                + "<script type=\"text/javascript\">RegisterSod(\"sp.ui.dialog.js\", \"\u002f_layouts\u002fsp.ui.dialog.js?rev=IuXtJ2CrScK6oX4zOTTy\u00252BA\u00253D\u00253D\");RegisterSodDep(\"sp.ui.dialog.js\", \"sp.core.js\");RegisterSodDep(\"sp.ui.dialog.js\", \"sp.res.resx\");</script>"
                + "<script type=\"text/javascript\">RegisterSod(\"core.js\", \"\u002f_layouts\u002f1033\u002fcore.js?rev=c3ROI4x\u00252BKHVTMbn4JuFndQ\u00253D\u00253D\");</script>"
                + "<script type=\"text/javascript\"> "
                + "//<![CDATA["
                + "Sys.Application.initialize();"
                + "//]]>"
                + "</script>"
                + "</form>"
                + "</body>"
                + "</html>";
            }
            return StatusDescription;
        }

        public static string GetSiteProperty(SPSite site,string propName)
        {
            return GetSiteProperty(site.RootWeb, propName);
        }

        public static void SetSiteProperty(SPSite site, string sitePropName, string sitePropValue)
        {
            SetSiteProperty(site.RootWeb, sitePropName, sitePropValue);
        }

        public static string GetSiteProperty(SPWeb web, string propName)
        {
            string strPropValue = null;
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    strPropValue = web.GetProperty(propName) as string;
                });
            }
            catch
            {
            }
            return strPropValue;
        }

        public static void SetSiteProperty(SPWeb web, string propName, string propValue)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(web.Site.Url))
                    {
                        using (SPWeb setWeb = site.OpenWeb(web.ServerRelativeUrl))
                        {
                            bool oldUpdate = setWeb.AllowUnsafeUpdates;
                            setWeb.AllowUnsafeUpdates = true;
                            setWeb.SetProperty(propName, propValue);
                            setWeb.Update();
                            setWeb.AllowUnsafeUpdates = oldUpdate;
                        }
                    }
                });
            }
            catch
            {
            }
        }


        /*get the user with full control permission of site
       * return value: string
       * all the users' email, delimilited with ","
       */
        public static string GetFullControlUsersEmail(SPWeb web)
        {
            string strUsers = "";
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(web.Site.Url))
                    {
                        using (SPWeb curWeb = site.OpenWeb(web.ServerRelativeUrl))
                        {
                            HashSet<string> userEmailSet = new HashSet<string>();
                            SPRoleCollection spRoles = curWeb.Roles;
                            SPRole fullControlRole = null;
                            foreach (SPRole role in spRoles)
                            {
                                if (role.Name.Equals("full control", StringComparison.OrdinalIgnoreCase))
                                {
                                    fullControlRole = role;
                                    break;
                                }
                            }

                            if (fullControlRole != null)
                            {
                                SPUserCollection fullControlUsers = fullControlRole.Users;
                                foreach (SPUser user in fullControlUsers)
                                {
                                    if (!String.IsNullOrEmpty(user.Email))
                                    {
                                        userEmailSet.Add(user.Email);
                                    }
                                }

                                SPGroupCollection fullControlGroups = fullControlRole.Groups;
                                foreach (SPGroup group in fullControlGroups)
                                {
                                    SPUserCollection usersInFullControlGroup = group.Users;

                                    foreach (SPUser user in usersInFullControlGroup)
                                    {
                                        if (!String.IsNullOrEmpty(user.Email))
                                        {
                                            userEmailSet.Add(user.Email);
                                        }
                                    }
                                }
                            }
                            strUsers = String.Join(",", userEmailSet.ToArray());
                        }
                    }
                });
            }
            catch
            { }
            return strUsers;
        }


        public static string GetSiteCollectionAdminsEmail(SPWeb web)
        {
            string strUsers = "";
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(web.Site.Url))
                    {
                        using (SPWeb curWeb = site.OpenWeb(web.ServerRelativeUrl))
                        {
                            SPUserCollection users = curWeb.SiteAdministrators;
                            foreach (SPUser user in users)
                            {
                                if (!String.IsNullOrEmpty(user.Email))
                                {
                                    strUsers += user.Email + ",";
                                }
                            }
                            strUsers = strUsers.Substring(0,strUsers.Length-1);
                        }
                    }
                });
            }
            catch
            { }
            return strUsers;
        }


        /*for send email with parameters
         *
         *return value: bool
         * all the recipients that SPE want to send to. Multiple recipients should be delimilited with “,”.
         */
        public static void SPESendEmail(SPWeb web, string strRecipients, string strSubject, string strBody, string attachmentPath = "")
        {
            try
            {
                if (web == null || string.IsNullOrEmpty(strRecipients))
                {
                    return;
                }
                strRecipients = strRecipients.Replace(";", ","); // Use "," to split the recipients.
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(web.Site.Url))
                    {
                        Microsoft.SharePoint.Administration.SPWebApplication webApp = site.WebApplication;
                        using (System.Net.Mail.SmtpClient client = new SmtpClient())
                        {
                            client.Host = webApp.OutboundMailServiceInstance.Server.Name;
                            client.UseDefaultCredentials = false; //setting.
                            client.EnableSsl = (webApp.OutboundMailEnableSsl == true);//setting
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                            string senderName = webApp.OutboundMailSenderAddress;
                            if (string.IsNullOrEmpty(attachmentPath))
                            {
                                client.Send(senderName, strRecipients, strSubject, strBody);
                            }
                            else
                            {
                                using (System.Net.Mail.MailMessage message = new MailMessage())
                                {
                                    message.From = new MailAddress(senderName);
                                    string[] arrayRecipients = strRecipients.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                    IEnumerable<string> distinctRecipients = arrayRecipients.Distinct();
                                    string strDistinctRecipients = string.Join(",", distinctRecipients.ToArray<string>());
                                    message.To.Add(strDistinctRecipients);
                                    message.Subject = strSubject;
                                    message.Body = strBody;
                                    message.BodyEncoding = System.Text.Encoding.GetEncoding(webApp.OutboundMailCodePage);
                                    message.IsBodyHtml = true;
                                    if (!String.IsNullOrEmpty(attachmentPath))
                                    {
                                        Attachment mailAttach = new Attachment(attachmentPath);
                                        System.Net.Mime.ContentDisposition cd = mailAttach.ContentDisposition;
                                        message.Attachments.Add(mailAttach);
                                    }
                                    client.Send(message);
                                }
                            }
                        }
                    }
                });
            }
            catch(Exception ex)
            {
                // Log the exception except "Unable to relay".
                if (-1 == ex.Message.IndexOf("Unable to relay", StringComparison.OrdinalIgnoreCase))
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during SPESendEmail:", null, ex);
                }
            }
        }

        public enum SupFileType
        {
            WORD2003,
            WORD2007,
            EXCEL2003,
            EXCEL2007,
            POWERPOINT2003,
            POWERPOINT2007,
            PDF,
            OTHER
        }

        public static bool IsSupportFileType(SupFileType emFileType)
        {
            return (SupFileType.OTHER != emFileType);
        }
        public static bool IsSupportFileType(string strFileName)
        {
            SupFileType emFileType = GetFileType(strFileName);
            return IsSupportFileType(emFileType);
        }

        public static SupFileType GetFileType(string strFileName)
        {
            string[] wordSuffixs = { "doc", "dot" };
            string[] wordxSuffixs = { "docx", "docm", "dotx", "dotm" };
            string[] excelSuffixs = { "xls", "xla", "xlt", "xlsb" };
            string[] excelxSuffixs = { "xlsx", "xlam", "xltm", "xltx", "xlsm" };
            string[] pptxSuffixs = { "pptx", "potm", "ppsx", "ppsm", "potx", "ppam", "pptm" };
            string[] pptSuffixs = { "ppt", "pot", "pps", "ppa" };
            string[] pdfSuffixs = { "pdf" };

            //get file suffix
            string strSuffix = GetFileSuffix(strFileName);

            if (wordSuffixs.Contains(strSuffix))
            {
                return SupFileType.WORD2003;
            }
            else if (wordxSuffixs.Contains(strSuffix))
            {
                return SupFileType.WORD2007;
            }
            else if (excelSuffixs.Contains(strSuffix))
            {
                return SupFileType.EXCEL2003;
            }
            else if (excelxSuffixs.Contains(strSuffix))
            {
                return SupFileType.EXCEL2007;
            }
            else if (pptSuffixs.Contains(strSuffix))
            {
                return SupFileType.POWERPOINT2003;
            }
            else if (pptxSuffixs.Contains(strSuffix))
            {
                return SupFileType.POWERPOINT2007;
            }
            else if (pdfSuffixs.Contains(strSuffix))
            {
                return SupFileType.PDF;
            }
            else
            {
                return SupFileType.OTHER;
            }
        }

        public static string GetFileSuffix(string strFileName)
        {
            string strSuffix = "";
            int nPos = strFileName.LastIndexOf('.');
            if (nPos >= 0)
            {
                strSuffix = strFileName.Substring(nPos + 1);
            }
            return strSuffix.ToLower();
        }

        // Get Object From "Identity".
        public static object GetObjectFromIdentity(string identity)
        {
            Object destObj = null;
            try
            {
                SPSite site = SPControl.GetContextSite(HttpContext.Current);
                string webGuid = GetGuidOrIdFromIndentityName(identity, "web:");
                if (!string.IsNullOrEmpty(webGuid))
                {
                    SPWeb web = site.OpenWeb(new Guid(webGuid));
                    SPEEvalAttrs.Current().AddDisposeWeb(web);
                    SPList list = null;
                    SPFolder folder = null;
                    SPFile file = null;
                    SPFileVersion fileVersion = null;
                    SPListItem item = null;
                    SPField field = null;
                    SPView view = null;

                    string listGuid = GetGuidOrIdFromIndentityName(identity, "list:");
                    if (!string.IsNullOrEmpty(listGuid))
                    {
                        list = web.Lists[new Guid(listGuid)];
                    }

                    string folderGuid = GetGuidOrIdFromIndentityName(identity, "folder:");
                    if (!string.IsNullOrEmpty(folderGuid))
                    {
                        folder = web.GetFolder(new Guid(folderGuid));
                    }

                    string fileUrl = GetGuidOrIdFromIndentityName(identity, "file:");
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        file = web.GetFile(fileUrl);
                    }

                    string versionId = GetGuidOrIdFromIndentityName(identity, "fi:");
                    if (!string.IsNullOrEmpty(versionId) && file != null)
                    {
                        fileVersion = file.Versions.GetVersionFromID(int.Parse(versionId));
                    }

                    string itemId = GetGuidOrIdFromIndentityName(identity, "item:"); // like "item:3,1"
                    if (!string.IsNullOrEmpty(itemId) && list != null)
                    {
                        int endInd = itemId.IndexOf(",");
                        if (-1 != endInd)
                        {
                            itemId = itemId.Substring(0, endInd);
                            item = list.GetItemById(int.Parse(itemId));
                        }
                    }

                    string fieldId = GetGuidOrIdFromIndentityName(identity, "field:");
                    if (!string.IsNullOrEmpty(fieldId) && list != null)
                    {
                        field = list.Fields[new Guid(fieldId)];
                    }

                    string viewId = GetGuidOrIdFromIndentityName(identity, "view:");
                    if (!string.IsNullOrEmpty(viewId) && list != null)
                    {
                        view = list.Views[new Guid(viewId)];
                    }

                    if (fileVersion != null)
                    {
                        destObj = fileVersion;
                    }
                    else if (file != null)
                    {
                        destObj = file;
                    }
                    else if (item != null)
                    {
                        destObj = item;
                    }
                    else if (field != null)
                    {
                        destObj = field;
                    }
                    else if (view != null)
                    {
                        destObj = view;
                    }
                    else if (folder != null)
                    {
                        destObj = folder;
                    }
                    else if (list != null)
                    {
                        destObj = list;
                    }
                    else if (web != null)
                    {
                        destObj = web;
                    }
                }
                else
                {
                    destObj = site;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, $"Exception during {identity} ,GetObjectFromIdentity:", null, ex);
            }
            return destObj;
        }

        private static string GetGuidOrIdFromIndentityName(string IdentityName, string objSymbel)
        {
            string destGuid = null;
            int beginInd = IdentityName.IndexOf(objSymbel);
            if (beginInd != -1)
            {
                int endInd = IdentityName.IndexOf(":", beginInd + objSymbel.Length);
                if (-1 != endInd)
                {
                    destGuid = IdentityName.Substring(beginInd + objSymbel.Length, endInd - beginInd - objSymbel.Length);
                }
                else
                {
                    destGuid = IdentityName.Substring(beginInd + objSymbel.Length);
                }
            }
            return destGuid;
        }
        public static string GetFileName(string strFileFullPath)
        {
            string strFileNameRet = "";
            if (!String.IsNullOrEmpty(strFileFullPath))
			{
                strFileNameRet = Path.GetFileName(strFileFullPath);
            }
            return strFileNameRet;
        }
        public static SPListItem GetSPListItemFromSPFile(SPWeb web, SPFile file)
        {
            if (file == null)
            {
                return null;
            }
            if (web == null)
            {
                web = file.Web;
            }
            SPListItem item = null;
            try
            {
                item = file.Item;
            }
            catch
            { }

            if (item == null)
            {
                try
                {
                    string fullUrl = web.Site.MakeFullUrl(file.ServerRelativeUrl);
                    using (SPSite site = new SPSite(fullUrl))
                    {
                        SPWeb destWeb = site.OpenWeb();
                        if (-1 != fullUrl.IndexOf("Attachments", StringComparison.OrdinalIgnoreCase))
                        {
                            item = ParseItemFromAttachmentURL(destWeb, fullUrl);
                        }
                        else
                        {
                            item = destWeb.GetListItem(fullUrl);
                        }
                        SPEEvalAttrs.Current().AddDisposeWeb(destWeb);
                    }
                }
                catch
                {
                }
            }

            return item;
        }

        public static object GetListOrItemFromSPFolder(SPWeb web, SPFolder folder)
        {
            if (folder == null)
            {
                return null;
            }
            if (web == null)
            {
                web = folder.ParentWeb;
            }
            SPListItem item = null;
            try
            {
                item = folder.Item;
            }
            catch
            { }

            if (item == null)
            {
                string fullUrl = web.Site.MakeFullUrl(folder.ServerRelativeUrl);
                using (SPSite site = new SPSite(fullUrl))
                {
                    SPWeb destWeb = site.OpenWeb();
                    item = (SPListItem)Utilities.GetCachedSPContent(destWeb, fullUrl, Utilities.SPUrlListItem);
                    if (item == null)
                    {
                        return Utilities.GetCachedSPContent(destWeb, fullUrl, Utilities.SPUrlList);
                    }
                    SPEEvalAttrs.Current().AddDisposeWeb(destWeb);
                }
            }

            return item;
        }

        public static void GetPolicyNameMessageByObligation(string[] enforcerObligations, ref string policyName, ref string policyMsg)
        {
            List<Obligation> obligations = new List<Obligation>();
            ParseObligations(enforcerObligations, obligations);
            GetPolicyNameMessageByObligation(obligations, ref policyName, ref policyMsg);
        }

        public static void GetPolicyNameMessageByObligation(List<Obligation> obligations, ref string policyName, ref string policyMsg)
        {
            foreach(Obligation ob in obligations)
            {
                if (ob.Name.Equals(SPUserAlert, StringComparison.OrdinalIgnoreCase))
                {
                    policyName = ob.GetAttribute(SPPolicyName);
                    policyMsg = ob.GetAttribute(SPPolicyMessage);
                    break;
                }
            }
        }

        public static void GetPolicyNameMessageByObligation(List<CEObligation> obligations, ref string policyName, ref string policyMsg)
        {
            foreach (CEObligation ob in obligations)
            {
                if (ob.GetName().Equals(SPUserAlert, StringComparison.OrdinalIgnoreCase))
                {
                    QueryCloudAZSDK.CEModel.CEAttres ceAttrs = ob.GetCEAttres();
                    int count = ceAttrs.Count;
                    for (int i = 0; i < count; i++)
                    {
                        QueryCloudAZSDK.CEModel.CEAttribute ceAttr = ceAttrs[i];
                        if (!string.IsNullOrEmpty(ceAttr.Name) && !string.IsNullOrEmpty(ceAttr.Value) && ceAttr.Name.Equals(SPPolicyName, StringComparison.OrdinalIgnoreCase))
                        {
                            policyName = ceAttr.Value;
                        }
                        else if (!string.IsNullOrEmpty(ceAttr.Name) && !string.IsNullOrEmpty(ceAttr.Value) && ceAttr.Name.Equals(SPPolicyMessage, StringComparison.OrdinalIgnoreCase))
                        {
                            policyMsg = ceAttr.Value;
                        }
                    }
                    break;
                }
            }
        }

        public static void ParseObligations(string[] enforcerObligations, List<Obligation> obligationsList)
        {
            // Parse enforcerObligations to m_Obligations
            if (enforcerObligations.Length > 0)
            {
                Dictionary<string, string> obligations = new Dictionary<string, string>();
                for (int i = 0; i < enforcerObligations.Length; i += 2)
                {
                    obligations.Add(enforcerObligations[i], enforcerObligations[i + 1]);
                }

                int obligation_count = 0;
                try
                {
                    string count = obligations.ContainsKey(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT) ? obligations[CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT] : null;
                    if (!string.IsNullOrEmpty(count))
                    {
                        obligation_count = int.Parse(count);
                    }
                }
                catch(Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ParseObligations:", null, ex);
                }

                string nameKey = null;
                string policyKey = null;
                string attrNumKey = null;
                string attrValueName = null;
                string attrKey = null;
                string attrValue = null;
                for (int i = 0; i < obligation_count; i++)
                {
                    Obligation ob = new Obligation();
                    nameKey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_NAME + ":" + (i + 1);
                    policyKey = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_POLICY + ":" + (i + 1);
                    attrNumKey = "CE_ATTR_OBLIGATION_NUMVALUES:" + (i + 1);
                    ob.Name = obligations[nameKey];
                    if (!Globals.g_JPCParams.bUseJavaPC && !string.IsNullOrEmpty(policyKey))
                    {
                        ob.Policy = obligations[policyKey];
                    }

                    if (ob.Name != CETYPE.CEAttrVal.CE_OBLIGATION_NOTIFY)
                    {
                        int attrNum = Int32.Parse(obligations[attrNumKey]);
                        for (int j = 0; j < attrNum; j += 2)
                        {
                            attrValueName = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1) + ":" + (j + 1);
                            attrKey = obligations[attrValueName];
                            attrValueName = CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1) + ":" + (j + 2);
                            attrValue = obligations[attrValueName];

                            if (!String.IsNullOrEmpty(attrKey) && attrValue != null)
                            {
                                ob.AddAttribute(attrKey, attrValue);
                            }
                        }
                        obligationsList.Add(ob);
                    }
                }
            }
        }

        static public string[] ConvertObligationListtoArray(List<CEObligation> lsObligation)
        {
            List<string> lstRes = new List<string>();
            lstRes.Add(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_COUNT);
            lstRes.Add(lsObligation.Count.ToString());
            for (int i = 0; i < lsObligation.Count; i++)
            {
                CEObligation obl = lsObligation[i];
                lstRes.Add(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_NAME + ":" + (i + 1).ToString());
                lstRes.Add(obl.GetName());
                QueryCloudAZSDK.CEModel.CEAttres att = obl.GetCEAttres();

                int j = 0;
                for (; j < att.Count; j++)
                {
                    QueryCloudAZSDK.CEModel.CEAttribute ceAttr = att[j];
                    lstRes.Add(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1).ToString() + ":" + (2 * j + 1).ToString());
                    lstRes.Add(ceAttr.Name);
                    lstRes.Add(CETYPE.CEAttrKey.CE_ATTR_OBLIGATION_VALUE + ":" + (i + 1).ToString() + ":" + (2 * j + 2).ToString());
                    lstRes.Add(ceAttr.Value);
                }

                lstRes.Add("CE_ATTR_OBLIGATION_NUMVALUES:" + (i + 1).ToString());
                lstRes.Add((j * 2).ToString());
            }

	       return lstRes.ToArray();
        }

        public static string[] SetAttrsResourceSignature(string[] strAttrs, string strSrcName)
        {
            bool bFind = false;
            string strResSignatrue = "Resource Signature";
            string strUrl = "url"; // to support CEPC 8.7
            for (int i = 0; i < strAttrs.Length; i++)
            {
                if (strAttrs[i].Equals(strResSignatrue, StringComparison.OrdinalIgnoreCase))
                {
                    bFind = true;
                    strAttrs[i + 1] = strSrcName;
                    break;
                }
            }
            if (!bFind)
            {
                List<string> listAttrs = strAttrs.ToList<string>();
                listAttrs.Add(strResSignatrue);
                listAttrs.Add(strSrcName);
                listAttrs.Add(strUrl);
                listAttrs.Add(strSrcName);
                return listAttrs.ToArray();
            }
            else
            {
                return strAttrs;
            }
        }
        public static string ParseXHeaderProperties(HttpRequest Req, List<string> properties)
        {
            string strRetIp = "";
            try
            {
                if (Globals.g_lstrXHeaders == null || Globals.g_lstrXHeaders.Count < 1 || Req == null)
                    return strRetIp;

                NameValueCollection nvc = Req.Headers;
                string strValue = null;

                strValue = nvc[Globals.g_lstrXHeaders[0].Key]; //ip address
                if (!string.IsNullOrEmpty(strValue))
                {
                    strRetIp = strValue;  //replace the old one
                }
                for (int i = 1; i < Globals.g_lstrXHeaders.Count; i++)
                {
                    strValue = nvc[Globals.g_lstrXHeaders[i].Key];
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        properties.Add(Globals.g_lstrXHeaders[i].Value);
                        properties.Add(strValue);
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ParseXHeaderProperties:", null, ex);
            }
            return strRetIp;
        }

        public static string GetSPObjectCacheGuid(object obj, ref DateTime modifyTime)
        {
            string guid = "";
            if (obj is SPWeb)
            {
                SPWeb evalWeb = obj as SPWeb;
                guid = evalWeb.Url;
            }
            else if (obj is SPList)
            {
                SPList evalList = obj as SPList;
                guid = NextLabs.Common.Utilities.ReConstructListUrl(evalList);
            }
            else if (obj is SPListItem)
            {
                SPListItem evalItem = obj as SPListItem;
                guid = evalItem.ParentList.ID.ToString() + evalItem.ID.ToString();
                modifyTime = NextLabs.Common.Utilities.GetLastModifiedTime(evalItem);
            }

            return guid;
        }

        public static bool CheckIgnoreTrimControl(HttpRequest request)
        {
            // For RestApi, CSOM and SOAP trimming, ignore the trimming contorl except browser. "Mozilla" is browser flag.
            string strUserAgent = request.UserAgent;
            if (!string.IsNullOrEmpty(strUserAgent) && -1 != strUserAgent.IndexOf("Mozilla", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static bool SetActivatedSiteIds(SPWebApplication webApp, string value)
        {
            bool bUpdate = false;
            try
            {
                if (webApp != null)
                {
                    webApp.Properties[ActivatedSiteIds] = value;
                    webApp.Update();
                    bUpdate = true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during set activated site ids, webApp:[{0}], value:[{1}]:", new object[] { webApp, value }, ex);
            }
            return bUpdate;
        }

        public static string GetActivatedSiteIds(SPWebApplication webApp)
        {
            string value = "";
            if (webApp != null && webApp.Properties.ContainsKey(ActivatedSiteIds))
            {
                value = webApp.Properties[ActivatedSiteIds] as string;
            }
            return value;
        }

        public static void SetNewSitePEDefault(SPWebApplication webApp, string value)
        {
            if (webApp != null)
            {
                webApp.Properties[NewSitePEDefault] = value;
                webApp.Update();
            }
        }

        public static string GetNewSitePEDefault(SPWebApplication webApp)
        {
            string value = "";
            if (webApp != null && webApp.Properties.ContainsKey(NewSitePEDefault))
            {
                value = webApp.Properties[NewSitePEDefault] as string;
            }
            return value;
        }
    }
}
