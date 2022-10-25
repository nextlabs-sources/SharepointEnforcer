using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using System.Security.Principal;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    /// <summary>
    /// Trap events that are raised after the Feature installation,
    /// uninstallation, activation, or deactivation action has been performed.
    /// It is not possible to cancel an installation or uninstallation through
    /// Feature events.
    /// </summary>
    public class FeatureHandler : SPFeatureReceiver
    {
        public static string g_strAssemblyVersion = "NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e";
        public static string g_strClassName = "NextLabs.SPEnforcer.WebSiteHandler";
        #region AddWebSiteHandlerHelper
        /// <summary>
        /// These 2 function help to add our website handler to the new created website.
        /// </summary>
        static public bool CheckWebSiteHandlerExisting
        (SPWeb web, SPEventReceiverDefinition handler)
        {
            if (web == null) return false;

            // Walk through web's all Event Receivers
            SPEventReceiverDefinitionCollection AllEventReceivers =
                web.EventReceivers;
            if (AllEventReceivers.Count == 0) return false;
            foreach (SPEventReceiverDefinition it in AllEventReceivers)
            {
                if (it.Type == handler.Type)
                {
                    if (it.Assembly == handler.Assembly &&
                        it.Class == handler.Class)
                        return true;
                }
            }
            return false;
        }

        private bool RemoveWebSiteEventeceivers(SPWeb web)
        {
            if (web == null)
                return false;

            Type webSiteHandlerType = typeof(WebSiteHandler);
            IList<Guid> eventsToBeRemoved = new List<Guid>();
            foreach (SPEventReceiverDefinition def in web.EventReceivers)
            {
                if (def.Assembly == webSiteHandlerType.Assembly.FullName
                    && def.Class == webSiteHandlerType.FullName)
                {
                    eventsToBeRemoved.Add(def.Id);
                }
            }

            if (eventsToBeRemoved.Count > 0)
            {
                foreach (Guid defId in eventsToBeRemoved)
                {
                    var def = web.EventReceivers[defId];
                    def.Delete();
                }
                web.Update();
            }

            return true;
        }

        private bool AddWebSiteEventReceiver(SPWeb web, bool isSite)
        {
            // add WebDeleting/WebMoving to the web
            if (web == null) return false;
            SPEventReceiverDefinition WebDeleting = web.EventReceivers.Add();
            WebDeleting.Assembly =  g_strAssemblyVersion;
            WebDeleting.Class = g_strClassName;
            WebDeleting.SequenceNumber = 20000;

            WebDeleting.Name = "WebDeletingEventReceiver";
            WebDeleting.Type = SPEventReceiverType.WebDeleting;
            if (!CheckWebSiteHandlerExisting(web, WebDeleting))
            {
                WebDeleting.Update();
            }

            SPEventReceiverDefinition WebMoving = web.EventReceivers.Add();
            WebMoving.Assembly = g_strAssemblyVersion;
            WebMoving.Class = g_strClassName;
            WebMoving.SequenceNumber = 20000;

            WebMoving.Name = "WebMovingEventReceiver";
            WebMoving.Type = SPEventReceiverType.WebMoving;
            if (!CheckWebSiteHandlerExisting(web, WebMoving))
            {
                WebMoving.Update();
            }

            // add SiteDeleting to the site
            if (isSite)
            {
                SPEventReceiverDefinition SiteDeleting = web.EventReceivers.Add();
                SiteDeleting.Assembly = g_strAssemblyVersion;
                SiteDeleting.Class = g_strClassName;
                SiteDeleting.SequenceNumber = 20000;

                SiteDeleting.Name = "SiteDeletingEventReceiver";
                SiteDeleting.Type = SPEventReceiverType.SiteDeleting;
                if (!CheckWebSiteHandlerExisting(web, SiteDeleting))
                {
                    SiteDeleting.Update();
                }
            }
            return true;
            // TODO: Make sure we have add them sucessfully!
        }
        #endregion

        public override void FeatureActivated
        (SPFeatureReceiverProperties properties)
        {
            try
            {
                SPWeb web = (SPWeb)properties.Feature.Parent;
                if (web != null)
                {
                    bool bRootWeb = (web.Site.RootWeb == web);
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite site = new SPSite(web.Url))
                        {
                            using (SPWeb openWeb = site.OpenWeb())
                            {
                                AddWebSiteEventReceiver(web, bRootWeb);
                            }
                        }
                    });
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FeatureActivated AddWebSiteEventReceiver:", null, ex);
            }
            // add and activate our item and list event handlers
        }

        #region OtherFeatureEvents
        /// <summary>
        /// Occurs when a Feature is Deactivating/Installed/Uninstalling.
        /// </summary>
        public override void FeatureDeactivating
        (SPFeatureReceiverProperties properties)
        {
            try
            {
                SPWeb web = (SPWeb)properties.Feature.Parent;
                if (web != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite site = new SPSite(web.Url))
                        {
                            using (SPWeb openWeb = site.OpenWeb())
                            {
                                RemoveWebSiteEventeceivers(web);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FeatureDeactivating RemoveWebSiteEventeceivers:", null, ex);
            }
        }

        public override void FeatureInstalled
        (SPFeatureReceiverProperties properties)
        {
            // Do nothing
        }

        public override void FeatureUninstalling
        (SPFeatureReceiverProperties properties)
        {
            // Do nothing
        }
        #endregion
    }
}
