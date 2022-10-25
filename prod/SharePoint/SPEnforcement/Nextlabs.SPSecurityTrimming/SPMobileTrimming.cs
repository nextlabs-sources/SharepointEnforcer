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
    public class MobileNodeData
    {
        public object obj;
        public string strNode;
        public XmlNode xmlNode;
        public bool bAllow;
        public int evalId;
        public string guid;
        public System.DateTime modifyTime;
        public MobileNodeData()
        {
            obj = null;
            bAllow = true;
            strNode = null;
            evalId = -1;
            guid = null;
            modifyTime = new DateTime(1, 1, 1);
        }
    }

    public enum SPMobileType
    {
        SPWebs,
        SPLists,
        SPItems,
        Unknown,    //if SPMobileType is unknown, it's mean can not judge the type, maybe is list, site, item..
        NotCheck    //
    }

    public class SPMobileTrimming
    {
        public void DoTrimming(HttpRequest Request, HttpContext context)
        {
            try
            {
                string url = Globals.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
                if (-1 != url.IndexOf("mobile/mbllists.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/mbllistsa.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/viewa.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/view.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/mblwikia.aspx", StringComparison.OrdinalIgnoreCase)  // SP2013 Home
                    || -1 != url.IndexOf("mobile/mblwiki.aspx", StringComparison.OrdinalIgnoreCase)   // SP2010 Home
                    || -1 != url.IndexOf("_vti_bin/lists.asmx", StringComparison.OrdinalIgnoreCase)   //ios offic
                    || -1 != url.IndexOf("_vti_bin/webs.asmx", StringComparison.OrdinalIgnoreCase)    //ios offic
                    || -1 != url.IndexOf("mobile/dispforma.aspx", StringComparison.OrdinalIgnoreCase) //enter an item from detail view
                    || -1 != url.IndexOf("mobile/newforma.aspx", StringComparison.OrdinalIgnoreCase)  // new item page
                    || -1 != url.IndexOf("mobile/mblwpa.aspx", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    SPWeb web = SPControl.GetContextWeb(context);
                    bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(Request);
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(web.Site))
                    {
                        if (bIgnoreTrimControl || manager.CheckSecurityTrimming())
                        {
                            ResponseFilter filter = ResponseFilters.Current(context.Response);
                            filter.AddFilterType(FilterType.MobileTrimmer);
                            filter.Request = context.Request;
                            filter.Web = web;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPMobileEvaluation DoTrimming:", null, ex);
            }
        }
    }

    public class MobileTrimmer
    {
        private HttpRequest m_request;
        private HttpResponse m_response;
        private SPWeb m_web;
        private string m_remoteAddr;
        public MobileTrimmer(HttpRequest request, HttpResponse response, SPWeb web)
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
                    strFinal = HtmlTrimming(strInput);
                }
                else if (-1 != m_response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
                {
                    strFinal = XmlTrimming(strInput);
                }
            }
            catch
            {
            }
            return strFinal;
        }

        private bool HandleXmlNode(XmlDocument XmlDoc, string tagName, string UrlXmlAttrName, string type,
            ref List<MobileNodeData> listNodeData)
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
                        obj = NextLabs.Common.Utilities.GetCachedSPContent(m_web, urlAttr.Value, type);
                    }
                    if (obj != null)
                    {
                        MobileNodeData nodeData = new MobileNodeData();
                        nodeData.xmlNode = node;
                        nodeData.obj = obj;
                        listNodeData.Add(nodeData);
                    }
                }
                bResult = true;
            }
            return bResult;
        }

        private string XmlTrimming(string responseXml)
        {
            string finalXml = responseXml;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = responseXml;
            List<MobileNodeData> listNodeData = new List<MobileNodeData>();
            XmlNode node = xmlDoc.DocumentElement;
            bool bResult = false;
            string tagName = "";
            string urlXmlAttrName = "";
            string type = "";
            if (-1 != responseXml.IndexOf("GetListCollectionResult", StringComparison.OrdinalIgnoreCase))
            {
                tagName = "Lists";
                urlXmlAttrName = "DefaultViewUrl";
                type = NextLabs.Common.Utilities.SPUrlList;
            }
            else if (-1 != responseXml.IndexOf("GetWebCollectionResult", StringComparison.OrdinalIgnoreCase))
            {
                tagName = "Webs";
                urlXmlAttrName = "Url";
                type = NextLabs.Common.Utilities.SPUrlWeb;
            }
            else if (-1 != responseXml.IndexOf("GetListItemsResult", StringComparison.OrdinalIgnoreCase))
            {
                tagName = "rs:data";
                urlXmlAttrName = "ows_EncodedAbsUrl";
                type = NextLabs.Common.Utilities.SPUrlListItem;
            }
            // Added for Android office trimming
            else if (-1 != responseXml.IndexOf("GetListItemChangesSinceTokenResult", StringComparison.OrdinalIgnoreCase))
            {
                tagName = "rs:data";
                urlXmlAttrName = "ows_ServerUrl";
                type = NextLabs.Common.Utilities.SPUrlListItem;
            }
            bResult = HandleXmlNode(xmlDoc, tagName, urlXmlAttrName, type, ref listNodeData);
            if (listNodeData.Count != 0 && bResult)
            {
                SPMobileEvaluation evaluator = new SPMobileEvaluation(m_web, m_remoteAddr);
                evaluator.Run(listNodeData);
                foreach (MobileNodeData nodeData in listNodeData)
                {
                    if (!nodeData.bAllow && nodeData.xmlNode != null)
                    {
                        nodeData.xmlNode.ParentNode.RemoveChild(nodeData.xmlNode);
                    }
                }
            }
            finalXml = xmlDoc.InnerXml;
            return finalXml;
        }

        private string HtmlTrimming(string responseHtml)
        {
            string strFinal = responseHtml;
            try
            {
                string url = Globals.HttpModule_ReBuildURL(m_request.Url.AbsoluteUri, m_request.FilePath, m_request.Path);
                List<MobileNodeData> listNodeData = new List<MobileNodeData>();
#if SP2013 || SP2016 || SP2019
                string[] webRegexArr = {"<div.*(?=mb-lists-list-panel)(.|\n)*?<table.*(?=mb-list-item)(.|\n)*?</table>(.|\n)*?</div>"  ,
                            @"<a href(.|\n)*?(?=menu-item-text)(.|\n)*?</a>",
                            "<a\\s+([^>]*?)(\\s*)class=\"mb-wp-more-link\"([^>]*?)>[^<>]*?</a>",
                            "<a\\s+([^>]*?)(\\s*)class=\"mb-wp-title-link\"([^>]*?)>[^<>]*?</a>"

                                       };
                string[] listRegexArr = { @"<table.*(?=mb-list-item)(.|\n)*?</table>",
                            @"<a href(.|\n)*?(?=menu-item-text)(.|\n)*?</a>",
                            @"<div.*(?=mb-list-detail-item)(.|\n)*?View Properties(.|\n)*?</table>(.|\n)*?</div>(.|\n)*?</div>"};

#elif SP2010
                string[] webRegexArr = { "<a(.|\n)*?</a><br>", "<img(.|\n)*?<br>" };
                string[] listRegexArr = { "<div(.|\n)*?<img(.|\n)*?</a>",
                       "<div.*(?=Center)(.|\n)*?<div(.|\n)*?</div>"};
#endif
                List<string> regexList = new List<string>();
                SPList list = null;
                if (-1 != url.IndexOf("ViewMode=Detail", StringComparison.OrdinalIgnoreCase))
                {
                    string listId = m_request.QueryString["List"];
                    list = m_web.Lists[new Guid(listId)];
                    if (list != null)
                    {
                        regexList.Add(listRegexArr[1]);
                        regexList.Add(listRegexArr[2]);
                    }
                }
                else if (-1 != url.IndexOf("mobile/viewa.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/view.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    string listId = m_request.QueryString["List"];
                    list = m_web.Lists[new Guid(listId)];
                    if (list != null)
                    {
                        regexList.Add(listRegexArr[0]);
                        regexList.Add(listRegexArr[1]);
                    }
                }
                else if (-1 != url.IndexOf("mobile/mbllists.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/mbllistsa.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/dispforma.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/newforma.aspx", StringComparison.OrdinalIgnoreCase)
                    || -1 != url.IndexOf("mobile/mblwpa.aspx", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    regexList.Add(webRegexArr[0]);
                    regexList.Add(webRegexArr[1]);
                }
                else if( -1 != url.IndexOf("mobile/mblwikia.aspx", StringComparison.OrdinalIgnoreCase)
                    ||- 1 != url.IndexOf("mobile/mblwiki.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    regexList.Add(listRegexArr[0]);
                    regexList.Add(webRegexArr[2]);
                    regexList.Add(webRegexArr[3]);
                    regexList.Add(webRegexArr[1]);
                }

                if (regexList.Count > 0)
                {
                    foreach (string regex in regexList)
                    {
                        Regex re = new Regex(regex);
                        MatchCollection matches = re.Matches(responseHtml);
                        string strNode = null;
                        SPMobileType type = SPMobileType.NotCheck;
                        string relUrl = null; ;
                        System.Collections.IEnumerator enu = matches.GetEnumerator();
                        while (enu.MoveNext() && enu.Current != null)
                        {
                            Match match = (Match)(enu.Current);
                            strNode = match.Value;
                            if (!string.IsNullOrEmpty(strNode))
                            {
                                bool bDecode = DecodeStrNode(strNode, list, ref relUrl,ref type);
                                object obj = null;
                                if (bDecode && !string.IsNullOrEmpty(relUrl))
                                {
                                    // George, decode the url.
                                    relUrl = HttpUtility.UrlDecode(relUrl);

                                    string fullUrl = m_web.Site.MakeFullUrl(relUrl);
                                    if (type.Equals(SPMobileType.SPItems))
                                    {
                                        int id = -1;
                                        bool bResult = GetIdFromStrNode(strNode, ref id);
                                        if (bResult)
                                        {
                                            obj = list.GetItemById(id);
                                        }
                                        else
                                        {
                                            obj = NextLabs.Common.Utilities.GetCachedSPContent(m_web,
                                                fullUrl, NextLabs.Common.Utilities.SPUrlListItem); // Get library item.
                                        }
                                    }
                                    else if (type.Equals(SPMobileType.SPLists))
                                    {
                                        obj = NextLabs.Common.Utilities.GetCachedSPContent(m_web,
                                            fullUrl, NextLabs.Common.Utilities.SPUrlList); // Get list or library.
                                    }
                                    else if (type.Equals(SPMobileType.SPWebs))
                                    {
                                        obj = NextLabs.Common.Utilities.GetCachedSPContent(null,
                                            fullUrl, NextLabs.Common.Utilities.SPUrlWeb); // Get sub web.
                                    }
                                    else if (type.Equals(SPMobileType.Unknown))
                                    {
                                        using (SPHttpUrlParser parser = new SPHttpUrlParser(fullUrl))
                                        {
                                            parser.Parse();
                                            obj = parser.ParsedObject;
                                        }
                                    }

                                    if (obj != null)
                                    {
                                        SPList parentList = obj as SPList;
                                        if (parentList != null)
                                        {

                                            if (-1 != fullUrl.IndexOf("id", StringComparison.OrdinalIgnoreCase))
                                            {
                                                Uri uri = new Uri(fullUrl);
                                                string[] queryStrings = uri.Query.Split(new string[] { "?", "&amp;" }, StringSplitOptions.RemoveEmptyEntries);
                                                foreach (string query in queryStrings)
                                                {
                                                    if ((query.Split('=')[0]).Equals("id", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        int listItemId = Convert.ToInt32(query.Split('=')[1]);
                                                        try
                                                        {
                                                            obj = parentList.GetItemById(listItemId);
                                                        }
                                                        catch { }
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        MobileNodeData nodeData = new MobileNodeData();
                                        nodeData.strNode = strNode;
                                        nodeData.obj = obj;
                                        listNodeData.Add(nodeData);
                                    }
                                }
                            }
                        }
                    }
                }

                if (listNodeData.Count != 0)
                {
                    SPMobileEvaluation evaluator = new SPMobileEvaluation(m_web, m_remoteAddr);
                    evaluator.Run(listNodeData);
                    foreach (MobileNodeData nodeData in listNodeData)
                    {
                        if (!nodeData.bAllow && !string.IsNullOrEmpty(nodeData.strNode))
                        {
                            strFinal = strFinal.Replace(nodeData.strNode, "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPMobileEvaluation HtmlTrimming:", null, ex);
            }
            return strFinal;
        }

        private bool  GetIdFromStrNode(string strNode, ref int id)
        {
            bool bResult = false;
			string idflag = "id=\"";
            int pos = strNode.IndexOf(idflag,StringComparison.OrdinalIgnoreCase);
            if (-1 == pos)
            {
                idflag = "&ID=";
                pos = strNode.IndexOf(idflag, StringComparison.OrdinalIgnoreCase);
                if (-1 == pos)
                {
                    idflag = "&amp;ID=";
                    pos = strNode.IndexOf(idflag, StringComparison.OrdinalIgnoreCase);
                }
            }
            if (-1 != pos)
            {
                string tail = strNode.Substring(pos + idflag.Length);
                pos = tail.IndexOf("\"");
                int result;
                if (-1 != pos && int.TryParse(tail.Substring(0, pos), out result))
                {
                    id = result;
                    bResult = true;
                }
            }
            return bResult;
        }

        private bool DecodeStrNode(string strNode, SPList list, ref string relUrl, ref SPMobileType type)
        {
            relUrl = "";
            bool bDecode = false;
            string tail = "";
            string decodeKey = "href=\"";
            int ind = strNode.IndexOf(decodeKey, StringComparison.OrdinalIgnoreCase);
            if (-1 != ind)
            {
                tail = strNode.Substring(ind + decodeKey.Length);
                ind = tail.IndexOf("\"");
                if (-1 != ind)
                {
                    relUrl = tail.Substring(0, ind);
                    string folderStr = "RootFolder=";
                    //listitem is a folder
                    if (-1 != (ind = relUrl.IndexOf(folderStr, StringComparison.OrdinalIgnoreCase)))
                    {
                        relUrl = relUrl.Substring(ind + folderStr.Length);
                        relUrl = HttpUtility.UrlDecode(relUrl);
                        if (-1 != (ind = relUrl.IndexOf("&", StringComparison.OrdinalIgnoreCase)))
                        {
                            relUrl = relUrl.Substring(0,ind);
                        }
                    }
                    else
                    {
                        decodeKey = "http://";
                        ind = relUrl.IndexOf(decodeKey, StringComparison.OrdinalIgnoreCase);
                        if (-1 == ind)
                        {
                            decodeKey = "https://";
                            ind = relUrl.IndexOf(decodeKey, StringComparison.OrdinalIgnoreCase);
                        }
                        if (-1 != ind)
                        {
                            ind = relUrl.IndexOf("/", ind + decodeKey.Length);
                            if (-1 != ind)
                            {
                                relUrl = relUrl.Substring(ind);
                            }
                        }
                    }
                }
            }
            if (-1 != strNode.IndexOf("menu-item-text",StringComparison.OrdinalIgnoreCase))  // handle menu item
            {
                type = SPMobileType.Unknown;
                bDecode = true;
            }
            else
            {
                if (list != null)
                {
                    type = SPMobileType.SPItems;
                }
                else
                {
                    type = SPMobileType.Unknown;
                }
                bDecode = true;
            }
            return bDecode; // return the relative URL.
        }
    }

    public class SPMobileEvaluation
    {
        private SPWeb m_web;
        private string m_remoteAddr;
        private string m_userId;
        public SPMobileEvaluation(SPWeb web, string remoteAddr)
        {
            m_web = web;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
        }

        public bool Run(List<MobileNodeData> listNodeData)
        {
            try
            {
                EvaluationMultiple mulEval = null;
                TrimmingEvaluationMultiple.NewEvalMult(m_web, ref mulEval);

                foreach (MobileNodeData nodeData in listNodeData)
                {
                    CheckEvalCacheAndSetTrim(mulEval, nodeData);
                }
                mulEval.run();
                foreach (MobileNodeData nodeData in listNodeData)
                {
                    int evalId = nodeData.evalId;
                    if (evalId != -1)
                    {
                        bool bAllow = mulEval.GetTrimEvalResult(evalId);
                        nodeData.bAllow = bAllow;
                        DateTime evalTime = DateTime.Now;
                        TrimmingEvaluationMultiple.AddEvaluationResultCache(m_userId, m_remoteAddr, nodeData.guid, bAllow, evalTime, nodeData.modifyTime);
                    }
                }
                mulEval.ClearRequest();
                mulEval = null;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPMobileEvaluation run:", null, ex);
            }
            return true;
        }

        public void CheckEvalCacheAndSetTrim(EvaluationMultiple mulEval, MobileNodeData nodeData)
        {
            try
            {
                object obj = nodeData.obj;
                if (obj != null)
                {
                    string guid = null;
                    DateTime modifyTime = new DateTime(1, 1, 1);
                    bool bIgnoreTrimControl = Globals.CheckIgnoreTrimControl(HttpContext.Current.Request);
                    using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
                    {
                        bool bTrimLists = bIgnoreTrimControl || manager.CheckListTrimming();

                        if (obj is SPWeb)
                        {
                            SPWeb evalWeb = obj as SPWeb;
                            guid = evalWeb.Url;
                        }
                        else if (obj is SPList)
                        {
                            if (bTrimLists)
                            {
                                SPList evalList = obj as SPList;
                                guid = NextLabs.Common.Utilities.ReConstructListUrl(evalList);
                            }
                        }
                        else if (obj is SPListItem)
                        {
                            SPListItem evalItem = obj as SPListItem;
                            if (bTrimLists || manager.CheckListTrimming(evalItem.ParentList))
                            {
                                guid = evalItem.ParentList.ID.ToString() + evalItem.ID.ToString();
                                modifyTime = NextLabs.Common.Utilities.GetLastModifiedTime(evalItem);
                            }
                        }
                    }


                    if (guid != null)
                    {
                        bool bAllow = true;
                        string srcName = null;
                        string[] srcAttr = null;
                        int idRequest = -1;
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(m_userId, m_remoteAddr, guid, ref bAllow, modifyTime);
                        if (bExisted)
                        {
                            nodeData.bAllow = bAllow;
                        }
                        else
                        {
                            HttpContext Context = HttpContext.Current;
                            string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(obj);
                            Globals.GetSrcNameAndSrcAttr(obj, objUrl, Context, ref srcName, ref srcAttr);
                            mulEval.SetTrimRequest(obj, srcName, srcAttr, out idRequest);
                            nodeData.guid = guid;
                            nodeData.evalId = idRequest;
                            nodeData.modifyTime = modifyTime;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during SPMobileEvaluation CheckEvalCacheAndSetTrim:", null, ex);
            }
        }
    }
}
