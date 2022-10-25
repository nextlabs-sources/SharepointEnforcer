using System;
using System.Collections;
using System.Collections.Generic;
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
using NextLabs.Common;
using Microsoft.SharePoint.WebControls;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Linq;
using NextLabs.Diagnostic;
using Microsoft.SharePoint.Utilities;
using static System.Environment;

namespace NextLabs.SPEnforcer
{
    /// <summary>
    /// The ItemXXXing synchronized events apply to both List Items as well as
    /// Document Library items.
    /// This is because everything in MOSS is pretty much a List. SPList has
    /// been improved to include folders and versioning.  As a result Document
    /// libraries are now based on SPList and only have a few additional
    /// methods and properties.  So the ItemXXXing event would be applicable
    /// to any kind of List that exists in MOSS.
    /// </summary>
    public class ItemHandler : SPItemEventReceiver
    {
        #region Constructor
        private HttpContext _currentContext;

        static private HttpModuleEventHandler m_HttpEventHandler=null;

        public ItemHandler()
        {
            _currentContext = HttpContext.Current;
        }

        static ItemHandler()
        {
            m_HttpEventHandler = new HttpModuleEventHandler();
            NextLabs.HttpEnforcer.HttpEnforcerModule.SetHttpModuleEventHandler(m_HttpEventHandler);
        }

		new public void DisableEventFiring()
        {
                base.EventFiringEnabled = false;
        }

        new public void EnableEventFiring()
        {
                base.EventFiringEnabled = true;
        }

        #endregion

        #region ItemDeleting
        /// <summary>
        /// Synchronous before event that occurs before an existing item is
        /// completely deleted.
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemDeleting(SPItemEventProperties properties)
        {
            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemDeleting Get list:", null, ex);
            }
            if (list != null)
            {
                try
                {

                    SPWeb web = properties.Web;
                    string url = web.Url;
                    SPUser user = web.CurrentUser;
                    string loginName = user.LoginName;
                    string sid = user.Sid;
                    if (string.IsNullOrEmpty(sid))
                        sid = UserSid.GetUserSid(loginName);
                    if (string.IsNullOrEmpty(sid))
                        sid = loginName;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(loginName, web.Url, ref PrincipalUser);
                    SPBaseType baseType = list.BaseType;
                    string beforeUrl = properties.BeforeUrl;
                    string afterUrl = properties.AfterUrl;
                    PreFilterModule.setSource(loginName, sid, clientIpAddr, url + '/' + beforeUrl);
                    var args = new EventHandlerEventArgs(properties, _currentContext);
                    EventHelper.Instance.OnBeforeEventExecuting(this, args);
                    PreFilterModule.releaseSource();
                    if (args.Cancel)
                    {
                        this.EventFiringEnabled = true;
                        return;
                    }
                    SPItemEventDataCollection afterProp = properties.AfterProperties;
                    SPListItem listitem = properties.ListItem;

                    if (listitem == null)
                    {
                        listitem = Globals.ParseItemFromAttachmentURL(web, url + '/' + beforeUrl);
                    }
                    if (listitem == null)
                    {
                        // This event is actually "noise" event from other user action,
                        // most likely Delete List.
                        base.ItemDeleting(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Delete);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc
                        NLLogger.OutputLog(LogLevel.Debug, "policy no match", null);
                        base.ItemDeleting(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion

                    Hashtable itemProp = listitem.Properties;
                    string[] extraAttrs = null;
                    if (listitem.File != null && listitem.File.Properties.Contains("IsDeleteByCode"))
                    {
                        extraAttrs = new string[]{
                            CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                            listitem.Name,
                            CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                            Globals.GetItemCreatedBySid(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                            Globals.GetItemModifiedBySid(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                            Globals.GetItemCreatedStr(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                            Globals.GetItemModifiedStr(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                            Globals.GetItemFileSizeStr(listitem),
                            "IsDeleteByCode",
                            "Yes"
                        };
                    }
                    else
                    {
                        extraAttrs = new string[]{
                                CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                                listitem.Name,
                                CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                                Globals.GetItemCreatedBySid(listitem),
                                CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                                Globals.GetItemModifiedBySid(listitem),
                                CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                                Globals.GetItemCreatedStr(listitem),
                                CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                                Globals.GetItemModifiedStr(listitem),
                                CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                                Globals.GetItemFileSizeStr(listitem)
                            };
                    }
                    string[] itemPropArray = Globals.BuildAttrArrayFromItemProperties(itemProp, extraAttrs, baseType, list.Fields);
                    //Fix bug 8222, replace the "created" and "modified" properties
                    itemPropArray = Globals.ReplaceHashTime(web, list, listitem, itemPropArray);
                    //Fix bug 8694 and 8692,add spfield attr to tailor
                    itemPropArray = Globals.BuildAttrArray2FromSPField(web, list, listitem, itemPropArray);

                    string[] emptyArray = new string[0];
                    bool bUseEmptyPropArray = false;
                    string strEvaUrl = string.Empty;
                    string policyName = null;
                    string policyMessage = null;
                    CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;

                    if (baseType == SPBaseType.UnspecifiedBaseType)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "ItemDeleting: Unsupported list type:[{0}], loginName:[{1}], clientIp:[{2}]", new object[] { baseType, url, loginName, clientIpAddr });
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["ContentType"] == null ||
                             string.IsNullOrEmpty(afterProp["ContentType"].ToString())) &&
                             (itemProp["ContentType"] != null &&
                              itemProp["ContentType"].ToString() == "Document") &&
                             (afterProp["vti_docstoretype"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString()))
                             )
                    {
                        bUseEmptyPropArray = true;
                        strEvaUrl = url + "/" + beforeUrl;
                    }
                    //Fix bug 8216 need to remove ContentType detection
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["vti_docstoretype"] != null &&
                              afterProp["vti_docstoretype"].ToString() == "0") &&
                             (!string.IsNullOrEmpty(beforeUrl)) &&
                             (!string.IsNullOrEmpty(afterUrl)) &&
                             beforeUrl != afterUrl)
                    {
                        strEvaUrl = url + "/" + beforeUrl;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (!string.IsNullOrEmpty(beforeUrl)) &&
                             (!string.IsNullOrEmpty(afterUrl)) &&
                             beforeUrl != afterUrl &&
                             (afterProp["vti_docstoretype"] != null &&
                              afterProp["vti_docstoretype"].ToString() == "1"))
                    {
                        strEvaUrl = url + "/" + beforeUrl;
                    }
                    else if (baseType != SPBaseType.DocumentLibrary &&
                             (string.IsNullOrEmpty(beforeUrl))
                             )
                    {
                        //Fix bug 8730, use a contructed url instead, added by William 20090226
                        string itemUrl = Globals.ConstructListUrl(web, list) + '/' + listitem.Title;
                        strEvaUrl = url + "/" + itemUrl;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (itemProp["ContentType"] == null ||
                             string.IsNullOrEmpty(itemProp["ContentType"].ToString()))
                             )
                    {
                        strEvaUrl = url + "/" + beforeUrl;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["ContentType"] == null ||
                             string.IsNullOrEmpty(afterProp["ContentType"].ToString())) &&
                             (afterProp["vti_docstoretype"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString())))
                    {
                        bUseEmptyPropArray = true;
                        strEvaUrl = url + "/" + beforeUrl;
                    }
                    else if (baseType != SPBaseType.DocumentLibrary)
                    {
                        string itemUrl = Globals.ConstructListUrl(web, list) + '/' + listitem.Title;

                        string[] itemPropArray2 = Globals.BuildAttrArrayFromItemProperties(itemProp, extraAttrs, baseType, list.Fields);
                        //Fix bug 8222, replace the "created" and "modified" properties
                        itemPropArray2 = Globals.ReplaceHashTime(web, list, listitem, itemPropArray2);
                        //Fix bug 8694 and 8692,add spfield attr to tailor
                        itemPropArray2 = Globals.BuildAttrArray2FromSPField(web, list, listitem, itemPropArray2);

                        itemPropArray = itemPropArray2;
                        bUseEmptyPropArray = true;
                        strEvaUrl = itemUrl;
                    }

                    if (bUseEmptyPropArray)
                    {
                        response = Globals.CallEval(CETYPE.CEAction.Delete,
                                    strEvaUrl,
                                    null,
                                    ref itemPropArray,
                                    ref emptyArray,
                                    clientIpAddr,
                                    loginName,
                                    sid,
                                    ref policyName,
                                    ref policyMessage,
                                    strEvaUrl,
                                    null,
                                    Globals.EventHandlerName,
                                    CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                    web,
                                PrincipalUser);
                    }

                    else
                    {
                        response = Globals.CallEval(CETYPE.CEAction.Delete,
                                strEvaUrl,
                                null,
                                ref itemPropArray,
                                ref itemPropArray,
                                clientIpAddr,
                                loginName,
                                sid,
                                ref policyName,
                                ref policyMessage,
                                strEvaUrl,
                                null,
                                Globals.EventHandlerName,
                                CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                web,
                            PrincipalUser);
                    }

                    if (response == CETYPE.CEResponse_t.CEAllow)
                    {
                        base.ItemDeleting(properties);
                    }
                    else
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemDeleting:", null, ex);
                }
            }
            else
            {
                base.ItemDeleting(properties);
            }
            this.EventFiringEnabled = true;
        }
        #endregion

        #region ItemUpdated
        public override void ItemUpdated(SPItemEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;

            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemUpdated Get list:", null, ex);
            }
            if (list != null)
            {
                try
                {
                    SPWeb web = properties.Web;
                    string loginName = web.CurrentUser.LoginName;

                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(loginName, web.Url, ref PrincipalUser);

                    SPBaseType baseType = list.BaseType;
                    //string beforeUrl = properties.BeforeUrl;
                    //string afterUrl = properties.AfterUrl;
                    SPListItem listItem = properties.ListItem;
                    if (baseType == SPBaseType.DocumentLibrary)
                    {
                        #region add prefilter
                        // add prefilter
                        string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
                        var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                        if (noMatch)
                        {
                            //donnot query pc,
                            NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                            base.ItemUpdated(properties);
                            this.EventFiringEnabled = true;
                            return;
                        }
                        #endregion
                        ItemVersionControl.PostCAFun = PostContentAnalysisEventHandler;
                         ItemVersionControl.OnItemUpdated(properties, web, list, listItem, PrincipalUser, clientIpAddr);
                    }

                  base.ItemUpdated(properties);
                  this.EventFiringEnabled = true;

                }
                catch(Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemUpdated:", null, ex);
                }
            }
        }
        #endregion

        #region ItemUpdating
        /// <summary>
        /// Synchronous before event that occurs when an existing item is
        /// changed, for example, when the user changes data in one or more
        /// fields.
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemUpdating(SPItemEventProperties properties)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Begin item updating event, BeforeUrl:[{0}], AfterUrl:[{1}]", new object[] { properties.BeforeUrl, properties.AfterUrl });

            string itemUrl = String.Empty;
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemUpdated Get list:", null, ex);
            }

            if (list != null)
            {
                try
                {
                    SPWeb web = properties.Web;
                    string url = web.Url;
                    SPUser user = web.CurrentUser;
                    string loginName = user.LoginName;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(loginName, web.Url, ref PrincipalUser);
                    string sid = user.Sid;
                    SPBaseType baseType = list.BaseType;
                    string beforeUrl = properties.BeforeUrl;
                    //SPItemEventDataCollection afterProp = properties.AfterProperties;
                    SPListItem listitem = properties.ListItem;
                    SPFile _fileObj = null;
                    string policyName = null;
                    string policyMessage = null;
                    CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
                    string[] emptyArray = new string[0];

                    if (listitem == null)
                    {
                        listitem = Globals.ParseItemFromAttachmentURL(web, url + '/' + beforeUrl);
                    }

                    if (baseType == SPBaseType.DocumentLibrary)
                    {
                       ItemVersionControl.OnItemUpdating(properties, web, list, listitem, PrincipalUser, clientIpAddr );
                        if (properties.Status.Equals(SPEventReceiverStatus.CancelWithError))
                       {
                            this.EventFiringEnabled = true;
                           return;
                       }
                    }

                    // fix bug 8873 by derek
                    if (listitem == null)
                    {
                        try
                        {
                            //George: convert object to SPFile before check it is SPFile.
                            object obj = web.GetObject(url + '/' + beforeUrl);
                            if (obj != null && obj is SPFile)
                            {
                                _fileObj = obj as SPFile;
                            }
                        }
                        catch
                        {
                            _fileObj = null;
                        }

                        if (_fileObj == null || !_fileObj.Exists)
                        {
                            base.ItemUpdating(properties);
                            this.EventFiringEnabled = true;
                            return;
                        }
                    }

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc,
                        NLLogger.OutputLog(LogLevel.Debug, "prefilter policy no match");
                        base.ItemUpdating(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion
                    #region save file to server
                    string filePath = SaveFileToLoaclServerDuringUploading(_currentContext, properties);
                    #endregion
                    #region Do pre-authorization for uploading
                    List<KeyValuePair<string, string>> lsPrepareExtralAttributes = new List<KeyValuePair<string, string>>();
                    Globals.DoPreAuthorizationForUpload(properties, filePath, url + '/' + beforeUrl, action, ref lsPrepareExtralAttributes);
                    Globals.MyDeleteFile(filePath);
                    #endregion

                    if (listitem != null)   // fix bug 8873 by derek
                    {
                        String _Name = null;
                        String _Title = null;
                        try
                        {
                            _Name = listitem.Name;
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Error, "Exception during ItemUpdating getting listname.Name:", null, ex);
                        }
                        if (list.BaseTemplate != SPListTemplateType.WebPageLibrary)
                        {
                            try
                            {
                                _Title = listitem.Title;
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Error, "Exception during ItemUpdating getting listname.Title:", null, ex);
                            }
                        }
                        if (string.IsNullOrEmpty(_Title))
                            _Title = _Name;
                        else if (string.IsNullOrEmpty(_Name))
                            _Name = _Title;

                        string[] extraAttrs = {
                            CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                            _Name,
                            CETYPE.CEAttrKey.CE_ATTR_SP_TITLE,
                            _Title,
                            CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                            Globals.GetItemCreatedBySid(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                            Globals.GetItemModifiedBySid(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                            Globals.GetItemCreatedStr(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                            Globals.GetItemModifiedStr(listitem),
                            CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                            Globals.GetItemFileSizeStr(listitem)
                        };
                        if (baseType == SPBaseType.UnspecifiedBaseType)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "ItemUpdating: Unsupported list type:[{0}], url:[{1}] loginName:[{2}], clientIp:[{3}]", new object[] { baseType, url, loginName, clientIpAddr });
                        }
                        else if (baseType == SPBaseType.DocumentLibrary)
                        {
                            string[] beforePropArray = Globals.BuildAttrArrayFromItemEventProperties(properties.BeforeProperties, extraAttrs, baseType, list.Fields);
                            //Fix bug 8222, replace the "created" and "modified" properties
                            beforePropArray = Globals.ReplaceHashTime(web, list, listitem, beforePropArray);
                            //Fix bug 8694 and 8692,add spfield attr to tailor
                            beforePropArray = Globals.BuildAttrArray2FromSPField(web, list, listitem, beforePropArray);

                            Globals.SetListPairInfoIntoArray(ref beforePropArray, lsPrepareExtralAttributes);

                            // Call eval.
                            response = Globals.CallEval(CETYPE.CEAction.Write,
                                                        url + "/" + beforeUrl,
                                                        null,
                                                        ref beforePropArray,
                                                        ref emptyArray,
                                                        clientIpAddr,
                                                        loginName,
                                                        sid,
                                                        ref policyName,
                                                        ref policyMessage,
                                                        null,
                                                        url + "/" + beforeUrl,
                                                        Globals.EventHandlerName,
                                                        CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                        web,
                                                PrincipalUser);
                        }
                        else
                        {
                            itemUrl = Globals.ConstructListUrl(web, list) + "/" +
                                listitem.Title;

                            // properties.BeforeProperties is always empty, while
                            // properties.AfterProperties contains only the
                            // after-properties.  So we have no choice but to pass the
                            // after-properties to eval as if they were the
                            // before-properties.

                            // The only before-property that we can find is the item
                            // title, and it is in item.Title.  So we temporarily
                            // overwrite the title (if any) in the after-properties with
                            // item.Title.
                            //To fix bug 9779, do not use after properties, modified by William 20090914
                            string[] afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(null, extraAttrs, baseType, list.Fields);
                            //Fix bug 8222, replace the "created" and "modified" properties
                            afterPropArray = Globals.ReplaceHashTime(web, list, listitem, afterPropArray);
                            //Fix bug 8694 and 8692,add spfield attr to tailor
                            afterPropArray = Globals.BuildAttrArray2FromSPField(web, list, listitem, afterPropArray);

                            Globals.SetListPairInfoIntoArray(ref afterPropArray, lsPrepareExtralAttributes);

                            // This line is for the caller (outside enforcer) to use, not for us.
                            // That's why you don't see any reference to afterprop below
                            // Call eval.
                            response = Globals.CallEval(CETYPE.CEAction.Write,
                                                        itemUrl,
                                                        null,
                                                        ref afterPropArray,
                                                        ref emptyArray,
                                                        clientIpAddr,
                                                        loginName,
                                                        sid,
                                                        ref policyName,
                                                        ref policyMessage,
                                                        null,
                                                        itemUrl,
                                                        Globals.EventHandlerName,
                                                        CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                        web,
                                                PrincipalUser);
                        }
                    }
                    // fix bug 8873 by derek
                    else
                    {
                        string[] extraAttrs = {
                            CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                            _fileObj.Name,
                            CETYPE.CEAttrKey.CE_ATTR_SP_TITLE,
                            _fileObj.Title,
                            CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                            _fileObj.Author.Sid,
                            CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                            _fileObj.ModifiedBy.Sid,
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                            Globals.ConvertDataTime2Str(_fileObj.TimeCreated),
                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                            Globals.ConvertDataTime2Str(_fileObj.TimeLastModified),
                            CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                            _fileObj.TotalLength.ToString()
                        };
                        string[] beforePropArray = Globals.BuildAttrArrayFromItemEventProperties(properties.BeforeProperties, extraAttrs, baseType,list.Fields);
                        beforePropArray = Globals.BuildAttrArrayFromHashTable(_fileObj.Properties, beforePropArray);

                        Globals.SetListPairInfoIntoArray(ref beforePropArray, lsPrepareExtralAttributes);

                        // Call eval.
                        response = Globals.CallEval(CETYPE.CEAction.Write,
                                                    url + '/' + beforeUrl,
                                                    null,
                                                    ref beforePropArray,
                                                    ref emptyArray,
                                                    clientIpAddr,
                                                    loginName,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    null,
                                                    url + '/' + beforeUrl,
                                                    Globals.EventHandlerName,
                                                    CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                    web,
                                                PrincipalUser);
                    }

                    if (response == CETYPE.CEResponse_t.CEAllow)
                    {
                        base.ItemUpdating(properties);
                    }
                    else
                    {
                        #region Add By Roy 2014.2.28
                        if (itemUrl.ToLower().Contains("_catalogs/users"))
                        {
                            this.EventFiringEnabled = true;
                            return;
                        }
                        #endregion
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemUpdating:", null, ex);
                }
            }
            else
            {
                base.ItemUpdating(properties);
            }
            this.EventFiringEnabled = true;

            NLLogger.OutputLog(LogLevel.Debug, "End item updating event", null);
        }
        #endregion

        #region ItemAdding
        /// <summary>
        /// Synchronous before event that occurs when a new item is added, for
        /// example, when the user changes data in one or more fields.
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemAdding(SPItemEventProperties properties)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Begin item adding event, BeforeUrl:[{0}], AfterUrl:[{1}]", new object[] { properties.BeforeUrl, properties.AfterUrl });

            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;
            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemAdding Get list:", null, ex);
            }
            if (list != null)
            {
                try
                {
                    SPWeb web = properties.Web;
                    string url = web.Url;
                    SPUser user = web.CurrentUser;
                    string loginName = user.LoginName;
                    string sid = user.Sid;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                        (loginName, web.Url, ref PrincipalUser);
                    if (PrincipalUser == null)
                    {
                        HttpContext context = HttpContext.Current;
                        if (context != null)
                            PrincipalUser = context.User;
                    }
                    SPBaseType baseType = list.BaseType;
                    string beforeUrl = properties.BeforeUrl;
                    string afterUrl = properties.AfterUrl;
                    SPItemEventDataCollection afterProp = properties.AfterProperties;

                    bool isDocumentSet = afterProp["HTML_x0020_File_x0020_Type"] != null ? (afterProp["HTML_x0020_File_x0020_Type"].ToString().Equals("Sharepoint.DocumentSet", StringComparison.CurrentCultureIgnoreCase)) : false;
                    // We don't want to hit a timing hole where "created" and
                    // "modified" differ by 1ms or more, which might cause problem
                    // somewhere.  So we call GetCurrentTimeStr() only once.
                    string curTimeStr = Globals.GetCurrentTimeStr();
                    string[] emptyArray = new string[0];
                    string policyName = null;
                    string policyMessage = null;
                    SPListItem listItem = properties.ListItem;
                    //added by Gavin Mar 3rd, 2014
                    if (string.IsNullOrEmpty(sid))
                    {
                        //in SP2013, the sid is empty but we need the sid for attributes created by/modified by
                        //get sid from AD by login name
                        sid = Globals.getADUserSid(loginName);
                    }
                    //when uploading an attachments from windows explorer, the ItemAdding event is triggered before ItemUpdating
                    //we need the listitem for evaluation
                    if (listItem == null && Regex.Match(beforeUrl, "lists/[^/]+/attachments/[0-9]+/", RegexOptions.IgnoreCase).Success)
                    {
                        //remove the attachment from url, because it has not been created. will throw exception when get object from SPWeb
                        string listItemUrl = beforeUrl.Substring(0, beforeUrl.LastIndexOf('/'));
                        //check if it's adding an attachment, and get the list item from attachment url
                        listItem = Globals.ParseItemFromAttachmentURL(web, string.Format("{0}/{1}", url, listItemUrl));
                    }
                    //Try to get attributes title/name from list item
                    string title, name;
                    if (listItem != null)
                    {
                        title = listItem.Title;
                        name = listItem.Name;
                    }
                    else
                    {
                        title = name = (afterProp["Title"] ?? string.Empty).ToString();
                    }//End:Mar 3rd, 2014
                    CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
                    string[] extraAttrs = {
                                              CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                                              name,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_TITLE,
                                              title,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                                              sid,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                                              sid,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                                              curTimeStr,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                                              curTimeStr,
                                              CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                                              (afterProp["vti_filesize"] == null
                                                    ? string.Empty
                                                    : afterProp["vti_filesize"].ToString())};
                    string beforeName = string.IsNullOrEmpty(beforeUrl) ? "" : beforeUrl.Substring(beforeUrl.LastIndexOf('/') + 1);
                    string afterName = string.IsNullOrEmpty(afterUrl) ? beforeName : afterUrl.Substring(afterUrl.LastIndexOf('/') + 1);
                    string[] extraAttrs2 = {
                                            CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                                            afterName,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_TITLE,
                                            afterName,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                                            sid,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                                            sid,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                                            curTimeStr,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                                            curTimeStr,
                                            CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                                            (afterProp["vti_filesize"] == null ?
                                             "" : afterProp["vti_filesize"].ToString())
                                           };
                    string[] afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs, baseType, list.Fields);
                    string strEvalUrl = string.Empty;

                     if (baseType == SPBaseType.DocumentLibrary)
                     {
                         ItemVersionControl.OnItemAdding(properties, web, list, listItem, PrincipalUser, clientIpAddr);

                         if (properties.Status.Equals(SPEventReceiverStatus.CancelWithError))
                         {
                             this.EventFiringEnabled = true;
                             return;
                         }
                     }

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Upload);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc,
                        NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                        base.ItemAdding(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion

                    #region save file to server
                    string filePath = SaveFileToLoaclServerDuringUploading(_currentContext, properties);
                    #endregion

                    #region Do pre-authorization for uploading
                    List<KeyValuePair<string, string>> lsPrepareExtralAttributes = new List<KeyValuePair<string, string>>();
                    Globals.DoPreAuthorizationForUpload(properties, filePath, strEvalUrl, action, ref lsPrepareExtralAttributes);
                    Globals.MyDeleteFile(filePath);
                    #endregion

                    if (baseType == SPBaseType.UnspecifiedBaseType)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "ItemAdding: Unsupported list type:[{0}], loginName:[{1}], ClientIp:[{2}]", new object[] { baseType, url, loginName, clientIpAddr });
                        // Always ALLOW.
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                        (afterProp["vti_docstoretype"] == null ||
                        string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString())) &&
                        (afterProp["ContentType"] != null &&
                         afterProp["ContentType"].ToString() == "Folder") &&
                        (afterProp["vti_modifiedby"] == null ||
                        string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())))
                    {
                        strEvalUrl = Globals.ConstructListUrl(web, list) + '/' + title;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["vti_docstoretype"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString())) &&
                             (afterProp["vti_modifiedby"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())))
                    {
                        // Fix bug 8735
                        string inUseUrl = afterUrl;
                        if (inUseUrl == null)
                            afterUrl = inUseUrl = beforeUrl;
                        List<string> ls = new List<string>();
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_NAME);
                        ls.Add(inUseUrl.Substring(inUseUrl.LastIndexOf('/') + 1));
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_TITLE);
                        ls.Add(afterUrl.Substring(afterUrl.LastIndexOf('/') + 1));
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY);
                        ls.Add(sid);
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY);
                        ls.Add(sid);
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED);
                        ls.Add(curTimeStr);
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED);
                        ls.Add(curTimeStr);
                        ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE);
                        ls.Add(afterProp["vti_filesize"] == null ? "" : afterProp["vti_filesize"].ToString());
                        if (isDocumentSet)
                        {
                            ls.Add("content type");
                            ls.Add("document set");
                        }
                        string[] extraAttrs3 = ls.ToArray();
                        afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs3, baseType, list.Fields);
                        strEvalUrl = url + '/' + inUseUrl;
                    }
                    else if (baseType != SPBaseType.DocumentLibrary &&
                             (afterProp["Modified"] == null ||
                             string.IsNullOrEmpty(afterProp["Modified"].ToString())))
                    {

                        string defaultViewUrl = list.DefaultViewUrl;
                        int lastSlashIndex = defaultViewUrl.LastIndexOf('/');
                        if (lastSlashIndex != -1)
                        {
                            strEvalUrl = web.Site.MakeFullUrl(defaultViewUrl.Remove(lastSlashIndex)) + '/' + title;
                        }
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["vti_docstoretype"] == null ||
                              string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString())) &&
                             (afterProp["ContentType"] != null &&
                              afterProp["ContentType"].ToString() == "Document") &&
                             (afterProp["vti_modifiedby"] != null &&
                             !string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())))
                    {
                        afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs2, baseType, list.Fields);
                        strEvalUrl = url + '/' + afterUrl;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["vti_docstoretype"] != null &&
                              afterProp["vti_docstoretype"].ToString() == "0") &&
                             (afterProp["vti_modifiedby"] != null &&
                             !string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())))
                    {
                        afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs2, baseType, list.Fields);
                        strEvalUrl = url + '/' + afterUrl;
                    }
                    else if (baseType != SPBaseType.DocumentLibrary &&
                             (afterProp["Modified"] != null &&
                             !string.IsNullOrEmpty(afterProp["Modified"].ToString())))
                    {
                        strEvalUrl = Globals.ConstructListUrl(web, list) + '/' + title;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["ContentType"] == null ||
                             string.IsNullOrEmpty(afterProp["ContentType"].ToString())) &&
                             (afterProp["vti_docstoretype"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_docstoretype"].ToString())) &&
                             (afterProp["Modified"] == null ||
                             string.IsNullOrEmpty(afterProp["Modified"].ToString())) &&
                             (afterProp["vti_modifiedby"] != null &&
                             !string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())) &&
                             !string.IsNullOrEmpty(beforeUrl) &&
                             !string.IsNullOrEmpty(afterUrl) &&
                             beforeUrl == afterUrl)
                    {
                        // Site Content & Structure -> Move (document, same list) STEP 1
                        // We don't have src URL, so we can't call CE_ACTION_MOVE.
                        // Hence we can only call CE_ACTION_WRITE.
                        afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs2, baseType, list.Fields);
                        strEvalUrl = url + '/' + afterUrl;
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                             (afterProp["vti_docstoretype"] != null &&
                              afterProp["vti_docstoretype"].ToString() == "1") &&
                             (afterProp["vti_modifiedby"] == null ||
                             string.IsNullOrEmpty(afterProp["vti_modifiedby"].ToString())))
                    {
                        // Web Folder -> Move (folder, across list) STEP 2
                        afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs2, baseType, list.Fields);
                        strEvalUrl = url + '/' + afterUrl;
                    }

                    afterPropArray = Globals.ReplaceHashTime(web, list, listItem, afterPropArray);
                    afterPropArray = Globals.BuildAttrArray2FromSPField(web, list, listItem, afterPropArray);

                    Globals.SetListPairInfoIntoArray(ref afterPropArray, lsPrepareExtralAttributes);

                    response = Globals.CallEval(CETYPE.CEAction.Upload,
                            strEvalUrl,
                            null,
                            ref afterPropArray,
                            ref emptyArray,
                            clientIpAddr,
                            loginName,
                            sid,
                            ref policyName,
                            ref policyMessage,
                            null,
                            strEvalUrl,
                            Globals.EventHandlerName,
                            CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                            web,
                        PrincipalUser);
                    if (response == CETYPE.CEResponse_t.CEAllow)
                    {
                        base.ItemAdding(properties);
                    }
                    else
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemAdding: ", null, ex);
                }
            }
            else
            {
                base.ItemAdding(properties);
            }
            this.EventFiringEnabled = true;

            NLLogger.OutputLog(LogLevel.Debug, "End item adding event", null);
        }
        #endregion

        #region ItemAdded
        //
        // Summary:
        //     Asynchronous After event that occurs after a new item has been added to its
        //     containing object.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPItemEventProperties object that represents properties
        //     of the event handler.
        public override void ItemAdded(SPItemEventProperties properties)
        {
            this.EventFiringEnabled = false;

            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemAdded Get list: ", null, ex);
            }
            try
            {
                if (list != null)
                {
                    SPWeb web = properties.Web;
                    string loginName = web.CurrentUser.LoginName;
                    string sid = web.CurrentUser.Sid;
                    if (string.IsNullOrEmpty(sid))
                        sid = UserSid.GetUserSid(loginName);
                    if (string.IsNullOrEmpty(sid))
                        sid = loginName;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                        (loginName, web.Url, ref PrincipalUser);

                    SPBaseType baseType = list.BaseType;
                    //string beforeUrl = properties.BeforeUrl;
                    string afterUrl = properties.AfterUrl;
                    SPListItem item = properties.ListItem;

               		PreFilterModule.setSource(loginName, sid, clientIpAddr, web.Url + "/" + afterUrl);
                    var args = new EventHandlerEventArgs(properties, _currentContext);
                    EventHelper.Instance.OnBeforeEventExecuting(this, args);
                    PreFilterModule.releaseSource();
                    if (args.Cancel)
                    {
                        this.EventFiringEnabled = true;
                        return;
                    }

                    if (baseType == SPBaseType.DocumentLibrary && item != null)
                    {
                        #region add prefilter
                        string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Upload);
                        var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                        if (noMatch)
                        {
                            //donnot query pc,
                            NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                            base.ItemAdded(properties);
                            this.EventFiringEnabled = true;
                            return;
                        }
                        #endregion

                        ItemVersionControl.PostCAFun = PostContentAnalysisEventHandler;
                        ItemVersionControl.OnItemAdded(properties, web, list, item, PrincipalUser, clientIpAddr);
                    }

                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemAdded:", null, ex);
            }
            finally
            {
                base.ItemAdded(properties);
                this.EventFiringEnabled = true;
            }
        }
        #endregion

        #region ItemFileMoving
        /// <summary>
        /// Occurs when a file is being moved.
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemFileMoving(SPItemEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemFileMoving Get list:", null, ex);
            }
            if (list != null)
            {
                try
                {
                    SPWeb web = properties.Web;
                    string url = web.Url;
                    SPUser user = web.CurrentUser;
                    string loginName = user.LoginName;
                    string sid = user.Sid;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                        (loginName, web.Url, ref PrincipalUser);
                    SPBaseType baseType = list.BaseType;
                    string beforeUrl = properties.BeforeUrl;
                    string afterUrl = properties.AfterUrl;
                    SPItemEventDataCollection afterProp = properties.AfterProperties;
                    SPListItem listItem = properties.ListItem;

                    if (listItem == null)
                    {
                        listItem = Globals.ParseItemFromAttachmentURL(web, url + '/' + beforeUrl);
                        if (listItem == null)
                        {
                            this.EventFiringEnabled = true;
                            return;
                        }
                    }

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc,
                        NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                        base.ItemFileMoving(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion

                    string[] extraAttrs = {
                        CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                        listItem.Name,
                        CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                        Globals.GetItemCreatedBySid(listItem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                        Globals.GetItemModifiedBySid(listItem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                        Globals.GetItemCreatedStr(listItem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                        Globals.GetItemModifiedStr(listItem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                        Globals.GetItemFileSizeStr(listItem)
                    };
                    string[] afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs, baseType, list.Fields);
                    string[] emptyArray = new string[0];
                    string policyName = null;
                    string policyMessage = null;
                    CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
                    //Fix bug 8222, replace the "created" and "modified" properties
                    afterPropArray = Globals.ReplaceHashTime(web, list, listItem, afterPropArray);
                    //Fix bug 8694 and 8692,add spfield attr to tailor
                    afterPropArray = Globals.BuildAttrArray2FromSPField(web, list, listItem, afterPropArray);
                    if (baseType == SPBaseType.UnspecifiedBaseType)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "ItemFileMoving: Unsupported list type:[{0}], LoginName:[{1}], ClientIp:[{2}]", new object[] { baseType, url, loginName, clientIpAddr });
                    }
                    else if (baseType == SPBaseType.DocumentLibrary &&
                        (beforeUrl.Substring(beforeUrl.LastIndexOf('/') + 1) ==
                         afterUrl.Substring(afterUrl.LastIndexOf('/') + 1)))
                    {
                        // Web Folder -> Move (document, same list)
                        // Call eval.
                        response = Globals.CallEval(CETYPE.CEAction.Write,
                                                    url + "/" + beforeUrl,
                                                    url + "/" + afterUrl,
                                                    ref afterPropArray,
                                                    ref afterPropArray,
                                                    clientIpAddr,
                                                    loginName,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    null,
                                                    null,
                                                    Globals.EventHandlerName,
                                                    CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                    web,
                                                PrincipalUser);
                    }
                    else if ((baseType == SPBaseType.DocumentLibrary) &&
                        (beforeUrl.Substring(beforeUrl.LastIndexOf('/') + 1) !=
                         afterUrl.Substring(afterUrl.LastIndexOf('/') + 1)))
                    {
                        // Edit Properties -> Rename Document
                        // Web Folder -> Rename Document
                        // Call eval.
                        // Since the PF in SharePoint doesn't support
                        // CE_ACTION_RENAME, we use CE_ACTION_WRITE instead.
                        response = Globals.CallEval(CETYPE.CEAction.Write,
                                                    url + "/" + beforeUrl,
                                                    null,
                                                    ref afterPropArray,
                                                    ref emptyArray,
                                                    clientIpAddr,
                                                    loginName,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    null,
                                                    null,
                                                    Globals.EventHandlerName,
                                                    CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                    web,
                                                PrincipalUser);
                    }
                    if (response == CETYPE.CEResponse_t.CEAllow)
                    {
                        base.ItemFileMoving(properties);
                    }
                    else
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemFileMoving:", null, ex);
                }
            }
            else
            {
                base.ItemFileMoving(properties);
            }
            this.EventFiringEnabled = true;
        }
        #endregion

        #region ItemAttachmentAdding
        /// <summary>
        /// Occurs when a file is being attached to an item.
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemAttachmentAdding(SPItemEventProperties properties)
        {
            NLLogger.OutputLog(LogLevel.Debug, "Begin item attachment adding event, BeforeUrl:[{0}], AfterUrl:[{1}]", new object[] { properties.BeforeUrl, properties.AfterUrl });

            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                list = properties.List;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ItemAttachmentAdding Get list:", null, ex);
            }
            if (list != null)
            {
                try
                {
                    SPWeb web = properties.Web;
                    string url = web.Url;
                    SPUser user = web.CurrentUser;
                    string loginName = user.LoginName;
                    string sid = user.Sid;
                    IPrincipal PrincipalUser = null;
                    string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                        (loginName, web.Url, ref PrincipalUser);

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Attach);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc,
                        NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                        base.ItemAttachmentAdding(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion

                    SPBaseType baseType = list.BaseType;
                    string beforeUrl = properties.BeforeUrl;
                    string afterUrl = properties.AfterUrl;
                    SPItemEventDataCollection afterProp = properties.AfterProperties;
                    SPListItem listitem = properties.ListItem;
                    string[] extraAttrs = {
                        CETYPE.CEAttrKey.CE_ATTR_SP_NAME,
                        listitem.Name,
                        CETYPE.CEAttrKey.CE_ATTR_SP_TITLE,
                        listitem.Title,
                        CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY,
                        Globals.GetItemCreatedBySid(listitem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY,
                        Globals.GetItemModifiedBySid(listitem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED,
                        Globals.GetItemCreatedStr(listitem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED,
                        Globals.GetItemModifiedStr(listitem),
                        CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE,
                        Globals.GetItemFileSizeStr(listitem)
                    };
                    string[] afterPropArray = Globals.BuildAttrArrayFromItemEventProperties(afterProp, extraAttrs, baseType, list.Fields);
                    string[] emptyArray = new string[0];
                    string policyName = null;
                    string policyMessage = null;

                    //Fix bug 8222, replace the "created" and "modified" properties
                    afterPropArray = Globals.ReplaceHashTime(web, list, listitem, afterPropArray);
                    //Fix bug 8694 and 8692,add spfield attr to tailor
                    afterPropArray = Globals.BuildAttrArray2FromSPField(web, list, listitem, afterPropArray);
                    if (baseType == SPBaseType.UnspecifiedBaseType)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "ItemAttachmentAdding: Unsupported list type:{0}, url:[{1}], loginName:[{2}], clientIp:[{3}]", new object[] { baseType, url, loginName, clientIpAddr });
                    }
                    else if (baseType != SPBaseType.DocumentLibrary &&
                             (!string.IsNullOrEmpty(beforeUrl)) &&
                             (!string.IsNullOrEmpty(afterUrl)) &&
                             beforeUrl == afterUrl)
                    {
                        // List Item -> Edit -> Attach
                        string itemUrl = Globals.ConstructListUrl(web, list) + "/" + listitem.Title;

                        #region save file to server
                        string strUploadFileName = properties.AfterUrl.Substring(properties.AfterUrl.LastIndexOf("/") + 1);
                        string strLocalFileFullPath = SaveFileToLoaclServerDuringUploading(_currentContext, properties);
                        #endregion

                        #region do pre-authorization for uploading
                        List<KeyValuePair<string, string>> lsPrepareExtralAttributes = new List<KeyValuePair<string, string>>();
                        Globals.DoPreAuthorizationForUpload(properties, strLocalFileFullPath, itemUrl, action, ref lsPrepareExtralAttributes);
                        Globals.MyDeleteFile(strLocalFileFullPath);
                        #endregion

                        Globals.SetListPairInfoIntoArray(ref afterPropArray, lsPrepareExtralAttributes);
                        /// Call eval.
                        CETYPE.CEResponse_t response = Globals.CallEval(CETYPE.CEAction.Attach,
                                                    itemUrl,
                                                    null,
                                                    ref afterPropArray,
                                                    ref emptyArray,
                                                    clientIpAddr,
                                                    loginName,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    null,
                                                    null,
                                                    Globals.EventHandlerName,
                                                    CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                    web,
                                                PrincipalUser);

                        if (response == CETYPE.CEResponse_t.CEAllow)
                        {
                            base.ItemAttachmentAdding(properties);
                        }
                        else
                        {
                            // The attachment adding event is ordered by user upload order
                            // If return error and canceled the operation, the attachments which after current attachment will not continue upload
                            // properties.Status = SPEventReceiverStatus.CancelWithError;
                            properties.Status = SPEventReceiverStatus.CancelNoError;
                            properties.ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, "Attachment deny:" + policyMessage + "\nAttachment name: " + strUploadFileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemAttachmentAdding:", null, ex);
                }
            }
            else
            {
                base.ItemAttachmentAdding(properties);
            }
            this.EventFiringEnabled = true;

            NLLogger.OutputLog(LogLevel.Debug, "End item attachment adding event", null);
        }
        #endregion

        #region ItemAttachmentAdded
        //
        // Summary:
        //     Asynchronous after event that occurs after a user adds an attachment to an
        //     item.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPItemEventProperties object that represents properties
        //     of the event handler.
        public override void ItemAttachmentAdded(SPItemEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;
            SPList list = null;
            try
            {
                SPWeb web = properties.Web;
                string loginName = web.CurrentUser.LoginName;
                //string sid = web.CurrentUser.Sid;
                IPrincipal PrincipalUser = null;
                string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                    (loginName, web.Url, ref PrincipalUser);
                list = properties.List;
                if (!Globals.CheckListProperty(list, Globals.strLibraryProcessUploadPropName))
                {
                    this.EventFiringEnabled = true;
                    return;
                }
                SPBaseType baseType = list.BaseType;
                //string beforeUrl = properties.BeforeUrl;
                string afterUrl = properties.AfterUrl;
                SPListItem item = properties.ListItem;

                if (baseType != SPBaseType.DocumentLibrary)
                {

                    #region add prefilter
                    string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Upload);
                    var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
                    if (noMatch)
                    {
                        //donnot query pc,
                        NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                        base.ItemAttachmentAdded(properties);
                        this.EventFiringEnabled = true;
                        return;
                    }
                    #endregion

                    string fileUrl = web.Url + "/" + afterUrl;

                    FileContentAnalysis fileCA = new FileContentAnalysis(item, fileUrl, clientIpAddr, PrincipalUser,false);
                    fileCA.PostContentAnalysisEventHandler += new PostContentAnalysisEventDelegate(PostContentAnalysisEventHandler);
                    fileCA.Run();
					if (fileCA.CADenied)
					{
						properties.Status = SPEventReceiverStatus.CancelWithError;
						properties.ErrorMessage = Globals.EnforcementMessage;
					}
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during ItemAttachmentAdded:", null, ex);
            }
            this.EventFiringEnabled = true;
        }
        #endregion

        #region ItemEventLog
        /// <summary>
        /// Log the current Event type and information.
        /// </summary>
        /// <param name="properties"></param>
        public void ItemEventLog(string EventType,
                                 SPItemEventProperties properties)
        {
        }
        #endregion

        #region Independence tools
        static public void WriteAuditEvent(SPListItem spListItem, string strMsg)
        {
            string xmlData = "<CAResult>";
            xmlData += "<User>";
            xmlData += Utilities.ConvertToXmlString(spListItem.ParentList.ParentWeb.CurrentUser.LoginName);
            xmlData += "</User>";
            xmlData += "<ItemName>";
            if (String.IsNullOrEmpty(spListItem.Name))
                xmlData += Utilities.ConvertToXmlString(spListItem.Title);
            else
                xmlData += Utilities.ConvertToXmlString(spListItem.Name);
            xmlData += "</ItemName>";
            xmlData += "<FileUrl>";
            xmlData += Utilities.ConvertToXmlString(spListItem.Url);
            xmlData += "</FileUrl>";

            xmlData += "<Modified>";
            xmlData += Utilities.ConvertToXmlString(strMsg);
            xmlData += "</Modified>";

            xmlData += "<Failed></Failed></CAResult>";

            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                spListItem.ParentList.Audit.WriteAuditEvent(SPAuditEventType.Custom, "ContentAnalysis", xmlData);
            });
        }

        static public void PostContentAnalysisEventHandler(object sender, EventArgs e)
        {
            FileContentAnalysis fileCA = sender as FileContentAnalysis;
            if (fileCA.Result.FailedReason.Count != 0 || fileCA.Result.ModifiedFields.Count != 0)
            {
                string xmlData = "<CAResult>";
                xmlData += "<User>";
                xmlData += Utilities.ConvertToXmlString(fileCA.Result.ListItem.ParentList.ParentWeb.CurrentUser.LoginName);
                xmlData += "</User>";
                xmlData += "<ItemName>";
                if (String.IsNullOrEmpty(fileCA.Result.ListItem.Name))
                    xmlData += Utilities.ConvertToXmlString(fileCA.Result.ListItem.Title);
                else
                    xmlData += Utilities.ConvertToXmlString(fileCA.Result.ListItem.Name);
                xmlData += "</ItemName>";
                xmlData += "<FileUrl>";
                xmlData += Utilities.ConvertToXmlString(fileCA.Result.FileUrl);
                xmlData += "</FileUrl>";

                xmlData += "<Modified>";
                if (fileCA.Result.ModifiedFields.Count > 0)
                {
                    xmlData += "Succeeded to set ";
                    int count = 0;
                    foreach (KeyValuePair<string, string> keyValue in fileCA.Result.ModifiedFields)
                    {
                        xmlData += Utilities.ConvertToXmlString(keyValue.Key);
                        xmlData += "=";
                        xmlData += Utilities.ConvertToXmlString(keyValue.Value);
                        count++;
                        if (count != fileCA.Result.ModifiedFields.Count)
                            xmlData += ",";
                    }
                    xmlData += ". ";
                }
                xmlData += "</Modified>";

                xmlData += "<Failed>";
                if (fileCA.Result.FailedReason.Count > 0)
                {
                    Int32 count = 1;
                    foreach (string failed in fileCA.Result.FailedReason)
                    {
                        xmlData += count.ToString() + ". ";
                        xmlData += Utilities.ConvertToXmlString(failed);
                        xmlData += " ";
                        count++;
                    }
                }
                xmlData += "</Failed>";

                xmlData += "</CAResult>";

                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    fileCA.Result.ListItem.ParentList.Audit.WriteAuditEvent(SPAuditEventType.Custom, "ContentAnalysis", xmlData);
                });

                NLLogger.OutputLog(LogLevel.Debug, "Content Analysis for Uploading: Audit XML Data=" + xmlData);
            }
        }

#if false
        // For item adding, updating and attachment updating events
        private static string SaveFileToLoaclServerDuringUploading(HttpContext obHttpContext, string strBeforeUrl, params List<string>[] szListUrlContansFlags)
        {
            List<List<string>> lsFlags = null;
            if (null == szListUrlContansFlags)
            {
                lsFlags = null;
            }
            else
            {
                lsFlags = new List<List<string>>();
                foreach (List<string> lsGroup in szListUrlContansFlags)
                {
                    lsFlags.Add(lsGroup);
                }
            }
            return SaveFileToLoaclServerDuringUploadingWithUrlCheckFlags(obHttpContext, strBeforeUrl, lsFlags);
        }
        // Between the files, it is logic or
        private static string SaveFileToLoaclServerDuringUploading(HttpContext obHttpContext, string strBeforeUrl, params string[] szUrlContansFlags)
        {
            List<List<string>> lsFlags = null;
            if (null == szUrlContansFlags)
            {
                lsFlags = null;
            }
            else
            {
                lsFlags = new List<List<string>>();
                foreach (string strItem in szUrlContansFlags)
                {
                    lsFlags.Add(new List<string>() { strItem });
                }
            }
            return SaveFileToLoaclServerDuringUploadingWithUrlCheckFlags(obHttpContext, strBeforeUrl, lsFlags);
        }
        // lsUrlContansFlags between groups is or, inner group is and
        private static string SaveFileToLoaclServerDuringUploadingWithUrlCheckFlags(HttpContext obHttpContext, string strBeforeUrl, IEnumerable<List<string> > lsUrlContansFlags)
        {
            if ((null == obHttpContext) || (null == obHttpContext.Request) || (null == obHttpContext.Request.Url) || (String.IsNullOrEmpty(strBeforeUrl)))
            {
                NLLogger.OutputLog(LogLevel.Debug, "Parameters error, one the need parameters is null, cannot save files, Content:[{0}], Url:[{1}]", new object[] { obHttpContext, strBeforeUrl });
                return null;
            }

            string strFilePathRet = "";
            NLLogger.OutputLog(LogLevel.Debug, "Request.Url:[{0}] during save file to local server for enforcement", new object[] { obHttpContext.Request.Url });

            // if (obHttpContext.Request.Url.ToString().Contains("upload.aspx") || obHttpContext.Request.Url.ToString().Contains("UploadEx.aspx"))
            bool bFlagMatched = CheckIfStringContainsFlags(obHttpContext.Request.Url.ToString(), lsUrlContansFlags);
            if (bFlagMatched)
            {
                string strCurFileName = strBeforeUrl.Substring(strBeforeUrl.LastIndexOf("/") + 1);
                strFilePathRet = InnerSaveFileToLoaclServerDuringUploading(obHttpContext, strCurFileName);
            }
            return strFilePathRet;
        }
#endif
        private static string SaveFileToLoaclServerDuringUploading(HttpContext obHttpContext, SPItemEventProperties obSPItemEventProperties)
		{
            string strFilePathRet = "";
            try
			{
                if ((null == obHttpContext) || (null == obSPItemEventProperties))
				{
                    NLLogger.OutputLog(LogLevel.Debug, "Parameters error, one the need parameters is null, cannot save files, Content:[{0}], ItemEventProperties:[{1}]", new object[] { obHttpContext, obSPItemEventProperties });
                }
                else
				{
                    HashSet<string> setFileNames = new HashSet<string>();
                    
                    try
                    {
                        string strFileNameFromBeforeUrl = GetItemNameFromItemBeforeOrAfterUrl(obSPItemEventProperties.BeforeUrl);
                        if (!String.IsNullOrEmpty(strFileNameFromBeforeUrl))
                        {
                            setFileNames.Add(strFileNameFromBeforeUrl);
                        }

                        string strFileNameFromAfterUrl = GetItemNameFromItemBeforeOrAfterUrl(obSPItemEventProperties.AfterUrl);
                        if ((!String.IsNullOrEmpty(strFileNameFromBeforeUrl)) && (!setFileNames.Contains(strFileNameFromAfterUrl)))
                        {
                            setFileNames.Add(strFileNameFromAfterUrl);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }

                    foreach (string strItemFileName in setFileNames)
					{
						if (String.IsNullOrEmpty(strItemFileName))
						{
                            // Continue
						}
                        else
						{
							strFilePathRet = InnerSaveFileToLoaclServerDuringUploading(obHttpContext, strItemFileName);
                            if (!String.IsNullOrEmpty(strFilePathRet))
							{
                                break;
							}
						}
					}
                    NLLogger.OutputLog(LogLevel.Debug, "Saved file before url:[{0}] after url:[{1}] with local path:[{2}]", new object[] { obSPItemEventProperties.BeforeUrl, obSPItemEventProperties.AfterUrl, strFilePathRet });
                }
			}
            catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Debug, "Exception during try to save file to local server, Content:[{0}], ItemEventProperties:[{1}]", new object[] { obHttpContext, obSPItemEventProperties }, ex);
			}
			return strFilePathRet;
        }

        private static string InnerSaveFileToLoaclServerDuringUploading(HttpContext obHttpContext, string strCurFileName)
        {
			string strFilePathRet = "";
			try
			{
				NLLogger.OutputLog(LogLevel.Debug, "Request.Url:[{0}], current file name:[{1}] during save file to local server for enforcement", new object[] { obHttpContext.Request.Url, strCurFileName });

				HttpFileCollection obFileCollection = obHttpContext.Request.Files;
				if (null == obFileCollection)
				{
					NLLogger.OutputLog(LogLevel.Debug, "Failed save file to local server, Request.Url:[{0}], current file name:[{1}] during save file to local server for enforcement, file collection object is null", new object[] { obHttpContext.Request.Url, strCurFileName });
				}
				else
				{
					HttpPostedFile obPostedFile = null;
					NLLogger.OutputLog(LogLevel.Debug, "Begin get current file:[{0}] object from file collection", new object[] { strCurFileName });
					for (int i = 0; i < obFileCollection.Count; ++i)
					{
						//sometimes,when user upload one file,the file name is full path,e.g. C:\xx\xx\xx\xx\ATO_3.docx.xml,we need the actual file name
						string strItemFileName = Globals.GetFileName(obFileCollection[i]?.FileName);
                        NLLogger.OutputLog(LogLevel.Debug, "Current item file:[{0}], pass in specify file:[{1}]", new object[] { strItemFileName, strCurFileName });
                        if (String.IsNullOrEmpty(strItemFileName))
						{
                            // Continue
						}
                        else
						{
							if (strItemFileName.Equals(strCurFileName, StringComparison.OrdinalIgnoreCase))
							{
								obPostedFile = obFileCollection[i];
								NLLogger.OutputLog(LogLevel.Debug, "get share point file object from array");
								break;
							}
						}
					}

					if (null == obPostedFile)
					{
						NLLogger.OutputLog(LogLevel.Info, "Cannot find the file:[{0}] object, cannot download to do pre-authorization", new object[] { strCurFileName });
					}
					else
					{
						// Using CommonApplicationData, the user temp folder need run as RunWithElevatedPrivileges
						string strTempFolderPath = GetSPEApplicationTempFileFolder();
						strFilePathRet = strTempFolderPath + Guid.NewGuid() + strCurFileName;

                        obPostedFile.SaveAs(strFilePathRet);
					}

				}
			}
			catch (Exception ex)
			{
				strFilePathRet = "";
				NLLogger.OutputLog(LogLevel.Debug, "Exception during save file to local server:", null, ex);
			}
			return strFilePathRet;
		}


        private static string GetItemNameFromItemBeforeOrAfterUrl(string strBeforeOrAfterUrl)
		{
            string strItemName = "";
			if (!String.IsNullOrEmpty(strBeforeOrAfterUrl))
			{
                strItemName = strBeforeOrAfterUrl.Substring(strBeforeOrAfterUrl.LastIndexOf("/") + 1);
			}
            return strItemName;
		}
        private static void MakeStandardFolderPath(ref string strFolderPathRef)
        {
            if (!String.IsNullOrEmpty(strFolderPathRef))
            {
                if ('\\' == strFolderPathRef[strFolderPathRef.Length-1])
                {
                    // OK
                }
                else
                {
                    strFolderPathRef += '\\';
                }
            }
        }
        // lsUrlContansFlags between groups is or, inner group is and
        private static bool CheckIfStringContainsFlags(string strSourceInfo, IEnumerable<List<string>> lsFlags)
        {
            bool bFlagMatchedRet = false;
            if (null == lsFlags)
            {
                bFlagMatchedRet = true;
            }
            else
            {
                if (String.IsNullOrEmpty(strSourceInfo))
                {
                    bFlagMatchedRet = false;
                }
                else
                {
                    bFlagMatchedRet = false;
                    foreach (List<string> lsGroup in lsFlags)
                    {
                        // Inner group, logic and
                        bool bGroupItemAllMatched = true;
                        foreach (string strItem in lsGroup)
                        {
                            if (strSourceInfo.Contains(strItem))
                            {
                                // OK, continue
                            }
                            else
                            {
                                // Failed, break
                                bGroupItemAllMatched = false;
                                break;
                            }
                        }

                        // Between group, logic or
                        if (bGroupItemAllMatched)
                        {
                            bFlagMatchedRet = true;
                            break;
                        }
                        else
                        {
                            // Current group do not matched, continue to check next one
                        }
                    }
                }
            }
            return bFlagMatchedRet;
        }
        private static string GetSPEApplicationTempFileFolder()
        {
            return GetSPEApplicationFolder("TempFiles", true);
        }
        private static string GetSPEApplicationFolder(string strSubFolderName, bool bForceMakeFolderExist)
        {
            string strTempFolderPath = Environment.GetFolderPath(SpecialFolder.CommonApplicationData);
            MakeStandardFolderPath(ref strTempFolderPath);
            strTempFolderPath += "Nextlabs\\SPE\\" + strSubFolderName;

            if (bForceMakeFolderExist)
            {
                if (!Directory.Exists(strTempFolderPath))
                {
                    Directory.CreateDirectory(strTempFolderPath);
                }
            }

            return strTempFolderPath;
        }
#endregion
    }
}
