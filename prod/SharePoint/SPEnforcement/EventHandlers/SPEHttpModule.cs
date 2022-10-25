using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
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
using System.Web.UI;
using NextLabs.SPEnforcer;
using NextLabs.Common;
using Microsoft.Win32;
using System.Xml.Serialization;
using System.Reflection;
using NextLabs.SPE.WebSvcEntitlement;
using Nextlabs.SPSecurityTrimming;
using Microsoft.SharePoint.Administration;
using System.Configuration;
using Microsoft.SharePoint.Administration.Claims;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NextLabs.PluginInterface;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public class SPEWebServiceClass
    {
        public static WebServiceConfig _WebServiceConfig = null;
        public static bool IsWebService = false;
        private static String GetSPEPath()
        {
            String SPEDir = null;
            try
            {
                RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegSPEInstallDir = null;
                if (CE_key != null)
                    RegSPEInstallDir = CE_key.GetValue("InstallDir");
                if (RegSPEInstallDir != null)
                {
                    SPEDir = Convert.ToString(RegSPEInstallDir);
                    if (SPEDir != null && !SPEDir.EndsWith("\\"))
                        SPEDir = SPEDir + "\\";
                }
            }
            catch
            {
            }
            return SPEDir;
        }

        public static SPEWebSvcResBuilder GetSPEWebServiceClass(String name, String classtype, ref Type _type)
        {
            if (string.IsNullOrEmpty(classtype))
            {
                _type = Type.GetType(name);
                Object obj = Activator.CreateInstance(_type);
                if (obj != null)
                    return (SPEWebSvcResBuilder)obj;
            }
            else
            {
                try
                {
                    Type _type1 = null;
                    Object obj = null;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            Assembly ass = Assembly.LoadFrom(classtype);
                            _type1 = ass.GetType(name);
                            obj = Activator.CreateInstance(_type1);
                        });
                    _type = _type1;
                    return (SPEWebSvcResBuilder)obj;
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during GetSPEWebServiceClass", null, ex);
                }
            }
            return null;
        }

        private static String GetObjectType(String type)
        {
            if (type != null)
            {
                if (type.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                }
                else if (type.Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                }
                else if (type.Equals("site", StringComparison.OrdinalIgnoreCase))
                {
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                }
            }
            return null;
        }

        private static XmlDocument GenerateXmlDoc(HttpRequest Request, SPEEvalAttr _SPEEvalAttr)
        {
            XmlDocument _inputdoc = null;
            Encoding encoding = Request.ContentEncoding;
            try
            {
                if (_SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase))
                {
                    _inputdoc = new XmlDocument();
                    Stream _inputStream = Request.InputStream;
                    byte[] ContentBuffer = new byte[_inputStream.Length + 1];
                    int _readlen = 0;
                    long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
                    _readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
                    _inputStream.Seek(_oldPos, SeekOrigin.Begin);
                    String _strContent = encoding.GetString(ContentBuffer);

                    byte[] InputBuffer = Encoding.Default.GetBytes(_strContent);
                    using (Stream InputStream = new MemoryStream(InputBuffer))
                    {
                        _inputdoc.Load(InputStream);
                    }
                }
                else if (_SPEEvalAttr.RequestURL_path.EndsWith(".svc", StringComparison.OrdinalIgnoreCase))
                {
                    _inputdoc = new XmlDocument();
                    Stream _inputStream = Request.InputStream;
                    byte[] ContentBuffer = new byte[_inputStream.Length + 1];
                    int _readlen = 0;

                    //save current stream position
                    long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
                    _readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
                    //set stream position back
                    _inputStream.Seek(_oldPos, SeekOrigin.Begin);
                    String _strContent = Globals.UrlDecode(encoding.GetString(ContentBuffer));
                    try
                    {
                        XmlDocument tryDoc = new XmlDocument();
                        tryDoc.LoadXml(_strContent);
                    }
                    catch
                    {
					     // George: change for "?<...>" xml string.
                    	_strContent = _strContent.TrimStart('?');
                        // "Globals.UrlDecode" change the xml format, use the original string.
                        _strContent = encoding.GetString(ContentBuffer);
                    }
                    if (_SPEEvalAttr.RequestURL_path.EndsWith("CELLSTORAGE.svc", StringComparison.OrdinalIgnoreCase))
                    {
                        _strContent = _strContent.Substring(_strContent.IndexOf("<s:Envelope xmlns"));
                        string end = "</s:Envelope>\r\n";
                        _strContent = _strContent.Substring(0, _strContent.IndexOf(end) + end.Length);
                    }
                    byte[] InputBuffer = Encoding.Default.GetBytes(_strContent); ;
                    using (Stream InputStream = new MemoryStream(InputBuffer))
                    {
                        _inputdoc.Load(InputStream);
                    }
                }
            }
            catch
            {
            }
            return _inputdoc;
        }

    public static bool DoSPEWebServiceClass(HttpRequest _request,ref bool isWebSvc)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String request = _SPEEvalAttr.RequestURL_path;
            string JSON_strContent = "";
            isWebSvc = false;
            if (_WebServiceConfig == null)
            {
                String _configfile = GetSPEPath() + "config\\" + "WebServiceConfig.xml";
                using (TextReader reader = new StreamReader(_configfile))
                {
                    XmlSerializer xmlSl = new XmlSerializer(typeof(WebServiceConfig));
                    _WebServiceConfig = (WebServiceConfig)xmlSl.Deserialize(reader);
                }
            }
            if (_WebServiceConfig != null && _SPEEvalAttr != null)
            {
                XmlDocument _node = null;
                if (_request.RawUrl.ToString().IndexOf("view.svc",StringComparison.OrdinalIgnoreCase) > 0
                    || _request.RawUrl.ToString().IndexOf("edit.svc", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    Stream _inputStream = _request.InputStream;
                    byte[] ContentBuffer = new byte[_inputStream.Length + 1];
                    int _readlen = 0;
                    long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
                    _readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
                    _inputStream.Seek(_oldPos, SeekOrigin.Begin);
                    JSON_strContent =Globals.UrlDecode(Encoding.UTF8.GetString(ContentBuffer));
                }
                else
                {
                    try {
                        _node = GenerateXmlDoc(_request, _SPEEvalAttr);
                    }
                    catch {
                        _node = null;
                    }
                }
                if (_node == null && string.IsNullOrEmpty(JSON_strContent))
                    return false;
                int index = request.LastIndexOf("/");
                if (index != -1)
                {
                    String webservice_name = request.Substring(index + 1);
                    int webservicenumber = -1;
                    int keynumber = -1;
                    for (int i = 0; i < _WebServiceConfig.WebService.Length; i++)
                    {
                        if (_WebServiceConfig.WebService[i].name.Equals(webservice_name, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_WebServiceConfig.WebService[i].disabled != true)
                                webservicenumber = i;
                            break;
                        }
                    }
                    if (webservicenumber != -1)
                    {
                        XmlNamespaceManager _xmlmgr=null;
                        if (_node != null)
                        {
                            _xmlmgr = new XmlNamespaceManager(_node.NameTable);
                        }
                        if (_WebServiceConfig.WebService[webservicenumber].WebServiceNameSpace != null
                            && _WebServiceConfig.WebService[webservicenumber].WebServiceNameSpace.Length > 0
                            && _xmlmgr!=null)
                        {
                            for (int j = 0; j < _WebServiceConfig.WebService[webservicenumber].WebServiceNameSpace.Length; j++)
                            {
                                _xmlmgr.AddNamespace(_WebServiceConfig.WebService[webservicenumber].WebServiceNameSpace[j].name, _WebServiceConfig.WebService[webservicenumber].WebServiceNameSpace[j].value);
                            }
                        }
                        for (int i = 0; i < _WebServiceConfig.WebService[webservicenumber].Method.Length; i++)
                        {
                            if (_WebServiceConfig.WebService[webservicenumber].Method[i].patternxpath.Contains("json:") == true)
                            {
                                if (_request.RawUrl.ToString().IndexOf(_WebServiceConfig.WebService[webservicenumber].Method[i].patternxpath.Replace("json:","")) > 0)
                                {
                                    if (_WebServiceConfig.WebService[webservicenumber].Method[i].disabled != true)
                                        keynumber = i;
                                    break;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(_WebServiceConfig.WebService[webservicenumber].Method[i].conditionxpath))
                                {
                                    XmlNodeList conditionNodes = _node.SelectNodes(_WebServiceConfig.WebService[webservicenumber].Method[i].conditionxpath, _xmlmgr);
                                    if (conditionNodes.Count == 0)
                                        continue;
                                }
                                XmlNodeList _nodelist = _node.SelectNodes(_WebServiceConfig.WebService[webservicenumber].Method[i].patternxpath, _xmlmgr);
                                if (_nodelist.Count != 0)
                                {
                                    if (_WebServiceConfig.WebService[webservicenumber].Method[i].disabled != true)
                                        keynumber = i;
                                    break;
                                }
                            }
                        }
                        if (keynumber != -1)
                        {
                            Type _type = null;
                            SPEWebSvcResBuilder _SPEWebServiceBase = GetSPEWebServiceClass(_WebServiceConfig.WebService[webservicenumber].Method[keynumber].@class,
                                                                                        _WebServiceConfig.WebService[webservicenumber].Method[keynumber].type, ref _type);
                            if (_SPEWebServiceBase != null)
                            {
                                if (_node != null)
                                {
                                    _SPEWebServiceBase.Init(_WebServiceConfig.WebService[webservicenumber].Method[keynumber], _node, _xmlmgr);
                                }
                                else
                                {
                                    _SPEWebServiceBase.InitJson(_WebServiceConfig.WebService[webservicenumber].Method[keynumber],JSON_strContent);
                                }
                                _SPEEvalAttr.ObjType = GetObjectType(_WebServiceConfig.WebService[webservicenumber].Method[keynumber].resourcetype);
                                //Internal class

                                if (string.IsNullOrEmpty(_WebServiceConfig.WebService[webservicenumber].Method[keynumber].type))
                                {
                                    _SPEEvalAttr.ObjEvalUrl = _SPEWebServiceBase.BuildResourceString();
                                    if (string.IsNullOrEmpty(_SPEEvalAttr.ObjType))
                                    {
                                        _SPEEvalAttr.ObjType = _SPEWebServiceBase.GetResType();
                                    }
                                    IsWebService = true;
                                    isWebSvc = true;
                                }
                                //Outer dll class
                                else if (_type != null)
                                {
                                    MethodInfo mi = _type.GetMethod("BuildResourceString");
                                    object[] parm = new object[0];
                                    object ret = mi.Invoke(_SPEWebServiceBase, parm);
                                    _SPEEvalAttr.ObjEvalUrl = (String)ret;
                                }
                                if (_SPEEvalAttr.ObjEvalUrl != null)
                                {
                                    if (_SPEEvalAttr.ObjType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET)
                                    {
                                        try
                                        {
                                            _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_SPEEvalAttr.ObjEvalUrl);
                                        }
                                        catch
                                        {
                                        }
                                        if (_SPEEvalAttr.ListObj != null)
                                        {
                                            _SPEEvalAttr.Action = _WebServiceConfig.WebService[webservicenumber].Method[keynumber].policyaction;
                                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                                        }
                                    }
                                    else if (_SPEEvalAttr.ObjType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM)
                                    {
                                        try
                                        {
                                            _SPEEvalAttr.ItemObj = _SPEEvalAttr.WebObj.GetListItem(_SPEEvalAttr.ObjEvalUrl);
                                        }
                                        catch
                                        {
                                        }
                                        if (_SPEEvalAttr.ItemObj != null)
                                        {
                                            _SPEEvalAttr.Action = _WebServiceConfig.WebService[webservicenumber].Method[keynumber].policyaction;
                                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                                        }
                                        else
                                        {
											if (_SPEEvalAttr.ObjEvalUrl.Contains("Attachments"))
                                            {
                                                _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_SPEEvalAttr.ObjEvalUrl);
                                                string[] splitBuf = _SPEEvalAttr.ObjEvalUrl.Split(new Char[] { '/' });
                                                int splitCount = 0;
                                                foreach (string s in splitBuf)
                                                {
                                                    splitCount++;
                                                }
                                                string itemId = splitBuf[splitCount - 2];
                                                _SPEEvalAttr.ItemObj = _SPEEvalAttr.ListObj.GetItemById(Convert.ToInt32(itemId));
												_SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
												_SPEEvalAttr.ObjName = _SPEEvalAttr.ItemObj.Name;
								                _SPEEvalAttr.Action = _WebServiceConfig.WebService[webservicenumber].Method[keynumber].policyaction;
                                            }
                                            SPFile _file = null;
                                            try
                                            {
                                                _file = _SPEEvalAttr.WebObj.GetFile(_SPEEvalAttr.ObjEvalUrl);
                                            }
                                            catch
                                            {
                                                //normal exception, no need to log it
                                            }
                                            if (_file != null && _file.Exists)
                                            {
                                                _SPEEvalAttr.ObjName = _file.Name;
                                                _SPEEvalAttr.ObjTitle = _file.Title;
                                                _SPEEvalAttr.Action = _WebServiceConfig.WebService[webservicenumber].Method[keynumber].policyaction;
                                            }
                                        }
                                    }
                                    else if (_SPEEvalAttr.ObjType == CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE)
                                    {
                                        SPWeb subWeb = (SPWeb)Utilities.GetCachedSPContent(null, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlWeb);
                                        if (subWeb != null && !subWeb.Equals(_SPEEvalAttr.WebObj))
                                        {
                                            _SPEEvalAttr.WebObj = subWeb;
                                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                                        }
                                        else
                                        {
                                            #region add by roy
                                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                                            #endregion
                                        }
                                        _SPEEvalAttr.ObjDesc = _SPEEvalAttr.WebObj.Description;
                                        _SPEEvalAttr.ObjName = _SPEEvalAttr.WebObj.Name;
                                        try
                                        {
                                            _SPEEvalAttr.ObjTitle = _SPEEvalAttr.WebObj.Title;
                                        }
                                        catch
                                        {
                                        }
                                        _SPEEvalAttr.Action = _WebServiceConfig.WebService[webservicenumber].Method[keynumber].policyaction;
                                    }
                                    if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (_SPEEvalAttr.Action.EndsWith("Read", StringComparison.OrdinalIgnoreCase))
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                        else if (_SPEEvalAttr.Action.EndsWith("Write", StringComparison.OrdinalIgnoreCase) || _SPEEvalAttr.Action.EndsWith("Edit", StringComparison.OrdinalIgnoreCase))
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                        else if (_SPEEvalAttr.Action.EndsWith("Export", StringComparison.OrdinalIgnoreCase))
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Export;
                                        else if (_SPEEvalAttr.Action.EndsWith("Delete", StringComparison.OrdinalIgnoreCase))
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                                        else
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                    }


                                }
                            }

                        }
                    }
                }
            }
            return false;
        }
    }

    public class SPEClass
    {
        public static Hashtable SPEClassContainer = null;

        public static SPEModuleBase GetSPEClass(String _page)
        {
            string pageInternal = _page.ToLower();
            if (SPEClassContainer == null)
            {
                SPEClassContainer = new Hashtable();
                String[] _PageParams = {
                                            "admin.dll",
                                            "cellstorage.svc",
                                            "client.svc",
                                            "author.dll",
                                            "prjsetng.aspx",
                                            "sitemanager.aspx",
                                            "policy.aspx",
                                            "policycts.aspx",
                                            "ListGeneralSettings.aspx",
                                            "advsetng.aspx",
                                            "AddGallery.aspx",
                                            "new.aspx",
                                            "slnew.aspx",
                                            "NewTranslationManagement.aspx",
                                            "CreateListPickerPage.aspx",
                                            "owssvr.dll",
                                            "ListEnableTargeting.aspx",
                                            "listsyndication.aspx",
                                            "ManageItemScheduling.aspx",
                                            "listedit.aspx",
                                            "survedit.aspx",
                                            "newsbweb.aspx",
                                            "deleteweb.aspx",
                                            "discussions",//fix bug:21006,For Discussion list and their list item object
                                            "formserver.aspx",
                                             "download.aspx",
											 "checkin.aspx",
                                             "ratingssettings.aspx"
                                        };
                for (int i = 0; i < _PageParams.Length; i++)
                {
                    String _ClassName = _PageParams[i].Replace(".", "_");
                    _ClassName = _ClassName.ToUpper();
                    _ClassName = "NextLabs.HttpEnforcer.SPE_" + _ClassName + "_Module";
                    Type _type = Type.GetType(_ClassName);
                    Object obj = Activator.CreateInstance(_type);
                    SPEClassContainer.Add(_PageParams[i].ToLower(), obj);
                }
            }
            if (SPEClassContainer != null && SPEClassContainer.Contains(pageInternal))
            {
                return (SPEModuleBase)SPEClassContainer[pageInternal];
            }
            return null;
        }

    }

    public partial class SPEHttpModule
    {
        // Partial list of HTTP/1.1 Status Code.  See RFC 2616 for a complete
        // list.
        enum HttpStatusCode
        {
            // Successful 2xx
            OK = 200,

            // Client Error 4xx
            Unauthorized = 401,
            Forbidden = 403,
            NotFound = 404
        }


        public String ModuleName
        {
            get { return "HttpEnforcerModule"; }
        }

        private enum UrlSPType
        {
            SITE,
            WEB,
            DOC_LIB,
            OTHER_LIST,
            DOC_LIB_ITEM,
            OTHER_LIST_ITEM,
            NOT_SURE
        }

        private enum CESTEPType
        {
            CESTEP_1,
            CESTEP_2,
            CESTEP_3,
            CESTEP_4,
            CESTEP_MAX,
            CESTEP_NONE,
        }

        //
        // The three SharePoint standard content page lists below are found in
        // the article titled "Windows SharePoint Services Default Master
        // Pages" under the "Windows SharePoint Services 3.0" section in the
        // MSDN web site.  The article lists the following:
        //
        // - default.aspx
        // - AllItems.aspx, DispForm.aspx, NewForm.aspx, and EditForm.aspx: for
        //   all lists
        // - Upload.aspx and Webfldr.aspx: for all document libraries
        //
        private string[] SPStdWebContentPages =
        {
            "default.aspx","Home.aspx"
        };

        private string[] SPStdDocLibContentPages =
        {
            "Upload.aspx", "Webfldr.aspx"
        };

        private string[] SPStdListContentPages =
        {
            "AllItems.aspx", "DispForm.aspx", "NewForm.aspx", "EditForm.aspx"
        };

        private string[] SPStdListItemQueryPages =
        {
            "DispForm.aspx", "EditForm.aspx"
        };

        private void DoSearchPreFilter(HttpRequest Request, SPWeb web)
        {
            bool bIsNextpage = false;
            NameValueCollection nameValue = new NameValueCollection(Request.QueryString);
            string SPEKeyword = "";
            if (nameValue["k"] != null)
            {
                SPEKeyword = nameValue["k"];
            }
            if (Request.Headers["SPEKeyWord"] == null)
            {
                string filterValue = "";
                Globals.DoPreFilterForKql(Request, web, CETYPE.CEAction.View, ref filterValue);
                if (!string.IsNullOrEmpty(filterValue))
                {
                    filterValue = " AND " + filterValue;
                    // next page
                    if (-1 != Request.RawUrl.IndexOf("&start1=", StringComparison.OrdinalIgnoreCase))
                    {
                        bIsNextpage = true;
                    }
                    if (SPEKeyword.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        string keyWord = SPEKeyword.Substring(0, SPEKeyword.Length - filterValue.Length);
                        ChangeEditBoxContent(Request, keyWord);
                        return;
                    }
                    else if (!bIsNextpage)   //first page
                    {
                        Request.Headers["SPEKeyWord"] = SPEKeyword;
                        string newRawUrl = Request.RawUrl;
                        int ind = 0;
                        if (-1 != (ind = newRawUrl.IndexOf("k=", StringComparison.OrdinalIgnoreCase)))
                        {
                            string tail = newRawUrl.Substring(ind);
                            if (-1 != (ind = tail.IndexOf("&", 3)))
                            {
                                tail = tail.Substring(0, ind);
                            }
                            SPEKeyword = "(" + SPEKeyword + ")";
                            newRawUrl = newRawUrl.Replace(tail, "k=" + SPEKeyword + filterValue);
                        }
                        HttpContext.Current.Server.TransferRequest(newRawUrl, true, Request.HttpMethod, Request.Headers);
                    }
                }
            }
            else
            {
                string userKeyWord = Request.Headers["SPEKeyWord"];
                ChangeEditBoxContent(Request, userKeyWord);
#if SP2010
                ChangeNoResultAlert(Request, SPEKeyword, userKeyWord);
#endif
            }
        }

        private void ChangeEditBoxContent(HttpRequest Request, string keyWord)
        {
            System.Web.UI.Page page = HttpContext.Current.CurrentHandler as System.Web.UI.Page;
            // get the searchbox of the search page, and change the content of the box to user input, modify by emily.
            string modifyKeyWordBoxScript = "function ResetUserKeyword(nInterval, strNewKeyword){" +
               "var inputObjs = document.getElementsByTagName('input');" +
               " if (!inputObjs){ " +
               " setTimeout(function () { ResetUserKeyword(nInterval, strNewKeyword), nInterval });}" +
               " else {" +
               "var oSearchEditBox = null;" +
               "for(var i = 0; i<inputObjs.length; i++){" +
               "if(-1 < inputObjs[i].title.toLowerCase().indexOf('search')){" +
               "oSearchEditBox=inputObjs[i];break;}}" +
               "if(oSearchEditBox && -1 < oSearchEditBox.value.indexOf(strNewKeyword)){" +
               "oSearchEditBox.value = strNewKeyword;}" +
               "else{setTimeout(function () { ResetUserKeyword(nInterval, strNewKeyword), nInterval });}}}" +
               "ResetUserKeyword(10,'" +
               keyWord + "');";
            ScriptManager.RegisterStartupScript(page, page.ClientScript.GetType(), "changeKey", "<script>" + modifyKeyWordBoxScript + "</script>", false);
        }

        private void ChangeNoResultAlert(HttpRequest Request, string filterKeyWord,string userKeyWord)
        {
            filterKeyWord = filterKeyWord.Replace("<", "&lt;").Replace(">", "&gt;");
            System.Web.UI.Page page = HttpContext.Current.CurrentHandler as System.Web.UI.Page;
            string resetNoResultAlertScript = "function ResetNoResultAlert(nInterval, filterKeyWord, userKeyWord){" +
               "var objNoResultSpan = document.getElementById('CSR_NO_RESULTS');" +
               " if (!objNoResultSpan){ " +
               " setTimeout(function () { ResetUserKeyword(nInterval, filterKeyWord), nInterval });}" +
               " else {" +
               "objNoResultSpan.innerHTML=objNoResultSpan.innerHTML.replace(filterKeyWord, userKeyWord);}}" +
               "ResetNoResultAlert(10,'" + filterKeyWord + "', '"+userKeyWord+"');";
            ScriptManager.RegisterStartupScript(page, page.ClientScript.GetType(), "changeNoResultAlert", "<script>" + resetNoResultAlertScript + "</script>", false);
        }

        static public bool CheckPreFilterSearchTrimming(SPWeb web)
        {
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
            {
                if (!manager.CheckSearchPrefilterTrimming())
                {
                    // Search pre-filter trimming is not opened by customer.
                    return false;
                }
            }
            return true;
        }

        public bool SkipRequest()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            bool _auth = HttpContext.Current.Request.IsAuthenticated;
            if (_SPEEvalAttr.RequestURL_path.EndsWith("/_vti_inf.html", StringComparison.OrdinalIgnoreCase))
                return false;
#if SP2019
            // bug 51524, SP2019 don't care create news in http module.
            if (_SPEEvalAttr.IsPost && !string.IsNullOrEmpty(_SPEEvalAttr.RequestURL_path) && _SPEEvalAttr.RequestURL_path.EndsWith("_api/sitepages/pages", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
#endif
            if (_SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.RequestURL_path.IndexOf("OSSSearchResults.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.RequestURL_path.IndexOf("results.aspx", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // Add by George, for search pre-filter.
                HttpRequest Request = HttpContext.Current.Request;
                SPWeb web = null;
                try
                {
                    web = SPControl.GetContextWeb(HttpContext.Current);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("SkipRequest error:"+ex.ToString());
                }
                if (CheckPreFilterSearchTrimming(web))
                {
                    DoSearchPreFilter(Request, web);
                }

                SPSite site = SPControl.GetContextSite(HttpContext.Current);
                bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (bIgnoreTrimControl || (manager.CheckSecurityTrimming() && manager.CheckFastSearchTrimming()))
                    {
                        int index = _SPEEvalAttr.RequestURL_path.LastIndexOf("/");
                        string pagename = null;
                        if (index != -1)
                            pagename = _SPEEvalAttr.RequestURL_path.Substring(index + 1);
                        ResponseFilter filter = ResponseFilters.Current(HttpContext.Current.Response);
                        filter.AddFilterType(FilterType.SearchTrimmer);
                        filter.PageName = pagename;
                    }
                }
            }
            if (_auth == false)
            {
                return false;
            }
            //For those requests for some static content/file which store at layouts folder but not end with ".aspx"
            //if these request go through the whole SPE code, the content will be blocked from the server to client.
            //So here skip these requests.
            if (_SPEEvalAttr.RequestURL_path.IndexOf("/_layouts/", StringComparison.OrdinalIgnoreCase) != -1 &&
                _SPEEvalAttr.RequestURL_path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) == false &&
                _SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase) == false)
                return false;
            if (_SPEEvalAttr.RequestURL_path != null

                && (_SPEEvalAttr.RequestURL_path.IndexOf("_layouts/Authenticate.aspx", StringComparison.OrdinalIgnoreCase) != -1
                 || _SPEEvalAttr.RequestURL_path.IndexOf("_layouts/15/Authenticate.aspx", StringComparison.OrdinalIgnoreCase) != -1
                 || _SPEEvalAttr.RequestURL_path.IndexOf("_windows/default.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path.IndexOf("_login/default.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path.IndexOf("/_forms/default.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path.IndexOf("_layouts/AccessDenied.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path.IndexOf("_layouts/reqacc.aspx", StringComparison.OrdinalIgnoreCase) != -1
                || _SPEEvalAttr.RequestURL_path.IndexOf("closeConnection.aspx", StringComparison.OrdinalIgnoreCase) != -1))
            {
                return false;
            }
            return true;
        }


        public void BeginRequest(Object source, EventArgs e)
        {
			SPEWebServiceClass.IsWebService = false;
            // Create HttpApplication and HttpContext objects to access
            // request and response properties.
            HttpApplication application = (HttpApplication)source;
            HttpContext context = application.Context;
            HttpRequest Request = context.Request;
            //HttpResponse Response = context.Response;

            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            if (_SPEEvalAttr == null)
            {
                return;
            }
            _SPEEvalAttr.Init();
            _SPEEvalAttr.BeginTicks = System.Environment.TickCount;

            NLLogger.OutputLog(LogLevel.Debug, "BeginRequest: enter, thread ID = " + Thread.CurrentThread.ManagedThreadId);
            // Common information that we need
            _SPEEvalAttr.RemoteAddr = Request.UserHostAddress;
            _SPEEvalAttr.HttpMethod = Request.HttpMethod;
            _SPEEvalAttr.IsPost = _SPEEvalAttr.HttpMethod.Equals("POST");
            //To fix bug 9973, rebuild the url modified by William 20090914
            _SPEEvalAttr.RequestURL = Globals.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
            _SPEEvalAttr.LogonUser = Request.ServerVariables["LOGON_USER"];
            //To fix bug 9973, rebuild the url modified by William 20090914
            _SPEEvalAttr.RequestURL_path = Globals.HttpModule_ReBuildURL(Request.Url.LocalPath, Request.FilePath, Request.Path);

            NLLogger.OutputLog(LogLevel.Debug, "BeginRequest: URL = " + _SPEEvalAttr.RequestURL + ", method = " + _SPEEvalAttr.HttpMethod);
            if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/_vti_bin/client.svc") && _SPEEvalAttr.IsPost && Request.ContentLength > 0)
            {
                RequestFilter filter = new RequestFilter(Request.Filter);
                Request.Filter = filter;
            }
        }
        private object LoadObject(Assembly asm, string classname, string interfacename, object[] param)
        {
            try
            {
                Type t = asm.GetType(classname);
                if (t == null || !t.IsClass || !t.IsPublic || t.IsAbstract || t.GetInterface(interfacename) == null)
                {
                    return null;
                }
                object o = Activator.CreateInstance(t, param);
                if (o == null)
                {
                    return null;
                }
                return o;
            }
            catch
            {
                return null;
            }
        }
        private CETYPE.CEAction ConverFromPluginAction(EvalAction ea, ref string Action)
        {
            CETYPE.CEAction RetAction = CETYPE.CEAction.Read;
            switch(ea)
            {
                case EvalAction.EARead:
                    RetAction = CETYPE.CEAction.Read;
                    Action = "Read";
                    break;
                case EvalAction.EAWrite:
                    RetAction = CETYPE.CEAction.Write;
                    Action = "Write";
                    break;
                case EvalAction.EADelete:
                    RetAction = CETYPE.CEAction.Delete;
                    Action = "Delete";
                    break;
            }
            return RetAction;
        }
        private bool ExecuteRequestPlugins(HttpRequest Request, HttpResponse Response)
        {
            bool bNeedContinueParseAfterPlugins = false;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();

            string SiteUrl = string.Empty;
            string ListUrl = string.Empty;
            string ItemUrl = string.Empty;

            EvalObjectType eot = EvalObjectType.EOTUnknown;
            EvalAction ea = EvalAction.EAUknown;
            bNeedContinueParseAfterPlugins = PluginFrame.Instance.RunRequestPlugins(Request, Response, ref eot, ref ea, ref SiteUrl, ref ListUrl, ref ItemUrl, 1000);
            string Action = string.Empty;
            _SPEEvalAttr.PolicyAction = ConverFromPluginAction(ea, ref Action);
			if (!string.IsNullOrEmpty(Action))
            	_SPEEvalAttr.Action = Action;

            if (_SPEEvalAttr.WebObj == null)
                return true;
            switch (eot)
            {
                case EvalObjectType.EOTSite:
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                    break;
                case EvalObjectType.EOTList:
                    _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, ListUrl, Utilities.SPUrlList);
                    if (_SPEEvalAttr.ListObj == null)
                        return true;
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                    break;
                case EvalObjectType.EOTItem:
                    _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, ListUrl, Utilities.SPUrlList);
                    if (_SPEEvalAttr.ListObj != null)
                        _SPEEvalAttr.ItemObj = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, ItemUrl, Utilities.SPUrlListItem);
                    else
                        return true;
                    if (_SPEEvalAttr.ItemObj != null)
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                    else
                        return true;
                    break;
                default:
                    break;
            }

            return bNeedContinueParseAfterPlugins;
        }
        private bool ParsePostRequest(HttpApplication application,ref bool bIsWebSvc, ref bool bReturnDirectly)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            HttpResponse Response = context.Response;
            //process set rowlimit case.
            if (_SPEEvalAttr.RequestURL_path.EndsWith("owssvr.dll", StringComparison.OrdinalIgnoreCase) && Request.QueryString["CS"] == "65001")
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(SPControl.GetContextSite(HttpContext.Current)))
                {
                    if (manager.CheckSecurityTrimming())
                    {
                        int nRowLimit = Convert.ToInt32(Request.Form["RowLimit"]);
                        if (nRowLimit > 70) // for trimming, the row limit should be set to 70!!
                        {
                            String backurl = "";
                            String httpserver = "";
                            String msg = "";
                            Common.Utilities.GenerateBackUrl(Request, "Trimming Row Limit (Not a Policy) ", "The action was denied for Trimming.", ref backurl, ref httpserver, ref msg);
                            blockRequest(application, Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                            bReturnDirectly = true;
                            return false;
                        }
                    }
                }
            }

            bool bIsLib = false;
            string qsInitialTabId = Request.QueryString["InitialTabId"];
            string MSOLayout_InDesignMode = Request.Form["MSOLayout_InDesignMode"];
            string _wikiPageMode = Request.Form["_wikiPageMode"];
            string __EVENTTARGET = Request.Form["__EVENTTARGET"];
            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
            if (String.IsNullOrEmpty(qsInitialTabId) || qsInitialTabId.Length > 0)
            {//edit site home page or other wiki page
                //if "_wikiPageMode" is null, it means just open the page in design mode. SO it is just READ
                if (string.IsNullOrEmpty(_wikiPageMode))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
            }
            else
            {//edit list/lib page
                bIsLib = true;
                if (SPE_GetEval(Request))
                    return true;
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                //if "MSOLayout_InDesignMode" is 1, mean it is open the page in design mode. There is not any change made on the page yet.
                if (MSOLayout_InDesignMode != null && MSOLayout_InDesignMode.Equals("1", StringComparison.OrdinalIgnoreCase))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                else if (String.IsNullOrEmpty(__EVENTTARGET))
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
            }
            if (!(String.IsNullOrEmpty(qsInitialTabId) || qsInitialTabId.Length > 0) || !string.IsNullOrEmpty(_wikiPageMode) || !string.IsNullOrEmpty(MSOLayout_InDesignMode))
            {
                // we deal with page edit here!!! This is edit page for site, e.g. sitepages/home.aspx
                // FORM PARAM <MSOSPWebPartManager_DisplayModeName> =Design
                // FORM PARAM <__SPSCEditMenu>                      =true
                _SPEEvalAttr.Action = "EDIT PAGE";

                if (bIsLib)
                {
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                    if (_SPEEvalAttr.ListObj == null)
                    {
                        if ((_SPEEvalAttr.RequestURL_path.EndsWith("author.dll", StringComparison.OrdinalIgnoreCase)
                                || _SPEEvalAttr.RequestURL_path.EndsWith("owssvr.dll", StringComparison.OrdinalIgnoreCase)
                                || _SPEEvalAttr.RequestURL_path.EndsWith("admin.dll", StringComparison.OrdinalIgnoreCase)
                                || _SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase)))
                        {
                            HttpContext.Current.Request.Headers["X_READ"] = "1";
                            HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                        }
                        return true;
                    }
                    if (_SPEEvalAttr.ListObj.BaseType == SPBaseType.DocumentLibrary)
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    else
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                }
                else
                {
                    _SPEEvalAttr.ObjEvalUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Globals.TrimEndUrlSegments(_SPEEvalAttr.RequestURL_path, 1);
                    if (_SPEEvalAttr.WebObj != null)
                    {
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                    }
                }
            }
            else if (_SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase)
                || _SPEEvalAttr.RequestURL_path.EndsWith(".svc", StringComparison.OrdinalIgnoreCase))
            {
                if (HttpContext.Current.Request.Headers["X_READ"] != "1")
                {
                    bool ret = false;
                    try
                    {
                        ret = SPEWebServiceClass.DoSPEWebServiceClass(Request, ref bIsWebSvc);
                    }
                    catch { }
                    if (ret)
                    {
                        bReturnDirectly = true;
                        return true;
                    }

                }
            }
            else
            {
                String _page = _SPEEvalAttr.RequestURL_path.Substring(_SPEEvalAttr.RequestURL_path.LastIndexOf("/") + 1);
                SPEModuleBase _SPEModuleBase = SPEClass.GetSPEClass(_page);
                if (_SPEModuleBase != null)
                {
                    _SPEModuleBase.Init(Request);
                    if (_SPEModuleBase.DoSPEProcess())
                    {
                        bReturnDirectly = true;
                        return true;
                    }

                }
                else if ((Request.Url.AbsoluteUri.IndexOf("_vti_bin/EwaInternalWebService.json/CloseWorkbook", StringComparison.OrdinalIgnoreCase) > 0 && Request.UrlReferrer.ToString().IndexOf("_layouts/xlviewer.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                    || (Request.Url.AbsoluteUri.IndexOf("_vti_bin/OneNote.ashx") > 0 && Request.UrlReferrer.ToString().IndexOf("_layouts/OneNoteFrame.aspx") > 0))
                {
                    if (SPE_GetEval(Request))
                    {
                        bReturnDirectly = true;
                        return true;
                    }

                }
                else if (getSPTypeFromUrl(Request.Url) == UrlSPType.DOC_LIB && !String.IsNullOrEmpty(Request.QueryString["RootFolder"]))
                {
                    // it is doc lib folder READ (left-clicking on folder name)
                    // URL Query: ?RootFolder=/Team Site 1/Team Site 1 1/Doc Lib 1/Folder 1/Folder 1 1&FolderCTID=&View={7DD1375E-31DC-42C6-A654-1D89660CB1B4}
                    string[] segments = Request.Url.Segments;
                    char[] slashChArr = { '/' };
                    string rootFolderurl = Request.QueryString["RootFolder"];
                    string docLibName = Globals.UrlDecode(segments[segments.Length - 3].TrimEnd(slashChArr));
                    _SPEEvalAttr.Action = "DOC LIB FOLDER READ";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.SiteObj.Url + Globals.UrlDecode(rootFolderurl);
                    try
                    {
                        _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.Lists[docLibName];

                        if (_SPEEvalAttr.ListObj != null)
                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);

                    }
                    catch
                    {
                        /* No such doc lib. */
                        _SPEEvalAttr.Action = "UNKNOWN_ACTION";
                    }
                }
                else
                {
                    // Action not handler
                }
            }

            // SPE file version metadata enforcement.
            if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/versions.aspx?", StringComparison.OrdinalIgnoreCase)
                || -1 != _SPEEvalAttr.RequestURL.IndexOf("/DispForm.aspx", StringComparison.OrdinalIgnoreCase))
            {
                CheckItemFileVersion(Request, _SPEEvalAttr);
            }
            return true;
        }
        private bool ParseGetRequest(HttpApplication application, ref bool bReturnDirectly)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            HttpResponse Response = context.Response;

            if (_SPEEvalAttr.RequestURL_path.Contains("ReusableContent"))
            {
                try
                {
                    int index = Request.FilePath.LastIndexOf("/");
                    String list_path = Request.FilePath.Substring(0, index);
                    SPList _listObj = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                    //If it's format is list and can get a list, then it is a list indeed
                    if (_listObj != null)
                    {
                        _SPEEvalAttr.Action = "READ";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(_listObj, _SPEEvalAttr);
                    }
                }
                catch (Exception ex)
                {
                    //If some exception happens, it means we can not get the SPList Object
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during Check ReusableContent list:", null, ex);
                    if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                    {
                        bReturnDirectly = true;
                        return false;
                    }
                }
            }
            if (_SPEEvalAttr.RequestURL_path.Contains("layouts/itemexpiration.aspx") ||
                _SPEEvalAttr.RequestURL_path.Contains("layouts/15/itemexpiration.aspx") ||
                _SPEEvalAttr.RequestURL_path.Contains("layouts/createws.aspx") ||
                _SPEEvalAttr.RequestURL_path.Contains("layouts/15/createws.aspx"))
            {
                bool bException = false;
                try
                {

                    String _paramListId = Request.QueryString["List"];
                    String _IDStr = Request.QueryString["ID"];
                    if (string.IsNullOrEmpty(_IDStr))
                    {
                        _IDStr = Request.QueryString["Item"];
                    }
                    if (!string.IsNullOrEmpty(_paramListId) && !string.IsNullOrEmpty(_IDStr))
                    {
                        SPList _curList = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                        SPListItem _curItem = null;

                        if (_curList != null)
                        {
                            int obj_id_int = Convert.ToInt32(_IDStr);
                            _curItem = _curList.GetItemById(obj_id_int);
                        }
                        if (null != _curItem)
                        {
                            // we get current list and current item
                            // fill the parameters
                            _SPEEvalAttr.Action = "READ";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            _SPEEvalAttr.ObjEvalUrl = ""; //add this line to avoid exception from blockRequest throwing exception.

                            if (_curItem != null)
                            {
                                SPEEvalAttrHepler.SetObjEvalAttr(_curItem, _SPEEvalAttr);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    bException = true;
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ParseGetRequest:", null, ex);
                }
                if (bException && !EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                {
                    bReturnDirectly = true;
                    return false;
                }
            }

            if (-1 != _SPEEvalAttr.RequestURL_path.IndexOf("/SitePages/Category.aspx", StringComparison.OrdinalIgnoreCase))
            {
                // Case : "/SitePages/Category.aspx?CategoryID=1&SiteMapTitle=General"
                if (_SPEEvalAttr.WebObj != null)
                {
                    try
                    {
                        string categoryId = Request.QueryString["CategoryID"];
                        if (!string.IsNullOrEmpty(categoryId))
                        {
                            SPList list = _SPEEvalAttr.WebObj.Lists["Categories"];
                            if (list != null)
                            {
                                SPListItem item = list.GetItemById(int.Parse(categoryId));
                                if (item != null)
                                {
                                    _SPEEvalAttr.Action = "Open Catrgory item";
                                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                    SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                                    return true;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (!_SPEEvalAttr.RequestURL_path.EndsWith(".ASPX", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".ASP", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".DLL", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".JS", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".JSP", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".CS", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".CSS", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".HTM", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".HTML", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".SHTML", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".GIF", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase)
                && !_SPEEvalAttr.RequestURL_path.EndsWith(".JPEG", StringComparison.OrdinalIgnoreCase)
                && _SPEEvalAttr.RequestURL_path.IndexOf("_vti_bin", StringComparison.OrdinalIgnoreCase) == -1
                )
            {
                bool bException = false;
                // parse the path to get current list and current item
                string[] splitBuf = _SPEEvalAttr.RequestURL_path.Split(new Char[] { '/' });
                int splitCount = 0;
                foreach (string s in splitBuf)
                {
                    splitCount++;
                }

                if (!((splitCount == 3 || splitCount == 4)
                    && splitBuf[1].Equals("sites", StringComparison.OrdinalIgnoreCase)
                    ))
                {//it is not something like http://server/sites/psr or http://server/sites/psr/
                    //on certain machine, for the first access, when access _SPEEvalAttr.WebObj.Lists, it will raise exception "access denied".
                    //Here code is for a document, so skip these type URLs
                    if (splitCount > 2 && _SPEEvalAttr.WebObj != null)
                    {

                        //string _SiteName = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "siteurl");
                        string _FileName = splitBuf[splitCount - 1];
                        //string _ListName = splitBuf[splitCount - 2];
                        SPList _curList = null;
                        SPListItem _curItem = null;
                        try
                        {
                            _curList = _SPEEvalAttr.WebObj.GetList(_SPEEvalAttr.RequestURL_path);
                        }
                        catch (Exception ex)
                        {
                            bException = true;
                            int historyVersionPos = _SPEEvalAttr.RequestURL_path.IndexOf("/_vti_history/");
                            int ind = -1;
                            // prior version of listitem case, example : /_vti_history/512/Shared Documents/test.docx;
                            try
                            {
                                if (-1 != historyVersionPos)
                                {
                                    ind = _SPEEvalAttr.RequestURL_path.Substring(historyVersionPos + 14).IndexOf("/");
                                }
                                if (-1 != ind)
                                {
                                    string correctListUrl = _SPEEvalAttr.RequestURL_path.Remove(historyVersionPos, ind + 14);
                                    _curList = _SPEEvalAttr.WebObj.GetList(correctListUrl);
                                    bException = false;
                                }
                                else
                                {
                                    NLLogger.OutputLog(LogLevel.Debug, "Exception during GetList:", null, ex);
                                }
                            }
                            catch (Exception ex2)
                            {
                                bException = true;
                                NLLogger.OutputLog(LogLevel.Debug, "Exception during GetList-versionhistory:", null, ex2);
                            }
                        }
                        if (_curList != null)
                        {
                            try
                            {
                                SPListItem lItem = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.RequestURL, Utilities.SPUrlListItem);

                                if (lItem != null)
                                {
                                    _curItem = lItem;
                                }
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Debug, "Exception during get the current item in Open using url directtly:", null, ex);
                            }

                            if (_curItem == null)
                            {
                                //use spquery for searching an item
                                try
                                {
                                    SPQuery query = new SPQuery();
                                    SPListItemCollection spListItems;
                                    query.RowLimit = 2000; // Only select the top 2000.
                                    query.ViewAttributes = "Scope=\"Recursive\"";
                                    string format = "<Where><Eq><FieldRef Name=\"FileLeafRef\" /><Value Type=\"File\">{0}</Value></Eq></Where>";
                                    query.Query = String.Format(format, _FileName);
                                    spListItems = _curList.GetItems(query);

                                    if (spListItems.Count > 0)
                                    {
                                        foreach (SPListItem tmpSPListItem in spListItems)
                                        {
                                            if (_SPEEvalAttr.RequestURL_path.EndsWith(tmpSPListItem.Url, StringComparison.OrdinalIgnoreCase))
                                            {
                                                _curItem = tmpSPListItem;
                                                break;
                                            }
                                        }
                                        if (_curItem == null)
                                        {
                                            _curItem = spListItems[0];
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    bException = true;
                                    NLLogger.OutputLog(LogLevel.Debug, "Exception during get the current item in Open:", null, ex);
                                }
                            }

                        }

                        if (null != _curList && null != _curItem)
                        {
                            // we get current list and current item
                            // fill the parameters
                            _SPEEvalAttr.Action = "READ";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            SPEEvalAttrHepler.SetObjEvalAttr(_curItem, _SPEEvalAttr);
                        }

                        if (bException)
                        {
                            if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                            {
                                bReturnDirectly = true;
                                return false;
                            }
                        }
                    }
                }//end of if (!((splitCount == 3 || splitCount == 4)
            }
            //To fix document set bug
            CheckDocumentSet(Request, ref _SPEEvalAttr);

            //To fix bug 23468, when policy is deny edit, when upload mutiple file case
            if (Request.Url.AbsoluteUri.IndexOf("/_layouts/upload.aspx", StringComparison.OrdinalIgnoreCase) > 0 && Request.QueryString["MultipleUpload"] != null)
            {
                string listGuid = Request.QueryString["List"];
                SPWeb _webObj = null;
                try
                {
                    _webObj = SPControl.GetContextWeb(context);
                }
                catch
                {
                }
                if (listGuid != null && _webObj != null)
                {
                    Guid _listGuid = new Guid(listGuid);
                    if (listGuid != null && !listGuid.StartsWith("{"))
                    {
                        listGuid = "{" + listGuid + "}";
                    }
                    SPList list = _webObj.Lists.GetList(_listGuid, true);

                    _SPEEvalAttr.Action = "UPLOAD MUTIPLE FILE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;

                    if (list !=null)
                    {
                        SPEEvalAttrHepler.SetObjEvalAttr(list, _SPEEvalAttr);
                    }
                }
            }

            //this is send to ->other location, evaluate on source with "open" action
            if (_SPEEvalAttr.RequestURL_path.EndsWith("/copyresults.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("/copy.aspx", StringComparison.OrdinalIgnoreCase))
            {
                string strSrcUrl = Request.QueryString["SourceUrl"];
                if (!string.IsNullOrEmpty(strSrcUrl))
                {
                    try
                    {
                        _SPEEvalAttr.Action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Read);
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPListItem curItem = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, strSrcUrl, Utilities.SPUrlListItem);
                        if (curItem != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(curItem, _SPEEvalAttr);
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "Exception during SendtoOtherlocaiton:", null, ex);
                        if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                        {
                            bReturnDirectly = true;
                            return false;
                        }
                    }
                }
            }

            //access list from mobiles (ios/Android)
            if (_SPEEvalAttr.RequestURL_path.EndsWith("_layouts/15/mobile/viewa.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("_layouts/15/mobile/dispforma.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("_layouts/15/mobile/viewdaily.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("_layouts/mobile/view.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("_layouts/mobile/dispform.aspx", StringComparison.OrdinalIgnoreCase) ||
                _SPEEvalAttr.RequestURL_path.EndsWith("_layouts/mobile/viewdaily.aspx", StringComparison.OrdinalIgnoreCase) ||
                (_SPEEvalAttr.RequestURL_path.EndsWith("_layouts/15/touchapp.aspx", StringComparison.OrdinalIgnoreCase))
                )
            {
                try
                {
                    ProcessRequestFromMobile(Request);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during libraries accessed by mobile, webUrl:[{0}], loginName:[{1}], remoteAddr:[{2}]", new object[] { _SPEEvalAttr.WebUrl, _SPEEvalAttr.LoginName, _SPEEvalAttr.RemoteAddr }, ex);
                    if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                    {
                        bReturnDirectly = true;
                        return false;
                    }
                }
            }
            // SPE file version metadata enforcement.
            if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/versions.aspx?", StringComparison.OrdinalIgnoreCase)
                || -1 != _SPEEvalAttr.RequestURL.IndexOf("/DispForm.aspx", StringComparison.OrdinalIgnoreCase)
                 || -1 != _SPEEvalAttr.RequestURL.IndexOf("/_vti_history/", StringComparison.OrdinalIgnoreCase))
            {
                CheckItemFileVersion(Request, _SPEEvalAttr);
            }

            // fix bug 50340 (SP2019, open site contents)
            if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/_layouts/15/viewlsts.aspx", StringComparison.OrdinalIgnoreCase))
            {
                if (_SPEEvalAttr.WebObj != null)
                {
                    _SPEEvalAttr.Action = "Open Site";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                }
            }
#if SP2019
            // fix bug 50374 (SP2019, OWA open file)
            else if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/_layouts/15/Doc.aspx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string listId = Request.QueryString["ListId"];
                    string listItemId = Request.QueryString["ListItemId"];
                    if (!string.IsNullOrEmpty(listId) && !string.IsNullOrEmpty(listItemId) && _SPEEvalAttr.WebObj != null)
                    {
                        SPList list = _SPEEvalAttr.WebObj.Lists[new Guid(listId)];
                        if (list != null)
                        {
                            SPListItem item = list.GetItemById(int.Parse(listItemId));
                            if (item != null)
                            {
                                _SPEEvalAttr.Action = "Open";
                                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            //  SP2019 open event item in communication site.
            else if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/_layouts/15/Event.aspx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string listId = Request.QueryString["ListGuid"];
                    string listItemId = Request.QueryString["ItemId"];
                    if (!string.IsNullOrEmpty(listId) && !string.IsNullOrEmpty(listItemId) && _SPEEvalAttr.WebObj != null)
                    {
                        SPList list = _SPEEvalAttr.WebObj.Lists[new Guid(listId)];
                        if (list != null)
                        {
                            SPListItem item = list.GetItemById(int.Parse(listItemId));
                            if (item != null)
                            {
                                _SPEEvalAttr.Action = "Open";
                                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
#endif
            //  Open the discussion list item.
            else if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/SitePages/Topic.aspx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string rootFolder = Request.QueryString["RootFolder"];
                    if (!string.IsNullOrEmpty(rootFolder) && _SPEEvalAttr.WebObj != null)
                    {
                        SPListItem item = _SPEEvalAttr.WebObj.GetListItem(rootFolder);
                        if (item != null)
                        {
                            _SPEEvalAttr.Action = "Open";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                            SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                        }
                    }
                }
                catch
                {
                }
            }

            //to check owa 2013 case
            if (Request.Url.AbsoluteUri.IndexOf("_layouts/15/xlviewer.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/15/WopiFrame.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/15/WopiFrame2.aspx", StringComparison.OrdinalIgnoreCase) > 0)
            {
                SPListItem item = null;
                string sourcedoc = Request.QueryString["sourcedoc"];

                if (_SPEEvalAttr.WebObj != null && !string.IsNullOrEmpty(sourcedoc))
                {
                    Guid guidItem = Guid.Empty;
                    bool bGuid = Guid.TryParse(sourcedoc, out guidItem);
                    try
                    {
                        SPFile file = null;
                        if (bGuid)
                        {
                            file = _SPEEvalAttr.WebObj.GetFile(guidItem);
                        }
                        else
                        {
                            file = _SPEEvalAttr.WebObj.GetFile(sourcedoc);
                        }

                        if (file != null && file.Item != null)
                        {
                            item = file.Item;
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during CheckIsOPenByOWA webCur.GetFile:", null, ex);
                    }
                    if (Request.UrlReferrer != null && item == null)
                    {
                        SPList listTarget = null;
                        try
                        {
                            listTarget = _SPEEvalAttr.WebObj.GetListFromUrl(Request.UrlReferrer.AbsoluteUri);
                        }
                        catch { }//just try to get list rom url;no need to do anything if failed

                        //it is for document set case.
                        if (listTarget == null)
                        {
                            string strListGuid = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["List"];
                            if (!string.IsNullOrEmpty(strListGuid))
                            {
                                Guid guidList = Guid.Empty;
                                if (Guid.TryParse(strListGuid, out guidList))
                                {
                                    listTarget = _SPEEvalAttr.WebObj.Lists.GetList(guidList, true);
                                }
                            }
                        }

                        if (listTarget != null)
                        {
                            try
                            {
                                item = listTarget.GetItemByUniqueId(guidItem);
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Debug, "Exception during CheckIsOPenByOWA listTarget.GetItemByUniqueId:", null, ex);
                            }
                        }
                    }
                }
                if (item != null)
                {
                    _SPEEvalAttr.Action = "Open";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                    SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                }
            }

            if (_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
            {
                if (_SPEEvalAttr.RequestURL_path != null
                    && (_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/download.aspx", StringComparison.OrdinalIgnoreCase)
                    || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/15/download.aspx", StringComparison.OrdinalIgnoreCase)
                    || _SPEEvalAttr.RequestURL_path.EndsWith("/_vti_bin/owssvr.dll", StringComparison.OrdinalIgnoreCase)
                    || _SPEEvalAttr.RequestURL_path.EndsWith("formserver.aspx", StringComparison.OrdinalIgnoreCase)
                    || _SPEEvalAttr.RequestURL_path.EndsWith("flat.aspx", StringComparison.OrdinalIgnoreCase)))
                {
                    String _page = _SPEEvalAttr.RequestURL_path.Substring(_SPEEvalAttr.RequestURL_path.LastIndexOf("/") + 1);
                    SPEModuleBase _SPEModuleBase = SPEClass.GetSPEClass(_page);
                    if (_SPEModuleBase != null)
                    {
                        _SPEModuleBase.Init(Request);

                        //if return value is true, means excpetion was caught.
                        if (_SPEModuleBase.DoSPEProcess())
                        {
                            if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
                            {
                                bReturnDirectly = true;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        if (SPE_GetEval(Request))
                        {
                            bReturnDirectly = true;
                            return true;
                        }
                    }
                }
                else
                {
                    if (SPE_GetEval(Request))
                    {
                        bReturnDirectly = true;
                        return true;
                    }
                }
            }
            return true;
        }
        private bool ParsePropFindRequest(HttpApplication application)
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            //HttpResponse Response = context.Response;
            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
            _SPEEvalAttr.Action = "READ";
            //William add this to treat all propfind command as no log action evaluation in 20090224
            _SPEEvalAttr.NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_APPLICATION;
            if (_SPEEvalAttr.ItemObj == null)
            {
                Object obj = null;
                try
                {
                    obj = _SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.RequestURL);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ParsePropFindRequest:", null, ex);
                }

                if (obj != null)
                {
                    Type type = obj.GetType();
                    // URL is not an item.
                    //SPFolder folder = null;
                    if (Object.ReferenceEquals(type, typeof(SPFolder)))
                    {
                        //folder = (SPFolder)obj;
                        _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode(Request.Url.GetLeftPart(UriPartial.Path));
                        //Check if it is SPList, fix bug 9807, by William 20090828
                        SPList _List = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, Globals.UrlDecode(Request.Url.GetLeftPart(UriPartial.Path)), Utilities.SPUrlList);
                        if (_List != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(_List, _SPEEvalAttr);
                        }
                        else
                        {
                            try
                            {
                                SPWeb _web = SPControl.GetContextWeb(context);
                                if (_web != null)
                                {
                                    SPEEvalAttrHepler.SetObjEvalAttr(_web, _SPEEvalAttr);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    // Add documentset and folder open in pop dialog case.
                    else if (Object.ReferenceEquals(type, typeof(SPListItem)))
                    {
                        SPListItem item = obj as SPListItem;
                        if (item != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                        }
                    }
                    else
                    {
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.RequestURL;
                        SPList _List = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.RequestURL, Utilities.SPUrlList);
                        if (_List != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(_List, _SPEEvalAttr);
                        }
                    }

                }
                else
                {
                    _SPEEvalAttr.Action = "UNKNOWN_ACTION";
                }

            }
            return true;
        }
        private bool ParseMoveRequest(HttpApplication application)
        {
            bool bRet = true;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            //HttpResponse Response = context.Response;
            // IE6.0: Rename document file in Sharepoint Explorer View, HTTP Method = "MOVE"
            _SPEEvalAttr.Action = "Edit";
            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
            _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode(Request.Url.AbsoluteUri);
            _SPEEvalAttr.ObjTargetUrl = Request.Headers["Destination"];
            if (_SPEEvalAttr.ObjTargetUrl != null)
                _SPEEvalAttr.ObjTargetUrl = Globals.UrlDecode(_SPEEvalAttr.ObjTargetUrl);
            //If action is rename, it is write action, else it is a move
            //if a rename, the former url euqals, else not
            {
                String _source = null;
                String _target = null;
                int index = 0; ;
                if (_SPEEvalAttr.ObjEvalUrl.EndsWith("/"))
                {
                    index = _SPEEvalAttr.ObjEvalUrl.LastIndexOf("/");
                    _source = _SPEEvalAttr.ObjEvalUrl.Substring(0, index);
                    index = _source.LastIndexOf("/");
                    _source = _source.Substring(0, index);
                }
                else
                {
                    index = _SPEEvalAttr.ObjEvalUrl.LastIndexOf("/");
                    _source = _SPEEvalAttr.ObjEvalUrl.Substring(0, index);
                }
                if (_SPEEvalAttr.ObjTargetUrl.EndsWith("/"))
                {
                    index = _SPEEvalAttr.ObjTargetUrl.LastIndexOf("/");
                    _target = _SPEEvalAttr.ObjTargetUrl.Substring(0, index);
                    index = _target.LastIndexOf("/");
                    _target = _target.Substring(0, index);
                }
                else
                {
                    index = _SPEEvalAttr.ObjTargetUrl.LastIndexOf("/");
                    _target = _SPEEvalAttr.ObjTargetUrl.Substring(0, index);
                }
                if (_source != null && _target != null
                    && _source.Equals(_target))
                {
                    _SPEEvalAttr.Action = "EDIT";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                }
            }

            try
            {
                _SPEEvalAttr.ItemObj = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlListItem);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during ParseMoveRequest:", null, ex);
            }
            if (_SPEEvalAttr.ItemObj == null)
                _SPEEvalAttr.ItemObj = Globals.ParseItemFromAttachmentURL(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl);

            if (_SPEEvalAttr.ItemObj != null)
            {

                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
            }
            else
            {
                try
                {
                    _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _SPEEvalAttr.ObjEvalUrl, Utilities.SPUrlList);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during ParseMoveRequest:", null, ex);
                }
                if (_SPEEvalAttr.ListObj != null)
                {

                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);

                }
                else
                {
                    if (_SPEEvalAttr.WebObj!=null)
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                }
            }
            return bRet;
        }

        private bool ParseRequest(HttpApplication application,ref bool bIsWebSvc, ref bool bReturnDirectly)
        {
            bool bRet = false;
            bReturnDirectly = false;
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            HttpResponse Response = context.Response;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // CSOM Module
            CSOMModule csomModule = new CSOMModule(Request, Response);
            if (csomModule.IsCsomRequest())
            {
                bool bAllow = csomModule.Run();
                if (!bAllow)
                {
                    blockRequest(application, Response, csomModule.DenyMessage);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            // SOAP Module
            SOAPModule soapModule = new SOAPModule(Request, Response);
            if (soapModule.IsSOAPRequest())
            {
                NLLogger.OutputLog(LogLevel.Debug, "ParseRequest: Enter soap module.");
                bool bAllow = soapModule.Run();
                if (!bAllow)
                {
                    blockRequest(application, Response, soapModule.DenyMessage);
                    return false;
                }
                else
                {
                    return true;
                }
            }


            RestAPIPQuery restApiQuery = new RestAPIPQuery(Request);
            // for REST API service request.
            if (RESTAPIModule.IsRESTAPIRequest(Request.RawUrl))
            {
                RESTAPIModule restApiModule = new RESTAPIModule(Request);
                bRet = restApiModule.DecodeRestApiRequest();
            }
            // George: Fix bug 30864, add lib in Sharepoint designer.
            else if (restApiQuery.IfNeedEvalQuery())
            {
                restApiQuery.SetEvalAttrs();
            }
            else if (_SPEEvalAttr.IsPost)
            {
                bRet = ParsePostRequest(application, ref bIsWebSvc, ref bReturnDirectly);
            }
            // fix bug 8464 by derek
            else if (_SPEEvalAttr.HttpMethod.Equals("MOVE", StringComparison.OrdinalIgnoreCase))
            {
                bRet = ParseMoveRequest(application);
            }
            else if (_SPEEvalAttr.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                bRet = ParseGetRequest(application, ref bReturnDirectly);
            }

            if (_SPEEvalAttr.HttpMethod.Equals("PROPFIND", StringComparison.OrdinalIgnoreCase))
            {
                bRet = ParsePropFindRequest(application);
            }
            return bRet;

        }

        public bool PreRequest(Object source, EventArgs e)
        {
            try
            {
                bool bResult = true;
				bool bIsWebSvc = false;
                string UserSid = "";
                // we check the request at here
                // Q: Why not check the request at begining?
                // A: It is too early at begining because many objects are not initialized properly.
                // NOTE: We only check POST method
                HttpApplication application = (HttpApplication)source;
                HttpRequest Request = application.Context.Request;
                HttpContext context = application.Context;
                HttpResponse Response = context.Response;

                // this fix is for 8214, so we can't comments them.
                if (Request == null)
                {
                    return true;
                }

                if (SPE_Preparation(source))
                    return true;
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();

                NLLogger.OutputLog(LogLevel.Debug, "SPEHttpModule PreRequest enter, thread ID = " +
                             Thread.CurrentThread.ManagedThreadId + ", URL = " +
                             _SPEEvalAttr.RequestURL + ", method = " + _SPEEvalAttr.HttpMethod);


                bool bNeedContinueParseAfterPlugins = true;
                if (PluginFrame.IsPluginEnabled())
                {
                    bNeedContinueParseAfterPlugins = ExecuteRequestPlugins(Request,Response);
                }
                if (bNeedContinueParseAfterPlugins)
                {
                    bool bReturnDirectly=false;
                    bool bRet = ParseRequest(application, ref bIsWebSvc, ref bReturnDirectly);
                    if (bReturnDirectly) return bRet;
                }
                //---------------------------------------------------------
                if (!_SPEEvalAttr.Action.EndsWith("UNKNOWN_ACTION", StringComparison.OrdinalIgnoreCase))
                {
                    // This is something that we want to check. Do Policy Check here.
                    CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
                    string[] emptyArray = new string[0];
                    string policyName = null;
                    string policyMessage = null;
                    string[] propertyArray = getProperty(bIsWebSvc, _SPEEvalAttr);

                    // modified by derek for bug8788
                    if ((_SPEEvalAttr.PolicyAction == CETYPE.CEAction.Move) || (_SPEEvalAttr.PolicyAction == CETYPE.CEAction.Copy))
                    {
                        //this is for RPC move, because RPC move is not considered as move now,
                        //we don't process it in httpmodule. however,
                        //it will invoke sp events, so you will be treated it in events.
                    }
                    else
                    {
                        //bear fix bug 24702,2014424,if get sid form _SPEEvalAttr.WebObj.CurrentUser.Sid,it will return a wrong value
                        if (string.IsNullOrEmpty(UserSid) && _SPEEvalAttr.IsOWA)
                        {
                            UserSid = NextLabs.Common.UserSid.GetUserSid(context);
                            //fix bug 24888,when user is FBA and ADFS, we can't get sid form AD
                            if (string.IsNullOrEmpty(UserSid))
                            {
                                UserSid = _SPEEvalAttr.LogonUser;
                            }
                        }
                        string sid = "";
                        if (!string.IsNullOrEmpty(UserSid))
                        {
                            sid = UserSid;
                        }
                        else
                        {
                            sid = _SPEEvalAttr.WebObj.CurrentUser.Sid;
                        }

                        //to fix bug 9973 rebuild the url. modified by William 20090914
                        _SPEEvalAttr.ObjEvalUrl = Globals.HttpModule_ReBuildURL(_SPEEvalAttr.ObjEvalUrl, Request.FilePath, Request.Path);

                        response = Globals.CallEval(_SPEEvalAttr.PolicyAction,
                                                    _SPEEvalAttr.ObjEvalUrl,
                                                    _SPEEvalAttr.ObjTargetUrl,  // derek bug8612
                                                    ref propertyArray,
                                                    ref emptyArray,
                                                    _SPEEvalAttr.RemoteAddr,
                                                    _SPEEvalAttr.LogonUser,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    _SPEEvalAttr.BeforeUrl,
                                                    _SPEEvalAttr.AfterUrl,
                                                    Globals.HttpModuleName,
                                                    _SPEEvalAttr.NoiseLevel,
                                                    _SPEEvalAttr.WebObj,
                                                    null);
                        if (_SPEEvalAttr.IsOWA && (response == CETYPE.CEResponse_t.CEAllow) && (_SPEEvalAttr.PolicyAction == CETYPE.CEAction.Write))
                        {
                            //When policy is deny open, but user is click "edit in browser", in this case need to do two time evalutation.

                            response = Globals.CallEval(CETYPE.CEAction.Read,
                                                        _SPEEvalAttr.ObjEvalUrl,
                                                        _SPEEvalAttr.ObjTargetUrl,  // derek bug8612
                                                        ref propertyArray,
                                                        ref emptyArray,
                                                        _SPEEvalAttr.RemoteAddr,
                                                        _SPEEvalAttr.LogonUser,
                                                        sid,
                                                        ref policyName,
                                                        ref policyMessage,
                                                        _SPEEvalAttr.BeforeUrl,
                                                        _SPEEvalAttr.AfterUrl,
                                                        Globals.HttpModuleName,
                                                        _SPEEvalAttr.NoiseLevel,
                                                        _SPEEvalAttr.WebObj,
                                                        null);
                        }
                        else if (response == CETYPE.CEResponse_t.CEAllow && _SPEEvalAttr.Action.Equals("Delete File Version"))
                        {
							// Do "Edit" action for deleting history file version.
                            propertyArray = GetPropertyForNonHistoryVersion(bIsWebSvc, _SPEEvalAttr);

                            response = Globals.CallEval(CETYPE.CEAction.Write,
                                                    _SPEEvalAttr.ObjEvalUrl,
                                                    _SPEEvalAttr.ObjTargetUrl,
                                                    ref propertyArray,
                                                    ref emptyArray,
                                                    _SPEEvalAttr.RemoteAddr,
                                                    _SPEEvalAttr.LogonUser,
                                                    sid,
                                                    ref policyName,
                                                    ref policyMessage,
                                                    _SPEEvalAttr.BeforeUrl,
                                                    _SPEEvalAttr.AfterUrl,
                                                    Globals.HttpModuleName,
                                                    _SPEEvalAttr.NoiseLevel,
                                                    _SPEEvalAttr.WebObj,
                                                    null);
                        }
                    }

                    if (response == CETYPE.CEResponse_t.CEDeny)
                    {
                        //to fix 8108 and 8393 and the same problem, we use spweb.url as the backurl.
                        //if the denied url is current site url, we go up one level
                        String backurl = "";
                        String httpserver = "";
                        String msg = "";
                        Common.Utilities.GenerateBackUrl(Request, policyName, policyMessage, ref backurl, ref httpserver, ref msg);

                        // George: fix bug 51438, unfriendly alert message by rest api.
                        if (RESTAPIModule.IsRESTAPIRequest(Request.RawUrl))
                        {
                            // Block rest api with statuscode "500".
                            BlockRestApiRequest(application, Response, msg);
                            return false;
                        }

                        // when client have slivelight installed in create library, we can't show customer error page.
                        bool bCreateLibInSliveLightBrowser = false;
                        if(Request.Url.AbsoluteUri.EndsWith("AddGallery.aspx"))
                        {
                            string strFormTask = Request.Form["Task"];
                            if( (!string.IsNullOrEmpty(strFormTask)) && strFormTask.Equals("CreateList", StringComparison.OrdinalIgnoreCase) )
                            {
                                bCreateLibInSliveLightBrowser = true;
                            }
                        }

                        if ((!CustomDenyPageSwitch.IsEnabled()) || bCreateLibInSliveLightBrowser)
                        {
                            bool bAppDocumentSetBlock = _SPEEvalAttr.ItemObj != null
                                && _SPEEvalAttr.ItemObj.ContentType.Name.Equals("Document Set", StringComparison.CurrentCultureIgnoreCase)
                                && -1 != Request.Url.AbsoluteUri.IndexOf("_vti_bin/owssvr.dll", StringComparison.OrdinalIgnoreCase)
                                && (Request.QueryString["dialogview"] != null) && Request.QueryString["dialogview"].Equals("fileopen", StringComparison.OrdinalIgnoreCase);
                            if (bAppDocumentSetBlock)
                            {
                                blockDocumentSetAppRequest(Request, Response, application);
                            }
                            else
                            {
                                blockRequest(application, Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                            }
                        }
                        else
                        {
                            string strWebUrl = Utilities.GetWebsiteUrl();
                            if (string.IsNullOrEmpty(strWebUrl))
                            {
                                if (_SPEEvalAttr.WebObj != null)
                                {
                                    strWebUrl = _SPEEvalAttr.WebObj.Url;
                                }
                            }

                            if (string.IsNullOrEmpty(strWebUrl))
                            {
                                blockRequest(application, Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                            }
                            else
                            {
                                Response.Redirect(strWebUrl + "/_layouts/error-template/DenyPage.aspx?loginName=" + HttpUtility.UrlEncode(_SPEEvalAttr.LoginName) + "&resouceID=" + HttpUtility.UrlEncode(_SPEEvalAttr.ObjEvalUrl) + "&policyMessage=" + HttpUtility.UrlEncode(msg), false);
                                context.ApplicationInstance.CompleteRequest();
                            }
                        }

                        bResult = false;
                    }
                }

                // Trimming model, depend some enforcement attributes.
                {
                    // Do trimming for some rest api cases(webs/lists/files/folders).
                    if (RESTAPIModule.IsRESTAPIRequest(Request.RawUrl))
                    {
                        RestApiTrimming RestApiTrim = new RestApiTrimming();
                        string restApiUrl = Request.RawUrl;
                        RESTAPIModule.DecodeSpecificUrl(Request, ref restApiUrl);
                        RestApiTrim.DoTrimming(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.RemoteAddr, restApiUrl);
                    }

                    // Do trimming for dialog view, sharepoint designer and search(SP2016).
                    SpServiceTrimming serviceTrim = new SpServiceTrimming();
                    serviceTrim.DoTrimming(context, _SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.RemoteAddr);
                }
                NLLogger.OutputLog(LogLevel.Debug, "SPEHttpModule PreRequest leave, thread ID = " + Thread.CurrentThread.ManagedThreadId);
                return bResult;
            }
            catch (Exception ex)
            {
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                if ((_SPEEvalAttr.RequestURL_path.EndsWith("author.dll", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("owssvr.dll", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("admin.dll", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase)))
                {
                    HttpContext.Current.Request.Headers["X_READ"] = "1";
                    HttpContext.Current.Server.TransferRequest(HttpContext.Current.Request.RawUrl, true, HttpContext.Current.Request.HttpMethod, HttpContext.Current.Request.Headers);
                }
                if (Object.ReferenceEquals(ex.GetType(), typeof(System.NullReferenceException)))
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during PreRequestHandlerExecute:", null, ex);
                }
                else
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during PreRequestHandlerExecute:", null, ex);
                }

                HttpApplication application = (HttpApplication)source;
                HttpRequest Request = application.Context.Request;
                HttpContext context = application.Context;
                HttpResponse Response = context.Response;

                if (!EnforceByDefaultSettings(Request, Response, application, _SPEEvalAttr.LoginName))
				{
					return false;
				}
				else
				{
					return true;
				}

            }
            finally
            {
                //do nothing now
            }
        }

        private string[] GetPropertyForNonHistoryVersion(bool bIsWebSvc, SPEEvalAttr _SPEEvalAttr)
        {
            int iPropLen = 5;
            if (bIsWebSvc)
                iPropLen = 6;
            string[] propertyArray = new string[iPropLen * 2];

            propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
            propertyArray[0 * 2 + 1] = _SPEEvalAttr.ObjName;
            propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            propertyArray[1 * 2 + 1] = _SPEEvalAttr.ObjTitle;
            propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
            propertyArray[2 * 2 + 1] = _SPEEvalAttr.ObjDesc;
            propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_TYPE;
            propertyArray[3 * 2 + 1] = _SPEEvalAttr.ObjType;
            propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_SUBTYPE;
            propertyArray[4 * 2 + 1] = _SPEEvalAttr.ObjSubtype;
            if (bIsWebSvc)
            {
                propertyArray[5 * 2 + 0] = "EvaluationOrigin";
                propertyArray[5 * 2 + 1] = "WebServiceEntitlement";
            }
            if (_SPEEvalAttr.ItemObj != null)
            {
                int oldLen = propertyArray.Length;
                string[] newArray = new string[oldLen + 5 * 2 + 2];

                for (int i = 0; i < oldLen; i++)
                {
                    newArray[i] = propertyArray[i];
                }

                newArray[oldLen + 0 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_CREATED_BY;
                newArray[oldLen + 0 * 2 + 1] = Globals.
                    GetItemCreatedBySid(_SPEEvalAttr.ItemObj);
                newArray[oldLen + 1 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_MODIFIED_BY;
                newArray[oldLen + 1 * 2 + 1] = Globals.
                    GetItemModifiedBySid(_SPEEvalAttr.ItemObj);
                newArray[oldLen + 2 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_DATE_CREATED;
                newArray[oldLen + 2 * 2 + 1] = Globals.
                    GetItemCreatedStr(_SPEEvalAttr.ItemObj);
                newArray[oldLen + 3 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_DATE_MODIFIED;
                newArray[oldLen + 3 * 2 + 1] = Globals.
                    GetItemModifiedStr(_SPEEvalAttr.ItemObj);
                newArray[oldLen + 4 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_FILE_SIZE;
                newArray[oldLen + 4 * 2 + 1] = Globals.
                    GetItemFileSizeStr(_SPEEvalAttr.ItemObj);

                propertyArray = newArray;

                // Add other fixed and custom item attributes to the array.
                propertyArray = Globals.BuildAttrArrayFromItemProperties
                    (_SPEEvalAttr.ItemObj.Properties, propertyArray,
                     _SPEEvalAttr.ItemObj.ParentList.BaseType, _SPEEvalAttr.ItemObj.Fields);
                //Fix bug 8222, replace the "created" and "modified" properties
                propertyArray = Globals.ReplaceHashTime(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, propertyArray);
                //Fix bug 8694 and 8692,add spfield attr to tailor
                propertyArray = Globals.BuildAttrArray2FromSPField(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, _SPEEvalAttr.ItemObj, propertyArray);
            }
            return propertyArray;
        }

        // Get the item properties.
        private string[] getProperty(bool bIsWebSvc, SPEEvalAttr _SPEEvalAttr)
        {
            int iPropLen = 5;
            if (bIsWebSvc)
                iPropLen = 6;
            string[] propertyArray = new string[iPropLen * 2];

            if (string.IsNullOrEmpty(_SPEEvalAttr.ObjName))
                _SPEEvalAttr.ObjName = _SPEEvalAttr.ObjTitle;
            else if (string.IsNullOrEmpty(_SPEEvalAttr.ObjTitle))
                _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName;
            if (_SPEEvalAttr.IsOWA && _SPEEvalAttr.ItemObj != null)
            {
                propertyArray = _SPEEvalAttr.Params4OWA;
            }
            else
            {
                if (!string.IsNullOrEmpty(_SPEEvalAttr.FileVersion) && _SPEEvalAttr.ItemObj != null)  //Get property from history file verion.
                {
                    string strVersionNo = _SPEEvalAttr.FileVersion;
                    //Fix bug 44242, use history "item Version" to replace "File Version" properties.
                    SPListItemVersion itemVersion = _SPEEvalAttr.ItemObj.Versions.GetVersionFromID(int.Parse(strVersionNo));
                    if (itemVersion != null)
                    {
                        propertyArray = BuildAttrArrayFromSPItemVersion(itemVersion);
                    }
                }
                else
                {
                    propertyArray = GetPropertyForNonHistoryVersion(bIsWebSvc, _SPEEvalAttr);
                }
            }
            return propertyArray;
        }

        public void EndRequest(Object sender, EventArgs e)
        {
            try
            {
                HttpApplication app = (HttpApplication)sender;
               // HttpContext context = app.Context;
                HttpRequest request = app.Request;
                HttpResponse response = app.Response;



                if (response.StatusCode == (int)HttpStatusCode.Forbidden)
                    return;
                if (response.StatusCode != 200)
                    return;
                if (!request.FilePath.EndsWith("/_vti_bin/owssvr.dll", StringComparison.OrdinalIgnoreCase))
                    return;
                string web_url = "";
                int subsitelen = request.RawUrl.IndexOf("/_vti_bin/", StringComparison.OrdinalIgnoreCase);
                int topsitelen = request.Url.AbsoluteUri.IndexOf("/_vti_bin/", StringComparison.OrdinalIgnoreCase);
                if (subsitelen >= 0 && topsitelen >= 0)
                {
                    string subsitename = request.RawUrl.Substring(0, subsitelen);
                    string topsitename = request.Url.AbsoluteUri.Substring(0, topsitelen);
                    StringBuilder fullsitename = new StringBuilder(topsitename);
                    fullsitename.Append(subsitename);
                    web_url = fullsitename.ToString();
                }
                using (SPSite spsite = Globals.GetValidSPSite(web_url, HttpContext.Current))
                {
                    using (SPWeb spweb = spsite.OpenWeb())
                    {
                        if (spweb == null)
                            return;
                        if (request.ContentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase))
                        {
                            XmlTextReader reader = new XmlTextReader(request.InputStream);
                            reader.Namespaces = false;
                            string Cmd = "";
                            string ListTemplate = "";
                            string Title = "";
                            string FeatureId = "";
                           // string CreateList = "";
                            try
                            {
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        if (reader.Name.Equals("SetVar", StringComparison.OrdinalIgnoreCase))
                                        {
                                            while (reader.MoveToNextAttribute())
                                            {
                                                if (reader.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (reader.Value.Equals("Cmd", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        Cmd = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("ListTemplate", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        ListTemplate = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("Title", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        Title = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("FeatureId", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        FeatureId = reader.ReadString();
                                                    }
                                                    else if (reader.Value.Equals("CreateLists", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        //CreateList = reader.ReadString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Warn, "Exception during EndRequest:", null, ex);
                                return;
                            }
                            if (Cmd.Equals("NewList", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ListTemplate) && !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(FeatureId))
                            {
                                SPList splist = spweb.Lists[Title];
                                if (splist == null)
                                    return;
                                SPEventReceiverType[] receiverTypes = new SPEventReceiverType[]{
                                                                                                SPEventReceiverType.ItemAdding,
                                                                                                SPEventReceiverType.ItemAdded,
                                                                                                SPEventReceiverType.ItemUpdating,
                                                                                                SPEventReceiverType.ItemDeleting,
                                                                                                SPEventReceiverType.ItemAttachmentAdding,
                                                                                                SPEventReceiverType.ItemAttachmentAdded,
                                                                                                SPEventReceiverType.ItemFileMoving
                                                                                                };
                                string[] receiverNames = new string[]{
                                                                        "ItemAddingEventHandler",
                                                                        "ItemAddedEventHandler",
                                                                        "ItemUpdatingEventHandler",
                                                                        "ItemDeletingEventHandler",
                                                                        "ItemAttachmentAddingEventHandler",
                                                                        "ItemAttachmentAddedEventHandler",
                                                                        "ItemFileMovingEventHandler"
                                                                      };
                                string assemblyFullName = "NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e";
                                string assemblyClassName = "NextLabs.SPEnforcer.ItemHandler";

                                for (int i = 0; i < receiverTypes.Length; ++i)
                                {
                                    if (!CheckListReceiverExisting(splist, receiverTypes[i], assemblyFullName, assemblyClassName))
                                    {
                                        AddListReceiver(splist, receiverTypes[i], receiverNames[i], assemblyFullName, assemblyClassName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during EndRequest:", null, ex);
            }
            finally
            {
                //Do nothing now
            }
        }

        /*enforce  the current action according to the registry setting
         * return true if allowed, otherwise return false.
         */
        private bool EnforceByDefaultSettings(HttpRequest Request, HttpResponse Response, HttpApplication application, string strLoginName)
        {
            bool bRet = true;
            CETYPE.CEResponse_t resp = Globals.GetPolicyDefaultBehavior() ? CETYPE.CEResponse_t.CEAllow : CETYPE.CEResponse_t.CEDeny;
            if (resp == CETYPE.CEResponse_t.CEDeny)
            {
                bRet = false;
                string policyMessage = Globals.GetDenyByExceptionMsg();
                string policyName = "None";
                String backurl = "";
                String httpserver = "";
                String msg = "";
                Common.Utilities.GenerateBackUrl(Request, policyName, policyMessage, ref backurl, ref httpserver, ref msg);
                if (!CustomDenyPageSwitch.IsEnabled())
                {
                    blockRequest(application, Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                }
                else
                {
                    Response.Redirect(httpserver + "/_layouts/error-template/DenyPage.aspx?loginName=" + HttpUtility.UrlEncode(strLoginName), false);
                    application.CompleteRequest();
                }
            }
            return bRet;
        }

        private void blockRequest(HttpApplication app, HttpResponse Response, String StatusDescription)
        {
			CommonVar.Clear();
            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            Response.ContentType = "text/html";
            Response.Write(StatusDescription);

            // 2009/03/10 ayuen:
            // Don't call Response.Flush() here, because calling it causes
            // Response.End() below to generate ThreadAbortException in some
            // cases.  I don't understand why the problem happens, and I don't
            // understand why removing the call fixes the problem either.
            app.CompleteRequest();
        }

        private void BlockRestApiRequest(HttpApplication app, HttpResponse Response, string denyMessage)
        {
            CommonVar.Clear();

            string StatusDescription = "{"
                + "\"error\":{"
                + "\"code\":\"-2130575223, Microsoft.SharePoint.SPException\","
                + "\"message\":{"
                + "\"lang\":\"en-US\","
                + "\"value\":\"" + denyMessage + "\"}}}";
            Response.Clear();
            Response.StatusCode = 500;
            Response.ContentType = "application/json;charset=utf-8";
            byte[] buff = Encoding.UTF8.GetBytes(StatusDescription);
            Response.BinaryWrite(buff);
            app.CompleteRequest();
        }

        private void blockDocumentSetAppRequest(HttpRequest Request, HttpResponse Response, HttpApplication app)
        {
            SPWeb web = null;
            try
            {
                web = SPControl.GetContextWeb(app.Context);
            }
            catch
            {
            }
            ResponseFilter filter = ResponseFilters.Current(Response);
            filter.AddFilterType(FilterType.RestApiTrimmer);
            filter.Request = Request;
            filter.Web = web;
        }

        private bool CheckListReceiverExisting(SPList splist, SPEventReceiverType ReceiverType, string Assembly, string Class)
        {
            // The caller should ensure splist is NOT null

            // Walk through list's all Event Receivers
            SPEventReceiverDefinitionCollection AllEventReceivers = splist.EventReceivers;
            if (AllEventReceivers.Count == 0)
                return false;
            foreach (SPEventReceiverDefinition it in AllEventReceivers)
            {
                if (it.Type == ReceiverType)
                {
                    if (it.Assembly.Equals(Assembly, StringComparison.OrdinalIgnoreCase) &&
                        it.Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void AddListReceiver(SPList splist, SPEventReceiverType ReceiverType, string ReceiverName, string Assembly, string Class)
        {
            // The caller should ensure splist is NOT null
            SPEventReceiverDefinitionCollection eventReceivers = splist.EventReceivers;
            SPEventReceiverDefinition receiverDefinition = eventReceivers.Add();
            receiverDefinition.Name = ReceiverName;
            receiverDefinition.Type = ReceiverType;
            receiverDefinition.SequenceNumber = 20000;
            receiverDefinition.Synchronization = SPEventReceiverSynchronization.Synchronous;
            receiverDefinition.Assembly = Assembly;
            receiverDefinition.Class = Class;
            receiverDefinition.Update();
        }

        public void BeginProcessUploadEvent(object sender)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;
            HttpRequest request = app.Request;

            if (request.HttpMethod.Equals("POST") &&
                (request.Url.AbsoluteUri.IndexOf("/_layouts/upload.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || request.Url.AbsoluteUri.IndexOf("/_layouts/15/upload.aspx", StringComparison.OrdinalIgnoreCase) > 0))
            {
                string listGuid = request.QueryString["List"];
                Guid _listGuid = new Guid(listGuid);
                try
                {
                    SPWeb _webObj = SPControl.GetContextWeb(context);
                    if (_webObj != null)
                    {
                        if (_webObj.WebTemplateId == 21)
                        {
                            return;
                        }

                        SPList list = _webObj.Lists.GetList(_listGuid, true);
                        if (!Globals.CheckListProperty(list, Globals.strLibraryProcessUploadPropName))
                            return;
                        if (listGuid != null && !listGuid.StartsWith("{"))
                            listGuid = "{" + listGuid + "}";
                        if (request.QueryString["MultipleUpload"] == null && listGuid != null)
                        {
                            UploadSyncObject UploadSync = UploadSyncObject.CreateInstance();
                            UploadSync.AddNewItem(context.User.Identity.Name, request.UserHostAddress, listGuid);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public void EndProcessUploadEvent(object sender)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;
            HttpRequest request = app.Request;

            try
            {
                if (request.HttpMethod.Equals("GET") && request.Url.AbsoluteUri.IndexOf("/Forms/EditForm.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    SPWeb _webObj = SPControl.GetContextWeb(context);
                    SPList list = _webObj.GetListFromWebPartPageUrl(request.Url.AbsoluteUri);
                    if (!Globals.CheckListProperty(list, Globals.strLibraryProcessUploadPropName))
                        return;
                    string listGuid = "{" + list.ID.ToString() + "}";
                    string mode = request.QueryString["Mode"];
                    if (mode != null && mode.Equals("Upload", StringComparison.OrdinalIgnoreCase))
                    {
                        UploadSyncObject UploadSync = UploadSyncObject.CreateInstance();
                        Int32 num = 0;

                        while (true)
                        {
                            num = UploadSync.QueryItem(context.User.Identity.Name, request.UserHostAddress, listGuid);
                            if (num != 0)
                            {
                                Thread.Sleep(300);
                            }
                            else
                            {
                                Thread.Sleep(Globals.sleepTimeWhenUpload);
                                if (Globals.succeedCountForUpload >= 100 && Globals.sleepTimeWhenUpload >= 1100)
                                {
                                    Globals.sleepTimeWhenUpload -= 100;
                                    Globals.succeedCountForUpload = 0;
                                }
                                break;
                            }
                        }
                        UploadSync.DeleteItem(context.User.Identity.Name, request.UserHostAddress, listGuid);
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during EndProcessUploadEvent:", null, ex);
            }
        }

        private bool isStrInArrayNoCase(string s, string[] arr)
        {
            foreach (string s2 in arr)
            {
                if (s.Equals(s2, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // See if the passed dir name is a SharePoint built-in dir for sites.
        // Trailing slash is allowed.
        private bool isDirSPSiteBuiltinDir(string dirName)
        {
            string[] SPSiteBuiltinDirs = { "Pages" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPSiteBuiltinDirs);
        }

        // See if the passed dir name is a SharePoint built-in dir for doc
        // libs.  Trailing slash is allowed.
        private bool isDirSPDocLibBuiltinDir(string dirName)
        {
            string[] SPDocLibBuiltinDirs = { "Forms" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPDocLibBuiltinDirs);
        }

        // See if the passed dir name is a SharePoint built-in dir for
        // non-doc-lib lists.  Trailing slash is allowed.
        private bool isDirSPOtherListBuiltinDir(string dirName)
        {
            string[] SPOtherListBuiltinDirs = { "Lists" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPOtherListBuiltinDirs);
        }



        // See if the passed file name is a SharePoint standard content page
        // for sites and webs.
        private bool isFileSPStdWebContentPage(string fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdWebContentPages);
        }

        // See if the passed file name is a SharePoint standard content page
        // for document libraries.
        private bool isFileSPStdDocLibContentPage(string fileName)
        {
            return (isStrInArrayNoCase(fileName, SPStdDocLibContentPages) ||
                    isFileSPStdListContentPage(fileName));
        }

        // See if the passed file name is a SharePoint standard content page
        // for lists.
        private bool isFileSPStdListContentPage(string fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdListContentPages);
        }

        // See if the passed file name is a SharePoint standard item query page
        // for lists.
        private bool isFileSPStdListItemQueryPage(string fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdListItemQueryPages);
        }

        // See if the passed file name may be a content page for lists.
        private bool isFileMaybeListContentPage(string fileName)
        {
            return !fileName.Contains("/") &&
                fileName.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase);
        }

        // See if the passed query is for a list item
        private bool isQueryListItem(string query)
        {
            return query.Contains("?ID=") || query.Contains("&ID=");
        }

        // See if the passed URL represents a site.
        private bool isUrlSite(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Pages/default.aspx
            return (len >= 3 &&
                    isDirSPSiteBuiltinDir(segments[len - 2]) &&
                    isFileSPStdWebContentPage(segments[len - 1]));
        }

        // See if the passed URL represents a web.
        private bool isUrlWeb(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]<web>/default.aspx
            // where web cannot be "Pages"
            return (len >= 3 &&
                    !isDirSPSiteBuiltinDir(segments[len - 2]) &&
                    isFileSPStdWebContentPage(segments[len - 1]));
        }

        // See if the passed URL represents a document library.
        private bool isUrlDocLib(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]<doclib>/Forms/<aspx>[<query>]
            // where <doclib> cannot be "Lists", excluding the case when <aspx>
            // is a standard item query page and <query> contains ID.
            return (len >= 4 &&
                    !isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isDirSPDocLibBuiltinDir(segments[len - 2]) &&
                    isFileMaybeListContentPage(segments[len - 1]) &&
                    !(isFileSPStdListItemQueryPage(segments[len - 1]) &&
                      isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a non-doc-lib list.
        private bool isUrlOtherList(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Lists/<list>/<aspx>[<query>]
            // where <list> can be "Forms", excluding the case when <aspx> is a
            // standard item query page and <query> contains ID.
            return (len >= 4 &&
                    isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isFileMaybeListContentPage(segments[len - 1]) &&
                    !(isFileSPStdListItemQueryPage(segments[len - 1]) &&
                      isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a doc lib item *for sure*.
        //
        // Returns:     true if we are sure that the URL is a doc lib item.
        //              false if we are not sure whether or not the URL is a
        //              doc lib item.
        private bool isUrlDocLibItemForSure(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format we are detecting here is
            // <scheme>://<host>/[...]<doclib>/Forms/<aspx><query>
            // where <doclib> cannot be "Lists", <aspx> is a standard item
            // query page and query contains ID.
            //
            // The format we are not detecting here is
            // <scheme>://<host>/[...]<doclib>/[...]<file>
            //
            // Thus, even if we don't detect a match here, the URL might still
            // be a doc lib item.
            return (len >= 4 &&
                    !isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isDirSPDocLibBuiltinDir(segments[len - 2]) &&
                    (isFileSPStdListItemQueryPage(segments[len - 1]) &&
                     isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a non-doc-lib list item.
        private bool isUrlOtherListItem(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Lists/<list>/<aspx><query>
            // where <list> can be "Forms", <aspx> is a standard item query
            // page and <query> contains ID.
            return (len >= 4 &&
                    isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    (isFileSPStdListItemQueryPage(segments[len - 1]) &&
                     isQueryListItem(Url.Query)));
        }

        private UrlSPType getSPTypeFromUrl(Uri Url)
        {
            if (isUrlSite(Url))
                return UrlSPType.SITE;
            else if (isUrlWeb(Url))
                return UrlSPType.WEB;
            else if (isUrlDocLib(Url))
                return UrlSPType.DOC_LIB;
            else if (isUrlOtherList(Url))
                return UrlSPType.OTHER_LIST;
            else if (isUrlDocLibItemForSure(Url))
                return UrlSPType.DOC_LIB_ITEM;
            else if (isUrlOtherListItem(Url))
                return UrlSPType.OTHER_LIST_ITEM;
            else
                return UrlSPType.NOT_SURE;
        }

        public void RibbonCreateDS(HttpRequest req, ref SPEEvalAttr _SPEEvalAttr)
        {
            if (req.Url.AbsoluteUri.Contains("NewDocSet.aspx?"))
            {
                _SPEEvalAttr.Action = "EDIT";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                // url: " http://pf1-w12-sps01/_layouts/15/NewDocSet.aspx?List="
                int index = req.Url.AbsoluteUri.IndexOf("_layouts");
                string part1 = req.Url.AbsoluteUri.Substring(0, index - 1);
                string part2 = req.QueryString["RootFolder"];
                string nameDS = "";
                foreach (string key in req.Form.Keys)
                {
                    if (key.Contains("FileLeafRef"))
                    {
                        nameDS = req.Form[key];
                    }
                }
                _SPEEvalAttr.ObjEvalUrl = part1 + part2 + "/" + nameDS;
                _SPEEvalAttr.ItemObj = _SPEEvalAttr.WebObj.GetListItem(_SPEEvalAttr.ObjEvalUrl);

                if (_SPEEvalAttr.ItemObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                }
            }
        }

        public void LinkVieworEditDS(HttpRequest req, ref SPEEvalAttr _SPEEvalAttr)
        {
            try
            {
                _SPEEvalAttr.Action = "READ";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                if (req.Url.AbsoluteUri.Contains("EditForm.aspx?"))
                {
                    _SPEEvalAttr.Action = "WRITE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                }

                string part1 = Globals.UrlDecode(req.Url.AbsoluteUri);
                string part2 = req.QueryString["RootFolder"];
                int index = part2.LastIndexOf("/");
                string searchStr = "";
                if (index >= 0)
                {
                    searchStr = part2.Substring(0, index + 1);
                }
                int index2 = part1.IndexOf(searchStr);
                string hostStr = part1.Substring(0, index2);

                _SPEEvalAttr.ObjEvalUrl = hostStr + part2;
                _SPEEvalAttr.ItemObj = _SPEEvalAttr.WebObj.GetListItem(_SPEEvalAttr.ObjEvalUrl);

                if (_SPEEvalAttr.ItemObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                }
            }
            catch
            {
            }
        }

        public void CheckDocumentSet(HttpRequest req, ref SPEEvalAttr _SPEEvalAttr)
        {
            if (req.Url.AbsoluteUri.Contains("docsethomepage.aspx?") && req.Url.AbsoluteUri.Contains("RootFolder="))
            {
                LinkVieworEditDS(req, ref _SPEEvalAttr);
            }
            // fix bug 51558, document set open in mobile.
            else if (-1 != _SPEEvalAttr.RequestURL.IndexOf("/DocSetHome.aspx", StringComparison.OrdinalIgnoreCase))
            {
                string relativeUrl = req.QueryString["id"];
                if (!string.IsNullOrEmpty(relativeUrl))
                {
                    string fullUrl = _SPEEvalAttr.WebObj.Site.MakeFullUrl(relativeUrl);
                    SPListItem item = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, fullUrl, Utilities.SPUrlListItem);
                    if (item != null)
                    {
                        _SPEEvalAttr.Action = "READ";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                    }
                }
            }
        }

        public string MenuItemViewVersion(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            // url: "http://pf1-w12-sps01/rmslib/Forms/DispForm.aspx?ID=15&VersionNo=2048&RootFolder=/rmslib&IsDlg=1"
            // Referer: "http://pf1-w12-sps01/_layouts/15/Versions.aspx?FileName=/rmslib/111.txt&"
            ItemDispForm(req, evalAttr, ref item);
            string versionNo = req.QueryString["VersionNo"];
            return versionNo;
        }

        public string LinkViewVersion(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            // url: "http://pf1-w12-sps01/sites/test/sub1/_vti_history/2048/Shared Documents/111.txt"
            string versionNo = "";
            string key = "/_vti_history/";
            string url = evalAttr.RequestURL;
            int indBegin = url.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if(-1 != indBegin)
            {
                int indEnd = url.IndexOf("/", indBegin + key.Length);
                versionNo = url.Substring(indBegin + key.Length, indEnd - indBegin - key.Length);
                string itemUrl = url.Substring(0, indBegin) + url.Substring(indEnd);
                item = evalAttr.WebObj.GetListItem(itemUrl);
            }
            return versionNo;
        }

        public string MenuItemChangeVersion(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            // url: "http://pf1-w12-sps01/_layouts/15/versions.aspx?FileName=/rmslib/111.txt&list={E19B37FE-91C8-4AE4-95C7-04EF80D3F432}&ID=16&**&op=Restore&ver=2&IsDlg=1"
            ViewItemVersions(req, evalAttr, ref item);
            string versionNo = req.QueryString["ver"];
            string op = req.QueryString["op"];
            if (op.Equals("Delete", StringComparison.OrdinalIgnoreCase))
            {
                evalAttr.Action = "Delete File Version";
                evalAttr.PolicyAction = CETYPE.CEAction.Delete;
            }
            else if (op.Equals("Restore", StringComparison.OrdinalIgnoreCase))
            {
                evalAttr.Action = "Restore";
                evalAttr.PolicyAction = CETYPE.CEAction.Read;
            }
            else if (op.Equals("DeleteAll", StringComparison.OrdinalIgnoreCase) || op.Equals("DeleteAllMinor", StringComparison.OrdinalIgnoreCase))
            {
                evalAttr.Action = op;
                evalAttr.PolicyAction = CETYPE.CEAction.Write;
            }
            return versionNo;
        }

        public string RibbonChangeVersion(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            ItemDispForm(req, evalAttr, ref item);
            string versionNo = req.QueryString["VersionNo"];
            string eventTarget = req.Form["__EVENTTARGET"];
            if (!string.IsNullOrEmpty(eventTarget))
            {
                if (-1 != eventTarget.IndexOf("DeleteItemVersion", StringComparison.OrdinalIgnoreCase))
                {
                    evalAttr.Action = "Delete File Version";
                    evalAttr.PolicyAction = CETYPE.CEAction.Delete;
                }
                else if (-1 != eventTarget.IndexOf("RestoreItemVersion", StringComparison.OrdinalIgnoreCase))
                {
                    evalAttr.Action = "Restore";
                    evalAttr.PolicyAction = CETYPE.CEAction.Read;
                }
            }
            return versionNo;
        }

        public void ViewItemVersions(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            // url: "http://pf1-w12-sps01/_layouts/15/versions.aspx?**&list={E19B37FE-91C8-4AE4-95C7-04EF80D3F432}&ID=16&**"
            string listGuid = req.QueryString["list"];
            string id = req.QueryString["ID"];
            string fileName = req.QueryString["FileName"];
            if (!string.IsNullOrEmpty(listGuid) && !string.IsNullOrEmpty(id))
            {
                //listGuid = listGuid.Trim('{').Trim('}');
                SPList list = evalAttr.WebObj.Lists[new Guid(listGuid)];
                item = list.GetItemById(int.Parse(id));
            }
            else if (!string.IsNullOrEmpty(fileName))
            {
                item = evalAttr.WebObj.GetListItem(fileName);
            }
        }

        public void ItemDispForm(HttpRequest req, SPEEvalAttr evalAttr, ref SPListItem item)
        {
            // url: "http://pf1-w12-sps01/rmslib/Forms/DispForm.aspx?ID=15&**"
            string key = "/Forms/";
            string url = evalAttr.RequestURL;
            int ind = url.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (-1 != ind)
            {
                string listUrl = url.Substring(0, ind);
                SPList list = evalAttr.WebObj.GetList(listUrl);
                if (list != null)
                {
                    string itemId = req.QueryString["ID"];
                    item = list.GetItemById(int.Parse(itemId));
                }
            }
        }

        // SPE file version metadata enforcement.
        public bool CheckItemFileVersion(HttpRequest req, SPEEvalAttr evalAttr)
        {
            bool bVersion = false;
            try
            {
                string versionNo = null;
                string reqUrl = evalAttr.RequestURL;
                SPListItem item = null;

                if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    if (-1 != reqUrl.IndexOf("/versions.aspx?", StringComparison.OrdinalIgnoreCase))
                    {
                        versionNo = MenuItemChangeVersion(req, evalAttr, ref item);
                    }
                    else if (-1 != reqUrl.IndexOf("/DispForm.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        versionNo = RibbonChangeVersion(req, evalAttr, ref item);
                    }
                }
                else if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    if (-1 != reqUrl.IndexOf("/_vti_history/", StringComparison.OrdinalIgnoreCase))
                    {
                        versionNo = LinkViewVersion(req, evalAttr, ref item);
                    }
                    else if (-1 != reqUrl.IndexOf("/DispForm.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        versionNo = MenuItemViewVersion(req, evalAttr, ref item);
                    }
                    else if (-1 != reqUrl.IndexOf("/versions.aspx?", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewItemVersions(req, evalAttr, ref item);
                    }
                    if (item != null)
                    {
                        evalAttr.Action = "View";
                        evalAttr.PolicyAction = CETYPE.CEAction.Read;
                    }
                }

                if (item != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(item, evalAttr);
                    if (!string.IsNullOrEmpty(versionNo))
                    {
                        evalAttr.FileVersion = versionNo;
                        bVersion = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CheckItemFileVersion:", null, ex);
            }
            return bVersion;
        }

        public string[] BuildAttrArrayFromSPItemVersion(SPListItemVersion itemVersion)
        {
            SPFileVersion fileVersion = itemVersion.FileVersion;
            List<string> ls = new List<string>();
            // "type" and "sub_type"
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE);
            ls.Add(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM);
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE);
            ls.Add(CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM);

            // "created_by", "created", "modified_by", "modified", "file_size"
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY);
            object createBy = itemVersion["Created By"];
            ls.Add(createBy != null ? Globals.GetItemUserSidFromUserString(itemVersion.ListItem, createBy.ToString()) : "");

            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED);
            object created = itemVersion["Created"];
            ls.Add(created != null ? Globals.ConvertTime(created.ToString()) : "");

            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY);
            object modifiedBy = itemVersion["Modified By"];
            ls.Add(modifiedBy != null ? Globals.GetItemUserSidFromUserString(itemVersion.ListItem, modifiedBy.ToString()) : "");

            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED);
            object modified = itemVersion["Modified"];
            ls.Add(modified != null ? Globals.ConvertTime(modified.ToString()) : "");

            // Use File version "file size"
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE);
            object fileSize = fileVersion.Properties["vti_filesize"];
            ls.Add(fileSize != null ? fileSize.ToString() : "");

            ls.Add("Version");
            ls.Add(itemVersion.VersionLabel);

            foreach (SPField field in itemVersion.Fields)
            {
                string fieldName = field.Title;
                object fieldValue = itemVersion[fieldName];
                if (!string.IsNullOrEmpty(fieldName) && fieldValue != null && !string.IsNullOrEmpty(fieldValue.ToString()))
                {
                    //if prefilterResList !=null,means prefilter is enable
                    if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(fieldName))
                    {
                        continue;
                    }
                    if (fieldName.Equals("Created") || fieldName.Equals("Modified"))
                    {
                        continue; // Have converted the field value.
                    }
                    string strFieldValue = fieldValue.ToString();
                    Globals.ReConstructByFieldType(field, ref strFieldValue); // Re-construct value for some field types.
                    ls.Add(fieldName);
                    ls.Add(strFieldValue);
                }
            }
            return ls.ToArray();
        }

        public string[] BuildAttrArrayFromSPFile(SPFileVersion fileVer)
        {
            List<string> ls = new List<string>();
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_NAME);
            ls.Add(fileVer.File.Name);
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_TITLE);
            object title = fileVer.Properties["vti_title"];
            ls.Add(title != null ? title.ToString() : "");
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE);
            ls.Add(CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM);
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE);
            ls.Add(CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM);
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY);
            object author = fileVer.Properties["vti_author"];
            ls.Add(UserSid.GetUserSid(author != null ? author.ToString() : ""));
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY);
            object modifiedBy = fileVer.Properties["vti_modifiedby"];
            ls.Add(UserSid.GetUserSid(modifiedBy != null ? modifiedBy.ToString() : ""));
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED);
            object timeLastModified = fileVer.Properties["vti_timelastmodified"];
            ls.Add(Globals.ConvertTime(timeLastModified != null ? timeLastModified.ToString() : ""));
            ls.Add(CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE);
            object filesize = fileVer.Properties["vti_filesize"];
            ls.Add(filesize != null ? filesize.ToString() : "");
            ls.Add("Version");
            ls.Add(fileVer.VersionLabel);

            foreach (DictionaryEntry dic in fileVer.Properties)
            {
                if (!dic.Key.ToString().StartsWith("vti_"))
                {
                    ls.Add(dic.Key.ToString());
                    ls.Add(dic.Value.ToString());
                }
            }
            return ls.ToArray();
        }

        public string ConvertVersionNo(string ver)
        {
            int versionINT = int.Parse(ver);
            float percent = (float)0.1;
            if (versionINT < 512)
            {
                percent = (float)0.01;
            }
            float versionNo = versionINT / 512 + (versionINT % 512) * percent;
            return versionNo.ToString();
        }

        //process open action for list, library, list item here.
        public void ProcessRequestFromMobile(HttpRequest Request)
        {
            string strListGuid = Request.QueryString["List"];
            string strListItemID = Request.QueryString["ID"];
            string strDS = Request.QueryString["RootFolder"];
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
#if SP2016 || SP2019
			// SP2016 mobile access library, folder and document set.
            string mode = Request.QueryString["Mode"];
            string page = Request.QueryString["Page"];

            if (!string.IsNullOrEmpty(mode) && mode.Equals("doclibs", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(page))
            {
                strListGuid = page;
                strDS = Request.QueryString["Path"];
            }
#endif
            if (!string.IsNullOrEmpty(strListGuid))
            {
                Guid GuidList = new Guid(strListGuid);
                _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.Lists.GetList(GuidList, true);
                if (_SPEEvalAttr.ListObj != null)
                {

                    _SPEEvalAttr.Action = "Open";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;

                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                    if (!string.IsNullOrEmpty(strListItemID)) //list item
                    {
                        SPListItem ListItemCur = _SPEEvalAttr.ListObj.GetItemById(Convert.ToInt32(strListItemID));
                        if(ListItemCur != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(ListItemCur, _SPEEvalAttr);
                        }
                    }
                    else if (!string.IsNullOrEmpty(strDS))//document set case
                    {
#if SP2016 || SP2019
                        strDS = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj) + "/" + strDS;
#endif
                        _SPEEvalAttr.ItemObj = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, strDS, Utilities.SPUrlListItem);
                        if (_SPEEvalAttr.ItemObj != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        }
                    }
                    else //list or library
                    {
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                    }
                }
            }
        }
    }
}

