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
using NextLabs.SPEnforcer;
using NextLabs.Common;
using System.Text.RegularExpressions;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public partial class SPEHttpModule
    {
        void getSPEEvalAttrFromDocLibItem(HttpRequest Request, SPEEvalAttr _SPEEvalAttr, string[] segments, char[] slashChArr, string _NewUrl)
        {
            string docLibName = Globals.UrlDecode(segments[segments.Length - 3].TrimEnd(slashChArr));

            // We know that docLibName exists in _SPEEvalAttr.WebObj.Lists.
            // Otherwise, this HTTP request, with an URL matching
            // this format but with an invalid docLibName, wouldn't
            // even reach our HTTP module.  Thus _SPEEvalAttr.WebObj.Lists[]
            // won't throw ArgumentException.
            //_listObj = _SPEEvalAttr.WebObj.Lists[docLibName];
            //Actually this place will throw exception if the doclib's name
            //is Changed
            //Code changed to below to fix bug 8166 by William
            try
            {
                _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_NewUrl);
            }
            catch
            {
            }
            if (_SPEEvalAttr.ListObj == null)
            {
                foreach (SPList f in _SPEEvalAttr.WebObj.Lists)
                {
                    if (f != null)
                    {
                        //To verify the name
                        if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            _SPEEvalAttr.ListObj = f;
                            break;
                        }
                    }
                }
            }

            try
            {
                string strId = Request.QueryString["ID"];
                if (string.IsNullOrEmpty(strId))
                {
                    _SPEEvalAttr.Action = "UNKNOWN_ACTION";
                }
                else
                {
                    if (strId.Contains("."))
                    {
                        strId = strId.Substring(0, strId.IndexOf(".")); // convert "4.0.2017-11-29T08:00:00Z" to "4".
                    }
                    int itemId = 0;
                    if (int.TryParse(strId, out itemId))
                    {
                        _SPEEvalAttr.ItemObj = _SPEEvalAttr.ListObj.GetItemById(itemId);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                // No such item ID exists in this doc lib.  URL
                // can't be doc lib item.
                NLLogger.OutputLog(LogLevel.Warn, "Exception during getSPEEvalAttrFromDocLibItem:", null, ex);
                _SPEEvalAttr.Action = "UNKNOWN_ACTION";

            }
            // fix bug 31608, cover all item include file/folder/documentSet.
            if (_SPEEvalAttr.ItemObj != null)
            {
                // URL is a doc lib file item.  We evaluate the
                // item.
                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
            }
        }

        bool SPE_GetEval(HttpRequest Request)
        {
            // It is "Read"
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            char[] slashChArr = { '/' };
            _SPEEvalAttr.Action = "READ";
            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
            //Use a rebuild url to analyse to fix bug 9973, modified by William 20090914
            String _NewUrl = Globals.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
            Uri _RebuildUrl = new Uri(_NewUrl);
            string[] segments = _RebuildUrl.Segments;
            switch (getSPTypeFromUrl(_RebuildUrl))
            {
                case UrlSPType.SITE:
                    {
                        _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode
                            (Globals.TrimEndUrlSegments
                                (_RebuildUrl.GetLeftPart(UriPartial.Path), 2));

                        if (_SPEEvalAttr.WebObj != null)
                        {
                            // fix bug 45184, using web "title" and "description" attributes.
                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                        }
                        break;
                    }
                case UrlSPType.WEB:
                    {
                        using (SPSite elevatedSite = Globals.GetValidSPSite(_RebuildUrl.AbsoluteUri, HttpContext.Current))
                        {
                            SPWeb curWebObj = elevatedSite.OpenWeb();
                            SPEEvalAttrHepler.SetObjEvalAttr(curWebObj, _SPEEvalAttr);
                            _SPEEvalAttr.AddDisposeWeb(curWebObj);
                        }
                    }
                    break;

                case UrlSPType.DOC_LIB:
                    {
                        string docLibName = Globals.UrlDecode
                            (segments[segments.Length - 3].
                                TrimEnd(slashChArr));

                        _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode
                            (Globals.TrimEndUrlSegments
                                (_RebuildUrl.GetLeftPart(UriPartial.Path), 2));
                        try
                        {
                            _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_NewUrl);
                            if (_NewUrl.Contains(_SPEEvalAttr.ListObj.DefaultDisplayFormUrl) ||
                               _NewUrl.Contains(_SPEEvalAttr.ListObj.DefaultEditFormUrl))
                            {
                               getSPEEvalAttrFromDocLibItem(Request, _SPEEvalAttr, segments, slashChArr, _NewUrl);
                                break;
                            }
                        }
                        catch(Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "Exception during case UrlSPType.DOC_LIB:", null, ex);
                        }
                        if (_SPEEvalAttr.ListObj == null)
                        {
                            //Fix bug 8166, added by William in 20090202
                            foreach (SPList f in _SPEEvalAttr.WebObj.Lists)
                            {
                                if (f != null)
                                {
                                    if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.ListObj = f;
                                        break;
                                    }
                                }
                            }
                        }

                        // Fix bug 52498, click folder to open.
                        try
                        {
                            if (_SPEEvalAttr.ListObj != null && !string.IsNullOrEmpty(Request.QueryString["RootFolder"]))
                            {
                                string fullUrl = _SPEEvalAttr.ListObj.ParentWeb.Site.MakeFullUrl(Request.QueryString["RootFolder"]);
                                SPListItem item = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.ListObj.ParentWeb, fullUrl, Utilities.SPUrlListItem);
                                if (item != null)
                                {
                                    SPEEvalAttrHepler.SetObjEvalAttr(item, _SPEEvalAttr);
                                    break;
                                }
                            }
                        }
                        catch
                        {}

                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                        break;
                    }

                case UrlSPType.OTHER_LIST:
                    {
                        string listName = Globals.UrlDecode
                            (segments[segments.Length - 2].
                                TrimEnd(slashChArr));

                        _SPEEvalAttr.ObjEvalUrl = Globals.UrlDecode
                            (Globals.TrimEndUrlSegments
                                (_RebuildUrl.GetLeftPart(UriPartial.Path), 1));

                        try
                        {
                            _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_NewUrl);
                        }
                        catch(Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "Exception during OTHER_LIST GetList:", null, ex);
                        }
                        if (_SPEEvalAttr.ListObj == null)
                        {
                            //Fix bug 8166, added by William in 20090202
                            foreach (SPList f in _SPEEvalAttr.WebObj.Lists)
                            {
                                if (f != null)
                                {
                                    if (listName != null && listName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.ListObj = f;
                                        break;
                                    }
                                }
                            }
                        }

                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                        if (_SPEEvalAttr.ListObj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                        {
                            string classname = "DISCUSSIONS";
                            SPEModuleBase _SPEModuleBase = SPEClass.GetSPEClass(classname);
                            _SPEModuleBase.Init(Request);
                            _SPEModuleBase.DoSPEProcess();
                        }
                        break;
                    }
                case UrlSPType.DOC_LIB_ITEM:
                    {
                        getSPEEvalAttrFromDocLibItem(Request, _SPEEvalAttr, segments, slashChArr, _NewUrl);
                        break;
                    }
                case UrlSPType.OTHER_LIST_ITEM:
                    {
                        string listName = Globals.UrlDecode
                            (segments[segments.Length - 2].
                                TrimEnd(slashChArr));
                        try
                        {
                             _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(_NewUrl);
                        }
                        catch
                        {
                        }
                        if (_SPEEvalAttr.ListObj == null)
                        {
                            foreach (SPList f in _SPEEvalAttr.WebObj.Lists)
                            {
                                if (f != null)
                                {
                                    //To verify the name
                                    if (listName != null && listName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.ListObj = f;
                                        break;
                                    }
                                }
                            }
                        }
                        try
                        {
                            // We know that listName exists in _SPEEvalAttr.WebObj.Lists.
                            // Otherwise, this HTTP request, with an URL
                            // matching this format but with an invalid
                            // listName, wouldn't even reach our HTTP module.
                            // Thus _SPEEvalAttr.WebObj.Lists[] won't throw
                            // ArgumentException.
                            string strId = Request.QueryString["ID"];
                            if (string.IsNullOrEmpty(strId))
                            {
                                _SPEEvalAttr.Action = "UNKNOWN_ACTION";
                            }
                            else
                            {
                                if (strId.Contains("."))
                                {
                                    strId = strId.Substring(0, strId.IndexOf(".")); // convert "4.0.2017-11-29T08:00:00Z" to "4".
                                }
                                int itemId = 0;
                                if (int.TryParse(strId, out itemId))
                                {
                                    _SPEEvalAttr.ItemObj = _SPEEvalAttr.ListObj.GetItemById(itemId);
                                }
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            // No such item ID exists in this list.  URL can't
                            // be other list item.

                            NLLogger.OutputLog(LogLevel.Warn, "Exception during SPE_GetEval:", null, ex);
                            _SPEEvalAttr.Action = "UNKNOWN_ACTION";
                            break;
                        }

                        //for task in sharepoint 2013
                        // For other list items, we use the item title to
                        // construct the "URL" for the item, even though it's
                        // not a real URL that SharePoint recognizes.
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        break;
                    }

                case UrlSPType.NOT_SURE:
                    {
                        // This URL may be a doc lib item in the format
                        // <scheme>://<host>/[...]<doclib>/[...]<file>
                        // or
                        // <scheme>://<host>/[...]<doclib>/[...]<folder>
                        // We check if this is indeed the case.

                        _SPEEvalAttr.Action = "UNKNOWN_ACTION";

                        //Added by William 20090420, to avoid FBA login aspx problem
                        string object_url = Globals.UrlDecode(Request.Url.GetLeftPart(UriPartial.Path));
                        if (object_url.EndsWith("login.aspx", StringComparison.OrdinalIgnoreCase))
                            break;
                        OWA owa = new OWA();
                        string siteURL = _SPEEvalAttr.RequestURL;
                        string requestObjRealUrl = "";
                        owa = CheckIsOPenByOWA(Request, siteURL, ref requestObjRealUrl);
                        bool bFromMObile = false;
                        Regex rgx = new Regex("_layouts(/15)?/mobile/.+.aspx$", RegexOptions.IgnoreCase);
                        Match mt = rgx.Match(siteURL);

                        if (mt.Success)
                        {
                            bFromMObile = true;
                            requestObjRealUrl = siteURL.Substring(0, mt.Index);
                        }
                        else if (Request.QueryString["Mobile"] == "1") //special case for community site accessed from mobile.
                        {
                            int nIndex = siteURL.IndexOf("?Mobile=1", StringComparison.OrdinalIgnoreCase);
                            if (nIndex > 0)
                            {
                                requestObjRealUrl = siteURL.Substring(0, nIndex);
                                requestObjRealUrl = requestObjRealUrl.EndsWith("/") ? requestObjRealUrl : requestObjRealUrl + "/";
                                bFromMObile = true;
                            }
                        }

                        // Added by Derek
                        if (!owa.isOWA && Request.Url.AbsoluteUri.IndexOf("/_layouts/", StringComparison.OrdinalIgnoreCase) > 0 && !bFromMObile)
                        {
                            return true;
                        }
                        if (owa.isOWA)
                        {
                            _SPEEvalAttr.IsOWA = true;
                            //When OWA enable, To get SPListItem's properties somehow  will call "File not found" exception.
                            _SPEEvalAttr.SiteObj = new SPSite(requestObjRealUrl);
                            if (_SPEEvalAttr.SiteObj != null)
                            {
                                _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                                _SPEEvalAttr.AddDisposeSite(_SPEEvalAttr.SiteObj);
                                _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                            }
                            if (owa.objectType == OWA_OBJECT_TYPE.OWAFramePage)
                            {
                                SetEvalAttr4OWAFramePage(Request, owa, requestObjRealUrl, ref _SPEEvalAttr);
                            }
                            else
                            {
                                SetEvalAttr(Request, owa, requestObjRealUrl, ref _SPEEvalAttr);
                            }
                            GetParamsFromSPListItem4OWA(ref _SPEEvalAttr, _SPEEvalAttr.ItemObj);
                        }
                        else
                        {
                            try
                            {
                                _SPEEvalAttr.SiteObj = new SPSite(requestObjRealUrl);
                                _SPEEvalAttr.AddDisposeSite(_SPEEvalAttr.SiteObj);
                            }
                            catch
                            {
                                _SPEEvalAttr.SiteObj = _SPEEvalAttr.WebObj.Site;
                            }

                            if (_SPEEvalAttr.SiteObj != null)
                            {
                                _SPEEvalAttr.WebObj = _SPEEvalAttr.SiteObj.OpenWeb();
                                _SPEEvalAttr.AddDisposeWeb(_SPEEvalAttr.WebObj);
                            }
                            if (owa.objectType == OWA_OBJECT_TYPE.OWAFramePage)
                            {
                                SetEvalAttr4OWAFramePage(Request, owa, requestObjRealUrl, ref _SPEEvalAttr);
                            }
                            else
                            {
                                SetEvalAttr(Request, owa, requestObjRealUrl, ref _SPEEvalAttr);
                            }
                        }
                        break;
                    }
            } /* switch */
            return false;
        }


        bool SPE_Preparation(Object source)
        {
            bool bMultipleFile = false;
            bool bNeedSPDesignerWorkaround = false;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            HttpApplication application = (HttpApplication)source;
            HttpRequest Request = application.Context.Request;
            HttpContext context = application.Context;
            HttpResponse Response = context.Response;
            if (_SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.RequestURL_path.EndsWith("closeConnection.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (_SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.IsPost && (_SPEEvalAttr.RequestURL_path.EndsWith("author.dll", StringComparison.OrdinalIgnoreCase)
                || _SPEEvalAttr.RequestURL_path.EndsWith("owssvr.dll", StringComparison.OrdinalIgnoreCase)
                || _SPEEvalAttr.RequestURL_path.EndsWith("admin.dll", StringComparison.OrdinalIgnoreCase)
                || _SPEEvalAttr.RequestURL_path.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase)))
            {
                if (HttpContext.Current.Request.Headers["X_READ"] == "1")
                {
                    NLLogger.OutputLog(LogLevel.Debug, "PreRequestHandlerExecute: author.dll second request come in");
                    //String _name = "Request ID " + Thread.CurrentThread.ManagedThreadId;
                    if (HttpContext.Current.Request.Headers["X_Result"] == "1")
                    {
                         //bear wu fix bug 23854
                        String backurl = "";
                        String httpserver = "";
                        String msg = "";
                        if (HttpContext.Current.Request.Headers["X_BackUrl"] != null && HttpContext.Current.Request.Headers["X_Message"] != null && HttpContext.Current.Request.Headers["X_HttpServer"]!=null)
                        {
                            backurl = HttpContext.Current.Request.Headers["X_BackUrl"];
                            httpserver = HttpContext.Current.Request.Headers["X_HttpServer"];
                            msg = HttpContext.Current.Request.Headers["X_Message"];
                        }
                        if (!CustomDenyPageSwitch.IsEnabled())
                        {
                            blockRequest((HttpApplication)source, HttpContext.Current.Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
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
                        //end
                    }
                    return true;
                }
            }
            // Ignore Default Search Indexing User's request
            if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(context.User.Identity.Name))
            {
                return true;
            }
            if (((Request.UserAgent != null && Request.UserAgent.IndexOf("Sharepoint Active-X Upload Control") > -1) && (Request.HttpMethod == "POST")))
            {
                bMultipleFile = true;
            }
            // This fixes Bug 9037.  When the user tries to edit a page in
            // SharePoint Designer, accessing _webObj.CurrentUser.LoginName
            // or _webObj.Url would cause error on the client side.  So in
            // this case we use a workaround to figure out the URL.
            //
            // SharePoint Designer 2007 calls itself both
            // "MSFrontPage/12.0" and "Mozilla/4.0 (compatible; MS
            // FrontPage 12.0)".  It seems like we only need this
            // workaround when it calls itself "MSFrontPage/12.0" and the
            // method is "POST".
            if (Request.UserAgent != null && Request.UserAgent == "MSFrontPage/12.0" && Request.HttpMethod == "POST")
            {
                bNeedSPDesignerWorkaround = true;
            }

            BeginProcessUploadEvent(source);
            EndProcessUploadEvent(source);

            // start from here, edited by tonny for bug 8214
            /*
             * we don't need to do anything if upload file in this function.
             * So we check the request if  upload file we return immediately.
            */
            // start from here, edited by tonny for bug 8214
            // Whether this is Target to a SharePoint Web?
            // Record the Remote Addr.
            // NOTICE: we now have problem for "same user, same website, diffierent IPs"!
            {
                NLLogger.OutputLog(LogLevel.Debug, "Get the SPWeb object, thread ID = " +
                         Thread.CurrentThread.ManagedThreadId + ", URL = " +
                         _SPEEvalAttr.RequestURL + ", method = " + _SPEEvalAttr.HttpMethod);

                if (bMultipleFile || bNeedSPDesignerWorkaround)
                {
                    _SPEEvalAttr.LoginName = _SPEEvalAttr.LogonUser = context.User.Identity.Name;
                    if (Request.RawUrl != null && Request.Url != null && Request.Url.AbsoluteUri != null)
                    {
                        int subsitelen = Request.RawUrl.IndexOf("/_vti_bin/", StringComparison.OrdinalIgnoreCase);
                        int topsitelen = Request.Url.AbsoluteUri.IndexOf("/_vti_bin/", StringComparison.OrdinalIgnoreCase);
                        if (subsitelen >= 0 && topsitelen >= 0)
                        {
                            string subsitename = Request.RawUrl.Substring(0, subsitelen);
                            string topsitename = Request.Url.AbsoluteUri.Substring(0, topsitelen);
                            StringBuilder fullsitename = new StringBuilder(topsitename);
                            fullsitename.Append(subsitename);
                            _SPEEvalAttr.WebUrl = fullsitename.ToString();
                        }
                    }
                }
                else
                {
                    _SPEEvalAttr.GenerateSPWeb(Request);
                    if (!_SPEEvalAttr.IsPost && (_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/upload.aspx", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/15/Upload.aspx", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/WordViewer.aspx", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/PowerPoint.aspx", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/xlviewer.aspx", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/OneNote.aspx", StringComparison.OrdinalIgnoreCase)))
                    {
                        int _index = Request.RawUrl.IndexOf("?");
                        String _RelativeUrl = (_index > -1) ? (Request.RawUrl.Substring(0, _index)) : Request.RawUrl;
                        String _Host = Request.Params["HTTP_HOST"];
                        if (string.IsNullOrEmpty(_Host))
                            _Host = Request.Url.Host;
                        String _Url = "http://" + _Host + _RelativeUrl;
                        int pos = _Url.IndexOf("/_layouts/", StringComparison.OrdinalIgnoreCase);
                        if (pos != -1)
                            _SPEEvalAttr.WebUrl = _Url.Remove(pos);
                        else
                            _SPEEvalAttr.WebUrl = _Url;
                        _SPEEvalAttr.LoginName = _SPEEvalAttr.LogonUser = context.User.Identity.Name;
                    }
                    else
                    {
                        try
                        {
                            if ((_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/upload.aspx", StringComparison.OrdinalIgnoreCase))
                                || (_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/15/upload.aspx", StringComparison.OrdinalIgnoreCase))
                                || (_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/15/uploadex.aspx", StringComparison.OrdinalIgnoreCase))
                                || (_SPEEvalAttr.RequestURL_path.EndsWith("/_layouts/uploadex.aspx", StringComparison.OrdinalIgnoreCase)))
                            {
                                int _index = Request.RawUrl.IndexOf("?");
                                String _RelativeUrl = (_index > -1) ? (Request.RawUrl.Substring(0, _index)) : Request.RawUrl;
                                String _Url = "http://" + Request.Url.Host + _RelativeUrl;
                                int pos = _Url.IndexOf("/_layouts/", StringComparison.OrdinalIgnoreCase);
                                if (pos != -1)
                                    _SPEEvalAttr.WebUrl = _Url.Remove(pos);
                                else
                                    _SPEEvalAttr.WebUrl = _Url;
                                _SPEEvalAttr.LoginName = _SPEEvalAttr.LogonUser;
                            }
                            else
                            {
                                _SPEEvalAttr.WebUrl = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "url");
                                _SPEEvalAttr.LoginName = _SPEEvalAttr.LogonUser = CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "loginname");
                            }
                        }
                        catch
                        {
                            _SPEEvalAttr.WebUrl = null;
                            _SPEEvalAttr.LoginName = _SPEEvalAttr.LogonUser = context.User.Identity.Name;
                        }
                    }
                }
                if ((_SPEEvalAttr.LoginName != null) && (_SPEEvalAttr.WebUrl != null))
                {
                    WebRemoteAddressMap.TrytoAddNewRemoteAddress(_SPEEvalAttr.LoginName, _SPEEvalAttr.WebUrl, _SPEEvalAttr.RemoteAddr, context.User,Request);
                }
            }
            if (_SPEEvalAttr.RequestURL_path != null
                && (_SPEEvalAttr.RequestURL_path.EndsWith("shtml.dll", StringComparison.OrdinalIgnoreCase)
                || _SPEEvalAttr.RequestURL_path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                || (_SPEEvalAttr.RequestURL_path.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase) && !_SPEEvalAttr.RequestURL_path.EndsWith("OneNote.ashx", StringComparison.OrdinalIgnoreCase))
                || _SPEEvalAttr.RequestURL_path.EndsWith(".axd", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (bMultipleFile
                || _SPEEvalAttr.HttpMethod.Equals("LOCK")
                || _SPEEvalAttr.HttpMethod.Equals("UNLOCK")
                || Request == null
                || (_SPEEvalAttr.RequestURL_path != null && _SPEEvalAttr.RequestURL_path.IndexOf("_layouts/", StringComparison.OrdinalIgnoreCase) != -1
                    && _SPEEvalAttr.RequestURL_path.IndexOf("/images", StringComparison.OrdinalIgnoreCase) != -1))
            {
                return true;
            }
            if (_SPEEvalAttr.WebObj == null)
            {
                try
                {
                    _SPEEvalAttr.WebObj = SPControl.GetContextWeb(context);
                }
                catch
                {
                }
            }

           // char[] chHeader = { 'S', 'P', 'W', 'e', 'b', ':' };

            if (Request.UrlReferrer != null)
            {
                _SPEEvalAttr.ObjEvalUrl = Request.UrlReferrer.AbsoluteUri;
            }
            if (_SPEEvalAttr.WebObj != null)
            {
                if (_SPEEvalAttr.RequestURL_path.IndexOf("default.aspx", StringComparison.OrdinalIgnoreCase) > -1
                    || _SPEEvalAttr.RequestURL_path.IndexOf("category.aspx", StringComparison.OrdinalIgnoreCase) > -1
                    || _SPEEvalAttr.RequestURL_path.IndexOf("home.aspx", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    System.Guid guid = new System.Guid("4f6fd05e-b392-418b-9dbf-b0fb92f12271");
                    if (_SPEEvalAttr.WebObj.Features[guid] == null)
                    {
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            using (SPSite elevatedSite = Globals.GetValidSPSite(_SPEEvalAttr.WebUrl, HttpContext.Current))
                            {
                                using (SPWeb elevatedWeb = elevatedSite.OpenWeb())
                                {
                                    elevatedWeb.AllowUnsafeUpdates = true;
                                    elevatedWeb.Features.Add(guid, false);
                                }
                            }
                        });
                    }

                }
            }
            return false;
        }

        enum OWA_OPEN_TYPE
        {
            View = 0,
            Edit
        };

        enum OWA_OBJECT_TYPE
        {
            UnKnow = -1,
            Excel = 0,
            Word = 1,
            PPT = 2,
            OneNote = 3,
            OWAFramePage = 4,
        };

        class OWA
        {
            public bool isOWA = false;
            public OWA_OPEN_TYPE type = OWA_OPEN_TYPE.View;
            public OWA_OBJECT_TYPE objectType = OWA_OBJECT_TYPE.UnKnow;
        };

        OWA CheckIsOPenByOWA(HttpRequest Request, string siteUrl, ref string requestObjRealUrl)
        {
            OWA owa = new OWA();
            string object_url = siteUrl;
            //check the normal open
            if (Request.Url.AbsoluteUri.IndexOf("_layouts/OneNote.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/xlviewer.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/WordViewer.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/PowerPoint.aspx", StringComparison.OrdinalIgnoreCase) > 0)
            {
                owa.isOWA = true;
                string queryID = null;
                queryID = Request.QueryString["id"];
                string startIndex = "https://";
                int endIndex = siteUrl.IndexOf('/', startIndex.Length);
                if (endIndex > 0)
                {
                    siteUrl = siteUrl.Substring(0, endIndex);
                }

                if (Request.Url.AbsoluteUri.IndexOf("Edit=1", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    owa.type = OWA_OPEN_TYPE.Edit;
                }
                if (Request.Url.AbsoluteUri.IndexOf("EditView", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    queryID = Request.QueryString["PresentationId"];
                    owa.type = OWA_OPEN_TYPE.Edit;
                    owa.objectType = OWA_OBJECT_TYPE.PPT;
                }
                if (Request.Url.AbsoluteUri.IndexOf("ReadingView", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    queryID = Request.QueryString["PresentationId"];
                    owa.objectType = OWA_OBJECT_TYPE.PPT;
                }
                object_url = siteUrl + queryID;
            }

            if (Request.Url.AbsoluteUri.IndexOf("_layouts/WordEditor.aspx", StringComparison.OrdinalIgnoreCase) > 0)
            {
                owa.isOWA = true;
                string queryID = null;
                queryID = Request.QueryString["id"];
                string startIndex = "https://";
                int endIndex = siteUrl.IndexOf('/', startIndex.Length);
                if (endIndex > 0)
                {
                    siteUrl = siteUrl.Substring(0, endIndex);
                }
                object_url = siteUrl + queryID;
                owa.type = OWA_OPEN_TYPE.Edit;
            }

            //check "Edit in browser" tab from view mode to edit mode
            if (Request.Url.AbsoluteUri.IndexOf("_layouts/OneNoteFrame.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/WordEditorFrame.aspx", StringComparison.OrdinalIgnoreCase) > 0
                || Request.Url.AbsoluteUri.IndexOf("_layouts/PowerPointFrame.aspx", StringComparison.OrdinalIgnoreCase) > 0)
            {
                owa.isOWA = true;
                owa.type = OWA_OPEN_TYPE.Edit;
                if (Request.Url.AbsoluteUri.IndexOf("Edit=0", StringComparison.OrdinalIgnoreCase) > 0
                    || Request.Url.AbsoluteUri.IndexOf("ReadingView", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    owa.type = OWA_OPEN_TYPE.View;
                }
                if (Request.Url.AbsoluteUri.IndexOf("_layouts/OneNoteFrame.aspx", StringComparison.OrdinalIgnoreCase) > 0
                    && Request.UrlReferrer.ToString().IndexOf("DefaultItemOpen=1", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    owa.type = OWA_OPEN_TYPE.View;
                }

                owa.objectType = OWA_OBJECT_TYPE.OWAFramePage;
            }

            //check "Edit in browser" tab from view mode to edit mode when excel case
            if (Request.Url.AbsoluteUri.IndexOf("_vti_bin/EwaInternalWebService.json/CloseWorkbook", StringComparison.OrdinalIgnoreCase) > 0
                && Request.UrlReferrer.ToString().IndexOf("_layouts/xlviewer.aspx", StringComparison.OrdinalIgnoreCase) > 0)
            {
                owa.isOWA = true;
                owa.type = OWA_OPEN_TYPE.Edit;
                string[] urlReferrer = Request.UrlReferrer.ToString().Split('&');
                string[] param = urlReferrer[0].Split('?');
                string startIndex = "https://";
                int endIndex = siteUrl.IndexOf('/', startIndex.Length);
                if (endIndex > 0)
                {
                    siteUrl = siteUrl.Substring(0, endIndex);
                }
                object_url = siteUrl + param[1].Substring(param[1].IndexOf("=") + 1);
            }

            //check "Edit in browser" tab from view mode to edit mode when onenote case
            if (Request.Url.AbsoluteUri.IndexOf("_vti_bin/OneNote.ashx") > 0
                && Request.UrlReferrer.ToString().IndexOf("_layouts/OneNoteFrame.aspx") > 0)
            {
                owa.isOWA = true;
                byte[] bytes = new byte[Request.InputStream.Length];
                Request.InputStream.Read(bytes, 0, bytes.Length);
                Request.InputStream.Position = 0;
                string content = Encoding.ASCII.GetString(bytes);
                if (content.Contains("IsVersionHistoryEnabled"))
                {
                    owa.type = OWA_OPEN_TYPE.Edit;
                }
                owa.objectType = OWA_OBJECT_TYPE.OWAFramePage;
            }
            requestObjRealUrl = object_url;
            return owa;
        }

        void SetEvalAttr(HttpRequest request, OWA owa, string requestObjRealUrl, ref SPEEvalAttr _SPEEvalAttr)
        {
            Object obj = null;
            SPWeb mSPWeb = null;
            SPList msplist = null;
            mSPWeb = _SPEEvalAttr.WebObj;
            {
                string _weburl = (mSPWeb.Url.EndsWith("/") ? mSPWeb.Url : mSPWeb.Url + "/");
                //to check site
                if (_weburl.Equals(requestObjRealUrl, StringComparison.OrdinalIgnoreCase) ||
                    _weburl.Equals(requestObjRealUrl + "/", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.Action = "READ Web";
                    SPEEvalAttrHepler.SetObjEvalAttr(mSPWeb, _SPEEvalAttr);
                    return;
                }
                //to check list
                if (requestObjRealUrl.EndsWith("AllItems.aspx", StringComparison.OrdinalIgnoreCase)
                        || requestObjRealUrl.EndsWith("NewForm.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    //to fix 18120, if such format, it maybe a list
                    msplist = mSPWeb.GetList(requestObjRealUrl);
                }
                if (msplist != null)
                {
                    _SPEEvalAttr.Action = "READ LIST";
                    SPEEvalAttrHepler.SetObjEvalAttr(msplist, _SPEEvalAttr);
                }
                else
                {
                    try
                    {
                        obj = mSPWeb.GetObject(requestObjRealUrl);
                    }
                    catch
                    {
                    }

                    if (obj == null)
                    {
                        // URL is not an item.
                        return;
                    }
                    Type type = obj.GetType();
                    if (!Object.ReferenceEquals(type, typeof(SPListItem)) && !Object.ReferenceEquals(type, typeof(SPFile)))
                    {
                        SPListItem item = Globals.ParseItemFromAttachmentURL(mSPWeb, requestObjRealUrl);
                        if (item != null)
                            obj = item as Object;
                    }
                    type = obj.GetType();
                    if (Object.ReferenceEquals(type, typeof(SPListItem)))
                    {
                        _SPEEvalAttr.Action = "READ";
                        if (owa.type == OWA_OPEN_TYPE.Edit)
                        {
                            _SPEEvalAttr.Action = "EDIT";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                        }
                        _SPEEvalAttr.ItemObj = (SPListItem)obj;
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                    }
                    else if (Object.ReferenceEquals(type, typeof(SPFile)))
                    {
                        SPFile dsFile = (SPFile)obj;
                        if (requestObjRealUrl.Contains("Attachments"))
                        {
                            _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(requestObjRealUrl);
                            string[] splitBuf = requestObjRealUrl.Split(new Char[] { '/' });
                            int splitCount = 0;
                            foreach (string s in splitBuf)
                            {
                                splitCount++;
                            }
                            string itemId = splitBuf[splitCount-2];
                            _SPEEvalAttr.ItemObj = _SPEEvalAttr.ListObj.GetItemById(Convert.ToInt32(itemId));
                        }
                        if (dsFile != null && dsFile.Exists)
                        {
                            _SPEEvalAttr.Action = "READ ATTACHMENT";
                            if (owa.type == OWA_OPEN_TYPE.Edit)
                            {
                                _SPEEvalAttr.Action = "EDIT ATTACHMENT";
                                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                            }
                            if (_SPEEvalAttr.ItemObj!=null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        }
                    }
                }
            }
        }

        void SetEvalAttr4OWAFramePage(HttpRequest Request, OWA owa, string requestObjRealUrl, ref SPEEvalAttr _SPEEvalAttr)
        {
            /*
             * To deal with just as below request url:
             * Site:/_layouts/PowerPointFrame.aspx?PowerPointView=ReadingView&d=F-SITEID-m-WEBID-m-ITEMID-m&source=Site:/.../AllItems.aspx
             * Site:/_layouts/WordEditorFrame.aspx?d=F-SITEID-m-WEBID-m-ITEMID-m&source=Site:/.../AllItems.aspx
             * Site:/_layouts/OneNoteFrame.aspx?d=F-SITEID-m-WEBID-m-ITEMID-m&edit=0&source=Site:/.../AllItems.aspx
             * */
            SPWeb mSPWeb = null;
            SPList msplist = null;
            SPListItem msplistitem = null;
            try
            {
                mSPWeb = _SPEEvalAttr.WebObj;
                string spListItemId = "";
                string[] urlArgs = { };
                string listUrl = "";
                if (Request.Url.ToString().Contains("_vti_bin/OneNote.ashx"))
                {
                    string url = Globals.UrlDecode(Request.UrlReferrer.ToString());
                    urlArgs = url.Split('&');
                    int start = url.IndexOf("source=");
                    listUrl = url.Substring(start + 7);
                }
                else
                {
                    urlArgs = _SPEEvalAttr.RequestURL.ToString().Split('&');
                    listUrl = Request.QueryString["source"];
                }
                //get Listitem guid
                {
                    string guid = null;
                    //PPT case
                    if (urlArgs[0].Contains("PowerPointView"))
                    {
                        guid = urlArgs[1];
                    }
                    else
                    {
                        string[] args = urlArgs[0].Split('?');
                        guid = args[args.Length - 1];
                    }

                    if (guid != null)
                    {
                        string[] urlstring = guid.Split('m');
                        spListItemId = urlstring[urlstring.Length - 2];
                    }

                    //Click SPListItem from home page.
                    if (listUrl.Contains("SitePages/Home.aspx"))
                    {
                        SPListItem temp = null;
                        foreach (SPList list in mSPWeb.Lists)
                        {
                            try
                            {
                                temp = list.GetItemByUniqueId(new Guid(spListItemId));
                            }
                            catch
                            {
                            }
                            if ( temp != null)
                            {
                                msplistitem = temp;
                                msplist = msplistitem.ParentList;
                                break;
                            }
                        }
                    }
                    else
                    {
                        msplist = mSPWeb.GetList(listUrl);
                    }
                }

                if (msplist != null && _SPEEvalAttr.RequestURL.ToString().IndexOf("/Lists/") > 0)
                {
                    //List Attachement file.
                    foreach (SPListItem m_ListItem in msplist.Items)
                    {
                        SPAttachmentCollection attachments = m_ListItem.Attachments;
                        foreach (string url in attachments)
                        {
                            SPFile _file = null;
                            _file = m_ListItem.ParentList.ParentWeb.GetFile(attachments.UrlPrefix + url);
                            if (_file.UniqueId.ToString().Replace("-", "") == spListItemId)
                            {
                                msplistitem = m_ListItem;
                                _SPEEvalAttr.ItemObj = m_ListItem;
                                _SPEEvalAttr.ListObj = _SPEEvalAttr.ItemObj.ParentList;
                                if (owa.type == OWA_OPEN_TYPE.Edit)
                                {
                                    _SPEEvalAttr.Action = "EDIT ATTACHMENT";
                                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                }
                                else
                                {
                                    _SPEEvalAttr.Action = "Read ATTACHMENT";
                                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                }

                                if (_SPEEvalAttr.ItemObj !=null)
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);



                                int index = _SPEEvalAttr.RequestURL.ToString().IndexOf("/_layouts/WordEditorFrame.aspx");
                                if (index == -1)
                                {
                                    index = _SPEEvalAttr.RequestURL.ToString().IndexOf("/_layouts/PowerPointFrame.aspx");
                                }
                                if (index == -1)
                                {
                                    index = _SPEEvalAttr.RequestURL.ToString().IndexOf("/_layouts/OneNoteFrame.aspx");
                                }
                                string spWebUrl = _SPEEvalAttr.WebObj.Url.ToString();
                                spWebUrl = _SPEEvalAttr.RequestURL.Substring(0, index + 1);
                                _SPEEvalAttr.ObjEvalUrl = spWebUrl + _SPEEvalAttr.ObjEvalUrl;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //document libary item
                    if (msplistitem == null)
                    {
                        msplistitem = msplist.GetItemByUniqueId(new Guid(spListItemId));
                    }

                    if (msplistitem != null)
                    {
                        if (owa.type == OWA_OPEN_TYPE.Edit)
                        {
                            _SPEEvalAttr.Action = "Edit";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                        }
                        else
                        {
                            _SPEEvalAttr.Action = "Read";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        }

                        SPEEvalAttrHepler.SetObjEvalAttr(msplistitem, _SPEEvalAttr);
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SetEvalAttr4OWAFramePage:", null, ex);
            }
        }

        void GetParamsFromSPListItem4OWA(ref SPEEvalAttr _SPEEvalAttr, SPListItem spListItem)
        {
            string[] propertyArray = new string[5 * 2];
            propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
            propertyArray[0 * 2 + 1] = _SPEEvalAttr.ObjName;
            propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            propertyArray[1 * 2 + 1] = _SPEEvalAttr.ObjTitle;
            propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
            propertyArray[2 * 2 + 1] = _SPEEvalAttr.ObjDesc;
            propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE;
            propertyArray[3 * 2 + 1] = _SPEEvalAttr.ObjType;
            propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE;
            propertyArray[4 * 2 + 1] = _SPEEvalAttr.ObjSubtype;

            if (spListItem != null)
            {
                int oldLen = propertyArray.Length;
                string[] newArray = new string[oldLen + 5 * 2];

                for (int i = 0; i < oldLen; i++)
                {
                    newArray[i] = propertyArray[i];
                }

                newArray[oldLen + 0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY;
                newArray[oldLen + 0 * 2 + 1] = Globals.GetItemCreatedBySid(spListItem);
                newArray[oldLen + 1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY;
                newArray[oldLen + 1 * 2 + 1] = Globals.GetItemModifiedBySid(spListItem);
                newArray[oldLen + 2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_CREATED;
                newArray[oldLen + 2 * 2 + 1] = Globals.GetItemCreatedStr(spListItem);
                newArray[oldLen + 3 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DATE_MODIFIED;
                newArray[oldLen + 3 * 2 + 1] = Globals.GetItemModifiedStr(spListItem);
                newArray[oldLen + 4 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE;
                newArray[oldLen + 4 * 2 + 1] = Globals.GetItemFileSizeStr(spListItem);

                propertyArray = newArray;

                //Add other fixed and custom item attributes to the array.
                propertyArray = Globals.BuildAttrArrayFromItemProperties(spListItem.Properties, propertyArray, spListItem.ParentList.BaseType, spListItem.Fields);
                //Fix bug 8222, replace the "created" and "modified" properties
                propertyArray = Globals.ReplaceHashTime(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, spListItem, propertyArray);
                //Fix bug 8694 and 8692,add spfield attr to tailor
                propertyArray = Globals.BuildAttrArray2FromSPField(_SPEEvalAttr.WebObj, _SPEEvalAttr.ListObj, spListItem, propertyArray);
                _SPEEvalAttr.Params4OWA = propertyArray;
            }
        }
    }
}
