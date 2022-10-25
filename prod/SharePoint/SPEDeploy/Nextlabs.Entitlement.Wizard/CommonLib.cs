using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using Microsoft.SharePoint;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.SharePoint.Administration;
using System.Collections.ObjectModel;

namespace Nextlabs.Entitlement.Wizard
{
    public class CommonLib
    {
        // NextLabs.Entitlement.wsp Guid 
        public const string NextLabsWspfeatureId = "6c15412b-290c-49ac-bd38-9b0ad852973b";
        //NextLabs.Entitlement.Basic Feature ID
        public const string BasicFeatureId = "ddf3439c-65aa-443b-8973-b87b003c0254";
        // NextLabs.Entitlement.EventReceiver Feature ID
        public const string EventFeatureId = "4f6fd05e-b392-418b-9dbf-b0fb92f12271";
        // WSP package path
        public const string wspPath = @"C:\Program Files\NextLabs\SharePoint Enforcer\solution\NextLabs.Entitlement.wsp";

        public static string GetSPEIntalledPath()
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

        public static string GetSPTemplatePath()
        {
            RegistryKey _rootKey = Registry.LocalMachine;
            string strTemplatePath = "";

            try
            {
#if SP2016 || SP2019
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\16.0");
#elif SP2010
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\14.0");
#elif SP2013
                RegistryKey _subKey = _rootKey.OpenSubKey("SOFTWARE\\Microsoft\\Office Server\\15.0");
#endif
                strTemplatePath = (string)_subKey.GetValue("TemplatePath");
            }
            catch (Exception)
            {
            }

            return strTemplatePath;
        }

        public static string CopyFolder(string sPath, string dPath)
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

        public static void Prepare14FeatureFiles()
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

                if (Directory.Exists(pathForm))
                {
                    CopyFolder(pathForm, pathTo);
                }
            }

        }

        public static void ExecuteCommand(string path, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo(path);
            start.Arguments = args;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardInput = false;
            start.UseShellExecute = false;
            Process p = Process.Start(start);
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        /// <summary>
        /// Passed to <see cref="GetTokenInformation"/> to specify what
        /// information about the token to return.
        /// </summary>
        enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The elevation type for a user token.
        /// </summary>
        enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public static bool IsRunAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
            var principal = new WindowsPrincipal(identity);
            // Check if this user has the Administrator role. If they do, return immediately.
            // If UAC is on, and the process is not elevated, then this will actually return false.
            if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return true;

            // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
            {
                // Operating system does not support UAC; skipping elevation check.
                return false;
            }

            int tokenInfLength = Marshal.SizeOf(typeof(int));
            IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

            try
            {
                var token = identity.Token;
                var result = GetTokenInformation(token, TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                if (!result)
                {
                    var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    throw new InvalidOperationException("Couldn't get token information", exception);
                }

                var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                switch (elevationType)
                {
                    case TokenElevationType.TokenElevationTypeDefault:
                        // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                        return false;
                    case TokenElevationType.TokenElevationTypeFull:
                        // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                        return true;
                    case TokenElevationType.TokenElevationTypeLimited:
                        // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                        return true;
                    default:
                        // Unknown token elevation type.
                        return false;
                }
            }
            finally
            {
                if (tokenInformation != IntPtr.Zero) Marshal.FreeHGlobal(tokenInformation);
            }
        }

        public static List<SPWebApplication> RemoveBasicFeature(Collection<SPWebApplication> applications)
        {
            List<SPWebApplication> selectedWebApps = new List<SPWebApplication>();
            Guid basicFeatureId = new Guid(BasicFeatureId);
            for (int i = 0; i < applications.Count; i++)
            {
                try
                {
                    SPWebApplication webApp = applications[i];
                    //Deactive NextLabs.Entitlement.Basic.feature
                    SPFeature basicFeature = webApp.Features[basicFeatureId];
                    if (basicFeature != null)
                    {
                        selectedWebApps.Add(webApp);
                        bool formDigestSetting = webApp.FormDigestSettings.Enabled;
                        webApp.FormDigestSettings.Enabled = false;
                        try
                        {
                            webApp.Features.Remove(basicFeatureId);
                        }
                        catch (Exception exp)
                        {
                            Trace.WriteLine("Remove BasicFeature: " + exp);
                        }
                        webApp.FormDigestSettings.Enabled = formDigestSetting;
                        webApp.Update();
                    }
                }
                catch(Exception ex)
                {
                    Trace.WriteLine("RemoveBasicFeature: " + ex);
                }
            }
            return selectedWebApps;
        }

        private static void AddBasicFeature(SPWebApplication webApp)
        {
            try
            {
                Guid basicFeatureId = new Guid(BasicFeatureId);
                //Deactive NextLabs.Entitlement.Basic.feature
                SPFeature basicFeature = webApp.Features[basicFeatureId];
                if (basicFeature == null)
                {
                    bool formDigestSetting = webApp.FormDigestSettings.Enabled;
                    webApp.FormDigestSettings.Enabled = false;
                    try
                    {
                        webApp.Features.Add(basicFeatureId);
                    }
                    catch (Exception exp)
                    {
                        Trace.WriteLine("Add BasicFeature: " + exp);
                    }
                    webApp.FormDigestSettings.Enabled = formDigestSetting;
                    webApp.Update();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("AddBasicFeature: " + ex);
            }
        }

        public static void UpgradeRestore(List<SPWebApplication> selectedWebApps)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                //Add basic feature after features deployment.
                for (int i = 0; i < selectedWebApps.Count; i++)
                {
                    AddBasicFeature(selectedWebApps[i]);
                }

            });
        }
    }
}
