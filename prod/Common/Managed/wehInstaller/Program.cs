using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace NextLabs.SPSecurityEnforcer.wehInstaller 
{
    class Program
    {
        /*
         * This is the main method
         */
        static void Main(string[] args)
        {
            try
            {
                // no commandline args provided
                if (args.Length == 0)
                {
                    Console.WriteLine("Invalid number of command line arguments");
                    Console.WriteLine("Example: wehInstaller -help");
                    return;
                }

                // -help option 
                string strCommand = args[Helper.INDEX_COMMAND].ToLower();
                if (strCommand == Helper.HELP)
                {
                    Console.WriteLine("wehInstaller.exe can be used to enumerate site event receivers, add site event receivers and delete site event receivers");
                    Console.WriteLine();
                    Console.WriteLine("The following commands are valid: ");
                    Console.WriteLine("-help (displays information about usage of wehInstaller tool)");
                    Console.WriteLine("-enum (enumerates current event receivers for SharePoint site)");
                    Console.WriteLine("-add (adds event receiver to SharePoint site (SPWeb object))");
                    Console.WriteLine("-del (deletes event receiver from SharePoint site)");
                    Console.WriteLine("-reg (adds event receiver to all SharePoint sites (SPWeb object))");
                    Console.WriteLine("-unreg (deletes event receiver from all SharePoint sites)");
                    Console.WriteLine();
                    Console.WriteLine("Example 1: wehInstaller -help");
                    Console.WriteLine("Example 2: wehInstaller -enum -url [site url]");
                    Console.WriteLine("Example 3: wehInstaller -add -url [site url] -config [path to config file]");
                    Console.WriteLine("Example 4: wehInstaller -del -url [site url] -name [name of event receiver] ");
                    Console.WriteLine("Example 4: wehInstaller -reg -url [site url] -config [path to config file]");
                    Console.WriteLine("Example 4: wehInstaller -unreg -url [site url] -config [path to config file]");
                    Console.WriteLine();
                    return;
                }

                // wrong number of commandline args provided
                if (args.Length < 3)
                {
                    Console.WriteLine("Invalid number of command line arguments");
                    WriteHelpInfo();
                    return;
                }

                // -url should always be the second arg
                string strUrlCommand = args[Helper.INDEX_URL_COMMAND].ToLower();
                if (strUrlCommand != Helper.URL)
                {
                    Console.WriteLine("Missing site url argument: -url [site url]");
                    WriteHelpInfo();
                    return;
                }

                string strUrl = args[Helper.INDEX_URL].ToLower();

                // -enum option 
                if (strCommand == Helper.ENUM)
                {
                    EnumSiteEventReceiver(strUrl);
                    return;
                }

                // all other options should have at least 5 commandline args
                if (args.Length != 5)
                {
                    Console.WriteLine("Invalid number of command line arguments");
                    WriteHelpInfo();
                    return;
                }

                // -del option
                if (strCommand == Helper.DELETE)
                {
                    string strNameCommand = args[Helper.INDEX_NAME_COMMAND];
                    if (strNameCommand != Helper.NAME)
                    {
                        Console.WriteLine("Missing name argument");
                        WriteHelpInfo();
                        return;
                    }
                    DeleteSiteEventReceiver(strUrl, args[Helper.INDEX_NAME]);
                }

                // -add option
                if (strCommand == Helper.ADD)
                {
                    string strConfigCommand = args[Helper.INDEX_CONFIG_COMMAND].ToLower();
                    if (strConfigCommand != Helper.CONFIG)
                    {
                        Console.WriteLine("Missing config argument");
                        WriteHelpInfo();
                        return;
                    }
                    string strConfig = args[Helper.INDEX_CONFIG].ToLower();
                    AddSiteEventReceiver(strUrl, strConfig);
                }

                // -reg option
                if (strCommand == Helper.REG)
                {
                    string strConfigCommand = args[Helper.INDEX_CONFIG_COMMAND].ToLower();
                    if (strConfigCommand != Helper.CONFIG)
                    {
                        Console.WriteLine("Missing config argument");
                        WriteHelpInfo();
                        return;
                    }
                    string strConfig = args[Helper.INDEX_CONFIG].ToLower();
                    RegisterSPSiteEventReceiver(strUrl, strConfig);
                }

                // -unreg option
                if (strCommand == Helper.UNREG)
                {
                    string strConfigCommand = args[Helper.INDEX_CONFIG_COMMAND].ToLower();
                    if (strConfigCommand != Helper.CONFIG)
                    {
                        Console.WriteLine("Missing config argument");
                        WriteHelpInfo();
                        return;
                    }
                    string strConfig = args[Helper.INDEX_CONFIG].ToLower();
                    UnregisterSPSiteEventReceiver(strUrl, strConfig);
                }
            }

            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        /*
         * This method is called when the user typed in the wrong
         * arguments
         */
        private static void WriteHelpInfo()
        {
            Console.WriteLine("Type 'wehInstaller -help' for more information");
        }

        /*
         * This method adds the event receiver to the given URL site
         */
        private static void AddSiteEventReceiver(string strUrl, string strConfigPath)
        {
            ConfigManager config = new ConfigManager(strConfigPath);
            using (SPSite site = new SPSite(strUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    while (config.GetNextComponent())
                    {
                        SPEventReceiverDefinitionCollection receivers = web.EventReceivers;
                        SPEventReceiverDefinition def = receivers.Add();
                        def.Name = config.GetName();
                        def.Assembly = config.GetAssembly();
                        def.Class = config.GetClass();
                        def.Type = config.GetEventReceiverType();
                        def.SequenceNumber = config.GetSequence();
                        def.Update();    
                        
                        /*
                        Console.WriteLine(config.GetName());
                        Console.WriteLine(config.GetAssembly());
                        Console.WriteLine(config.GetClass());
                        Console.WriteLine(config.GetEventReceiverType());
                        Console.WriteLine(config.GetSequence());
                        */
                         
                        Console.WriteLine("Registration of event " + def.Name + " for url " + strUrl + " successful");
                    }
                }
            }
        }

        /*
         * This method prints out all registered event receivers
         * on the given URL site
         */
        private static void EnumSiteEventReceiver(string strUrl)
        {
            using (SPSite site = new SPSite(strUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    SPEventReceiverDefinitionCollection coll = web.EventReceivers;
                    if (coll.Count == 0)
                    {
                        Console.WriteLine("0 event receivers registered at " + strUrl);
                    }

                    foreach (SPEventReceiverDefinition def in coll)
                    {
                        Console.WriteLine("Event receiver: " + def.Name);
                    }
                }
            }
        }

        /*
         * This method deletes a event receiver from the URL site
         */
        private static void DeleteSiteEventReceiver(string strUrl, string strName)
        {
            using (SPSite site = new SPSite(strUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    SPEventReceiverDefinitionCollection coll = web.EventReceivers;
                    foreach (SPEventReceiverDefinition def in coll)
                    {
                        if (def.Name == strName)
                        {
                            def.Delete();
                            Console.WriteLine("Deleted event receiver " + strName + " from site " + strUrl);
                            break;
                        }
                    }
                }
            }
        }

        /*
         * This method deletes the event receivers on the URL 
         * from a config file
         */
        private static void DeleteSiteEventReceiverFromConfig(string strUrl, string strConfigPath)
        {
            ConfigManager config = new ConfigManager(strConfigPath);
            using (SPSite site = new SPSite(strUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    while (config.GetNextComponent())
                    {
                        SPEventReceiverDefinitionCollection coll = web.EventReceivers;
                        foreach (SPEventReceiverDefinition def in coll)
                        {
                            if (def.Name == config.GetName())
                            {
                                def.Delete();
                                Console.WriteLine("Deleted event receiver " + config.GetName() + " from site " + strUrl);
                                break;
                            }
                        } 
                    }
                }
            }
        }

        /*
         * This method registers the given even receiver to all
         * SPSites under the URL
         */
        private static void RegisterSPSiteEventReceiver(string url, string config)
        {
            SPGlobalAdmin globalAdmin = new SPGlobalAdmin();

            System.Uri uri = new System.Uri(url);
            SPVirtualServer virtualServer = globalAdmin.OpenVirtualServer(uri);
            SPSiteCollection siteCollections = virtualServer.Sites;

            foreach (SPSite site in siteCollections)
            {
                AddSiteEventReceiver(site.Url, config);
                SPWebCollection webs = site.AllWebs;
               
                try
                {
                    foreach (SPWeb web in webs)
                    {
                        RegisterSPWebEventReceiver(web, config);
                    }
                }
                catch
                {

                }
                finally
                {
                    site.Close();
                }
            }
        }

        /*
         * This method registers the given even receiver to all
         * SPWebs under the given SPWeb
         */
        private static void RegisterSPWebEventReceiver(SPWeb web, string config)
        {
            Console.WriteLine("SPWeb: " + web.Url);
            AddSiteEventReceiver(web.Url, config);
            SPWebCollection webCollection = web.Webs;
            foreach (SPWeb subWeb in webCollection)
            {
                RegisterSPWebEventReceiver(subWeb, config);
                subWeb.Close();
            }
        }

        /*
         * This method unregisters the given even receiver to all
         * SPSites under the URL
         */
        private static void UnregisterSPSiteEventReceiver(string url, string config)
        {
            SPGlobalAdmin globalAdmin = new SPGlobalAdmin();

            System.Uri uri = new System.Uri(url);
            SPVirtualServer virtualServer = globalAdmin.OpenVirtualServer(uri);
            SPSiteCollection siteCollections = virtualServer.Sites;

            foreach (SPSite site in siteCollections)
            {
                Console.WriteLine("SPSite: " + site.Url);
                DeleteSiteEventReceiverFromConfig(site.Url, config);
                SPWebCollection webs = site.AllWebs;

                try
                {
                    foreach (SPWeb web in webs)
                    {
                        UnregisterSPWebEventReceiver(web, config);
                    }
                }
                catch
                {

                }
                finally
                {
                    site.Close();
                }
            }
        }

        /*
         * This method unregisters the given even receiver to all
         * SPWebs under the given SPWeb
         */
        private static void UnregisterSPWebEventReceiver(SPWeb web, string config)
        {
            Console.WriteLine("SPWeb: " + web.Url);
            DeleteSiteEventReceiverFromConfig(web.Url, config);
            SPWebCollection webCollection = web.Webs;
            foreach (SPWeb subWeb in webCollection)
            {
                UnregisterSPWebEventReceiver(subWeb, config);
                subWeb.Close();
            }
        }
    }
}
