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
    enum StaticMethodType
    {
        SMT_SetRating,
        SMT_GetDocumentLibraries,
        SMT_Unknown
    }

    class CSOMEvalObj
    {
        public CSOMEvalObj()
        {
            evalUrl = null;
            evalObj = null;
            actions = new List<CETYPE.CEAction>();
        }

        public string evalUrl { get; set; }
        public object evalObj { get; set; }
        public List<CETYPE.CEAction> actions { get; set; }
    }

    class CSOMBodyActions
    {
        public CSOMBodyActions()
        {
            id = 0;
            objPathId = 0;
            nodeName = null;
            name = null;
        }

        public int id { get; set; }
        public int objPathId { get; set; }
        public string nodeName { get; set; }
        public string name { get; set; }
    }

    class CSOMBodyObjectPaths
    {
        public CSOMBodyObjectPaths()
        {
            parameters = new List<string>();
            id = 0;
            parentId = -1;
            nodeName = null;
            name = null;
        }

        public int id { get; set; }
        public int parentId { get; set; }
        public string nodeName { get; set; }
        public string name { get; set; }
        public List<string> parameters;
    }

    // Enforcement for "CSOM", "JSOM", "Silverlight".
    public class CSOMModule
    {
        private const string URLSymbel = "_vti_bin/client.svc/ProcessQuery";
        private HttpRequest m_request;
        private HttpResponse m_response;
        private bool m_bResult; // enforcement result: allow or deny.
        private Dictionary<int, CSOMBodyObjectPaths> m_dicObjPaths;
        private List<CSOMBodyActions> m_listActions; // "Actions" that need to do evaluation.
        private List<CSOMBodyObjectPaths> m_destListObjPaths; // Path work flow for each object that need to do evaluation.
        private string m_denyMsg;
        private bool m_bCollection;
        public string DenyMessage
        {
            get { return m_denyMsg; }
        }

        public CSOMModule(HttpRequest request, HttpResponse response)
        {
            m_request = request;
            m_response = response;
            m_dicObjPaths = new Dictionary<int, CSOMBodyObjectPaths>();
            m_destListObjPaths = new List<CSOMBodyObjectPaths>();
            m_bResult = true; // Use "true" as default value, allow some cases that system used.
            m_listActions = new List<CSOMBodyActions>();
            m_denyMsg = "";
            m_bCollection = false;
        }

        public bool IsCsomRequest()
        {
            string rawUrl = m_request.RawUrl;
            if (-1 != rawUrl.IndexOf(URLSymbel, StringComparison.OrdinalIgnoreCase))
            {
                //search result trimming case
                if (m_request.UrlReferrer != null)
                {
                    string strRefUrl = m_request.UrlReferrer.ToString();
                    if (!string.IsNullOrEmpty(strRefUrl) && -1 != strRefUrl.IndexOf("/osssearchresults.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
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
                if (m_request.ContentLength > 0)
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
                        AnalyzeBodyDatas();
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AnalyzeBody:", null, ex);
            }
        }

        private void AnalyzeBodyDatas()
        {
            bool bAllow = false; // Enforcement result.
            bool bCollection = false;
            // Decode the "Actions" with different operarion.
            Dictionary<int, List<CETYPE.CEAction>> dicDecodeActions = new Dictionary<int, List<CETYPE.CEAction>>();
            SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
            Dictionary<string, CSOMEvalObj> dicEvalObjs = new Dictionary<string, CSOMEvalObj>();

            foreach (CSOMBodyActions bodyActions in m_listActions)
            {
                int objPathId = bodyActions.objPathId;
                CETYPE.CEAction action = DecodeBodyActions(bodyActions);
                if (dicDecodeActions.ContainsKey(objPathId))
                {
                    if (!dicDecodeActions[objPathId].Contains(action))
                    {
                        dicDecodeActions[objPathId].Add(action);
                    }
                }
                else
                {
                    List<CETYPE.CEAction> listActions = new List<CETYPE.CEAction>();
                    listActions.Add(action);
                    dicDecodeActions[objPathId] = listActions;
                }
            }

            foreach (int objPathId in dicDecodeActions.Keys)
            {
                m_destListObjPaths.Clear(); // Clear the objPaths.
                AnalyzeActionById(objPathId, m_destListObjPaths);
                if (m_destListObjPaths.Count > 0)
                {
                    CSOMEvalObject csomEvalObject = new CSOMEvalObject(m_destListObjPaths);
                    bool bRet = csomEvalObject.Run();
                    if (bRet)
                    {
                        bool bDecodeOver = csomEvalObject.DecodeOver;
                        Object evalObj = csomEvalObject.EvalObject;
                        bool bColl = csomEvalObject.CheckCollection;
                        if (bColl)
                        {
                            bCollection = true;
                        }
                        if (web != null && evalObj != null)
                        {
                            List<CETYPE.CEAction> userActions = dicDecodeActions[objPathId];
                            foreach (CETYPE.CEAction userAction in userActions)
                            {
                                CETYPE.CEAction destAction = userAction;
                                if (destAction.Equals(CETYPE.CEAction.Delete) && !bDecodeOver)
                                {
                                    destAction = CETYPE.CEAction.Write; // Set "edit" action for "delete property" case.
                                }
                                string evalUrl = Globals.ConstructObjectUrl(evalObj);
                                if(dicEvalObjs.ContainsKey(evalUrl))
                                {
                                    CSOMEvalObj csomEvalObj = dicEvalObjs[evalUrl];
                                    if(!csomEvalObj.actions.Contains(destAction))
                                    {
                                        csomEvalObj.actions.Add(destAction);
                                    }
                                }
                                else
                                {
                                    CSOMEvalObj csomEvalObj = new CSOMEvalObj();
                                    csomEvalObj.evalUrl = evalUrl;
                                    csomEvalObj.evalObj = evalObj;
                                    csomEvalObj.actions.Add(destAction);
                                    dicEvalObjs.Add(evalUrl, csomEvalObj);
                                }
                            }
                        }
                    }
                }
            }

            foreach (CSOMEvalObj csomEvalObj in dicEvalObjs.Values)
            {
                List<CETYPE.CEAction> userActions = csomEvalObj.actions;
                if (userActions.Contains(CETYPE.CEAction.Read) && userActions.Count > 1)
                {
                    userActions.Remove(CETYPE.CEAction.Read); // ignore the "Read" when have other action.
                }
                foreach (CETYPE.CEAction destAction in userActions)
                {
                    if (csomEvalObj.evalObj != null && web != null && m_request != null)
                    {
                        SPObjectEvaluation evaluter = new SPObjectEvaluation(m_request, web, csomEvalObj.evalObj, destAction);
                        bAllow = evaluter.Run();
                        if (!bAllow)
                        {
                            m_bResult = false;
                            m_denyMsg = evaluter.GetDenyMessage();
                            return;
                        }
                    }
                }
            }

            // Do trimming for "collection" type.
            if (bCollection || m_bCollection)
            {
                ResponseFilter filter = ResponseFilters.Current(m_response);
                filter.AddFilterType(FilterType.CsomTrimmer);
                filter.Web = web;
            }
        }

        // Set "Read", "Write" and "delete" action.
        private CETYPE.CEAction DecodeBodyActions(CSOMBodyActions bodyActions)
        {
            CETYPE.CEAction action = CETYPE.CEAction.Unknown;
            if (bodyActions != null && bodyActions.nodeName != null)
            {
                if (bodyActions.nodeName.Equals("Method") && bodyActions.name != null)
                {
                    if (bodyActions.name.Contains("Get") || bodyActions.name.Contains("Open") || bodyActions.name.Contains("Render")
                        || bodyActions.name.Equals("ReserveListItemId") || bodyActions.name.Equals("DoesUserHavePermissions")
                        || bodyActions.name.Equals("DoesPushNotificationSubscriberExist"))
                    {
                        action = CETYPE.CEAction.Read;
                    }
                    else if (bodyActions.name.Equals("DeleteObject") || bodyActions.name.Equals("Recycle"))
                    {
                        action = CETYPE.CEAction.Delete;
                    }
                    else
                    {
                        action = CETYPE.CEAction.Write;
                    }
                }
                else if(bodyActions.nodeName.Equals("Query"))
                {
                    action = CETYPE.CEAction.Read;
                }
            }
            return action;
        }

        // Find by "ParentId" and add to list end.
        private void AnalyzeActionById(int id, List<CSOMBodyObjectPaths> destListObjPaths)
        {
            if (m_dicObjPaths.ContainsKey(id))
            {
                CSOMBodyObjectPaths objPaths = m_dicObjPaths[id];
                destListObjPaths.Add(objPaths);
                if (objPaths.parentId != -1)
                {
                    AnalyzeActionById(objPaths.parentId, destListObjPaths);
                }
            }
        }

        private void CheckCollectionInXml(XmlNode node, ref bool bCollection)
        {
            if(node.HasChildNodes)
            {
                foreach(XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Name.Equals("Property", StringComparison.OrdinalIgnoreCase))
                    {
                        if (childNode.Attributes["Name"] != null)
                        {
                            XmlAttribute attr = childNode.Attributes["Name"];
                            if(attr.Value.Equals("Webs", StringComparison.OrdinalIgnoreCase)
                                || attr.Value.Equals("Lists", StringComparison.OrdinalIgnoreCase)
                                || attr.Value.Equals("Files", StringComparison.OrdinalIgnoreCase)
                                || attr.Value.Equals("Folders", StringComparison.OrdinalIgnoreCase))
                            {
                                bCollection = true;
                                return;
                            }
                            else
                            {
                                CheckCollectionInXml(childNode, ref bCollection);
                            }
                        }
                    }
                    else
                    {
                        CheckCollectionInXml(childNode, ref bCollection);
                    }
                }

            }
        }
        private StaticMethodType GetStaticMethodType(string strStaticMethod)
        {
            StaticMethodType smt = StaticMethodType.SMT_Unknown;
            if (strStaticMethod == "SetRating")
            {
                smt = StaticMethodType.SMT_SetRating;
            }
            else if (strStaticMethod == "GetDocumentLibraries")
            {
                smt = StaticMethodType.SMT_GetDocumentLibraries;
            }
            return smt;
        }

        private void ProcessStaticMethods(XmlNode node)
        {
            SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
            CETYPE.CEAction action = CETYPE.CEAction.Unknown;
            foreach (XmlNode nodestaticMtd in node.ChildNodes)
            {
                if (nodestaticMtd.Name != "StaticMethod")
                {
                    continue;
                }
                object objEval = null;
                try
                {
                    objEval = ProcessStaticMethod(nodestaticMtd, out action);
                }
                catch(Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ProcessStaticMethods:", null, ex);
                }
                if (objEval != null && web != null && m_request != null)
                {
 					SPObjectEvaluation eva = new SPObjectEvaluation(m_request, web, objEval, action);
                    if (!eva.Run())
                    {
	                    m_bResult = false;
                        m_denyMsg = eva.GetDenyMessage();
                        return;
                   }
                }
            }
        }

        private object ProcessStaticMethod(XmlNode nodestaticMtd, out CETYPE.CEAction action)
        {
            action = CETYPE.CEAction.Read;
            SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
            object objEval = null;

            //get method name
            XmlAttribute attName = nodestaticMtd.Attributes["Name"];
            if (attName != null)
            {
                string strAttValue = attName.Value;

                XmlNode nodeParams = nodestaticMtd["Parameters"];
                XmlNode nodeFirst = null;
                if (nodeParams != null && nodeParams.HasChildNodes)
                {
                    nodeFirst = nodeParams.FirstChild;
                    if (nodeFirst == null)
                    {
                        return objEval;
                    }
                }
                StaticMethodType smt = GetStaticMethodType(strAttValue);
                switch(smt)
                {
                    case StaticMethodType.SMT_SetRating:
                        string strListGuid = nodeFirst.InnerText;
                        if (!string.IsNullOrEmpty(strListGuid))
                        {
                            SPList objList = web.Lists.GetList(new Guid(strListGuid), true);
                            if (objList != null)
                            {
                                //get item id from second child node.
                                XmlNode nodeItemId = nodeFirst.NextSibling;
                                if (nodeItemId != null)
                                {
                                    int nItemId = int.Parse(nodeItemId.InnerText);
                                    if (nItemId > 0)
                                    {
                                        objEval = objList.GetItemById(nItemId);
                                        action = CETYPE.CEAction.Write;
                                    }
                                }
                            }
                        }
                        break;
                    case StaticMethodType.SMT_GetDocumentLibraries:
                        string strSiteUrl = nodeFirst.InnerText;
                        if (!string.IsNullOrEmpty(strSiteUrl))
                        {
                            try
                            {
                                using (SPSite site = new SPSite(strSiteUrl))
                                {
                                    SPWeb opWeb = site.OpenWeb();
                                    SPEEvalAttrs.Current().AddDisposeWeb(opWeb);
                                    objEval = opWeb;
                                    action = CETYPE.CEAction.Read;
                                }
                            }
                            catch
                            {
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            return objEval;
        }

        private void AnalyzeXml(string xmlBody)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = xmlBody;
            XmlNode rootNode = xmlDoc.DocumentElement;


            if (rootNode.Name.Equals("Request") && rootNode["ObjectPaths"] != null && rootNode["Actions"] != null)
            {
                // Get the "Query" and "method" objects in xml.
                XmlNode actionsNode = rootNode["Actions"];
                if (actionsNode["StaticMethod"] != null)
                {
                    ProcessStaticMethods(actionsNode);
                }
                else
                {
	                AnalyzeActionsAttributes(actionsNode);

	                // Get the "Method", "StaticProperty", "Property", "Identity" objects in xml.
	                XmlNode objPathsNode = rootNode["ObjectPaths"];
	                AnalyzeObjectPathsAttributes(objPathsNode);

	                // Get collection flag in xml boday.
	                CheckCollectionInXml(actionsNode, ref m_bCollection);
				}
            }
        }

        private void AnalyzeActionsAttributes(XmlNode xmlNode)
        {
            if (xmlNode != null && xmlNode.HasChildNodes)
            {
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    if ((childNode.Name.Equals("Query") || childNode.Name.Equals("Method"))
                        && childNode.Attributes["Id"] != null && childNode.Attributes["ObjectPathId"] != null)
                    {
                        CSOMBodyActions bodyAtions = new CSOMBodyActions();
                        bodyAtions.nodeName = childNode.Name;
                        bodyAtions.id = int.Parse(childNode.Attributes["Id"].Value);
                        bodyAtions.objPathId = int.Parse(childNode.Attributes["ObjectPathId"].Value);
                        if (childNode.Name.Equals("Method") && childNode.Attributes["Name"] != null)
                        {
                            bodyAtions.name = childNode.Attributes["Name"].Value;
                        }
                        m_listActions.Add(bodyAtions);
                    }
                    else
                    {
                        AnalyzeActionsAttributes(childNode);
                    }
                }
            }
        }

        // Get the "Id", "ParentId" and "Name" in XML attributes.
        private void AnalyzeObjectPathsAttributes(XmlNode xmlNode)
        {
            if (xmlNode != null && xmlNode.HasChildNodes)
            {
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    if ((childNode.Name.Equals("StaticProperty") || childNode.Name.Equals("Property") || childNode.Name.Equals("Identity")
                        || childNode.Name.Equals("Method") || childNode.Name.Equals("StaticMethod"))
                        && childNode.Attributes["Id"] != null && childNode.Attributes["Name"] != null)
                    {
                        CSOMBodyObjectPaths bodyObjPath = new CSOMBodyObjectPaths();
                        bodyObjPath.nodeName = childNode.Name;
                        bodyObjPath.id = int.Parse(childNode.Attributes["Id"].Value);
                        if (childNode.Attributes["ParentId"] != null)
                        {
                            bodyObjPath.parentId = int.Parse(childNode.Attributes["ParentId"].Value);
                        }
                        bodyObjPath.name = childNode.Attributes["Name"].Value;
                        if ((childNode.Name.Equals("Method") || childNode.Name.Equals("StaticMethod")) && childNode["Parameters"] != null)
                        {
                            XmlNode paramsNode = childNode["Parameters"];
                            foreach (XmlNode paramNode in paramsNode.ChildNodes)
                            {
                                if (paramNode.Name.Equals("Parameter"))
                                {
                                    bodyObjPath.parameters.Add(paramNode.InnerText);
                                }
                            }
                        }
                        m_dicObjPaths[bodyObjPath.id] = bodyObjPath;
                    }
                    else
                    {
                        AnalyzeObjectPathsAttributes(childNode);
                    }
                }
            }
        }
    }

    class CSOMEvalObject
    {
        private List<CSOMBodyObjectPaths> m_listObjPaths; // Path work flow for each object that need to do evaluation.
        private SPWeb m_web;
        private bool m_bDecodeOver; // Flag for "m_evalObj": if it is over to check "delete" action.
        public bool DecodeOver
        {
            get { return m_bDecodeOver; }
        }

        private object m_evalObj; // The object to do evaluation.
        public object EvalObject
        {
            get { return m_evalObj; }
        }

        private bool m_bCollection; // The flag to check "Collection" type.
        public bool CheckCollection
        {
            get { return m_bCollection; }
        }

        public CSOMEvalObject(List<CSOMBodyObjectPaths> listObjPaths)
        {
            m_web = SPControl.GetContextWeb(HttpContext.Current);
            m_bDecodeOver = false;
            m_evalObj = null;
            m_bCollection = false;
            m_listObjPaths = listObjPaths;
        }

        public bool Run()
        {
            AnalyzeBeginAction();
            if (m_web != null && m_evalObj != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void AnalyzeBeginAction()
        {
            try
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);
                if (objPaths.nodeName.Equals("StaticProperty"))
                {
                    if (objPaths.name.Equals("Current"))
                    {
                        ContextRun();
                    }
                }
                else if (objPaths.nodeName.Equals("Identity"))
                {
                    object obj = Globals.GetObjectFromIdentity(objPaths.name);
                    if (obj != null)
                    {
                        if (obj is SPSite)
                        {
                            SiteRun(obj as SPSite);
                        }
                        else if (obj is SPWeb)
                        {
                            WebRun(obj as SPWeb);
                        }
                        else if (obj is SPList)
                        {
                            ListRun(obj as SPList);
                        }
                        else if (obj is SPFolder)
                        {
                            FolderRun(obj as SPFolder);
                        }
                        else if (obj is SPListItem)
                        {
                            ListItemRun(obj as SPListItem);
                        }
                        else if (obj is SPFile)
                        {
                            FileRun(obj as SPFile);
                        }
                        else if (obj is SPField)
                        {
                            FieldRun(obj as SPField);
                        }
                        else if (obj is SPView)
                        {
                            ViewRun(obj as SPView);
                        }
                        else if (obj is SPFileVersion)
                        {
                            FileVersionRun(obj as SPFileVersion);
                        }
                    }
                }
                else if (objPaths.nodeName.Equals("StaticMethod"))
                {
                    if (objPaths.name.Equals("GetListItemSharingInformation"))
                    {
                        ListItemSharingRun(objPaths.parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AnalyzeBeginAction:", null, ex);
            }
        }

        private void ListItemSharingRun(List<string> parameters)
        {
            string strListGuid = parameters[0];
            string strItemId = parameters[1];
            if (!string.IsNullOrEmpty(strListGuid) && !string.IsNullOrEmpty(strItemId))
            {
                SPList objList = m_web.Lists.GetList(new Guid(strListGuid), true);
                if (objList != null)
                {
                    //get item id from second child node.
                    int nItemId = int.Parse(strItemId);
                    m_evalObj = objList.GetItemById(nItemId);
                }
            }
        }

        private void ContextRun()
        {
            if (m_listObjPaths.Count > 0)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);
                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("Site"))
                    {
                        SPSite site = SPControl.GetContextSite(HttpContext.Current);
                        SiteRun(site);
                    }
                    else if (objPaths.name.Equals("Web"))
                    {
                        SPWeb web = SPControl.GetContextWeb(HttpContext.Current);
                        WebRun(web);
                    }
                }
            }
        }

        private void SiteRun(SPSite site)
        {
            if (site != null)
            {
                m_evalObj = site.RootWeb;
            }

            if (m_listObjPaths.Count > 0 && site != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);
                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("RootWeb"))
                    {
                        WebRun(site.RootWeb);
                    }
                }
                else if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("OpenWeb"))
                    {
                        using (SPWeb web = site.OpenWeb(parameters[0]))
                        {
                            WebRun(web);
                        }
                    }
                    else if (objPaths.name.Equals("OpenWebById"))
                    {
                        SPWeb web = site.AllWebs[int.Parse(parameters[0])];
                        WebRun(web);
                    }
                    else if (objPaths.name.Equals("GetCatalog"))
                    {
                        SPListTemplateType templateType = (SPListTemplateType)(int.Parse(parameters[0]));
                        SPList list = site.GetCatalog(templateType);
                        ListRun(list);
                    }
                }
            }
        }

        private void WebRun(SPWeb web)
        {
            m_evalObj = web;
            if (m_listObjPaths.Count == 0)
            {
                m_bDecodeOver = true; // Decode over
            }

            if (m_listObjPaths.Count > 0 && web != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("Webs"))
                    {
                        WebCollectionRun(web.Webs);
                    }
                    else if (objPaths.name.Equals("Lists"))
                    {
                        ListCollectionRun(web.Lists);
                    }
                    else if (objPaths.name.Equals("Folders"))
                    {
                        FolderCollectionRun(web.Folders);
                    }
                    else if (objPaths.name.Equals("RootFolder"))
                    {
                        FolderRun(web.RootFolder);
                    }
                    else if (objPaths.name.Equals("ParentWeb"))
                    {
                        WebRun(web.ParentWeb);
                    }
                    else if (objPaths.name.Equals("SiteUserInfoList"))
                    {
                        ListRun(web.SiteUserInfoList);
                    }
                }
                else if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetFileById"))
                    {
                        FileRun(web.GetFile(new Guid(parameters[0])));
                    }
                    else if (objPaths.name.Equals("GetFileByServerRelativeUrl"))
                    {
                        FileRun(web.GetFile(parameters[0]));
                    }
                    else if (objPaths.name.Equals("GetFolderById"))
                    {
                        FolderRun(web.GetFolder(new Guid(parameters[0])));
                    }
                    else if (objPaths.name.Equals("GetFolderByServerRelativeUrl"))
                    {
                        FolderRun(web.GetFolder(parameters[0]));
                    }
                    else if (objPaths.name.Equals("GetSubwebsForCurrentUser"))
                    {
                        WebCollectionRun(web.GetSubwebsForCurrentUser());
                    }
                    else if (objPaths.name.Equals("GetCatalog"))
                    {
                        SPListTemplateType templateType = (SPListTemplateType)(int.Parse(parameters[0]));
                        SPList list = web.GetCatalog(templateType);
                        ListRun(list);
                    }
#if SP2016 || SP2019
                    else if (objPaths.name.Equals("GetList"))
                    {
                        ListRun(web.GetList(parameters[0]));
                    }
                    else if (objPaths.name.Equals("DefaultDocumentLibrary"))
                    {
                        ListRun(web.DefaultDocumentLibrary());
                    }
                    else if (objPaths.name.Equals("GetFileByGuestUrl"))
                    {
                        FileRun(web.GetFileByGuestUrl(parameters[0]));
                    }
                    else if (objPaths.name.Equals("GetFileByLinkingUrl"))
                    {
                        FileRun(web.GetFileByLinkingUrl(parameters[0]));
                    }
                    else if (objPaths.name.Equals("GetFileById"))
                    {
                        FileRun(web.GetFile(new Guid(parameters[0])));
                    }
                    else if (objPaths.name.Equals("GetFolderById"))
                    {
                        FolderRun(web.GetFolder(new Guid(parameters[0])));
                    }
# endif
                }
            }
        }

        private void WebCollectionRun(SPWebCollection webColl)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bCollection = true;
            }
        }

        private void ListRun(SPList list)
        {
            m_evalObj = list;
            if (m_listObjPaths.Count == 0)
            {
                m_bDecodeOver = true; // Decode over
            }

            if (m_listObjPaths.Count > 0 && list != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("RootFolder"))
                    {
                        FolderRun(list.RootFolder);
                    }
                    else if (objPaths.name.Equals("ParentWeb"))
                    {
                        WebRun(list.ParentWeb);
                    }
                }
                else if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetItemById") || objPaths.name.Equals("GetItemByStringId"))
                    {
                        ListItemRun(list.GetItemById(int.Parse(parameters[0])));
                    }
                    else if (objPaths.name.Equals("GetItems"))
                    {
                        ListItemCollectionRun(list.Items);
                    }
                }
            }
        }

        private void ListCollectionRun(SPListCollection listColl)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bCollection = true;
            }

            if (m_listObjPaths.Count > 0 && listColl != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetById"))
                    {
                        ListRun(listColl[new Guid(parameters[0])]);
                    }
                    else if (objPaths.name.Equals("GetByTitle"))
                    {
                        ListRun(listColl.TryGetList(parameters[0]));
                    }
                }
            }
        }

        private void ListItemRun(SPListItem item)
        {
            m_evalObj = item;
            if (m_listObjPaths.Count == 0)
            {
                m_bDecodeOver = true; // Decode over
            }

            if (m_listObjPaths.Count > 0 && item != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("ParentList"))
                    {
                        ListRun(item.ParentList);
                    }
                    else if (objPaths.name.Equals("File"))
                    {
                        FileRun(item.File);
                    }
                    else if (objPaths.name.Equals("Folder"))
                    {
                        FolderRun(item.Folder);
                    }
                }
            }
        }

        private void ListItemCollectionRun(SPListItemCollection itemColl)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bCollection = true;
            }

            if (m_listObjPaths.Count > 0 && itemColl != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetById") || objPaths.name.Equals("GetByStringId"))
                    {
                        ListItemRun(itemColl.GetItemById(int.Parse(parameters[0])));
                    }
                }
            }
        }

        private void FolderRun(SPFolder folder)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bDecodeOver = true; // Decode over
            }

            if (folder != null)
            {
                Object evalObj = Globals.GetListOrItemFromSPFolder(m_web, folder);
                if (evalObj != null)
                {
                    m_evalObj = evalObj;
                }
            }

            if (m_listObjPaths.Count > 0 && folder != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("ParentFolder"))
                    {
                        FolderRun(folder.ParentFolder);
                    }
                    else if (objPaths.name.Equals("Files"))
                    {
                        FileCollectionRun(folder.Files);
                    }
                    else if (objPaths.name.Equals("Folders"))
                    {
                        FolderCollectionRun(folder.SubFolders);
                    }
                }
            }
        }

        private void FolderCollectionRun(SPFolderCollection folderColl)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bCollection = true;
            }

            if (m_listObjPaths.Count > 0 && folderColl != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);
                if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetByUrl"))
                    {
                        FolderRun(folderColl[parameters[0]]);
                    }
                }
            }
        }

        private void FileRun(SPFile file)
        {
            if (file != null)
            {
                m_evalObj = Globals.GetSPListItemFromSPFile(m_web, file); // Get the item to do evaluation.
            }
            if (m_listObjPaths.Count == 0)
            {
                m_bDecodeOver = true; // Decode over
            }

            if (m_listObjPaths.Count > 0 && file != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Property"))
                {
                    if (objPaths.name.Equals("ListItemAllFields"))
                    {
                        ListItemRun(file.ListItemAllFields);
                    }
                }
            }
        }

        private void FileCollectionRun(SPFileCollection fileColl)
        {
            if (m_listObjPaths.Count == 0)
            {
                m_bCollection = true;
            }

            if (m_listObjPaths.Count > 0 && fileColl != null)
            {
                int lastIndx = m_listObjPaths.Count - 1;
                CSOMBodyObjectPaths objPaths = m_listObjPaths[lastIndx]; // get last action.
                m_listObjPaths.RemoveAt(lastIndx);

                if (objPaths.nodeName.Equals("Method"))
                {
                    List<string> parameters = objPaths.parameters;
                    if (objPaths.name.Equals("GetByUrl"))
                    {
                        FileRun(fileColl[parameters[0]]);
                    }
                }
            }
        }

        private void FieldRun(SPField field)
        {
            if (field != null && field.ParentList != null)
            {
                m_evalObj = field.ParentList; // Get the list to do evaluation.
            }
        }

        private void ViewRun(SPView view)
        {
            if (view != null && view.ParentList != null)
            {
                m_evalObj = view.ParentList; // Get the list to do evaluation.
            }
        }

        private void FileVersionRun(SPFileVersion fileVersion)
        {
            if (fileVersion != null)
            {
                m_evalObj = Globals.GetSPListItemFromSPFile(m_web, fileVersion.File); // Get the list item to do evaluation.
            }
        }
    }
}