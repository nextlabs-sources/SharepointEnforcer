using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using Microsoft.SharePoint;
using Microsoft.Win32;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class SPSecurityTrimmingManager:IDisposable
    {
        private SPSite SiteCollection;
        private SPWeb RootWeb;

        static StringDictionary ListTrimmingProperties = new StringDictionary();
        static Object LockObj = new Object();

        public SPSecurityTrimmingManager()
        {
            SiteCollection = null;
            RootWeb = null;
        }

        public SPSecurityTrimmingManager(SPSite site)
        {
            SiteCollection = site;
            RootWeb = SiteCollection.RootWeb;
        }

        public void Dispose()
        {
        }
        public void SetCacheTime(int minutes)
        {
            EvaluationCache.TimeOutInterval = new TimeSpan(0, minutes, 0);
            EvaluationCache.Instance.ClearTimeOut();

            CacheMonitorSchedule.Instance.UpdateTimer();
        }

        public void ClearCache()
        {
            EvaluationUserCache.Instance.Clear();
            EvaluationCache.Instance.Clear();
        }

        public void Enable(bool bMasterPage)
        {
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        try
                        {
                            RootWeb.Properties["spsecuritytrimming"] = "enabled";
                            RootWeb.Properties.Update();
                        }
                        catch
                        {
                            using (SPSite _site = new SPSite(RootWeb.Url))
                            {
                                using (SPWeb _RootWeb = _site.OpenWeb())
                                {
                                    _RootWeb.Properties["spsecuritytrimming"] = "enabled";
                                    _RootWeb.Properties.Update();
                                }
                            }
                        }
                    });
                }

                if (bMasterPage)
                    AddDelegateControlToMasterPages();
                else
                    AddDelegateControlToLayoutPages();

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Enable SharePoint Security Trimming:", null, ex);
            }
        }

        public void EnableInPublishingConsole()
        {
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        try
                        {
                            RootWeb.Properties["spsecuritytrimming"] = "enabled";
                            RootWeb.Properties.Update();
                        }
                        catch
                        {
                            using (SPSite _site = new SPSite(RootWeb.Url))
                            {
                                using (SPWeb _RootWeb = _site.OpenWeb())
                                {
                                    _RootWeb.Properties["spsecuritytrimming"] = "enabled";
                                    _RootWeb.Properties.Update();
                                }
                            }
                        }

                    });
                }

                AddDelegateControlToPublishingConsole();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Enable SharePoint Security Trimming In PublishingConsole:", null, ex);
            }
        }

        public void Disable()
        {

            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        try
                        {
                            RootWeb.Properties["spsecuritytrimming"] = "disabled";
                            RootWeb.Properties.Update();
                        }
                        catch
                        {
                            using (SPSite _site = new SPSite(RootWeb.Url))
                            {
                                using (SPWeb _RootWeb = _site.OpenWeb())
                                {
                                    _RootWeb.Properties["spsecuritytrimming"] = "disabled";
                                    _RootWeb.Properties.Update();
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Disable SharePoint Security Trimming:", null, ex);
            }
            DeactivateListTrimming();
            DeactivateListPrefilterTrimming();
            DeactivateTabTrimming();
            DeactivateWebpartTrimming();
            DeactivatePageTrimming();
            DeactivateFastSearchTrimming();
        }

        public void Remove()
        {
            try
            {
                if (SiteCollection != null)
                {
                    Disable();
                    RemoveDelegateControlFromMasterPages();
                    RemoveDelegateControlFromLayoutPages();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Remove SharePoint Security Trimming:", null, ex);
            }
        }

        public bool CheckSecurityTrimming()
        {
            bool bRet = false;
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        //Using privileges to create site and web.
                        using (SPSite site = new SPSite(SiteCollection.Url))
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                string status = web.Properties["spsecuritytrimming"];
                                if (status != null && status.Equals("enabled"))
                                {
                                    bRet = true;
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Check SharePoint Security Trimming:", null, ex);
            }

            return bRet;
        }

        public void ActivateSearchPrefilterTrimming()
        {
            ActivateTrimming("spsearchprefiltertrimming");
        }

        public void DeactivateSearchPrefilterTrimming()
        {
            DeactivateTrimming("spsearchprefiltertrimming");
        }

        public void ActivateListTrimming()
        {
            ActivateTrimming("splisttrimming");
        }

        public void DeactivateListTrimming()
        {
            DeactivateTrimming("splisttrimming");
        }

        public void ActivateListPrefilterTrimming()
        {
            ActivateTrimming("splistprefiltertrimming");
        }

        public void DeactivateListPrefilterTrimming()
        {
            DeactivateTrimming("splistprefiltertrimming");
        }

        public void ActivateTabTrimming()
        {
            ActivateTrimming("sptabtrimming");
        }

        public void DeactivateTabTrimming()
        {
            DeactivateTrimming("sptabtrimming");
        }

        public void ActivateWebpartTrimming()
        {
            ActivateTrimming("spwebparttrimming");
        }

        public void DeactivateWebpartTrimming()
        {
            DeactivateTrimming("spwebparttrimming");
        }

        public void ActivatePageTrimming()
        {
            ActivateTrimming("sppagetrimming");
        }

        public void DeactivatePageTrimming()
        {
            DeactivateTrimming("sppagetrimming");
        }

        public void ActivateFastSearchTrimming()
        {
            ActivateTrimming("spfastsearchtrimming");
        }

        public void DeactivateFastSearchTrimming()
        {
            DeactivateTrimming("spfastsearchtrimming");
        }

        public void ActivateTrimming(string trimer)
        {
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        try
                        {
                            RootWeb.Properties[trimer] = "activated";
                            RootWeb.Properties.Update();
                        }
                        catch
                        {
                            using (SPSite _site = new SPSite(RootWeb.Url))
                            {
                                using (SPWeb _RootWeb = _site.OpenWeb())
                                {
                                    _RootWeb.Properties[trimer] = "activated";
                                    _RootWeb.Properties.Update();
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Activate SharePoint Security Trimming:", null, ex);
            }
        }

        public void DeactivateTrimming(string trimer)
        {
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        try
                        {
                            RootWeb.Properties[trimer] = "deactivated";
                            RootWeb.Properties.Update();
                        }
                        catch
                        {
                            using (SPSite _site = new SPSite(RootWeb.Url))
                            {
                                using (SPWeb _RootWeb = _site.OpenWeb())
                                {
                                    _RootWeb.Properties[trimer] = "deactivated";
                                    _RootWeb.Properties.Update();
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Deactivate SharePoint Security Trimming:", null, ex);
            }
        }

        public bool CheckSearchPrefilterTrimming()
        {
            return CheckTrimming("spsearchprefiltertrimming");
        }

        public bool CheckListTrimming()
        {
            return CheckTrimming("splisttrimming");
        }

        public bool CheckListPrefilterTrimming()
        {
            return CheckTrimming("splistprefiltertrimming");
        }

        public bool CheckTabTrimming()
        {
            return CheckTrimming("sptabtrimming");
        }

        public bool CheckWebpartTrimming()
        {
            return CheckTrimming("spwebparttrimming");
        }

        public bool CheckPageTrimming()
        {
            return CheckTrimming("sppagetrimming");
        }

        public bool CheckFastSearchTrimming()
        {
            return CheckTrimming("spfastsearchtrimming");
        }

        private bool CheckTrimming(string trimmer)
        {
            bool bRet = false;
            try
            {
                if (SiteCollection != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        //Using privileges to create site and web.
                        using (SPSite site = new SPSite(SiteCollection.Url))
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                string status = web.Properties[trimmer];
                                if (status != null && status.Equals("activated"))
                                {
                                    bRet = true;
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Check SharePoint List Trimming:", null, ex);
            }

            return bRet;
        }

        public void EnableListTrimming()
        {
            ActivateListTrimming();

            foreach (SPWeb web in SiteCollection.AllWebs)
            {
                try
                {
                    SPListCollection lists = web.Lists;
                    foreach (SPList list in lists)
                    {
                        EnableListTrimming(list);
                    }
                }
                catch
                {
                }
                finally
                {
                    web.Dispose();
                }
            }
        }

        public void DisableListTrimming()
        {
            DeactivateListTrimming();

            foreach (SPWeb web in SiteCollection.AllWebs)
            {
                try
                {
                    SPListCollection lists = web.Lists;
                    foreach (SPList list in lists)
                    {
                        DisableListTrimming(list);
                    }
                }
                catch
                {
                }
                finally
                {
                    web.Dispose();
                }
            }
        }

        public void EnableListTrimming(SPList list)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties["splisttrimming"] = "enable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Enable SharePoint List Trimming:", null, ex);
            }
        }

        public void DisableListTrimming(SPList list)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties["splisttrimming"] = "disable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Disable SharePoint List Trimming:", null, ex);
            }
        }

        public void EnableListPrefilterTrimming(SPList list)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties["splistprefiltertrimming"] = "enable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Enable SharePoint List Prefilter Trimming:", null, ex);
            }
        }

        public void DisableListPrefilterTrimming(SPList list)
        {
            try
            {
                if (list != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        list.RootFolder.Properties["splistprefiltertrimming"] = "disable";
                        list.RootFolder.Update();
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Disable SharePoint List Prefilter Trimming:", null, ex);
            }
        }

        public bool CheckListTrimming(SPList list)
        {
            bool bRet = false;

            try
            {
                if (list != null)
                {
                    String status = null;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        status = list.RootFolder.Properties["splisttrimming"] as String;
                    });
                    if (status != null && status.Equals("enable"))
                    {
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Check SharePoint List Trimming:", null, ex);
            }
            return bRet;
        }

        public bool CheckListPrefilterTrimming(SPList list)
        {
            bool bRet = false;

            try
            {
                if (list != null)
                {
                    String status = null;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        status = list.RootFolder.Properties["splistprefiltertrimming"] as String;
                    });
                    if (status != null && status.Equals("enable"))
                    {
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Check SharePoint List Pre-filter Trimming:", null, ex);
            }
            return bRet;
        }

        public bool CheckMasterPageCheckedOutStatus(bool bMasterPage, out string url, out string checkedoutby)
        {
            bool bCheckedOut = false;

            url = "";
            checkedoutby = "";

            try
            {
                if (SiteCollection != null)
                {
                    {
                        SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

                        SPListItemCollection items = list.Items;
                        foreach (SPListItem _item in items)
                        {
                            if ((bMasterPage && _item.Name.IndexOf(".master", StringComparison.OrdinalIgnoreCase) > 0)
                                || (!bMasterPage && _item.Name.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase) > 0))
                            {
                                if (_item.File.CheckOutType != SPFile.SPCheckOutType.None)
                                {
                                    bCheckedOut = true;
                                    url = _item.Url;
                                    checkedoutby = _item.File.CheckedOutByUser.LoginName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during Check master page status:", null, ex);
            }

            return bCheckedOut;
        }

        public int GetNonCEDelegateControlMasterPageCount()
        {
            int iCount = 0;

            if (SiteCollection != null)
            {
                {
                    SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);
                    bool bContained = false;

                    SPListItemCollection items = list.Items;
                    foreach (SPListItem _item in items)
                    {
                        try
                        {
                            if (_item.Name.IndexOf(".master", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                bContained = IsDelegateControlContainedInMasterPage(_item);
                                if (!bContained)
                                {
                                    iCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during GetNoCEDelegate:", null, ex);
                        }
                    }
                }
            }
            return iCount;
        }

        private bool IsDelegateControlContainedInMasterPage(SPListItem item)
        {
            string strDelegateControlKeyFormat = "<{0}:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            SPFile file = null;
            bool bContained = false;

            try
            {
                if (item != null && item.File != null)
                {
                    file = item.File;

                    byte[] _data = file.OpenBinary();
                    string _text = System.Text.Encoding.Default.GetString(_data);

                    string tagPrefix = GetSPWebControlsTagPrefix(_text);
                    if (tagPrefix == null)
                    {
                        return bContained;
                    }
                    string strDelegateControlKey = String.Format(strDelegateControlKeyFormat, tagPrefix);

                    if (_text.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        bContained = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during IsDelegateControl:", null, ex);
            }

            return bContained;
        }

        public void AddDelegateControlToMasterPages()
        {
            if (SiteCollection != null)
            {
                {
                    SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

                    SPListItemCollection items = list.Items;
                    foreach (SPListItem _item in items)
                    {
                        try
                        {
                            if (_item.Name.IndexOf(".master", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                string url = SiteCollection.Url + "/" + _item.Url;
                                AddDelegateControl(_item, url, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during AddDelegateControl:", null, ex);
                        }
                    }
                }
            }
        }

        public void RemoveDelegateControlFromMasterPages()
        {
            if (SiteCollection != null)
            {
                {
                    SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

                    SPListItemCollection items = list.Items;
                    foreach (SPListItem _item in items)
                    {
                        try
                        {
                            if (_item.Name.IndexOf(".master", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                string url = SiteCollection.Url + "/" + _item.Url;
                                RemoveDelegateControl(_item, url, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during RemoveDeegateConrol:", null, ex);
                        }
                    }
                }
            }

        }

        public void AddDelegateControlToLayoutPages()
        {
            if (SiteCollection != null)
            {
                {
                    SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

                    SPListItemCollection items = list.Items;
                    foreach (SPListItem _item in items)
                    {
                        try
                        {
                            if (_item.Name.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                string url = SiteCollection.Url + "/" + _item.Url;
                                AddDelegateControl(_item, url, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during AddDelegateControltoLayout:", null, ex);
                        }
                    }
                }
            }
        }

        public void RemoveDelegateControlFromLayoutPages()
        {
            if (SiteCollection != null)
            {
                {
                    SPList list = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

                    SPListItemCollection items = list.Items;
                    foreach (SPListItem _item in items)
                    {
                        try
                        {
                            if (_item.Name.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                string url = SiteCollection.Url + "/" + _item.Url;
                                RemoveDelegateControl(_item, url, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Warn, "Exception during RemoveDelegateConrolFromLayout:", null, ex);
                        }
                    }
                }
            }
        }

        private void AddDelegateControlToWelcome()
        {
            string strDelegateControlKey = "<SharePoint:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            string strSPServerTemplatePath = SPSecurityTrimmingManager.GetSPTemplatePath();
            if (!strSPServerTemplatePath.EndsWith("\\"))
            {
                strSPServerTemplatePath += "\\";
            }
            string strWelcomePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\Welcome.ascx";

            if (!File.Exists(strWelcomePath))
            {
                return;
            }
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                FileInfo fileInfo = new FileInfo(strWelcomePath);
                FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read);
                StreamReader reader = new StreamReader(fs);
                string strContent = reader.ReadToEnd();
                reader.Close();
                fs.Close();


                if (strContent.IndexOf(strDelegateControlKey,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    string strBackupFileName = fileInfo.FullName;
                    strBackupFileName += "_bak";
                    fileInfo.CopyTo(strBackupFileName, true);

                    fs = fileInfo.Open(FileMode.Create, FileAccess.ReadWrite);
                    StreamWriter writer = new StreamWriter(fs);
                    int insertPos = strContent.IndexOf("<SharePoint:PersonalActions", StringComparison.OrdinalIgnoreCase);
                    if (-1 != insertPos)
                    {
                        writer.Write(strContent.Substring(0, insertPos));
                        writer.WriteLine(strDelegateControlKey);
                        writer.Write(strContent.Substring(insertPos));
                    }
                    writer.Close();
                    fs.Close();
                }
            });
        }

        private void RemoveDelegateControlFromWelcome()
        {
            string strDelegateControlKey = "<SharePoint:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            string strSPServerTemplatePath = SPSecurityTrimmingManager.GetSPTemplatePath();
            if (!strSPServerTemplatePath.EndsWith("\\"))
            {
                strSPServerTemplatePath += "\\";
            }
            string strWelcomePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\Welcome.ascx";

            if (!File.Exists(strWelcomePath))
            {
                return;
            }
            FileInfo fileInfo = new FileInfo(strWelcomePath);
            FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(fs);
            string strContent = reader.ReadToEnd();
            reader.Close();
            fs.Close();

            int delPos = strContent.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase);

            if (delPos > 0)
            {
                string strBackupFileName = fileInfo.FullName;
                strBackupFileName += "_bak";

                FileInfo fiBackup = new FileInfo(strBackupFileName);
                if (fiBackup.Exists)
                {
                    string strFullName = fileInfo.FullName;
                    fileInfo.Delete();
                    fiBackup.MoveTo(strFullName);
                }
                else
                {
                    fs = fileInfo.Open(FileMode.Create, FileAccess.ReadWrite);
                    StreamWriter writer = new StreamWriter(fs);

                    writer.Write(strContent.Substring(0, delPos));
                    writer.Write(strContent.Substring(delPos + strDelegateControlKey.Length));
                    writer.Close();
                    fs.Close();
                }
            }
        }

        public void AddDelegateControlToPublishingConsole()
        {

            //when sp2013 is upgraded from sp2010, in this case we should both need to add delegatecontrol to 15\..\welcome.ascx and 14\..\PublishingConsole.ascx
            AddDelegateControlToWelcome();
            string strDelegateControlKey = "<SharePoint:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            string strSPServerTemplatePath = SPSecurityTrimmingManager.GetSPTemplatePath();
            if (!strSPServerTemplatePath.EndsWith("\\"))
            {
                strSPServerTemplatePath += "\\";
            }
#if SP2013
            strSPServerTemplatePath = strSPServerTemplatePath.Replace("15", "14");
            string strPublishingConsolePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\PublishingConsole.ascx";
#else
            string strPublishingConsolePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\PublishingConsole.ascx";
#endif

            if (!File.Exists(strPublishingConsolePath))
            {
                return;
            }
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                FileInfo fileInfo = new FileInfo(strPublishingConsolePath);
                FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read);
                StreamReader reader = new StreamReader(fs);
                string strContent = reader.ReadToEnd();
                reader.Close();
                fs.Close();
                if (strContent.IndexOf(strDelegateControlKey,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    string strBackupFileName = fileInfo.FullName;
                    strBackupFileName += "_bak";
                    fileInfo.CopyTo(strBackupFileName, true);
                    fs = fileInfo.Open(FileMode.Create, FileAccess.ReadWrite);
                    StreamWriter writer = new StreamWriter(fs);
                    int insertPos = strContent.IndexOf("<SharePoint:UIVersionedContent", StringComparison.OrdinalIgnoreCase);
                    if (-1 != insertPos)
                    {
                        writer.Write(strContent.Substring(0, insertPos));
                        writer.WriteLine(strDelegateControlKey);
                        writer.Write(strContent.Substring(insertPos));
                    }
                    writer.Close();
                    fs.Close();
                }
            });
        }

        public void RemoveDelegateControlFromPublishingConsole()
        {

            //when sp2013 is upgraded from sp2010, in this case we should both need to remove delegatecontrol from 15\..\welcome.ascx and 14\..\PublishingConsole.ascx
            RemoveDelegateControlFromWelcome();
            string strDelegateControlKey = "<SharePoint:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            string strSPServerTemplatePath = SPSecurityTrimmingManager.GetSPTemplatePath();
            if (!strSPServerTemplatePath.EndsWith("\\"))
            {
                strSPServerTemplatePath += "\\";
            }

#if SP2013
            strSPServerTemplatePath = strSPServerTemplatePath.Replace("15", "14");
            string strPublishingConsolePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\PublishingConsole.ascx";

#else
            string strPublishingConsolePath = strSPServerTemplatePath + "CONTROLTEMPLATES\\PublishingConsole.ascx";
#endif
            if (!File.Exists(strPublishingConsolePath))
            {
                return;
            }
            FileInfo fileInfo = new FileInfo(strPublishingConsolePath);
            FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(fs);
            string strContent = reader.ReadToEnd();
            reader.Close();
            fs.Close();

            int delPos = strContent.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase);

            if (delPos > 0)
            {
                string strBackupFileName = fileInfo.FullName;
                strBackupFileName += "_bak";

                FileInfo fiBackup = new FileInfo(strBackupFileName);
                if (fiBackup.Exists)
                {
                    string strFullName = fileInfo.FullName;
                    fileInfo.Delete();
                    fiBackup.MoveTo(strFullName);
                }
                else
                {
                    fs = fileInfo.Open(FileMode.Create, FileAccess.ReadWrite);
                    StreamWriter writer = new StreamWriter(fs);

                    writer.Write(strContent.Substring(0, delPos));
                    writer.Write(strContent.Substring(delPos + strDelegateControlKey.Length));
                    writer.Close();
                    fs.Close();
                }
            }
        }

        private void AddDelegateControl(SPListItem masterPageItem, string strMasterUrl, bool bMasterPage)
        {
            SPFile masterPageFile = null;
            string strDelegateControlKeyFormat = "<{0}:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            SPList masterPageGalleryList = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

            if (masterPageItem != null)
            {
                masterPageFile = masterPageItem.File;
                // Discard already checked out pages
                if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                {
                    masterPageFile.UndoCheckOut();
                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    masterPageFile = masterPageItem.File;
                }

                SPFile approvedMasterPageFile = masterPageItem.File;
                SPFile currentNotApprovedMaterPageFile = null;
                SPFileVersion approvedMasterPageFileVersion = null;
                string currentNotApprovedVersionLabel = null;
                string currentApprovedVersionLabel = null;

                SPModerationStatusType status = SPModerationStatusType.Approved;

                if (masterPageItem.ModerationInformation != null)
                {
                    // Get the approved master page version
                    status = masterPageItem.ModerationInformation.Status;

                    // This section code is used for Installation Tool. Not used in HttpModule.
                    if (status != SPModerationStatusType.Approved)
                    {
                        //currentNotApprovedMaterPageItem = masterPageItem;
                        currentNotApprovedMaterPageFile = masterPageFile;
                        currentNotApprovedVersionLabel = masterPageItem.Versions[0].VersionLabel;
                    }


                    SPFileVersionCollection fileVersions = masterPageFile.Versions;
                    SPFileVersion fileVersion = null;
                    for (int i = fileVersions.Count - 1; i >= 0; i--)
                    {
                        fileVersion = fileVersions[i];
                        if (fileVersion.Level == SPFileLevel.Published && fileVersion.IsCurrentVersion)
                        {
                            approvedMasterPageFileVersion = fileVersion;
                            currentApprovedVersionLabel = fileVersion.VersionLabel;
                            break;
                        }
                    }
                }

                if (currentNotApprovedMaterPageFile != null && approvedMasterPageFileVersion != null)
                {
                    // Restore latest approved version
                    masterPageFile.CheckOut();
                    masterPageFile.Versions.RestoreByLabel(currentApprovedVersionLabel);
                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    AddDelegateControl2(masterPageItem, strMasterUrl, SPModerationStatusType.Approved, bMasterPage);
                    if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                    {
                        masterPageFile.UndoCheckOut();
                    }

                    // Restore current non-approved version
                    if (masterPageGalleryList.EnableVersioning)
                    {
                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        masterPageFile = masterPageItem.File;
                        masterPageFile.CheckOut();
                        masterPageFile.Versions.RestoreByLabel(currentNotApprovedVersionLabel);
                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        AddDelegateControl2(masterPageItem, strMasterUrl, status, bMasterPage);
                        if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                        {
                            masterPageFile.UndoCheckOut();
                        }
                    }
                }
                else
                {
                    byte[] _data = masterPageFile.OpenBinary();
                    string _text = System.Text.Encoding.Default.GetString(_data);

                    string tagPrefix = GetSPWebControlsTagPrefix(_text);
                    if (tagPrefix == null)
                    {
                        return;
                    }

                    string strDelegateControlKey = String.Format(strDelegateControlKeyFormat, tagPrefix);

                    if (_text.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        AddDelegateControl2(masterPageItem, strMasterUrl, SPModerationStatusType.Approved, bMasterPage);
                    }
                }
            }
        }

        private void AddDelegateControl2(SPListItem masterPageItem, string strMasterUrl, SPModerationStatusType status, bool bMasterPage)
        {
            string strDelegateControlKeyFormat = "<{0}:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            SPFile masterPageFile = null;

            if (masterPageItem != null && masterPageItem.File != null)
            {
                masterPageFile = masterPageItem.File;

                byte[] _data = masterPageFile.OpenBinary();
                string _text = System.Text.Encoding.Default.GetString(_data);

                string tagPrefix = GetSPWebControlsTagPrefix(_text);
                if (tagPrefix == null)
                {
                    return;
                }

                string strDelegateControlKey = String.Format(strDelegateControlKeyFormat, tagPrefix);

                if (_text.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    int iHeadEndTag = 0;
                    if (bMasterPage)
                    {
                        iHeadEndTag = _text.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        iHeadEndTag = _text.IndexOf("</asp:Content>", StringComparison.OrdinalIgnoreCase);
                    }
                    if (iHeadEndTag > 0)
                    {
                        string _newText = _text.Substring(0, iHeadEndTag);
                        _newText += strDelegateControlKey;
                        _newText += "\n";
                        _newText += _text.Substring(iHeadEndTag);

                        byte[] _newData = System.Text.Encoding.Default.GetBytes(_newText);

                        if (masterPageFile.CheckOutType == SPFile.SPCheckOutType.None)
                            masterPageFile.CheckOut();
                        masterPageFile.SaveBinary(_newData);

                        string strCheckInComment = "This modification was made by Compliant Enterprise installer automatically to enable web part trimming feature. From more information, please refer to the NextLabs Compliant Enterprise Product Documentation.";

                        SPContentTypeId cTypeId = masterPageItem.ContentType.Id;
                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        masterPageItem["ContentTypeId"] = cTypeId;
                        masterPageItem.Update();

                        masterPageFile.CheckIn(strCheckInComment);

                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        if (masterPageItem.ModerationInformation != null)
                        {
                            // Recover original status
                            masterPageItem.ModerationInformation.Status = status;
                            masterPageItem.Update();
                        }

                        string message = "";
                        if (bMasterPage)
                            message = "Modify Master Page ";
                        else
                            message = "Modify Layout Page ";
                        message += masterPageItem.Url;
                        message += " Version ";
                        message += (masterPageItem.File.MajorVersion.ToString() + "." + masterPageItem.File.MinorVersion.ToString());

                        NLLogger.OutputLog(LogLevel.Debug, message);
                    }
                }
            }
        }

        private void RemoveDelegateControl(SPListItem masterPageItem, string strMasterUrl, bool bMasterPage)
        {
            SPFile masterPageFile = null;
            string strDelegateControlKeyFormat = "<{0}:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            SPList masterPageGalleryList = RootWeb.GetCatalog(SPListTemplateType.MasterPageCatalog);

            if (masterPageItem != null)
            {
                masterPageFile = masterPageItem.File;
                // Discard already checked out pages
                if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                {
                    masterPageFile.UndoCheckOut();
                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    masterPageFile = masterPageItem.File;
                }

                SPFile approvedMasterPageFile = masterPageItem.File;
                SPFile currentNotApprovedMaterPageFile = null;
                SPFileVersion approvedMasterPageFileVersion = null;
                string currentNotApprovedVersionLabel = null;
                string currentApprovedVersionLabel = null;

                SPModerationStatusType status = SPModerationStatusType.Approved;

                if (masterPageItem.ModerationInformation != null)
                {
                    // Get the approved master page version
                    status = masterPageItem.ModerationInformation.Status;

                    // This section code is used for Installation Tool. Not used in HttpModule.
                    if (status != SPModerationStatusType.Approved)
                    {
                        //currentNotApprovedMaterPageItem = masterPageItem;
                        currentNotApprovedMaterPageFile = masterPageFile;
                        currentNotApprovedVersionLabel = masterPageItem.Versions[0].VersionLabel;
                    }


                    SPFileVersionCollection fileVersions = masterPageFile.Versions;
                    SPFileVersion fileVersion = null;
                    for (int i = fileVersions.Count - 1; i >= 0; i--)
                    {
                        fileVersion = fileVersions[i];
                        if (fileVersion.Level == SPFileLevel.Published && fileVersion.IsCurrentVersion)
                        {
                            approvedMasterPageFileVersion = fileVersion;
                            currentApprovedVersionLabel = fileVersion.VersionLabel;
                            break;
                        }
                    }
                }

                if (currentNotApprovedMaterPageFile != null && approvedMasterPageFileVersion != null)
                {
                    // Restore latest approved version
                    masterPageFile.CheckOut();
                    masterPageFile.Versions.RestoreByLabel(currentApprovedVersionLabel);
                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    RemoveDelegateControl2(masterPageItem, strMasterUrl, SPModerationStatusType.Approved, bMasterPage);
                    if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                    {
                        masterPageFile.UndoCheckOut();
                    }

                    // Restore current non-approved version
                    if (masterPageGalleryList.EnableVersioning)
                    {
                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        masterPageFile = masterPageItem.File;
                        masterPageFile.CheckOut();
                        masterPageFile.Versions.RestoreByLabel(currentNotApprovedVersionLabel);
                        masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                        RemoveDelegateControl2(masterPageItem, strMasterUrl, status, bMasterPage);
                        if (masterPageFile.CheckOutType != SPFile.SPCheckOutType.None)
                        {
                            masterPageFile.UndoCheckOut();
                        }
                    }
                }
                else
                {
                    byte[] _data = masterPageFile.OpenBinary();
                    string _text = System.Text.Encoding.Default.GetString(_data);

                    string tagPrefix = GetSPWebControlsTagPrefix(_text);
                    if (tagPrefix == null)
                    {
                        return;
                    }

                    string strDelegateControlKey = String.Format(strDelegateControlKeyFormat, tagPrefix);

                    if (_text.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        RemoveDelegateControl2(masterPageItem, strMasterUrl, SPModerationStatusType.Approved, bMasterPage);
                    }
                }
            }
        }

        private void RemoveDelegateControl2(SPListItem masterPageItem, string strMasterUrl, SPModerationStatusType status, bool bMasterPage)
        {
            string strDelegateControlKeyFormat = "<{0}:DelegateControl runat=\"server\" ControlId=\"ComplianceControl\" AllowMultipleControls=\"true\"/>";
            SPFile masterPageFile = null;

            if (masterPageItem != null && masterPageItem.File != null)
            {
                masterPageFile = masterPageItem.File;

                byte[] _data = masterPageFile.OpenBinary();
                string _text = System.Text.Encoding.Default.GetString(_data);

                string tagPrefix = GetSPWebControlsTagPrefix(_text);
                if (tagPrefix == null)
                {
                    return;
                }

                string strDelegateControlKey = String.Format(strDelegateControlKeyFormat, tagPrefix);

                int iDelegateControlPos = _text.IndexOf(strDelegateControlKey, StringComparison.OrdinalIgnoreCase);
                if (iDelegateControlPos > 0)
                {
                    string _newText = _text.Substring(0, iDelegateControlPos);
                    if (_text[iDelegateControlPos + strDelegateControlKey.Length] == '\n')
                        _newText += _text.Substring(iDelegateControlPos + strDelegateControlKey.Length + 1);
                    else
                        _newText += _text.Substring(iDelegateControlPos + strDelegateControlKey.Length);

                    byte[] _newData = System.Text.Encoding.Default.GetBytes(_newText);

                    if (masterPageFile.CheckOutType == SPFile.SPCheckOutType.None)
                        masterPageFile.CheckOut();
                    masterPageFile.SaveBinary(_newData);

                    string strCheckInComment = "This modification was made by Compliant Enterprise installer automatically to uninstall web part trimming feature. From more information, please refer to the NextLabs Compliant Enterprise Product Documentation.";

                    SPContentTypeId cTypeId = masterPageItem.ContentType.Id;
                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    masterPageItem["ContentTypeId"] = cTypeId;
                    masterPageItem.Update();

                    masterPageFile.CheckIn(strCheckInComment);

                    masterPageItem = (SPListItem)RootWeb.GetObject(strMasterUrl);
                    if (masterPageItem.ModerationInformation != null)
                    {
                        // Recover original status
                        masterPageItem.ModerationInformation.Status = status;
                        masterPageItem.Update();
                    }

                    string message = "";
                    if (bMasterPage)
                        message = "Modify Master Page ";
                    else
                        message = "Modify Layout Page ";
                    message += masterPageItem.Url;
                    message += " Version ";
                    message += (masterPageItem.File.MajorVersion.ToString() + "." + masterPageItem.File.MinorVersion.ToString());

                    NLLogger.OutputLog(LogLevel.Debug, message);
                }
            }
        }

        private string GetSPWebControlsTagPrefix(string content)
        {
            string tagPrefix = null;

            if (content == null)
                return tagPrefix;

            int endPos = content.IndexOf("Namespace=\"Microsoft.SharePoint.WebControls\"");
            if (endPos <= 0)
                return tagPrefix;

            string subContent1 = content.Substring(0, endPos);
            string keyWords = "Tagprefix=\"";
            int startPos = subContent1.LastIndexOf(keyWords);
            if (startPos <= 0)
                return tagPrefix;

            string subContent2 = subContent1.Substring(startPos + keyWords.Length);
            endPos = subContent2.IndexOf("\"");
            if (endPos <= 0)
                return tagPrefix;

            tagPrefix = subContent2.Substring(0, endPos);

            return tagPrefix;
        }

        public static string GetSPTemplatePath()
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
    }
}
