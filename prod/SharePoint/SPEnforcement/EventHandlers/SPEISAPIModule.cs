using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using NextLabs.Common;
using Microsoft.SharePoint;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint.WebControls;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public class SPE_AUTHOR_DLL_Module : SPEModuleBase
    {
        public static string FileDecode(string file)
        {
            string decodeFile = file;
            decodeFile = decodeFile.Replace("\\[", "[");
            decodeFile = decodeFile.Replace("\\]", "]");
            decodeFile = decodeFile.Replace("\\;", ";");
            decodeFile = decodeFile.Replace("\\=", "=");
            return decodeFile;
        }

        public override bool DoSPEProcess()
        {
            if (HttpContext.Current.Request.Headers["X_READ"] != "1")
            {
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                String _authorMethod = m_Request.Form["method"];
                String _authorDocName = m_Request.Form["document_name"];
                _authorDocName = Globals.UrlDecode(_authorDocName);
                _authorDocName = FileDecode(_authorDocName);
                String _authorOldUrl = m_Request.Form["oldUrl"];
                String _authorNewUrl = m_Request.Form["newUrl"];
                String _authorDoCopy = m_Request.Form["docopy"];
                //fix bug 8569&8570 by derek
                String _authorService = m_Request.Form["service_name"];
                String _authorURLList = m_Request.Form["url_list"];
                String _initialUrl = m_Request.Form["initialUrl"];
                String _authorDoc = ""; // fix bug 8444 by derek

                // fix bug 8813 & 8853 to disable "put document"
                // fix bug 8444 by derek
                if (String.IsNullOrEmpty(_authorMethod))
                {
                    Stream _inputStream = m_Request.InputStream;
                    byte[] ContentBuffer = new byte[1024];
                    int _readlen = 0;

                    long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
                    _readlen = _inputStream.Read(ContentBuffer, 0, 1023);
                    _inputStream.Seek(_oldPos, SeekOrigin.Begin);

                    String _strContent = Globals.UrlDecode(Encoding.UTF8.GetString(ContentBuffer));
                    int _methodLen = _strContent.IndexOf('\n') - _strContent.IndexOf("method");
                    String _strMethod = "";
                    if (_methodLen > 0)
                        _strMethod = _strContent.Substring(_strContent.IndexOf("method"), _methodLen);
                    else
                        _strMethod = _strContent.Substring(_strContent.IndexOf("method"));

                    char[] _delemiters = { '&' };
                    String[] _arrayContent = _strMethod.Split(_delemiters);
                    NameValueCollection _methodCollection = new NameValueCollection();
                    int _seperatorPos = 0;
                    for (int i = 0; i < _arrayContent.GetLength(0); i++)
                    {
                        _seperatorPos = _arrayContent[i].IndexOf('=');
                        _methodCollection.Add(((String)_arrayContent.GetValue(i)).Substring(0, _seperatorPos),
                            ((String)_arrayContent.GetValue(i)).Substring(_seperatorPos + 1));
                    }

                    _authorMethod = _methodCollection["method"];
                    _authorService = _methodCollection["service_name"];
                    _authorDoc = _methodCollection["document"];

                }
                //fix bug 8144 to remove "\\"  by william
                int char_pos = -1;
                if (_authorOldUrl != null)
                {
                    _authorOldUrl = FileDecode(_authorOldUrl);
                }
                if (_authorNewUrl != null)
                {
                    _authorNewUrl = FileDecode(_authorNewUrl);
                }
                if (_authorService != null)
                {
                    char [] trimChars = new char[2];
                    trimChars[0]='[';
                    trimChars[1]=']';
                    _authorService = _authorService.Trim(trimChars);
                }
                if (_authorURLList != null)
                {
                    _authorURLList = _authorURLList.Trim('[');
                    for (; ((char_pos = _authorURLList.IndexOf("]\n")) != -1); )
                    {
                        _authorURLList = _authorURLList.Remove(char_pos, 1);
                    }
                }

                // George: Add the check out document case.
                if (_authorMethod != null && !String.IsNullOrEmpty(_authorDocName)
                     && (_authorMethod.StartsWith("get document", StringComparison.OrdinalIgnoreCase)
						|| _authorMethod.StartsWith("checkout document", StringComparison.OrdinalIgnoreCase)
                        || _authorMethod.StartsWith("uncheckout document", StringComparison.OrdinalIgnoreCase)))
                {
                    //it is opening documents using Explorer View, external app, or remote client.
                    //Remove any leading slash.

                    if (_authorDocName.StartsWith("/"))
                    {
                        _authorDocName = _authorDocName.Substring(1);
                    }
                    try
                    {
                        object obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _authorDocName);
                        if(obj != null && obj is SPListItem)
                        {
                            _SPEEvalAttr.ItemObj = obj as SPListItem;
                        }
                    }
                    catch
                    {
                    }
                    if (_SPEEvalAttr.ItemObj == null)
                    {
                        _SPEEvalAttr.ItemObj = Globals.ParseItemFromAttachmentURL(_SPEEvalAttr.WebObj, _SPEEvalAttr.WebUrl + '/' + _authorDocName);
                    }
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;

                    //Document lib item route
                    if (_SPEEvalAttr.ItemObj != null)
                    {
                        _SPEEvalAttr.Action = "READ DOCLIB ITEM";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                    }
                    //List item attachment route.Fix bug 8968, added by William 20090316
                    else
                    {
                        SPFile _fileObj = null;
                        try
                        {
                            _fileObj = (SPFile)_SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _authorDocName);
                        }
                        catch
                        {
                        }
                        if (_fileObj != null && _fileObj.Exists)
                        {
                            _SPEEvalAttr.Action = "READ LIST ITEM ATTACHMENT";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' +_authorDocName;
                            _SPEEvalAttr.ObjName = _fileObj.Name;
                            _SPEEvalAttr.ObjTitle = _fileObj.Title;
                        }
                    }
                    if (_authorMethod.StartsWith("checkout document", StringComparison.OrdinalIgnoreCase)
                        || _authorMethod.StartsWith("uncheckout document", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.Action = "CHECK OUT LIST ITEM";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                    }
                }
                else if ((_authorMethod != null &&
                             _authorMethod.StartsWith("move document",
                             StringComparison.OrdinalIgnoreCase)) &&
                             !String.IsNullOrEmpty(_authorOldUrl) &&
                             !String.IsNullOrEmpty(_authorNewUrl))
                {
                    // it is "Explorer View -> Rename".
                    // Since the PF in SharePoint doesn't support
                    // CE_ACTION_RENAME, we use CE_ACTION_WRITE instead.

                    // Derek bug 8612. Cut/Paste & Rename will send "move document"
                    Object _obj = null;
                    try
                    {
                        _obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _authorOldUrl);
                    }
                    catch
                    {
                    }

                    if (_obj != null)
                    {

                        if (!String.IsNullOrEmpty(_authorDoCopy) && _authorDoCopy.StartsWith("true", StringComparison.OrdinalIgnoreCase))
                        {
                            _SPEEvalAttr.Action = "COPY DOCUMENT";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        }
                        else
                        {
                            // Rename or Move File
                            _SPEEvalAttr.Action = "MOVE DOCUMENT";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                        }
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' + _authorOldUrl;
                        _SPEEvalAttr.ObjTargetUrl = _SPEEvalAttr.WebUrl + '/' + _authorNewUrl;

                        // fix bug 8665 by derek
                        if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFolder)))
                        {
                            _SPEEvalAttr.Action = "MOVE LIST";
                            _SPEEvalAttr.ObjName = ((SPFolder)_obj).Name;

                            if ((int)((SPFolder)_obj).Properties["vti_docstoretype"] == 1)
                            {
                                // Can't get Title/Description for SPFolder(List, Document Library)
                                _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                                //In fact, we can get the Title/Description for SPFolder(List, Document Library, added by William 20090317
                                try
                                {
                                    _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlList);
                                }
                                catch
                                {
                                }
                                if (_SPEEvalAttr.ListObj != null)
                                {
                                    _SPEEvalAttr.ObjDesc = CommonVar.GetSPListContent(_SPEEvalAttr.ListObj, "description");
                                    _SPEEvalAttr.ObjTitle = CommonVar.GetSPListContent(_SPEEvalAttr.ListObj, "title");
                                }

                                if (((SPFolder)_obj).ContainingDocumentLibrary.Equals(Guid.Empty))
                                {
                                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                                }
                                else
                                {
                                    _SPEEvalAttr.Action = "MOVE LIBRARY";
                                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                                }

                            }
                            else if ((int)((SPFolder)_obj).Properties["vti_docstoretype"] == 2)
                            {
                                // SITE
                                _SPEEvalAttr.Action = "MOVE SITE";
                                _SPEEvalAttr.ObjName = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "title");
                                _SPEEvalAttr.ObjDesc = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "description");

                                _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                                _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                            }
                            //Fix bug 8964, as move is not detected in HttpModule,we must change it to Create/Edit,Added by William 20090317
                            if (_SPEEvalAttr.ObjEvalUrl != null && _SPEEvalAttr.ObjTargetUrl != null)
                            {
                                if (_SPEEvalAttr.ObjEvalUrl.Substring(0, _SPEEvalAttr.ObjEvalUrl.LastIndexOf('/')) ==
                                    _SPEEvalAttr.ObjTargetUrl.Substring(0, _SPEEvalAttr.ObjTargetUrl.LastIndexOf('/')))
                                {
                                    _SPEEvalAttr.Action = "RENAME SPFOLDER";
                                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                }
                            }
                        }
                        else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFile)))
                        {
                            _SPEEvalAttr.ItemObj = Globals.ParseItemFromAttachmentURL(_SPEEvalAttr.WebObj, _SPEEvalAttr.WebUrl + '/' + _authorOldUrl);
                            if (_SPEEvalAttr.ItemObj == null)
                            {
                                _SPEEvalAttr.ObjName = ((SPFile)_obj).Name;
                            }
                            else
                            {
                                _SPEEvalAttr.ObjName = CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "name");
                                _SPEEvalAttr.ObjTitle = CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "title");
                            }

                            _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                        }
                        else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPListItem)))
                        {
                            _SPEEvalAttr.ObjName = ((SPListItem)_obj).Name;
                            _SPEEvalAttr.ObjTitle = ((SPListItem)_obj).Title;

                            _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                        }
                    }
                }
                //fix bug 8569&8570 by derek
                else if ((_authorMethod != null &&
                        _authorMethod.StartsWith("list documents",
                        StringComparison.OrdinalIgnoreCase)) &&
                    !_initialUrl.StartsWith("_sharedtemplates", StringComparison.OrdinalIgnoreCase))
                {
                    object obj = null;
                    try
                    {
                        obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _initialUrl);
                    }
                    catch
                    {
                    }
                    Type type = obj.GetType();
                    if (obj != null && Object.ReferenceEquals(type, typeof(SPFolder)))
                    {
                        SPFolder _folderObj = obj as SPFolder;
                        _SPEEvalAttr.Action = "READ LIST";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        if (!String.IsNullOrEmpty(_initialUrl))
                            _SPEEvalAttr.ObjEvalUrl = (_SPEEvalAttr.WebUrl + '/' + _initialUrl);
                        else
                            _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl;
                        _SPEEvalAttr.ObjName = _folderObj.Name;

                        if ((int)_folderObj.Properties["vti_docstoretype"] == 1)
                        {
                            // List/Document Library
                            // Failed to get Title/Description
                            _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                            //Fix bug 8999
                            //In fact, we can get the Title/Description for SPFolder(List, Document Library, added by William 20090318
                            try
                            {
                                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlList);
                            }
                            catch
                            {
                            }
                            if (_SPEEvalAttr.ListObj != null)
                            {
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                            }
                            if (!_folderObj.ContainingDocumentLibrary.Equals(Guid.Empty))
                            {
                                _SPEEvalAttr.Action = "READ LIBRARY";
                            }
                        }
                        else if ((int)_folderObj.Properties["vti_docstoretype"] == 2)
                        {
                            // SITE
                            _SPEEvalAttr.Action = "READ SITE";
                            if (_SPEEvalAttr.WebObj!=null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                        }
                    }
                    // Add document set and folder open in sharepoint designer.
                    else if (obj != null && Object.ReferenceEquals(type, typeof(SPListItem)))
                    {
                        _SPEEvalAttr.Action = "READ";
                        SPListItem item = obj as SPListItem;

                        if (item != null)
                            SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);

                    }
                }
                else if ((_authorMethod != null &&
                        _authorMethod.StartsWith("remove document",
                    StringComparison.OrdinalIgnoreCase)) &&
                    !String.IsNullOrEmpty(_authorURLList))
                {
                    Object _obj = null;
                    try
                    {
                        _obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _authorURLList);
                    }
                    catch
                    {
                    }

                    if (_obj != null)
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' +_authorURLList;

                        if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFolder)))
                        {

                            try
                            {
                                _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.Lists[((SPFolder)_obj).ParentListId];
                            }
                            catch
                            {
                            }
                            if ((int)((SPFolder)_obj).Properties["vti_docstoretype"] == 1)
                            {

                                if (_SPEEvalAttr.ListObj!=null)
                                {
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                                }
                                if (((SPFolder)_obj).ContainingDocumentLibrary.Equals(Guid.Empty))
                                {
                                    _SPEEvalAttr.Action = "DELETE LIST";
                                }
                                else
                                {
                                    _SPEEvalAttr.Action = "DELETE LIBRARY";
                                }
                            }
                        }
                        else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPFile)))
                        {
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                            _SPEEvalAttr.ItemObj = Globals.ParseItemFromAttachmentURL(_SPEEvalAttr.WebObj, _SPEEvalAttr.WebUrl + '/' + _authorURLList);

                            if (_SPEEvalAttr.ItemObj!=null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);

                        }
                        else if (Object.ReferenceEquals(_obj.GetType(), typeof(SPListItem)))
                        {
                            if (_SPEEvalAttr.ItemObj != null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        }
                    }
                }
                // fix bug 8444 by derek
                // George: fix bug 30172, "put+document" case in sharepoint designer create document.
                else if (_authorMethod != null && (_authorMethod.StartsWith("put document", StringComparison.OrdinalIgnoreCase)
                    || _authorMethod.StartsWith("put+document", StringComparison.OrdinalIgnoreCase))
                    && !String.IsNullOrEmpty(_authorDoc))
                {
                    // new office 2007 & create new document in Sharepoint Designer will send "put document" command
                    int _docNameLen = _authorDoc.IndexOf(';') - _authorDoc.IndexOf('=') - 1;
                    if (_docNameLen > 0)
                        _authorDocName = _authorDoc.Substring(_authorDoc.IndexOf('=') + 1, _docNameLen);
                    else
                        _authorDocName = _authorDoc.Substring(_authorDoc.IndexOf('=') + 1);

                    _SPEEvalAttr.Action = "CREATE DOCUMENT";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' + _authorDocName;

                    try
                    {
                        _SPEEvalAttr.ItemObj = (SPListItem)_SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.WebUrl + '/' + _authorDocName);
                    }
                    catch
                    {
                        _SPEEvalAttr.ItemObj = null;
                    }


                    if (_SPEEvalAttr.ItemObj != null)
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);

                }
                HttpContext.Current.Request.Headers["X_READ"] = "1";
                if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
                {
                    CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                    CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                    String[] _emptyArray = new string[0];
                    String[] _propertyArray = new string[5 * 2];
                    String _policyName = null;
                    String _policyMessage = null;
                    String _before_url = null;
                    String _after_url = null;
                    _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                    _propertyArray[0 * 2 + 1] = _SPEEvalAttr.ObjName;
                    _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                    _propertyArray[1 * 2 + 1] = _SPEEvalAttr.ObjTitle;
                    _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                    _propertyArray[2 * 2 + 1] = _SPEEvalAttr.ObjDesc;
                    _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE;
                    _propertyArray[3 * 2 + 1] = _SPEEvalAttr.ObjType;
                    _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE;
                    _propertyArray[4 * 2 + 1] = _SPEEvalAttr.ObjSubtype;

                    if (_SPEEvalAttr.ItemObj != null)
                    {
                        int oldLen = _propertyArray.Length;
                        string[] newArray = new string[oldLen + 5 * 2];
                        for (int i = 0; i < oldLen; i++)
                        {
                            newArray[i] = _propertyArray[i];
                        }

                        newArray[oldLen + 0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY;
                        newArray[oldLen + 0 * 2 + 1] = Globals.GetItemCreatedBySid(_SPEEvalAttr.ItemObj);
                        newArray[oldLen + 1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY;
                        newArray[oldLen + 1 * 2 + 1] = Globals.GetItemModifiedBySid(_SPEEvalAttr.ItemObj);
                        newArray[oldLen + 2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED;
                        newArray[oldLen + 2 * 2 + 1] = Globals.GetItemCreatedStr(_SPEEvalAttr.ItemObj);
                        newArray[oldLen + 3 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED;
                        newArray[oldLen + 3 * 2 + 1] = Globals.GetItemModifiedStr(_SPEEvalAttr.ItemObj);
                        newArray[oldLen + 4 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE;
                        newArray[oldLen + 4 * 2 + 1] = Globals.GetItemFileSizeStr(_SPEEvalAttr.ItemObj);

                        _propertyArray = newArray;

                        // Add other fixed and custom item attributes to the array.
                        _propertyArray = Globals.BuildAttrArrayFromItemProperties
                            (_SPEEvalAttr.ItemObj.Properties, _propertyArray,
                            _SPEEvalAttr.ItemObj.ParentList.BaseType, _SPEEvalAttr.ItemObj.Fields);
                        //Fix bug 8222, replace the "created" and "modified" properties
                        _propertyArray = Globals.ReplaceHashTime(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, _propertyArray);
                        //Fix bug 8694 and 8692,add spfield attr to tailor
                        _propertyArray = Globals.BuildAttrArray2FromSPField(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, _propertyArray);
                    }
                    _response = Globals.CallEval(_SPEEvalAttr.PolicyAction,
                                                _SPEEvalAttr.ObjEvalUrl,
                                                _SPEEvalAttr.ObjTargetUrl,  // derek bug8612
                                                ref _propertyArray,
                                                ref _emptyArray,
                                                _SPEEvalAttr.RemoteAddr,
                                                _SPEEvalAttr.LogonUser,
                                                _SPEEvalAttr.WebObj.CurrentUser.Sid,
                                                ref _policyName,
                                                ref _policyMessage,
                                                _before_url,
                                                _after_url,
                                                Globals.HttpModuleName,
                                                _NoiseLevel,
                                                _SPEEvalAttr.WebObj,
                                                null);
                    if (_response == CETYPE.CEResponse_t.CEDeny)
                    {
                        NextLabs.Common.Utilities.SetDenyRequestHeader(HttpContext.Current.Request, _policyName, _policyMessage);
                    }
                    HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                    return true;
                }
                HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                return true;
            }
            return false;
        }
    }

    public class SPE_ADMIN_DLL_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            if (HttpContext.Current.Request.Headers["X_READ"] != "1")
            {
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                String _authorMethod = m_Request.Form["method"];
                String _authorService = m_Request.Form["service_name"];

                if ((_authorMethod != null &&
                     _authorMethod.StartsWith("create service", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(_authorService))
                {
                    _SPEEvalAttr.Action = "CREATE SITE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;

                    if (_authorService.LastIndexOf('/') < 0)
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService;
                    else
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService.Substring(_authorService.LastIndexOf('/') + 1);

                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' + _SPEEvalAttr.ObjName;
                    try
                    {
                        _SPEEvalAttr.SiteObj = Globals.GetValidSPSite(_SPEEvalAttr.ObjEvalUrl, HttpContext.Current);
                        if (_SPEEvalAttr.SiteObj != null)
                        {
                            _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                            _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                        }
                    }
                    catch
                    {
                    }
                    _SPEEvalAttr.ObjDesc = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "description");
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                }
                else if ((_authorMethod != null &&
                     _authorMethod.StartsWith("rename service",
                                              StringComparison.OrdinalIgnoreCase)) &&
                    !String.IsNullOrEmpty(_authorService))
                {
                    _SPEEvalAttr.Action = "RENAME SITE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;

                    if (_authorService.LastIndexOf('/') < 0)
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService;
                    else
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService.Substring(_authorService.LastIndexOf('/') + 1);

                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' + _SPEEvalAttr.ObjName;
                    try
                    {
                        _SPEEvalAttr.SiteObj = Globals.GetValidSPSite(_SPEEvalAttr.ObjEvalUrl, HttpContext.Current);
                        if (_SPEEvalAttr.SiteObj != null)
                        {
                            _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                            if (_SPEEvalAttr.WebObj != null)
                                _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                        }
                    }
                    catch
                    {
                    }
                    _SPEEvalAttr.ObjDesc = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "description");
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                }
                // fix bug8641 by derek
                else if ((_authorMethod != null &&
                     _authorMethod.StartsWith("remove service",
                                              StringComparison.OrdinalIgnoreCase)) &&
                    !String.IsNullOrEmpty(_authorService))
                {
                    // Delete Site Evaluation is done in EnforceWebDeleting()
                    if (_authorService.LastIndexOf('/') < 0)
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService;
                    else
                        _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _authorService.Substring(_authorService.LastIndexOf('/') + 1);

                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + '/' + _SPEEvalAttr.ObjName;
                    try
                    {
                        _SPEEvalAttr.SiteObj = Globals.GetValidSPSite(_SPEEvalAttr.ObjEvalUrl, HttpContext.Current);
                        if (_SPEEvalAttr.SiteObj != null)
                        {
                            _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                            if (_SPEEvalAttr.WebObj != null)
                                _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                        }
                    }
                    catch
                    {
                    }
                    _SPEEvalAttr.ObjDesc = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "description");
                    // Add remote address to WebRemoteAddressMap
                    if (_SPEEvalAttr.LogonUser != null)
                    {
                        System.Security.Principal.IPrincipal userPrincipal = null;
                        if (HttpContext.Current != null)
                            userPrincipal = HttpContext.Current.User;
                        WebRemoteAddressMap.TrytoAddNewRemoteAddress(_SPEEvalAttr.LogonUser, _SPEEvalAttr.ObjEvalUrl, _SPEEvalAttr.RemoteAddr, userPrincipal,m_Request);
                    }
                    _SPEEvalAttr.ObjName = "";
                    _SPEEvalAttr.ObjEvalUrl = "";
                }
                // Do evaluation and transfer request
                HttpContext.Current.Request.Headers["X_READ"] = "1";
                    if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
                    {
                        CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                        CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                        String[] _emptyArray = new string[0];
                        String[] _propertyArray = new string[5 * 2];
                        String _policyName = null;
                        String _policyMessage = null;
                        String _before_url = null;
                        String _after_url = null;
                        _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                        _propertyArray[0 * 2 + 1] = _SPEEvalAttr.ObjName;
                        _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                        _propertyArray[1 * 2 + 1] = _SPEEvalAttr.ObjTitle;
                        _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                        _propertyArray[2 * 2 + 1] = _SPEEvalAttr.ObjDesc;
                        _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_TYPE;
                        _propertyArray[3 * 2 + 1] = _SPEEvalAttr.ObjType;
                        _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_SUBTYPE;
                        _propertyArray[4 * 2 + 1] = _SPEEvalAttr.ObjSubtype;

                    _response = Globals.CallEval(_SPEEvalAttr.PolicyAction,
                                                    _SPEEvalAttr.ObjEvalUrl,
                                                    _SPEEvalAttr.ObjTargetUrl,
                                                    ref _propertyArray,
                                                    ref _emptyArray,
                                                    _SPEEvalAttr.RemoteAddr,
                                                    _SPEEvalAttr.LogonUser,
                                                    _SPEEvalAttr.WebObj.CurrentUser.Sid,
                                                    ref _policyName,
                                                    ref _policyMessage,
                                                    _before_url,
                                                    _after_url,
                                                    Globals.HttpModuleName,
                                                    _NoiseLevel,
                                                    _SPEEvalAttr.WebObj,
                                                    null);
                        if (_response == CETYPE.CEResponse_t.CEDeny)
                        {
                            NextLabs.Common.Utilities.SetDenyRequestHeader(HttpContext.Current.Request, _policyName, _policyMessage);
                        }
                        HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                        return true;
                    }
                    HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                    return true;
            }
            return false;
        }
    }

    public class SPE_OWSSVR_DLL_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            if (_SPEEvalAttr.IsPost)
            {
                //POST
                // it is "List -> List Settings -> Versioning Settings"
                if (HttpContext.Current.Request.Headers["X_READ"] != "1")
                {
                    String _owssvrCmd = m_Request.Form["Cmd"];
                    String _RootFolder = "";
                    String _Title = "";
                    String _ListTemplateID = "";
                    String _paramListId = null;
                    String _owssvrListId = m_Request.Form["List"];

                    String _owssvrCmd1 = m_Request.QueryString["Cmd"];
                    String _owssvrListId1 = m_Request.QueryString["List"];
                    String _owssvrItemId = m_Request.QueryString["ID"];
                    System.OperatingSystem _os = System.Environment.OSVersion;
                    if (_owssvrListId != null && _owssvrListId.Length > 0 && _owssvrCmd != null && _owssvrCmd == "MODLISTSETTINGS")
                    {
                        _SPEEvalAttr.Action = "LIST SETTING:VERSIONING SETTINGS";
                    }
                    //fix bug 11541, just infect windows 2008
                    else if (_owssvrListId1 != null && _owssvrListId1.Length > 0 && _owssvrCmd1 != null && _owssvrCmd1.Equals("Delete", StringComparison.OrdinalIgnoreCase)
                             && _os.Platform == PlatformID.Win32NT && _os.Version.Major == 6 && _os.Version.Minor == 0)
                    {
                        _SPEEvalAttr.Action = "DELETE LIST ITEM";
                    }
                    //bear wu fix bug 23854
                    else if (_owssvrListId != null && _owssvrListId.Length > 0 && _owssvrCmd != null && _Title != null && _owssvrCmd == "NewWebPage" && m_Request.Form["type"] == "WebPartPage" && m_Request.Form["ID"] == "New" && _SPEEvalAttr.RequestURL.EndsWith("owssvr.dll?CS=65001", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.Action = "CREATE WEB PART PAGE";
                    }
                    //Fix bug 8937, add Sharepoint designer list/doclib property edit enforcement.Add by William 20090313
                    else if (m_Request.ContentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            Stream _inputStream = m_Request.InputStream;
                            byte[] ContentBuffer = new byte[_inputStream.Length + 1];
                            int _readlen = 0;
                            long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
                            _readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
                            _inputStream.Seek(_oldPos, SeekOrigin.Begin);
                            String _strContent = Globals.UrlDecode(Encoding.UTF8.GetString(ContentBuffer));
                            //We shall delete this string to let the input stream follow the xml format.
                            _strContent = _strContent.Replace("<ows:Batch OnError=\"Return\" Version=\"15.0.0.000\">\r\n", "");
                            _strContent = _strContent.Replace("<ows:Batch OnError=\"Return\" Version=\"14.0.0.000\">\r\n", "");
                            _strContent = _strContent.Replace("<ows:Batch OnError=\"Return\" Version=\"12.0.0.000\">\r\n", "");
                            byte[] InputBuffer = Encoding.Default.GetBytes(_strContent);
                            using (Stream InputStream = new MemoryStream(InputBuffer))
                            {
                                XmlTextReader reader = new XmlTextReader(InputStream);
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        if (reader.Name.Equals("SetList", StringComparison.OrdinalIgnoreCase))
                                        {
                                            reader.MoveToNextAttribute();//Fix bug 9838
                                            {
                                                if (reader.Name.Equals("Scope", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (reader.Value.Equals("m_Request", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _paramListId = reader.ReadString();
                                                    }
                                                }
                                            }
                                        }
                                        else if (reader.Name.Equals("SetVar", StringComparison.OrdinalIgnoreCase))
                                        {
                                            reader.MoveToNextAttribute();//Do not use the while loop as it can cause dead loop case, modified by William 20090901
                                            {
                                                if (reader.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (reader.Value.Equals("Cmd", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _owssvrCmd = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("RootFolder", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _RootFolder = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("Title", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _Title = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("ListTemplate", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _ListTemplateID = reader.ReadString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                        if (!string.IsNullOrEmpty(_paramListId) && _owssvrCmd != null && _owssvrCmd == "MODLISTSETTINGS")
                        {
                            _owssvrListId = _paramListId;
                            _SPEEvalAttr.Action = "LIST SETTING:TITLE AND DESCRIPTION";
                        }
                        //Add sharepoint designer new list enforcement, by William 20090820
                        else if (_owssvrCmd != null && _owssvrCmd.Equals("NewList", StringComparison.OrdinalIgnoreCase))
                        {
                            _SPEEvalAttr.Action = "CREATE LIST";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                            if (!string.IsNullOrEmpty(_RootFolder))
                                _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _RootFolder + "/" + _Title;
                            else
                                _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _Title;
                            _SPEEvalAttr.ObjName = _Title;
                            _SPEEvalAttr.ObjTitle = _Title;
                            _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                            if (_ListTemplateID != null && Globals.IsLibraryTemplateID(_ListTemplateID))
                            {
                                _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                            }
                            else
                            {
                                _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                            }
                        }
                    }
                    if (_SPEEvalAttr.Action == "LIST SETTING:VERSIONING SETTINGS"
                        || _SPEEvalAttr.Action == "LIST SETTING:TITLE AND DESCRIPTION")
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                        {
                            if (null != _SPEEvalAttr.WebObj)
                            {
                                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _owssvrListId, Utilities.SPUrlListID);
                                if (_SPEEvalAttr.ListObj != null)
                                {
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                                }
                            }
                        }
                    }
                    else if (_SPEEvalAttr.Action == "DELETE LIST ITEM")
                    {
                        {
                            if (null != _SPEEvalAttr.WebObj)
                            {
                                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _owssvrListId1, Utilities.SPUrlListID);
                                if (_SPEEvalAttr.ListObj != null)
                                {
                                    _SPEEvalAttr.ItemObj = _SPEEvalAttr.ListObj.GetItemById(Convert.ToInt32(_owssvrItemId));
                                    if (_SPEEvalAttr.ItemObj != null)
                                    {
                                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                                    }
                                }
                            }
                        }
                    }
                    //add bear fix bug 23854
                    else if (_SPEEvalAttr.Action == "CREATE WEB PART PAGE")
                    {
                        try
                        {
                            _Title = m_Request.Form["Title"];
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                            if (null != _SPEEvalAttr.WebObj)
                            {
                                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _owssvrListId, Utilities.SPUrlListID);
                                if (_SPEEvalAttr.ListObj != null)
                                {

                                    _SPEEvalAttr.ObjEvalUrl = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj);
                                    _SPEEvalAttr.ObjName = _Title;
                                    _SPEEvalAttr.ObjTitle = _Title;
                                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during DoSPEProcess:", null, ex);
                        }

                    }
                    //  Do evaluation and transfer m_Request
                    {
                        HttpContext.Current.Request.Headers["X_READ"] = "1";
                        if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
                        {
                            CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                            CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                            String[] _emptyArray = new string[0];
                            String[] _propertyArray = new string[5 * 2];
                            String _policyName = null;
                            String _policyMessage = null;
                            String _beforeurl = null;
                            String _afterUrl = null;
                            _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                            _propertyArray[0 * 2 + 1] = _SPEEvalAttr.ObjName;
                            _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                            _propertyArray[1 * 2 + 1] = _SPEEvalAttr.ObjTitle;
                            _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                            _propertyArray[2 * 2 + 1] = _SPEEvalAttr.ObjDesc;
                            _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_RESOURCE_TYPE;
                            _propertyArray[3 * 2 + 1] = _SPEEvalAttr.ObjType;
                            _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_RESOURCE_SUBTYPE;
                            _propertyArray[4 * 2 + 1] = _SPEEvalAttr.ObjSubtype;

                            if (_SPEEvalAttr.ItemObj != null)
                            {
                                int oldLen = _propertyArray.Length;
                                string[] newArray = new string[oldLen + 5 * 2];
                                _propertyArray.CopyTo(newArray, 0);
                                newArray[oldLen + 0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY;
                                newArray[oldLen + 0 * 2 + 1] = Globals.GetItemCreatedBySid(_SPEEvalAttr.ItemObj);
                                newArray[oldLen + 1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY;
                                newArray[oldLen + 1 * 2 + 1] = Globals.GetItemModifiedBySid(_SPEEvalAttr.ItemObj);
                                newArray[oldLen + 2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED;
                                newArray[oldLen + 2 * 2 + 1] = Globals.GetItemCreatedStr(_SPEEvalAttr.ItemObj);
                                newArray[oldLen + 3 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED;
                                newArray[oldLen + 3 * 2 + 1] = Globals.GetItemModifiedStr(_SPEEvalAttr.ItemObj);
                                newArray[oldLen + 4 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE;
                                newArray[oldLen + 4 * 2 + 1] = Globals.GetItemFileSizeStr(_SPEEvalAttr.ItemObj);
                                _propertyArray = newArray;

                                // Add other fixed and custom item attributes to the array.
                                _propertyArray = Globals.BuildAttrArrayFromItemProperties
                                    (_SPEEvalAttr.ItemObj.Properties, _propertyArray,
                                     _SPEEvalAttr.ItemObj.ParentList.BaseType, _SPEEvalAttr.ItemObj.Fields);
                                //Fix bug 8222, replace the "created" and "modified" properties
                                _propertyArray = Globals.ReplaceHashTime(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, _propertyArray);
                                //Fix bug 8694 and 8692,add spfield attr to tailor
                                _propertyArray = Globals.BuildAttrArray2FromSPField(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, _propertyArray);
                            }
                            _response = Globals.CallEval(_SPEEvalAttr.PolicyAction,
                                                        _SPEEvalAttr.ObjEvalUrl,
                                                        _SPEEvalAttr.ObjTargetUrl,
                                                        ref _propertyArray,
                                                        ref _emptyArray,
                                                        _SPEEvalAttr.RemoteAddr,
                                                        _SPEEvalAttr.LogonUser,
                                                        _SPEEvalAttr.WebObj.CurrentUser.Sid,
                                                        ref _policyName,
                                                        ref _policyMessage,
                                                        _beforeurl,
                                                        _afterUrl,
                                                        Globals.HttpModuleName,
                                                        _NoiseLevel,
                                                        _SPEEvalAttr.WebObj,
                                                        null);
                            if (_response == CETYPE.CEResponse_t.CEDeny)
                            {
                                NextLabs.Common.Utilities.SetDenyRequestHeader(HttpContext.Current.Request, _policyName, _policyMessage);
                            }
                            HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                            return true;
                        }
                        HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                        return true;
                    }
                }
            }
            else
            {
                //GET
                // it is still a action we need to know
                // Get 2 params Using & List
                String _paramUsing = m_Request.QueryString["Using"];
                String _paramList = m_Request.QueryString["List"];
                //String _paramMode = m_Request.QueryString["dialogview"];
                String _paramLocation = m_Request.QueryString["location"];
                bool isQuery = false;

                if (_paramUsing != null)
                {
                    isQuery = (_paramUsing.Equals("_layouts/query.iqy", StringComparison.OrdinalIgnoreCase) ||
                                _paramUsing.Equals("_layouts/15/query.iqy", StringComparison.OrdinalIgnoreCase));
                }

                if (!String.IsNullOrEmpty(_paramList) && isQuery)
                {
                    // It is "List/Library -> Export To DataSheet"
                    _SPEEvalAttr.Action = "LIST SETTING:EXPORT TO DATASHEET";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Export;
                    _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramList, Utilities.SPUrlListID);
                    if (_SPEEvalAttr.ListObj != null)
                    {
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                    }
                }
                else if (_paramLocation != null)
                {
                    if (!string.IsNullOrEmpty(_paramLocation))
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _paramLocation;
                    else
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl;
                    Object obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.ObjEvalUrl);
                    Type type = obj.GetType();
                    if (Object.ReferenceEquals(type,typeof(SPList))
                        || Object.ReferenceEquals(type,typeof(SPFolder)))
                    {
                        if (Object.ReferenceEquals(type,typeof(SPList)))
                        {
                            _SPEEvalAttr.Action = "LIST VIEW";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            _SPEEvalAttr.ListObj = (SPList)(obj);

                            if (_SPEEvalAttr.ListObj != null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                        }
                        else
                        {
                            SPFolder _Folder = ((SPFolder)(obj));
                            Guid _uid = _Folder.ParentListId;
                            if (Guid.Empty == _uid)
                            {
                                _SPEEvalAttr.Action = "WEB VIEW";
                                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                if (_SPEEvalAttr.WebObj!=null)
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                            }
                            else
                            {
                                _SPEEvalAttr.Action = "LIST VIEW";
                                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;

                                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _uid.ToString(), Utilities.SPUrlListID);
                                if (_SPEEvalAttr.ListObj != null)
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                            }
                        }
                    }
                    else if (Object.ReferenceEquals(type,typeof(SPWeb)))
                    {
                        _SPEEvalAttr.Action = "WEB VIEW";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;

                        if (_SPEEvalAttr.WebObj != null)
                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                    }
                    else if (Object.ReferenceEquals(type, typeof(SPListItem)))
                    {
                        SPListItem item = obj as SPListItem;
                        _SPEEvalAttr.Action = "DocSet VIEW";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;

                        if (item != null)
                            SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                    }
                }
                else
                {
                    /*
                     * added by chellee on 03/10/2009; for the bug8847/8921.
                     * Word->open->input XXXX/site, open, there has not evaluation for this action.
                     */
                    try
                    {
                        String location = m_Request.QueryString["location"];
                        String alterStr = null;
                        string[] buftemp = m_Request.RawUrl.Split('/');
                        int i = 0;
                        // changed by Tonny to avoid over array range
                        while (i < buftemp.Length)
                        {
                            if (buftemp[i] != "_vti_bin")
                            {
                                if (!string.IsNullOrEmpty(buftemp[i]))
                                {
                                    alterStr = alterStr + "/" + buftemp[i];
                                }
                            }
                            else
                            {
                                break;
                            }
                            i++;
                        }
                        SPSite _siteObj = SPControl.GetContextSite(HttpContext.Current);
                        if (!string.IsNullOrEmpty(location))
                        {
                            alterStr = alterStr + "/" + location;
                        }
                        SPWeb _srcWebObj = _siteObj.OpenWeb(alterStr);
                        if (_srcWebObj != null)
                        {
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            _SPEEvalAttr.Action = "READ";
                            SPEEvalAttrHepler.SetObjEvalAttr(_srcWebObj, _SPEEvalAttr);
                            _SPEEvalAttr.AddDisposeWeb(_srcWebObj);
                        }
                    }
                    catch
                    {
                    }

                }
            }
            return false;
        }
    }

}
