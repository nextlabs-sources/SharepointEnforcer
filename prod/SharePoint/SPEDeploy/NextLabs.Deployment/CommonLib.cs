using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Threading;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.Deployment
{
    public class CommonLib
    {
        const string BASESITESTAPLING_NODE0 = "/Elements[@xmlns='http://schemas.microsoft.com/sharepoint/']";
        const string BASESITESTAPLING_NODE1 = "FeatureSiteTemplateAssociation[@Id='4f6fd05e-b392-418b-9dbf-b0fb92f12271']";

        const string DOCICON_NODE0 = "/DocIcons/ByExtension";
        const string DOCICON_NODE1 = "/DocIcons/ByExtension/Mapping[@Key='nxl']";

        //NextLabs.Entitlement.wsp Guid 
        public const string NEXTLABSWSPGUID = "6c15412b-290c-49ac-bd38-9b0ad852973b";
        //NextLabs.Entitlement.Basic Feature ID
        public const string NEXTLABSENTITLEMENTBASICFEATUREID = "ddf3439c-65aa-443b-8973-b87b003c0254";
        //NextLabs.Entitlement.EventReceiver Feature ID
        public const string NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID = "4f6fd05e-b392-418b-9dbf-b0fb92f12271";
        //NextLabs.Entitlement.Administration Feature ID
        public const string NEXTLABSENTITLEMENTADMINISTRATIONFEATUREID = "b710de11-a2b7-49ff-b6e4-7bb5449178a3";

#if SP2013
        public const string GIFACTIVEURL = "/_layouts/15/images/FeatureManager/Active.gif";
        public const string GIFDEACTIVEURL = "/_layouts/15/images/FeatureManager/Deactive.gif";
        const string NEXTLABSFEATUREFILE = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\15\\TEMPLATE\\FEATURES\\NextLabs.Entitlement.Basic\\Feature.xml";
        const string REGSHAREPOINT = "HKLM\\SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\15.0\\WSS\\";
#elif SP2010
        public const string GIFACTIVEURL = "/_layouts/images/FeatureManager/Active.gif";
        public const string GIFDEACTIVEURL = "/_layouts/images/FeatureManager/Deactive.gif";
        const string NEXTLABSFEATUREFILE = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\14\\TEMPLATE\\FEATURES\\NextLabs.Entitlement.Basic\\Feature.xml";
        const string REGSHAREPOINT = "HKLM\\SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\14.0\\WSS\\";
#else
        public const string GIFACTIVEURL = "/_layouts/15/images/FeatureManager/Active.gif";
        public const string GIFDEACTIVEURL = "/_layouts/15/images/FeatureManager/Deactive.gif";
        const string NEXTLABSFEATUREFILE = "C:\\Program Files\\Common Files\\Microsoft Shared\\Web Server Extensions\\16\\TEMPLATE\\FEATURES\\NextLabs.Entitlement.Basic\\Feature.xml";
        const string REGSHAREPOINT = "HKLM\\SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\16.0\\WSS\\";
#endif

        private Dictionary<int, string> ErrorInfo;

        public enum SharepointType
        {
            StandAlone = 0,
            FarmStandAlone = 1,
            FarmComplete = 2
        }

        public CommonLib()
        {
            ErrorInfo = new Dictionary<int, string>()
            {
                {0,"Excute Successfully"},
                {1,"There is no permission or exception, you can use Dbgview.exe to see more detail."},
                {2,"Add the solution file to the farm's configuration database failed"},
                {3,"Deploy the solution failed"},
                {4,"Retract the solution failed"},
                {5,"Remove the solution file from the farm's configuration database failed"},
                {6,"Excute EnableSP2010JobUpdate failed"},
                {7,"timer job for solution excute completely"},
                {8,"timer job for solution excute failed or was aborted then fail"}
            };
        }

        public string GetSPTemplatePath()
        {
            RegistryKey _rootKey = Registry.LocalMachine;
            string strTemplatePath = "";

            try
            {
#if SP2016 || SP2019
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\16.0");
#elif SP2013
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\15.0");
#elif SP2010
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\14.0");
#endif
                strTemplatePath = (string)_subKey.GetValue("TemplatePath");
            }
            catch (Exception)
            {
            }

            return strTemplatePath;
        }

        public string CopyFolder(string sPath, string dPath)
        {
            string flag = "success";
            try
            {
                if (!Directory.Exists(dPath))
                {
                    Directory.CreateDirectory(dPath);
                }
                DirectoryInfo sDir = new DirectoryInfo(sPath);
                FileInfo[] fileArray = sDir.GetFiles();
                foreach (FileInfo file in fileArray)
                {
                    file.CopyTo(dPath + "\\" + file.Name, true);
                }
                DirectoryInfo dDir = new DirectoryInfo(dPath);
                DirectoryInfo[] subDirArray = sDir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirArray)
                {
                    CopyFolder(subDir.FullName, dPath + "//" + subDir.Name);
                }
            }
            catch (Exception ex)
            {
                flag = ex.ToString();
            }
            return flag;
        }

        public void Prepare14FeatureFiles()
        {
            string templatePath = GetSPTemplatePath();
            if (!templatePath.EndsWith("\\"))
            {
                templatePath += "\\";
            }
            string basicFeaturePath15 = templatePath;
            string basicFeaturePath14 = templatePath.Replace("15", "14");

            List<string> Need2Copy = new List<string> { "NextLabs.Entitlement.Basic", "NextLabs.Entitlement.EventReceiver" };
            foreach (string path in Need2Copy)
            {
                string pathForm = basicFeaturePath15 + "LAYOUTS\\featuremanager\\" + path;
                string pathTo = basicFeaturePath14 + "FEATURES\\" + path;
                if (!Directory.Exists(pathTo))
                {
                    if (Directory.Exists(pathForm))
                    {
                        CopyFolder(pathForm, pathTo);
                    }
                }
            }

        }

        public string GetErrorMsg(int errorcode)
        {
            string ret = "Not in error dictionary!";
            if (ErrorInfo.ContainsKey(errorcode))
            {
                ret = ErrorInfo[errorcode];
            }
            return ret;
        }

        public bool IsSPEInstalled()
        {
            RegistryKey CE_key = null;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
            });

            if (CE_key != null)
            {
                CE_key.Close();
                return true;
            }
            return false;
        }

        public bool IsDeployOnThisServer()
        {
            if (File.Exists(NEXTLABSFEATUREFILE))
            {
                return true;
            }
            return false;
        }

        public string GetSPEIntalledPath()
        {
            RegistryKey CE_key = null;
            string RegCEInstallDir_str = "";
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
                object RegCEInstallDir = null;
                if (CE_key != null)
                {
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                }
                if (RegCEInstallDir != null)
                {
                    RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (!RegCEInstallDir_str.EndsWith("\\"))
                    {
                        RegCEInstallDir_str += "\\";
                    }
                }
            });
            return RegCEInstallDir_str;
        }

        public string GetSPECommonBinPath()
        {
            RegistryKey CE_key = null;
            string RegCEInstallDir_str = "";
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegCEInstallDir = null;
                if (CE_key != null)
                {
                    RegCEInstallDir = CE_key.GetValue("InstallDir");
                }
                if (RegCEInstallDir != null)
                {
                    RegCEInstallDir_str = Convert.ToString(RegCEInstallDir);
                    if (!RegCEInstallDir_str.EndsWith("\\"))
                    {
                        RegCEInstallDir_str += "\\";
                    }
                }
            });
            return RegCEInstallDir_str + "Common\\bin64\\";
        }

        public SharepointType GetSharepointType()
        {
            string regPath = REGSHAREPOINT;
            RegistryKey Reg_key = null;
            object type = null;
            string strType = "";
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                Reg_key = Registry.LocalMachine.OpenSubKey(regPath, false);
                if (Reg_key != null)
                    type = Reg_key.GetValue("ServerRole");
            });
            if (type != null)
            {
                strType = Convert.ToString(type);
                if (strType.Equals("APPLICATION"))
                    return SharepointType.FarmComplete;
                if (strType.Equals("WFE"))
                    return SharepointType.FarmStandAlone;
            }
            return SharepointType.StandAlone;
        }

        public void ExecuteCommand(string path, string args, ref int result)
        {
            ProcessStartInfo start = new ProcessStartInfo(path);
            start.Arguments = args;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardInput = false;
            start.UseShellExecute = false;
            Process p = Process.Start(start);
            p.WaitForExit();
            result = p.ExitCode;
            p.Close();
        }

        public void WriteXmlDocumentToFile(XmlDocument docToWrite, String filePath)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(filePath, Encoding.UTF8);
            docToWrite.WriteTo(xmlWriter);
            xmlWriter.Close();
        }

        public void ModifyBaseSiteStaplingXml(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.PreserveWhitespace = true;
            XmlDoc.Load(filePath);
            XmlNode ElementsNode = null;
            ElementsNode = XmlDoc.ChildNodes[0];
            XmlNode FeatureSiteTemplateAssociationNode = ElementsNode.SelectSingleNode(BASESITESTAPLING_NODE1);
            if (FeatureSiteTemplateAssociationNode != null)
            {
                ElementsNode.RemoveChild(FeatureSiteTemplateAssociationNode);
            }

            XmlElement nodeToAdd = null;
            XmlAttribute idAttribute = null;
            XmlAttribute templateNameAttribute = null;

            nodeToAdd = XmlDoc.CreateElement("FeatureSiteTemplateAssociation");
            idAttribute = XmlDoc.CreateAttribute("Id");
            idAttribute.Value = NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID;
            nodeToAdd.Attributes.Append(idAttribute);

            templateNameAttribute = XmlDoc.CreateAttribute("TemplateName");
            templateNameAttribute.Value = "GLOBAL";
            nodeToAdd.Attributes.Append(templateNameAttribute);

            ElementsNode.PrependChild(nodeToAdd);
            WriteXmlDocumentToFile(XmlDoc, filePath);
        }

        public void RestoreBaseSiteStaplingXml(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.PreserveWhitespace = true;
            XmlDoc.Load(filePath);

            XmlNode ElementsNode = null;
            ElementsNode = XmlDoc.ChildNodes[0];
            XmlNode FeatureSiteTemplateAssociationNode = ElementsNode.SelectSingleNode(BASESITESTAPLING_NODE1);

            if (FeatureSiteTemplateAssociationNode != null)
            {
                ElementsNode.RemoveChild(FeatureSiteTemplateAssociationNode);
            }

            WriteXmlDocumentToFile(XmlDoc, filePath);
        }

        public void ModifyDOCICONXml(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.PreserveWhitespace = true;
            XmlDoc.Load(filePath);
            XmlNode byExtensionNode = null;

            byExtensionNode = XmlDoc.SelectSingleNode(DOCICON_NODE0);
            XmlNode mappingNode = XmlDoc.SelectSingleNode(DOCICON_NODE1);
            if (mappingNode != null)
            {
                byExtensionNode.RemoveChild(mappingNode);
            }

            XmlElement nodeToAdd = null;
            XmlAttribute keyAttribute = null;
            XmlAttribute valueAttribute = null;
            XmlAttribute openControlAttribute = null;

            nodeToAdd = XmlDoc.CreateElement("Mapping");

            keyAttribute = XmlDoc.CreateAttribute("Key");
            keyAttribute.Value = "nxl";
            nodeToAdd.Attributes.Append(keyAttribute);

            valueAttribute = XmlDoc.CreateAttribute("Value");
            valueAttribute.Value = "nxl.png";
            nodeToAdd.Attributes.Append(valueAttribute);

            openControlAttribute = XmlDoc.CreateAttribute("OpenControl");
            openControlAttribute.Value = "SharePoint.OpenDocuments";
            nodeToAdd.Attributes.Append(openControlAttribute);

            byExtensionNode.PrependChild(nodeToAdd);
            WriteXmlDocumentToFile(XmlDoc, filePath);
        }

        public void RestoreDOCICONXml(String filePath)
        {
            if (System.IO.File.Exists(filePath) == false)
                return;
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.PreserveWhitespace = true;
            XmlDoc.Load(filePath);

            XmlNode byExtensionNode = XmlDoc.SelectSingleNode(DOCICON_NODE0);

            XmlNode mappingNode = XmlDoc.SelectSingleNode(DOCICON_NODE1);

            if (mappingNode != null)
            {
                byExtensionNode.RemoveChild(mappingNode);
            }

            WriteXmlDocumentToFile(XmlDoc, filePath);
        }

        public void BackupFile(string path, string backupFile, string fileToBackup)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            backupFile = path + backupFile;
            fileToBackup = path + fileToBackup;
            if (File.Exists(fileToBackup))
            {
                File.Delete(fileToBackup);
            }

            File.Move(backupFile, fileToBackup);
        }

        public void RestoreFile(string path, string backupFile, string fileToRestore)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            backupFile = path + backupFile;
            fileToRestore = path + fileToRestore;

            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
            else
            {
                return;
            }

            File.Move(backupFile, fileToRestore);

        }

        public int WaitForSPSolutionJobToComplete(SPSolution solution)
        {
            //Waiting for timer job to complete for solution
            while (solution.JobExists)
            {
                SPRunningJobStatus jobStatus = solution.JobStatus;
                if (jobStatus.Equals(SPRunningJobStatus.Succeeded))
                {
                    return 7;
                }

                if (jobStatus.Equals(SPRunningJobStatus.Failed) || jobStatus.Equals(SPRunningJobStatus.Aborted))
                {
                    return 8;
                }

                Thread.Sleep(1000);
            }
            return 7;
        }

        public int GetCounter()
        {
            string strline = "";
            try
            {
                Monitor.Enter(this);
                string SPEinstallPath = this.GetSPEIntalledPath();
                SPEinstallPath += "solution\\Counter.txt";
                StreamReader objReader = new StreamReader(SPEinstallPath);
                strline = objReader.ReadLine();
                objReader.Close();
            }
            finally
            {
                Monitor.Exit(this);
            }
            return Convert.ToInt32(strline);
        }

        public void AddCounter()
        {
            try
            {
                Monitor.Enter(this);
                string SPEinstallPath = this.GetSPEIntalledPath();
                SPEinstallPath += "solution\\Counter.txt";
                using (FileStream fs = new FileStream(SPEinstallPath, FileMode.Open))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    int counter = this.GetCounter() + 1;
                    sw.Write(Convert.ToString(counter));
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public void DeCounter()
        {
            try
            {
                Monitor.Enter(this);
                string SPEinstallPath = this.GetSPEIntalledPath();
                SPEinstallPath += "solution\\Counter.txt";
                using (FileStream fs = new FileStream(SPEinstallPath, FileMode.Open))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    int counter = this.GetCounter() - 1;
                    sw.Write(Convert.ToString(counter));
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public void InsertStrToFile(string fileFullPath, int insertLine, string insertText)
        {
            if (!File.Exists(fileFullPath))
            {
                return;
            }

            string sText = "";
            int iLnTmp = 0;
            StreamReader sr = new StreamReader(fileFullPath);
            while (!sr.EndOfStream)
            {
                iLnTmp++;
                if (iLnTmp == insertLine)
                {
                    sText += insertText + "\r\n";
                }
                string sTmp = sr.ReadLine();
                sText += sTmp + "\r\n";
            }
            sr.Close();
            StreamWriter sw = new StreamWriter(fileFullPath, false);
            sw.Write(sText);
            sw.Flush();
            sw.Close();
        }

        public void RemoveStrFromFile(string fileFullPath, string removeText)
        {
            if (!File.Exists(fileFullPath))
            {
                return;
            }
            string sText = "";
            StreamReader sr = new StreamReader(fileFullPath);
            while (!sr.EndOfStream)
            {
                string sTmp = sr.ReadLine();
                if (sTmp.Contains(removeText))
                {
                    sTmp = sTmp.Replace(removeText, "");
                }
                sText += sTmp + "\r\n";
            }
            sr.Close();
            StreamWriter sw = new StreamWriter(fileFullPath, false);
            sw.Write(sText);
            sw.Flush();
            sw.Close();
        }

        public void DeactivateWebScopeFeature(SPWebApplication webApp, Guid featureId, List<string> selectedSiteIDs)
        {
            ActiveOrDeactiveWebScopeFeature(webApp, featureId, selectedSiteIDs, false);
        }

        public void ActivateWebScopeFeature(SPWebApplication webApp, Guid featureId, List<string> selectedSiteIDs)
        {
            ActiveOrDeactiveWebScopeFeature(webApp, featureId, selectedSiteIDs, true);
        }

        private void ActiveOrDeactiveWebScopeFeature(SPWebApplication webApp, Guid featureId, List<string> selectedSiteIDs, bool bActive)
        {
            if (webApp != null)
            {
                string strAction = bActive ? "Active" : "Deactive";
                foreach (SPSite site in webApp.Sites)
                {
                    try // use try-catch to avoid to block workflow.
                    {
                        using (site)
                        {
                            if (site == null || site.ReadOnly)
                            {
                                continue; // Don't care read-only site colletion.
                            }
                            if (bActive)
                            {
                                if (selectedSiteIDs.Contains(site.ID.ToString()))
                                {
                                    AddOrRemoveFeatureForWebs(webApp, site, featureId, true);
                                }
                                else
                                {
                                    AddOrRemoveFeatureForWebs(webApp, site, featureId, false);
                                }
                            }
                            else
                            {
                                AddOrRemoveFeatureForWebs(webApp, site, featureId, false);
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        Progress.WriteLog(webApp.Name, "ActiveOrDeactiveWebScopeFeature: " + "Site URL:" + site.Url + ", " + strAction + " feature failed, Exception: " + exp);
                    }
                }
            }
        }

        public void AddOrRemoveFeatureForWebs(SPWebApplication webApp, SPSite site, Guid featureId, bool bAdd)
        {
            foreach (SPWeb web in site.AllWebs)
            {
                try // use try-catch to avoid to block workflow.
                {
                    if (web == null)
                    {
                        continue;
                    }
                    using (web)
                    {
                        bool bOld = web.AllowUnsafeUpdates;
                        web.AllowUnsafeUpdates = true;
                        if (bAdd)
                        {
                            if (web.Features[featureId] == null)
                            {
                                web.Features.Add(featureId, true);
                                web.Update();
                            }
                        }
                        else
                        {
                            if (web.Features[featureId] != null)
                            {
                                web.Features.Remove(featureId, true);
                                web.Update();
                            }
                        }
                        web.AllowUnsafeUpdates = bOld;
                    }
                }
                catch (Exception exp)
                {
                    string enforceAction = bAdd ? "Add" : "Remove";
                    Progress.WriteLog(webApp.Name, "Web URL:" + web.Url + ", " + enforceAction + " Feature Failed, Exception: " + exp);
                }
            }
        }

        // Check added/removed features and events is completely.
        public static bool CheckFeaturesAndEvents(SPWebApplication webApp, List<string> selectedSiteIDs, bool bActive, List<string> activedSiteIDs)
        {
            bool bRet = true;
            if (webApp != null)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    Guid basicGuid = new Guid(NEXTLABSENTITLEMENTBASICFEATUREID);
                    string strAction = bActive ? "Active" : "Deactive";
                    if (bActive && webApp.Features[basicGuid] == null)
                    {
                        bRet = false;
                        Progress.WriteLog(webApp.Name, "WebApp Add Basic feature Failed, webApp: " + webApp.DisplayName);
                    }
                    else if (!bActive && webApp.Features[basicGuid] != null)
                    {
                        bRet = false;
                        Progress.WriteLog(webApp.Name, "Web Remove Basic feature Failed, webApp: " + webApp.DisplayName);
                    }
                    foreach (SPSite site in webApp.Sites)
                    {
                        try // use try-catch to avoid to block workflow.
                        {
                            using (site)
                            {
                                if (site == null || site.ReadOnly)
                                {
                                    continue; // Don't care read-only site colletion.
                                }
                                if (bActive)
                                {
                                    if (selectedSiteIDs.Contains(site.ID.ToString()))
                                    {
                                        if (CheckFeatureForWebs(webApp, site, true))
                                        {
                                            activedSiteIDs.Add(site.ID.ToString());
                                        }
                                        else
                                        {
                                            bRet = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!CheckFeatureForWebs(webApp, site, false))
                                        {
                                            bRet = false;
                                            activedSiteIDs.Add(site.ID.ToString());
                                        }
                                    }
                                }
                                else
                                {
                                    if (!CheckFeatureForWebs(webApp, site, false))
                                    {
                                        bRet = false;
                                        activedSiteIDs.Add(site.ID.ToString());
                                    }
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            bRet = false;
                            Progress.WriteLog(webApp.Name, "CheckFeaturesAndEvents: " + "Site URL:" + site.Url + ", " + strAction + " feature failed, Exception: " + exp);
                        }
                    }
                });
            }
            return bRet;
        }

        public static bool CheckFeatureForWebs(SPWebApplication webApp, SPSite site, bool bActive)
        {
            bool bRet = true;
            Progress.WriteLog(webApp.Name, "CheckFeatureForWebs site collection: " + site.Url);
            System.Guid guidEventReceiver = new Guid(NEXTLABSENTITLEMENTEVENTRECEIVERFEATUREID);
            foreach (SPWeb web in site.AllWebs)
            {
                try // use try-catch to avoid to block workflow.
                {
                    using (web)
                    {
                        if (web == null)
                        {
                            continue;
                        }
                        if (bActive)
                        {
                            if (web.Features[guidEventReceiver] == null)
                            {
                                bRet = false;
                                Progress.WriteLog(webApp.Name, "Web Add EventReceiver Failed, Url: " + web.Url);
                            }
                        }
                        else
                        {
                            if (web.Features[guidEventReceiver] != null)
                            {
                                bRet = false;
                                Progress.WriteLog(webApp.Name, "Web Remove EventReceiver Failed, Url: " + web.Url);
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    Progress.WriteLog(webApp.Name, "Web URL:" + web.Url + ", check feature failed, Exception: " + exp);
                }
            }
            return bRet; 
        }

        public void RegIRMSetting()
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
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
                        RegistryKey MsoIrm_key = commonexten_key.OpenSubKey("MsoIrmProtector", true);
                        if (MsoIrm_key != null)
                        {
                            MsoIrm_key.SetValue("Extensions", "", RegistryValueKind.String);
                            MsoIrm_key.Close();
                        }
                        RegistryKey OfcIrm_key = commonexten_key.OpenSubKey("OfcIrmProtector", true);
                        if (OfcIrm_key != null)
                        {
                            OfcIrm_key.SetValue("Extensions", "xps,xlam,thmx", RegistryValueKind.String);
                            OfcIrm_key.Close();
                        }
                        Software_key.Close();
                    }
                    RegistryKey IRM_key = commonexten_key.OpenSubKey("TagDocProtector", true);
                    if (IRM_key == null)
                    {
                        IRM_key = commonexten_key.CreateSubKey("TagDocProtector");
                    }
                    if (IRM_key != null)
                    {
                        IRM_key.SetValue("Extensions", spExtensions, RegistryValueKind.String);
                        IRM_key.SetValue("Product", sTagProtector, RegistryValueKind.String);
                        IRM_key.SetValue("Version", "1", RegistryValueKind.String);
                        IRM_key.Close();
                    }
                });
            }
            catch (System.Exception e)
            {
                Trace.WriteLine("Exception happened:" + e.Message);
            }


        }

        public void UnRegIRMSetting()
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
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
                        Software_key.DeleteValue("{6EC4BB1F-3F73-4799-BC98-A3DF9AE23A0B}");
                        RegistryKey MsoIrm_key = commonexten_key.OpenSubKey("MsoIrmProtector", true);
                        if (MsoIrm_key != null)
                        {
                            MsoIrm_key.SetValue("Extensions", "doc,dot,xls,xlt,xla,ppt,pot,pps", RegistryValueKind.String);
                            MsoIrm_key.Close();
                        }
                        RegistryKey OfcIrm_key = commonexten_key.OpenSubKey("OfcIrmProtector", true);
                        if (OfcIrm_key != null)
                        {
                            OfcIrm_key.SetValue("Extensions", "xps,docx,docm,dotx,dotm,xlsx,xlsm,xlsb,xltx,xltm,xlam,pptx,pptm,potx,potm,thmx,ppsx,ppsm", RegistryValueKind.String);
                            OfcIrm_key.Close();
                        }
                        RegistryKey IRM_key = commonexten_key.OpenSubKey("TagDocProtector", false);
                        if (IRM_key != null)
                        {
                            commonexten_key.DeleteSubKey("TagDocProtector");
                        }
                        Software_key.Close();
                    }
                });
            }
            catch (System.Exception e)
            {
                Trace.WriteLine("Exception happened:" + e.Message);
            }
        }

        public string GetSPEVersion()
        {
            RegistryKey CE_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer\\", false);
            object productVersion = "";
            if (CE_key != null)
            {
                productVersion = CE_key.GetValue("ProductVersion");
            }

            if (productVersion != null)
            {
                CE_key.Close();
            }
            else
            {
                return "0.0.0.0";
            }
            return productVersion.ToString();
        }

        public void ClearNLFeatureStatus()
        {
            SPFarm farm = SPFarm.Local;
            SPWebService spws = farm.Services.GetValue<SPWebService>("");
            foreach (SPWebApplication webApp in spws.WebApplications)
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    bool ishas = false;
                    //To check if is there  save the nextlabs feature status before.
                    if (webApp.Properties.ContainsKey("NextlabsFeature_WebAPP_Status"))
                    {
                        webApp.Properties.Remove("NextlabsFeature_WebAPP_Status");
                        ishas = true;
                    }
                    if (webApp.Properties.ContainsKey("NextlabsFeature_SiteCollection_Status"))
                    {
                        webApp.Properties.Remove("NextlabsFeature_SiteCollection_Status");
                        ishas = true;
                    }
                    if (webApp.Properties.ContainsKey("NextlabsFeature_Webs_Status"))
                    {
                        webApp.Properties.Remove("NextlabsFeature_Webs_Status");
                        ishas = true;
                    }
                    if (ishas)
                    {
                        webApp.Update();
                    }
                });
            }
        }
    }

    //SharePoint ULS Log
    public class LoggingService : SPDiagnosticsServiceBase
    {
        public static string MyDiagnosticAreaName = "NextLabsDiagnostic";
        private static LoggingService _Current;
        public static LoggingService Current
        {
            get
            {
                if (_Current == null)
                { _Current = new LoggingService(); }
                return _Current;
            }
        }

        private LoggingService()
            : base("NextLabsDiagnostic Logging Service", SPFarm.Local)
        { }

        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            List<SPDiagnosticsArea> areas = new List<SPDiagnosticsArea>  {            
                    new SPDiagnosticsArea(MyDiagnosticAreaName, new List<SPDiagnosticsCategory>
                    {new SPDiagnosticsCategory("Nextlabs", TraceSeverity.Unexpected, EventSeverity.Error) })};
            return areas;
        }

        public static void LogError(string categoryName, string errorMessage)
        {
            SPDiagnosticsCategory category = LoggingService.Current.Areas[MyDiagnosticAreaName].Categories[categoryName];
            LoggingService.Current.WriteTrace(0, category, TraceSeverity.Unexpected, errorMessage);
        }
    }
}
