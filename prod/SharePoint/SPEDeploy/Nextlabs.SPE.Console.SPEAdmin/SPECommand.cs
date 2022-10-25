using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
using System.Collections;
namespace Nextlabs.SPE.Console
{

    public class GETPROPCommand : ISpeAdminCommand
    {
        private string _targe = null;
        private string _pfn = null;
        public GETPROPCommand()
        {
        }

        public string GetHelpString(string feature)
        {
            string help = "";
            help = "\nCE_SPAdmin.exe -o getproperty -url siteurl -pn propname";
            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            try
            {
                _targe = keyValues["-url"];
                _pfn = keyValues["-pn"];
            }
            catch
            {

            }
            Process();
        }

        protected void Process()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                SPSite _site = null;
                SPWeb _web = null;
                try
                {
                    using (SPSite _site1 = new SPSite(_targe))
                    {
                        using (SPWeb _web1 = _site1.OpenWeb())
                        {
                            _site = _site1;
                            _web = _web1;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("New SPSite failed:" + e.Message);
                }
                if (_site == null)
                {
                    foreach (SPService service in SPFarm.Local.Services)
                    {
                        if (service is SPWebService)
                        {
                            SPWebService webService = (SPWebService)service;
                            foreach (SPWebApplication webapp in webService.WebApplications)
                            {
                                if (!webapp.IsAdministrationWebApplication)
                                {
                                    foreach (SPSite site in webapp.Sites)
                                    {
                                        if (site.Url.Equals(_targe))
                                            _site = site;
                                    }
                                }
                            }
                        }
                    }
                }
                if (_web == null)
                    _web = _site.RootWeb;
                if (!string.IsNullOrEmpty(_pfn))
                {
                    foreach (DictionaryEntry de in _web.AllProperties)
                    {
                        if (_pfn.Equals(de.Key.ToString()))
                        {
                            Console.WriteLine("AllProperties Name:" + de.Key.ToString());
                            Console.WriteLine("AllProperties Value:" + de.Value.ToString());
                            break;
                        }
                    }
                }
                else
                {
                    foreach (DictionaryEntry de in _web.AllProperties)
                    {
                        Console.WriteLine("AllProperties Name:" + de.Key.ToString());
                        Console.WriteLine("AllProperties Value:" + de.Value.ToString());
                    }
                }
            });
        }
    }

    public class PROPBAGCommand : ISpeAdminCommand
    {
        private string _targe = null;
        private string _pfn = null;
        private string _pfv = null;
        enum PropBagCommand
        {
            PropBagCommandAddPropBags = 0,
        }

        public PROPBAGCommand()
        {
        }

        public string GetHelpString(string feature)
        {
            string help = "";
            help = "\nCE_SPAdmin.exe -o setproperty -url siteurl -pn propname -pv propvalue";
            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            _targe = keyValues["-url"];
            _pfn = keyValues["-pn"];
            _pfv = keyValues["-pv"];
            Process();
        }

        protected void Process()
        {
            AddPropBags();
        }

        private void AddPropBags()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                SPSite _site = null;
                SPWeb _web = null;
                try
                {
                    using (SPSite _site1 = new SPSite(_targe))
                    {
                        using (SPWeb _web1 = _site1.OpenWeb())
                        {
                            _site = _site1;
                            _web = _web1;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("New SPSite failed:" + e.Message);
                }
                if (_site == null)
                {
                    foreach (SPService service in SPFarm.Local.Services)
                    {
                        if (service is SPWebService)
                        {
                            SPWebService webService = (SPWebService)service;
                            foreach (SPWebApplication webapp in webService.WebApplications)
                            {
                                if (!webapp.IsAdministrationWebApplication)
                                {
                                    foreach (SPSite site in webapp.Sites)
                                    {
                                        if (site.Url.Equals(_targe))
                                            _site = site;
                                    }
                                }
                            }
                        }
                    }
                }
                if (_web == null)
                    _web = _site.RootWeb;
                _web.AllowUnsafeUpdates = true;
                if (_web.AllProperties.Contains(_pfn))
                {
                    _web.AllProperties[_pfn] = _pfv;
                }
                else
                {
                    _web.AllProperties.Add(_pfn, _pfv);
                }
                _web.Update();
                _web.AllowUnsafeUpdates = false;
                Console.WriteLine("Add propertybad successfully! ");
            });
        }
    }

    public class SPECommand : ISpeAdminCommand
    {
        SPEventReceiverType[] receiverTypes = new SPEventReceiverType[]{
                    SPEventReceiverType.ItemAdding, 
                    SPEventReceiverType.ItemUpdating, 
                    SPEventReceiverType.ItemDeleting, 
                    SPEventReceiverType.ItemCheckingIn, 
                    SPEventReceiverType.ItemCheckingOut, 
                    SPEventReceiverType.ItemUncheckingOut,
                    SPEventReceiverType.ItemAttachmentAdding,
                    SPEventReceiverType.ItemAttachmentDeleting,
                    SPEventReceiverType.ItemFileMoving,
                    SPEventReceiverType.ItemAdded, 
                    SPEventReceiverType.ItemUpdated, 
                    SPEventReceiverType.ItemDeleted, 
                    SPEventReceiverType.ItemCheckedIn, 
                    SPEventReceiverType.ItemCheckedOut, 
                    SPEventReceiverType.ItemUncheckedOut,
                    SPEventReceiverType.ItemAttachmentAdded,
                    SPEventReceiverType.ItemAttachmentDeleted,
                    SPEventReceiverType.ItemFileMoved,
                    SPEventReceiverType.ItemFileConverted};

        SPEventReceiverType[] listreceiverTypes = new SPEventReceiverType[]{
                    SPEventReceiverType.FieldAdding, 
                    SPEventReceiverType.FieldUpdating, 
                    SPEventReceiverType.FieldDeleting};

        SPEventReceiverType[] add_receiverTypes = new SPEventReceiverType[]{
                    SPEventReceiverType.ItemAdding, 
                    SPEventReceiverType.ItemUpdating, 
                    SPEventReceiverType.ItemDeleting, 
                    SPEventReceiverType.ItemAttachmentAdding,
                    SPEventReceiverType.ItemFileMoving,
                    SPEventReceiverType.ItemAdded, 
                    SPEventReceiverType.ItemAttachmentAdded};

        SPEventReceiverType[] add_listreceiverTypes = new SPEventReceiverType[]{
                    SPEventReceiverType.FieldAdding, 
                    SPEventReceiverType.FieldUpdating};


        SPEventReceiverType[] webreceiverTypes = new SPEventReceiverType[]{
                    SPEventReceiverType.SiteDeleting, 
                    SPEventReceiverType.WebDeleting, 
                    SPEventReceiverType.WebMoving};


        string[] add_receiverNames = new string[]{
                    "ItemAddingEventHandler", 
                    "ItemUpdatingEventHandler", 
                    "ItemDeletingEventHandler", 
                    "ItemAttachmentAddingEventHandler",
                    "ItemFileMovingEventHandler",
                    "ItemAddedEventHandler", 
                    "ItemAttachmentAddedEventHandler"};

        string[] add_listreceiverNames = new string[]{
                    "FieldAddingEventHandler",                  
                    "FieldUpdatingEventHandler"};

        string[] webreceiverNames = new string[]{
                    "SiteDeletingEventReceiver",                  
                    "WebDeletingEventReceiver",                  
                    "WebMovingEventReceiver",};

        static string assemblyFullName = "NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e";
        string assemblyClassName = "NextLabs.SPEnforcer.ItemHandler";
        string listassemblyClassName = "NextLabs.SPEnforcer.ListHandler";
        string webassemblyClassName = "NextLabs.SPEnforcer.WebSiteHandler";
        string _target = null;
        string _webtarget = null;
        string _webapptarget = null;
        int _listtemplate = 0;
        bool _checkfeature = false;
        bool _checkevents = false;
        string _feature = null;
        bool _allevents = false;
        enum SpeCommand
        {
            SpeCommandUnknown = 0,
            SpeCommandDeactivate,
            SpeCommandactivate,
            SpeCommandClearSPEEvents,
            SpeCommandInstallSPEEvents,
            SpeCommandPrintSPEEvents
        }

        private SpeCommand Command;

        public SPECommand()
        {
            Command = SpeCommand.SpeCommandUnknown;
        }

        public string GetHelpString(string feature)
        {
            string help = "";

            help = "\nCE_SPAdmin.exe -o spe -deactivatefeature [-checkfeature] [-url url | -site url | -web name] [-featureid featureid]";
            help += "\nCE_SPAdmin.exe -o spe -activatefeature [-checkfeature] [-url url | -site url | -web name] [-featureid featureid]";
            help += "\nCE_SPAdmin.exe -o spe -clearspeevents [-checkevents] [-url url | -site url | -web name] [-listtemplate listid][-all]";
            help += "\nCE_SPAdmin.exe -o spe -installspeevents [-checkevents] [-url url | -site url | -web name] [-all]";
            help += "\nCE_SPAdmin.exe -o spe -checkfeature [-url url | -site url | -web name] [-featureid featureid]";
            help += "\nCE_SPAdmin.exe -o spe -checkevents  [-url url | -site url | -web name] [-all]";
            help += "\nCE_SPAdmin.exe -o spe -printevents  -url url";
            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            bool bBadOperation = false;
            _feature = keyValues["-featureid"];
            System.Guid guid = System.Guid.Empty;
            if (_feature != null)
                guid = new System.Guid(_feature);
            _target = keyValues["-url"];
            _webtarget = keyValues["-site"];
            _webapptarget = keyValues["-web"];
            string _slisttemplate = keyValues["-listtemplate"];
            if (_slisttemplate != null)
                _listtemplate = Convert.ToInt32(_slisttemplate);
            try
            {
                if (keyValues.ContainsKey("-checkfeature"))
                {
                    _checkfeature = true;
                }
                if (keyValues.ContainsKey("-checkevents"))
                {
                    _checkevents = true;
                }
                if (keyValues.ContainsKey("-all"))
                {
                    _allevents = true;
                }
                if (keyValues.ContainsKey("-deactivatefeature"))
                {
                    Command = SpeCommand.SpeCommandDeactivate;
                }
                else if (keyValues.ContainsKey("-activatefeature"))
                {
                    Command = SpeCommand.SpeCommandactivate;
                }
                else if (keyValues.ContainsKey("-clearspeevents"))
                {
                    Command = SpeCommand.SpeCommandClearSPEEvents;
                }
                else if (keyValues.ContainsKey("-installspeevents"))
                {
                    Command = SpeCommand.SpeCommandInstallSPEEvents;
                }
                else if (keyValues.ContainsKey("-printevents") && _target != null)
                {
                    Command = SpeCommand.SpeCommandPrintSPEEvents;
                }
                else
                {
                    bBadOperation = true;
                }

                if (!bBadOperation || _checkfeature || _checkevents)
                    Process();
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception: " + exp.Message);
            }

            if (bBadOperation && !_checkfeature && !_checkevents)
            {
                throw new InvalidOperationException("Unsupported arguments for spe operation.");
            }
        }

        protected void Process()
        {
            switch (Command)
            {
                case SpeCommand.SpeCommandDeactivate:
                    DeactivateSPE(true);
                    break;
                case SpeCommand.SpeCommandactivate:
                    ActivateSPE();
                    break;
                case SpeCommand.SpeCommandClearSPEEvents:
                    ClearSPEEvents(true);
                    break;
                case SpeCommand.SpeCommandInstallSPEEvents:
                    InstallSPEEvents();
                    break;
                case SpeCommand.SpeCommandPrintSPEEvents:
                    PrintSPEEvents();
                    break;
                default:
                    {
                        if (_checkfeature)
                            DeactivateSPE(false);
                        else if (_checkevents)
                            ClearSPEEvents(false);
                    }
                    break;
            }
        }

        private void CheckFeatures(SPWeb rootWeb, bool action, bool install)
        {
            System.Guid _guid = Guid.Empty;
            if (_feature != null)
                _guid = new System.Guid(_feature);
            //NextLabs.Entitlement.EventReceiver Feature ID
            System.Guid guid = new System.Guid("4f6fd05e-b392-418b-9dbf-b0fb92f12271");

            if (_checkfeature)
            {
                if (install)
                {
                    if (_feature != null)
                    {
                        if (rootWeb.Features[_guid] == null)
                            Console.WriteLine("SPWeb : " + rootWeb.Url + " has no feature :" + _feature);
                    }
                    else
                    {
                        if (rootWeb.Features[guid] == null)
                            Console.WriteLine("SPWeb : " + rootWeb.Url + " has no NextLabs.Entitlement.EventReceiver Feature");
                    }
                }
                else
                {
                    if (_feature != null)
                    {
                        if (rootWeb.Features[_guid] != null)
                            Console.WriteLine("SPWeb : " + rootWeb.Url + " has feature : " + _feature);
                        else
                        {
                            if (!action)
                                Console.WriteLine("SPWeb : " + rootWeb.Url + " has no feature : " + _feature);
                        }
                    }
                    else
                    {
                        if (rootWeb.Features[guid] != null)
                            Console.WriteLine("SPWeb : " + rootWeb.Url + " has NextLabs.Entitlement.EventReceiver feature");
                        else
                        {
                            if (!action)
                                Console.WriteLine("SPWeb : " + rootWeb.Url + " has no NextLabs.Entitlement.EventReceiver Feature");
                        }
                    }
                }
            }
        }

        private void CheckEvents(SPWeb rootWeb, bool action, bool install)
        {
            try
            {
                if (_checkevents)
                {
                    for (int i = 0; i < webreceiverTypes.Length; i++)
                    {
                        if (install)
                        {
                            if (!CheckWebReceiverExisting(rootWeb, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                            {
                                Console.WriteLine("SPWeb in " + rootWeb.Url + "does not have event: " + webreceiverTypes[i].ToString());
                            }
                        }
                        else
                        {
                            if (CheckWebReceiverExisting(rootWeb, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                            {
                                Console.WriteLine("SPWeb in " + rootWeb.Url + "has event: " + webreceiverTypes[i].ToString());
                            }
                            else if (!action)
                                Console.WriteLine("SPWeb in " + rootWeb.Url + "does not have event: " + webreceiverTypes[i].ToString());

                        }
                    }
                    for (int j = 0; j < rootWeb.Lists.Count; j++)
                    {
                        if (_allevents)
                        {
                            for (int i = 0; i < receiverTypes.Length; i++)
                            {
                                if (install)
                                {
                                    if (!CheckListReceiverExisting(rootWeb.Lists[j], receiverTypes[i], assemblyFullName, assemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + receiverTypes[i].ToString());
                                    }
                                }
                                else
                                {
                                    if (CheckListReceiverExisting(rootWeb.Lists[j], receiverTypes[i], assemblyFullName, assemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "has event: " + receiverTypes[i].ToString());
                                    }
                                    else if (!action)
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + receiverTypes[i].ToString());
                                }
                            }
                            for (int i = 0; i < listreceiverTypes.Length; i++)
                            {
                                if (install)
                                {
                                    if (!CheckListReceiverExisting(rootWeb.Lists[j], listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + listreceiverTypes[i].ToString());
                                    }
                                }
                                else
                                {
                                    if (CheckListReceiverExisting(rootWeb.Lists[j], listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "has event: " + listreceiverTypes[i].ToString());
                                    }
                                    else if (!action)
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + listreceiverTypes[i].ToString());
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < add_receiverTypes.Length; i++)
                            {
                                if (install)
                                {
                                    if (!CheckListReceiverExisting(rootWeb.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + add_receiverTypes[i].ToString());
                                    }
                                }
                                else
                                {
                                    if (CheckListReceiverExisting(rootWeb.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "has event: " + add_receiverTypes[i].ToString());
                                    }
                                    else if (!action)
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + add_receiverTypes[i].ToString());
                                }
                            }
                            for (int i = 0; i < add_listreceiverTypes.Length; i++)
                            {
                                if (install)
                                {
                                    if (!CheckListReceiverExisting(rootWeb.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + add_listreceiverTypes[i].ToString());
                                    }
                                }
                                else
                                {
                                    if (CheckListReceiverExisting(rootWeb.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                    {
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "has event: " + add_listreceiverTypes[i].ToString());
                                    }
                                    else if (!action)
                                        Console.WriteLine("SPList in " + rootWeb.Url + "\\" + rootWeb.Lists[j].Title + "does not have event: " + add_listreceiverTypes[i].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }



        private void DeactivateSPE(bool action)
        {
            System.Guid _guid = Guid.Empty;
            if (_feature != null)
                _guid = new System.Guid(_feature);
            //NextLabs.Entitlement.EventReceiver Feature ID
            System.Guid guid = new System.Guid("4f6fd05e-b392-418b-9dbf-b0fb92f12271");
            if (!string.IsNullOrEmpty(_webtarget))
            {
                using (SPSite _site = new SPSite(_webtarget))
                {
                    SPWeb rootWeb = _site.OpenWeb();
                    if (rootWeb != null)
                    {
                        if (action)
                        {
                            try
                            {
                                if (_feature != null)
                                {
                                    if (rootWeb.Features[_guid] != null)
                                    {
                                        rootWeb.Features.Remove(_guid, true);
                                    }
                                }
                                else
                                {
                                    if (rootWeb.Features[guid] != null)
                                    {
                                        rootWeb.Features.Remove(guid, true);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Deactivate feature Exception : " + e.Message + " in " + rootWeb.Url);
                            }
                        }
                        CheckFeatures(rootWeb, action, false);
                        rootWeb.Dispose();
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_target))
            {
                using (SPSite _site = new SPSite(_target))
                {
                    foreach (SPWeb web in _site.AllWebs)
                    {
                        using (web)
                        {
                            if (action)
                            {
                                try
                                {
                                    if (_feature != null)
                                    {
                                        if (web.Features[_guid] != null)
                                        {
                                            web.Features.Remove(_guid, true);
                                        }
                                    }
                                    else
                                    {
                                        if (web.Features[guid] != null)
                                        {
                                            web.Features.Remove(guid, true);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Deactivate feature Exception : " + e.Message + " in " + web.Url);
                                }
                            }
                            CheckFeatures(web, action, false);
                        }
                    }
                }
            }
            else
            {
                bool _findweb = false;
                foreach (SPService service in SPFarm.Local.Services)
                {
                    if (service is SPWebService)
                    {
                        SPWebService webService = (SPWebService)service;
                        foreach (SPWebApplication webapp in webService.WebApplications)
                        {
                            if (_webapptarget != null)
                            {
                                if (!webapp.DisplayName.Equals(_webapptarget, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                else
                                    _findweb = true;
                            }
                            foreach (SPSite site in webapp.Sites)
                            {
                                using(site)
                                {
                                    foreach (SPWeb web in site.AllWebs)
                                    {
                                        using(web)
                                        {
                                            if (action)
                                            {
                                                try
                                                {
                                                    if (_feature != null)
                                                    {
                                                        if (web.Features[_guid] != null)
                                                        {
                                                            web.Features.Remove(_guid, true);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (web.Features[guid] != null)
                                                        {
                                                            web.Features.Remove(guid, true);
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine("Deactivate feature Exception : " + e.Message + " in " + web.Url);
                                                }
                                            }
                                            CheckFeatures(web, action, false);
                                        }
                                    }
                                }                              
                            }
                        }
                    }
                }
                if (_webapptarget != null && !_findweb)
                {
                    Console.WriteLine("Can not find specified web application : " + _webapptarget);
                    return;
                }
            }

            Console.WriteLine("Operation completed successfully.");
        }

        private void ActivateSPE()
        {
            System.Guid _guid = Guid.Empty;
            if (_feature != null)
                _guid = new System.Guid(_feature);
            // NextLabs.Entitlement.EventReceiver Feature ID
            System.Guid guid = new System.Guid("4f6fd05e-b392-418b-9dbf-b0fb92f12271");
            if (!string.IsNullOrEmpty(_webtarget))
            {
                using (SPSite _site = new SPSite(_webtarget))
                {
                    using(SPWeb rootWeb = _site.OpenWeb())
                    {
                        if (rootWeb != null)
                        {
                            try
                            {
                                SPSecurity.RunWithElevatedPrivileges(delegate()
                                {
                                    rootWeb.AllowUnsafeUpdates = true;
                                    if (_feature != null)
                                    {
                                        if (rootWeb.Features[_guid] == null)
                                        {
                                            rootWeb.Features.Add(_guid, true);
                                            rootWeb.Update();
                                        }
                                    }
                                    else
                                    {
                                        if (rootWeb.Features[guid] == null)
                                        {
                                            rootWeb.Features.Add(guid, true);
                                            rootWeb.Update();
                                        }
                                    }
                                    rootWeb.AllowUnsafeUpdates = false;
                                    CheckFeatures(rootWeb, false, true);
                                });
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Activate feature Exception : " + e.Message + " in " + rootWeb.Url);
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_target))
            {
                using (SPSite _site = new SPSite(_target))
                {
                    foreach (SPWeb web in _site.AllWebs)
                    {
                        try
                        {
                            SPSecurity.RunWithElevatedPrivileges(delegate()
                            {
                                web.AllowUnsafeUpdates = true;
                                if (_feature != null)
                                {
                                    if (web.Features[_guid] == null)
                                    {
                                        web.Features.Add(_guid, true);
                                        web.Update();
                                    }
                                }
                                else
                                {
                                    if (web.Features[guid] == null)
                                    {
                                        web.Features.Add(guid, true);
                                        web.Update();
                                    }
                                }
                                web.AllowUnsafeUpdates = false;
                                CheckFeatures(web, false, true);
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Activate feature Exception : " + e.Message + " in " + web.Url);
                        }
                        if(web != null)
                        web.Dispose();
                    }
                }
            }
            else
            {
                bool _findweb = false;
                foreach (SPService service in SPFarm.Local.Services)
                {
                    if (service is SPWebService)
                    {
                        SPWebService webService = (SPWebService)service;
                        foreach (SPWebApplication webapp in webService.WebApplications)
                        {
                            if (_webapptarget != null)
                            {
                                if (!webapp.DisplayName.Equals(_webapptarget, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                else
                                    _findweb = true;
                            }
                            foreach (SPSite site in webapp.Sites)
                            {
                                try
                                {
                                    foreach (SPWeb web in site.AllWebs)
                                    {
                                        try
                                        {
                                            SPSecurity.RunWithElevatedPrivileges(delegate()
                                            {
                                                web.AllowUnsafeUpdates = true;
                                                if (_feature != null)
                                                {
                                                    if (web.Features[_guid] == null)
                                                    {
                                                        web.Features.Add(_guid, true);
                                                        web.Update();
                                                    }
                                                }
                                                else
                                                {
                                                    if (web.Features[guid] == null)
                                                    {
                                                        web.Features.Add(guid, true);
                                                        web.Update();
                                                    }
                                                }
                                                web.AllowUnsafeUpdates = false;
                                                CheckFeatures(web, false, true);
                                            });
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Activate feature Exception : " + e.Message + " in " + web.Url);
                                        }
                                        finally
                                        {
                                            if(web != null)
                                            web.Dispose();
                                        }
                                    }
                                }
                                catch
                                { 
                                }
                                finally
                                {
                                    if(site != null)
                                    site.Dispose();
                                }
                            }
                        }
                    }

                }
                if (_webapptarget != null && !_findweb)
                {
                    Console.WriteLine("Can not find specified web application : " + _webapptarget);
                    return;
                }
            }

            Console.WriteLine("Operation completed successfully.");
        }


        private void AddWebReceiver(SPWeb spweb, SPEventReceiverType ReceiverType, string ReceiverName, string Assembly, string Class)
        {
            // The caller should ensure splist is NOT null
            SPEventReceiverDefinitionCollection eventReceivers = spweb.EventReceivers;
            SPEventReceiverDefinition receiverDefinition = eventReceivers.Add();
            receiverDefinition.Name = ReceiverName;
            receiverDefinition.Type = ReceiverType;
            receiverDefinition.SequenceNumber = 20000;
            receiverDefinition.Assembly = Assembly;
            receiverDefinition.Class = Class;
            receiverDefinition.Update();
        }

        private void AddListReceiver(SPList splist, SPEventReceiverType ReceiverType, string ReceiverName, string Assembly, string Class, bool sync)
        {
            // The caller should ensure splist is NOT null
            SPEventReceiverDefinitionCollection eventReceivers = splist.EventReceivers;
            SPEventReceiverDefinition receiverDefinition = eventReceivers.Add();
            receiverDefinition.Name = ReceiverName;
            receiverDefinition.Type = ReceiverType;
            if (sync)
                receiverDefinition.Synchronization = SPEventReceiverSynchronization.Synchronous;
            receiverDefinition.SequenceNumber = 20000;
            receiverDefinition.Assembly = Assembly;
            receiverDefinition.Class = Class;
            receiverDefinition.Update();
        }

        private bool CheckWebReceiverExisting(SPWeb spweb, SPEventReceiverType ReceiverType, string Assembly, string Class)
        {
            SPEventReceiverDefinitionCollection AllEventReceivers = spweb.EventReceivers;
            if (AllEventReceivers.Count == 0)
                return false;
            foreach (SPEventReceiverDefinition it in AllEventReceivers)
            {
                if (it.Type == ReceiverType)
                {
                    if (it.Assembly.Equals(Assembly, StringComparison.OrdinalIgnoreCase) &&
                        it.Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private bool CheckListReceiverExisting(SPList splist, SPEventReceiverType ReceiverType, string Assembly, string Class)
        {
            // The caller should ensure splist is NOT null

            // Walk through list's all Event Receivers
            SPEventReceiverDefinitionCollection AllEventReceivers = splist.EventReceivers;
            if (AllEventReceivers.Count == 0)
                return false;
            foreach (SPEventReceiverDefinition it in AllEventReceivers)
            {
                if (it.Type == ReceiverType)
                {
                    if (it.Assembly.Equals(Assembly, StringComparison.OrdinalIgnoreCase) &&
                        it.Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void PrintSPEEvents()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                if (!string.IsNullOrEmpty(_target))
                {
                    using (SPSite _site = new SPSite(_target))
                    {
                        using (SPWeb rootWeb = _site.OpenWeb())
                        {
                            SPList _list = null;
                            try
                            {
                                _list = rootWeb.GetList(_target);
                            }
                            catch
                            {
                            }
                            if (_list != null)
                            {
                                Console.WriteLine("SPList : " + rootWeb.Url + "/" + _list.Title + " has events:");
                                foreach (SPEventReceiverDefinition it in _list.EventReceivers)
                                {
                                    Console.WriteLine("Event : " + it.Name);
                                }
                            }
                            else
                            {
                                Console.WriteLine("SPWeb : " + rootWeb.Url + " has events:");
                                foreach (SPEventReceiverDefinition it in rootWeb.EventReceivers)
                                {
                                    Console.WriteLine("Event : " + it.Name);
                                }
                            }

                        }
                    }
                }
            });
        }

        private void InstallSPEEvents()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                if (!string.IsNullOrEmpty(_webtarget))
                {
                    using (SPSite _site = new SPSite(_webtarget))
                    {
                        using(SPWeb rootWeb = _site.OpenWeb())
                        {
                            if (rootWeb != null)
                            {
                                Console.WriteLine("Start to Install events in " + rootWeb.Url);
                                try
                                {
                                    rootWeb.AllowUnsafeUpdates = true;
                                    try
                                    {
                                        for (int i = 0; i < webreceiverTypes.Length; i++)
                                        {
                                            if (!CheckWebReceiverExisting(rootWeb, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                                            {
                                                AddWebReceiver(rootWeb, webreceiverTypes[i], webreceiverNames[i], assemblyFullName, webassemblyClassName);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Add events Exception : " + e.Message + " in " + rootWeb.Url);
                                    }
                                    for (int j = 0; j < rootWeb.Lists.Count; j++)
                                    {
                                        try
                                        {
                                            for (int i = 0; i < add_receiverTypes.Length; i++)
                                            {
                                                if (!CheckListReceiverExisting(rootWeb.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                                {
                                                    AddListReceiver(rootWeb.Lists[j], add_receiverTypes[i], add_receiverNames[i], assemblyFullName, assemblyClassName, true);
                                                    rootWeb.Lists[j].Update();
                                                }
                                            }
                                            for (int i = 0; i < add_listreceiverTypes.Length; i++)
                                            {
                                                if (!CheckListReceiverExisting(rootWeb.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                                {
                                                    AddListReceiver(rootWeb.Lists[j], add_listreceiverTypes[i], add_listreceiverNames[i], assemblyFullName, listassemblyClassName, false);
                                                    rootWeb.Lists[j].Update();
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Install list events Exception : " + e.Message + " in " + rootWeb.Url + "/" + rootWeb.Lists[j].Title);
                                        }
                                    }
                                    rootWeb.Update();
                                    rootWeb.AllowUnsafeUpdates = false;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Install events Exception : " + e.Message + " in " + rootWeb.Url);
                                }
                                CheckEvents(rootWeb, true, true);

                                foreach (SPWeb web in rootWeb.Webs)
                                {
                                    Console.WriteLine("Start to Install events in " + web.Url);
                                    try
                                    {
                                        web.AllowUnsafeUpdates = true;
                                        try
                                        {
                                            for (int i = 0; i < webreceiverTypes.Length; i++)
                                            {
                                                if (!CheckWebReceiverExisting(web, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                                                {
                                                    AddWebReceiver(web, webreceiverTypes[i], webreceiverNames[i], assemblyFullName, webassemblyClassName);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Add events Exception : " + e.Message + " in " + web.Url);
                                        }
                                        for (int j = 0; j < web.Lists.Count; j++)
                                        {
                                            try
                                            {
                                                for (int i = 0; i < add_receiverTypes.Length; i++)
                                                {
                                                    if (!CheckListReceiverExisting(web.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                                    {
                                                        AddListReceiver(web.Lists[j], add_receiverTypes[i], add_receiverNames[i], assemblyFullName, assemblyClassName, true);
                                                        web.Lists[j].Update();
                                                    }
                                                }
                                                for (int i = 0; i < add_listreceiverTypes.Length; i++)
                                                {
                                                    if (!CheckListReceiverExisting(web.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                                    {
                                                        AddListReceiver(web.Lists[j], add_listreceiverTypes[i], add_listreceiverNames[i], assemblyFullName, listassemblyClassName, false);
                                                        web.Lists[j].Update();
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("Install list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                            }
                                        }
                                        web.Update();
                                        web.AllowUnsafeUpdates = false;
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Install events Exception : " + e.Message + " in " + web.Url);
                                    }
                                    CheckEvents(web, true, true);
                                    web.Dispose();
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(_target))
                {
                    using (SPSite _site = new SPSite(_target))
                    {
                        foreach (SPWeb web in _site.AllWebs)
                        {
                            Console.WriteLine("Start to Install events in " + web.Url);
                            try
                            {
                                web.AllowUnsafeUpdates = true;
                                try
                                {
                                    for (int i = 0; i < webreceiverTypes.Length; i++)
                                    {
                                        if (!CheckWebReceiverExisting(web, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                                        {
                                            AddWebReceiver(web, webreceiverTypes[i], webreceiverNames[i], assemblyFullName, webassemblyClassName);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Add events Exception : " + e.Message + " in " + web.Url);
                                }
                                for (int j = 0; j < web.Lists.Count; j++)
                                {
                                    try
                                    {
                                        for (int i = 0; i < add_receiverTypes.Length; i++)
                                        {
                                            if (!CheckListReceiverExisting(web.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                            {
                                                AddListReceiver(web.Lists[j], add_receiverTypes[i], add_receiverNames[i], assemblyFullName, assemblyClassName, true);
                                                web.Lists[j].Update();
                                            }
                                        }
                                        for (int i = 0; i < add_listreceiverTypes.Length; i++)
                                        {
                                            if (!CheckListReceiverExisting(web.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                            {
                                                AddListReceiver(web.Lists[j], add_listreceiverTypes[i], add_listreceiverNames[i], assemblyFullName, listassemblyClassName, false);
                                                web.Lists[j].Update();
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Install list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                    }
                                }
                                web.Update();
                                web.AllowUnsafeUpdates = false;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Install events Exception : " + e.Message + " in " + web.Url);
                            }
                            CheckEvents(web, true, true);
                            web.Dispose();
                        }
                    }
                }
                else
                {
                    bool _findweb = false;
                    foreach (SPService service in SPFarm.Local.Services)
                    {
                        if (service is SPWebService)
                        {
                            SPWebService webService = (SPWebService)service;
                            foreach (SPWebApplication webapp in webService.WebApplications)
                            {
                                if (_webapptarget != null)
                                {
                                    if (!webapp.DisplayName.Equals(_webapptarget, StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    else
                                        _findweb = true;
                                }
                                if (!webapp.IsAdministrationWebApplication)
                                {
                                    foreach (SPSite site in webapp.Sites)
                                    {
                                        try
                                        {
                                            foreach (SPWeb web in site.AllWebs)
                                            {
                                                Console.WriteLine("Start to Install events in " + web.Url);
                                                try
                                                {
                                                    web.AllowUnsafeUpdates = true;
                                                    try
                                                    {
                                                        for (int i = 0; i < webreceiverTypes.Length; i++)
                                                        {
                                                            if (!CheckWebReceiverExisting(web, webreceiverTypes[i], assemblyFullName, webassemblyClassName))
                                                            {
                                                                AddWebReceiver(web, webreceiverTypes[i], webreceiverNames[i], assemblyFullName, webassemblyClassName);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine("Add events Exception : " + e.Message + " in " + web.Url);
                                                    }
                                                    for (int j = 0; j < web.Lists.Count; j++)
                                                    {
                                                        try
                                                        {
                                                            for (int i = 0; i < add_receiverTypes.Length; i++)
                                                            {
                                                                if (!CheckListReceiverExisting(web.Lists[j], add_receiverTypes[i], assemblyFullName, assemblyClassName))
                                                                {
                                                                    AddListReceiver(web.Lists[j], add_receiverTypes[i], add_receiverNames[i], assemblyFullName, assemblyClassName, true);
                                                                    web.Lists[j].Update();
                                                                }
                                                            }
                                                            for (int i = 0; i < add_listreceiverTypes.Length; i++)
                                                            {
                                                                if (!CheckListReceiverExisting(web.Lists[j], add_listreceiverTypes[i], assemblyFullName, listassemblyClassName))
                                                                {
                                                                    AddListReceiver(web.Lists[j], add_listreceiverTypes[i], add_listreceiverNames[i], assemblyFullName, listassemblyClassName, false);
                                                                    web.Lists[j].Update();
                                                                }
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Console.WriteLine("Install list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                                        }
                                                    }
                                                    web.Update();
                                                    web.AllowUnsafeUpdates = false;
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine("Install events Exception : " + e.Message + " in " + web.Url);
                                                }
                                                CheckEvents(web, true, true);
                                                web.Dispose();
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        finally
                                        {
                                            if (site != null)
                                                site.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (_webapptarget != null && !_findweb)
                        Console.WriteLine("Can not find specified web application : " + _webapptarget);
                }
            });
        }

        private void ClearSPEEvents(bool action)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                if (!string.IsNullOrEmpty(_webtarget))
                {
                    using (SPSite _site = new SPSite(_webtarget))
                    {
                        using(SPWeb rootWeb = _site.OpenWeb())
                        {
                            if (rootWeb != null)
                            {
                                if (action)
                                {
                                    try
                                    {
                                        Console.WriteLine("Start to Unistall events in " + rootWeb.Url);
                                        rootWeb.AllowUnsafeUpdates = true;
                                        try
                                        {
                                            for (int i = 0; i < webreceiverTypes.Length; i++)
                                            {
                                                DeleteWebReceiverExisting(rootWeb, webreceiverTypes[i], webassemblyClassName);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Delete events Exception : " + e.Message + " in " + rootWeb.Url);
                                        }
                                        for (int j = 0; j < rootWeb.Lists.Count; j++)
                                        {
                                            if (_listtemplate != 0 && rootWeb.Lists[j].BaseTemplate != (SPListTemplateType)_listtemplate)
                                                continue;
                                            Console.WriteLine("Unistall Event Hanlder for " + rootWeb.Url + '/' + rootWeb.Lists[j].Title);
                                            try
                                            {
                                                for (int i = 0; i < receiverTypes.Length; i++)
                                                {
                                                    DeleteListReceiverExisting(rootWeb.Lists[j], receiverTypes[i], assemblyClassName);
                                                    rootWeb.Lists[j].Update();
                                                }
                                                for (int i = 0; i < listreceiverTypes.Length; i++)
                                                {
                                                    DeleteListReceiverExisting(rootWeb.Lists[j], listreceiverTypes[i], listassemblyClassName);
                                                    rootWeb.Lists[j].Update();
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("Clear list events Exception : " + e.Message + " in " + rootWeb.Url + "/" + rootWeb.Lists[j].Title);
                                            }
                                        }
                                        rootWeb.Update();
                                        rootWeb.AllowUnsafeUpdates = false;
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Clear events Exception : " + e.Message + " in " + rootWeb.Url);
                                    }
                                }
                                CheckEvents(rootWeb, action, false);
                                foreach (SPWeb web in rootWeb.Webs)
                                {
                                    if (action)
                                    {
                                        try
                                        {
                                            Console.WriteLine("Start to Unistall events in " + web.Url);
                                            web.AllowUnsafeUpdates = true;
                                            try
                                            {
                                                for (int i = 0; i < webreceiverTypes.Length; i++)
                                                {
                                                    DeleteWebReceiverExisting(web, webreceiverTypes[i], webassemblyClassName);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("Delete events Exception : " + e.Message + " in " + web.Url);
                                            }
                                            for (int j = 0; j < web.Lists.Count; j++)
                                            {
                                                if (_listtemplate != 0 && web.Lists[j].BaseTemplate != (SPListTemplateType)_listtemplate)
                                                    continue;
                                                Console.WriteLine("Unistall Event Hanlder for " + web.Url + '/' + web.Lists[j].Title);
                                                try
                                                {
                                                    for (int i = 0; i < receiverTypes.Length; i++)
                                                    {
                                                        DeleteListReceiverExisting(web.Lists[j], receiverTypes[i], assemblyClassName);
                                                        web.Lists[j].Update();
                                                    }
                                                    for (int i = 0; i < listreceiverTypes.Length; i++)
                                                    {
                                                        DeleteListReceiverExisting(web.Lists[j], listreceiverTypes[i], listassemblyClassName);
                                                        web.Lists[j].Update();
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine("Clear list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                                }
                                            }
                                            web.Update();
                                            web.AllowUnsafeUpdates = false;
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Clear events Exception : " + e.Message + " in " + web.Url);
                                        }
                                    }
                                    CheckEvents(web, action, false);
                                    web.Dispose();
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(_target))
                {
                    using (SPSite _site = new SPSite(_target))
                    {
                        foreach (SPWeb web in _site.AllWebs)
                        {
                            if (action)
                            {
                                try
                                {
                                    Console.WriteLine("Start to Unistall events in " + web.Url);
                                    web.AllowUnsafeUpdates = true;
                                    try
                                    {
                                        for (int i = 0; i < webreceiverTypes.Length; i++)
                                        {
                                            DeleteWebReceiverExisting(web, webreceiverTypes[i], webassemblyClassName);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Delete events Exception : " + e.Message + " in " + web.Url);
                                    }
                                    for (int j = 0; j < web.Lists.Count; j++)
                                    {
                                        if (_listtemplate != 0 && web.Lists[j].BaseTemplate != (SPListTemplateType)_listtemplate)
                                            continue;
                                        Console.WriteLine("Unistall Event Hanlder for " + web.Url + '/' + web.Lists[j].Title);
                                        try
                                        {
                                            for (int i = 0; i < receiverTypes.Length; i++)
                                            {
                                                DeleteListReceiverExisting(web.Lists[j], receiverTypes[i], assemblyClassName);
                                                web.Lists[j].Update();
                                            }
                                            for (int i = 0; i < listreceiverTypes.Length; i++)
                                            {
                                                DeleteListReceiverExisting(web.Lists[j], listreceiverTypes[i], listassemblyClassName);
                                                web.Lists[j].Update();
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Clear list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                        }
                                    }
                                    web.Update();
                                    web.AllowUnsafeUpdates = false;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Clear events Exception : " + e.Message + " in " + web.Url);
                                }
                            }
                            CheckEvents(web, action, false);
                            web.Dispose();
                        }
                    }
                }
                else
                {
                    bool _findweb = false;
                    foreach (SPService service in SPFarm.Local.Services)
                    {
                        if (service is SPWebService)
                        {
                            SPWebService webService = (SPWebService)service;
                            foreach (SPWebApplication webapp in webService.WebApplications)
                            {
                                if (_webapptarget != null)
                                {
                                    if (!webapp.DisplayName.Equals(_webapptarget, StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    else
                                        _findweb = true;
                                }
                                if (!webapp.IsAdministrationWebApplication)
                                {
                                    foreach (SPSite site in webapp.Sites)
                                    {
                                        try
                                        {
                                            foreach (SPWeb web in site.AllWebs)
                                            {
                                                if (action)
                                                {
                                                    try
                                                    {
                                                        web.AllowUnsafeUpdates = true;
                                                        try
                                                        {
                                                            for (int i = 0; i < webreceiverTypes.Length; i++)
                                                            {
                                                                DeleteWebReceiverExisting(web, webreceiverTypes[i], webassemblyClassName);
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Console.WriteLine("Delete events Exception : " + e.Message + " in " + web.Url);
                                                        }
                                                        for (int j = 0; j < web.Lists.Count; j++)
                                                        {
                                                            if (_listtemplate != 0 && web.Lists[j].BaseTemplate != (SPListTemplateType)_listtemplate)
                                                                continue;
                                                            Console.WriteLine("Unistall Event Hanlder for " + web.Url + '/' + web.Lists[j].Title);
                                                            try
                                                            {
                                                                for (int i = 0; i < receiverTypes.Length; i++)
                                                                {
                                                                    DeleteListReceiverExisting(web.Lists[j], receiverTypes[i], assemblyClassName);
                                                                    web.Lists[j].Update();
                                                                }
                                                                for (int i = 0; i < listreceiverTypes.Length; i++)
                                                                {
                                                                    DeleteListReceiverExisting(web.Lists[j], listreceiverTypes[i], listassemblyClassName);
                                                                    web.Lists[j].Update();
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Console.WriteLine("Clear list events Exception : " + e.Message + " in " + web.Url + "/" + web.Lists[j].Title);
                                                            }
                                                        }
                                                        web.Update();
                                                        web.AllowUnsafeUpdates = false;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine("Clear events Exception : " + e.Message + " in " + web.Url);
                                                    }
                                                }
                                                CheckEvents(web, action, false);
                                                web.Dispose();
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
                                }
                            }
                        }
                    }
                    if (_webapptarget != null && !_findweb)
                        Console.WriteLine("Can not find specified web application : " + _webapptarget);
                }
            });
        }

        private void DeleteWebReceiverExisting(SPWeb spweb, SPEventReceiverType ReceiverType, string Class)
        {
            SPEventReceiverDefinitionCollection AllEventReceivers = spweb.EventReceivers;
            if (AllEventReceivers.Count == 0)
                return;
            for (int i = AllEventReceivers.Count - 1; i >= 0; i--)
            {
                if (AllEventReceivers[i].Type == ReceiverType)
                {
                    if (AllEventReceivers[i].Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                    {
                        AllEventReceivers[i].Delete();
                    }
                }
            }
        }

        private void DeleteListReceiverExisting(SPList splist, SPEventReceiverType ReceiverType, string Class)
        {
            // The caller should ensure splist is NOT null

            // Walk through list's all Event Receivers
            SPEventReceiverDefinitionCollection AllEventReceivers = splist.EventReceivers;
            if (AllEventReceivers.Count == 0)
                return;
            for (int i = AllEventReceivers.Count - 1; i >= 0; i--)
            {
                if (AllEventReceivers[i].Type == ReceiverType)
                {
                    if (AllEventReceivers[i].Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                    {
                        AllEventReceivers[i].Delete();
                    }
                }
            }
        }
    }
}
