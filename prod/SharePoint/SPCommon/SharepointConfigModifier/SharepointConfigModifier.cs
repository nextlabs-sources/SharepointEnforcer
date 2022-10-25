using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SharepointConfigModifier
{
    class SharepointConfigModifier
    {
        public enum CMD_TYPE
        {
            CMD_ADD,
            CMD_REMOVE,
            CMD_UNKNOW
        };
        const string ADD_OPTION = "-a";
        const string REMOVE_OPTION = "-r";

        const string SITE_ONET_XML_LOCATION = "c:\\Program Files\\Common Files\\Microsoft Shared\\web server extensions\\12\\TEMPLATE\\SiteTemplates\\sts\\xml\\onet.xml";
        const string SITE_ONET_XML_LOCATION14 = "c:\\Program Files\\Common Files\\Microsoft Shared\\web server extensions\\14\\TEMPLATE\\SiteTemplates\\sts\\xml\\onet.xml";
        const string SITE_ONET_XML_LOCATION15 = "c:\\Program Files\\Common Files\\Microsoft Shared\\web server extensions\\15\\TEMPLATE\\SiteTemplates\\sts\\xml\\onet.xml";
        
        const string XMLPATH_HTTPMODULE = "/configuration/system.webServer/modules";
        const string XML_HTTPMODULE_SECURITYTRIMMING = "Nextlabs.SPSecurityTrimming.SPSecurityTrimmingModule, Nextlabs.SPSecurityTrimming, Version=3.0.0.0, Culture=neutral, PublicKeyToken=7030e9011c5eb860";
        const string XML_HTTPMODULE_SPE = "NextLabs.HttpEnforcer.HttpEnforcerModule, NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e";
        const string XML_HTTPMODULE_PLE = "NextLabs.PLE.HttpModule.PLEHttpEnforcerModule, NextLabs.PLE, Version=3.0.0.0, Culture=neutral, PublicKeyToken=72dcea101a86dcde";
        const string XML_SECURITY_SAFECONTROL = "Nextlabs.SPSecurityTrimming, Version=3.0.0.0, Culture=neutral, PublicKeyToken=7030e9011c5eb860";

        string GetTheSiteOnetXMLPath()
        {
            string strSiteOnetXMLPath;
            if (System.IO.File.Exists(SITE_ONET_XML_LOCATION15) == true)
                strSiteOnetXMLPath = SITE_ONET_XML_LOCATION15;
            else if (System.IO.File.Exists(SITE_ONET_XML_LOCATION14) == true)
                strSiteOnetXMLPath = SITE_ONET_XML_LOCATION14;
            else
                strSiteOnetXMLPath = SITE_ONET_XML_LOCATION;
            return strSiteOnetXMLPath;
        }
        static void Main(string[] args)
        {
            string strCmdLine = "SharepointConfigModifier.exe ";
            string logFile = null;
            CMD_TYPE cmdType = CMD_TYPE.CMD_UNKNOW;
            Console.WriteLine("Modifying Sharepoint configuration files...");

            for (int i = 0; i < args.Length; i++)
            {
                strCmdLine += args[i] + " ";
                if (args[i].Equals("-logfile", StringComparison.OrdinalIgnoreCase))
                {
                    logFile = args[++i];
                    strCmdLine += args[i] + " ";
                }
                else if (args[i].Equals(ADD_OPTION, StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_ADD;
                else if (args[i].Equals(REMOVE_OPTION, StringComparison.OrdinalIgnoreCase))
                    cmdType = CMD_TYPE.CMD_REMOVE;
            }
            StreamWriter logOut = null;
            try
            {
                if (logFile != null)
                    logOut = new StreamWriter(logFile, true);
            }
            catch (IOException ioExp)
            {
                Console.WriteLine("Redirect stdout to log file failed. All output will be printed at console.\n Exception:" + ioExp.Message);
            }
            if (logOut != null)
            {
                Console.WriteLine("Output is redirected to log file  " + logFile + ".");
                Console.SetOut(logOut);
                Console.WriteLine("=============================================================================");
                Console.WriteLine("Command Line:" + strCmdLine);
            }
            try
            {
                SharepointConfigModifier configModifier = new SharepointConfigModifier();
                if (cmdType==CMD_TYPE.CMD_ADD)
                {
                    configModifier.AddEnforcerHttpModules();
                    configModifier.AddOnetDotXMLFeatures();
                }
                else
                {
                    configModifier.RemoveEnforcerHttpModules();
                    configModifier.RemoveOnetDotXMLFeatures();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to modify Sharepoint configuration files: " + exception.Message);
                Console.WriteLine(exception.StackTrace);
                System.Environment.ExitCode = 2;
            }
            if (logOut != null)
            {
                logOut.Close();
                logOut.Dispose();
                logOut = null;
            }
        }

        private void AddEnforcerHttpModules()
        {
            // Get the list of deployed Sharepoint Web Services
            SPWebServiceCollection webServiceCollection = GetSharepointWebServices();

            try
            {
                // Iterate through the Sharepoint Web Services
                foreach (SPWebService webService in webServiceCollection)
                {
                    try
                    {
                        // Iterate through the Sharepoint Web Applications
                        foreach (SPWebApplication webApplication in webService.WebApplications)
                        {
                            try
                            {
                                if (!webApplication.IsAdministrationWebApplication)
                                {
                                    foreach (KeyValuePair<SPUrlZone, SPIisSettings> nextSettingsPair in webApplication.IisSettings)
                                    {
                                        try
                                        {
                                            Console.WriteLine("the path of web.config is " + nextSettingsPair.Value.Path.FullName + "\\web.config");
                                            AddEnforcerHttpModuleToWebConfigFile(nextSettingsPair.Value.Path.FullName + "\\web.config");
                                        }
                                        catch (System.Exception expAdd)
                                        {
                                            Console.WriteLine("Exception when add http module to " + webApplication.Name + " \n" + expAdd.Message);
                                            Console.WriteLine(expAdd.StackTrace);
                                        }
                                    }
                                }
                            }
                            catch (System.Exception expNoAdmin)
                            {
                                Console.WriteLine("Exception when check non-admin Web application...\n" + expNoAdmin.Message);
                                Console.WriteLine(expNoAdmin.StackTrace);
                            }
                        }
                    }
                    catch (System.Exception expWeb)
                    {
                        Console.WriteLine("Exception when loop Web application...\n"+expWeb.Message);
                        Console.WriteLine(expWeb.StackTrace);
                    }
                }
            }
            catch (System.Exception exp)
            {
                Console.WriteLine("Exception when add Enforcer Http Modules...\n"+exp.Message);
                Console.WriteLine(exp.StackTrace);
            }
        }

        private void AddOnetDotXMLFeatures()
        {            
            XmlDocument onetDoc = new XmlDocument();
            onetDoc.PreserveWhitespace = true;
            onetDoc.Load(GetTheSiteOnetXMLPath());

            XmlNodeList webFeaturesNodeList = onetDoc.SelectNodes("/Project/Configurations/Configuration/WebFeatures");
            foreach (XmlNode webFeatureNode in webFeaturesNodeList) 
            {
                XmlElement nodeToAdd = onetDoc.CreateElement("Feature");
                XmlAttribute idAttribute = onetDoc.CreateAttribute("ID");
                idAttribute.Value = "4f6fd05e-b392-418b-9dbf-b0fb92f12271";
                nodeToAdd.Attributes.Append(idAttribute);

                webFeatureNode.AppendChild(nodeToAdd);
            }

            WriteXmlDocumentToFile(onetDoc, GetTheSiteOnetXMLPath());
        }

        private void RemoveEnforcerHttpModules()
        {
            // Get the list of deployed Sharepoint Web Services
            SPWebServiceCollection webServiceCollection = GetSharepointWebServices();
            try
            {
                // Iterate through the Sharepoint Web Services
                foreach (SPWebService webService in webServiceCollection)
                {
                    try
                    {
                        // Iterate through the Sharepoint Web Applications
                        foreach (SPWebApplication webApplication in webService.WebApplications)
                        {
                            try
                            {
                                if (!webApplication.IsAdministrationWebApplication)
                                {
                                    foreach (KeyValuePair<SPUrlZone, SPIisSettings> nextSettingsPair in webApplication.IisSettings)
                                    {
                                        try
                                        {
                                            RemoveEnforcerHttpModuleFromWebConfigFile(nextSettingsPair.Value.Path.FullName + "\\web.config");
                                        }
                                        catch (System.Exception expRemove)
                                        {
                                            Console.WriteLine("Exception when remove http module to " + webApplication.Name + " \n" + expRemove.Message);
                                        }
                                    }
                                }
                            }
                            catch (System.Exception expNoAdmin)
                            {
                                Console.WriteLine("Exception when check non-admin Web application for remove...\n" + expNoAdmin.Message);
                            }
                        }
                    }
                    catch (System.Exception expWeb)
                    {
                        Console.WriteLine("Exception when loop remove Web application...\n" + expWeb.Message);
                    }
                }
            }
            catch (System.Exception exp)
            {
                Console.WriteLine("Exception when remove Enforcer Http Modules...\n" + exp.Message);
            }
        }

        private void RemoveOnetDotXMLFeatures()
        {
            XmlDocument onetDoc = new XmlDocument();
            onetDoc.PreserveWhitespace = true;
            onetDoc.Load(GetTheSiteOnetXMLPath());

            XmlNodeList webFeaturesNodeList = onetDoc.SelectNodes("/Project/Configurations/Configuration/WebFeatures");
            foreach (XmlNode webFeatureNode in webFeaturesNodeList) 
            {
				//NextLabs.Entitlement.EventReceiver Feature ID
                XmlNode featureNode = webFeatureNode.SelectSingleNode("Feature[@ID=\"4f6fd05e-b392-418b-9dbf-b0fb92f12271\"]");
                if (featureNode != null)
                {
                    webFeatureNode.RemoveChild(featureNode);
                }
            }

            WriteXmlDocumentToFile(onetDoc, GetTheSiteOnetXMLPath());
        }

        private void AddEnforcerHttpModuleToWebConfigFile(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument webConfigDoc = new XmlDocument();
            webConfigDoc.PreserveWhitespace = true;
            webConfigDoc.Load(filePath);
            XmlNode httpModulesNode = null;

            httpModulesNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE);
            XmlElement nodeToAdd = null;
            XmlAttribute nameAttribute = null;
            XmlAttribute typeAttribute = null;

            // Add main enforcer module
            XmlNode enforcerHttpModuleNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE+"/add[@name='NextLabs.HttpEnforcer.HttpEnforcerModule']");
            if (enforcerHttpModuleNode != null)
            {
                httpModulesNode.RemoveChild(enforcerHttpModuleNode);
            }	
            nodeToAdd = webConfigDoc.CreateElement("add");
            nameAttribute = webConfigDoc.CreateAttribute("name");
            nameAttribute.Value = "NextLabs.HttpEnforcer.HttpEnforcerModule";
            nodeToAdd.Attributes.Append(nameAttribute);
            typeAttribute = webConfigDoc.CreateAttribute("type");

            typeAttribute.Value = XML_HTTPMODULE_SPE;

            nodeToAdd.Attributes.Append(typeAttribute);
            httpModulesNode.PrependChild(nodeToAdd);

            // Add security trimming safe controls (TODO change method name and refactor out to reduce this methods size)
            XmlNode safeControlsNode = webConfigDoc.SelectSingleNode("/configuration/SharePoint/SafeControls");
            XmlNode trimmingSafeControlsNode = webConfigDoc.SelectSingleNode("/configuration/SharePoint/SafeControls/SafeControl[@Namespace='Nextlabs.SPSecurityTrimming']");
            if (trimmingSafeControlsNode != null)
            {
                safeControlsNode.RemoveChild(trimmingSafeControlsNode);
            }
            nodeToAdd = webConfigDoc.CreateElement("SafeControl");
            XmlAttribute assemblyAttribute = webConfigDoc.CreateAttribute("Assembly");

            assemblyAttribute.Value = XML_SECURITY_SAFECONTROL;

            nodeToAdd.Attributes.Append(assemblyAttribute);
            XmlAttribute namespaceAttribute = webConfigDoc.CreateAttribute("Namespace");
            namespaceAttribute.Value = "Nextlabs.SPSecurityTrimming";
            nodeToAdd.Attributes.Append(namespaceAttribute);
            XmlAttribute typeNameAttribute = webConfigDoc.CreateAttribute("TypeName");
            typeNameAttribute.Value = "*";
            nodeToAdd.Attributes.Append(typeNameAttribute);
            XmlAttribute safeAttribute = webConfigDoc.CreateAttribute("Safe");
            safeAttribute.Value="True";
            nodeToAdd.Attributes.Append(safeAttribute);

            safeControlsNode.AppendChild(nodeToAdd);

            WriteXmlDocumentToFile(webConfigDoc, filePath);
        }

 

        private void RemoveEnforcerHttpModuleFromWebConfigFile(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument webConfigDoc = new XmlDocument();
            webConfigDoc.PreserveWhitespace = true;
            webConfigDoc.Load(filePath);

            XmlNode httpModulesNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE);

            //XmlNode PLEHttpModuleNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE+"/add[@name='NextLabs.PLE.HttpModule.PLEHttpEnforcerModule']");

            //if (PLEHttpModuleNode != null)
            //{
            //    httpModulesNode.RemoveChild(PLEHttpModuleNode);
            //}

            XmlNode enforcerHttpModuleNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE+"/add[@name='NextLabs.HttpEnforcer.HttpEnforcerModule']");

            if (enforcerHttpModuleNode != null)
            {
                httpModulesNode.RemoveChild(enforcerHttpModuleNode);
            }	

            //XmlNode trimmingHttpModuleNode = webConfigDoc.SelectSingleNode(XMLPATH_HTTPMODULE+"/add[@name='SPSecurityTrimmingModule']");

            //if (trimmingHttpModuleNode != null)
            //{
            //    httpModulesNode.RemoveChild(trimmingHttpModuleNode);
            //}

            // remove security trimming safe controls (TODO change method name)
            XmlNode safeControlsNode = webConfigDoc.SelectSingleNode("/configuration/SharePoint/SafeControls");
            XmlNode trimmingSafeControlsNode = webConfigDoc.SelectSingleNode("/configuration/SharePoint/SafeControls/SafeControl[@Namespace='Nextlabs.SPSecurityTrimming']");
            if (trimmingSafeControlsNode != null)
            {
                safeControlsNode.RemoveChild(trimmingSafeControlsNode);
            }

            WriteXmlDocumentToFile(webConfigDoc, filePath);
        }



        private void WriteXmlDocumentToFile(XmlDocument docToWrite, String filePath)
        {
            using (XmlTextWriter xmlWriter = new XmlTextWriter(filePath, Encoding.UTF8))
            {
                docToWrite.WriteTo(xmlWriter);
                xmlWriter.Close();
            }
        }

        private SPWebServiceCollection GetSharepointWebServices()
        {
            // Obtain a reference to the local Sharepoint Farm
            SPFarm farm = SPFarm.Local;

            // Get the list of deployed Sharepoint Web Services
            return new SPWebServiceCollection(farm);
        }
    }
}
