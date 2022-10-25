using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using System.IO;
using System.Xml;
using NextLabs.Common;
using Microsoft.SharePoint.WebControls;
using Nextlabs.SPSecurityTrimming;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    enum SoapURLType
    {
        Sites,
        Webs,
        Lists,
        Forms,
        Copy,
        Views,
        SiteData,
        WebpartPages,
        ExcelService,
        Imaging,
        Search,
        SPSerach,
        Versions,
        Unknow
    }

    class SOAPBodyData
    {
        public SOAPBodyData()
        {
            parameters = new Dictionary<string, string>();
            nodeName = null;
            name = null;
        }

        public string nodeName { get; set; }
        public string name { get; set; }
        public Dictionary<string, string> parameters;
    }

    // Enforcement for "SOAP".
    public class SOAPModule
    {
        private string[] URLSymbels = {   "_vti_bin/sites.asmx", "_vti_bin/webs.asmx", "_vti_bin/lists.asmx",
                                                "_vti_bin/forms.asmx", "_vti_bin/copy.asmx", "_vti_bin/views.asmx",
                                                "_vti_bin/sitedata.asmx", "_vti_bin/webpartpages.asmx", "_vti_bin/excelservice.asmx",
                                                "_vti_bin/imaging.asmx", "_vti_bin/search.asmx", "_vti_bin/spsearch.asmx", "_vti_bin/versions.asmx" };
        private HttpRequest m_request;
        private HttpResponse m_response;
        private SoapURLType m_urlType;
        private SOAPBodyData m_bodyData;
        private bool m_bResult; // enforcement result: allow or deny.
        private string m_denyMsg;
        public string DenyMessage
        {
            get { return m_denyMsg; }
        }

        public SOAPModule(HttpRequest request, HttpResponse response)
        {
            m_request = request;
            m_response = response;
            m_urlType = SoapURLType.Unknow;
            m_bodyData = new SOAPBodyData();
            m_bResult = true; // Use "true" as default value, allow some cases that system used.
            m_denyMsg = "";
        }

        public bool IsSOAPRequest()
        {
            string rawUrl = m_request.RawUrl;
            for (int i = 0; i < URLSymbels.Length; ++i)
            {
                string urlSymbel = URLSymbels[i];
                if (-1 != rawUrl.IndexOf(urlSymbel, StringComparison.OrdinalIgnoreCase))
                {
                    m_urlType = (SoapURLType)i; // Set URL type.
                    return true;
                }
            }
            return false;
        }

        public bool Run()
        {
            AnalyzeBody();
            return m_bResult;
        }

        private void AnalyzeBody()
        {
            try
            {
                Stream inputStream = m_request.InputStream;
                long _oldPos = inputStream.Seek(0, SeekOrigin.Current);
                byte[] contentBuf = new byte[inputStream.Length];
                inputStream.Read(contentBuf, 0, (int)inputStream.Length);
                Encoding encoding = m_request.ContentEncoding != null ? m_request.ContentEncoding : Encoding.UTF8;
                String strBody = Globals.UrlDecode(encoding.GetString(contentBuf));

                inputStream.Seek(_oldPos, SeekOrigin.Begin);
                if (-1 != m_request.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
                {
                    AnalyzeXml(strBody);
                    SOAPEvalObject evalObject = new SOAPEvalObject(m_urlType, m_bodyData);
                    bool bEvalObject = evalObject.Run();
                    if (bEvalObject)
                    {
                        object evalObj = evalObject.EvalObject;
                        List<object> evalObjList = evalObject.EvalObjectList;
                        bool bCollection = evalObject.CheckCollection;
                        CETYPE.CEAction userAction = evalObject.UserAction;
                        SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
                        if (evalObjList.Count > 0)
                        {
                            foreach (object cellObj in evalObjList)
                            {
                                SPObjectEvaluation evaluter = new SPObjectEvaluation(m_request, web, cellObj, userAction);
                                m_bResult = evaluter.Run();
                                if (!m_bResult)
                                {
                                    m_denyMsg = evaluter.GetDenyMessage();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            SPObjectEvaluation evaluter = new SPObjectEvaluation(m_request, web, evalObj, userAction);
                            m_bResult = evaluter.Run();
                            if (!m_bResult)
                            {
                                m_denyMsg = evaluter.GetDenyMessage();
                                return;
                            }
                        }

                        if (bCollection)
                        {
                            ResponseFilter filter = ResponseFilters.Current(m_response);
                            filter.AddFilterType(FilterType.SoapTrimmer);
                            filter.Web = web;
                            filter.EvalObj = evalObj;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AnalyzeBody:", null, ex);
            }
        }

        private void AnalyzeXml(string xmlBody)
        {
            // George: like "?<...>" xml string.
            if (!xmlBody.StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                int ind = xmlBody.IndexOf("<", StringComparison.OrdinalIgnoreCase);
                if (-1 != ind)
                {
                    xmlBody = xmlBody.Substring(ind);
                }
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = xmlBody;
            XmlNode rootNode = xmlDoc.DocumentElement;

            // Existed "soap:Envelope" and "Envelope", body: "soap:Body" and "Body";
            if (-1 != rootNode.Name.IndexOf("Envelope", StringComparison.OrdinalIgnoreCase) && rootNode.HasChildNodes)
            {
                // Get the "Query" and "method" objects in xml.
                XmlNode bodyNode = null;
                foreach (XmlNode childNode in rootNode.ChildNodes)
                {
                    if (-1 != childNode.Name.IndexOf("body", StringComparison.OrdinalIgnoreCase))
                    {
                        bodyNode = childNode;
                        break;
                    }
                }

                if (bodyNode != null && bodyNode.HasChildNodes)
                {
                    XmlNode childNode = bodyNode.ChildNodes[0]; // just one child node in it.
                    string nodeName = childNode.Name;
                    int ind = nodeName.IndexOf(":");
                    if (-1 != ind)
                    {
                        nodeName = nodeName.Substring(ind + 1); // Example: Chang "ns2:GetListCollection" to "GetListCollection".
                    }
                    m_bodyData.nodeName = nodeName;

                    foreach (XmlNode paramNode in childNode.ChildNodes)
                    {
                        string innerText = "";
                        if (paramNode.HasChildNodes && paramNode.ChildNodes.Count > 1)
                        {
                            foreach (XmlNode cellNode in paramNode.ChildNodes)
                            {
                                innerText += (cellNode.InnerText + ";");  // using ";" to separate.
                            }
                        }
                        else
                        {
                            innerText = paramNode.InnerText;
                        }
                        m_bodyData.parameters[paramNode.Name] = innerText.Trim(';');
                    }
                }
            }
        }
    }

    class SOAPEvalObject
    {
        private object m_evalObj; // The object to do evaluation.
        public object EvalObject
        {
            get { return m_evalObj; }
        }

        private List<object> m_listEvalObj; // The objects to do evaluation.
        public List<object> EvalObjectList
        {
            get { return m_listEvalObj; }
        }

        private CETYPE.CEAction m_action; // The action to do evaluation.
        public CETYPE.CEAction UserAction
        {
            get { return m_action; }
        }

        private bool m_bCollection; // The flag to check "Collection" type.
        public bool CheckCollection
        {
            get { return m_bCollection; }
        }

        private SPWeb m_web; // The web in current context.
        private SoapURLType m_urlType;
        private SOAPBodyData m_bodyData;

        public SOAPEvalObject(SoapURLType urlType, SOAPBodyData bodyData)
        {
            m_urlType = urlType;
            m_bodyData = bodyData;
            m_web = null;
            m_evalObj = null;
            m_action = CETYPE.CEAction.Read;
            m_listEvalObj = new List<object>();
        }

        public bool Run()
        {
            bool bRet = false;
            try
            {
                m_web = SPControl.GetContextWeb(HttpContext.Current);
                if(m_web != null)
                {
                    AnalyzeBodyData();
                }
                if (m_evalObj != null || m_listEvalObj.Count > 0)
                {
                    bRet = true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SOAPEvalObject Run:", null, ex);
            }
            return bRet;
        }

        private void AnalyzeBodyData()
        {
            switch (m_urlType)
            {
                case SoapURLType.Sites:
                    SitesRun();
                    break;
                case SoapURLType.Webs:
                    WebsRun();
                    break;
                case SoapURLType.Lists:
                    ListsRun();
                    break;
                case SoapURLType.Forms:
                    FormsRun();
                    break;
                case SoapURLType.Copy:
                    CopyRun();
                    break;
                case SoapURLType.Views:
                    ViewsRun();
                    break;
                case SoapURLType.SiteData:
                    SiteDataRun();
                    break;
                case SoapURLType.WebpartPages:
                    WebpartPagsRun();
                    break;
                case SoapURLType.ExcelService:
                    ExcelServiceRun();
                    break;
                case SoapURLType.Imaging:
                    ImagingRun();
                    break;
                case SoapURLType.Search:
                    SearchRun();
                    break;
                case SoapURLType.SPSerach:
                    SPSearchRun();
                    break;
                case SoapURLType.Versions:
                    VersionsRun();
                    break;
                default:
                    break;
            }
        }

        //change url "http://george-w12-sp13/mylib/ttfolder" to "/mylib/ttfolder".
        private string GetServerRelativeURLFromFullUrl(string fullUrl)
        {
            string beginStr = "://";
            string relativeUrl = fullUrl;
            int indBegin = fullUrl.IndexOf(beginStr);
            if(-1 != indBegin)
            {
                indBegin = fullUrl.IndexOf("/", indBegin + beginStr.Length);
                if (-1 != indBegin)
                {
                    relativeUrl = fullUrl.Substring(indBegin);
                }
                else
                {
                    relativeUrl = "/";
                }
            }
            return relativeUrl;
        }

        private SPList GetListByNameOrGuid(SPWeb web, string listName)
        {
            SPList list = null;
            Guid guid = Guid.Empty;
            Guid.TryParse(listName, out guid);
            if (!guid.Equals(Guid.Empty))
            {
                list = web.Lists.GetList(guid, true);
            }
            else
            {
                list = web.Lists[listName];
            }
            return list;
        }

        private void SitesRun()
        {
            m_evalObj = m_web;
            string relativeUrl = null;
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("CreateWeb") || nodeName.Equals("ImportWeb"))
            {
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("DeleteWeb"))
            {
                if(m_bodyData.parameters.ContainsKey("url"))
                {
                    string fullUrl = m_web.Url + "/" + m_bodyData.parameters["url"];
                    using(SPSite site = new SPSite(fullUrl))
                    {
                        SPWeb opWeb = site.OpenWeb();
                        SPEEvalAttrs.Current().AddDisposeWeb(opWeb);
                        if (opWeb != null)
                        {
                            m_evalObj = opWeb;
                        }
                    }
                    m_action = CETYPE.CEAction.Delete;
                }
            }
            else if (nodeName.Equals("GetSite"))
            {
                if (m_bodyData.parameters.ContainsKey("SiteUrl"))
                {
                    relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["SiteUrl"]);
                }
            }
            else if (nodeName.Equals("ExportWeb"))
            {
                if (m_bodyData.parameters.ContainsKey("webUrl"))
                {
                    relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["webUrl"]);
                }
            }
            else if (nodeName.Equals("GetUpdatedFormDigestInformation"))
            {
                if (m_bodyData.parameters.ContainsKey("url"))
                {
                    relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["url"]);
                }
            }

            if (!string.IsNullOrEmpty(relativeUrl))
            {
                SPSite site = m_web.Site;
                SPWeb opWeb = site.OpenWeb(relativeUrl);
                SPEEvalAttrs.Current().AddDisposeWeb(opWeb);
                m_evalObj = opWeb;
            }
        }

        private void WebsRun()
        {
            m_evalObj = m_web;
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("CreateContentType") || nodeName.Equals("CustomizeCss")
                || nodeName.Equals("DeleteContentType") || nodeName.Equals("RemoveContentTypeXmlDocument")
                || nodeName.Equals("RevertFileContentStream") || nodeName.Equals("UpdateColumns")
                || nodeName.Equals("UpdateContentType") || nodeName.Equals("UpdateContentTypeXmlDocument")
                || nodeName.Equals("RevertAllFileContentStreams") || nodeName.Equals("RevertCss"))
            {
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("GetWeb"))
            {
                if (m_bodyData.parameters.ContainsKey("webUrl"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["webUrl"]);
                    SPWeb opWeb = m_web.Site.OpenWeb(relativeUrl);
                    SPEEvalAttrs.Current().AddDisposeWeb(opWeb);
                    m_evalObj = opWeb;
                }
            }

            if (nodeName.Equals("GetWebCollection") || nodeName.Equals("GetAllSubWebCollection"))
            {
                m_bCollection = true;
            }
        }

        private void ListsRun()
        {
            m_evalObj = m_web;
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("UpdateList")|| nodeName.Equals("UpdateListItemsWithKnowledge") || nodeName.Equals("UpdateListItems")
                || nodeName.Equals("CreateContentType") || nodeName.Equals("UpdateContentType") || nodeName.Equals("DeleteContentType")
                || nodeName.Equals("UpdateContentTypeXmlDocument") || nodeName.Equals("UpdateContentTypesXmlDocument")
                || nodeName.Equals("DeleteContentTypeXmlDocument"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                }
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("AddList") || nodeName.Equals("AddListFromFeature"))
            {
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("AddAttachment") || nodeName.Equals("DeleteAttachment"))
            {
                if (m_bodyData.parameters.ContainsKey("listName") && m_bodyData.parameters.ContainsKey("listItemID"))
                {
                    SPList list = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                    m_evalObj = list.GetItemById(int.Parse(m_bodyData.parameters["listItemID"]));
                }
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("ApplyContentTypeToList"))
            {
                if (m_bodyData.parameters.ContainsKey("webUrl") && m_bodyData.parameters.ContainsKey("listName"))
                {
                    SPWeb opWeb = m_web.Site.OpenWeb(m_bodyData.parameters["webUrl"]);
                    SPEEvalAttrs.Current().AddDisposeWeb(opWeb);
                    m_evalObj = GetListByNameOrGuid(opWeb, m_bodyData.parameters["listName"]);
                }
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("GetList") || nodeName.Equals("GetListAndView")
                || nodeName.Equals("GetListContentType") || nodeName.Equals("GetListContentTypes")
                || nodeName.Equals("GetListContentTypesAndProperties") || nodeName.Equals("GetListItemChanges")
                || nodeName.Equals("GetListItemChangesSinceToken") || nodeName.Equals("GetListItemChangesWithKnowledge")
                || nodeName.Equals("GetListItems") || nodeName.Equals("GetListItemChangesWithKnowledge"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                }
            }
            else if (nodeName.Equals("GetAttachmentCollection"))
            {
                if (m_bodyData.parameters.ContainsKey("listName") && m_bodyData.parameters.ContainsKey("listItemID"))
                {
                    SPList list = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                    m_evalObj = list.GetItemById(int.Parse(m_bodyData.parameters["listItemID"]));
                }
            }
            else if (nodeName.Equals("CheckInFile") || nodeName.Equals("CheckOutFile") || nodeName.Equals("UndoCheckOut"))
            {
                string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["pageUrl"]);
                m_evalObj = m_web.GetListItem(relativeUrl);
                m_action = CETYPE.CEAction.Write;
            }
            else if (nodeName.Equals("DeleteList"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                }
                m_action = CETYPE.CEAction.Delete;
            }
            else if (nodeName.Equals("GetVersionCollection")) // Returns version information for the specified field in SharePoint list item.
            {
                if (m_bodyData.parameters.ContainsKey("strlistID") && m_bodyData.parameters.ContainsKey("strlistItemID"))
                {
                    SPList list = GetListByNameOrGuid(m_web, m_bodyData.parameters["strlistID"]);
                    m_evalObj = list.GetItemById(int.Parse(m_bodyData.parameters["strlistItemID"]));
                }
            }
            else if (nodeName.Equals("AddWikiPage"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["strListName"]);
                }
            }

            if (nodeName.Equals("GetListCollection") || nodeName.Equals("GetListItems") || nodeName.Equals("GetListItemChangesSinceToken"))
            {
                m_bCollection = true;
            }
        }

        private void FormsRun()
        {
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("GetForm") || nodeName.Equals("GetFormCollection"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                }
            }
        }

        private void CopyRun()
        {
            //Note: CopyIntoItems Destination will trigger "Adding" event.
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("CopyIntoItemsLocal") || nodeName.Equals("CopyIntoItems"))
            {
                if (m_bodyData.parameters.ContainsKey("SourceUrl"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["SourceUrl"]);
                    m_evalObj = m_web.GetListItem(relativeUrl);
                }
            }
            else if (nodeName.Equals("GetItem"))
            {
                if (m_bodyData.parameters.ContainsKey("Url"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["Url"]);
                    m_evalObj = m_web.GetListItem(relativeUrl);
                }
            }
        }

        private void ViewsRun()
        {
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("GetView") || nodeName.Equals("GetViewCollection") || nodeName.Equals("GetViewHtml"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                }
            }
            else if (nodeName.Equals("AddView") || nodeName.Equals("DeleteView") || nodeName.Equals("UpdateView")
                || nodeName.Equals("UpdateViewHtml") || nodeName.Equals("UpdateViewHtml2"))
            {
                if (m_bodyData.parameters.ContainsKey("listName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["listName"]);
                    m_action = CETYPE.CEAction.Write;
                }
            }
        }

        private void SiteDataRun()
        {
            m_evalObj = m_web;
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("GetAttachments"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName") && m_bodyData.parameters.ContainsKey("strItemId"))
                {
                    SPList list = GetListByNameOrGuid(m_web, m_bodyData.parameters["strListName"]);
                    m_evalObj = list.GetItemById(int.Parse(m_bodyData.parameters["strItemId"]));
                }
            }
            else if (nodeName.Equals("GetList") || nodeName.Equals("GetListItems"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["strListName"]);
                }
            }
            else if (nodeName.Equals("EnumerateFolder"))
            {
                if (m_bodyData.parameters.ContainsKey("strFolderUrl"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["strFolderUrl"]);
                    m_evalObj = Globals.GetListOrItemFromSPFolder(m_web, m_web.GetFolder(relativeUrl));
                }
            }

            if (nodeName.Equals("GetListItems") || nodeName.Equals("GetListCollection"))
            {
                m_bCollection = true;
            }
        }

        private void WebpartPagsRun()
        {
             //// no need process
        }

        private void ExcelServiceRun()
        {
            string nodeName = m_bodyData.nodeName;
/*          Don't Process NewWorkbook case, since no new file generated
            if (nodeName.Equals("NewWorkbook"))
            {
                if (m_bodyData.parameters.ContainsKey("path"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["path"]);
                    m_evalObj = m_web.GetListItem(relativeUrl);
                }
            }
            else  */
			if (nodeName.Equals("OpenWorkbook") || nodeName.Equals("OpenWorkbookEx"))
            {
                if (m_bodyData.parameters.ContainsKey("workbookPath"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["workbookPath"]);
                    m_evalObj = m_web.GetListItem(relativeUrl);
                }
            }
            else if(nodeName.Equals("OpenWorkbookForEditing"))
            {
                if (m_bodyData.parameters.ContainsKey("workbookPath"))
                {
                    string relativeUrl = GetServerRelativeURLFromFullUrl(m_bodyData.parameters["workbookPath"]);
                    m_evalObj = m_web.GetListItem(relativeUrl);
                }
                m_action = CETYPE.CEAction.Write;
            }
        }

        private void ImagingRun()
        {
            string nodeName = m_bodyData.nodeName;
            if(nodeName.Equals("ListPictureLibrary"))
            {
                m_evalObj = m_web;
                m_bCollection = true;
            }
            else if (nodeName.Equals("GetItemsByIds") || nodeName.Equals("GetItemsXMLData") || nodeName.Equals("GetListItems"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["strListName"]);
                }
                m_bCollection = true;
            }

            if(nodeName.Equals("Download"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName") && m_bodyData.parameters.ContainsKey("strFolder")
                    && m_bodyData.parameters.ContainsKey("itemFileNames"))
                {
                    string folderUrl = null;
                    if (string.IsNullOrEmpty(m_bodyData.parameters["strFolder"]))
                    {
                        folderUrl = m_web.Url + "/" + m_bodyData.parameters["strListName"];
                    }
                    else
                    {
                        folderUrl = m_web.Url + "/" + m_bodyData.parameters["strListName"] + "/" + m_bodyData.parameters["strFolder"];
                    }
                    string[] fileNames = m_bodyData.parameters["itemFileNames"].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string fileName in fileNames)
                    {
                        string fileRelativeUrl = GetServerRelativeURLFromFullUrl(folderUrl + "/" + fileName);
                        try
                        {
                            SPListItem item = m_web.GetListItem(fileRelativeUrl);
                            if (item != null)
                            {
                                m_listEvalObj.Add(item);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            else if (nodeName.Equals("CreateNewFolder"))
            {
                if (m_bodyData.parameters.ContainsKey("strListName"))
                {
                    m_evalObj = GetListByNameOrGuid(m_web, m_bodyData.parameters["strListName"]);
                }
                m_action = CETYPE.CEAction.Write;
            }
        }

        private void SearchRun()
        {
            m_evalObj = m_web;
            if (m_bodyData.nodeName.Equals("Query") || m_bodyData.nodeName.Equals("QueryEx"))
            {
                m_bCollection = true;
            }
        }

        private void SPSearchRun()
        {
            m_evalObj = m_web;
            if (m_bodyData.nodeName.Equals("Query") || m_bodyData.nodeName.Equals("QueryEx"))
            {
                m_bCollection = true;
            }
        }

        private void VersionsRun()
        {
            string nodeName = m_bodyData.nodeName;
            if (nodeName.Equals("GetVersions"))
            {
                if (m_bodyData.parameters.ContainsKey("fileName"))
                {
                    string fullUrl = m_web.Url + "/" + m_bodyData.parameters["fileName"];
                    SPFile file = m_web.GetFile(fullUrl);
                    m_evalObj = Globals.GetSPListItemFromSPFile(m_web, file);
                }
            }
            else if (nodeName.Equals("DeleteAllVersions") || nodeName.Equals("DeleteVersion") || nodeName.Equals("RestoreVersion"))
            {
                if (m_bodyData.parameters.ContainsKey("fileName"))
                {
                    string fullUrl = m_web.Url + "/" + m_bodyData.parameters["fileName"];
                    SPFile file = m_web.GetFile(fullUrl);
                    m_evalObj = Globals.GetSPListItemFromSPFile(m_web, file);
                }
                m_action = CETYPE.CEAction.Write;
            }
        }
    }
}