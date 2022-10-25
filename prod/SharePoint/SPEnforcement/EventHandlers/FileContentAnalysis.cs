using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint;
using System.Security.Principal;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public delegate void PreContentAnalysisEventDelegate(object sender, EventArgs e);
    public delegate void PostContentAnalysisEventDelegate(object sender, EventArgs e);

    public class ContentAnalysisResult
    {
        private SPListItem m_ListItem;
        private string m_FileUrl;
        private List<KeyValuePair<string, string>> m_ModifiedFields;
        private List<string> m_FailedReason;
        public SPListItem ListItem
        {
            get { return m_ListItem; }
            set { m_ListItem = value; }
        }

        public string FileUrl
        {
            get { return m_FileUrl; }
            set { m_FileUrl = value; }
        }

        public List<KeyValuePair<string, string>> ModifiedFields
        {
            get { return m_ModifiedFields; }
        }

        public List<string> FailedReason
        {
            get { return m_FailedReason; }
        }

        public ContentAnalysisResult()
        {
            m_ListItem = null;
            m_ModifiedFields = new List<KeyValuePair<string, string>>();
            m_FailedReason = new List<string>();
        }
    }

    public class FileContentAnalysis
    {
        public const string ContentAnalysisIndicationKey = "ce::nativeresname";

        private SPWeb m_Web;
        private SPList m_List;
        private SPListItem m_ListItem;
        private string m_FileUrl;
        private string m_ClientIP;
        private bool m_Batch;

        private string m_UserName;
        private string m_UserSid;

        private string m_FilePath;
        private ContentAnalysisResult m_Result;
        private IPrincipal m_PrincipalUser;

        private bool m_bCADenied;
        public bool CADenied
        {
            get { return m_bCADenied; }
            set { m_bCADenied = value; }
        }

        public ContentAnalysisResult Result
        {
            get { return m_Result; }
        }

        public event PreContentAnalysisEventDelegate PreContentAnalysisEventHandler;
        public event PostContentAnalysisEventDelegate PostContentAnalysisEventHandler;

        public FileContentAnalysis(SPListItem item, string fileUrl, string clientIP, IPrincipal PrincipalUser, bool bBatch)
        {
            m_ListItem = item;
            m_List = item.ParentList;
            m_Web = m_List.ParentWeb;
            m_FileUrl = fileUrl;
            m_ClientIP = clientIP;
            m_Batch = bBatch;

            m_Result = new ContentAnalysisResult();
            m_Result.ListItem = item;
            m_Result.FileUrl = fileUrl;

            m_UserName = null;
            m_UserSid = null;
            m_PrincipalUser = PrincipalUser;
        }

        public FileContentAnalysis(SPListItem item, string fileUrl, string userName, string userSid, string clientIP,  IPrincipal PrincipalUser, bool bBatch)
        {
            m_ListItem = item;
            m_List = item.ParentList;
            m_Web = m_List.ParentWeb;
            m_FileUrl = fileUrl;
            m_ClientIP = clientIP;
            m_Batch = bBatch;

            m_Result = new ContentAnalysisResult();
            m_Result.ListItem = item;
            m_Result.FileUrl = fileUrl;

            m_UserName = userName;
            m_UserSid = userSid;
            m_PrincipalUser = PrincipalUser;
        }

        private bool CheckItemCheckOutByOther()
        {
            try
            {
                if (m_List.BaseType == SPBaseType.DocumentLibrary && m_Batch
                        && m_ListItem.File.CheckOutType != SPFile.SPCheckOutType.None
                        && !m_ListItem.Web.CurrentUser.Name.Equals(m_ListItem.File.CheckedOutByUser.Name))
                {
                    string failed = "Failed. Reason: The file is checked out for editing by " + m_ListItem.File.CheckedOutByUser.Name;
                    m_Result.FailedReason.Add(failed);
                    NLLogger.OutputLog(LogLevel.Debug, "FileContentAnalysis: " + failed);
                    if (PostContentAnalysisEventHandler != null)
                        PostContentAnalysisEventHandler(this, new EventArgs());
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public void Run()
        {
            try
            {
                if (m_ListItem == null || String.IsNullOrEmpty(m_FileUrl))
                    return;


                if (PreContentAnalysisEventHandler != null)
                    PreContentAnalysisEventHandler(this, new EventArgs());
                if (CheckItemCheckOutByOther())
                {
                    return;
                }

                // Save file to temp
                string strFileExtension = GetFileExtensionFromFileUrl(m_FileUrl);
                bool bIsSupportType = Globals.IsSupportFileType(strFileExtension);
                if (bIsSupportType)
                {
                    m_FilePath = SaveFileToTemp(m_Web, m_FileUrl);
                    if (String.IsNullOrEmpty(m_FilePath))
                    {
                        string failed = "Warning - Can not access file: " + m_FileUrl;
                        m_Result.FailedReason.Add(failed);

                        NLLogger.OutputLog(LogLevel.Error, "FileContentAnalysis: " + failed);
                        return;
                    }
                    NLLogger.OutputLog(LogLevel.Debug, "FileContentAnalysis: URL=" + m_FileUrl + " TempFile=" + m_FilePath);
                }
                else
                {
                    m_FilePath = "";
                    NLLogger.OutputLog(LogLevel.Debug, String.Format("Current file with url:[{0}], extension:[{1}] do not need content analysis FileContentAnalysis: URL:{0}", m_FileUrl, strFileExtension));
                }

                // TODO Evaluation
                EvaluationEdit(m_Batch);

                DeleteTempFile();

                if (PostContentAnalysisEventHandler != null)
                    PostContentAnalysisEventHandler(this, new EventArgs());
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during FileContentAnalysis: ", null, ex);
            }
        }

        static private string GetFileExtensionFromFileUrl(string strFileUrl)
        {
            string strExtension = "";
            int nLastDotPos = strFileUrl.LastIndexOf(".");
            if (nLastDotPos > 0)
            {
                strExtension = strFileUrl.Substring(nLastDotPos);
            }
            return strExtension;
        }

        static private string SaveFileToTemp(SPWeb spWeb, string fileUrl)
        {
            string strFilePath = "";
            FileStream dstStream = null;
            BinaryWriter writer = null;
            try
            {
                //added file to IRM ignore list
                string strUserName = spWeb.CurrentUser.Email;
                int nTicks = System.Environment.TickCount;
                Globals._TagProtector_AddFileEncryptIgnore(fileUrl, strUserName, nTicks);

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SPFile targetFile = spWeb.GetFile(fileUrl);
                    strFilePath = Path.GetTempFileName();
                    File.Delete(strFilePath);
                    int ext = fileUrl.LastIndexOf(".");
                    if (ext > 0)
                    {
                        string extName = fileUrl.Substring(ext);
                        strFilePath += extName;
                    }
                    byte[] data = targetFile.OpenBinary();
                    dstStream = File.Create(strFilePath);
                    writer = new BinaryWriter(dstStream);
                    writer.Write(data);
                });

                //remove file to IRM ignore list
                Globals._TagProtector_RemoveFileEncryptIgnore(fileUrl, strUserName, nTicks);

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FileContentAnalysis SaveFileToTemp: ", null, ex);
                strFilePath = "";
            }
            finally
            {
                if (dstStream != null)
                {
                    dstStream.Close();
                }
                if(writer != null)
                {
                    writer.Close();
                }
            }

            return strFilePath;
        }

        static private string SaveFileToTemp(SPWeb spWeb, byte[] binaryContent)
        {
            string strFilePath = "";
            FileStream dstStream = null;
            BinaryWriter writer = null;
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    strFilePath = Path.GetTempFileName();
                    File.Delete(strFilePath);
                    dstStream = File.Create(strFilePath);
                    writer = new BinaryWriter(dstStream);
                    writer.Write(binaryContent);
                });

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FileContentAnalysis SaveFileToTemp binary: ", null, ex);
                strFilePath = "";
            }
            finally
            {
                if (dstStream != null)
                {
                    dstStream.Close();
                }
                if (writer != null)
                {
                    writer.Close();
                }
            }

            return strFilePath;
        }

        private void DeleteTempFile()
        {
            try
            {
                if (String.IsNullOrEmpty(m_FilePath))
                {
                    // Ignore
                }
                else
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate ()
                    {
                        if (File.Exists(m_FilePath))
                        {
                            File.Delete(m_FilePath);
                        }
                        else
                        {
                            // Ignore
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Delete temp file:[{0}] failed with error:[{1}]\n", new object[] { m_FilePath, ex.Message });
            }
        }
        private void EnforceUploadWithCA()
        {
            try
            {
                if (m_ListItem != null)
                {
                    bool bIsVersioningOn = m_List.EnableVersioning;
                    if (!bIsVersioningOn)
                    {
                        //cannot block the upload,
                        NLLogger.OutputLog(LogLevel.Debug, "EnforceUploadWithCA: Cannot block the upload since the versioning feature of the list is not on.");
                    }
                    else
                    {
                        int nVersCount = 0;
                        bool bIsDocLib = (m_ListItem.ParentList.BaseType == SPBaseType.DocumentLibrary);
                        if (bIsDocLib)
                        {
                            nVersCount = m_ListItem.File.Versions.Count;
                        }
                        else
                        {
                            nVersCount = m_ListItem.Versions.Count;
                        }

                        bool bNewadded = false;
                        if (bIsDocLib && nVersCount == 0 || !bIsDocLib && nVersCount == 1)
                            bNewadded = true;

                        if (bNewadded)//newly added, just delete it
                        {
                            m_ListItem.Delete();
                        }
                        else //updated
                        {
                            if (bIsDocLib)
                            {
                                m_ListItem.File.Versions.Restore(nVersCount - 1);
                            }
                            else
                            {
                                m_ListItem.Versions.Restore(nVersCount - 1);
                                string strAttName = m_FileUrl;
                                strAttName = strAttName.Substring(strAttName.LastIndexOf('/') + 1);
                                m_ListItem.Attachments.DeleteNow(strAttName);
                            }
                            m_ListItem.Versions[1].Delete();
                            m_ListItem.Versions[1].Delete();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during EnforceUploadWithCA:", null, ex);
            }

        }
        private void EvaluationEdit(bool isRunByTimerJob)
        {
            EvaluatorContext evaContext = new EvaluatorContext();
            evaContext.ActionStr = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
            evaContext.PrincipalUser = m_PrincipalUser;
            if (m_Batch)
                evaContext.NoiseLevel = NoiseLevel.Application;
            else
                evaContext.NoiseLevel = NoiseLevel.UserAction;
            evaContext.Web = m_Web;
            evaContext.RemoteAddress = m_ClientIP;

            evaContext.UserName = m_UserName;
            evaContext.UserSid = m_UserSid;

            if (String.IsNullOrEmpty(m_UserName) && String.IsNullOrEmpty(m_UserSid))
            {
                evaContext.UserName = m_Web.CurrentUser.LoginName;
                evaContext.UserSid = m_Web.CurrentUser.Sid;
            }

            evaContext.SrcName = m_FileUrl;
            evaContext.TargetName = "";

            EvaluatorProperties evaProperties = new EvaluatorProperties();
            List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
            evaProperties.ConstructForItem(m_ListItem, ref attributes);
            evaContext.SrcAttributes = attributes;

            Evaluator evaluator = new Evaluator();
            if (!String.IsNullOrEmpty(m_FilePath))
            {
                KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(ContentAnalysisIndicationKey, m_FilePath);
                evaContext.SrcAttributes.Add(keyVaule);

                // Register Content Analysis Obligation Processor
                ContentAnalysisObligation caObligation = new ContentAnalysisObligation(m_ListItem, m_Result, m_FilePath, m_FileUrl, attributes, isRunByTimerJob);
                evaluator.RegisterIObligation(caObligation);
            }
            else
            {
                NLLogger.OutputLog(LogLevel.Debug, String.Format("Current the item:[{0}] temp file path:[{1}] is null, no need register content analysis obligation\n", m_FileUrl, m_FilePath));
            }

            bool bResult;
            if (Globals.g_JPCParams.bUseJavaPC)
                bResult = evaluator.CheckPortalResource_CloudAZ(ref evaContext);
            else
                bResult = evaluator.CheckPortalResource(ref evaContext);

            if (!bResult && !isRunByTimerJob) //upload denied, delete the item/version
            {
                EnforceUploadWithCA();
                m_bCADenied = true;
            }

            if (!String.IsNullOrEmpty(evaContext.FailedReason))
                m_Result.FailedReason.Add(evaContext.FailedReason);
        }
    }
}
