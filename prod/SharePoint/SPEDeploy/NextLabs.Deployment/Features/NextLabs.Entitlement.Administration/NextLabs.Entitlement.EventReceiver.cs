using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;
using System.Collections.ObjectModel;

namespace NextLabs.Deployment.Features.NextLabs.Entitlement.Administration
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("55b720ab-5062-40d2-989a-a2648f33515e")]
    public class NextLabsEntitlementEventReceiver : SPFeatureReceiver
    {
        private const string WebConfigModificationOwnerFeatureController = "NextLabs.FeatureController.aspx";
        private const string WebConfigModificationOwnerFeatureManager = "NextLabs.FeatureManager.aspx";
        SPWebConfigModification spWebConfigModificationFeatureController = new SPWebConfigModification()
        {
            // The owner of the web.config modification, useful for removing a
            // group of modifications
            Owner = WebConfigModificationOwnerFeatureController,
            // Make sure that the name is a unique XPath selector for the element
            // we are adding. This name is used for removing the element
            Name = "location[@path=\"_layouts/15/FeatureManager/FeatureController.aspx\"]",

            // We are going to add a new XML node to web.config
            Type = SPWebConfigModification.SPWebConfigModificationType.EnsureChildNode,
            // The XPath to the location of the parent node in web.config
            Path = "configuration",
            // Sequence is important if there are multiple equal nodes that
            // can't be identified with an XPath expression
            Sequence = 0,
            // The XML to insert as child node, make sure that used names match the Name selector
            Value = "<location path=\"_layouts/15/FeatureManager/FeatureController.aspx\"><system.web><httpRuntime maxRequestLength=\"2097151\" executionTimeout=\"86400\" /></system.web></location>"
        };
        SPWebConfigModification spWebConfigModificationFeatureManager = new SPWebConfigModification()
        {
            // The owner of the web.config modification, useful for removing a
            // group of modifications
            Owner = WebConfigModificationOwnerFeatureManager,
            // Make sure that the name is a unique XPath selector for the element
            // we are adding. This name is used for removing the element
            Name = "location[@path=\"_layouts/15/FeatureManager/FeatureManager.aspx\"]",

            // We are going to add a new XML node to web.config
            Type = SPWebConfigModification.SPWebConfigModificationType.EnsureChildNode,
            // The XPath to the location of the parent node in web.config
            Path = "configuration",
            // Sequence is important if there are multiple equal nodes that
            // can't be identified with an XPath expression
            Sequence = 0,
            // The XML to insert as child node, make sure that used names match the Name selector
            Value = "<location path=\"_layouts/15/FeatureManager/FeatureManager.aspx\"><system.web><httpRuntime maxRequestLength=\"2097151\" executionTimeout=\"86400\" /></system.web></location>"
        };

        // Uncomment the method below to handle the event raised after a feature has been installed.
        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            bool blIsModifyed = true;
            SPWebServiceCollection webservices = new SPWebServiceCollection(SPFarm.Local);
            foreach (SPWebService webservice in webservices)
            {
                if (!blIsModifyed)
                {
                    break;
                }
                foreach (SPWebApplication webApp in webservice.WebApplications)
                {
                    if (webApp.IsAdministrationWebApplication)
                    {
                        try
                        {
                            SPWebService.ContentService.WebApplications[webApp.Id].WebConfigModifications.Add(spWebConfigModificationFeatureController);
                            SPWebService.ContentService.WebApplications[webApp.Id].WebConfigModifications.Add(spWebConfigModificationFeatureManager);
                            SPWebService.ContentService.WebApplications[webApp.Id].Update();
                            // Push modifications through the farm
                            foreach (SPJobDefinition job in webApp.JobDefinitions)
                            {
                                if (job.Status.Equals(SPObjectStatus.Online) && job.Name.Equals("Application Server Administration Service Timer Job"))
                                {
                                    System.Threading.Thread.Sleep(3000);
                                }
                            }
                            SPWebService.ContentService.WebApplications[webApp.Id].WebService.ApplyWebConfigModifications();
                            blIsModifyed = true;
                            break;

                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("NextLabs.Entitlement.Administration Exception" + ex.Message);
                        }
                    }
                }
            }       
        }


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            bool blIsModifyed = true;
            SPWebServiceCollection webservices = new SPWebServiceCollection(SPFarm.Local);
            foreach (SPWebService webservice in webservices)
            {
                if (!blIsModifyed)
                {
                    break;
                }
                foreach (SPWebApplication webApp in webservice.WebApplications)
                {
                    if (webApp.IsAdministrationWebApplication)
                    {
                        try
                        {
                            Collection<SPWebConfigModification> modificationCollection = SPWebService.ContentService.WebApplications[webApp.Id].WebConfigModifications;
                            Collection<SPWebConfigModification> removeCollection = new Collection<SPWebConfigModification>();
                            int count = modificationCollection.Count;
                            for (int i = 0; i < count; i++)
                            {
                                SPWebConfigModification modification = modificationCollection[i];
                                if (modification.Name == spWebConfigModificationFeatureManager.Name || modification.Name == spWebConfigModificationFeatureController.Name)
                                {
                                    // collect modifications to delete
                                    removeCollection.Add(modification);
                                }
                            }
                            // now delete the modifications from the web application
                            if (removeCollection.Count > 0)
                            {
                                foreach (SPWebConfigModification modificationItem in removeCollection)
                                {
                                    SPWebService.ContentService.WebApplications[webApp.Id].WebConfigModifications.Remove(modificationItem);
                                }

                                // Commit modification removals to the specified web application
                                SPWebService.ContentService.WebApplications[webApp.Id].Update();
                                // Push modifications through the farm
                                SPWebService.ContentService.WebApplications[webApp.Id].WebService.ApplyWebConfigModifications();
                            }
                            blIsModifyed = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("NextLabs.Entitlement.Administration Exception" + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
