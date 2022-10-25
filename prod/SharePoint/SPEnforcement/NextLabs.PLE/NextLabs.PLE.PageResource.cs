using System;
using System.Collections.Generic;
using System.Text;
using NextLabs.Common;
using NextLabs.CSCInvoke;
using System.Web;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.PLE.Log;
using System.Security.Principal;
using NextLabs.Diagnostic;

namespace Nextlabs.PLE.PageModule
{
    public interface IPageResource
    {
        CETYPE.CEResponse_t Process(HttpRequest Request, SPWeb _webObj);
        String GetSourceUrl();
        String GetPolicyName();
        String GetPolicyMessage();
    }




    public class SitePageResource : IPageResource
    {
        private String m_obj_referrerurl = "";
        private String m_policyName = "";
        private String m_policyMessage = "";
        public static bool IsSitePageResource(HttpRequest Request)
        {
            try
            {
                if (Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during SitePageResource IsSitePageResource :", null, ex);
            }
            return false;
        }

        public String GetSourceUrl()
        {
            return m_obj_referrerurl;
        }
        public String GetPolicyName()
        {
            return m_policyName;
        }
        public String GetPolicyMessage()
        {
            return m_policyMessage;
        }

        public CETYPE.CEResponse_t Process(HttpRequest Request, SPWeb _webObj)
        {

            try
            {
                String _obj_name = "";
                String _obj_id = "";
                String _obj_type = "";
                String _obj_description = "";
                String _obj_subtype = "";
                String _before_url = null;
                String _after_url = null;
                String _obj_targeturl = null;
                CETYPE.CEAction _policy_action = CETYPE.CEAction.Read;
                CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                URLAnalyser _URLAnalyser = new URLAnalyser();
                if (_webObj != null)
                {
                    String[] _emptyArray = new String[0];
                    String[] _propertyArray = new String[6 * 2];
                    m_obj_referrerurl = CommonVar.GetSPWebContent(_webObj, "url");
                    _obj_name = CommonVar.GetSPWebContent(_webObj, "title");
                    _obj_id = CommonVar.GetSPWebContent(_webObj, "id");
                    _obj_type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                    _obj_description = CommonVar.GetSPWebContent(_webObj, "description");
                    _obj_subtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                    m_obj_referrerurl = _URLAnalyser.ConvertAspxPath(m_obj_referrerurl, Request, "Site");
                    if (m_obj_referrerurl != null
                        && !m_obj_referrerurl.EndsWith("RedirectPage.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                        _propertyArray[0 * 2 + 1] = _obj_name;
                        _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                        _propertyArray[1 * 2 + 1] = _obj_name;
                        _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                        _propertyArray[2 * 2 + 1] = _obj_description;
                        _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_TYPE;
                        _propertyArray[3 * 2 + 1] = _obj_type;
                        _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_SUBTYPE;
                        _propertyArray[4 * 2 + 1] = _obj_subtype;
                        //Add a page type attribute
                        if (Request.FilePath != null && Request.FilePath.StartsWith("/_layouts", StringComparison.OrdinalIgnoreCase))
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_APPLICATION;
                        }
                        else
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_NORMAL;
                        }

                        if (Request.HttpMethod == "POST")
                        {
                            _policy_action = CETYPE.CEAction.Write;
                        }
                        else
                        {
                            _policy_action = CETYPE.CEAction.Read;
                        }
                        IntPtr localConnectHandle = IntPtr.Zero;
                        String[] enforcement_obligation = { "" };
                        _response = Globals.Custom_CallEval(_policy_action,
                                                    m_obj_referrerurl,
                                                    _obj_targeturl,
                                                    ref _propertyArray,
                                                    ref _emptyArray,
                                                    Request.UserHostAddress,
                                                    CommonVar.GetSPWebContent(_webObj, "loginname"),
                                                    _webObj.CurrentUser.Sid,
                                                    ref m_policyName,
                                                    ref m_policyMessage,
                                                    _before_url,
                                                    _after_url,
                                                    Globals.HttpModuleName,
                                                    _NoiseLevel,
                                                    _webObj,
                                                    true,
                                                    ref localConnectHandle,
                                                    ref enforcement_obligation,
                                                    HttpContext.Current.User);
                        if (Request.HttpMethod == "POST" && _response != CETYPE.CEResponse_t.CEDeny)
                        {
                            PLE_ReportAdminObligationLog _PLE_ReportAdminObligationLog = new PLE_ReportAdminObligationLog();
                            string sitevalue = null;
                            if (_webObj != null)
                            {
                                sitevalue = CommonVar.GetSPWebContent(_webObj, "url");
                            }
                            //Fix bug 8354, added by William 20090203
                            if (sitevalue != null)
                            {
                                sitevalue = Globals.UrlToResSig(sitevalue).ToLower();
                            }
                            AdminPageLogs _AdminPageLogs = new AdminPageLogs();
                            String _SiteVersion = _webObj.AllProperties["vti_extenderversion"].ToString();
                            String[] _adminlogs = _AdminPageLogs.ProcessAdminLogs(Request, _obj_name, "site", _SiteVersion);
                            if (_adminlogs != null)
                            {
                                _PLE_ReportAdminObligationLog.DoReportLog(localConnectHandle, enforcement_obligation, sitevalue, _adminlogs);
                            }
                        }
                    }
                    return _response;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during SitePageResource Process :", null, ex);
            }
            return CETYPE.CEResponse_t.CEAllow;
        }

    }

    public class PortletPageResource : IPageResource
    {
        private String m_obj_referrerurl = "";
        private String m_policyName = "";
        private String m_policyMessage = "";

        public static bool IsPortletPageResource(HttpRequest Request)
        {
            String _paramListId = null;
            String _objStr = null;
            String _FileStr = null;
            String _IDStr = null;
            String _PageStr = null;
            String _CancelSourceStr = null;
            try
            {
                _paramListId = Request.QueryString["List"];
                _objStr = Request.QueryString["obj"];
                _FileStr = Request.QueryString["FileName"];
                _IDStr = Request.QueryString["ID"];
                _PageStr = Request.QueryString["Page"];
                _CancelSourceStr = Request.QueryString["CancelSource"];
                if (_PageStr != null && _PageStr.Equals("1"))
                {
                    //This means a page item is being processed, in order to fix bug 580,added by Wiliam 20090713
                    return false;
                }

                if (!string.IsNullOrEmpty(_paramListId)
                    && string.IsNullOrEmpty(_FileStr) && string.IsNullOrEmpty(_IDStr))
                //ID string can exist, the format shall be list=xx,id=yy, source=zz,see wiki page lib versiondiff.aspx
                {
                    if (!string.IsNullOrEmpty(_objStr))
                    {
                        //fix bug 9782 ,add "doclib" by William 20090824
                        if ((_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("list", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("doclib", StringComparison.OrdinalIgnoreCase)) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                        {
                            //There could be some url like obj=listid, listitemid,folder,fix bug 10586
                            if (_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                            {
                                String[] _key = _objStr.Split(new String[] { "," }, StringSplitOptions.None);
                                if (!string.IsNullOrEmpty(_key[1]) && !_key[1].Equals("folder", StringComparison.OrdinalIgnoreCase))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }

                    }
                    else if (string.IsNullOrEmpty(_objStr))
                    {
                        if (Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                else if (!string.IsNullOrEmpty(_objStr))
                {
                    if ((_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("list", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("doclib", StringComparison.OrdinalIgnoreCase)) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                        {
                            String[] _key = _objStr.Split(new String[] { "," }, StringSplitOptions.None);
                            if (!string.IsNullOrEmpty(_key[1]) && !_key[1].Equals("folder", StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                //Fix bug 581
                else if (string.IsNullOrEmpty(_FileStr) && string.IsNullOrEmpty(_IDStr))
                {
                    try
                    {
                        int index = Request.FilePath.LastIndexOf("/");
                        String list_path = Request.FilePath.Substring(0, index);
                        SPList _list = null;
                        _list = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                        if (_list != null && !Request.FilePath.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase)
                            && !Request.FilePath.EndsWith("Home.aspx", StringComparison.OrdinalIgnoreCase)
                            && !Request.FilePath.EndsWith("category.aspx", StringComparison.OrdinalIgnoreCase)
                            //Wiki Site home page
                            && !Request.FilePath.EndsWith("/Wiki Pages/Home.aspx", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource IsPortletPageResource :", null, ex);
                    }
                }
                //Fix bug 600, url format:_layouts/xx.aspx?canclesource=yyy
                if (!string.IsNullOrEmpty(_CancelSourceStr))
                {
                    try
                    {
                        int index = _CancelSourceStr.LastIndexOf("/");
                        String list_path = _CancelSourceStr.Substring(0, index);
                        SPList _list = null;
                        _list = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                        if (_list != null)
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource IsPortletPageResource :", null, ex);
                    }

                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource IsPortletPageResource :", null, ex);
            }
            return false;
        }

        public String GetSourceUrl()
        {
            return m_obj_referrerurl;
        }
        public String GetPolicyName()
        {
            return m_policyName;
        }
        public String GetPolicyMessage()
        {
            return m_policyMessage;
        }

        public CETYPE.CEResponse_t Process(HttpRequest Request, SPWeb _webObj)
        {

            try
            {
                String _obj_name = "";
                String _obj_id = "";
                String _obj_type = "";
                String _obj_description = "";
                String _obj_subtype = "";
                String _before_url = null;
                String _after_url = null;
                String _obj_targeturl = null;
                String _paramListId = Request.QueryString["List"];
                String _CancelSourceStr = Request.QueryString["CancelSource"];
                String _objStr = Request.QueryString["obj"];
                HttpContext _context = HttpContext.Current;
                SPList _listObj = null;
                CETYPE.CEAction _policy_action = CETYPE.CEAction.Read;
                CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                URLAnalyser _URLAnalyser = new URLAnalyser();
                URLAnalyser.UrlSPType _spType = _URLAnalyser.getSPTypeFromUrl(Request.Url);
                if (!string.IsNullOrEmpty(_paramListId))
                {
                    _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, _paramListId, Utilities.SPUrlListID);
                    m_obj_referrerurl = Globals.ConstructListUrl
                        (_webObj, _listObj);
                }
                else if (!string.IsNullOrEmpty(_objStr))
                {
                    //fix bug 10586, add folder detection, url format can be obj = listid,folder
                    if ((_objStr.EndsWith("FOLDER", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("list", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("doclib", StringComparison.OrdinalIgnoreCase)) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        String _listStr = "";
                        int _index = _objStr.LastIndexOf("}");
                        _listStr = _objStr.Substring(1, _index - 1);
                        _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, _listStr, Utilities.SPUrlListID);
                        m_obj_referrerurl = Globals.ConstructListUrl
                            (_webObj, _listObj);
                    }
                }
                else
                {
                    //Fix bug 600, url format:_layouts/xx.aspx?canclesource=yyy
                    if (!string.IsNullOrEmpty(_CancelSourceStr))
                    {
                        try
                        {
                            int index = _CancelSourceStr.LastIndexOf("/");
                            String list_path = _CancelSourceStr.Substring(0, index);
                            _listObj = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                            //If it's format is list and can get a list, then it is a list indeed
                            if (_listObj != null)
                            {
                                m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                            }
                        }
                        catch (Exception ex)
                        {
                            //If some exception happens, it means we can not get the SPList Object
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource Process :", null, ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            int index = Request.FilePath.LastIndexOf("/");
                            String list_path = Request.FilePath.Substring(0, index);
                            _listObj = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                            //If it's format is list and can get a list, then it is a list indeed
                            if (_listObj != null)
                            {
                                m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                            }
                        }
                        catch (Exception ex)
                        {
                            //If some exception happens, it means we can not get the SPList Object
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource Process :", null, ex);
                        }
                    }
                    if (_listObj == null)
                    {
                        string[] segments = Request.Url.Segments;
                        char[] slashChArr = { '/' };
                        string docLibName = "";
                        if (_spType == URLAnalyser.UrlSPType.DOC_LIB)
                        {
                            docLibName = Globals.UrlDecode
                                (segments[segments.Length - 3].
                                 TrimEnd(slashChArr));
                            m_obj_referrerurl = Globals.UrlDecode
                                (Globals.TrimEndUrlSegments
                                 (Request.Url.GetLeftPart(UriPartial.Path), 2));
                        }
                        else
                        {
                            docLibName = Globals.UrlDecode
                                (segments[segments.Length - 2].
                                 TrimEnd(slashChArr));

                            m_obj_referrerurl = Globals.UrlDecode
                                (Globals.TrimEndUrlSegments
                                 (Request.Url.GetLeftPart(UriPartial.Path), 1));
                        }
                        string cata_str = "_catalogs";
                        int cata_index = m_obj_referrerurl.IndexOf(cata_str, StringComparison.OrdinalIgnoreCase);
                        if (cata_index != -1)
                        {
                            m_obj_referrerurl = m_obj_referrerurl.Remove(cata_index, cata_str.Length + 1);//Delete the _catalogs/
                        }
                        foreach (SPList f in _webObj.Lists)
                        {
                            if (f != null)
                            {
                                if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    _listObj = f;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (_listObj != null)
                {
                    bool _MultipleUpload = false;
                    String[] _emptyArray = new String[0];
                    String[] _propertyArray;
                    _obj_name = CommonVar.GetSPListContent(_listObj, "title");
                    _obj_id = CommonVar.GetSPListContent(_listObj, "id");
                    _obj_type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                    _obj_description = CommonVar.GetSPListContent(_listObj, "description");
                    _obj_subtype = _URLAnalyser.ConvertSPBaseType2PolicySubtype(_listObj.BaseType.ToString(), "Portlet");
                    m_obj_referrerurl = _URLAnalyser.ConvertAspxPath(m_obj_referrerurl, Request, "Portlet");
                    //get MultipleUpload query string
                    if (Request.FilePath.EndsWith("upload.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        string queryValue = null;
                        for (int i = 0; i < Request.QueryString.AllKeys.Length; i++)
                        {
                            // George: check "Request.QueryString.AllKeys[i]" is not empty.
                            queryValue = Request.QueryString.AllKeys[i];
                            if (!string.IsNullOrEmpty(queryValue) && queryValue.Equals("MultipleUpload", StringComparison.OrdinalIgnoreCase))
                            {
                                _MultipleUpload = true;
                                break;
                            }
                        }
                    }
                    if (_MultipleUpload)
                    {
                        _propertyArray = new String[7 * 2];
                        _propertyArray[6 * 2 + 0] = "MultipleUpload";
                        _propertyArray[6 * 2 + 1] = "yes";
                    }
                    else
                        _propertyArray = new String[6 * 2];
                    if (m_obj_referrerurl != null)
                    {
                        _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                        _propertyArray[0 * 2 + 1] = _obj_name;
                        _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                        _propertyArray[1 * 2 + 1] = _obj_name;
                        _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                        _propertyArray[2 * 2 + 1] = _obj_description;
                        _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_TYPE;
                        _propertyArray[3 * 2 + 1] = _obj_type;
                        _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_SUBTYPE;
                        _propertyArray[4 * 2 + 1] = _obj_subtype;
                        //Add a page type attribute
                        if (Request.FilePath != null && Request.FilePath.StartsWith("/_layouts", StringComparison.OrdinalIgnoreCase))
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_APPLICATION;
                        }
                        else
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_NORMAL;
                        }
                        if (Request.HttpMethod == "POST")
                        {
                            _policy_action = CETYPE.CEAction.Write;
                        }
                        else
                        {
                            _policy_action = CETYPE.CEAction.Read;
                        }
                        IntPtr localConnectHandle = IntPtr.Zero;
                        String[] enforcement_obligation = { "" };
                        _response = Globals.Custom_CallEval(_policy_action,
                                                    m_obj_referrerurl,
                                                    _obj_targeturl,
                                                    ref _propertyArray,
                                                    ref _emptyArray,
                                                    Request.UserHostAddress,
                                                    CommonVar.GetSPWebContent(_webObj, "loginname"),
                                                    _webObj.CurrentUser.Sid,
                                                    ref m_policyName,
                                                    ref m_policyMessage,
                                                    _before_url,
                                                    _after_url,
                                                    Globals.HttpModuleName,
                                                    _NoiseLevel,
                                                    _webObj,
                                                    true,
                                                    ref localConnectHandle,
                                                    ref enforcement_obligation,
                                                    HttpContext.Current.User);
                        if (Request.HttpMethod == "POST" && _response != CETYPE.CEResponse_t.CEDeny)
                        {
                            PLE_ReportAdminObligationLog _PLE_ReportAdminObligationLog = new PLE_ReportAdminObligationLog();
                            string sitevalue = null;
                            if (_webObj != null)
                            {
                                sitevalue = CommonVar.GetSPWebContent(_webObj, "url");
                            }
                            //Fix bug 8354, added by William 20090203
                            if (sitevalue != null)
                            {
                                sitevalue = Globals.UrlToResSig(sitevalue).ToLower();
                            }
                            AdminPageLogs _AdminPageLogs = new AdminPageLogs();
                            String _SiteVersion = _webObj.AllProperties["vti_extenderversion"].ToString();
                            String[] _adminlogs = _AdminPageLogs.ProcessAdminLogs(Request, _obj_name, "portlet", _SiteVersion);
                            if (_adminlogs != null)
                            {
                                _PLE_ReportAdminObligationLog.DoReportLog(localConnectHandle, enforcement_obligation, sitevalue, _adminlogs);
                            }
                        }
                    }
                    return _response;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource Process :", null, ex);
            }
            return CETYPE.CEResponse_t.CEAllow;
        }

    }

    public class PorletItemPageResource : IPageResource
    {
        private String m_obj_referrerurl = "";
        private String m_policyName = "";
        private String m_policyMessage = "";

        public static bool IsPorletItemPageResource(HttpRequest Request)
        {
            String _paramListId = null;
            String _objStr = null;
            String _FileStr = null;
            String _ListIDStr = null;
            String _IDStr = null;
            String _PageStr = null;
            String _SourceStr = null;
            int itemId = 0;
            try
            {
                _paramListId = Request.QueryString["List"];
                _objStr = Request.QueryString["obj"];
                _FileStr = Request.QueryString["FileName"];
                _IDStr = Request.QueryString["ID"];
                _PageStr = Request.QueryString["Page"];
                _SourceStr = Request.QueryString["Source"];
                _ListIDStr = Request.QueryString["ListId"];
                if (_PageStr != null && _PageStr.Equals("1"))
                {
                    //This means a page item is being processed, in order to fix bug 580,added by Wiliam 20090713
                    return true;
                }
                if (!string.IsNullOrEmpty(_paramListId)
                    && (!string.IsNullOrEmpty(_objStr) || !string.IsNullOrEmpty(_FileStr)
                        || !string.IsNullOrEmpty(_IDStr)))
                {
                    if (!string.IsNullOrEmpty(_objStr))
                    {
                        if (_objStr.EndsWith("listitem", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                            return true;
                        //Fix bug 809, add "document" keyword detection, Added by William 20091130
                        else if (_objStr.EndsWith("DOCUMENT", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                            return true;
                        //fix bug 10586
                        else if (_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                        {
                            String[] _key = _objStr.Split(new String[] { "," }, StringSplitOptions.None);
                            if (!string.IsNullOrEmpty(_key[1]) && !_key[1].Equals("folder", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (!string.IsNullOrEmpty(_objStr))
                {
                    if (_objStr.EndsWith("listitem", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                        return true;
                    //Fix bug 809, add "document" keyword detection, Added by William 20091130
                    else if (_objStr.EndsWith("DOCUMENT", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                        return true;
                    //fix bug 10586
                    else if (_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) && Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        String[] _key = _objStr.Split(new String[] { "," }, StringSplitOptions.None);
                        if (!string.IsNullOrEmpty(_key[1]) && !_key[1].Equals("folder", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                //format:site/xx.aspx?ListId="b2ECE541C-32FB-4B84-A1AF-68539D69E21F"&id=1&rootfolder=yy
                else if (!string.IsNullOrEmpty(_ListIDStr) && !string.IsNullOrEmpty(_IDStr) && Int32.TryParse(_IDStr, out itemId))
                {
                    SPWeb _currentWeb = SPControl.GetContextWeb(HttpContext.Current);
                    SPList _list = _currentWeb.Lists[new Guid(_ListIDStr)];
                    if (_list != null)
                    {
                        SPListItem _item = _list.GetItemById(itemId);
                        if (_item != null)
                            return true;
                    }
                }

                //Fix bug 589,format:site/xx.aspx?id=1&rootfolder=yy
                else if (!string.IsNullOrEmpty(_SourceStr) && !string.IsNullOrEmpty(_IDStr) != null && Int32.TryParse(_IDStr, out itemId))
                {
                    int index = _SourceStr.LastIndexOf("/");
                    String list_path = _SourceStr.Substring(0, index);
                    SPList _list = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                    if (_list != null)
                    {
                        SPListItem _item = _list.GetItemById(itemId);
                        if (_item != null)
                            return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public String GetSourceUrl()
        {
            return m_obj_referrerurl;
        }
        public String GetPolicyName()
        {
            return m_policyName;
        }
        public String GetPolicyMessage()
        {
            return m_policyMessage;
        }

        private String GetListItemName(SPList _listObj, SPListItem _itemObj)
        {
            if (_listObj != null && _itemObj != null)
            {
                //Fix bug 605, survey's item name is null
                if (_listObj.BaseType == SPBaseType.Survey)
                {
                    return CommonVar.GetSPListItemContent(_itemObj, "displayname");
                }
                else
                {
                    return CommonVar.GetSPListItemContent(_itemObj, "name");
                }
            }
            return "";
        }

        public CETYPE.CEResponse_t Process(HttpRequest Request, SPWeb _webObj)
        {

            try
            {
                String _obj_name = "";
                String _obj_title = "";
                String _obj_id = "";
                String _obj_type = "";
                String _obj_description = "";
                String _obj_subtype = "";
                String _before_url = null;
                String _after_url = null;
                String _obj_targeturl = null;
                String weburl = null;
                String itemurl = null;
                HttpContext _context = HttpContext.Current;
                String _paramListId = Request.QueryString["List"];
                String _objStr = Request.QueryString["obj"];
                String _FileNameStr = Request.QueryString["FileName"];
                String _IDStr = Request.QueryString["ID"];
                String _PageStr = Request.QueryString["Page"];
                String _Source = Request.QueryString["Source"];
                CETYPE.CEAction _policy_action = CETYPE.CEAction.Read;
                CETYPE.CENoiseLevel_t _NoiseLevel = CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION;
                SPList _listObj = null;
                SPListItem _itemObj = null;
                CETYPE.CEResponse_t _response = CETYPE.CEResponse_t.CEAllow;
                URLAnalyser _URLAnalyser = new URLAnalyser();
                URLAnalyser.UrlSPType _spType = URLAnalyser.UrlSPType.NOT_SURE;
                String _NewUrl = Globals.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
                Uri _RebuildUrl = new Uri(_NewUrl);
                SPList Listobj = null;
                char[] slashChArr = { '/' };
                try
                {
                    string[] segmentsDiscussions = _RebuildUrl.Segments;

                    string listName = Globals.UrlDecode
                                (segmentsDiscussions[segmentsDiscussions.Length - 2].
                                    TrimEnd(slashChArr));
                    if (_webObj != null)
                        Listobj = _webObj.Lists[listName];
                }
                catch
                {
                }
                if (Listobj != null && Listobj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                {
                    _spType = Nextlabs.PLE.PageModule.URLAnalyser.UrlSPType.OTHER_LIST_ITEM;
                }
                else
                {
                    _spType = _URLAnalyser.getSPTypeFromUrl(Request.Url);
                }

                //Wiki page lib, format:page=xxx;sourcr=yyy;id=1
                if (_PageStr != null && _PageStr.Equals("1"))
                {
                    String _SourceStr = Request.QueryString["Source"];
                    if (_SourceStr != null)
                        _itemObj = (SPListItem)Utilities.GetCachedSPContent(_webObj, _SourceStr, Utilities.SPUrlListItem);
                    if (_itemObj != null)
                        _listObj = _itemObj.ParentList;
                    if (_itemObj != null && _listObj != null)
                    {
                        //If list type is doclib, url contains both item and doclib name;otherwise, the item name is not correct
                        if (_listObj.BaseType == SPBaseType.DocumentLibrary)
                        {
                            weburl = CommonVar.GetSPWebContent(_webObj, "url");
                            itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                            m_obj_referrerurl = weburl + '/' + itemurl;
                        }
                        else
                        {
                            m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                            m_obj_referrerurl = m_obj_referrerurl + '/' + GetListItemName(_listObj, _itemObj);
                        }

                    }

                }
                //format: List=xxx
                else if (!string.IsNullOrEmpty(_paramListId)
                    && (!string.IsNullOrEmpty(_objStr) || !string.IsNullOrEmpty(_FileNameStr) || !string.IsNullOrEmpty(_IDStr)))
                {
                    _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, _paramListId, Utilities.SPUrlListID);
                    //format:List=xxx;obj=yyy
                    if (!string.IsNullOrEmpty(_objStr))
                    {
                        int index1 = _objStr.IndexOf(",");
                        int index2 = _objStr.LastIndexOf(",");
                        String obj_id_str = _objStr.Substring(index1 + 1, index2 - index1 - 1);
                        int obj_id_int = Convert.ToInt32(obj_id_str);
                        _itemObj = _listObj.GetItemById(obj_id_int);
                    }
                    //format:List=xxx;FileName=yyy
                    else if (!string.IsNullOrEmpty(_FileNameStr))
                    {
                        try
                        {
                            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                            if (_SPEEvalAttr.WebObj != null)
                            {
                                _itemObj = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _FileNameStr, Utilities.SPUrlListItem);
		                    }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource Process ,Get item object by direct url:", null, ex);
                        }
                        if (_itemObj == null)
                        {
                            try
                            {
                                //use spquery for searching an item
                                SPQuery query = new SPQuery();
                                SPListItemCollection spListItems;
                                query.RowLimit = 2000; // Only select the top 2000.
                                query.ViewAttributes = "Scope=\"Recursive\"";
                                string format = "<Where><Eq><FieldRef Name=\"FileLeafRef\" /><Value Type=\"File\">{0}</Value></Eq></Where>";
                                query.Query = String.Format(format, _FileNameStr);
                                spListItems = _listObj.GetItems(query);
                                if (spListItems.Count > 0 && _listObj.ItemCount > 0)
                                    _itemObj = spListItems[0];
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Warn, "Exception during PortletPageResource Process ,Get item object by spquery:", null, ex);
                            }
                        }

                        //use loop as backup way
                        if (_itemObj == null)
                        {
                            foreach (SPListItem f in _listObj.Items)
                            {
                                if (_FileNameStr.EndsWith(f.Url))
                                {
                                    _itemObj = f;
                                    break;
                                }
                            }
                        }
                    }
                    //format:List=xxx;id=yyy
                    else if (!string.IsNullOrEmpty(_IDStr))
                    {
                        int obj_id_int = Convert.ToInt32(_IDStr);
                        _itemObj = _listObj.GetItemById(obj_id_int);
                    }
                    if (_itemObj != null && _listObj != null)
                    {
                        //If list type is doclib, url contains both item and doclib name;otherwise, the item name is not correct
                        if (_listObj.BaseType == SPBaseType.DocumentLibrary)
                        {
                            weburl = CommonVar.GetSPWebContent(_webObj, "url");
                            itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                            m_obj_referrerurl = weburl + '/' + itemurl;
                        }
                        else
                        {
                            m_obj_referrerurl = Globals.ConstructListUrl
                                (_webObj, _listObj);
                            m_obj_referrerurl = m_obj_referrerurl + '/' + GetListItemName(_listObj, _itemObj);
                        }
                    }
                }
                //format:source=xxx;id=yyy
                else if (!string.IsNullOrEmpty(_Source) && !string.IsNullOrEmpty(_IDStr))
                {
                    int index = _Source.LastIndexOf("/");
                    String list_path = _Source.Substring(0, index);
                    _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, list_path, Utilities.SPUrlList);
                    if (_listObj != null)
                    {
                        _itemObj = _listObj.GetItemById(Convert.ToInt32(_IDStr));
                    }
                    if (_itemObj != null && _listObj != null)
                    {
                        //If list type is doclib, url contains both item and doclib name;otherwise, the item name is not correct
                        if (_listObj.BaseType == SPBaseType.DocumentLibrary)
                        {
                            weburl = CommonVar.GetSPWebContent(_webObj, "url");
                            itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                            m_obj_referrerurl = weburl + '/' + itemurl;
                        }
                        else
                        {
                            m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                            m_obj_referrerurl = m_obj_referrerurl + '/' + GetListItemName(_listObj, _itemObj);
                        }
                    }
                }
                //format: obj={xxxx},id,doucment/listitem/folder
                else if (!string.IsNullOrEmpty(_objStr))
                {
                    if (_objStr.EndsWith("folder", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("listitem", StringComparison.OrdinalIgnoreCase) || _objStr.EndsWith("DOCUMENT", StringComparison.OrdinalIgnoreCase))
                    {
                        String[] _ItemArray = _objStr.Split(new String[] { "," }, StringSplitOptions.None);
                        String _list_id = _ItemArray[0].Substring(1, _ItemArray[0].Length - 2);
                        _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, _list_id, Utilities.SPUrlListID);
                        int obj_id_int = Convert.ToInt32(_ItemArray[1]);
                        _itemObj = _listObj.GetItemById(obj_id_int);
                        //If list type is doclib, url contains both item and doclib name;otherwise, the item name is not correct
                        if (_listObj.BaseType == SPBaseType.DocumentLibrary)
                        {
                            weburl = CommonVar.GetSPWebContent(_webObj, "url");
                            itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                            m_obj_referrerurl = weburl + '/' + itemurl;
                        }
                        else
                        {
                            m_obj_referrerurl = Globals.ConstructListUrl
                                (_webObj, _listObj);
                            m_obj_referrerurl = m_obj_referrerurl + '/' + GetListItemName(_listObj, _itemObj);
                        }
                    }
                }
                //If all parse above failed, try orignal url format, to fix bug
                if (_listObj == null && _itemObj == null)
                {
                    string[] segments = Request.Url.Segments;
                    if (_spType == URLAnalyser.UrlSPType.DOC_LIB_ITEM)
                    {
                        string docLibName = Globals.UrlDecode
                            (segments[segments.Length - 3].
                             TrimEnd(slashChArr));
                        int itemId = int.Parse(Request.QueryString["ID"]);
                        foreach (SPList f in _webObj.Lists)
                        {
                            if (f != null)
                            {
                                //To verify the name
                                if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    _listObj = f;
                                    break;
                                }
                            }
                        }
                        _itemObj = _listObj.GetItemById(itemId);
                        if (_itemObj.Folder == null)
                        {
                            weburl = CommonVar.GetSPWebContent(_webObj, "url");
                            itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                            m_obj_referrerurl = weburl + '/' +
                                 itemurl;
                        }
                        else
                        {
                            m_obj_referrerurl = Globals.ConstructListUrl
                                (_webObj, _listObj);
                        }
                    }
                    else if (_spType == URLAnalyser.UrlSPType.OTHER_LIST_ITEM)
                    {
                        if (Listobj != null && Listobj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                        {

                            String _RootFolder = Request.QueryString["RootFolder"];
                            string[] splitBuf = _RootFolder.Split(new Char[] { '/' });
                            int splitCount = 0;
                            foreach (string t in splitBuf)
                            {
                                splitCount++;
                            }
                            string listname = splitBuf[splitCount - 2];
                            string listitemname = splitBuf[splitCount - 1];
                            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                            try
                            {
                                _listObj = _SPEEvalAttr.WebObj.GetList(_SPEEvalAttr.RequestURL_path);
                            }
                            catch
                            {
                            }
                            if (_listObj == null)
                            {
                                foreach (SPList list in _SPEEvalAttr.WebObj.Lists)
                                {
                                    if (list.Title == listname)
                                    {
                                        _listObj = list;
                                        break;
                                    }
                                }
                            }

                            foreach (SPListItem item in _listObj.Folders)
                            {
                                if (_listObj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                                {
                                    try
                                    {
                                        if (item["BaseName"].ToString() == listitemname)
                                        {
                                            _itemObj = item;
                                            break;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                else
                                {
                                    if (item.DisplayName == listitemname)
                                    {
                                        _itemObj = item;
                                        break;
                                    }
                                }
                            }
                            if (_listObj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                            {
                                if (_itemObj != null)
                                {
                                    weburl = CommonVar.GetSPWebContent(_webObj, "url");
                                    itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                                    m_obj_referrerurl = weburl + '/' +
                                         itemurl;
                                }
                                else
                                    m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                            }
                            else
                            {
                                if (_itemObj.Folder == null)
                                {
                                    weburl = CommonVar.GetSPWebContent(_webObj, "url");
                                    itemurl = CommonVar.GetSPListItemContent(_itemObj, "url");
                                    m_obj_referrerurl = weburl + '/' +
                                         itemurl;
                                }
                                else
                                {
                                    m_obj_referrerurl = Globals.ConstructListUrl
                                        (_webObj, _listObj);
                                }
                            }
                        }
                        else
                        {

                            string docLibName = Globals.UrlDecode
                                (segments[segments.Length - 2].
                                 TrimEnd(slashChArr));
                            int itemId = int.Parse(Request.QueryString["ID"]);
                            foreach (SPList f in _webObj.Lists)
                            {
                                if (f != null)
                                {
                                    //To verify the name
                                    if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        _listObj = f;
                                        break;
                                    }
                                }
                            }
                            _itemObj = _listObj.GetItemById(itemId);
                            //Fix bug 605, survey's item name is null
                            m_obj_referrerurl =

                               Globals.UrlDecode
                                (Globals.TrimEndUrlSegments
                                 (Request.Url.GetLeftPart(UriPartial.Path), 1))
                                + '/' + GetListItemName(_listObj, _itemObj);
                        }
                    }
                }
                if (_itemObj != null)
                {
                    String[] _emptyArray = new String[0];
                    String[] _propertyArray = new String[6 * 2];
                    if (_listObj.BaseType == SPBaseType.DocumentLibrary)
                    {
                        _obj_name = CommonVar.GetSPListItemContent(_itemObj, "displayname");
                    }
                    else
                    {
                        _obj_name = CommonVar.GetSPListItemContent(_itemObj, "name");
                    }
                    _obj_id = CommonVar.GetSPListItemContent(_itemObj, "id");
                    _obj_type = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                    //Fix bug 596, fetch _itemObj.Title can cause exception
                    try
                    {
                        _obj_description = CommonVar.GetSPListItemContent(_itemObj, "title");
                    }
                    catch
                    {
                        _obj_description = "";
                    }
                    _obj_subtype = _URLAnalyser.ConvertSPBaseType2PolicySubtype(_listObj.BaseType.ToString(), "PortletItem");
                    m_obj_referrerurl = _URLAnalyser.ConvertAspxPath(m_obj_referrerurl, Request, "PortletItem");
                    if (m_obj_referrerurl != null && _itemObj != null)
                    {
                        _propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                        _propertyArray[0 * 2 + 1] = _obj_name;
                        _propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                        _propertyArray[1 * 2 + 1] = _obj_title;
                        _propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                        _propertyArray[2 * 2 + 1] = _obj_description;
                        _propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_TYPE;
                        _propertyArray[3 * 2 + 1] = _obj_type;
                        _propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                            CE_ATTR_SP_RESOURCE_SUBTYPE;
                        _propertyArray[4 * 2 + 1] = _obj_subtype;
                        //Add a page type attribute
                        if (Request.FilePath != null && Request.FilePath.StartsWith("/_layouts", StringComparison.OrdinalIgnoreCase))
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_APPLICATION;
                        }
                        else
                        {
                            _propertyArray[5 * 2 + 0] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE;
                            _propertyArray[5 * 2 + 1] = PageResourceFactory.PLE_ATTR_SP_PAGE_TYPE_NORMAL;
                        }
                        if (_itemObj != null)
                        {
                            int oldLen = _propertyArray.Length;
                            string[] newArray = new String[oldLen + 5 * 2];

                            for (int i = 0; i < oldLen; i++)
                            {
                                newArray[i] = _propertyArray[i];
                            }
                            newArray[oldLen + 0 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_CREATED_BY;
                            newArray[oldLen + 0 * 2 + 1] = Globals.
                                GetItemCreatedBySid(_itemObj);
                            newArray[oldLen + 1 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_MODIFIED_BY;
                            newArray[oldLen + 1 * 2 + 1] = Globals.
                                GetItemModifiedBySid(_itemObj);
                            newArray[oldLen + 2 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_DATE_CREATED;
                            newArray[oldLen + 2 * 2 + 1] = Globals.
                                GetItemCreatedStr(_itemObj);
                            newArray[oldLen + 3 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_DATE_MODIFIED;
                            newArray[oldLen + 3 * 2 + 1] = Globals.
                                GetItemModifiedStr(_itemObj);
                            newArray[oldLen + 4 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_FILE_SIZE;
                            newArray[oldLen + 4 * 2 + 1] = Globals.
                                GetItemFileSizeStr(_itemObj);
                            _propertyArray = newArray;
                            // Add other fixed and custom item attributes to the array.
                            _propertyArray = Globals.BuildAttrArrayFromItemProperties
                                (_itemObj.Properties, _propertyArray,
                                 _itemObj.ParentList.BaseType, _itemObj.Fields);
                            //Fix bug 8222, replace the "created" and "modified" properties
                            _propertyArray = Globals.ReplaceHashTime(_webObj, _listObj, _itemObj, _propertyArray);
                            //Fix bug 8694 and 8692,add spfield attr to tailor
                            _propertyArray = Globals.BuildAttrArray2FromSPField(_webObj, _listObj, _itemObj, _propertyArray);
                        }
                        if (Request.HttpMethod == "POST")
                        {
                            if (Request.FilePath.EndsWith("Checkin.aspx", StringComparison.OrdinalIgnoreCase))
                            {
                                String _CheckinAction = Request.Form["ActionCheckin"];
                                if (_CheckinAction != null && _CheckinAction.Equals("CheckinAction", StringComparison.OrdinalIgnoreCase))
                                    _policy_action = CETYPE.CEAction.Write;
                                else
                                    _policy_action = CETYPE.CEAction.Read;
                            }
                            else
                                _policy_action = CETYPE.CEAction.Write;
                        }
                        else
                        {
                            _policy_action = CETYPE.CEAction.Read;
                        }
                        IntPtr localConnectHandle = IntPtr.Zero;
                        String[] enforcement_obligation = { "" };
                        _response = Globals.Custom_CallEval(_policy_action,
                                                    m_obj_referrerurl,
                                                    _obj_targeturl,
                                                    ref _propertyArray,
                                                    ref _emptyArray,
                                                    Request.UserHostAddress,
                                                    CommonVar.GetSPWebContent(_webObj, "loginname"),
                                                    _webObj.CurrentUser.Sid,
                                                    ref m_policyName,
                                                    ref m_policyMessage,
                                                    _before_url,
                                                    _after_url,
                                                    Globals.HttpModuleName,
                                                    _NoiseLevel,
                                                    _webObj,
                                                    true,
                                                    ref localConnectHandle,
                                                    ref enforcement_obligation,
                                                    HttpContext.Current.User);
                        if (Request.HttpMethod == "POST" && _response != CETYPE.CEResponse_t.CEDeny)
                        {
                            PLE_ReportAdminObligationLog _PLE_ReportAdminObligationLog = new PLE_ReportAdminObligationLog();
                            string sitevalue = null;
                            if (_webObj != null)
                            {
                                sitevalue = CommonVar.GetSPWebContent(_webObj, "url");
                            }
                            //Fix bug 8354, added by William 20090203
                            if (sitevalue != null)
                            {
                                sitevalue = Globals.UrlToResSig(sitevalue).ToLower();
                            }
                            AdminPageLogs _AdminPageLogs = new AdminPageLogs();
                            String _SiteVersion = _webObj.AllProperties["vti_extenderversion"].ToString();
                            String[] _adminlogs = _AdminPageLogs.ProcessAdminLogs(Request, _obj_name, "PortletItem", _SiteVersion);
                            if (_adminlogs != null)
                            {
                                _PLE_ReportAdminObligationLog.DoReportLog(localConnectHandle, enforcement_obligation, sitevalue, _adminlogs);
                            }
                        }
                    }
                    return _response;
                }
            }
            catch
            {
            }
            return CETYPE.CEResponse_t.CEAllow;
        }
    }
}
