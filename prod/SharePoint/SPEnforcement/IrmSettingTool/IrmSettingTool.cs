using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using Microsoft.SharePoint.Administration;
using Microsoft.Win32;
namespace NextLabs.Tool
{
    public static class Console
    {
        static StreamWriter logOut = null;
        public static void SetLogFile(string logfile)
        {
            try
            {
                if (logfile != null)
                    logOut = new StreamWriter(logfile, true);
                logOut.WriteLine("Start to output information in " + DateTime.Now.ToString());
            }
            catch (IOException ioExp)
            {
                Console.WriteLine("Redirect stdout to log file failed. All output will be printed at console.\n Exception:" + ioExp.Message);
            }            
        }
        public static void WriteLine(string value)
        {
            if (logOut != null)
            {
                logOut.WriteLine(value);
            }
            System.Console.WriteLine(value);
        }

        public static void Close()
        {
            if (logOut != null)
            {
                logOut.WriteLine("End to output information in " + DateTime.Now.ToString());
                logOut.WriteLine("");
                logOut.WriteLine("");
                logOut.Flush();
                logOut.Close();
            }
        }
    }
    class IrmSettingTool
    {
        public enum CMD_TYPE
        {
            CMD_INSTALL,
            CMD_UNINSTALL,
            CMD_REG,
            CMD_UNREG,
            CMD_CHECK,
            CMD_UNKNOW
        };
        static int Main(string[] args)
        {
            string strCmdLine = "IrmSettingTool.exe ";
            int iRet = 0;
            string logFile=null;
            string url = null;
            CMD_TYPE cmdType = CMD_TYPE.CMD_UNKNOW;
            for (int i = 0; i < args.Length; i++)
            {
                strCmdLine += args[i]+" ";
                if (args[i].Equals("/logfile", StringComparison.OrdinalIgnoreCase))
                {
                    logFile = args[++i];
                    strCmdLine += args[i] + " ";
                    Console.SetLogFile(logFile);
                }
                else if (args[i].Equals("/url", StringComparison.OrdinalIgnoreCase))
                {
                    url = args[++i];
                    strCmdLine += args[i] + " ";
                }
                else if (args[i].Equals("/i", StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_INSTALL;
                else if (args[i].Equals("/u", StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_UNINSTALL;
                else if (args[i].Equals("/reg", StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_REG;
                else if (args[i].Equals("/unreg", StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_UNREG;
                else if (args[i].Equals("/check", StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_CHECK;
            }        
            if (cmdType==CMD_TYPE.CMD_UNKNOW)
            {
                Console.WriteLine("Bad command line!");
                Console.WriteLine("Usage: IrmSettingTool {/logfile filepath} /{i|u|reg|unreg|check}");
                return 1;
            }
             if (url != null)
                iRet = InstallIrmSettingsOnSiteCollection(cmdType, url);
            else
                iRet = InstallIrmSettings(cmdType);
            Console.Close();            
            return iRet;
        }
        static int InstallIrmSettingsOnSiteCollection(CMD_TYPE _CMD_TYPE, string url)
        {
            int iRet = 0;
            SPSite site;

            try
            {
                site = new SPSite(url);
            }
            catch (Exception)
            {
                Console.WriteLine("Fail to create SPSite for "+url);
                site = null;
                return 1;
            }

            SPWeb rootweb = site.OpenWeb();
            if (rootweb.IsRootWeb)
            {
                foreach (SPWeb web in site.AllWebs)
                {
                    Console.WriteLine("\t\t\tSPWeb:" + web.Url);
                    try
                    {
                        foreach (SPList list in web.Lists)
                        {
                            if (_CMD_TYPE == CMD_TYPE.CMD_INSTALL)
                            {
                                try
                                {
                                    Console.WriteLine("Install IRM Settings for " + web.Url + '/' + list.Title);
                                    list.IrmEnabled = true;
                                    list.IrmExpire = false;
                                    list.IrmReject = false;
                                    list.RootFolder.Properties["vti_irm_IrmVBA"] = 2;
                                    list.RootFolder.Properties["vti_irm_IrmDescription"] = "Nextlabs' Doc Protector";
                                    list.RootFolder.Properties["vti_irm_IrmTitle"] = "Nextlabs";

                                    list.RootFolder.Properties["vti_irm_IrmExpireDate"] = "Sun, 29 Nov 2009 13:13:52 GMT";
                                    list.RootFolder.Properties["vti_irm_IrmPrint"] = 0;
                                    list.RootFolder.Properties["vti_irm_IrmOfflineDays"] = 30;
                                    list.RootFolder.Properties["vti_irm_IrmOffline"] = 0;

                                    list.RootFolder.Update();
                                    list.Update();
                                }
                                catch (System.Exception e)
                                {
                                    Console.WriteLine("CMD_INSTALL Exception happened:" + e.Message);
                                    iRet = 1;
                                }
                            }
                            else
                            {
                                try
                                {
                                    Console.WriteLine("Uninstall IRM Settings for " + web.Url + '/' + list.Title);
                                    list.IrmEnabled = false;
                                    list.IrmExpire = false;
                                    list.IrmReject = false;
                                    list.RootFolder.Properties["vti_irm_IrmVBA"] = 0;
                                    list.RootFolder.Properties["vti_irm_IrmDescription"] = "";
                                    list.RootFolder.Properties["vti_irm_IrmTitle"] = "";

                                    list.RootFolder.Properties["vti_irm_IrmExpireDate"] = "Sun, 29 Nov 2009 13:13:52 GMT";
                                    list.RootFolder.Properties["vti_irm_IrmPrint"] = 0;
                                    list.RootFolder.Properties["vti_irm_IrmOfflineDays"] = 30;
                                    list.RootFolder.Properties["vti_irm_IrmOffline"] = 0;
                                    list.RootFolder.Update();
                                    list.Update();
                                }
                                catch (System.Exception e)
                                {
                                    Console.WriteLine("CMD_UNINSTALL Exception happened:" + e.Message);
                                    iRet = 1;
                                }
                            }
                        }//foreach SPList
                    }
                    catch (System.Exception expList)
                    {
                        Console.WriteLine("CMD_UNINSTALL Exception happened:" + expList.Message);
                        iRet = 1;
                    }
                }//foreach SPWeb
            }
            else
            {
                System.Console.WriteLine("Failed! Url " + url + " is not a Site Collection.");
            }

            rootweb.Dispose();
            site.Dispose();
            return iRet;
        }
        static int InstallIrmSettings(CMD_TYPE _CMD_TYPE)
        {
            int iRet = 0;
            if (_CMD_TYPE == CMD_TYPE.CMD_INSTALL)
            {
                Console.WriteLine("Start to install IrmSettings for farm " + SPFarm.Local.Name);
            }
            else if (_CMD_TYPE == CMD_TYPE.CMD_REG)
            {
                Console.WriteLine("Start to Register IrmSettings for farm " + SPFarm.Local.Name);
            }
            else if (_CMD_TYPE == CMD_TYPE.CMD_UNINSTALL)
            {
                Console.WriteLine("Start to uninstall IrmSettings for farm " + SPFarm.Local.Name);
            }
            else if (_CMD_TYPE == CMD_TYPE.CMD_UNREG)
            {
                Console.WriteLine("Start to UnRegister IrmSettings for farm " + SPFarm.Local.Name);
            }
            if (_CMD_TYPE == CMD_TYPE.CMD_INSTALL || _CMD_TYPE == CMD_TYPE.CMD_UNINSTALL || _CMD_TYPE == CMD_TYPE.CMD_CHECK)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    foreach (SPService service in SPFarm.Local.Services)
                    {
                        if (service is SPWebService)
                        {
                            Console.WriteLine("SPWebService:" + service.Name);
                            try
                            {
                                SPWebService webService = (SPWebService)service;
                                foreach (SPWebApplication webapp in webService.WebApplications)
                                {
                                    Console.WriteLine("\tSPWebApplication:" + webapp.Name);
                                    if (!webapp.IsAdministrationWebApplication)
                                    {
                                        try
                                        {
                                            foreach (SPSite site in webapp.Sites)
                                            {
                                                Console.WriteLine("\t\tSPSite:" + site.Url);
                                                try
                                                {
                                                    foreach (SPWeb web in site.AllWebs)
                                                    {
                                                        Console.WriteLine("\t\t\tSPWeb:" + web.Url);
                                                        try
                                                        {
                                                            foreach (SPList list in web.Lists)
                                                            {
                                                                if (_CMD_TYPE == CMD_TYPE.CMD_CHECK)
                                                                {
                                                                    Console.WriteLine("\t\t\t\tSPList:" + web.Url + '/' + list.Title);
                                                                }
                                                                else if (_CMD_TYPE == CMD_TYPE.CMD_INSTALL)
                                                                {
                                                                    try
                                                                    {
                                                                        Console.WriteLine("Install IRM Settings for " + web.Url + '/' + list.Title);
                                                                        list.IrmEnabled = true;
                                                                        list.IrmExpire = false;
                                                                        list.IrmReject = false;
                                                                        list.RootFolder.Properties["vti_irm_IrmVBA"] = 2;
                                                                        list.RootFolder.Properties["vti_irm_IrmDescription"] = "Nextlabs' Doc Protector";
                                                                        list.RootFolder.Properties["vti_irm_IrmTitle"] = "Nextlabs";

                                                                        list.RootFolder.Properties["vti_irm_IrmExpireDate"] = "Sun, 29 Nov 2009 13:13:52 GMT";
                                                                        list.RootFolder.Properties["vti_irm_IrmPrint"] = 0;
                                                                        list.RootFolder.Properties["vti_irm_IrmOfflineDays"] = 30;
                                                                        list.RootFolder.Properties["vti_irm_IrmOffline"] = 0;

                                                                        list.RootFolder.Update();
                                                                        list.Update();
                                                                    }
                                                                    catch (System.Exception e)
                                                                    {
                                                                        Console.WriteLine("CMD_INSTALL Exception happened:" + e.Message);
                                                                        iRet = 1;
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    try
                                                                    {
                                                                        Console.WriteLine("Uninstall IRM Settings for " + web.Url + '/' + list.Title);
                                                                        list.IrmEnabled = false;
                                                                        list.IrmExpire = false;
                                                                        list.IrmReject = false;
                                                                        list.RootFolder.Properties["vti_irm_IrmVBA"] = 0;
                                                                        list.RootFolder.Properties["vti_irm_IrmDescription"] = "";
                                                                        list.RootFolder.Properties["vti_irm_IrmTitle"] = "";

                                                                        list.RootFolder.Properties["vti_irm_IrmExpireDate"] = "Sun, 29 Nov 2009 13:13:52 GMT";
                                                                        list.RootFolder.Properties["vti_irm_IrmPrint"] = 0;
                                                                        list.RootFolder.Properties["vti_irm_IrmOfflineDays"] = 30;
                                                                        list.RootFolder.Properties["vti_irm_IrmOffline"] = 0;
                                                                        list.RootFolder.Update();
                                                                        list.Update();
                                                                    }
                                                                    catch (System.Exception e)
                                                                    {
                                                                        Console.WriteLine("CMD_UNINSTALL Exception happened:" + e.Message);
                                                                        iRet = 1;
                                                                    }
                                                                }
                                                            }//foreach SPList
                                                        }
                                                        catch (System.Exception expList)
                                                        {
                                                            Console.WriteLine("CMD_UNINSTALL Exception happened:" + expList.Message);
                                                            iRet = 1;
                                                        }
                                                    }//foreach SPWeb
                                                }
                                                catch (System.Exception expWeb)
                                                {
                                                    Console.WriteLine("CMD_UNINSTALL Exception happened:" + expWeb.Message);
                                                    iRet = 1;
                                                }
                                            }//foreach SPSite
                                        }
                                        catch (System.Exception expSite)
                                        {
                                            Console.WriteLine("CMD_UNINSTALL Exception happened:" + expSite.Message);
                                            iRet = 1;
                                        }
                                    }
                                }//foreach SPWebApplication
                            }
                            catch (System.Exception eWebApp)
                            {
                                Console.WriteLine("CMD_UNINSTALL Exception happened:" + eWebApp.Message);
                                iRet = 1;
                            }
                        }
                    }//foreach SPService
                });
            }
            else if (_CMD_TYPE == CMD_TYPE.CMD_REG)
            {
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        {
                            RegistryKey commonexten_key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Shared Tools\\Web Server Extensions", true);
#if SP2013
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("15.0", true);
#elif SP2010
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("14.0", true);
#else
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("16.0", true);
#endif
                            RegistryKey Software_key = extensions_key.OpenSubKey("IrmProtectors", true);                        
                            String sTagProtector = "TagDocProtector";
                            String spExtensions = "doc,dot,xls,xlt,ppt,pot,pps,docx,docm,dotx,dotm,xlsx,xlsm,xlsb,xltx,xltm,pptx,pptm,potx,potm,thmx,ppsx,ppsm,pdf,tiff,tif,nxl";
                            if (Software_key != null)
                            {
                                Software_key.SetValue("{6EC4BB1F-3F73-4799-BC98-A3DF9AE23A0B}", sTagProtector, RegistryValueKind.String);
                                RegistryKey MsoIrm_key = extensions_key.OpenSubKey("IrmProtectors\\MsoIrmProtector", true);
                                if (MsoIrm_key != null)
                                {
                                    MsoIrm_key.SetValue("Extensions", "", RegistryValueKind.String);
                                    MsoIrm_key.Close();
                                }
                                RegistryKey OfcIrm_key = extensions_key.OpenSubKey("IrmProtectors\\OfcIrmProtector", true);
                                if (OfcIrm_key != null)
                                {
                                    OfcIrm_key.SetValue("Extensions", "xps,xlam", RegistryValueKind.String);
                                    OfcIrm_key.Close();
                                }
                                RegistryKey PdfIrm_key = extensions_key.OpenSubKey("IrmProtectors\\PdfIrmProtector", true);
                                if (PdfIrm_key != null)
                                {
                                    PdfIrm_key.SetValue("Extensions", "", RegistryValueKind.String);
                                    PdfIrm_key.Close();
                                }
                                Software_key.Close();
                            }
                            RegistryKey IRM_key = extensions_key.OpenSubKey("IrmProtectors\\TagDocProtector", true);
                            if (IRM_key == null)
                            {
                                IRM_key = extensions_key.CreateSubKey("IrmProtectors\\TagDocProtector");
                            }
                            if (IRM_key != null)
                            {
                                IRM_key.SetValue("Extensions", spExtensions, RegistryValueKind.String);
                                IRM_key.SetValue("Product", sTagProtector, RegistryValueKind.String);
                                IRM_key.SetValue("Version", "1", RegistryValueKind.String);
                                IRM_key.Close();
                            }
                        }
                    });
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("CMD_REG Exception happened:" + e.Message);
                    iRet = 1;
                }
            }
            else if (_CMD_TYPE == CMD_TYPE.CMD_UNREG)
            {
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        {
                            RegistryKey commonexten_key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Shared Tools\\Web Server Extensions", true);
#if SP2013
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("15.0", true);
#elif SP2010
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("14.0", true);
#else
                            RegistryKey extensions_key = commonexten_key.OpenSubKey("16.0", true);
#endif
                            RegistryKey Software_key = extensions_key.OpenSubKey("IrmProtectors", true);
                            if (Software_key != null)
                            {
                                try
                                {
                                    Software_key.DeleteValue("{6EC4BB1F-3F73-4799-BC98-A3DF9AE23A0B}");
                                }
                                catch { }
                                RegistryKey MsoIrm_key = extensions_key.OpenSubKey("IrmProtectors\\MsoIrmProtector", true);
                                if (MsoIrm_key != null)
                                {
                                    MsoIrm_key.SetValue("Extensions", "doc,dot,xls,xlt,xla,ppt,pot,pps", RegistryValueKind.String);
                                    MsoIrm_key.Close();
                                }
                                RegistryKey OfcIrm_key = extensions_key.OpenSubKey("IrmProtectors\\OfcIrmProtector", true);
                                if (OfcIrm_key != null)
                                {
                                    OfcIrm_key.SetValue("Extensions", "xps,docx,docm,dotx,dotm,xlsx,xlsm,xlsb,xltx,xltm,xlam,pptx,pptm,potx,potm,thmx,ppsx,ppsm", RegistryValueKind.String);
                                    OfcIrm_key.Close();
                                }
                                RegistryKey PdfIrm_key = extensions_key.OpenSubKey("IrmProtectors\\PdfIrmProtector", true);
                                if (PdfIrm_key != null)
                                {
                                    PdfIrm_key.SetValue("Extensions", "PDF", RegistryValueKind.String);
                                    PdfIrm_key.Close();
                                }
                                RegistryKey IRM_key = extensions_key.OpenSubKey("IrmProtectors\\TagDocProtector", false);
                                if (IRM_key != null)
                                {
                                    extensions_key.DeleteSubKey("IrmProtectors\\TagDocProtector");
                                }
                                Software_key.Close();
                            }
                        }
                    });
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("CMD_UNREG Exception happened:" + e.Message);
                    iRet = 1;
                }
            }

            if (_CMD_TYPE == CMD_TYPE.CMD_INSTALL)
                Console.WriteLine("Complete to install IrmSettings for farm " + SPFarm.Local.Name);
            else if (_CMD_TYPE == CMD_TYPE.CMD_UNINSTALL)
                Console.WriteLine("Complete to uninstall IrmSettings for farm " + SPFarm.Local.Name);
            else if (_CMD_TYPE == CMD_TYPE.CMD_REG)
                Console.WriteLine("Complete to Register IrmSettings for farm " + SPFarm.Local.Name);
            else if (_CMD_TYPE == CMD_TYPE.CMD_UNREG)
                Console.WriteLine("Complete to UnRegister IrmSettings for farm " + SPFarm.Local.Name);
            return iRet;
        }
    }
}
