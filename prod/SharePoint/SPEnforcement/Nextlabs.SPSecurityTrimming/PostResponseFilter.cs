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
using System.Text.RegularExpressions;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class PostNode
    {
        public string itemId;
        public string nodeData;
        public int evalId;
        public bool bAllow;
        public string guid;
        public System.DateTime time;
        public PostNode()
        {
            itemId = null;
            nodeData = null;
            evalId = -1;
            bAllow = true;
            guid = null;
            time = new DateTime();
        }
    }

    public class FieldIdTrimmer
    {
        private string m_fieldKey;
        public FieldIdTrimmer(string strKey)
        {
            m_fieldKey = strKey;
        }
        // Delete our filters for "ID" in response.
        public string Run(string strInput)
        {
            string strFinal = strInput;
            if (-1 == strFinal.IndexOf("HierarchyHasIndention", StringComparison.OrdinalIgnoreCase))
            {
                // Don't match the case.
                return strFinal;
            }
            try
            {
                string FilterOp = "=In&";
                string pattern = string.Format(@"{0}=ID.+?{1}", m_fieldKey, FilterOp); // get the string from "FilterFields1=ID" to "FilterOp1=In&";
                Match match = Regex.Match(strInput, pattern);
                string idValues = match.Value; // my filter values for "ID".
                string linkId = ",\"FilterFields\" : \";ID;\"";
                pattern = ",\"FilterFields\" : \".+?\"";
                match = Regex.Match(strInput, pattern);
                string filterLink = match.Value;

                string endLink = "";
                if (!filterLink.Equals(linkId, StringComparison.OrdinalIgnoreCase))
                {
                    endLink = filterLink.Replace(";ID", "");
                }

                // delete our filter values and "ID" link;
                strFinal = strFinal.Replace(idValues, "");
                strFinal = strFinal.Replace(filterLink, endLink);
                strFinal = strFinal.Replace("&&&View", "&&View");

            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ChangeTrimFilter:", null, ex);
            }
            return strFinal;
        }
    }

    public class PostTrimmer
    {
        private SPWeb m_web;
        public PostTrimmer(SPWeb web)
        {
            m_web = web;
        }
        private HttpResponse m_response = null;
        public HttpResponse Response
        {
            get { return m_response; }
            set { m_response = value; }
        }

        private Stream m_initstream = null;
        public Stream InitFilter
        {
            get { return m_initstream; }
            set { m_initstream = value; }
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            if (-1 == strFinal.IndexOf("</html>", StringComparison.OrdinalIgnoreCase))
            {
                // Don't match the case.
                return strFinal;
            }
            string key = "\"FileRef\"";
            string strBegin = "ListData = { \"Row\" :";
            int indBegin = strFinal.IndexOf(strBegin, StringComparison.OrdinalIgnoreCase);
            while (-1 != indBegin)
            {
                try
                {
                    string metaData = strFinal.Substring(indBegin + strBegin.Length);
                    List<TrimNodeData> listNodeData = new List<TrimNodeData>();
                    bool bRet = TrimmerGlobal.GetPairString("[", "]", metaData, ref metaData);
                    if (bRet && !string.IsNullOrEmpty(metaData))
                    {
                        List<string> dataList = new List<string>();
                        TrimmerGlobal.SplitMetaData("{", "}", metaData, dataList);
                        foreach (string data in dataList)
                        {
                            string id = TrimmerGlobal.GetPairValue(data, key).Replace("\\u002f", "/");
                            TrimNodeData nodeData = new TrimNodeData();
                            nodeData.trimId = id;
                            nodeData.nodeStr = data;
                            listNodeData.Add(nodeData);
                        }
                        if (listNodeData.Count > 0)
                        {
                            DoItemsEval(listNodeData);
                            List<string> allowDataList = new List<string>();
                            foreach (TrimNodeData node in listNodeData)
                            {
                                if (node.bAllow && !string.IsNullOrEmpty(node.nodeStr))
                                {
                                    allowDataList.Add(node.nodeStr);
                                }
                            }
                            string endData = string.Join(",", allowDataList.ToArray());
                            strFinal = strFinal.Replace(metaData, endData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during PostResponseFilter:", null, ex);
                    break;
                }
                indBegin = strFinal.IndexOf(strBegin, indBegin + strBegin.Length, StringComparison.OrdinalIgnoreCase);
            }

            return strFinal;
        }

        public bool DoItemsEval(List<TrimNodeData> nodes)
        {
            int idRequest = -1;
            HttpContext Context = HttpContext.Current;
            string remoteAddr = Context.Request.UserHostAddress;
            string userId = m_web.CurrentUser.LoginName;
            System.DateTime modifyTime = new DateTime(1, 1, 1);
            EvaluationMultiple multEval = null;
            TrimmingEvaluationMultiple.NewEvalMult(m_web, ref multEval);
            SPList list = null;
            foreach (TrimNodeData node in nodes)
            {
                try
                {
                    SPListItem item = (SPListItem)Utilities.GetCachedSPContent(m_web, node.trimId, Utilities.SPUrlListItem);
                    if (item != null)
                    {
                        if (list == null)
                        {
                            bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
                            list = item.ParentList;
                            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                            {
                                if (!bIgnoreTrimControl && list != null && !manager.CheckListTrimming() && !manager.CheckListTrimming(list))
                                {
                                    return false;
                                }
                            }
                        }
                        bool bAllow = true;
                        string guid = item.ParentList.ID.ToString() + item.ID.ToString();
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddr, guid, ref bAllow, modifyTime);
                        if (bExisted)
                        {
                            node.bAllow = bAllow;
                        }
                        else
                        {
                            string itemUrl = m_web.Url + "/" + item.Url;
                            string srcName = null;
                            string[] srcAttr = null;
                            Globals.GetSrcNameAndSrcAttr(item, itemUrl, Context, ref srcName, ref srcAttr);
                            multEval.SetTrimRequest(item, srcName, srcAttr, out idRequest);
                            node.guid = guid;
                            node.evalId = idRequest;
                            node.time = modifyTime;
                        }
                    }
                }
                catch
                {
                }
            }
            multEval.run();
            foreach (TrimNodeData node in nodes)
            {
                int evalId = node.evalId;
                if (evalId != -1)
                {
                    bool bAllow = multEval.GetTrimEvalResult(evalId);
                    node.bAllow = bAllow;
                    DateTime evalTime = DateTime.Now;
                    TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddr, node.guid, bAllow, evalTime, node.time);
                }
            }
            multEval.ClearRequest();
            return true;
        }
    }

    public class EditorTrimmer
    {
        private HttpRequest m_request;
        private HttpResponse m_response;
        private SPWeb m_web;
        private string m_remoteAddr;
        public EditorTrimmer(HttpRequest request, HttpResponse response, SPWeb web)
        {
            m_request = request;
            m_response = response;
            m_web = web;
            m_remoteAddr = request.UserHostAddress;
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            try
            {
                if (-1 != m_response.ContentType.IndexOf("html", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = HandleHtml(strInput);
                }
                else if (-1 != m_response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = HandleXml(strInput);
                }
            }
            catch
            {
            }

            return strFinal;
        }

        private string HandleHtml(string responseHtml)
        {
            string finalHtml = responseHtml;
            try
            {
                string[] listRegexArr = { "<TR\\s+(.*?)(\\s*)fileattribute=(.|\n)*?</tr>" };
                List<string> regexList = new List<string>();
                regexList.Add(listRegexArr[0]);
                if (regexList.Count > 0)
                {
                    foreach (string regex in regexList)
                    {
                        Regex re = new Regex(regex);
                        MatchCollection matches = re.Matches(responseHtml);
                        string strNode = null;
                        System.Collections.IEnumerator enu = matches.GetEnumerator();
                        while (enu.MoveNext() && enu.Current != null)
                        {
                            Match match = (Match)(enu.Current);
                            strNode = match.Value;
                            if (!string.IsNullOrEmpty(strNode))
                            {
                                finalHtml = finalHtml.Replace(strNode, "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during HtmlTrimming:", null, ex);
            }
            return finalHtml;
        }


        private string HandleXml(string responseXml)
        {
            string finalXml = responseXml;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = responseXml;
            List<TrimNodeData> listNodeData = new List<TrimNodeData>();
            XmlNode node = xmlDoc.DocumentElement;
            bool bResult = false;
            string tagName = "";
            string urlXmlAttrName = "";
            string type = "";

            if (-1 != responseXml.IndexOf("GetListItemChangesSinceTokenResult", StringComparison.OrdinalIgnoreCase))
            {
                tagName = "rs:data";
                urlXmlAttrName = "ows_FileRef";
                type = NextLabs.Common.Utilities.SPUrlListItem;
            }
            bResult = HandleXmlNode(xmlDoc, tagName, urlXmlAttrName, type, ref listNodeData);
            if (listNodeData.Count != 0 && bResult)
            {
                SPEvaluation evaluator = new SPEvaluation(m_web, m_remoteAddr);
                evaluator.Run(listNodeData);
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    if (!nodeData.bAllow && nodeData.node != null)
                    {
                        nodeData.node.ParentNode.RemoveChild(nodeData.node);
                    }
                }
            }
            finalXml = xmlDoc.InnerXml;
            return finalXml;
        }

        private bool HandleXmlNode(XmlDocument XmlDoc, string tagName, string UrlXmlAttrName, string type,
           ref List<TrimNodeData> listNodeData)
        {
            bool bResult = false;
            XmlNodeList listNodes = null;
            XmlNode parentNode = XmlDoc.GetElementsByTagName(tagName).Item(0);
            if (parentNode != null)
            {
                listNodes = parentNode.ChildNodes;
                foreach (XmlNode node in listNodes)
                {
                    object obj = null;
                    XmlAttribute urlAttr = node.Attributes[UrlXmlAttrName];
                    if (urlAttr != null && !string.IsNullOrEmpty(urlAttr.Value))
                    {
                        //obj = NextLabs.Common.Utilities.GetCachedSPContent(m_web, urlAttr.Value, type);
                        int pos = urlAttr.Value.IndexOf("#");
                        if (-1 != pos)
                        {
                            string itemUrl = urlAttr.Value.Substring(pos + 1);
                            obj = NextLabs.Common.Utilities.GetCachedSPContent(m_web, itemUrl, type);
                        }
                    }

                    if (obj != null)
                    {
                        TrimNodeData nodeData = new TrimNodeData();
                        nodeData.node = node;
                        nodeData.obj = obj;
                        listNodeData.Add(nodeData);
                    }
                }
                bResult = true;
            }
            return bResult;
        }
    }
    public class SPEvaluation
    {
        private SPWeb m_web;
        private string m_remoteAddr;
        private string m_userId;
        public SPEvaluation(SPWeb web, string remoteAddr)
        {
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
        }

        public bool Run(List<TrimNodeData> listNodeData)
        {
            try
            {
                EvaluationMultiple mulEval = null;
                string userName = m_web.CurrentUser.LoginName;
                userName = NextLabs.Common.Utilities.ClaimUserConvertion(userName);
                string userSid = m_web.CurrentUser.Sid;
                if (String.IsNullOrEmpty(userSid))
                {
                    userSid = UserSid.GetUserSid(m_web, m_web.CurrentUser.LoginName);
                    if (String.IsNullOrEmpty(userSid))
                    {
                        userSid = userName;
                    }
                }
                TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval, CETYPE.CEAction.Read, userName, userSid);
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    CheckEvalCacheAndSetTrim(mulEval, nodeData);
                }
                mulEval.run();
                foreach (TrimNodeData nodeData in listNodeData)
                {
                    int evalId = nodeData.evalId;
                    if (evalId != -1)
                    {
                        bool bAllow = mulEval.GetTrimEvalResult(evalId);
                        nodeData.bAllow = bAllow;
                    }
                }
                mulEval.ClearRequest();
                mulEval = null;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPEvaluation run:", null, ex);
            }
            return true;
        }

        public void CheckEvalCacheAndSetTrim(EvaluationMultiple mulEval, TrimNodeData nodeData)
        {
            object obj = nodeData.obj;
            if (obj != null)
            {
                string guid = null;
                if (obj is SPListItem)
                {
                    SPListItem evalItem = obj as SPListItem;
                    guid = evalItem.ParentList.ID.ToString() + evalItem.ID.ToString();
                }

                if (guid != null)
                {
                    string srcName = null;
                    string[] srcAttr = null;
                    int idRequest = -1;
                    HttpContext Context = HttpContext.Current;
                    SPWeb web = SPControl.GetContextWeb(Context);
                    string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                    Globals.GetSrcNameAndSrcAttr(web, obj, objUrl, m_remoteAddr, ref srcName, ref srcAttr, CETYPE.CEAction.Read);
                    mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                    nodeData.guid = guid;
                    nodeData.evalId = idRequest;
                }
            }
        }
    }
}
