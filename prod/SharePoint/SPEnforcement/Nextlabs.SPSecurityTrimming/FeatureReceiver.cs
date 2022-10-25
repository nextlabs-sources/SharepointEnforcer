using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Reflection;
using System.Diagnostics;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class FeatureReceiver : SPFeatureReceiver
    {
        // Summary:
        //     Occurs after a Feature is activated.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPFeatureReceiverProperties object that represents
        //     the properties of the event.
        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            try
            {
                // Force to reconstruct layouts.sitemap.xml
                SPFarm.Local.Services.GetValue<SPWebService>().ApplyApplicationContentToLocalServer();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FeatureActivated:", null, ex);
            }
        }

        //
        // Summary:
        //     Occurs when a Feature is deactivated.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPFeatureReceiverProperties object that represents
        //     the properties of the event.
        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
        }

        //
        // Summary:
        //     Occurs after a Feature is installed.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPFeatureReceiverProperties object that represents
        //     the properties of the event.
        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
        }

        //
        // Summary:
        //     Occurs when a Feature is uninstalled.
        //
        // Parameters:
        //   properties:
        //     An Microsoft.SharePoint.SPFeatureReceiverProperties object that represents
        //     the properties of the event.
        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
        }
    }
}
