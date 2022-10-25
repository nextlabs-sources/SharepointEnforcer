using System;
using System.IO; // bug8444
using System.Collections; // bug8444
using System.Collections.Specialized; // bug8444
using System.Web;
using System.Web.UI.WebControls.WebParts;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Xml;
using NextLabs.Common;
using NextLabs.SPEnforcer;
using NextLabs.SPEnforcer.Utils;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public class SoapHttpModule
    {

        //use this function to avoid unknown error when fetching raw stream from request
        byte[] GetRequestRawdata(Stream _inputStream, HttpRequest Request)
        {
            //save current stream position
            long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
            String _strContent = string.Empty;
            using (StreamReader streamReader = new StreamReader(_inputStream, Request.ContentEncoding))
            {
                _strContent = Globals.UrlDecode(streamReader.ReadToEnd());
            }
            //set stream position back
            _inputStream.Seek(_oldPos, SeekOrigin.Begin);
            byte[] InputBuffer = Encoding.Default.GetBytes(_strContent);
            return InputBuffer;

        }


        void GetEvaluationValue(String SiteNameKey, String ListNameKey, String ItemNameKey, String EvalURLKey, String ListTemplateKey, String ItemFolderKey, XmlDocument _inputdoc,
            ref String SiteName, ref String ListName, ref String ItemName, ref String EvalURL, ref String ListTemplate, ref String ItemFolder)
        {
            if (_inputdoc != null)
            {
                if (SiteNameKey != null && SiteNameKey.Length > 0)
                {
                    XmlNodeList SiteNameNode = _inputdoc.DocumentElement.GetElementsByTagName(SiteNameKey);
                    if (SiteNameNode != null && SiteNameNode.Count > 0)
                    {
                        XmlNode childNode = SiteNameNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            if (childNode != null)
                                SiteName = childNode.Value;
                        }
                    }
                }
                if (ListNameKey != null && ListNameKey.Length > 0)
                {
                    XmlNodeList ListNameNode = _inputdoc.DocumentElement.GetElementsByTagName(ListNameKey);
                    if (ListNameNode != null && ListNameNode.Count > 0)
                    {
                        XmlNode childNode = ListNameNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            if (childNode != null)
                                ListName = childNode.Value;
                        }
                    }
                }
                if (ItemNameKey != null && ItemNameKey.Length > 0)
                {
                    XmlNodeList ItemNameNode = _inputdoc.DocumentElement.GetElementsByTagName(ItemNameKey);
                    if (ItemNameNode != null && ItemNameNode.Count > 0)
                    {
                        XmlNode childNode = ItemNameNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            if (childNode != null)
                                ItemName = childNode.Value;
                        }
                    }
                }
                if (EvalURLKey != null && EvalURLKey.Length > 0)
                {
                    XmlNodeList EvalURLNode = _inputdoc.DocumentElement.GetElementsByTagName(EvalURLKey);
                    if (EvalURLNode != null && EvalURLNode.Count > 0)
                    {
                        XmlNode childNode = EvalURLNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            EvalURL = childNode.Value;
                        }
                    }
                }
                if (ListTemplateKey != null && ListTemplateKey.Length > 0)
                {
                    XmlNodeList ListTemplateNode = _inputdoc.DocumentElement.GetElementsByTagName(ListTemplateKey);
                    if (ListTemplateNode != null && ListTemplateNode.Count > 0)
                    {
                        XmlNode childNode = ListTemplateNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            ListTemplate = childNode.Value;
                        }
                    }
                }
                if (ItemFolderKey != null && ItemFolderKey.Length > 0)
                {
                    XmlNodeList ItemFolderKeyNode = _inputdoc.DocumentElement.GetElementsByTagName(ItemFolderKey);
                    if (ItemFolderKeyNode != null && ItemFolderKeyNode.Count > 0)
                    {
                        XmlNode childNode = ItemFolderKeyNode[0].FirstChild;
                        if (childNode != null)
                        {
                            while (childNode != null && childNode.NodeType != XmlNodeType.Text)
                            {
                                childNode = childNode.FirstChild;
                            }
                            ItemFolder = childNode.Value;
                        }
                    }
                }
            }
        }
        void GetEvaluationKey(XmlDocument keyword_xml, ref String SiteNameKey, ref String SiteNameKeyType,
            ref String ListNameKey, ref String ListNameKeyType, ref String ItemNameKey, ref String ItemNameKeyType,
            ref String EvalURLKey, ref String EvalURLKeyType, ref String ListTemplateKey, ref String ItemFolderKey)
        {
            //Get Site,List and Item Name keyword
            XmlNodeList SiteNameKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("SiteNameKey");
            if (SiteNameKeyNode != null && SiteNameKeyNode.Count > 0 && SiteNameKeyNode[0].FirstChild != null)
            {
                if (SiteNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    SiteNameKey = SiteNameKeyNode[0].FirstChild.Value;
                }
                else if (SiteNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    SiteNameKeyType = SiteNameKeyNode[0].FirstChild.Name;
                    SiteNameKey = SiteNameKeyNode[0].FirstChild.FirstChild.Value;
                }
            }
            XmlNodeList ListNameKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("ListNameKey");
            if (ListNameKeyNode != null && ListNameKeyNode.Count > 0 && ListNameKeyNode[0].FirstChild != null)
            {
                if (ListNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    ListNameKey = ListNameKeyNode[0].FirstChild.Value;
                }
                else if (ListNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    ListNameKeyType = ListNameKeyNode[0].FirstChild.Name;
                    ListNameKey = ListNameKeyNode[0].FirstChild.FirstChild.Value;
                }
            }
            XmlNodeList ItemNameKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("ItemNameKey");
            if (ItemNameKeyNode != null && ItemNameKeyNode.Count > 0 && ItemNameKeyNode[0].FirstChild != null)
            {
                if (ItemNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    ItemNameKey = ItemNameKeyNode[0].FirstChild.Value;
                }
                else if (ItemNameKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    ItemNameKeyType = ItemNameKeyNode[0].FirstChild.Name;
                    ItemNameKey = ItemNameKeyNode[0].FirstChild.FirstChild.Value;
                }
            }

            XmlNodeList EvalURLKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("EvalURLKey");
            if (EvalURLKeyNode != null && EvalURLKeyNode.Count > 0 && EvalURLKeyNode[0].FirstChild != null)
            {
                if (EvalURLKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    EvalURLKey = EvalURLKeyNode[0].FirstChild.Value;
                }
                else if (EvalURLKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    EvalURLKeyType = EvalURLKeyNode[0].FirstChild.Name;
                    EvalURLKey = EvalURLKeyNode[0].FirstChild.FirstChild.Value;
                }
            }

            XmlNodeList ListTemplateKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("ListTemplateKey");
            if (ListTemplateKeyNode != null && ListTemplateKeyNode.Count > 0 && ListTemplateKeyNode[0].FirstChild != null)
            {
                if (ListTemplateKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    ListTemplateKey = ListTemplateKeyNode[0].FirstChild.Value;
                }
                else if (ListTemplateKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    ListTemplateKey = ListTemplateKeyNode[0].FirstChild.FirstChild.Value;
                }
            }
            XmlNodeList ItemFolderKeyNode = keyword_xml.DocumentElement.GetElementsByTagName("ItemFolderKey");
            if (ItemFolderKeyNode != null && ItemFolderKeyNode.Count > 0 && ItemFolderKeyNode[0].FirstChild != null)
            {
                if (ItemFolderKeyNode[0].FirstChild.NodeType == XmlNodeType.Text)
                {
                    ItemFolderKey = ItemFolderKeyNode[0].FirstChild.Value;
                }
                else if (ItemFolderKeyNode[0].FirstChild.NodeType == XmlNodeType.Element)
                {
                    ItemFolderKey = ItemFolderKeyNode[0].FirstChild.FirstChild.Value;
                }
            }
        }

        void SoapActionMap(XmlDocument keyword_xml, SPEEvalAttr _SPEEvalAttr)
        {
            String ActionType = "";
            String ActionTarget = "";
            XmlNodeList ActionTypeNode = keyword_xml.DocumentElement.GetElementsByTagName("ActionType");
            XmlNodeList ActionTargetNode = keyword_xml.DocumentElement.GetElementsByTagName("ActionTarget");
            if (ActionTypeNode != null && ActionTypeNode.Count > 0 && ActionTypeNode[0].FirstChild.NodeType == XmlNodeType.Text)
            {
                ActionType = ActionTypeNode[0].FirstChild.Value;
            }
            if (ActionTargetNode != null && ActionTargetNode.Count > 0 && ActionTargetNode[0].FirstChild.NodeType == XmlNodeType.Text)
            {
                ActionTarget = ActionTargetNode[0].FirstChild.Value;
            }
            if (ActionType != null)
            {
                if (ActionType.Equals("Read", StringComparison.OrdinalIgnoreCase))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                else if (ActionType.Equals("Write", StringComparison.OrdinalIgnoreCase))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                else if (ActionType.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
            }
            if (ActionTarget != null)
            {
                if (ActionTarget.Equals("Site", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                    CE_ATTR_SP_TYPE_VAL_SITE;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                }
                else if (ActionTarget.Equals("List", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                    CE_ATTR_SP_TYPE_VAL_PORTLET;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                }
                else if (ActionTarget.Equals("Item", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                    CE_ATTR_SP_TYPE_VAL_ITEM;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                }
                else if (ActionTarget.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.ObjType = "Unknown";
                    _SPEEvalAttr.ObjSubtype = "Unknown";
                }
            }
        }

        String ProcessEvalURL(String EvalURL, SPEEvalAttr _SPEEvalAttr, bool bRecursive)
        {
            SPListItem _item = null;
            SPList _list = null;
            SPFile _file = null;
            SPView _view = null;
            if (EvalURL != null)
            {
                _SPEEvalAttr.SiteObj = Globals.GetValidSPSite(EvalURL, HttpContext.Current);
                if (_SPEEvalAttr.SiteObj != null)
                {
                    _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                    _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                }
            }
            //Fix bug 916 918,in some cases ,the first _SPEEvalAttr.WebObj.GetXXX will retrive an exception,we do not konw why, but the second time it will be normal
            try
            {
               // Object _obj = _SPEEvalAttr.WebObj.GetObject(EvalURL);
            }
            catch
            {
                //normal exception, no need to log it
            }
            if (_SPEEvalAttr.ObjType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM || _SPEEvalAttr.ObjType == "Unknown")
            {
                //try list item first
                try
                {
                    _item = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, EvalURL, Utilities.SPUrlListItem);
                }
                catch
                {
                    //normal exception, no need to log it
                }
                if (_item != null)
                {
                    if (_item.ParentList.BaseType == SPBaseType.DocumentLibrary)
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_ITEM;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                    }
                    else
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_ITEM;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                    }
                    _SPEEvalAttr.ObjName = _item.Name;
                    _SPEEvalAttr.ObjTitle = _item.Title;
                    return EvalURL;
                }
                //try spfile, it should be list item attachment
                try
                {
                    _file = _SPEEvalAttr.WebObj.GetFile(EvalURL);
                }
                catch
                {
                    //normal exception, no need to log it
                }
                if (_file != null && _file.Exists)
                {
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                    CE_ATTR_SP_TYPE_VAL_ITEM;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                    _SPEEvalAttr.ObjName = _file.Name;
                    _SPEEvalAttr.ObjTitle = _file.Title;
                    return EvalURL;
                }
            }

            if (_SPEEvalAttr.ObjType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET || _SPEEvalAttr.ObjType == "Unknown")
            {
                //try the splist
                try
                {
                    _list = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, EvalURL, Utilities.SPUrlList);
                }
                catch
                {
                    //normal exception, no need to log it
                }
                if (_list != null)
                {
                    if (_list.BaseType == SPBaseType.DocumentLibrary)
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_PORTLET;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    }
                    else
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_PORTLET;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                    }
                    _SPEEvalAttr.ObjName = _list.Title;
                    _SPEEvalAttr.ObjTitle = _list.Title;
                    _SPEEvalAttr.ObjDesc = _list.Description;
                    EvalURL = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, _list);
                    return EvalURL;
                }
                //try the spview
                try
                {
                    _view = _SPEEvalAttr.WebObj.GetViewFromUrl(EvalURL);
                }
                catch
                {
                    //normal exception, no need to log it
                }
                if (_view != null)
                {
                    if (_view.ParentList.BaseType == SPBaseType.DocumentLibrary)
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_PORTLET;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    }
                    else
                    {
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                        CE_ATTR_SP_TYPE_VAL_PORTLET;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                    }
                    _SPEEvalAttr.ObjName = _view.ParentList.Title;
                    _SPEEvalAttr.ObjTitle = _view.ParentList.Title;
                    _SPEEvalAttr.ObjDesc = _view.ParentList.Description;
                    EvalURL = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, _view.ParentList);
                    return EvalURL;
                }
            }

            if (_SPEEvalAttr.ObjType == "Unknown")
            {
                _SPEEvalAttr.ObjType = "Unknown";
                _SPEEvalAttr.ObjSubtype = "Unknown";
            }
            return EvalURL;
        }

        bool IF_SoapAction(XmlDocument _inputdoc, ref String _SoapAction)
        {
            String _SoapURL = "http://schemas.microsoft.com/sharepoint/soap";
            if (_SoapAction != null)
            {
                //Get soap action word
                int _pos = _SoapAction.LastIndexOf("/");
                _SoapAction = _SoapAction.Substring(_pos + 1);
                _SoapAction = _SoapAction.Replace("\\", "");
                _SoapAction = _SoapAction.Replace("\"", "");
                return true;
            }
            else if (_SoapAction == null || _SoapAction.Length <= 0)
            {
                foreach (String key in SPEEvalInit.SoapActionMap.Keys)
                {
                    try
                    {
                        XmlNodeList SoapActionNode = _inputdoc.DocumentElement.GetElementsByTagName(key);
                        if (SoapActionNode != null && SoapActionNode.Count > 0)
                        {
                            if (SoapActionNode[0].NamespaceURI.StartsWith(_SoapURL, StringComparison.OrdinalIgnoreCase))
                            {
                                _SoapAction = SoapActionNode[0].Name;
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        //normal exception, no need to log it
                    }
                }
            }
            return false;
        }


        string GenerateListPath(SPWeb _webObj, string evalpath, string listName, string listTemplate)
        {
            SPList listObj = null;
            foreach (SPList _list in _webObj.Lists)
            {
                if (_list.Title.Equals(listName, StringComparison.OrdinalIgnoreCase))
                {
                    listObj = _list;
                    break;
                }
            }
            if (listObj != null)
            {
                return Globals.ConstructListUrl(_webObj, listObj);
            }
            if (listTemplate != null)
            {
                if (Globals.IsLibraryTemplateID(listTemplate))
                {
                    evalpath += "/" + listName;
                }
                else
                {
                    evalpath += "/" + "Lists" + "/" + listName;
                }
            }
            else
            {
                evalpath += "/" + listName;
            }
            return evalpath;
        }

        public bool SoapProcess(HttpRequest Request)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            string UserSid = "";
            InnerSoapProcess(Request, _SPEEvalAttr);
            if (_SPEEvalAttr.RequestURL_path.EndsWith("webpartpages.asmx", StringComparison.OrdinalIgnoreCase))
            {
                UserSid = Globals.getADUserSid(_SPEEvalAttr.LogonUser);
            }
            //  Do evaluation and transfer request
            HttpContext.Current.Request.Headers["X_READ"] = "1";
            if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
            {
                if (!String.IsNullOrEmpty(_SPEEvalAttr.ObjEvalUrl))
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
                }
                HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                return true;
            }
            HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
            return true;
        }

        private void InnerSoapProcess(HttpRequest Request, SPEEvalAttr _SPEEvalAttr)
        {
            String SiteNameKey = "";
            String SiteNameKeyType = "";
            String ListNameKey = "";
            String ListNameKeyType = "";
            String ItemNameKey = "";
            String ItemNameKeyType = "";
            String EvalURLKey = "";
            String EvalURLKeyType = "";
            String ListTemplateKey = "";
            String ListTemplate = "";
            String ItemFolderKey = "";
            String ItemFolder = "";

            String SiteName = "";
            String ListName = "";
            String ItemName = "";
            String EvalURL = "";

            try
            {
                XmlDocument _inputdoc = new XmlDocument();
                String _SoapAction = Request.Headers["SOAPAction"];
                Stream _inputStream = Request.InputStream;
                byte[] InputBuffer = GetRequestRawdata(_inputStream, Request);
                using (Stream InputStream = new MemoryStream(InputBuffer))
                {
                    _inputdoc.Load(InputStream);
                    if (IF_SoapAction(_inputdoc, ref _SoapAction))
                    {
                        //String _NamespaceURI = _inputdoc.DocumentElement.NamespaceURI;

                        XmlDocument keyword_xml = null;
                        if (SPEEvalInit.SoapActionMap == null || SPEEvalInit.SoapActionMap.Count <= 0)
                        {
                            Globals.TrySoapConfigFile();
                        }
                        if (SPEEvalInit.SoapActionMap.ContainsKey(_SoapAction))
                            keyword_xml = SPEEvalInit.SoapActionMap[_SoapAction];
                        if (keyword_xml != null)
                        {
                            _SPEEvalAttr.Action = "SOAP _SPEEvalAttr.Action";
                            GetEvaluationKey(keyword_xml, ref SiteNameKey, ref SiteNameKeyType,
                                    ref ListNameKey, ref ListNameKeyType, ref ItemNameKey, ref ItemNameKeyType,
                                    ref EvalURLKey, ref EvalURLKeyType, ref ListTemplateKey, ref ItemFolderKey);
                            SoapActionMap(keyword_xml, _SPEEvalAttr);
                        }
                        GetEvaluationValue(SiteNameKey, ListNameKey, ItemNameKey, EvalURLKey, ListTemplateKey, ItemFolderKey, _inputdoc, ref SiteName, ref ListName, ref ItemName, ref EvalURL,
                            ref ListTemplate, ref ItemFolder);
                    }

                    if ((SiteName != null && SiteName.Length > 0) || (ListName != null && ListName.Length > 0)
                        || (ItemName != null && ItemName.Length > 0) || (EvalURL != null && EvalURL.Length > 0))
                    {
                        _SPEEvalAttr.ObjEvalUrl = "";
                        if (SiteName != null && SiteName.Length > 0)
                        {
                            //if has a site name, just use it
                            _SPEEvalAttr.ObjEvalUrl += SiteName;
                            //it may be a site
                            _SPEEvalAttr.ObjName = SiteName;
                            _SPEEvalAttr.ObjTitle = SiteName;
                        }
                        else
                        {
                            if (EvalURL != null && EvalURL.Length > 0)
                            {
                                if (_SPEEvalAttr.ObjType != CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE)
                                {
                                    _SPEEvalAttr.ObjEvalUrl += ProcessEvalURL(EvalURL, _SPEEvalAttr, true);
                                }
                                else
                                {
                                    Uri _Url = new Uri(EvalURL);
                                    _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode(Globals.TrimEndUrlSegments
                                         (_Url.GetLeftPart(UriPartial.Path), 2));

                                }
                            }
                            else
                            {
                                if (_SoapAction.Equals("GetWebPartPage", StringComparison.OrdinalIgnoreCase))
                                {
                                    int beginpos = Request.Url.AbsoluteUri.IndexOf("_vti_bin");
                                    string header = Request.Url.AbsoluteUri.Substring(0, beginpos - 1);
                                    int endpos = Request.RawUrl.IndexOf("_vti_bin");
                                    string end = Request.RawUrl.Substring(0, endpos - 1);
                                    _SPEEvalAttr.ObjEvalUrl += header + end;
                                }
                                else
                                {
                                    _SPEEvalAttr.ObjEvalUrl += _SPEEvalAttr.WebObj.Url;
                                }
                            }
                        }
                        //we do not use else if because evalurl can exist with itemname in the same time
                        if (ListName != null && ListName.Length > 0)
                        {
                            if (ListNameKeyType != null && ListNameKeyType.Equals("id", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    SPList _listObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, ListName, Utilities.SPUrlListID);
                                    _SPEEvalAttr.ObjEvalUrl = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, _listObj);
                                    _SPEEvalAttr.ObjName = _listObj.Title;
                                    _SPEEvalAttr.ObjTitle = _listObj.Title;
                                }
                                catch
                                {
                                    //It may be not an id
                                    _SPEEvalAttr.ObjEvalUrl = GenerateListPath(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, ListName, ListTemplate);
                                    _SPEEvalAttr.ObjName = ListName;
                                    _SPEEvalAttr.ObjTitle = ListName;
                                }
                            }
                            else
                            {
                                _SPEEvalAttr.ObjEvalUrl = GenerateListPath(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, ListName, ListTemplate);
                                //it may be a list
                                _SPEEvalAttr.ObjName = ListName;
                                _SPEEvalAttr.ObjTitle = ListName;
                            }
                        }
                        if (ItemName != null && ItemName.Length > 0)
                        {
                            if (!string.IsNullOrEmpty(ItemFolder))
                                _SPEEvalAttr.ObjEvalUrl += "/" + ItemFolder;
                            _SPEEvalAttr.ObjEvalUrl += "/" + ItemName;
                            //At last, it shoud be a item
                            _SPEEvalAttr.ObjName = ItemName;
                            _SPEEvalAttr.ObjTitle = ItemName;
                            if (_SoapAction.Equals("GetWebPartPage", StringComparison.OrdinalIgnoreCase))
                                return;
                        }
                        //Check this evaluation's detail type
                        if (_SPEEvalAttr.ObjSubtype == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST)
                        {
                            SPList _list = null;
                            try
                            {
                                _list = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.ObjEvalUrl, EvalURL, Utilities.SPUrlList);
                            }
                            catch
                            {
                                //normal exception, no need to log it
                            }
                            if (_list != null)
                            {
                                _SPEEvalAttr.ObjName = _list.Title;
                                _SPEEvalAttr.ObjTitle = _list.Title;
                                _SPEEvalAttr.ObjDesc = _list.Description;
                                if (_list.BaseType == SPBaseType.DocumentLibrary)
                                {
                                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                                    CE_ATTR_SP_TYPE_VAL_PORTLET;
                                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                                }
                            }
                        }
                        else if (_SPEEvalAttr.ObjSubtype == CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM)
                        {
                            SPListItem _item = null;
                            try
                            {
                                _item = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlListItem);
                            }
                            catch
                            {
                                //normal exception, no need to log it
                            }
                            if (_item != null)
                            {
                                _SPEEvalAttr.ObjName = _item.Name;
                                _SPEEvalAttr.ObjTitle = _item.Title;
                                if (_item.ParentList.BaseType == SPBaseType.DocumentLibrary)
                                {
                                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                                                    CE_ATTR_SP_TYPE_VAL_ITEM;
                                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                                }
                            }
                            if (_item == null)
                            {
                                SPFile _file = null;
                                //try spfile, it should be list item attachment
                                try
                                {
                                    _file = _SPEEvalAttr.WebObj.GetFile(EvalURL);
                                }
                                catch
                                {
                                    //normal exception, no need to log it
                                }
                                if (_file != null && _file.Exists)
                                {
                                    _SPEEvalAttr.ObjName = _file.Name;
                                    _SPEEvalAttr.ObjTitle = _file.Title;
                                }
                            }
                        }
                        else if ((_SPEEvalAttr.ObjType == "Unknown") || (string.IsNullOrEmpty(_SPEEvalAttr.ObjType) && string.IsNullOrEmpty(_SPEEvalAttr.ObjSubtype)))
                        {
                            ProcessEvalURL(_SPEEvalAttr.ObjEvalUrl, _SPEEvalAttr, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during InnerSoapProcess:", null, ex);
            }
        }
    }
}
