using System;
using System.Web;
using System.Web.UI;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using Microsoft.SharePoint;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Xml;
using System.Web.SessionState;
using Microsoft.SharePoint.WebControls;
using NextLabs.Diagnostic;
using Nextlabs.PLE.PageModule;
using NextLabs.PLE.Log;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using NextLabs.Common;
using Microsoft.SharePoint.Utilities;
namespace NextLabs.PLE.HttpModule
{

    //Does not need to inherit from Microsoft.SharePoint.ApplicationRuntime.SharePointHandler
    public class PLEHttpModule
    {
        public void Init()
        {
            if (SPEEvalInit.InitAdminLogConfig())
            {
            }
        }

        public bool PreRequest(Object source, EventArgs e)
        {
            try
            {
                bool bResult = true;
                HttpApplication _HttpApplication = (HttpApplication)source;
                HttpRequest _HttpRequest = _HttpApplication.Context.Request;
                HttpContext context = _HttpApplication.Context;
                // Ignore Default Search Indexing User's request
                if (NextLabs.Common.Utilities.IsDefaultIndexingAccount(context.User.Identity.Name))
                {
                    return true;
                }
                if (_HttpRequest.Url.LocalPath != null && _HttpRequest.Url.LocalPath.EndsWith("closeConnection.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
#if SP2010
                if (_HttpRequest.Url.LocalPath.ToLower().EndsWith("/_layouts/enhancedsearch.aspx"))
                {
                    return true;
                }
#endif
                if (!_HttpRequest.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                {
                    NLLogger.OutputLog(LogLevel.Debug, "PreRequestHandlerExecute: enter, thread ID = " +
                     Thread.CurrentThread.ManagedThreadId + ", URL = " +
                     ((HttpApplication)source).Context.Request.Url.AbsoluteUri + ", method = " + ((HttpApplication)source).Context.Request.HttpMethod);
                }
                //Fix bug 8214
                //Add newsbweb.aspx detection to fix bug 615 problem,added by William 20091009
                if (((_HttpRequest.UserAgent!=null&&_HttpRequest.UserAgent.IndexOf("Sharepoint Active-X Upload Control") > -1) || _HttpRequest.FilePath.EndsWith("newsbweb.aspx", StringComparison.OrdinalIgnoreCase))
                    && (_HttpRequest.HttpMethod == "POST"))
                {
                    return true;
                }

                string object_url = Globals.UrlDecode(_HttpRequest.Url.GetLeftPart(UriPartial.Path));
                if (object_url.EndsWith("login.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("inplview.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("WordViewer.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("PowerPoint.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("OneNote.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("xlviewer.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("WordEditor.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("WordEditorFrame", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("OneNoteFrame.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("PowerPointFrame.aspx", StringComparison.OrdinalIgnoreCase)
                    || object_url.EndsWith("XLViewerInternal.aspx", StringComparison.OrdinalIgnoreCase)
                    )
                    return true;
                if (_HttpRequest.HttpMethod != "GET" &&
                    (!_HttpRequest.FilePath.EndsWith("Checkin.aspx", StringComparison.OrdinalIgnoreCase)
                    && !_HttpRequest.FilePath.EndsWith("mngfield.aspx", StringComparison.OrdinalIgnoreCase)
                    && !RunReportPageResource.IsRunReportPage(_HttpRequest)) && !AdminPageLogs.Is_AdminPages(_HttpRequest))
                {
                    return true;
                }

                #region Modify By Roy
                PLEManager pleManager = new PLEManager(_HttpApplication);
                if (!pleManager.IsPLEEnabled())
                {
                    return true;
                }
                #endregion

                HttpContext _context = _HttpApplication.Context;
                HttpResponse _HttpResponse = _context.Response;
                IPageResource _PageResource = null;
                CETYPE.CEResponse_t _reponse = CETYPE.CEResponse_t.CEAllow;
                String _obj_referrerurl = null;
                String _policyName = null;
                String _policyMessage = null;
                _PageResource = PageResourceFactory.create(_HttpRequest, (HttpApplication)source);
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                if (_PageResource != null)
                {
                    _SPEEvalAttr.GenerateSPWeb(_HttpRequest);
                    if (_SPEEvalAttr.WebObj != null)
                    {
                        WebRemoteAddressMap.TrytoAddNewRemoteAddress(CommonVar.GetSPWebContent(_SPEEvalAttr.WebObj, "loginname"), _SPEEvalAttr.WebObj.Url, _HttpRequest.UserHostAddress, _context.User, _HttpRequest);
                    }
                    _reponse = _PageResource.Process(_HttpRequest, _SPEEvalAttr.WebObj);
                    _obj_referrerurl = _PageResource.GetSourceUrl();
                    _policyName = _PageResource.GetPolicyName();
                    _policyMessage = _PageResource.GetPolicyMessage();

                }

                if (_reponse == CETYPE.CEResponse_t.CEDeny)
                {
                    String httpserver = "";
                    String backurl = "";
                    String msg = "";
                    bool _TargetIfSite = false;
                    if (_PageResource != null && Object.ReferenceEquals(_PageResource.GetType(), typeof(SitePageResource)))
                    {
                        _TargetIfSite = true;
                    }
                    if (_obj_referrerurl == null)
                        _obj_referrerurl = object_url;
                    GetDenyParameters(_obj_referrerurl, _policyName, _policyMessage, object_url, _TargetIfSite,
                                        ref httpserver, ref backurl, ref msg);

                    if (!CustomDenyPageSwitch.IsEnabled())
                    {
                        blockRequest(_HttpResponse, GetDenyPageHtml(httpserver, backurl, msg));
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
                            blockRequest(_HttpResponse, GetDenyPageHtml(httpserver, backurl, msg));
                        }
                        else
                        {
                            _HttpResponse.Redirect(strWebUrl + "/_layouts/error-template/DenyPage.aspx?loginName=" + HttpUtility.UrlEncode(context.User.Identity.Name) + "&resouceID=" + HttpUtility.UrlEncode(_SPEEvalAttr.ObjEvalUrl) + "&policyMessage=" + HttpUtility.UrlEncode(msg), false);
                            context.ApplicationInstance.CompleteRequest();
                        }

                    }
                    bResult = false;
                }
                return bResult;
            }
            catch (Exception ex)
            {
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                NLLogger.OutputLog(LogLevel.Warn, "Exception during PreRequest :", null, ex);
                return true;
            }
        }

        private void GetDenyParameters( String obj_referrerurl,
                                        String policyName,
                                        String policyMessage,
                                        String objecturl,
                                        bool TargetIfSite,
                                        ref String httpserver,
                                        ref String backurl,
                                        ref String msg)
        {

            SPWeb _webObj = null;
            String _obj_referrerurl = obj_referrerurl;
            String _policyName = policyName;
            String _policyMessage = policyMessage;

            if (_obj_referrerurl.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                int index2 = _obj_referrerurl.LastIndexOf("/");
                _obj_referrerurl = _obj_referrerurl.Remove(index2);
            }

            _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _backurl = "";
            //Fix bug
            if (_webObj != null)
            {
                if (TargetIfSite)
                {
                    SPWeb _backWeb = _webObj.ParentWeb;
                    if (_backWeb != null)
                    {
                        if (objecturl.IndexOf("_layouts", StringComparison.OrdinalIgnoreCase) < 0)
                            _backurl = _backWeb.Url;
                        else
                            _backurl = CommonVar.GetSPWebContent(_webObj, "url");
                    }
                    else
                    {
                        //There are two cases that its parent web is null:
                        //#1 the web is home/**.aspx
                        //#2 the web is /personal/user
                        String _ServerRelativeUrl = _webObj.ServerRelativeUrl;
                        String site_url = CommonVar.GetSPWebContent(_webObj, "url");
                        if (site_url.EndsWith(_ServerRelativeUrl) && objecturl.IndexOf("_layouts", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            _backurl = site_url.Remove(site_url.Length - _ServerRelativeUrl.Length, _ServerRelativeUrl.Length);
                        }
                        else
                        {
                            _backurl = CommonVar.GetSPWebContent(_webObj, "url");
                        }
                    }
                }
                else
                {
                    _backurl = CommonVar.GetSPWebContent(_webObj, "url");
                }
            }
            //Fix bug 593,site/sitedirectory/Pages/category.aspx is a site equal to site/pages/default.aspx
            if (obj_referrerurl.EndsWith("/sitedirectory/Pages/category.aspx", StringComparison.OrdinalIgnoreCase))
            {
                int index1 = _backurl.LastIndexOf("/");
                _backurl = _backurl.Substring(0, index1);
            }

            String _serverurl = _obj_referrerurl;
            int index = _serverurl.IndexOf("_layouts", StringComparison.OrdinalIgnoreCase);

            if (index > 0)
            {

                _serverurl = _serverurl.Remove(index);
            }

            String _httpserver = _serverurl;
            bool _https = false;
            index = _httpserver.IndexOf("http://");
            if (index >= 0)
            {
                _httpserver = _httpserver.Remove(index, 7);
            }
            index = _httpserver.IndexOf("https://");
            if (index >= 0)
            {
                _httpserver = _httpserver.Remove(index, 8);
                _https = true;
            }
            index = _httpserver.IndexOf("/");
            if (index > 0)
            {
                _httpserver = _httpserver.Remove(index);
            }
            if (!_https)
                _httpserver = "http://" + _httpserver;
            else
                _httpserver = "https://" + _httpserver;

            string _msg = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
            httpserver = _httpserver;
            backurl = _backurl;
            msg = _msg;
        }

        private string TrimEndUrlSegments(string url, int n)
        {
            int index = url.Length;

            for (; n > 0; n--)
            {
                index = url.LastIndexOf('/', index - 1, index);
            }

            return url.Remove(index);
        }

        private void blockRequest(HttpResponse Response, String StatusDescription)
        {
            CommonVar.Clear();
            Response.StatusCode = (int)403;
            Response.ContentType = "text/html";
            Response.Write(StatusDescription);
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        private String GetDenyPageHtml(String httpserver, String backurl, String message)
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
                //+ "<p"
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
    }
}
