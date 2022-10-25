using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Workflow;
using System.Net;
using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Diagnostics;

namespace ceSPService
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://www.nextlabs.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class DocUpload : System.Web.Services.WebService
    {

        [WebMethod]
        public bool IsDocLib(string pathFolder)
        {
            try
            {
                int iStartIndex = pathFolder.LastIndexOf("/");
                string sitePath = pathFolder.Remove(iStartIndex);
                SPSite site = new SPSite(sitePath);
                SPWeb web = site.OpenWeb();

                SPList list = web.GetList(pathFolder);
                if (list.BaseType == SPBaseType.DocumentLibrary)
                {
                    return true;
                }
                return false;

            }
            catch (System.Exception)
            {
                return false;
            }
        }
        [WebMethod]
        public string UploadDocument(string fileName, byte[] fileContents, string pathFolder,ref int ItemId,ref string itemPath,ref string webUrl,bool bIfUnique)
        {
            if(fileContents == null)
                return "Null Attachment";
            try
            {
                int iStartIndex = pathFolder.LastIndexOf("/");
                string sitePath = pathFolder.Remove(iStartIndex);
                string folderName=pathFolder.Substring(iStartIndex+1);
                if (IsDocLib(pathFolder))
                {
                    SPSite site = new SPSite(sitePath);
                    SPWeb web = site.OpenWeb();
                    SPFolder folder = web.GetFolder(folderName);
                    SPList list = web.GetList(pathFolder);
                    if (!folder.Exists || !(pathFolder.EndsWith(folder.ServerRelativeUrl, StringComparison.OrdinalIgnoreCase)))
                    {
                        foreach (SPListItem itemfolder in list.Folders)
                        {
                            if (pathFolder.EndsWith(itemfolder.Url, StringComparison.OrdinalIgnoreCase))
                            {
                                folder = itemfolder.Folder;
                                break;
                            }
                        }
                    }
                    string fileURL = fileName;
                    if (bIfUnique)
                    {
                        bool bHasFile = false;
                        if (list != null)
                        {
                            foreach (SPListItem it in list.Items)
                            {
                                if (fileName.Equals(it.Name))
                                {
                                    bHasFile = true; ;
                                    break;
                                }
                            }
                            if (bHasFile)
                            {
                                int lastfixpos = fileName.LastIndexOf(".");
                                string lastfix = fileName.Substring(lastfixpos);
                                fileURL = fileName.Substring(0,lastfixpos);
                                string _year = DateTime.Now.Year.ToString();
                                string _month = (DateTime.Now.Month < 10) ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString();
                                string _Day = (DateTime.Now.Day < 10) ? "0" + DateTime.Now.Day.ToString() : DateTime.Now.Day.ToString();
                                string _Hour = (DateTime.Now.Hour < 10) ? "0" + DateTime.Now.Hour.ToString() : DateTime.Now.Hour.ToString();
                                string _Minute = (DateTime.Now.Minute < 10) ? "0" + DateTime.Now.Minute.ToString() : DateTime.Now.Minute.ToString();
                                string _Second = (DateTime.Now.Second < 10) ? "0" + DateTime.Now.Second.ToString() : DateTime.Now.Second.ToString();
                                fileURL += _year + _month + _Day + _Hour + _Minute + _Second + lastfix;
                            }
                        }
                    }

                    if (list.ForceCheckout)
                    {
                        try
                        {
                            if (folder.Files[fileURL] != null)
                            {
                                if (folder.Files[fileURL].CheckedOutByUser == null)
                                {
                                    folder.Files[fileURL].CheckOut();
                                }
                            }
                        }
                        catch
                        {
                            //There could be a possbility that this file does not exist
                        }
                    }

                    SPFile file = folder.Files.Add(fileURL, fileContents,true);
                    itemPath = pathFolder + "/" + fileURL;
                    webUrl = web.Url;
                    if (folder.Files[fileURL].CheckedOutByUser != null && folder.Files[fileURL].CheckedOutByUser.Name != "")
                        folder.Files[fileURL].CheckIn("File Checked In by ceSPService");
                    SPListItem item = null;
                    if (list != null)
                    {
                        foreach (SPListItem it in list.Items)
                        {
                            if (fileURL.Equals(it.Name))
                            {
                                item = it;
                                break;
                            }
                        }
                        if (item != null)
                            ItemId = item.ID;
                    }
                    site.Dispose();
                    return "File added successfully!";
                }
                else
                {
                    bool _found = false;
                    string return_val = "Item added successfully!";
                    SPSite site = new SPSite(sitePath);
                    SPWeb web = site.OpenWeb();
                    SPList list = web.GetList(pathFolder);
                    SPListItemCollection ic = list.Items;                    
                    webUrl = web.Url;
                    SPListItem item = null;
                    string fileURL = fileName;
                    foreach (SPListItem it in ic)
                    {
                        if (fileURL.Equals(it.Title))
                        {
                            _found = true;
                            item = it;
                            return_val += "actually, it is a update";
                            break;
                        }
                    }
                    if (!_found)
                    {
                        item = ic.Add();
                        item["Title"] = fileURL;
                        item.Update();
                        item.Attachments.Add(fileURL, fileContents);
                        item.Update();
                        ItemId = item.ID;
                        itemPath = web.Url + "/" + item.Url;
                    }
                    else
                    {
                        if (bIfUnique)
                        {
                            int lastfixpos = fileName.LastIndexOf(".");
                            string lastfix = fileName.Substring(lastfixpos);
                            fileURL = fileName.Substring(0, lastfixpos);
                            string _year = DateTime.Now.Year.ToString();
                            string _month = (DateTime.Now.Month < 10) ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString();
                            string _Day = (DateTime.Now.Day < 10) ? "0" + DateTime.Now.Day.ToString() : DateTime.Now.Day.ToString();
                            string _Hour = (DateTime.Now.Hour < 10) ? "0" + DateTime.Now.Hour.ToString() : DateTime.Now.Hour.ToString();
                            string _Minute = (DateTime.Now.Minute < 10) ? "0" + DateTime.Now.Minute.ToString() : DateTime.Now.Minute.ToString();
                            string _Second = (DateTime.Now.Second < 10) ? "0" + DateTime.Now.Second.ToString() : DateTime.Now.Second.ToString();
                            fileURL += _year + _month + _Day + _Hour + _Minute + _Second + lastfix;
                            item = ic.Add();
                            item["Title"] = fileURL;
                            item.Update();
                            item.Attachments.Add(fileURL, fileContents);
                            item.Update();
                            ItemId = item.ID;
                            itemPath = web.Url + "/" + item.Url;
                        }
                        else
                        {
                            item.Attachments.Delete(fileURL);
                            item.Update();
                            item.Attachments.Add(fileURL, fileContents);
                            item.Update();
                            ItemId = item.ID;
                            itemPath = web.Url + "/" + item.Url;
                        }
                    }
                    list.Update();
                    site.Dispose();
                    return return_val;
                }
            }
            catch(System.Exception ex)
            {
                return "Error: " + ex.Source + " - " + ex.Message;
            }
        }

        [WebMethod]
        //id or guid, you can input either of one
        public string SetColumns(int id, Guid guid,string pathFolder, string fieldName, string fieldValue)
        {
            if (id < 0)
                return "Null Item";
            try
            {
                int iStartIndex = pathFolder.LastIndexOf("/");
                string sitePath = pathFolder.Remove(iStartIndex);
                SPSite site = new SPSite(sitePath);
                SPWeb web = site.OpenWeb();
                SPList list = web.GetList(pathFolder);
                SPListItem item = null;
                string _filedTitle = null;
                bool hasField = false;
                if (id >= 0)
                    item = list.GetItemById(id);
                else if (guid != null)
                    item = list.GetItemByUniqueId(guid);
                if (item != null)
                {
                    //We must detect if this filed exists or not   
                    foreach (SPField f in item.Fields)
                    {
                        if (f.Title.Equals(fieldName,StringComparison.OrdinalIgnoreCase))
                        {
                            hasField = true;
                            _filedTitle = f.Title;
                            break;
                        }

                    }
                    if (hasField)
                    {
                        if (item.File != null && item.File.Exists && list.ForceCheckout)
                        {
                            if (item.File.CheckedOutByUser == null)
                            {
                                item.File.CheckOut();
                            }
                        }
                        item[_filedTitle] = fieldValue;
                        item.Update();
                        if (item.File != null && item.File.Exists && list.ForceCheckout)
                        {
                            if (item.File.CheckedOutByUser != null)
                            {
                                item.File.CheckIn("File Checked In by ceSPService");
                            }
                        }
                        site.Dispose();
                        return "field update successfully!";
                    }
                    site.Dispose();
                    return "Null field";
                }
                site.Dispose();
                return "Null Item";
            }
            catch (System.Exception ex)
            {
                return "Error: " + ex.Source + " - " + ex.Message;
            }
        }

        [WebMethod]
        //id or guid, you can input either of one
        public bool IfInAssociationTemplates(string pathFolder, string associationName, string[] associationTemplates)
        {
            try
            {
                int iStartIndex = pathFolder.LastIndexOf("/");
                string sitePath = pathFolder.Remove(iStartIndex);
                SPSite site = new SPSite(sitePath);
                SPWeb web = site.OpenWeb();
                SPList list = web.GetList(pathFolder);
                SPWorkflowAssociation _WorkflowAssociation = null;
                foreach (SPWorkflowAssociation wfa in list.WorkflowAssociations)
                {
                    if (wfa.Name.Equals(associationName, StringComparison.OrdinalIgnoreCase))
                    {
                        _WorkflowAssociation = wfa;
                        break;
                    }
                }
                foreach (string associationTemplate in associationTemplates)
                {
                    if (associationTemplate.Equals(_WorkflowAssociation.BaseTemplate.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        site.Dispose();
                        return true;
                    }
                }
                site.Dispose();
                return false;

            }
            catch (System.Exception ex)
            {                
                return false;
            }
        }

        [WebMethod]
        //id or guid, you can input either of one
        public string StartWorkFlow(int itemID, string itemNamestring, string pathFolder, string associationName, string associationDes, bool bIsAutoStart,ref bool bHasWorkFlowRunning, ref int iWorkFlowStatus)
        {
            if (itemID < 0 && itemNamestring == null)
                return "Null Item";
            try
            {
                int iStartIndex = pathFolder.LastIndexOf("/");
                string sitePath = pathFolder.Remove(iStartIndex);
                SPSite site = new SPSite(sitePath);
                SPWeb web = site.OpenWeb();
                SPList list = web.GetList(pathFolder);
                SPWorkflowManager assmgr = site.WorkflowManager;
                SPListItem item = null;
                if (itemID >= 0)
                    item = list.GetItemById(itemID);
                else
                {
                    foreach (SPListItem f in list.Items)
                    {
                        if (f.Name.Equals(itemNamestring, StringComparison.OrdinalIgnoreCase))
                        {
                            item = f;
                            break;
                        }
                    }
                }                

                SPWorkflowAssociation _WorkflowAssociation = null;
                if (item != null)
                {
                    foreach (SPWorkflow wf in item.Workflows)
                    {
                        if (wf.ParentAssociation.Name.Equals(associationName, StringComparison.OrdinalIgnoreCase))
                        {                            
                            bHasWorkFlowRunning = true;
                            site.Dispose();
                            return "Workflow Already Exsits";
                        }
                    }

                    foreach (SPWorkflowAssociation wfa in list.WorkflowAssociations)
                    {
                        if (wfa.Name.Equals(associationName, StringComparison.OrdinalIgnoreCase))
                        {
                            _WorkflowAssociation = wfa;
                            break;
                        }
                    }

                    string associationData = _WorkflowAssociation.AssociationData;
                    if (associationDes != null && associationDes != "" && associationData != null && associationData != "")
                    {
                        try
                        {
                            //add id to the workflow associationData
                            int startpos = associationData.IndexOf("LogId>");
                            startpos += 6;
                            int endpos = associationData.LastIndexOf("LogId>");
                            string startstr = associationData.Substring(0, startpos);
                            string endstr1 = associationData.Substring(0, endpos);
                            string endstr2 = associationData.Substring(endpos);
                            int addpos = endstr1.LastIndexOf("</");
                            endstr1 = endstr1.Substring(addpos);
                            associationData = startstr + associationDes + endstr1 + endstr2;
                        }
                        catch(Exception e)
                        {
                            Trace.WriteLine("try to add log id to workflow associationData exception:" + e.Message);
                        }
                    }

                    SPWorkflow _workflow = assmgr.StartWorkflow(item, _WorkflowAssociation, associationData, bIsAutoStart);
                    if (_workflow != null)
                    {
                        iWorkFlowStatus = (int)_workflow.InternalState;
                        site.Dispose();
                        return "Workflow Started";
                    }
                    else
                    {
                        site.Dispose();
                        return "Workflow Start faile";
                    }
                }
                site.Dispose();
                return "Null Item";
            }
            catch (System.Exception ex)
            {
                return "Error: " + ex.Source + " - " + ex.Message;
            }
        }
                
    }
}
