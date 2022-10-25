using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.SharePoint;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Nextlabs.Entitlement.Wizard
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //added for ISV, Deployment module will be repalced during deployment. - 1
            string strTempPath = System.IO.Path.GetTempPath() + "Nextlabs.Deployment.dll";
            string strRealGACPath = string.Empty;
            bool bBackup = BackupSignedDll(strTempPath, ref strRealGACPath);

            InstallerForm form = new InstallerForm();
            form.Text = InstallConfiguration.FormatString("{SolutionTitle}");

            form.ContentControls.Add(CreateWelcomeControl());
            form.ContentControls.Add(CreateSystemCheckControl());

            Application.Run(form);

            if (Directory.Exists(@"C:\Program Files\Common Files\microsoft shared\Web Server Extensions\14\TEMPLATE"))
            {
                string strsrcDir = @"C:\Program Files\Common Files\microsoft shared\Web Server Extensions\15\TEMPLATE\LAYOUTS\error-template";
                string strdstDir = @"C:\Program Files\Common Files\microsoft shared\Web Server Extensions\14\TEMPLATE\LAYOUTS\error-template";

                if (!Directory.Exists(@strdstDir)) Directory.CreateDirectory(strdstDir);
                DirectoryCopy(strsrcDir, strdstDir);
            }

            // Check again after deploy, undeploy or upgrade restore.
            bool bDeployed = SystemCheckControl.IsSolutionDeployed();
            if (bBackup)
            {
                RecoverSignedll(strTempPath, strRealGACPath, bDeployed);
            }
        }

        static bool RunCmd(string strFn, string strArg)
        {
            if (string.IsNullOrEmpty(strFn))
                return false;

            bool bRet = true;
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = strFn;
                proc.StartInfo.Arguments = strArg;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                try
                {
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception exp)
                {
                    Trace.WriteLine(string.Format("Run cmd {0} with arguments {1} failed: {2}", strFn, strArg, exp.Message));
                    bRet = false;
                }
            }

            return bRet;
        }

        static bool RecoverSignedll(string strTempPath, string strRealGACPath, bool bDeployed)
        {
            bool bRet = true;
            string strBak = string.Empty;
            try
            {
                if (!bDeployed)
                {
                    //just delete file since it is in undeploying.
                    File.Delete(strTempPath);
                }
                else //deploy process
                {
                    if (File.Exists(strRealGACPath)) //backup org file
                    {
                        strBak = strRealGACPath + "_" + DateTime.Now.GetHashCode().ToString();
                        File.Move(strRealGACPath, strBak);
                    }
                    File.Copy(strTempPath, strRealGACPath, true);
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = "iisreset";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    try
                    {
                        using (Process exeProcess = Process.Start(startInfo))
                        {
                            exeProcess.WaitForExit();
                        }
                    }
                    catch
                    {
                        bRet = false;
                    }
                    File.Delete(strBak);    //delete org backup file.
                    File.Delete(strTempPath);//delete the backup file in temp folder
                }
            }
            catch 
            {
                bRet = false;
            }

            return bRet;
        }

        static bool BackupSignedDll(string strTempPath, ref string strRealGACPath)
        {
            bool bRet = true;
            try
            {
                string strGacDepPath = System.Environment.GetEnvironmentVariable("windir") + "\\Microsoft.NET\\assembly\\GAC_MSIL\\NextLabs.Deployment";

                if (Directory.Exists(strGacDepPath))
                {
                    DirectoryInfo sDir = new DirectoryInfo(strGacDepPath);
                    DirectoryInfo[] subDirArray = sDir.GetDirectories();
                    //search for next level folder
                    foreach (DirectoryInfo subDir in subDirArray)
                    {
                        try
                        {
                            strRealGACPath = subDir.FullName + "\\Nextlabs.Deployment.dll";
                            if (File.Exists(strTempPath))
                            {
                                File.Move(strTempPath, strTempPath + "_" + DateTime.Now.GetHashCode().ToString());
                            }
                            System.IO.File.Copy(strRealGACPath, strTempPath, true);
                        }
                        catch
                        {
                            bRet = false;
                        }
                        break;
                    }
                }
                else
                {
                    Trace.WriteLine("$$$$, not exist");
                    bRet = false;
                }
            }
            catch
			{
            	bRet = false;
			}
           
            return bRet;
        }


        private static void DirectoryCopy(string sourceDirectory, string targetDirectory)
        {
            if (!Directory.Exists(sourceDirectory) || !Directory.Exists(targetDirectory))
            {
                return;
            }
            DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfo = sourceInfo.GetFiles();
            foreach (FileInfo fiTemp in fileInfo)
            {
                File.Copy(sourceDirectory + "\\" + fiTemp.Name, targetDirectory + "\\" + fiTemp.Name, true);
            }
            DirectoryInfo[] diInfo = sourceInfo.GetDirectories();
            foreach (DirectoryInfo diTemp in diInfo)
            {
                string sourcePath = diTemp.FullName;
                string targetPath = diTemp.FullName.Replace(sourceDirectory, targetDirectory);
                Directory.CreateDirectory(targetPath);
                DirectoryCopy(sourcePath, targetPath);
            }
        }

        private static InstallerControl CreateWelcomeControl()
        {
            WelcomeControl control = new WelcomeControl();
            control.Title = InstallConfiguration.FormatString(Resources.CommonUIStrings.controlTitleWelcome);
            control.SubTitle = InstallConfiguration.FormatString(Resources.CommonUIStrings.controlSubTitleWelcome);
            return control;
        }

        private static InstallerControl CreateSystemCheckControl()
        {
            SystemCheckControl control = new SystemCheckControl();
            control.Title = Resources.CommonUIStrings.controlTitleSystemCheck;
            if (SystemCheckControl.IsSolutionDeployed())
            {
                control.SubTitle = InstallConfiguration.FormatString(Resources.CommonUIStrings.controlSubTitleOptionsUninstall);
            }
            else
            {
                control.SubTitle = InstallConfiguration.FormatString(Resources.CommonUIStrings.controlSubTitleSystemCheck);
            }
            control.RequireMOSS = InstallConfiguration.RequireMoss;
            control.RequireSearchSKU = false;
            return control;
        }

        internal static InstallerControl CreateUpgradeControl()
        {
            UpgradeControl control = new UpgradeControl();
            control.Title = Resources.CommonUIStrings.controlTitleUpgradeRemove;
            control.SubTitle = Resources.CommonUIStrings.controlSubTitleSelectOperation;
            return control;
        }

        internal static InstallerControl CreateRepairControl()
        {
            RepairControl control = new RepairControl();
            control.Title = Resources.CommonUIStrings.controlTitleRepairRemove;
            control.SubTitle = Resources.CommonUIStrings.controlSubTitleSelectOperation;
            return control;
        }

        internal static InstallerControl CreateEULAControl()
        {
            EULAControl control = new EULAControl();
            control.Title = Resources.CommonUIStrings.controlTitleLicense;
            control.SubTitle = Resources.CommonUIStrings.controlSubTitleLicense;
            return control;
        }

        internal static InstallerControl CreateDeploymentTargetsControl()
        {
            InstallerControl control = null;
            SPFeatureScope featureScope = InstallConfiguration.FeatureScope;
            if (featureScope == SPFeatureScope.Farm)
            {
                control = new DeploymentTargetsControl();
                control.Title = Resources.CommonUIStrings.controlTitleFarmDeployment;
                control.SubTitle = Resources.CommonUIStrings.controlSubTitleFarmDeployment;
            }
            else if (featureScope == SPFeatureScope.Site)
            {
                control = new SiteCollectionDeploymentTargetsControl();
                control.Title = Resources.CommonUIStrings.controlTitleSiteDeployment;
                control.SubTitle = Resources.CommonUIStrings.controlSubTitleSiteDeployment;
            }
            return control;
        }

        internal static InstallProcessControl CreateProcessControl()
        {
            InstallProcessControl control = new InstallProcessControl();
            control.Title = Resources.CommonUIStrings.controlTitleInstalling;
            control.SubTitle = Resources.CommonUIStrings.controlSubTitleInstalling;
            return control;
        }
    }
}
