using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace NextLabs.Diagnostic.RegCategory
{
    public class RegistrationFeatureReceiver : SPFeatureReceiver
    {
        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPWebService parentService = properties.Feature.Parent as SPWebService;

            // Register a new NextlabsDiagnosticsManager
            if (parentService != null)
            {
                parentService.Farm.Services.Add(new NextlabsDiagnosticsManager());
            }
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            SPWebService parentService = properties.Feature.Parent as SPWebService;

            if (parentService == null)
                return;

            // Remove any SandboxDiagnosticsManager registered on farm
            foreach (SPService service in parentService.Farm.Services)
            {
                if (service is NextlabsDiagnosticsManager)
                {
                    service.Delete();
                    continue;
                }
            }
        }

        public override void FeatureInstalled(SPFeatureReceiverProperties properties) { }

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties) { }
    }
}
