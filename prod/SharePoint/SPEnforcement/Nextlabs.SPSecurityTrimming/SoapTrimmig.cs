using System;
using System.IO;
using System.Web;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class TrimNodeData
    {
        public string trimId;
        public string nodeStr;
        public XmlNode node;
        public int evalId;
        public bool bAllow;
        public string guid;
        public System.DateTime time;
        public SoapType soapType;
        public SpServiceType spServiceType;
        public object obj;
        public TrimNodeData()
        {
            trimId = null;
            nodeStr = null;
            node = null;
            evalId = -1;
            bAllow = true;
            guid = null;
            time = new DateTime();
            soapType = SoapType.Unknown;
            spServiceType = SpServiceType.Unknown;
            obj = null;
        }
    }

    public enum SoapType
    {
        Web,
        List,
        ListItem,
        CSOMResponse,
        Unknown
    }

    public class SoapTrimmer
    {
        private HttpResponse m_response;
        private SPWeb m_web;
        private object m_evalObj;

        public SoapTrimmer(HttpResponse response, SPWeb web, object evalObj)
        {
            m_response = response;
            m_web = web;
            m_evalObj = evalObj;
        }

        private void SelectNodes(XmlNode xmlNode, string nodeName, List<XmlNode> listNodes)
        {
            if (xmlNode.HasChildNodes)
            {
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    if (childNode.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                    {
                        listNodes.Add(childNode);
                    }
                    else
                    {
                        SelectNodes(childNode, nodeName, listNodes);
                    }
                }
            }
        }

        private void SetSelectedNode(XmlNode rootNode, string strSelect, string attrName, SoapType soapType, List<TrimNodeData> listNodeData)
        {
            if (rootNode != null && !string.IsNullOrEmpty(strSelect) && !string.IsNullOrEmpty(attrName) && listNodeData != null)
            {
                List<XmlNode> listNodes = new List<XmlNode>();
                SelectNodes(rootNode, strSelect, listNodes);
                foreach (XmlNode listNode in listNodes)
                {
                    XmlAttribute attrId = listNode.Attributes[attrName];
                    if (attrId != null)
                    {
                        TrimNodeData nodeData = new TrimNodeData();
                        nodeData.node = listNode;
                        nodeData.trimId = attrId.Value;
                        nodeData.soapType = soapType;
                        listNodeData.Add(nodeData);
                    }
                }
            }
        }

        public string Run(string strInput)
        {
            bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
            string strFinal = strInput;
            if (-1 == m_response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
            {
                return strFinal;
            }

            if (m_web != null && !bIgnoreTrimControl)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return strFinal; // Don't do anything without trimming enable.
                    }
                }
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    string loadXml = strInput;
                    if (strInput.Contains("&lt;") && strInput.Contains("&gt;"))
                    {
                        // Use "HtmlDecode" to decode the xml.
                        loadXml = HttpUtility.HtmlDecode(strInput);
                    }
                    xmlDoc.LoadXml(loadXml);
                }
                catch
                {
                    try
                    {
                        xmlDoc.LoadXml(strInput);
                    }
                    catch
                    {
                        return ""; // Can't Decode this xml, deny this case default.
                    }
                }
                XmlNode rootNode = xmlDoc.DocumentElement;

                if (rootNode.Name.Contains("Envelope"))
                {
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                    {
                        // Get the objects in xml.("Webs", "Lists", "listitems")
                        List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                        if (bIgnoreTrimControl || manager.CheckSecurityTrimming())
                        {
                            SetSelectedNode(rootNode, "Web", "Url", SoapType.Web, listNodeData);
                            SetSelectedNode(rootNode, "List", "ID", SoapType.List, listNodeData);
                            SetSelectedNode(rootNode, "Library", "name", SoapType.List, listNodeData);
                            if (m_evalObj is SPList)
                            {
                                SPList list = m_evalObj as SPList;
                                if (bIgnoreTrimControl || manager.CheckListTrimming() || manager.CheckListTrimming(list))
                                {
                                    SetSelectedNode(rootNode, "item", "ID", SoapType.ListItem, listNodeData);
                                    SetSelectedNode(rootNode, "z:row", "ows_ID", SoapType.ListItem, listNodeData);
                                }
                            }
                        }
                        bool bDeny = false;
                        if (listNodeData.Count > 0)
                        {
                            SOAPResponseEvaluation soapEval = new SOAPResponseEvaluation(m_web, HttpContext.Current.Request.UserHostAddress, HttpContext.Current, m_evalObj);
                            soapEval.Run(listNodeData);
                            foreach (TrimNodeData nodeData in listNodeData)
                            {
                                if (!nodeData.bAllow && nodeData.node != null)
                                {
                                    bDeny = true;
                                    nodeData.node.ParentNode.RemoveChild(nodeData.node);
                                }
                            }
                            if (bDeny)
                            {
                                strFinal = xmlDoc.InnerXml;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SoapTrimmer Run:", null, ex);
            }
            return strFinal;
        }
    }

    public class SOAPResponseEvaluation
    {
        private SPWeb m_web;
        private string m_remoteAddr;
        private string m_userId;
        private HttpContext m_context;
        private object m_evalObj;
        public SOAPResponseEvaluation(SPWeb web, string remoteAddr, HttpContext context, object evalObj)
        {
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
            m_context = context;
            m_evalObj = evalObj;
        }

        private object GetObjectByTrimId(TrimNodeData nodeData)
        {
            object destObj = null;
            try
            {
                SoapType type = nodeData.soapType;
                string trimId = nodeData.trimId;
                if (type == SoapType.Web && m_evalObj is SPWeb)
                {
                    destObj = Utilities.GetCachedSPContent(null, trimId, Utilities.SPUrlWeb);
                }
                else if (type == SoapType.List && m_evalObj is SPWeb)
                {
                    SPWeb web = m_evalObj as SPWeb;
                    destObj = web.Lists.GetList(new Guid(trimId), true);
                }
                else if (type == SoapType.ListItem && m_evalObj is SPList)
                {
                    SPList list = m_evalObj as SPList;
                    destObj = list.GetItemById(int.Parse(trimId));
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during SOAPResponseEvaluation GetObjectByTrimId:", null, ex);
            }
            return destObj;
        }

        public bool Run(List<TrimNodeData> listNodeData)
        {
            EvaluationMultiple mulEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval);
            bool bDefault = Globals.GetPolicyDefaultBehavior();
            if (mulEval != null)
            {
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    try
                    {
                        int idRequest = -1;
                        object evalObj = GetObjectByTrimId(nodeData);
                        DateTime modifyTime = new DateTime(1, 1, 1);
                        string guid = Globals.GetSPObjectCacheGuid(evalObj, ref modifyTime);
                        nodeData.guid = guid;
                        nodeData.time = modifyTime;
                        bool bAllow = true;
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(m_userId, m_remoteAddr, guid, ref bAllow, modifyTime);
                        if (bExisted)
                        {
                            nodeData.bAllow = bAllow;
                        }
                        else
                        {
                            if (evalObj != null)
                            {
                                string evalUrl = Globals.ConstructObjectUrl(evalObj);
                                string srcName = string.Empty;
                                string[] srcAttr = new string[0];
                                Globals.GetSrcNameAndSrcAttr(evalObj, evalUrl, m_context, ref srcName, ref srcAttr);
                                mulEval.SetTrimRequest(evalObj, srcName, srcAttr, out idRequest);
                                nodeData.evalId = idRequest;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during SOAPResponseEvaluation run:", null, ex);
                    }

                }
                mulEval.run();
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    int evalId = nodeData.evalId;
                    if (evalId != -1)
                    {
                        nodeData.bAllow = mulEval.GetTrimEvalResult(evalId);
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(m_userId, m_remoteAddr, nodeData.guid, nodeData.bAllow, DateTime.Now, nodeData.time);
                    }
                    DateTime evalTime = DateTime.Now;
                }
                mulEval.ClearRequest();
                mulEval = null;
            }
            else
            {
                foreach (TrimNodeData item in listNodeData)
                {
                    item.bAllow = bDefault;
                }
            }
            return true;
        }
    }

    public enum SpServiceType
    {
        DlgView,
        SiteListView,
        SearchResult,
        SPWebs,
        SPLists,
        SPItems,
        Unknown
    }

    public class SpServiceTrimming
    {
        private SpServiceType m_Type;
        public SpServiceTrimming()
        {
            m_Type = SpServiceType.Unknown;
        }

        public void DoTrimming(HttpContext context, SPWeb web, SPList list, string remoteAddr)
        {

            // George: SpService Trimming for dialog view, sharepoint designer and SP2016 Mobile view.
            try
            {
                if (IfNeedTrim(web, list, context.Request))
                {
                    ResponseFilter filter = ResponseFilters.Current(context.Response);
                    filter.AddFilterType(FilterType.SPServiceTrimmer);
                    filter.Web = web;
                    filter.List = list;
                    filter.RemoteAddr = remoteAddr;
                    filter.ServiceType = m_Type;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer DoTrimming:", null, ex);
            }
        }

        public bool IfNeedTrim(SPWeb web, SPList list, HttpRequest request)
        {
            // Exception 11, check web, list is not null.
            bool bNeed = false;
            try
            {
                string rawUrl = Globals.UrlDecode(request.RawUrl);
                bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(request);
#if SP2016 || SP2019
                string referUrl = "";
                if (request.UrlReferrer != null)
                {
                    referUrl = request.UrlReferrer.ToString();
                }

                // Fix bug 38406, serach result trimming in SP2016.
                if(-1 != rawUrl.IndexOf("/_vti_bin/client.svc", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(referUrl)
                    && -1 != referUrl.IndexOf("/osssearchresults.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    m_Type = SpServiceType.SearchResult;
                    return true;
                }
#endif
                string method = request.HttpMethod;
                string contentType = request.ContentType;
                int contentLength = request.ContentLength;

                if (web != null)
                {
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                    {
                        if (bIgnoreTrimControl || manager.CheckSecurityTrimming())
                        {
                            bool bTrimLists = bIgnoreTrimControl || manager.CheckListTrimming();
                            bool bSpcListTrim = false;
                            if (list != null)
                            {
                                // check the lis trimming for special list.
                                bSpcListTrim = manager.CheckListTrimming(list);
                            }
                            bool bListTrim = bTrimLists || bSpcListTrim;
                            if (rawUrl.EndsWith("/_vti_bin/client.svc/ProcessQuery", StringComparison.OrdinalIgnoreCase) && contentLength != 0
                                && (!string.IsNullOrEmpty(contentType) && -1 != contentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase)))
                            {
                                Stream stream = request.InputStream;
                                bNeed = IsSPDesinerTrim(stream, request.ContentEncoding, bTrimLists);
                            }
                            else if (bListTrim && method.Equals("PROPFIND", StringComparison.OrdinalIgnoreCase))
                            {
                                string depth = request.Headers["Depth"];
                                if (depth != null && depth.Equals("1"))
                                {
                                    m_Type = SpServiceType.DlgView;
                                    bNeed = true;
                                }
                            }
                            else if (bListTrim && -1 != rawUrl.IndexOf("/_vti_bin/owssvr.dll") && -1 != rawUrl.IndexOf("dialogview=FileOpen"))
                            {
                                m_Type = SpServiceType.DlgView;
                                bNeed = true;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer IfNeedSpServiceTrim:", null, ex);
            }
            return bNeed;
        }

        public bool IsSPDesinerTrim(Stream stream, Encoding encoding, bool bTrimLists)
        {
            bool bTrim = false;
            try
            {
                byte[] contentBuf = new byte[stream.Length];
                long oldPos = stream.Seek(0, SeekOrigin.Current);
                stream.Read(contentBuf, 0, (int)stream.Length);
                string strBody = encoding.GetString(contentBuf);
                //set stream position back
                stream.Seek(oldPos, SeekOrigin.Begin);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = strBody;
                XmlNode node = xmlDoc.DocumentElement;
                XmlNode objPath = node["ObjectPaths"];
                if (objPath != null)
                {
                    bool bWebTrim = false;
                    bool bListTrim = false;
                    foreach (XmlNode child in objPath.ChildNodes)
                    {
                        if (child.Name.Equals("Property", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (XmlAttribute attr in child.Attributes)
                            {
                                if (attr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                    && attr.Value.Equals("Lists", StringComparison.OrdinalIgnoreCase))
                                {
                                    bListTrim = true;
                                    break;
                                }
                            }
                        }
                        else if (child.Name.Equals("Method", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (XmlAttribute attr in child.Attributes)
                            {
                                if (attr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                    && (attr.Value.Equals("GetSubwebsForCurrentUser", StringComparison.OrdinalIgnoreCase) || attr.Value.Equals("GetCustomListTemplates", StringComparison.OrdinalIgnoreCase)))
                                {
                                    bWebTrim = true;
                                    break;
                                }
                            }
                        }
                        if (bWebTrim && bListTrim) break;
                    }
                    if (bWebTrim && bListTrim)
                    {
                        m_Type = SpServiceType.SPWebs;
                        if (bTrimLists)
                        {
                            m_Type = SpServiceType.SPLists;
                        }
                        bTrim = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer IsSPDesinerTrim:", null, ex);
            }

            return bTrim;
        }

    }

    public class SPServiceTrimmer
    {
        private SPWeb m_web;
        private SPList m_list;
        private string m_remoteAddr;
        private SpServiceType m_type;
        private HttpResponse m_response;
        public SPServiceTrimmer(HttpResponse response, SPWeb web, string remoteAddr, SPList list, SpServiceType type)
        {
            m_response = response;
            m_web = web;
            m_list = list;
            m_remoteAddr = remoteAddr;
            m_type = type;
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            try
            {

                if (-1 != m_response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = XmlTrimming(strInput);
                }
                else if (-1 != m_response.ContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase))
                {
#if SP2016 || SP2019
                    if (m_type.Equals(SpServiceType.SearchResult))
                    {
                        strFinal = SearchResultTrimming(strInput);
                    }
                    else
#endif
                    {
                        strFinal = JsonTrimming(strInput);
                    }
                    if (!strFinal.Equals(strInput) && !TrimmerGlobal.CheckJsonFormat(strFinal))
                    {
                        strFinal = strInput;
                    }
                }
                else if (-1 != m_response.ContentType.IndexOf("html", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = HtmlTrimming(strInput);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer Run:", null, ex);
            }

            return strFinal;
        }
        public string XmlTrimming(string responseXml)
        {
            string strFinal = responseXml;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = responseXml;
                XmlNode node = xmlDoc.DocumentElement;
                List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                if (node != null)
                {
                    strFinal = responseXml;
                    if (m_type.Equals(SpServiceType.DlgView) && node.Name.Equals("D:multistatus"))
                    {
                        foreach (XmlNode child in node.ChildNodes)
                        {
                            if (child.Name.Equals("D:response"))
                            {
                                string id = child["D:href"].InnerXml;
                                if (!string.IsNullOrEmpty(id))
                                {
                                    TrimNodeData nodeData = new TrimNodeData();
                                    nodeData.trimId = id;
                                    nodeData.node = child;
                                    nodeData.spServiceType = SpServiceType.DlgView;
                                    listNodeData.Add(nodeData);
                                }
                            }
                        }
                    }
                    if (listNodeData.Count != 0)
                    {
                        SpServiceEvaluation SpServiceEval = new SpServiceEvaluation(m_web, m_list, m_remoteAddr);
                        SpServiceEval.Run(listNodeData);
                        foreach (TrimNodeData nodeData in listNodeData)
                        {
                            if (!nodeData.bAllow&& nodeData.node != null)
                            {
                                node.RemoveChild(nodeData.node);
                            }
                        }

                        strFinal = xmlDoc.InnerXml;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer XmlTrimming:", null, ex);
            }

            return strFinal;
        }



        private void CheckListsWebsData(string metaData, string[] splits, string joinText, string idFlag, List<TrimNodeData> listNodeData, SpServiceType SpServiceType)
        {
            string endData = metaData;
            string[] datas = metaData.Split(splits, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < datas.Length; i++)
            {
                try
                {
                    string data = datas[i];
                    data = joinText + data; // add splits string in begin
                    int ind = data.IndexOf(idFlag);
                    if (-1 != ind)
                    {
                        string id = data.Substring(ind + idFlag.Length, 36); // "list id and web id"
                        TrimNodeData nodeData = new TrimNodeData();
                        nodeData.trimId = id;
                        nodeData.nodeStr = data;
                        nodeData.spServiceType = SpServiceType;
                        listNodeData.Add(nodeData);
                    }
                }
                catch
                { }
            }
        }

        private void JsonSplit(string metaData, List<TrimNodeData> listNodeData, SpServiceType spServiceType)
        {
            string key = "";  // means "id", "guid" or "url".
            if (m_type.Equals(SpServiceType.SearchResult))
            {
                key = "\"Path\"";
            }

            string endData = metaData;
            List<string> datas = new List<string>();

            TrimmerGlobal.SplitMetaData("{", "}", metaData, datas);
            foreach (string data in datas)
            {
                string id = TrimmerGlobal.GetPairValue(data, key);

                // Deault is allow for data that is not contains "Path".
                if (!string.IsNullOrEmpty(id) || m_type.Equals(SpServiceType.SearchResult))
                {
                    TrimNodeData nodeData = new TrimNodeData();
                    nodeData.trimId = id;
                    nodeData.nodeStr = data;
                    nodeData.spServiceType = spServiceType;
                    listNodeData.Add(nodeData);
                }
            }
        }

        public string SearchResultTrimming(string responseJson)
        {
            string finalJson = responseJson;
            string beginStr = "\"ResultRows\":";
            int indBegin = finalJson.IndexOf(beginStr, StringComparison.OrdinalIgnoreCase);
            while (-1 != indBegin)
            {
                try
                {
                    string metaData = finalJson.Substring(indBegin + beginStr.Length);
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    bool bRet = TrimmerGlobal.GetPairString("[", "]", metaData, ref metaData);
                    if (bRet)
                    {
                        JsonSplit(metaData, listNodeData, m_type);
                    }

                    if (listNodeData.Count > 0)
                    {
                        SpServiceEvaluation SpServiceEval = new SpServiceEvaluation(m_web, m_list, m_remoteAddr);
                        SpServiceEval.Run(listNodeData);
                        List<string> dataList = new List<string>();
                        bool bDeny = false;
                        foreach (TrimNodeData node in listNodeData)
                        {
                            if (node.bAllow)
                            {
                                if (!string.IsNullOrEmpty(node.nodeStr))
                                {
                                    dataList.Add(node.nodeStr);
                                }
                            }
                            else
                            {
                                bDeny = true;
                            }
                        }
                        if (bDeny)
                        {
                            string endData = string.Join(",", dataList.ToArray());
                            finalJson = finalJson.Replace(metaData, endData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer SearchResultTrimming:", null, ex);
                }
                indBegin = finalJson.IndexOf(beginStr, indBegin + beginStr.Length, StringComparison.OrdinalIgnoreCase);
            }
            return finalJson;
        }

        // Todo: Use other function to implement decode "Json".
        public string JsonTrimming(string responseJson)
        {
            string finalJson = responseJson;
            try
            {
                string[] collection = { "\"_ObjectType_\":\"SP.ListCollection\",\"_Child_Items_\":[", "\"_ObjectType_\":\"SP.WebCollection\",\"_Child_Items_\":[" };
                List<string[]> splits = new List<string[]>();
                splits.Add(new string[] { "{\r\"_ObjectType_\":\"SP.List\"", "{ \"_ObjectType_\":\"SP.List\"" });
                splits.Add(new string[] { "{\r\"_ObjectType_\":\"SP.Web\"", "{ \"_ObjectType_\":\"SP.Web\"" });
                string[] joinText = { "{ \"_ObjectType_\":\"SP.List\"", "{ \"_ObjectType_\":\"SP.Web\"" };
                string[] idFlag = { "list:", "web:" };
                SpServiceType[] spServiceType = { SpServiceType.SPLists, SpServiceType.SPWebs };
                List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                for (int i = 0; i < collection.Length; i++)
                {
                    int indBegin = responseJson.IndexOf(collection[i]);
                    // Check collection.
                    if (-1 != indBegin)
                    {
                        string metaData = responseJson.Substring(indBegin);
                        bool bRet = TrimmerGlobal.GetPairString("[", "]", metaData, ref metaData);
                        if (bRet)
                        {
                            finalJson = finalJson.Replace(metaData, "");
                            CheckListsWebsData(metaData, splits[i], joinText[i], idFlag[i], listNodeData, spServiceType[i]);
                        }
                    }
                }
                if (listNodeData.Count > 0)
                {
                    StringBuilder listsStrBuild = new StringBuilder();
                    StringBuilder websStrBuild = new StringBuilder();
                    SpServiceEvaluation SpServiceEval = new SpServiceEvaluation(m_web, m_list, m_remoteAddr);
                    SpServiceEval.Run(listNodeData);
                    foreach (TrimNodeData node in listNodeData)
                    {
                        if (node.bAllow && !string.IsNullOrEmpty(node.nodeStr))
                        {
                            if (node.spServiceType == SpServiceType.SPLists)
                            {
                                listsStrBuild.Append(node.nodeStr);
                            }
                            else if (node.spServiceType == SpServiceType.SPWebs)
                            {
                                websStrBuild.Append(node.nodeStr);
                            }
                        }
                    }
                    int listsJsonPos = finalJson.IndexOf(collection[0], StringComparison.OrdinalIgnoreCase);
                    if (listsJsonPos != -1)
                    {
                        finalJson = finalJson.Insert(listsJsonPos + collection[0].Length, listsStrBuild.ToString().TrimEnd(','));
                    }

                    int websJsonPos = finalJson.IndexOf(collection[1], StringComparison.OrdinalIgnoreCase);
                    if (websJsonPos != -1)
                    {
                        finalJson = finalJson.Insert(websJsonPos + collection[1].Length, websStrBuild.ToString().TrimEnd(','));
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPServiceTrimmer JsonTrimming:", null, ex);
            }
            return finalJson;
        }

        public string HtmlTrimming(string responseData)
        {
            string html = responseData;
            string tail = responseData;
            string node = null;
            string id = null;
            int begin = 0;
            int end = 0;
            int ind = 0;
            List<TrimNodeData> listNodeData = new List<TrimNodeData>();
            if (-1 != tail.IndexOf("FileDialogViewTable", StringComparison.OrdinalIgnoreCase))
            {
                while (-1 != (begin = tail.IndexOf("<tr fileattribute", StringComparison.OrdinalIgnoreCase))
                    || -1 != (begin = tail.IndexOf("<tr class", StringComparison.OrdinalIgnoreCase)))
                {
                    end = tail.IndexOf("</tr>", begin, StringComparison.OrdinalIgnoreCase);
                    if (-1 != end)
                    {
                        node = tail.Substring(begin, end + 5 - begin);
                        if (-1 != (ind = node.IndexOf("id=\"", StringComparison.OrdinalIgnoreCase)))
                        {
                            id = node.Substring(ind + 4, node.IndexOf("\"", ind + 4) - (ind + 4));
                            if (id != null)
                            {
                                TrimNodeData nodeData = new TrimNodeData();
                                nodeData.nodeStr = node;
                                nodeData.trimId = id;
                                if (-1 != node.IndexOf("<tr class", StringComparison.OrdinalIgnoreCase))
                                {
                                    nodeData.spServiceType = SpServiceType.SiteListView;
                                }
                                else
                                {
                                    nodeData.spServiceType = SpServiceType.DlgView;
                                }
                                listNodeData.Add(nodeData);
                            }
                        }
                        tail = tail.Substring(end);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (listNodeData.Count > 0)
            {
                SpServiceEvaluation SpServiceEval = new SpServiceEvaluation(m_web, m_list, m_remoteAddr);
                SpServiceEval.Run(listNodeData);
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    if (!nodeData.bAllow && nodeData.nodeStr != null)
                    {
                        html = html.Replace(nodeData.nodeStr, "");
                    }
                }
            }
            return html;
        }
    }


    public class SpServiceEvaluation
    {
        private SPWeb m_web;
        private SPList m_list;
        private string m_remoteAddr;
        private string m_userId;

        public SpServiceEvaluation(SPWeb web, SPList list, string remoteAddr)
        {
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
            m_list = list;
        }

        public bool Run(List<TrimNodeData> listNodeData)
        {
            try
            {
                EvaluationMultiple mulEval = null;
                TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval);
                if (mulEval == null || listNodeData == null || listNodeData.Count == 0)
                {
                    return false;
                }
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    if (!string.IsNullOrEmpty(nodeData.trimId))
                    {
                        SetEvalAttrBytrimId(mulEval, nodeData);
                    }
                }
                mulEval.run();
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    int evalId = nodeData.evalId;
                    if (evalId != -1)
                    {
                        bool bAllow = mulEval.GetTrimEvalResult(evalId);
                        nodeData.bAllow = bAllow;
                        DateTime evalTime = DateTime.Now;
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(m_userId, m_remoteAddr, nodeData.guid, bAllow, evalTime, nodeData.time);
                    }
                }
                mulEval.ClearRequest();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SpServiceEvaluation run:", null, ex);
            }
            return true;
        }

        public void SetEvalAttrBytrimId(EvaluationMultiple mulEval, TrimNodeData nodeData)
        {
            SpServiceType type = nodeData.spServiceType;
            string trimId = nodeData.trimId;
            object obj = null;
            string url = null;
            if(!string.IsNullOrEmpty(trimId))
            {
                trimId = Globals.UrlDecode(trimId);
                if (type.Equals(SpServiceType.DlgView))
                {
                    url = trimId;
                    SPListItem item = (SPListItem)Utilities.GetCachedSPContent(m_web, trimId, Utilities.SPUrlListItem);
                    if (item == null && -1 != trimId.IndexOf("Attachments", StringComparison.OrdinalIgnoreCase))
                    {
                        item = Globals.ParseItemFromAttachmentURL(m_web, trimId);
                    }
                    // Add sites and lists trimming in pop dialog view.
                    if (item == null)
                    {
                        url = url.Trim('/');
                        SPList list = (SPList)Utilities.GetCachedSPContent(m_web, trimId, Utilities.SPUrlList);
                        if (list == null)
                        {
                            SPWeb web = (SPWeb)Utilities.GetCachedSPContent(null, trimId, Utilities.SPUrlWeb);
                            if (!web.Url.Equals(m_web.Url, StringComparison.OrdinalIgnoreCase))
                            {
                                obj = web;
                            }
                        }
                        else
                        {
                            obj = list;
                        }
                    }
                    else
                    {
                        obj = item;
                    }
                }
                else if (type.Equals(SpServiceType.SPWebs))
                {
                    SPWeb web = m_web.Webs[new Guid(trimId)];
                    obj = web;
                }
                else if (type.Equals(SpServiceType.SPLists))
                {
                    SPList list = m_web.Lists[new Guid(trimId)];
                    obj = list;
                }
                else if (type.Equals(SpServiceType.SiteListView))
                {
                    url = trimId;
                    SPList list = (SPList)Utilities.GetCachedSPContent(m_web, trimId, Utilities.SPUrlList);
                    if (list == null)
                    {
                        SPWeb web = (SPWeb)Utilities.GetCachedSPContent(null, trimId, Utilities.SPUrlWeb);
                        if (!web.Url.Equals(m_web.Url, StringComparison.OrdinalIgnoreCase))
                        {
                            obj = web;
                        }
                    }
                    else
                    {
                        obj = list;
                    }
                }
#if SP2016 || SP2019
                else if (type.Equals(SpServiceType.SearchResult))
                {
                    trimId = trimId.Replace("\\u002f", "/");
                    using (SPHttpUrlParser parser = new SPHttpUrlParser(trimId))
                    {
                        parser.Parse();
                        obj = parser.ParsedObject;
                    }
                    //obj = Utilities.GetSPObjectByFullUrl(m_web, trimId);
                }
#endif
            }

            CheckEvalCacheAndSetTrim(mulEval, obj, nodeData, url);
        }

        public void CheckEvalCacheAndSetTrim(EvaluationMultiple mulEval, object obj, TrimNodeData nodeData, string url)
        {
            string guid = null;
            bool bExisted = false;
            bool bAllow = true;
            System.DateTime modifyTime = new DateTime(1, 1, 1);
            string srcName = null;
            string[] srcAttr = null;
            int idRequest = -1;
            if (obj != null)
            {
                if (obj is SPWeb)
                {
                    SPWeb evalWeb = obj as SPWeb;
                    guid = evalWeb.Url;
                }
                else if (obj is SPList)
                {
                    SPList evalList = obj as SPList;
                    guid = NextLabs.Common.Utilities.ReConstructListUrl(evalList);
                }
                else if (obj is SPListItem)
                {
                    SPListItem evalItem = obj as SPListItem;
                    guid = evalItem.ParentList.ID.ToString() + evalItem.ID.ToString();
                    modifyTime = NextLabs.Common.Utilities.GetLastModifiedTime(evalItem);
                }
            }
            else if (url != null)
            {
                guid = url;
            }

            if (guid != null)
            {
                bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(m_userId, m_remoteAddr, guid, ref bAllow, modifyTime);
                if (bExisted)
                {
                    nodeData.bAllow = bAllow;
                }
                else
                {
                    if (obj != null)
                    {
                        HttpContext Context = HttpContext.Current;
                        string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                        Globals.GetSrcNameAndSrcAttr(obj, objUrl, Context, ref srcName, ref srcAttr);
                        mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                    }
                    else
                    {
                        string name = url.Substring(url.LastIndexOf("/") + 1);
                        string[] attr = { "name", name };
                        mulEval.SetTrimRequest(obj, url, attr, out idRequest);
                    }
                    nodeData.guid = guid;
                    nodeData.evalId = idRequest;
                    nodeData.time = modifyTime;
                }
            }
        }
    }
}
