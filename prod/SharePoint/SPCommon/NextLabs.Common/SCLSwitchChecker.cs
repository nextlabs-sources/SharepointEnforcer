using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Web;
using Microsoft.SharePoint.WebControls;
using System.ComponentModel;
using NextLabs.Diagnostic;
using Microsoft.SharePoint.Administration;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace NextLabs.Common
{
    public static class SCLSwitchChecker
    {
        #region Site Collection Switch check constant string
        //site collection level constants
        public const string SCL_DEFAULT_KEY = "nxl-emsp-webapp-status-default";
        public const string SCL_ACTIVATED_SITECOLLECTIONS_KEY = "nxl-emsp-status-site-collections";
        public const char SCL_ACTIVATED_SITECOLLECTION_SPLITTER = ';';

        public const bool DEFAULT_SWITCH_VALUE = false;//when cannot get the default value from webapplication
        #endregion
        private static PEStatusCache<bool> SiteCollectionStatusCache = new PEStatusCache<bool>();
        private static PEStatusCache<bool> DefaultStatusCache = new PEStatusCache<bool>();
        private static PEStatusCache<string> ActivatedSitesCache = new PEStatusCache<string>();

        #region public extension methods
        public static bool GetPEStatus(this SPSite siteCollection)
        {
            bool returnValue = DEFAULT_SWITCH_VALUE;
            if (siteCollection != null)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(siteCollection.ID))
                    {
                        returnValue = GetPEStatusInternal(elevatedSite);
                    }
                });
            }
            return returnValue;
        }

        public static bool SetPEStatus(this SPSite siteCollection, bool status)
        {
            bool success = false;
            if (siteCollection != null)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(siteCollection.ID))
                    {
                        success = SetPEStatusInternal(elevatedSite, status);
                    }
                });
            }
            return success;
        }

        public static bool GetPEDefaultStatus(this SPWebApplication webApplication)
        {
            if (IsCentralAdmin())
            {
                return webApplication.GetPEDefaultStatusInternal();
            }
            else
            {
                bool returnValue = DEFAULT_SWITCH_VALUE;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    returnValue = webApplication.GetPEDefaultStatusInternal();
                });
                return returnValue;
            }
        }

        public static bool UpdateSiteCollectionsStatus(this SPWebApplication webApplication, IList<string> activatedSiteCollectionIDs, bool byDefault)
        {
            bool returnValue=false;
            if (webApplication != null && activatedSiteCollectionIDs != null)
            {
                Microsoft.SharePoint.SPSecurity.CodeToRunElevated action = delegate()
                {
                    IEnumerable<SPSite> siteCollectionList;
                    if (byDefault)
                    {
                        //get the urls which is not included in the list
                        siteCollectionList =
                        webApplication.Sites
                        .Where((site) => !activatedSiteCollectionIDs.Contains(site.ID.ToString()));
                    }
                    else
                    {
                        siteCollectionList =
                        webApplication.Sites
                        .Where((site) => activatedSiteCollectionIDs.Contains(site.ID.ToString()));
                    }
                    webApplication.SetPEDefaultStatusInternal(byDefault);
                    webApplication.SetActivatedSiteCollectionsInternal(siteCollectionList.Select((sc)=>sc.ID.ToString()).ToList());
                    returnValue = true;
                    NLLogger.OutputLog(LogLevel.Warn, string.Format("SPE is {0} on following site collections and is {1} on other site collections:{3}{2}{3}",
                        byDefault ? "deactivated" : "activated",
                        byDefault ? "activated" : "deactivated",
                        string.Join(Environment.NewLine, siteCollectionList.Select((sc) => string.Format("{0}({1})", sc.HostName, sc.Url)).ToArray()),
                        Environment.NewLine));
                };
                if (IsCentralAdmin())
                {
                    action();
                }
                else
                {
                    SPSecurity.RunWithElevatedPrivileges(action);
                }
            }
            return returnValue;
        }
        #endregion

        #region private internal methods
        private static bool IsCentralAdmin()
        {
            return SPContext.Current != null
                && SPContext.Current.Site.WebApplication.IsAdministrationWebApplication;
        }
        #region Get&Set Activated SiteCollection Internal
        private static IList<string> GetActivatedSiteCollectionsInternal(this SPWebApplication webApplication)
        {
            IList<string> siteCollections = new List<string>();
            if (webApplication != null)
            {
                try
                {
                    string siteCollectionsString = string.Empty;
                    //try get string from cache first
                    if (!ActivatedSitesCache.TryGetStatus(webApplication.Id, out siteCollectionsString))
                    {
                        var properties = webApplication.Properties;
                        if (properties.ContainsKey(SCL_ACTIVATED_SITECOLLECTIONS_KEY))
                        {
                            siteCollectionsString = properties[SCL_ACTIVATED_SITECOLLECTIONS_KEY].ToString();
                        }
                        //update cache
                        ActivatedSitesCache.TrySetStatus(webApplication.Id, siteCollectionsString);
                    }
                    if (!string.IsNullOrEmpty(siteCollectionsString))
                    {
                        siteCollections = siteCollectionsString.Split(new[] { SCL_ACTIVATED_SITECOLLECTION_SPLITTER }).Distinct().ToList();
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, string.Format("SCLSwitchChecker:GetActivatedSiteCollectionsInternal - Failed to Get WebApplication({0}) Property({1})", webApplication.AlternateUrls.FirstOrDefault(), SCL_ACTIVATED_SITECOLLECTIONS_KEY), null, ex);
                }
            }
            return siteCollections;
        }
        private static bool SetActivatedSiteCollectionsInternal(this SPWebApplication webApplication, IList<string> siteCollections)
        {
            bool returnValue = false;
            if (webApplication != null && siteCollections != null)
            {
                string valueString = string.Join(SCL_ACTIVATED_SITECOLLECTION_SPLITTER.ToString(), siteCollections.ToArray());
                try
                {
                    var properties = webApplication.Properties;
                    if (properties.ContainsKey(SCL_ACTIVATED_SITECOLLECTIONS_KEY))
                    {
                        properties[SCL_ACTIVATED_SITECOLLECTIONS_KEY] = valueString;
                    }
                    else
                    {
                        properties.Add(SCL_ACTIVATED_SITECOLLECTIONS_KEY, valueString);
                    }
                    webApplication.Update();
                    ActivatedSitesCache.TrySetStatus(webApplication.Id, valueString);
                    returnValue = true;
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, string.Format("SCLSwitchChecker:SetActivatedSiteCollectionsInternal - Failed to Set WebApplication({0}) Property({1}={2})", webApplication.AlternateUrls.FirstOrDefault(), SCL_ACTIVATED_SITECOLLECTIONS_KEY, valueString), null, ex);
                }
            }
            return returnValue;
        }
        #endregion

        /// <summary>
        /// Check Site Collection Level Switch from a SiteCollection
        /// Note:Must call this method in SPSecurity.RunWithElevatedPrivileges()
        /// </summary>
        /// <param name="application">SPSite object of event properties</param>
        /// <returns>
        /// True - SPE is enabled for this SiteCollection
        /// False - SP is disabled for this SiteCollection
        /// </returns>
        private static bool GetPEStatusInternal(this SPSite siteCollection)
        {
            if (siteCollection == null)
                return DEFAULT_SWITCH_VALUE;

            bool returnValue;
            //TRY to read from cache
            if (SiteCollectionStatusCache.TryGetStatus(siteCollection.ID, out returnValue))
            {
                return returnValue;
            }

            var webApplication = siteCollection.WebApplication;
            var siteCollections = webApplication.GetActivatedSiteCollectionsInternal();
            var byDefault = webApplication.GetPEDefaultStatusInternal();
            var inList = siteCollections.Contains(siteCollection.ID.ToString());
            //if byDefault is true, not in list means active
            //if byDefault is false, in list means active
            returnValue = byDefault ^ inList;
            //cache the status value and return
            SiteCollectionStatusCache.TrySetStatus(siteCollection.ID, returnValue);

            return returnValue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="siteCollection"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool SetPEStatusInternal(this SPSite siteCollection, bool value)
        {
            bool returnValue = false;
            if (siteCollection != null)
            {
                bool defaultStatusCache = DefaultStatusCache.Enabled;
                bool activatedSiteCollectionCache = ActivatedSitesCache.Enabled;
                try
                {
                    //temporarily disable the cache to make sure the by default value and site collections string are up to date
                    DefaultStatusCache.Enabled = false;
                    ActivatedSitesCache.Enabled = false;
                    var webApplication = siteCollection.WebApplication;
                    var siteCollections = webApplication.GetActivatedSiteCollectionsInternal();
                    var byDefault = webApplication.GetPEDefaultStatusInternal();
                    var inList = siteCollections.Contains(siteCollection.ID.ToString());
                    string siteID = siteCollection.ID.ToString();
                    bool currentStatus = byDefault ^ inList;
                    if (value != currentStatus)
                    {
                        //new status should be updated
                        if (inList)
                        {
                            siteCollections.Remove(siteID);
                        }
                        else
                        {
                            siteCollections.Add(siteID);
                        }
                        //update property
                        webApplication.SetActivatedSiteCollectionsInternal(siteCollections);
                    }
                    //cache
                    SiteCollectionStatusCache.TrySetStatus(siteCollection.ID, value);
                    returnValue = true;
                    NLLogger.OutputLog(LogLevel.Warn, string.Format("SPE is {0} on site collection - {1}({2})",
                        value ? "activated" : "deactivated",
                        siteCollection.RootWeb.Title,
                        siteCollection.Url), null);
                }
                finally
                {
                    //restore cache status
                    DefaultStatusCache.Enabled = defaultStatusCache;
                    ActivatedSitesCache.Enabled = activatedSiteCollectionCache;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Get Default value from Properties of WebApplication
        /// </summary>
        /// <param name="webApplication"></param>
        /// <returns></returns>
        private static bool GetPEDefaultStatusInternal(this SPWebApplication webApplication)
        {
            //the default value of default switch is false
            bool defaultSwitch = DEFAULT_SWITCH_VALUE;
            if (webApplication != null)
            {
                try
                {
                    //try get from cache first
                    if (!DefaultStatusCache.TryGetStatus(webApplication.Id, out defaultSwitch))
                    {
                        var properties = webApplication.Properties;
                        if (properties.ContainsKey(SCL_DEFAULT_KEY))
                        {
                            if (bool.TryParse(properties[SCL_DEFAULT_KEY].ToString(), out defaultSwitch))
                            {
                                //update cache
                                DefaultStatusCache.TrySetStatus(webApplication.Id, defaultSwitch);
                            }
                            else
                            {
                                //is it neccessary to update web application property here?
                                //webApplication.SetPEDefaultStatusInternal(defaultSwitch);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, string.Format("Exception during GetPEDefaultStatusInternal - Failed to Get WebApplication({0}) Property({1})", webApplication.AlternateUrls.FirstOrDefault(), SCL_DEFAULT_KEY), null, ex);
                }
            }
            return defaultSwitch;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="siteCollection"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool SetPEDefaultStatusInternal(this SPWebApplication webApplication, bool value)
        {
            bool returnValue = false;
            if (webApplication != null)
            {
                try
                {
                    var properties = webApplication.Properties;
                    if (properties.ContainsKey(SCL_DEFAULT_KEY))
                    {
                        properties[SCL_DEFAULT_KEY] = value.ToString();
                    }
                    else
                    {
                        properties.Add(SCL_DEFAULT_KEY, value.ToString());
                    }
                    webApplication.Update();
                    DefaultStatusCache.TrySetStatus(webApplication.Id, value);
                    returnValue = true;
                    NLLogger.OutputLog(LogLevel.Warn, string.Format("SPE is {0} for new site collections by default on web application - {1}({2})",
                        value ? "activated" : "deactivated",
                        webApplication.Name,
                        webApplication.GetResponseUri(SPUrlZone.Default)));
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, string.Format("Exception during SetPEDefaultStatusInternal - Failed to Set WebApplication({0}) Property({1}={2})", webApplication.AlternateUrls.FirstOrDefault(), SCL_DEFAULT_KEY, value), null, ex);
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Check Site Collection Level Switch
        /// Used in HttpModule
        /// Note:Must call this method in SPSecurity.RunWithElevatedPrivileges()
        /// </summary>
        /// <param name="application">HttpApplication object of the request</param>
        /// <returns>
        /// True - SPE is enabled for this SiteCollection
        /// False - SP is disabled for this SiteCollection
        /// </returns>
        private static bool GetPolicyEnforcementStatusInternal(HttpApplication httpApplication)
        {
            bool propertyValue = DEFAULT_SWITCH_VALUE;
            if (httpApplication != null)
            {
                HttpContext context = httpApplication.Context;
                try
                {
                    var site = SPControl.GetContextSite(context);
                    if (site != null)
                    {
                        propertyValue = site.GetPEStatusInternal();
                        return propertyValue;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception suring CheckPolicyEnforcementStatusInternal: Failed to get property value from HttpContext\n", null, ex);
                }
                //faile to get property from SPContext
                string objectUrl = GetRequestUrl(context.Request);
                NLLogger.OutputLog(LogLevel.Debug, string.Format("SCLSwitchChecker:GetPolicyEnforcementStatusInternal:object URL={0}", objectUrl));
                using (SPSite siteCollection = new SPSite(objectUrl))
                {
                    propertyValue = siteCollection.GetPEStatusInternal();
                }
            }
            return propertyValue;
        }
        private static string GetRequestUrl(HttpRequest request)
        {
            //try to reuse the url created in BeginRequest, because in some scenario the Request.Url in PreRequestHandler is wrong
            //BeginRequest:http://server/sites/site1/_vti_bin/owssvr.dll?CS=65001
            //PreRequestHandler:http://server/_vti_bin/owssvr.dll?CS=65001
            string requestUrl = string.Empty;
            var evalAttrs = SPEEvalAttrs.Current();
            if (evalAttrs != null)
            {
                requestUrl = evalAttrs.RequestURL;
            }
            if(string.IsNullOrEmpty(requestUrl))
            {
                requestUrl = request.Url.GetLeftPart(UriPartial.Path);
            }
            return requestUrl;
        }
        #endregion

        #region Handler for EventHelper
        public static void OnBeforeEventExecuting(object sender, CancelEventArgs args)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {

                    SPSite site = null;
                    if (args is EventHandlerEventArgs)
                    {
                        var eventProperties = ((EventHandlerEventArgs)args).EventProperties;
                        if (eventProperties != null)
                        {
                            //this event is from an EventReceiver
                            site = new SPSite(eventProperties.SiteId);
                        }
                    }
                    else if (args is HttpModuleEventArgs)
                    {
                        //this event is from a http module
                        var httpModuleEventArgs = args as HttpModuleEventArgs;

                        switch (httpModuleEventArgs.EventType)
                        {
                            case HttpModuleEvents.BeginRequest:
                            case HttpModuleEvents.EndRequest:
                                //skip checking in BeginRequest to avoid 404 error
                                break;
                            default:
                                string requestUrl = GetRequestUrl(httpModuleEventArgs.Application.Request);
                                try
                                {
                                    site = new SPSite(requestUrl);
                                }
                                catch (Exception /*ex*/)
                                {
                                    NLLogger.OutputLog(LogLevel.Debug, String.Format("Establish SPSite object by url:[{0}] failed, maybe the url is not specify a share point site", requestUrl));
                                }
                                break;
                        }
                    }
                    if (site != null)
                    {
                        using (site)
                        {
                            string activatedValue = Globals.GetActivatedSiteIds(site.WebApplication);
                            string newSitePEValue = Globals.GetNewSitePEDefault(site.WebApplication);
                            if (site.WebApplication.GetPEDefaultStatus())
                            {
                                if (site.GetPEStatus())
                                {
                                    // check for new created site in this case.
                                    if (string.IsNullOrEmpty(activatedValue) || !activatedValue.Contains(site.ID.ToString()))
                                    {
                                        if (string.IsNullOrEmpty(newSitePEValue) || newSitePEValue.Equals(Globals.strGlobalEnabled))
                                        {
                                            args.Cancel = false;
                                        }
                                        else
                                        {
                                            args.Cancel = true;
                                            site.SetPEStatus(false); // remove the selected for the site.
                                        }
                                    }
                                    else
                                    {
                                        args.Cancel = false;
                                    }
                                }
                                else
                                {
                                    args.Cancel = true;
                                }
                            }
                            else
                            {
                                // Do nothing for site wehn this webapp is deactiaveted.
                                args.Cancel = true;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during OnBeforeEventExecuting", null, ex);
            }
        }
        #endregion
    }
    #region class for Policy Enforcement Status Cache
    public class PEStatusCache<TValue>
    {
        IDictionary<Guid, TValue> _statusCache;
        object _cacheRootObj = new object();
        DateTime _expiredTime;
        int _interval;
        public PEStatusCache(int mins)
        {
            _interval = mins;
            Enabled = mins > 0;
            _statusCache = new Dictionary<Guid, TValue>();
            ClearCache();
        }

        public PEStatusCache()
            : this(DefaultExpirationInterval)
        {
        }
        public bool Enabled { get; set; }

        private void ClearCache()
        {
            //clear cache
            lock (_cacheRootObj)
            {
                _statusCache.Clear();
                _expiredTime = DateTime.Now.AddMinutes(_interval);
            }
        }

        #region public members
        public bool TryGetStatus(Guid guid, out TValue status)
        {
            status = default(TValue);
            if (!Enabled)
            {
                return false;
            }
            if (_expiredTime >= DateTime.Now)
            {
                //clear cache when it's expired.
                ClearCache();
                return false;
            }
            return _statusCache.TryGetValue(guid, out status);
        }
        public bool TrySetStatus(Guid guid, TValue status)
        {
            try
            {
                if (!Enabled)
                    return false;

                lock (_cacheRootObj)
                {
                    if (_statusCache.ContainsKey(guid))
                    {
                        _statusCache[guid] = status;
                    }
                    else
                    {
                        _statusCache.Add(guid, status);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, $"Exception, Failed to add ({guid}={status}) into cacheduring TrySetStatus:", null, ex);
            }
            return false;
        }
        #endregion

        #region Static Members
        public static int DefaultExpirationInterval { get; private set; }
        static PEStatusCache()
        {
            int mins;
            try
            {
                using (var ceKey = Registry.LocalMachine.OpenSubKey(@"Software\NextLabs\Compliant Enterprise\Sharepoint Enforcer\", false))
                {
                    if (ceKey != null)
                    {
                        object regValue = ceKey.GetValue("CacheExpirationMinutes", 0);
                        if (int.TryParse(regValue.ToString(), out mins))
                        {
                            DefaultExpirationInterval = mins;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during PEStatusCache when reading registry key:", null, ex);
            }
        }
        #endregion
    }
#endregion
}
