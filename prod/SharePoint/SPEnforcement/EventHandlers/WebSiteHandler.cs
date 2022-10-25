using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.Common;
using System.Security.Principal;
using System.Linq;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    /// <summary>
    /// Provides methods for trapping events that occur to Web sites, but The
    /// SiteAdding and the WebAdding events are not available at the moment.
    /// "site" ==> site collection
    /// "web"  ==> Web site
    /// 1. Every site has its default web when creating the site! Site itself
    ///    is just a container!
    /// 2. Every web can also have its webs (subweb), but it is still the
    ///    "web", not the "site"!
    /// 3. Site is always create through "Central Administration"!
    ///
    /// ********************NOTICE*************************
    /// SPWebEventReceiver.WebDeleted Method & SPWebEventReceiver.WebDeleting
    /// Method
    ///
    /// Web site deletion events occur only for the first Web site in a chain
    /// of sites.  For example, if a Web site at http://TopSite/Site, which
    /// has the subwebs http://TopSite/Site/Subsite1 and
    /// http://TopSite/Site/Subsite2, is deleted in Office SharePoint Designer
    /// 2007, only one deletion event occurs, namely, for the
    /// http://TopSite/Site root Web site.
    /// </summary>
    public class WebSiteHandler : SPWebEventReceiver
    {
        #region Constructor
        private HttpContext _currentContext;
        public WebSiteHandler()
        {
            _currentContext = HttpContext.Current;
        }
        #endregion

        /// <summary>
        /// SPWebEventReceiver.WebDeleted Method &
        /// SPWebEventReceiver.WebDeleting Method
        ///
        /// Web site deletion events occur only for the first Web site in a
        /// chain of sites.  For example, if a Web site at
        /// http://TopSite/Site, which has the subwebs
        /// http://TopSite/Site/Subsite1 and http://TopSite/Site/Subsite2, is
        /// deleted in Office SharePoint Designer 2007, only one deletion
        /// event occurs, namely, for the http://TopSite/Site root Web site.
        /// </summary>

        #region SiteDeleting
        /// <summary>
        /// Occurs when a site is deleting
        /// </summary>
        public override void SiteDeleting(SPWebEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            // This is not trapped for now.
            base.SiteDeleting(properties);
        }
        #endregion

        #region WebDeletingWithEnforcer
        /// <summary>
        /// Occurs when a web is deleting
        /// </summary>
        private bool EnforceWebDeleting(SPWeb web, ref string ErrorMessage)
        {
            string loginName = web.CurrentUser.LoginName;
            IPrincipal PrincipalUser = null;
            string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                (loginName, web.Url, ref PrincipalUser);

            #region add prefilter
            string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Delete);
            var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
            if (noMatch)
            {
                //donnot query pc,
                NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                return true;
            }
            #endregion

            string sid = web.CurrentUser.Sid;
            string[] emptyArray = new string[0];
            string policyName = null;
            string policyMessage = null;
            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;

            string[] propertyArray = new string[5 * 2];
            string webName = web.Name;
            if (String.IsNullOrEmpty(webName))
                webName = web.Title;

            propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
            propertyArray[0 * 2 + 1] = webName;
            propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            propertyArray[1 * 2 + 1] = web.Title;
            propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
            propertyArray[2 * 2 + 1] = web.Description;
            propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_TYPE;
            propertyArray[3 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_TYPE_VAL_SITE;
            propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_SUBTYPE;
            propertyArray[4 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_SUBTYPE_VAL_SITE;
            response = Globals.CallEval(CETYPE.CEAction.Delete,
                                        web.Url,
                                        null,
                                        ref propertyArray,
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

            NLLogger.OutputLog(LogLevel.Debug, "WebDeleting: eval response is " + response);

            if (response == CETYPE.CEResponse_t.CEAllow) return true;
            else
            {
                ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                return false;
            }
        }

        public override void WebDeleting(SPWebEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            using (SPSite site = new SPSite(properties.SiteId))
            {
                using (SPWeb web = site.OpenWeb(properties.WebId))
                {
                    string ErrorMessage = "";
                    if (EnforceWebDeleting(web, ref ErrorMessage))
                    {
                        base.WebDeleting(properties);
                    }
                    else
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = ErrorMessage;
                    }
                }
            }
        }
        #endregion

        #region WebMovingWithEnforcer
        /// <summary>
        /// Occurs when a web is moving
        /// </summary>
        private bool EnforceWebMoving(SPWeb web, string targetURL, ref string ErrorMessage)
        {
            string loginName = web.CurrentUser.LoginName;
            IPrincipal PrincipalUser = null;
            string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress
                (loginName, web.Url,  ref PrincipalUser);

            #region add prefilter
            string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
            var noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, action);
            if (noMatch)
            {
                //donnot query pc,
                NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                return true;
            }
            #endregion

            string sid = web.CurrentUser.Sid;
            string policyName = null;
            string policyMessage = null;
            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;

            string[] propertyArray = new string[5 * 2];
            //To fix bug 8192 use different arrays to store prop
            string[] propertyArray1 = new string[5 * 2];

            string webName = web.Name;
            if (String.IsNullOrEmpty(webName))
                webName = web.Title;

            propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
            propertyArray[0 * 2 + 1] = webName;
            propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            propertyArray[1 * 2 + 1] = web.Title;
            propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
            propertyArray[2 * 2 + 1] = web.Description;
            propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_TYPE;
            propertyArray[3 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_TYPE_VAL_SITE;
            propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_SUBTYPE;
            propertyArray[4 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_SUBTYPE_VAL_SITE;

            propertyArray1[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
            propertyArray1[0 * 2 + 1] = webName;
            propertyArray1[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
            propertyArray1[1 * 2 + 1] = web.Title;
            propertyArray1[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
            propertyArray1[2 * 2 + 1] = web.Description;
            propertyArray1[3 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_TYPE;
            propertyArray1[3 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_TYPE_VAL_SITE;
            propertyArray1[4 * 2 + 0] = CETYPE.CEAttrKey.
                CE_ATTR_SP_RESOURCE_SUBTYPE;
            propertyArray1[4 * 2 + 1] = CETYPE.CEAttrVal.
                CE_ATTR_SP_SUBTYPE_VAL_SITE;

            response = Globals.CallEval(CETYPE.CEAction.Write,
                                        web.Url,
                                        targetURL,
                                        ref propertyArray,
                //ref propertyArray,
                //To fix bug 8192 use different arrays to store prop
                                        ref propertyArray1,
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

            if (response == CETYPE.CEResponse_t.CEAllow) return true;
            else
            {
                ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                return false;
            }
        }

        public override void WebMoving(SPWebEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            using (SPSite site = new SPSite(properties.SiteId))
            {
                using (SPWeb web = site.OpenWeb(properties.WebId))
                {
                    string ErrorMessage = "";
                    string targetURL = web.Site.Url + properties.NewServerRelativeUrl;
                    if (EnforceWebMoving(web, targetURL, ref ErrorMessage))
                    {
                        base.WebMoving(properties);
                    }
                    else
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = ErrorMessage;
                    }
                }
            }
        }
        #endregion
    }
}
