using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
using Nextlabs.SPSecurityTrimming;
namespace Nextlabs.SPE.Console
{
    public class SPSecurityTrimmingCommand : ISpeAdminCommand
    {
        enum SstCommand
        {
            SstCommandUnknown = 0,
            SstCommandEnable,
            SstCommandDisable,
            SstCommandInstall,
            SstCommandUninstall,
            SstCommandClearCache
        }

        //NextLabs.Entitlement.wsp Guid 
        private const string NEXTLABSWSPGUID = "6c15412b-290c-49ac-bd38-9b0ad852973b";

        private string Url;
        private SstCommand Command;
        private bool ListTrimming;

        public SPSecurityTrimmingCommand()
        {
            Url = "";
            Command = SstCommand.SstCommandUnknown;
            ListTrimming = false;
        }

        public string GetHelpString(string feature)
        {
            string help = "";

            help = "\nCE_SPAdmin.exe -o securitytrimming {{-enable [-l] | -disable [-l] | -install | -uninstall} [-url <Site Collection URL>] | -clearcache}\n";
            help += "\nOptions: \n";
            help += "\t -enable [-url <Site Collection URL>]: Enable security trimming for site collection(s).\n";
            help += "\t -disable [-url <Site Collection URL>]: Disable security trimming for site collection(s).\n";

            help += "\t -enable -l -url <Site Collection URL>: Enable list trimming for site collection.\n";
            help += "\t -disable -l -url <Site Collection URL>: Disable list trimming for site collection.\n";

            help += "\t -install: This command should be called after security trimming is installed.\n";
            help += "\t -uninstall: This command should be called before security trimming is uninstalled.\n";

            help += "\t -clearcache: Clear evaluation cache list.\n";

            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            Url = keyValues["-url"];

            bool bBadOperation = false;

            try
            {
                if (keyValues.ContainsKey("-l"))
                {
                    ListTrimming = true;
                }

                if (keyValues.ContainsKey("-enable"))
                {
                    Command = SstCommand.SstCommandEnable;
                }
                else if (keyValues.ContainsKey("-disable"))
                {
                    Command = SstCommand.SstCommandDisable;
                }
                else if (keyValues.ContainsKey("-install"))
                {
                    Command = SstCommand.SstCommandInstall;
                }
                else if (keyValues.ContainsKey("-uninstall"))
                {
                    Command = SstCommand.SstCommandUninstall;
                }
                else if (keyValues.ContainsKey("-clearcache"))
                {
                    Command = SstCommand.SstCommandClearCache;
                }
                else
                {
                    bBadOperation = true;
                }

                if (!bBadOperation)
                    Process();
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception: " + exp.Message);
            }

            if (bBadOperation)
            {
                throw new InvalidOperationException("Unsupported arguments for webparttrimming operation.");
            }


        }

        protected void Process()
        {
            if (Command == SstCommand.SstCommandClearCache)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager())
                {
                    manager.ClearCache();
                }
            }
            else if (Command == SstCommand.SstCommandInstall)
            {
                Install();
            }
            else
            {
                if (!String.IsNullOrEmpty(Url))
                {
                    SPSite site = null;
                    try
                    {
                        try
                        {
                            site = new SPSite(Url);
                        }
                        catch (Exception)
                        {
                            site = null;
                        }

                        // Process full domain URL
                        if (site == null)
                        {
                            string server = "";
                            int pos1 = Url.IndexOf("//");
                            if (pos1 > 0)
                            {
                                int pos2 = Url.IndexOf('/', pos1 + 2);
                                if (pos2 > 0)
                                {
                                    server = Url.Substring(pos1 + 2, pos2 - pos1 - 2);
                                }
                                else
                                {
                                    server = Url.Substring(pos1 + 2);
                                }

                                pos1 = server.IndexOf('.');
                                if (pos1 > 0)
                                {
                                    string host = server.Substring(0, pos1);
                                    string newUrl = Url.Replace(server, host);

                                    try
                                    {
                                        site = new SPSite(newUrl);
                                    }
                                    catch (Exception)
                                    {
                                        site = null;
                                    }
                                }
                            }
                        }

                        if (site != null)
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                if (web.IsRootWeb)
                                {
                                    SetWptForSite(site);
                                }
                                else
                                {
                                    Console.WriteLine("Failed! Url " + Url + " is not a Site Collection.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Failed! Url " + Url + " is not a Site Collection.");
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (site != null)
                        {
                            site.Dispose();
                        }
                    }
                }
                else
                {
                    if (ListTrimming == false)
                        SetWptForSites();
                    else
                        Console.WriteLine("Failed! Site Collection URL must be set for List Trimming command.");

                    //Finally to remove delegate control
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager())
                    {
                        manager.RemoveDelegateControlFromPublishingConsole();
                        Console.WriteLine("Finally to remove delegate control.");
                    }
                }
                
            }
        }

        protected void SetWptForSite(SPSite site)
        {
            if (site != null && ValidateUserPermission(site))
            {
                switch (Command)
                {
                    case SstCommand.SstCommandEnable:
                        Enable(site);
                        break;
                    case SstCommand.SstCommandDisable:
                        Disable(site);
                        break;
                    case SstCommand.SstCommandUninstall:
                        Uninstall(site);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine("\nFailed! Current User is not a SiteCollection Administrator for " + site.Url);
            }
        }

        protected void SetWptForSites()
        {
            SPSolution installedSolution = SPFarm.Local.Solutions[new Guid(NEXTLABSWSPGUID)];
            if (installedSolution != null)
            {
                foreach (SPWebApplication webapp in installedSolution.DeployedWebApplications)
                {
                    if (!webapp.IsAdministrationWebApplication)
                    {
                        foreach (SPSite site in webapp.Sites)
                        {
                            SetWptForSite(site);
                        }
                    }
                }
            }
        }

        protected void Enable(SPSite site)
        {
            if (site != null)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (ListTrimming)
                    {
                        Console.WriteLine("\nStarting to enable SharePoint List Trimming for " + site.Url);
                        manager.EnableListTrimming();
                        Console.WriteLine("Finished to enable SharePoint List Trimming for " + site.Url);
                    }
                    else
                    {
                        Console.WriteLine("\nStarting to enable SharePoint Security Trimming for " + site.Url);
                        manager.EnableInPublishingConsole();
                        Console.WriteLine("Finished to enable SharePoint Security Trimming for " + site.Url);
                    }
                }
            }
        }

        protected void Disable(SPSite site)
        {
            if (site != null)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    if (ListTrimming)
                    {
                        Console.WriteLine("\nStarting to disable SharePoint List Trimming for " + site.Url);
                        manager.DisableListTrimming();
                        Console.WriteLine("Finished to disable SharePoint List Trimming for " + site.Url);
                    }
                    else
                    {
                        Console.WriteLine("\nStarting to disable SharePoint Security Trimming for " + site.Url);
                        manager.Disable();
                        Console.WriteLine("Finished to disable SharePoint Security Trimming for " + site.Url);
                    }
                }
            }
        }

        protected void Install()
        {
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager())
            {
                manager.AddDelegateControlToPublishingConsole();
            }
        }

        protected void Uninstall(SPSite site)
        {
            if (site != null)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(site))
                {
                    manager.Remove();
                }
            }
        }

        private bool ValidateUserPermission(SPSite site)
        {
            bool bAllow = false;

            using (SPWeb web = site.OpenWeb())
            {
                if (web.DoesUserHavePermissions(SPBasePermissions.ManageWeb) && web.UserIsSiteAdmin)
                {
                    bAllow = true;
                }
            }

            return bAllow;
        }
    }
}
