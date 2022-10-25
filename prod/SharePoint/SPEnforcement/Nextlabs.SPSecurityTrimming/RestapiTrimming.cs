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
    public enum RestApiTrimType
    {
        SiteCollection,
        ListCollection,
        ListItemCollection,
        FileCollection,
        FolderCollection,
        RenderListData,
        GetChanges,
        WebAppTiles,
        GetSubwebs,
        HubGetListItems,
        HubGetTables,
        SearchQuery,
        Navigation,
        PagesFeed,
        UnKnown
    }

    public class RestApiTrimming
    {
        public string[] RestApiTrim = { "/webs", "/lists", "/items", "/files", "/folders", "/renderlistdataasstream",
                                "/web/getchanges", "/web/apptiles", "/web/getsubwebs", "/SP.APIHubConnector.GetListItems",
                                "/SP.APIHubConnector.GetTables", "/search/postquery", "/navigation/MenuState", "/sitepages/pages/feed" };

        public void DoTrimming(SPWeb web, SPList list, string remoteAddr, string url)
        {
            try
            {
                // Check RestApi trimming here, George.
                if (IfNeedTrimming(web, list, url))
                {
                    RestAPiEvaluation restApiEval = NewRestApiEval(web, list, url, remoteAddr);
                    HttpResponse restApiResponse = HttpContext.Current.Response;
                    ReplaceResponseFilter(restApiResponse, restApiEval);
                }
            }
            catch
            {
            }
        }

        public bool IfNeedTrimming(SPWeb web, SPList list, string url)
        {
            if (web != null && url != null)
            {
                bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
                RestApiTrimType type = GetTrimType(url);
                if ((bIgnoreTrimControl && type != RestApiTrimType.UnKnown) || type.Equals(RestApiTrimType.SearchQuery))
                {
                    return true;
                }
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                {
                    bool bSecrityTrimming = manager.CheckSecurityTrimming();
                    bool bListTrimming = manager.CheckListTrimming();
                    if (bSecrityTrimming)
                    {
                        if (type.Equals(RestApiTrimType.SiteCollection) || type.Equals(RestApiTrimType.GetChanges) || type.Equals(RestApiTrimType.WebAppTiles)
                            || type.Equals(RestApiTrimType.GetSubwebs) || type.Equals(RestApiTrimType.Navigation) || type.Equals(RestApiTrimType.PagesFeed))
                        {
                            return true;
                        }
                        else if (bListTrimming && (type.Equals(RestApiTrimType.ListCollection) || type.Equals(RestApiTrimType.HubGetTables)))
                        {
                            return true;
                        }
                        else if (list != null && (bListTrimming || manager.CheckListTrimming(list)) &&
                            (type.Equals(RestApiTrimType.HubGetListItems) || type.Equals(RestApiTrimType.ListItemCollection) || type.Equals(RestApiTrimType.FileCollection)
                            || type.Equals(RestApiTrimType.FolderCollection) || type.Equals(RestApiTrimType.RenderListData)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void ReplaceResponseFilter(HttpResponse response, RestAPiEvaluation restApiEval)
        {
            ResponseFilter filter = ResponseFilters.Current(response);
            filter.RestApiEval = restApiEval;
            filter.AddFilterType(FilterType.RestApiTrimmer);
        }

        public RestAPiEvaluation NewRestApiEval(SPWeb web, SPList list, string url, string remoteAddr)
        {
            RestApiTrimType type = GetTrimType(url);
            RestAPiEvaluation restApiEval = new RestAPiEvaluation(web, list, type, remoteAddr);
            return restApiEval;
        }

        public RestApiTrimType GetTrimType(string url)
        {
            RestApiTrimType type = RestApiTrimType.UnKnown;
            if (url.EndsWith(RestApiTrim[0], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[0] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.SiteCollection;
            }
            else if (url.EndsWith(RestApiTrim[1], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[1] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.ListCollection;
            }
            else if (url.EndsWith(RestApiTrim[2], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[2] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.ListItemCollection;
            }
            else if (url.EndsWith(RestApiTrim[3], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[3] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.FileCollection;
            }
            else if (url.EndsWith(RestApiTrim[4], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[4] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.FolderCollection;
            }
            else if (url.EndsWith(RestApiTrim[5], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[5] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.RenderListData;
            }
            else if (url.EndsWith(RestApiTrim[6], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[6] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.GetChanges;
            }
            else if (url.EndsWith(RestApiTrim[7], StringComparison.OrdinalIgnoreCase) || url.EndsWith(RestApiTrim[7] + "/", StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.WebAppTiles;
            }
            else if (-1 != url.IndexOf(RestApiTrim[8], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.GetSubwebs;
            }
            else if (-1 != url.IndexOf(RestApiTrim[9], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.HubGetListItems;
            }
            else if (-1 != url.IndexOf(RestApiTrim[10], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.HubGetTables;
            }
            else if (-1 != url.IndexOf(RestApiTrim[11], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.SearchQuery;
            }
            else if (-1 != url.IndexOf(RestApiTrim[12], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.Navigation;
            }
            else if (-1 != url.IndexOf(RestApiTrim[13], StringComparison.OrdinalIgnoreCase))
            {
                type = RestApiTrimType.PagesFeed;
            }
            return type;
        }
    }

    public class RestApiTrimmer
    {
        private HttpResponse m_response;
        private RestAPiEvaluation m_RestApiEval;
        public RestApiTrimmer(HttpResponse response, RestAPiEvaluation restApiEval)
        {
            m_response = response;
            m_RestApiEval = restApiEval;
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            if (-1 != m_response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
            {
                strFinal = XmlTrimming(strInput);
            }
            else if (-1 != m_response.ContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase))
            {
                if (m_RestApiEval.m_type == RestApiTrimType.RenderListData)
                {
                    strFinal = JsonRenderListTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.GetChanges)
                {
                    strFinal = JsonWebGetChangesTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.WebAppTiles)
                {
                    strFinal = JsonWebAppTilesTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.GetSubwebs)
                {
                    strFinal = JsonGetSubwebsTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.HubGetListItems)
                {
                    strFinal = JsonHubGetItemsTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.HubGetTables)
                {
                    strFinal = JsonHubGetTablesTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.SearchQuery)
                {
                    strFinal = JsonSearchQueryTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.Navigation)
                {
                    strFinal = JsonNavigationTrimming(strInput);
                }
                else if (m_RestApiEval.m_type == RestApiTrimType.PagesFeed)
                {
                    strFinal = JsonPagesFeedTrimming(strInput);
                }
                else
                {
                    strFinal = JsonTrimming(strInput);
                }
                if (!strFinal.Equals(strInput) && !TrimmerGlobal.CheckJsonFormat(strFinal) && !CheckJsonAgain(strInput, strFinal))
                {
                    strFinal = strInput;
                }
            }
            return strFinal;
        }

        public string XmlTrimming(string responseXml)
        {
            string strFinal = null;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = responseXml;
                XmlNode node = xmlDoc.DocumentElement;
                if (node.Name.Equals("feed"))
                {
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    foreach(XmlNode child in node.ChildNodes)
                    {
                        if (child.Name.Equals("entry"))
                        {
                            string id = FindXmlDataId(child);
                            if (!string.IsNullOrEmpty(id))
                            {
                                TrimNodeData nodeData = new TrimNodeData();
                                nodeData.trimId = id;
                                nodeData.node = child;
                                listNodeData.Add(nodeData);
                            }
                        }
                    }
                    m_RestApiEval.Run(listNodeData);
                    foreach (TrimNodeData nodeData in listNodeData)
                    {
                        if (!nodeData.bAllow && nodeData.node != null)
                        {
                            node.RemoveChild(nodeData.node);
                        }
                    }
                    strFinal = xmlDoc.InnerXml;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestApiTrimming XmlTrimming:", null, ex);
            }

            return strFinal;
        }

        private string FindXmlDataId(XmlNode node)
        {
            string id = null;
            try
            {
                XmlNode propNode = node["content"]["m:properties"];
                if (propNode != null)
                {
                    string idFlag = "";
                    switch (m_RestApiEval.m_type)
                    {
                        case RestApiTrimType.ListItemCollection:
                        case RestApiTrimType.ListCollection:
                            {
                                idFlag = "d:Id";
                            }
                            break;
                        case RestApiTrimType.SiteCollection:
                            {
                                idFlag = "d:Url";
                            }
                            break;
                        case RestApiTrimType.FileCollection:
                        case RestApiTrimType.FolderCollection:
                            {
                                idFlag = "d:ServerRelativeUrl";
                            }
                            break;
                    }
                    id = propNode[idFlag].InnerText;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FindXmlDataId:", null, ex);
            }
            return id;
        }

        public string JsonPagesFeedTrimming(string jsonText)
        {
            string strBegin = "\"Value\":";
            string key = "\"AbsoluteUrl\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonNavigationTrimming(string jsonText)
        {
            string strBegin = "\"Nodes\":";
            string key = "\"SimpleUrl\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonSearchQueryTrimming(string jsonText)
        {
            string strBegin = "\"Rows\":";
            string key = "\"Key\":\"Path\",\"Value\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonHubGetItemsTrimming(string jsonText)
        {
            string strBegin = "\"Value\":";
            string key = "\\\"ItemInternalId\\\":\\";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonHubGetTablesTrimming(string jsonText)
        {
            string strBegin = "\"Value\":";
            string key = "\\\"Name\\\":\\";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonGetSubwebsTrimming(string jsonText)
        {
            string strBegin = "\"results\":";
            string key = "\"ServerRelativeUrl\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonWebAppTilesTrimming(string jsonText)
        {
            string strBegin = "\"results\":";
            string key = "\"Target\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonWebGetChangesTrimming(string jsonText)
        {
            string strBegin = "\"value\":";
            string key = "\"ServerRelativeUrl\":";
            string finalJson = DataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        public string JsonRenderListTrimming(string jsonText)
        {
            string strBegin = "\"Row\" :";
            string key = "\"FileRef\":";
            string finalJson = LoopDataTrimming(jsonText, strBegin, key);
            return finalJson;
        }

        private string ReplaceListDataRow(string jsonText, int denyCount)
        {
            string finalJson = jsonText;
            string lastRowFlag = "\"LastRow\" :";
            int indBegin = finalJson.IndexOf(lastRowFlag, StringComparison.OrdinalIgnoreCase);
            if(-1 != indBegin)
            {
                int indEnd = finalJson.IndexOf(",", indBegin + lastRowFlag.Length, StringComparison.OrdinalIgnoreCase);
                if(-1 != indEnd)
                {
                    string strLastRow = finalJson.Substring(indBegin + lastRowFlag.Length, indEnd - indBegin - lastRowFlag.Length);
                    if(!string.IsNullOrEmpty(strLastRow))
                    {
                        strLastRow = strLastRow.Trim();
                        int nLastRow = 0;
                        bool bParse = int.TryParse(strLastRow, out nLastRow);
                        if (bParse)
                        {
                            int newLastRow = nLastRow - denyCount;
                            string strOldLastRow = finalJson.Substring(indBegin, indEnd - indBegin);
                            string strNewLastRow = lastRowFlag + " " + newLastRow.ToString();

                            finalJson = finalJson.Replace(strOldLastRow, strNewLastRow);
                            finalJson = finalJson.Replace("PageFirstRow=" + (nLastRow + 1).ToString(), "PageFirstRow=" + (newLastRow + 1).ToString());
                        }
                    }
                }
            }
            return finalJson;
        }

        public string LoopDataTrimming(string jsonText, string strBegin, string key)
        {
            string finalJson = jsonText;
            int ind = finalJson.IndexOf(strBegin, StringComparison.OrdinalIgnoreCase);
            while(true)
            {
                if (-1 != ind)
                {
                    string rightJson = finalJson.Substring(ind);
                    rightJson = DataTrimming(rightJson, strBegin, key);
                    finalJson = finalJson.Substring(0, ind) + rightJson;
                }
                else
                {
                    break;
                }
                ind = finalJson.IndexOf(strBegin, ind + strBegin.Length, StringComparison.OrdinalIgnoreCase);
            };
            return finalJson;
        }


        public string DataTrimming(string jsonText, string strBegin, string key)
        {
            string finalJson = jsonText;
            bool bDeny = false;
            try
            {
                int denyCount = 0;
                int indBegin = finalJson.IndexOf(strBegin, StringComparison.OrdinalIgnoreCase);
                if (indBegin != -1)
                {
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    string metaData = string.Empty;
                    metaData = finalJson.Substring(indBegin + strBegin.Length);
                    bool bRet = TrimmerGlobal.GetPairString("[", "]", metaData, ref metaData);
                    if (bRet)
                    {
                        JsonSplit(metaData, listNodeData, key);
                    }
                    if (listNodeData.Count > 0)
                    {
                        m_RestApiEval.Run(listNodeData);
                        List<string> dataList = new List<string>();
                        foreach (TrimNodeData node in listNodeData)
                        {
                            if (node.bAllow && !string.IsNullOrEmpty(node.nodeStr))
                            {
                                dataList.Add(node.nodeStr);
                            }
                            else
                            {
                                denyCount++;
                                bDeny = true;
                            }
                        }
                        if (bDeny)
                        {
                            string endData = string.Join(",", dataList.ToArray());
                            finalJson = finalJson.Replace(metaData, endData);
                            if (m_RestApiEval.m_type == RestApiTrimType.RenderListData)
                            {
                                finalJson = ReplaceListDataRow(finalJson, denyCount);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CsomTrimmer:", null, ex);
            }
            return finalJson;
        }

        private bool CheckJsonAgain(string jsonText, string finalJson)
        {
            // George: Fix bug 53152, If the orignal string is not json formart with sepical chars, check the finalJson with "}" and "]" again.
            string specialKey = "\"TaxonomyFieldValue:#Microsoft.SharePoint.Taxonomy\"";
            if (!TrimmerGlobal.CheckJsonFormat(jsonText) && (finalJson.EndsWith("}") || finalJson.EndsWith("]"))
                 && -1 != finalJson.IndexOf(specialKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private void JsonSplit(string metaData, List<TrimNodeData> listNodeData, string key)
        {
            string endData = metaData;
            List<string> datas = new List<string>();
            string strBegin = "\"Nodes\":";
            string strEmptyBegin = "\"Nodes\":[]";
            string navKey = "\"SimpleUrl\":";

            TrimmerGlobal.SplitMetaData("{", "}", metaData, datas);
            foreach (string data in datas)
            {
                if (m_RestApiEval.m_type == RestApiTrimType.Navigation && -1 != data.IndexOf(strBegin, StringComparison.OrdinalIgnoreCase)
                    && -1 == data.IndexOf(strEmptyBegin, StringComparison.OrdinalIgnoreCase))
                {
                    string finalData = DataTrimming(data, strBegin, navKey);
                    if (!string.IsNullOrEmpty(finalData))
                    {
                        TrimNodeData nodeData = new TrimNodeData();
                        nodeData.nodeStr = data;
                        listNodeData.Add(nodeData);
                    }
                }
                else
                {
                    string id = TrimmerGlobal.GetPairValue(data, key);
                    if (!string.IsNullOrEmpty(id))
                    {
                        TrimNodeData nodeData = new TrimNodeData();
                        nodeData.trimId = id;
                        nodeData.nodeStr = data;
                        listNodeData.Add(nodeData);
                    }
                }
            }
        }

        public string JsonTrimming(string jsonText)
        {
            string strFinal = jsonText;
            try
            {
                string beginText = "{\"d\":{\"results\":[";
                string[] splits = { ",{\"__metadata\":" };
                string joinText = "{\"__metadata\":";
                int indBegin = jsonText.IndexOf(beginText, StringComparison.OrdinalIgnoreCase);
                if (-1 != indBegin)
                {
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    int indEnd = jsonText.LastIndexOf("]");
                    string metadata = jsonText.Substring(indBegin + beginText.Length, indEnd - indBegin - beginText.Length);
                    string[] datas = metadata.Split(splits, StringSplitOptions.None);
                    for (int i = 0; i < datas.Length; i++)
                    {
                        string data = datas[i];
                        if (i != 0)
                        {
                            data = joinText + data;
                        }
                        string id = FindJsonDataId(data);
                        if (!string.IsNullOrEmpty(id))
                        {
                            TrimNodeData nodeData = new TrimNodeData();
                            nodeData.trimId = id;
                            nodeData.nodeStr = data;
                            listNodeData.Add(nodeData);
                        }
                    }
                    m_RestApiEval.Run(listNodeData);
                    List<string> finalData = new List<string>();
                    foreach (TrimNodeData nodeData in listNodeData)
                    {
                        if (nodeData.bAllow && !string.IsNullOrEmpty(nodeData.nodeStr))
                        {
                            finalData.Add(nodeData.nodeStr);
                        }
                        else
                        {
                        }
                    }
                    strFinal = jsonText.Substring(0, indBegin + beginText.Length) + string.Join(",", finalData.ToArray()) + jsonText.Substring(indEnd);
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestApiTrimming JsonTrimming:", null, ex);
            }

            return strFinal;
        }

        private string FindJsonDataId(string data)
        {
            string id = null;
            try
            {
                string idFlag = "";
                switch (m_RestApiEval.m_type)
                {
                    case RestApiTrimType.ListItemCollection:
                    case RestApiTrimType.ListCollection:
                        {
                            idFlag = "\"ID\":";
                        }
                        break;
                    case RestApiTrimType.SiteCollection:
                        {
                            idFlag = "\"URL\":";
                        }
                        break;
                    case RestApiTrimType.FileCollection:
                    case RestApiTrimType.FolderCollection:
                        {
                            idFlag = "\"ServerRelativeUrl\":";
                        }
                        break;
                }
                int indId = data.LastIndexOf(idFlag, StringComparison.OrdinalIgnoreCase);
                if(-1 != indId)
                {
                    string tail = data.Substring(indId + idFlag.Length);
                    id = tail.Substring(0, tail.IndexOf(","));
                }

                // Trim the '\"' in begin and end of id.
                char[] trimChar = { '\"' };
                id = id.Trim(trimChar);
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during FindJsonDataId:", null, ex);
            }
            return id;
        }
    }

    public class RestAPiEvaluation
    {
        private SPWeb m_web;
        private SPList m_list;
        public RestApiTrimType m_type;
        private string m_remoteAddr;
        private string m_userId;
        public RestAPiEvaluation(SPWeb web, SPList list, RestApiTrimType type, string remoteAddr)
        {
            m_web = web;
            m_list = list;
            m_type = type;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
        }

        public bool Run(List<TrimNodeData> listNodeData)
        {
            try
            {
                EvaluationMultiple mulEval = null;
                TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval);
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    SetEvalAttrBytrimId(nodeData, mulEval);
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
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestAPiEvaluation run:", null, ex);
            }
            return true;
        }

        public void SetEvalAttrBytrimId(TrimNodeData nodeData, EvaluationMultiple mulEval)
        {
            try
            {
                string trimId = nodeData.trimId;
                object obj = null;
                if (!string.IsNullOrEmpty(trimId))
                {
                    switch (m_type)
                    {
                        case RestApiTrimType.ListItemCollection:
                            {
                                if (m_list != null)
                                {
                                    // for item, trimId is "item id".
                                    int itemId = Int32.Parse(trimId);
                                    obj = m_list.GetItemById(itemId);
                                }
                            }
                            break;
                        case RestApiTrimType.ListCollection:
                            {
                                // for list, trimId is "GUID".
                                obj = m_web.Lists[new Guid(trimId)];
                            }
                            break;
                        case RestApiTrimType.SiteCollection:
                            {
                                // for web, trimId is "URL".
                                obj = Utilities.GetCachedSPContent(null, trimId, Utilities.SPUrlWeb);
                            }
                            break;
                        case RestApiTrimType.FileCollection:
                        case RestApiTrimType.FolderCollection:
                            {
                                // for File and folder, trimId is "ServerRelativeUrl".
                                obj = m_web.GetListItem(trimId);
                            }
                            break;
                        case RestApiTrimType.RenderListData:
                        case RestApiTrimType.GetChanges:
                            {
                                trimId = trimId.Replace("\\u002f", "/");
                                // trimId is "ServerRelativeUrl".
                                if (m_web != null)
                                {
                                    string fullUrl = m_web.Site.MakeFullUrl(trimId);
                                    obj = Utilities.GetCachedSPContent(m_web, fullUrl, Utilities.SPUrlListItem);
                                }
                            }
                            break;
                        case RestApiTrimType.WebAppTiles:
                            {
                                // trimId is "ServerRelativeUrl".
                                if (m_web != null)
                                {
                                    string fullUrl = m_web.Site.MakeFullUrl(trimId);
                                    obj = Utilities.GetCachedSPContent(m_web, fullUrl, Utilities.SPUrlList);
                                }
                            }
                            break;
                        case RestApiTrimType.GetSubwebs:
                            {
                                // trimId is "ServerRelativeUrl".
                                if (m_web != null)
                                {
                                    string fullUrl = m_web.Site.MakeFullUrl(trimId);
                                    obj = Utilities.GetCachedSPContent(null, fullUrl, Utilities.SPUrlWeb);
                                }
                            }
                            break;
                        case RestApiTrimType.HubGetListItems:
                            {
                                trimId = trimId.TrimEnd('\\');
                                int itemId = 0;
                                bool bParse = Int32.TryParse(trimId, out itemId);
                                if (bParse)
                                {
                                    if (m_list != null)
                                    {
                                        // for item, trimId is "item id".
                                        obj = m_list.GetItemById(itemId);
                                    }
                                }
                            }
                            break;
                        case RestApiTrimType.HubGetTables:
                            {
                                trimId = trimId.TrimEnd('\\');
                                if (m_web != null)
                                {
                                    // for list, trimId is "guid".
                                    obj = m_web.Lists[new Guid(trimId)];
                                }
                            }
                            break;
                        case RestApiTrimType.SearchQuery:
                        case RestApiTrimType.Navigation:
                            {
                                if (m_type == RestApiTrimType.Navigation && m_web != null)
                                {
                                    trimId = m_web.Site.MakeFullUrl(trimId);
                                }
                                using (SPHttpUrlParser parser = new SPHttpUrlParser(trimId))
                                {
                                    parser.Parse();
                                    obj = parser.ParsedObject;
                                }
                            }
                            break;
                        case RestApiTrimType.PagesFeed:
                            {
                                // trimId is "AbsoluteUrl".
                                if (m_web != null)
                                {
                                    obj = Utilities.GetCachedSPContent(m_web, trimId, Utilities.SPUrlListItem);
                                }
                            }
                            break;
                    }
                }

                CheckEvalCacheAndSetTrim(obj, nodeData, mulEval);
            }
            catch
            {
            }
        }

        public void CheckEvalCacheAndSetTrim(object obj, TrimNodeData nodeData, EvaluationMultiple mulEval)
        {
            if (obj != null)
            {
                string guid = null;
                bool bExisted = false;
                bool bAllow = true;
                System.DateTime modifyTime = new DateTime(1, 1, 1);
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
                if (guid != null)
                {
                    bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(m_userId, m_remoteAddr, guid, ref bAllow, modifyTime);
                    if (bExisted)
                    {
                        nodeData.bAllow = bAllow;
                    }
                    else
                    {
                        string srcName = null;
                        string[] srcAttr = null;
                        int idRequest = -1;
                        HttpContext Context = HttpContext.Current;
                        string url = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                        Globals.GetSrcNameAndSrcAttr(obj, url, Context, ref srcName, ref srcAttr);
                        mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                        nodeData.guid = guid;
                        nodeData.evalId = idRequest;
                        nodeData.time = modifyTime;
                    }
                }
            }
        }
    }

    public class CsomTrimmer
    {
        private SPWeb m_web;
        private bool m_bIgnoreTrimControl;
        public CsomTrimmer(SPWeb web)
        {
            m_bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
            m_web = web;
        }

        public string Run(string strInput)
        {
            string finalJson = strInput;
            if (m_web != null && !m_bIgnoreTrimControl)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                {
                    if (!manager.CheckSecurityTrimming())
                    {
                        return finalJson; // Don't do anything without trimming enable.
                    }
                }
            }

            bool bChanged = false;
            try
            {
                const string beginStr = "\"_Child_Items_\":";
                int indBegin = -1;
                indBegin = finalJson.IndexOf(beginStr, StringComparison.OrdinalIgnoreCase);
                while (indBegin != -1)
                {
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    string metaData = string.Empty;
                    if (finalJson.Length > indBegin + beginStr.Length)
                    {
                        metaData = finalJson.Substring(indBegin + beginStr.Length);
                        bool bRet = TrimmerGlobal.GetPairString("[", "]", metaData, ref metaData);
                        if (bRet)
                        {
                            JsonSplit(metaData, listNodeData, SoapType.CSOMResponse);
                        }
                        if (listNodeData.Count > 0)
                        {
                            CSOMResponseEvaluation csomEval = new CSOMResponseEvaluation(m_web, HttpContext.Current.Request.UserHostAddress, HttpContext.Current);
                            csomEval.Run(listNodeData);
                            List<string> dataList = new List<string>();
                            bool bDeny = false;
                            foreach (TrimNodeData node in listNodeData)
                            {
                                if (node.bAllow && !string.IsNullOrEmpty(node.nodeStr))
                                {
                                    dataList.Add(node.nodeStr);
                                }
                                else
                                {
                                    bDeny = true;
                                }
                            }
                            if (bDeny)
                            {
                                if(!bChanged)
                                {
                                    bChanged = true;
                                }
                                string endData = string.Join(",", dataList.ToArray());
                                finalJson = finalJson.Replace(metaData, endData);
                            }
                        }
                    }
                    indBegin = finalJson.IndexOf(beginStr, indBegin + beginStr.Length, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CsomTrimmer:", null, ex);
            }
            if (bChanged && !TrimmerGlobal.CheckJsonFormat(finalJson))
            {
                finalJson = strInput;
            }
            return finalJson;
        }
        private void JsonSplit(string metaData, List<TrimNodeData> listNodeData, SoapType soapType)
        {
            string key = "";  // means "id", "guid" or "url".
            if (soapType.Equals(SoapType.CSOMResponse))
            {
                key = "\"_ObjectIdentity_\"";
            }
            else
            {
                return;
            }
            string endData = metaData;
            List<string> datas = new List<string>();

            TrimmerGlobal.SplitMetaData("{", "}", metaData, datas);
            foreach (string data in datas)
            {
                string id = TrimmerGlobal.GetPairValue(data, key);
                if (!string.IsNullOrEmpty(id))
                {
                    TrimNodeData nodeData = new TrimNodeData();
                    nodeData.trimId = id;
                    nodeData.nodeStr = data;
                    nodeData.soapType = soapType;
                    listNodeData.Add(nodeData);
                }
            }
        }
    }

    public class CSOMResponseEvaluation
    {
        private SPWeb m_web;
        private string m_remoteAddr;
        private string m_userId;
        private HttpContext m_context = null;
        public CSOMResponseEvaluation(SPWeb web, string remoteAddr, HttpContext context)
        {
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
            m_context = context;
        }

        public bool Run(List<TrimNodeData> listNodeData)
        {
            EvaluationMultiple mulEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval);
            bool bDefault = Globals.GetPolicyDefaultBehavior();
            bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
            if (mulEval != null && m_web != null)
            {
                using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                {
                    bool bSecrityTrimming = bIgnoreTrimControl || manager.CheckSecurityTrimming();
                    bool bListTrimming = bIgnoreTrimControl || manager.CheckListTrimming();
                    foreach (TrimNodeData nodeData in listNodeData)
                    {
                        bool bNeedTrim = false;
                        if (bSecrityTrimming)
                        {
                            try
                            {
                                int idRequest = -1;
                                object evalObj = null;
                                try
                                {
                                    evalObj = Globals.GetObjectFromIdentity(System.Text.RegularExpressions.Regex.Unescape(nodeData.trimId));
                                }
                                catch
                                {
                                }

                                if (evalObj != null)
                                {
                                    if (evalObj is SPFile)
                                    {
                                        SPFile file = evalObj as SPFile;
                                        if (file != null && file.Item != null)
                                        {
                                            if (bListTrimming || manager.CheckListTrimming(file.Item.ParentList))
                                            {
                                                bNeedTrim = true;
                                                evalObj = file.Item;
                                            }
                                        }
                                    }
                                    else if (evalObj is SPFolder)
                                    {
                                        SPFolder folder = evalObj as SPFolder;
                                        if (folder != null && folder.Item != null)
                                        {
                                            if (bListTrimming || manager.CheckListTrimming(folder.Item.ParentList))
                                            {
                                                bNeedTrim = true;
                                                evalObj = folder.Item;
                                            }
                                        }
                                        else if (folder != null && folder.ParentListId != null)
                                        {
                                            if (bListTrimming)
                                            {
                                                SPList evalList = m_web.Lists.GetList(folder.ParentListId, true);
                                                bNeedTrim = true;
                                                evalObj = evalList;
                                            }
                                        }
                                        else
                                        {
                                            evalObj = folder.ParentWeb;
                                            bNeedTrim = true;
                                        }
                                    }
                                    else if (evalObj is SPList)
                                    {
                                        if (bListTrimming)
                                        {
                                            bNeedTrim = true;
                                        }
                                    }
                                    else
                                    {
                                        bNeedTrim = true;
                                    }

                                    if (bNeedTrim && evalObj != null)
                                    {
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
                                            string evalUrl = Globals.ConstructObjectUrl(evalObj);
                                            string srcName = string.Empty;
                                            string[] srcAttr = new string[0];
                                            Globals.GetSrcNameAndSrcAttr(evalObj, evalUrl, m_context, ref srcName, ref srcAttr);
                                            mulEval.SetTrimRequest(evalObj, srcName, srcAttr, out idRequest);
                                            nodeData.evalId = idRequest;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                NLLogger.OutputLog(LogLevel.Debug, "Exception during CSOMResponseEvaluation Run:", null, ex);
                            }
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
                    }
                    mulEval.ClearRequest();
                    mulEval = null;
                }
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
};
