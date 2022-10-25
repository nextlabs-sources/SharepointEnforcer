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
using System.DirectoryServices;
using NextLabs.SPEnforcer;
using NextLabs.Common;
using NextLabs.PLE.HttpModule;
using Nextlabs.SPSecurityTrimming;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public struct WebPartsEntry
    {
        public WebPartsEntry(SPLimitedWebPartCollection WebParts)
        {
            webParts = WebParts;
            lastTime = DateTime.Now;
        }
        public SPLimitedWebPartCollection webParts;
        public DateTime lastTime;
    }

    public class WebPartsCache
    {
        private static Dictionary<string, WebPartsEntry> webPartsDict;
        private static int CurrentMaxItem;
        private const int MAXITEM = 100;
        private const int MAXITEM_INCREMENT = 20;
        private const int MinimumExpiredSeconds = 30;

        static WebPartsCache()
        {
            webPartsDict = new Dictionary<string, WebPartsEntry>();
            CurrentMaxItem = MAXITEM;
        }
        ~WebPartsCache()
        {
            webPartsDict.Clear();
        }

        public static void AddWebParts(string UrlPath,
                                       SPLimitedWebPartCollection WebParts)
        {
            webPartsDict[UrlPath] = new WebPartsEntry(WebParts);

            if (webPartsDict.Count > CurrentMaxItem)
            {
                // Keep the cache smaller by removing removing the entries that
                // have expired.
                List<string> NeedToRemove = new List<string>();

                foreach (string key in webPartsDict.Keys)
                {
                    // Check the timestamp.
                    DateTime PreviousTime = webPartsDict[key].lastTime;
                    TimeSpan Interval = DateTime.Now - PreviousTime;

                    if (Interval.TotalSeconds > MinimumExpiredSeconds)
                    {
                        NeedToRemove.Add(key);
                    }
                }

                foreach (string key in NeedToRemove)
                {
                    webPartsDict.Remove(key);
                }

                // Adjust the MAXITEM number to avoid sacnning the expire too
                // constantly.
                if (webPartsDict.Count > CurrentMaxItem)
                {
                    CurrentMaxItem = webPartsDict.Count + MAXITEM_INCREMENT;
                }
                else if (webPartsDict.Count < CurrentMaxItem - MAXITEM_INCREMENT)
                {
                    if (webPartsDict.Count + MAXITEM_INCREMENT > MAXITEM)
                    {
                        CurrentMaxItem = webPartsDict.Count + MAXITEM_INCREMENT;
                    }
                    else
                    {
                        CurrentMaxItem = MAXITEM;
                    }
                }
            }
        }

        public static SPLimitedWebPartCollection GetWebParts(string UrlPath)
        {
            WebPartsEntry entry;

            if (!webPartsDict.TryGetValue(UrlPath, out entry))
            {
                // UrlPath not found in cache.
                return null;
            }
            else if ((DateTime.Now - entry.lastTime).TotalSeconds >
                     MinimumExpiredSeconds)
            {
                // The cached entry has expired.
                webPartsDict.Remove(UrlPath);
                return null;
            }
            else
            {
                // Found.
                return entry.webParts;
            }
        }
    }


    public class HttpEnforcerModule : Microsoft.SharePoint.ApplicationRuntime.SharePointHandler, IHttpModule
    {

        PLEHttpModule _PLEHttpModule;
        SPEHttpModule _SPEHttpModule;
        SPTHttpModule _SPTHttpModule;

        static protected IHttpModuleEventHandler _HttpModuleEventHandler = null;
        static private object policyEngineLock = new Object();
        static bool bPolicyEngineLoaded = false;
        // using obsolete object "Microsoft.SharePoint.ApplicationRuntime.SharePointHandler"
#pragma warning disable 618
        public HttpEnforcerModule()
        {
            //add for init dump catch feature
            DumpManager.DumpInitalize();
            _SPEHttpModule = new SPEHttpModule();
            _PLEHttpModule = new PLEHttpModule();
            _SPTHttpModule = new SPTHttpModule();
            NxlFileNotFoundBlocker.Initialize();
            PreFilterModule.Initialize();
        }
#pragma warning restore 618

        // In the Init function, register for HttpApplication
        // events by adding your handlers.
        public void Init(HttpApplication application)
        {
            application.BeginRequest += (new EventHandler(this.Application_BeginRequest));
            application.PreRequestHandlerExecute += (new EventHandler(this.Application_PreRequestHandlerExecute));
            application.EndRequest += (new EventHandler(this.Application_EndRequest));
            // prefilter init only once
            PolicyEngineModuleInit();
            _PLEHttpModule.Init();
        }

        private void PolicyEngineModuleInit()
        {
            lock (policyEngineLock)
            {
                try
                {
                    if (PolicyEngineModule.bDllLoaded && !bPolicyEngineLoaded)
                    {
                        Microsoft.SharePoint.Administration.SPWebApplication spAdminWebApp = null;
                        if (Globals.GetAdministrationWebApplication(ref spAdminWebApp) && spAdminWebApp != null)
                        {
                            var authHost = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHHost] as string;
                            var userName = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHUserName] as string;
                            var password = spAdminWebApp.Properties[Globals.strGlobalJavaPCAUTHPwd] as string;
                            var url = new Uri(authHost);
                            var ret = PolicyEngineModule.policyEngineModuleInit(url.Scheme + "://" + url.Host, url.Port.ToString(), userName, password, Globals.strGlobalPolicyEngineTag, Globals.strGlobalPolicyEngineIntervalSec);
                            if (ret == 0)
                            {
                                NLLogger.OutputLog(LogLevel.Debug, "prefilter init success");
                                bPolicyEngineLoaded = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during prefilter init:", null, ex);
                }
            }
        }
        public void Dispose()
        {
        }
        //To avoid xx/yy.aspx/ case

        private void Application_BeginRequest(Object source, EventArgs e)
        {
            try
            {
                InnerApplication_BeginRequest(source, e);
            }
            catch (System.Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during Application_BeginRequest:", null, ex);
            }
            // Cannot add finally block
            // In asp.net environment, if an thread abort exception throw out, NO ANY CODE CAN BE CONTINUE EXECUTE EXCEPT CATCH BLCOK, otherwise the process will be crash
        }
        private void InnerApplication_BeginRequest(Object source, EventArgs e)
        {
            CommonVar.Init();

            var args = new HttpModuleEventArgs(source as HttpApplication,HttpModuleEvents.BeginRequest);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;
            _SPEHttpModule.BeginRequest(source, e);
        }

        private void Application_PreRequestHandlerExecute(Object source, EventArgs e)
        {
            try
            {
                InnerApplication_PreRequestHandlerExecute(source, e);
            }
            catch (System.Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during Application_PreRequestHandlerExecute:", null, ex);
            }
            // Cannot add finally block
            // In asp.net environment, if an thread abort exception throw out, NO ANY CODE CAN BE CONTINUE EXECUTE EXCEPT CATCH BLCOK, otherwise the process will be crash
        }
        private void InnerApplication_PreRequestHandlerExecute(Object source, EventArgs e)
        {
            //fix bug 24284 , move method SkipRequest,bear wu
            var args = new HttpModuleEventArgs(source as HttpApplication, HttpModuleEvents.PreRequestHandlerExecute);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            try
            {
                if (args.Cancel)
                    return;

                #region add prefilter
                try
                {
                    HttpApplication app = (HttpApplication)source;
                    HttpRequest request = app.Request;
                    NLLogger.OutputLog(LogLevel.Debug, "url path:" + request.FilePath);
                    //fix bug59484,do export in library ,the request is /_vti_bin/owssvr.dll,
                    //our prefilte cannot resolve this request(if do ,delete file will get 403)
                    // if request endwith owssvr.dll,prefilter will skip this request
                    if (!request.FilePath.EndsWith("/_vti_bin/owssvr.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
                        if (null == web)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "Current context web is null");
                        }
                        else
                        {
                            SPUser user = web.CurrentUser;
                            if (user != null)
                            {
                                string loginName = user.LoginName;
                                IPrincipal PrincipalUser = HttpContext.Current.User;
                                var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, "");
                                if (noMatch)
                                {
                                    NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during prefilter:", null, ex);
                }
                #endregion

                if (!_SPEHttpModule.SkipRequest())
                    return;

                //In some cases, the cache is not cleared in last request, we must clear the cache in the request beginning
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                CommonVar.Clear();
                if (!_PLEHttpModule.PreRequest(source, e))
                    return;
                if (!_SPEHttpModule.PreRequest(source, e))
                    return;
                _SPTHttpModule.PreRequest(source, e);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during Application_PreRequestHandlerExecute:", null, ex);
            }
        }

        public void Application_EndRequest(Object source, EventArgs e)
        {
            try
            {
                InnerApplication_EndRequest(source, e);
            }
            catch (System.Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during Application_EndRequest:", null, ex);
            }
            // Cannot add finally block
            // In asp.net environment, if an thread abort exception throw out, NO ANY CODE CAN BE CONTINUE EXECUTE EXCEPT CATCH BLCOK, otherwise the process will be crash
        }
        private void InnerApplication_EndRequest(object sender, EventArgs e)
        {
            try
            {
                //Process Unlock event
                if (HttpContext.Current.Request.HttpMethod.Equals("unlock", StringComparison.OrdinalIgnoreCase))
                {
                    _HttpModuleEventHandler.OnUnlocked(sender, e, HttpContext.Current);
                }

                var args = new HttpModuleEventArgs(sender as HttpApplication, HttpModuleEvents.EndRequest);
                EventHelper.Instance.OnBeforeEventExecuting(this, args);
                if (args.Cancel)
                    return;

                _SPEHttpModule.EndRequest(sender, e);

                // EndRequest for "trimming" module.
                _SPTHttpModule.EndRequest(sender, e);

                CommonVar.Clear();
            }
            finally
            {
                SPEEvalAttrs.Current().SPDispose(); // Dispose the SPWeb and SPSite to avoid memory leakage.
            }
        }

        public static void SetHttpModuleEventHandler(IHttpModuleEventHandler eventHandler)
        {
            _HttpModuleEventHandler = eventHandler;
        }

    }
}
