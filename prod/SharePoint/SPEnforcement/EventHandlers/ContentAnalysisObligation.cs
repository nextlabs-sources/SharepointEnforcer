using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Workflow;
using NextLabs.Common;
using System.Globalization;
using Microsoft.SharePoint.Taxonomy;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public struct AddHeaderInfo
    {
        public bool bColumn; // True means the obligation is "SP_ADD_HEADER_BY_COL".
        public string value; // context value or column value.
        public string position;
    }

    public struct ClassificationInfo
    {
        public string name;
        public string values;
        public string compare;
        public string recipients;
        public string subject;
        public string body;
        public bool bSet;
    }

    public class ContentAnalysisObligation : IObligation
    {
        public const string CA_OBLIGATION_NAME = "SET_COLUMN_VALUE";
        public const string CA_ATTR_NAME = "Column Name";
        public const string CA_ATTR_VALUE = "Column Value";
        public const string CA_ATTR_ALL_TAGS = "All Matched Tags";
        public const string CA_ALL_COLUMN_MATCH = "Yes";

        private SPListItem m_ListItem;
        private Dictionary<string, string> m_DicFields;
        private List<KeyValuePair<string, string>> attributes;
        private ContentAnalysisResult m_Result;
        private string m_InputFilePath;
        private string m_FileUrl;
        private bool m_IsRunByTimerJob = false;
        private const int MaxTagsAccount = 1024;
        private const int MaxTagLength = 1024;
        private bool m_bJustCheckPC= false; //just check policy, not do further process

        public const string ADD_HEADER_OB = "SP_ADD_HEADER";
        public const string ADD_HEADER_FROM = "From";
        public const string ADD_HEADER_VALUE = "Value";
        public const string ADD_HEADER_POSITION = "Position";
        public const string CLASS_MISMATCH_OB = "SP_CLASSIFICATION_MISMATCH_NOTIFICATION";
        public const string CLASS_MISMATCH_NAME = "Classification Name";
        public const string CLASS_MISMATCH_VALUES = "Classification Values";
        public const string CLASS_MISMATCH_COMPARE = "Compare with";
        public const string CLASS_MISMATCH_Set = "Set Parent Site Classification";
        public const string CLASS_MISMATCH_RECIPIENTS = "Recipients";
        public const string CLASS_MISMATCH_SUBJECT = "Email Subject";
        public const string CLASS_MISMATCH_BODY = "Email Body";
        public const string SYNC_COLUMN_WITH_TAG_ = "$fso.";

		private List<AddHeaderInfo> m_HeaderObInfo;
        private List<ClassificationInfo> m_ClassMissObInfo;
        private Dictionary<string, string> m_FileTags;

        private List<string> m_OtherObligationName;


        public ContentAnalysisObligation(SPListItem item, ContentAnalysisResult result, string tempFilePath, string originalURL, List<KeyValuePair<string, string>> attrs, bool isRunByTimerJob)
        {
            m_IsRunByTimerJob = isRunByTimerJob;
            m_ListItem = item;
            m_Result = result;
            m_InputFilePath = tempFilePath;
            m_DicFields = new Dictionary<string, string>();
            m_FileUrl = originalURL;
            attributes = attrs;

            m_HeaderObInfo = new List<AddHeaderInfo>();
            m_ClassMissObInfo = new List<ClassificationInfo>();
            m_FileTags = new Dictionary<string, string>();
            m_OtherObligationName = new List<string>();
        }

        public bool JustCheckPC
        {
            get { return m_bJustCheckPC; }
            set { m_bJustCheckPC = value; }
        }

        public void Process(List<Obligation> obligations, IntPtr hConnect)
        {
            ParseContentAnalysis(obligations);

            if(m_bJustCheckPC)
            {
                //some times we just get the policy result and don't want to do further process.
                return;
            }

            if (PluginFrame.IsPluginEnabled() && m_OtherObligationName.Count > 0)
            {
                PluginFrame.Instance.RunObligationplugins(m_ListItem.Web.Url, m_ListItem.ParentList.ID, m_ListItem.ID, m_OtherObligationName, true, 5000);
            }

            if (m_HeaderObInfo.Count > 0)
            {
                AddHeaderToFile();
            }

            if (m_DicFields.Count > 0)
            {
                SetListItemFields();
            }

            // In this, we don't batch model for "Classification Mismatch";
            if (m_ClassMissObInfo.Count > 0 && !m_IsRunByTimerJob)
            {
                ResetSiteProperty();
            }
            if (PluginFrame.IsPluginEnabled() && m_OtherObligationName.Count > 0)
            {
                PluginFrame.Instance.RunObligationplugins(m_ListItem.Web.Url, m_ListItem.ParentList.ID, m_ListItem.ID, m_OtherObligationName, false, 5000);
            }

        }

        private List<SPField> GetSPFieldListByTitle(string fieldTitle)
        {
            List<SPField> fieldList = new List<SPField>();
            foreach (SPField field in m_ListItem.ParentList.Fields)
            {
                if (field.Title.Equals(fieldTitle, StringComparison.OrdinalIgnoreCase))
                {
                    SPFieldType fieldType = field.Type;
                    // SPFieldType.Invalid using for managed metadata field.
                    if (fieldType == SPFieldType.Note || fieldType == SPFieldType.Text || fieldType == SPFieldType.Integer ||
                       fieldType == SPFieldType.Number || fieldType == SPFieldType.Boolean || fieldType == SPFieldType.Invalid)
                    {
                        fieldList.Add(field);
                    }
                }
            }
            return fieldList;
        }

        private void GetFileTags()
        {
            try
            {
                string tempFile = m_InputFilePath;
                //string remoteUser = m_ListItem.ParentList.ParentWeb.CurrentUser.Email;
                int errCode = 0;
                bool bRet = true;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                   bRet = Globals.GetFileTags(tempFile, m_FileTags, ref errCode);
                });
                if (!bRet)
                {
                    m_Result.FailedReason.Add("Get file tags failed.");
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ContentAnalysisObligation ResetSiteProperty:", null, ex);
            }
        }

        //
        // Summary:
        //     Handle column values ,  the format might be $fso.trim or  $fso.trim;$fso.confidential, or the two mix:  itar;$fso.trim;
        //     Get the tag value from file and join the values to m_DicFields
        private void SetColumnValueToField(string columnName, string columnValue)
        {
            string[] values = columnValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> valueList = new List<string>();
            string tagName;
            foreach (string value in values)
            {
                if (value.StartsWith(SYNC_COLUMN_WITH_TAG_, StringComparison.OrdinalIgnoreCase))
                {
                    tagName = value.Substring(SYNC_COLUMN_WITH_TAG_.Length);
                    if (!string.IsNullOrEmpty(tagName) && m_FileTags.ContainsKey(tagName.ToLower()))
                    {
                        valueList.Add(m_FileTags[tagName.ToLower()]);
                    }
                }
                else if (!String.IsNullOrEmpty(value))
                {
                    valueList.Add(value);
                }
            }
            if (valueList.Count > 0)
            {
                m_DicFields[columnName.ToLower()] = String.Join(";", valueList.ToArray());
            }
        }

        private void SyncAllTagToField()
        {
            string tagLowerName = null;
            foreach (KeyValuePair<string, string> keyValue in m_FileTags)
            {
                tagLowerName = keyValue.Key.ToLower();
                if (!m_DicFields.ContainsKey(tagLowerName))
                {
                    m_DicFields[tagLowerName] = keyValue.Value;
                }
            }
        }

        private void ParseContentAnalysis(List<Obligation> obligations)
        {
            bool bCheck = false;
            bool bAllColumnMatch = false;
            foreach (Obligation obligation in obligations)
            {
                if (obligation.Name.Equals(CA_OBLIGATION_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    if (!bCheck)
                    {
                        GetFileTags();
                        bCheck = true;
                    }
                    string allColumn = obligation.GetAttribute(CA_ATTR_ALL_TAGS);
                    string columnName = obligation.GetAttribute(CA_ATTR_NAME);
                    string columnValue = obligation.GetAttribute(CA_ATTR_VALUE);
                    if (allColumn.Equals(CA_ALL_COLUMN_MATCH, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!bAllColumnMatch)
                        {
                            SyncAllTagToField();
                            bAllColumnMatch = true;
                        }
                    }
                    else if (!String.IsNullOrEmpty(columnName) && !String.IsNullOrEmpty(columnValue))
                    {
                        SetColumnValueToField(columnName, columnValue);
                    }
                }
                else if (obligation.Name.Equals(ADD_HEADER_OB, StringComparison.OrdinalIgnoreCase))
                {
                    AddHeaderInfo headerInfo = new AddHeaderInfo();
                    headerInfo.bColumn = false;
                    if (obligation.GetAttribute(ADD_HEADER_FROM).Equals("column", StringComparison.OrdinalIgnoreCase))
                    {
                        headerInfo.bColumn = true;
                    }
                    headerInfo.value = obligation.GetAttribute(ADD_HEADER_VALUE);
                    headerInfo.position = obligation.GetAttribute(ADD_HEADER_POSITION);
                    m_HeaderObInfo.Add(headerInfo);
                }
                else if (obligation.Name.Equals(CLASS_MISMATCH_OB, StringComparison.OrdinalIgnoreCase))
                {
                    ClassificationInfo classInfo = new ClassificationInfo();
                    classInfo.name = obligation.GetAttribute(CLASS_MISMATCH_NAME);
                    classInfo.values = obligation.GetAttribute(CLASS_MISMATCH_VALUES);
                    classInfo.compare = obligation.GetAttribute(CLASS_MISMATCH_COMPARE);
                    classInfo.recipients = obligation.GetAttribute(CLASS_MISMATCH_RECIPIENTS);
                    classInfo.subject = obligation.GetAttribute(CLASS_MISMATCH_SUBJECT);
                    classInfo.body = obligation.GetAttribute(CLASS_MISMATCH_BODY);
                    classInfo.bSet = obligation.GetAttribute(CLASS_MISMATCH_Set).Equals("Yes", StringComparison.OrdinalIgnoreCase);
                    m_ClassMissObInfo.Add(classInfo);
                }
                else
                {
                    m_OtherObligationName.Add(obligation.Name);
                }
            }
        }

        private bool SetFileTypeCacheKey(string key, int value)
        {
            return false;
        }

        private static void SaveWithoutCheckIn(SPFile file, byte[] filecontent, bool bRunByTimerJob)
        {
            SaveSafeFile(file, filecontent);
            if (bRunByTimerJob)
            {
                SetLastScanTimeFlag(file);
            }
        }

        private static void SetLastScanTimeFlag(SPFile file)
        {
            string _lastscantime = DateTime.Now.AddMinutes(1).ToString();
            if (file.Properties.ContainsKey("nextlabs_lastscan"))
            {
                file.Properties["nextlabs_lastscan"] = _lastscantime;
            }
            else
            {
                file.Properties.Add("nextlabs_lastscan", _lastscantime);
            }
            file.Update();
        }

        private void ResetSiteProperty()
        {
            foreach (ClassificationInfo classInfo in m_ClassMissObInfo)
            {
                string name = classInfo.name;
                string values = classInfo.values;
                string compare = classInfo.compare;
                string recipients = classInfo.recipients;
                string subject = classInfo.subject;
                string body = classInfo.body;
                try
                {
                   string itemValue = GetColumnValue(name);
                   if(!string.IsNullOrEmpty(itemValue))
                   {
                        if (itemValue.Contains(";"))
                        {
                            itemValue = GetFieldMismatch(itemValue, values);
                        }
                        SPWeb web = null;
                        SPWeb parWeb = m_ListItem.ParentList.ParentWeb;
                        SPWeb topWeb = parWeb.Site.RootWeb;
                        if (compare.Equals("Top Site", StringComparison.OrdinalIgnoreCase))
                        {
                            web = topWeb;
                        }
                        if (compare.Equals("Parent Site", StringComparison.OrdinalIgnoreCase))
                        {
                            web = parWeb;
                        }
                        string siteValue = Globals.GetSiteProperty(web, name);
                        string strPropValueParentWeb = Globals.GetSiteProperty(parWeb, name);

                        // define empty value to lowest level, so we need care site empty value.
                        if (!string.IsNullOrEmpty(itemValue) && !string.IsNullOrEmpty(siteValue) && !itemValue.Equals(siteValue, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(values))
                        {
                            string[] arrValues = values.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries); // don't care empty value in "classifacation Values".
                            int indItem = -1;
                            int indSite = -1;
                            int nIndexParentWeb = -1;
                            for (int i = 0; i < arrValues.Length; i++)
                            {
                                string value = arrValues[i];
                                if (value.Equals(itemValue, StringComparison.OrdinalIgnoreCase))
                                {
                                    indItem = i;
                                }
                                else if (value.Equals(siteValue, StringComparison.OrdinalIgnoreCase))
                                {
                                    indSite = i;
                                }
                                if (value.Equals(strPropValueParentWeb, StringComparison.OrdinalIgnoreCase))
                                {
                                    nIndexParentWeb = i;
                                }
                            }
                            if (indItem >= 0 && indSite > indItem)
                            {
                                if (classInfo.bSet && (nIndexParentWeb > indItem || nIndexParentWeb == -1))
                                {
                                    Globals.SetSiteProperty(parWeb, name, itemValue);
                                }
                                recipients = recipients.Replace(";", ",");
                                ReplaceSpecString(ref subject, m_FileUrl, parWeb);
                                ReplaceSpecString(ref body, m_FileUrl, parWeb);
                                string owner = Globals.GetFullControlUsersEmail(parWeb);
                                if (!string.IsNullOrEmpty(owner))
                                {
                                    recipients = owner + "," + recipients;
                                }
                                Globals.SPESendEmail(parWeb, recipients, subject, body);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ContentAnalysisObligation ResetSiteProperty:", null, ex);
                }
            }
        }

        private static string GetFieldMismatch(string itemValue, string obValues)
        {
            if (!string.IsNullOrEmpty(itemValue) && !string.IsNullOrEmpty(obValues))
            {
                string[] itemValues = itemValue.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                string[] arrValues = obValues.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string value in arrValues)
                {
                    foreach (string arrValue in itemValues)
                    {
                        if (value.Equals(arrValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return value; // find the mismatch value in itemValues.
                        }
                    }
                }
            }

            return null; // return null if not found.
        }

        // Using correct information to replace "[FILE], [SITE], [PARENTSITE] and [TOPSITE]".
        private void ReplaceSpecString(ref string source, string fileUrl, SPWeb web)
        {
            if (!string.IsNullOrEmpty(source))
            {
                SPWeb topWeb = web.Site.RootWeb;
                string[] specStrArr = { "[FILE]", "[SITE]", "[PARENTSITE]", "[TOPSITE]" };
                string[] replaceValue = new string[specStrArr.Length];
                replaceValue[0] = fileUrl;
                replaceValue[1] = web.Url;
                replaceValue[2] = web.Url;
                replaceValue[3] = topWeb.Url;
                int ind = 0;
                for(int i = 0; i < specStrArr.Length; i++)
                {
                    string specStr = specStrArr[i];
                    ind = source.IndexOf(specStr, StringComparison.OrdinalIgnoreCase);
                    if(ind != -1)
                    {
                        source = source.Substring(0, ind) + replaceValue[i] + source.Substring(ind + specStr.Length);
                    }
                }
            }
        }

        private void AddHeaderToFile()
        {
            try
            {
                SPFile file = m_ListItem.Web.GetFile(m_FileUrl);
                string tempFile = m_InputFilePath;
                List<string> values = new List<string>();
                string position = "";
                foreach (AddHeaderInfo headerInfo in m_HeaderObInfo)
                {
                    string value = "";
                    string obValue = headerInfo.value;
                    position = headerInfo.position;
                    if (headerInfo.bColumn)
                    {
                        string columnValue = GetColumnValue(obValue);
                        if (!string.IsNullOrEmpty(columnValue))
                        {
                            value = obValue + "__" + columnValue;
                        }
                    }
                    else
                    {
                        value = obValue;
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                }

                if (file != null && values.Count > 0)
                {
                    bool bRet = false;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        Globals.SupFileType fileType = Globals.GetFileType(tempFile);
                        if (fileType == Globals.SupFileType.POWERPOINT2003)
                        {
                            bRet =  SPAddHeader.AddHeaderFooterForPPT2003(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.POWERPOINT2007)
                        {
                            bRet = SPAddHeader.AddFooterForPPT2007(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.WORD2007)
                        {
                            bRet = SPAddHeader.AddHeaderForLocalWordFile(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.WORD2003)
                        {
                            bRet = SPAddHeader.AddHeaderForWord2003(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.EXCEL2007)
                        {
                            bRet = SPAddHeader.AddHeaderForExcel2007(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.EXCEL2003)
                        {
                            bRet = SPAddHeader.AddHeaderForExcel2003(tempFile, position, values);
                        }
                        else if (fileType == Globals.SupFileType.PDF)
                        {
                            bRet = SPAddHeader.AddHeaderForPdf(tempFile, position, values);
                        }
                    });
                    //Checkin the converted  file to the sharepoint
                    if (bRet)
                    {
                        byte[] fileContent = null;
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            fileContent = File.ReadAllBytes(tempFile);
                            try
                            {
                                SaveWithoutCheckIn(file, fileContent, m_IsRunByTimerJob);
                            }
                            catch
                            {
                                if (!string.IsNullOrEmpty(file.LockId))
                                {
                                    file.ReleaseLock(file.LockId);
                                }
                                SaveWithoutCheckIn(file, fileContent, m_IsRunByTimerJob);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                string failed = "Failed to add header Exception: " + ex.Message;
                m_Result.FailedReason.Add(failed);
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ContentAnalysisObligation AddHeaderToFile:", null, ex);
            }
        }

        private string GetColumnValue(string columnKey)
        {
            string columnValue = null;
            try
            {
                foreach (SPField field in m_ListItem.Fields)
                {
                    if (field.Title.Equals(columnKey, StringComparison.OrdinalIgnoreCase))
                    {
                        object objValue = m_ListItem[field.InternalName];
                        if (objValue != null)
                        {
                            columnValue = field.GetFieldValueAsText(objValue);
                            if (!string.IsNullOrEmpty(columnValue) && (columnValue.Contains("\n") || columnValue.Contains("\r")))
                            {
                                columnValue = columnValue.Replace("\n", ";").Replace("\r", ";");
                            }
                        }
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                string failed = "Failed to get column value Exception: " + ex.Message;
                m_Result.FailedReason.Add(failed);
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ContentAnalysisObligation GetColumnValue:", null, ex);
            }
            return columnValue;
        }

        // set Managed Metadata type column for file
        private void SetManagedMetadataListItemFields(TaxonomyField taxonomyField, KeyValuePair<string, string> keyValue, ref bool bModified)
        {
            using (SPSite theSite = new SPSite(m_ListItem.Web.Site.ID))
            {
                TaxonomySession taxonomySession = new TaxonomySession(theSite);
                bool isAllowMultipleValues = taxonomyField.AllowMultipleValues;
                Guid termSetId = taxonomyField.TermSetId;
                TermStore termStore = null;
                if (taxonomySession.TermStores != null && taxonomySession.TermStores.Count > 0)
                {
                    for (int i = 0; i < taxonomySession.TermStores.Count; i++)
                    {
                        termStore = taxonomySession.TermStores[i];
                        TermSet termset = termStore.GetTermSet(termSetId);
                        if (termset != null && isAllowMultipleValues)
                        {
                            string[] termValues = keyValue.Value.Split(';');
                            List<Term> termLists = new List<Term>();
                            foreach (string termValue in termValues)
                            {
                                try
                                {
                                    Term term = termset.Terms[termValue.Trim()];
                                    termLists.Add(term);
                                    KeyValuePair<string, string> managedMetadataPair = new KeyValuePair<string, string>(keyValue.Key, termValue.Trim());
                                    m_Result.ModifiedFields.Add(managedMetadataPair);
                                }
                                catch
                                {
                                    string failed = "Failed to set " + keyValue.Key + "=" + termValue.Trim();
                                    failed += ". Reason: Termset: " + termset.Name + " Not contain term value: " + termValue.Trim();
                                    m_Result.FailedReason.Add(failed);
                                }
                            }

                            taxonomyField.SetFieldValue(m_ListItem, termLists, CultureInfo.CurrentUICulture.LCID);
                            bModified = true;
                            break;
                        }
                        else if (termset != null && !isAllowMultipleValues)
                        {
                            try
                            {
                                Term term = termset.Terms[keyValue.Value];
                                taxonomyField.SetFieldValue(m_ListItem, term, CultureInfo.CurrentUICulture.LCID);
                                bModified = true;
                                m_Result.ModifiedFields.Add(keyValue);
                            }
                            catch
                            {
                                string failed = "Failed to set " + keyValue.Key + "=" + keyValue.Value;
                                failed += ". Reason: Termset: " + termset.Name + " Not contain term value: " + keyValue.Value;
                                m_Result.FailedReason.Add(failed);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    string failed = "Failed to set " + keyValue.Key + "=" + keyValue.Value;
                    failed += ". Reason: No TermStore";
                    m_Result.FailedReason.Add(failed);
                }
            }
        }

        private void UpdateItemFieldValue(ref bool bModified)
        {
            foreach (KeyValuePair<string, string> keyValue in m_DicFields)
            {
                List<SPField> fieldList = GetSPFieldListByTitle(keyValue.Key);
                if (fieldList.Count == 0)
                {
                    string failed = "Failed to set " + keyValue.Key + "=" + keyValue.Value;
                    failed += ". Reason: The column " + keyValue.Key + " does not pre-exist or Unsupported field type.";
                    m_Result.FailedReason.Add(failed);
                    continue;
                }
                foreach (SPField field in fieldList)
                {
                    try
                    {
                        if (field is TaxonomyField)
                        {
                            TaxonomyField taxonomyField = field as TaxonomyField;
                            if (taxonomyField != null)
                            {
                                SetManagedMetadataListItemFields(taxonomyField, keyValue, ref bModified);
                            }
                            continue;
                        }

                        switch (field.Type)
                        {
                            case SPFieldType.Text:
                            case SPFieldType.Note:
                                {
                                    bModified = true;
                                    m_ListItem[field.InternalName] = keyValue.Value;
                                    m_Result.ModifiedFields.Add(keyValue);
                                }
                                break;
                            case SPFieldType.Integer:
                            case SPFieldType.Number:
                                {
                                    bModified = true;
                                    m_ListItem[field.InternalName] = Int32.Parse(keyValue.Value);
                                    m_Result.ModifiedFields.Add(keyValue);
                                }
                                break;
                            case SPFieldType.Boolean:
                                {
                                    bModified = true;
                                    m_ListItem[field.InternalName] = Boolean.Parse(keyValue.Value);
                                    m_Result.ModifiedFields.Add(keyValue);
                                }
                                break;
                            default:
                                {
                                    string failed = "Failed to set " + keyValue.Key + "=" + keyValue.Value;
                                    failed += ". Reason: Unsupported field type for column " + keyValue.Key;
                                    m_Result.FailedReason.Add(failed);
                                }
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        // set column for file
        private void SetListItemFields()
        {
            if (m_ListItem == null)
            {
                return;
            }

            SPList list = m_ListItem.ParentList;
            bool bModified = false;
            SPFile _file = null;

            int itemID = m_ListItem.ID;
            m_ListItem = list.GetItemById(itemID); // Get the new item after upoload again.

            // Update field value
            UpdateItemFieldValue(ref bModified);

            if (bModified)
            {
                bool bSuccess = true;
                try
                {
                    m_ListItem.SystemUpdate(false);
                }
                catch (Exception exp)
                {
                    string failed = "Failed to set ";
                    foreach (KeyValuePair<string, string> keyValue in m_Result.ModifiedFields)
                    {
                        failed += keyValue.Key + "=" + keyValue.Value + ";";
                    }

                    if (exp.Message.Contains("is not checked out"))
                    {
                        try
                        {
                            _file.CheckOut();
                            m_ListItem.SystemUpdate(false);
                            _file.CheckIn("Set column value after content analyzed.");
                        }
                        catch (Exception exp2)
                        {
                            bSuccess = false;
                            failed = " Reason: " + exp2.Message;
                            m_Result.FailedReason.Add(failed);
                            m_Result.ModifiedFields.Clear();
                        }
                    }
                    else if (exp.Message.Contains("has been modified by"))
                    {
                        if(Globals.sleepTimeWhenUpload<=5000)
                            Globals.sleepTimeWhenUpload += 500;

                        SPList parentlist = m_ListItem.ParentList;
                        m_ListItem = parentlist.GetItemById(m_ListItem.ID);
                        bModified = false;

                        // Update field value
                        UpdateItemFieldValue(ref bModified);

                        if (bModified)
                        {
                            try
                            {
                                m_ListItem.SystemUpdate(false);
                            }
                            catch (Exception expin)
                            {
                                bSuccess = false;
                                failed = " Reason: " + expin.Message;
                                m_Result.FailedReason.Add(failed);
                                m_Result.ModifiedFields.Clear();
                            }
                        }
                    }
                    else
                    {
                        bSuccess = false;
                        failed = " Reason: " + exp.Message;
                        m_Result.FailedReason.Add(failed);
                        m_Result.ModifiedFields.Clear();
                    }
                }
                if (bSuccess == true && m_IsRunByTimerJob)
                {
                    Globals.succeedCountForUpload++;
                    // to set property[nextlabs_lastscan].
                    try
                    {
                        _file = m_ListItem.File;
                    }
                    catch
                    {
                        //when is task attachment case.
                        _file = m_ListItem.ParentList.ParentWeb.GetFile(m_FileUrl);
                    }
                    if (_file == null)
                        _file = m_ListItem.ParentList.ParentWeb.GetFile(m_FileUrl);

                    string _lastscantime = DateTime.Now.AddMinutes(1).ToString();
                    try
                    {
                        if (_file.Properties.ContainsKey("nextlabs_lastscan"))
                        {
                            _file.Properties["nextlabs_lastscan"] = _lastscantime;
                        }
                        else
                        {
                            _file.Properties.Add("nextlabs_lastscan", _lastscantime);
                        }

                        _file.Update();
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Error, $"Failed to update file:{m_FileUrl} in SetListItemFields", null, ex);
                    }
                }
            }
        }

        private static void SaveSafeFile(SPFile file, byte[] filecontent)
        {
            int nLoop = 0;
            while (nLoop++ < 1000)
            {
                try
                {
                    file.SaveBinary(filecontent);
                    break;
                }
                catch (UnauthorizedAccessException)
                {
                    System.Threading.Thread.Sleep(200);
                    nLoop += 100;
                }
                catch (SPFileLockException)
                {
                    if (!string.IsNullOrEmpty(file.LockId))
                    {
                        file.ReleaseLock(file.LockId);
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                }
            }

            if (nLoop > 1000)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Nextlabs -savebinaryfile failed file is: " + file.Url);
            }
        }
    }
}
