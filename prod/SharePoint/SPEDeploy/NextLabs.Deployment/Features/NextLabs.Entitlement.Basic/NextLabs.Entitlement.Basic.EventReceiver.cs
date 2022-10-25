using System;
using System.IO;
using System.Diagnostics;
using System.Web;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;
using NextLabs.Deployment;
using System.ServiceProcess;
using System.Threading;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.Solution.Features.NextLabsEntitlementBasic
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("a03ae9bd-600c-418a-bb57-b77135c6afc1")]
    public class NextLabsEntitlementBasicEventReceiver : SPFeatureReceiver
    {
#if SP2013
        private const string DOCICON_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\15\\TEMPLATE\\XML\\";
        private const string BASESITESTAPLING_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\15\\TEMPLATE\\FEATURES\\BaseSiteStapling\\";
        private const string viewlsts_path = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\15\\TEMPLATE\\LAYOUTS\\";
        private const string POWERSHELLPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\PowerShell.exe";
        private const string SP2013SHELLCONFIG = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\CONFIG\POWERSHELL\Registration\sharepoint.ps1 ";
        private const string NLEVENTRECEIVERFEATUREPATH = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\TEMPLATE\FEATURES\NextLabs.Entitlement.EventReceiver ";
#elif SP2016 || SP2019
        private const string DOCICON_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\16\\TEMPLATE\\XML\\";
        private const string BASESITESTAPLING_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\16\\TEMPLATE\\FEATURES\\BaseSiteStapling\\";
        private const string viewlsts_path = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\16\\TEMPLATE\\LAYOUTS\\";
        private const string POWERSHELLPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\PowerShell.exe";
        private const string SP2013SHELLCONFIG = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\CONFIG\POWERSHELL\Registration\sharepoint.ps1 ";
        private const string NLEVENTRECEIVERFEATUREPATH = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\TEMPLATE\FEATURES\NextLabs.Entitlement.EventReceiver ";
#else
        private const string DOCICON_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\14\\TEMPLATE\\XML\\";
        private const string BASESITESTAPLING_PATH = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\14\\TEMPLATE\\FEATURES\\BaseSiteStapling\\";
        private const string viewlsts_path = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\14\\TEMPLATE\\LAYOUTS\\";
#endif
        private const string cWebConfigModificationOwner = "NextLabs.HttpEnforcer.HttpEnforcerModule";

        CommonLib utility = new CommonLib();

        private int EnableSP2010JobUpdate()
        {
            try
            {
                SPWebService myService = SPWebService.ContentService;
                myService.RemoteAdministratorAccessDenied = false;
                myService.Update();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during EnableSP2010JobUpdate:", null, ex);
                return 6;
            }
            return 0;
        }

        private int DisableSP2010JobUpdate()
        {
            try
            {
                SPWebService myService = SPWebService.ContentService;
                myService.RemoteAdministratorAccessDenied = true;
                myService.Update();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DisableSP2010JobUpdate:", null, ex);
                return 7;
            }
            return 0;
        }

        SPWebConfigModification spWebConfigModification = new SPWebConfigModification()
        {
            // The owner of the web.config modification, useful for removing a
            // group of modifications
            Owner = cWebConfigModificationOwner,
            // Make sure that the name is a unique XPath selector for the element
            // we are adding. This name is used for removing the element
            Name = "add[@name=\"NextLabs.HttpEnforcer.HttpEnforcerModule\"]",
            // We are going to add a new XML node to web.config
            Type = SPWebConfigModification.SPWebConfigModificationType.EnsureChildNode,
            // The XPath to the location of the parent node in web.config
            Path = "configuration/system.webServer/modules",
            // Sequence is important if there are multiple equal nodes that
            // can't be identified with an XPath expression
            Sequence = 0,
            // The XML to insert as child node, make sure that used names match the Name selector
            Value = "<add name=\"NextLabs.HttpEnforcer.HttpEnforcerModule\" type=\"NextLabs.HttpEnforcer.HttpEnforcerModule, NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e\" />"
        };


        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPWebApplication webApp = properties.Feature.Parent as SPWebApplication;
            if (webApp != null)
            {
                try
                {
                    bool IsUpdateButton = false;
                    if (Progress.GetLastLine(webApp.Name).Equals("Flag:RunFormUpdateButton"))
                    {
                        IsUpdateButton = true;
                    }
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        webApp.WebConfigModifications.Add(spWebConfigModification);
                        // Commit modification additions to the specified web application
                        webApp.Update();
                        if (IsUpdateButton)
                        {
                            Progress.WriteLog(webApp.Name, "7| Updating web config for " + webApp.Name);
                        }
                        // Push modifications through the farm
                        foreach (SPJobDefinition job in webApp.JobDefinitions)
                        {
                            if (job.Status.Equals(SPObjectStatus.Online) && job.Name.Equals("Application Server Administration Service Timer Job"))
                            {
                                Thread.Sleep(3000);
                            }
                        }
                        webApp.Farm.Services.GetValue<SPWebService>().ApplyWebConfigModifications();
                        if (IsUpdateButton)
                        {
                            Progress.WriteLog(webApp.Name, "8| Web config updated");
                        }
                    });
                    // Register a new NextlabsDiagnosticsManager
                    SPWebService parentService = webApp.WebService;
                    if (parentService != null)
                    {
#warning Below line Need Delete
                        //parentService.Farm.Services.Add(new NextlabsDiagnosticsManager());
                    }
                    if (IsUpdateButton)
                    {
                        Progress.WriteLog(webApp.Name, "9|Start to activate NextLabs.Entitlement.EventReceiver");
                        Progress.WriteLog(webApp.Name, "Flag:RunFormUpdateButton");
                    }
                    //recycle the application pool after modify the web.config.
                    HttpRuntime.UnloadAppDomain();
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during FeatureActivated:", null, ex);
                    throw ex;
                }
            }
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            SPWebApplication webApp = properties.Feature.Parent as SPWebApplication;
            if (webApp != null)
            {
                try
                {
                    bool IsUpdateButton = false;
                    if (Progress.GetLastLine(webApp.Name).Equals("Flag:RunFormUpdateButton"))
                    {
                        IsUpdateButton = true;
                    }
                    if (IsUpdateButton)
                    {
                        Progress.WriteLog(webApp.Name, "7| Updating web config for " + webApp.Name);
                    }
                    Collection<SPWebConfigModification> modificationCollection = webApp.WebConfigModifications;
                    Collection<SPWebConfigModification> removeCollection = new Collection<SPWebConfigModification>();
                    int count = modificationCollection.Count;
                    for (int i = 0; i < count; i++)
                    {
                        SPWebConfigModification modification = modificationCollection[i];
                        if (modification.Owner == cWebConfigModificationOwner)
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
                            webApp.WebConfigModifications.Remove(modificationItem);
                        }
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            // Commit modification removals to the specified web application
                            webApp.Update();
                            if (IsUpdateButton)
                            {
                                Progress.WriteLog(webApp.Name, "8| Web config updated");
                            }
                            // Push modifications through the farm
                            webApp.Farm.Services.GetValue<SPWebService>().ApplyWebConfigModifications();
                        });
                    }
                    // Remove any SandboxDiagnosticsManager registered on farm
                    SPWebService parentService = webApp.WebService;
                    if (parentService == null)
                    {
                        return;
                    }
#warning Below line Need Delete
                    //foreach (SPService service in parentService.Farm.Services)
                    //{
                    //    if (service is NextlabsDiagnosticsManager)
                    //    {
                    //        service.Delete();
                    //        continue;
                    //    }
                    //}
                    if (IsUpdateButton)
                    {
                        Progress.WriteLog(webApp.Name, "9|Start to deactivate NextLabs.Entitlement.EventReceiver");
                        Progress.WriteLog(webApp.Name, "Flag:RunFormUpdateButton");
                    }
                    //recycle the application pool after modify the web.config.
                    HttpRuntime.UnloadAppDomain();
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Warn, "Exception during FeatureDeactivating:", null, ex);
                    throw ex;
                }
            }
        }

        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            try
            {
                EnableSP2010JobUpdate();
                string SPEinstallPath = utility.GetSPEIntalledPath();
                string path = "";
                string args = "";
                int result = 0;

                //Running commandline to install WebPartTrimmingPages
                path = SPEinstallPath + "bin\\CE_SPAdmin.exe";
                args = "-o securitytrimming -install";
                utility.ExecuteCommand(path, args, ref result);
#if SP2013
                utility.Prepare14FeatureFiles();
#endif
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during FeatureInstalled:", null, ex);
            }
        }

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            string SPEinstallPath = null;
            string path = "";
            string args = "";
            int result = 0;

            try
            {
                SPEinstallPath = utility.GetSPEIntalledPath();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during FeatureUninstalling GetSPEIntalledPath:", null, ex);
            }
            //Running commandline to uninstall WebPartTrimmingPages
            path = SPEinstallPath + "bin\\CE_SPAdmin.exe";
            args = "-o securitytrimming -uninstall";
            utility.ExecuteCommand(path, args, ref result);
			//Remove nextlabs farm properties.
            SPFarm farm = SPFarm.Local;
            object[] keyArray = new object[farm.Properties.Keys.Count];
            farm.Properties.Keys.CopyTo(keyArray, 0);
            foreach (object key in keyArray)
            {
                string name = key.ToString();
                if (name.Contains("Nextlabs"))
                {
                    try
                    {
                        farm.Properties.Remove(name);
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Error, "Exception during FeatureUninstalling:", null, ex);
                    }
                }
            }
            farm.Update();

            //Remove nextlabs webapp properties
            try
            {
                utility.ClearNLFeatureStatus();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during FeatureUninstalling:", null, ex);
            }

            DisableSP2010JobUpdate();
        }
    }
}
