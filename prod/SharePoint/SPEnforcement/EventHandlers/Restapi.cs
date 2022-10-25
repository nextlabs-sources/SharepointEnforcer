using System;
using System.IO;
using System.Web;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using System.Xml.Serialization;
using NextLabs.Common;
using System.Web.Script.Serialization;
using System.Collections.Specialized;
using System.Collections.Generic;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public enum RESTAPIMETHOD
    {
        GET,
        POST,
        PUT,
        MERGE,
        DELETE,
        PATCH,
        UNKNOWN
    }

    public class MetaData
    {
        public string type { get; set; }
    }

    public class BodyData
    {
        public MetaData __metadata { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string serverRelativeUrl { get; set; }
        public string url { get; set; }
        public int BaseTemplate { get; set; }
        public string FolderServerRelativeUrl { get; set; }
    }

    public class SiteBodyData
    {
        public BodyData parameters { get; set; }
    }


    public class CopyToData
    {
        public ResourceData srcPath { get; set; }
        public ResourceData destPath { get; set; }
    }

    public class ResourceData
    {
        public MetaData __metadata { get; set; }
        public string DecodedUrl { get; set; }
    }

    // class : decode the REST API request method, url and body, prepare to do evaluation.
    public class RESTAPIModule
    {
        static public string[] RestAPISymbel = { "/_api/", "/_vti_bin/client.svc/", "/_vti_bin/listdata.svc/" };
        private string[] WebTail = { "/web", "/lists"};
        private string[] SPWebType = { "SP.Web", "SP.WebCreationInformation" };
        private string[] SPListType = { "SP.List", "SP.ListCreationInformation" };
        private string[] PostSpecUrl = { "/recycle", "/contextinfo", "/ensuresitepageslibrary", "/ensuresiteassetslibrary", "/deleteobject" };
        private string[] PostOpenUrl = { "/contextinfo", "/getsharinginformation", "/getchanges", "/getitems", "/getlistitemchanges", "/apptiles",
                        "/getrelatedfields", "/getview", "/render", "/getlistdata", "/reservelistitemid", "/customactionelements", "/getonepagecontextasstream" };
        private string[] PostDontCare = { "/applytheme", "/applywebtemplate", "/breakroleinheritance",
                        "/doespushnotificationsubscriberexist", "/doesuserhavepermissions", "/ensureuser",
                        "/executeremotelob", "/getappbdccatalog", "/getappbdccatalogforappinstance",
                        "/getappinstancebyid", "/getappinstancesbyproductid", "/getavailablewebtemplates",
                        "/getcatalog", "/getcustomlisttemplates", "/getentity",
                        "/getpushnotificationsubscriber", "/getpushnotificationsubscribersbyargs",
                        "/getpushnotificationsubscribersbyuser", "/getsubwebsfilteredforcurrentuser",
                        "/getuserbyid", "/getusereffectivepermissions", "/loadandinstallapp",
                        "/loadandinstallappinspecifiedlocale", "/loadapp", "/maptoicon",
                        "/processexternalnotification", "/registerpushnotificationsubscriber",
                        "/resetroleinheritance", "/unregisterpushnotificationsubscriber" };
        private string[] BodyTail = { "parameters", "query" };
        private string[] ContentType = { "application/json", "application/atom+xml" };
        private SPEEvalAttr m_evaAttr;
        private HttpRequest m_request;
        private RESTAPIMETHOD m_method;
        private string m_version;

        public RESTAPIModule(HttpRequest request)
        {
            m_request = request;
            m_evaAttr = SPEEvalAttrs.Current();
            m_method = GetMethod(request.HttpMethod);
            m_version = null;
        }

        static public bool IsRESTAPIRequest(string url)
        {
            if (-1 != url.IndexOf(RestAPISymbel[0], StringComparison.OrdinalIgnoreCase)
                || -1 != url.IndexOf(RestAPISymbel[1], StringComparison.OrdinalIgnoreCase)
                 || -1 != url.IndexOf(RestAPISymbel[2], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private bool DecodeNewRestApi(string reqUrl)
        {
            try
            {
                if (-1 != reqUrl.IndexOf("content.downloadUrl", StringComparison.OrdinalIgnoreCase) && m_request.UrlReferrer != null && m_method == RESTAPIMETHOD.GET)
                {
                    string itemPath = HttpUtility.ParseQueryString(m_request.UrlReferrer.Query)["id"];
                    if (!string.IsNullOrEmpty(itemPath))
                    {
                        SPWeb web = (SPWeb)Utilities.GetCachedSPContent(null, m_request.UrlReferrer.AbsoluteUri, Utilities.SPUrlWeb);
                        if (web != null)
                        {
                            SPListItem item = web.GetListItem(itemPath);
                            if (item != null)
                            {
                                m_evaAttr.Action = "READ";
                                m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
                                SPEEvalAttrHepler.SetObjEvalAttr(item, m_evaAttr);
                                return true;
                            }
                        }
                    }
                }
                else if (-1 != reqUrl.IndexOf("/SP.MoveCopyUtil.CopyFileByPath()", StringComparison.OrdinalIgnoreCase) && m_request.ContentLength > 0)
                {
                    string contType = m_request.ContentType;
                    Stream inputStream = m_request.InputStream;
                    byte[] contentBuf = new byte[inputStream.Length];
                    long _oldPos = inputStream.Seek(0, SeekOrigin.Current);
                    inputStream.Read(contentBuf, 0, (int)inputStream.Length);
                    String strBody = Globals.UrlDecode(m_request.ContentEncoding.GetString(contentBuf));
                    inputStream.Seek(_oldPos, SeekOrigin.Begin);
                    if (!string.IsNullOrEmpty(strBody) && -1 != contType.IndexOf("/json", StringComparison.OrdinalIgnoreCase))
                    {
                        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        CopyToData dataObject = serializer.Deserialize<CopyToData>(strBody);
                        if (dataObject != null && !string.IsNullOrEmpty(dataObject.srcPath.DecodedUrl))
                        {
                            string fullUrl = dataObject.srcPath.DecodedUrl;
                            if (m_evaAttr.WebObj != null)
                            {
                                SPListItem item = (SPListItem)Utilities.GetCachedSPContent(m_evaAttr.WebObj, fullUrl, Utilities.SPUrlListItem);
                                if (item != null)
                                {
                                    m_evaAttr.Action = "READ";
                                    m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
                                    SPEEvalAttrHepler.SetObjEvalAttr(item, m_evaAttr);
                                    return true;
                                }
                            }
                        }
                    }
                }
                else if (-1 != reqUrl.IndexOf("/SP.APIHubConnector.GetListItems(", StringComparison.OrdinalIgnoreCase)
                    || -1 != reqUrl.IndexOf("/SP.APIHubConnector.GetTableMetadata(", StringComparison.OrdinalIgnoreCase))
                {
                    DecodeSpecificUrl(m_request, ref reqUrl);
                    string strParamFlag = "'";
                    if (m_evaAttr.WebObj != null && -1 != reqUrl.IndexOf(strParamFlag))
                    {
                        SPList list = null;
                        int indBegin = reqUrl.IndexOf(strParamFlag);
                        int indEnd = reqUrl.IndexOf(strParamFlag, indBegin + strParamFlag.Length);
                        string strListName = reqUrl.Substring(indBegin + strParamFlag.Length, indEnd - indBegin - strParamFlag.Length);
                        Guid listGuid = new Guid();
                        bool bParse = Guid.TryParse(strListName, out listGuid);
                        if (bParse)
                        {
                            list = m_evaAttr.WebObj.Lists[listGuid]; // SPList Guid
                        }
                        else
                        {
                            list = m_evaAttr.WebObj.Lists[strListName]; // SPList Title
                        }
                        if (list != null)
                        {
                            m_evaAttr.Action = "READ";
                            m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
                            SPEEvalAttrHepler.SetObjEvalAttr(list, m_evaAttr);
                            return true;
                        }
                    }
                }
                else if (-1 != reqUrl.IndexOf("/_api/sitepages/pages", StringComparison.OrdinalIgnoreCase)
                    && -1 != reqUrl.IndexOf("/checkoutpage", StringComparison.OrdinalIgnoreCase)
                    && m_request.UrlReferrer != null && m_method == RESTAPIMETHOD.POST)
                {
                    string fullUrl = m_request.UrlReferrer.ToString();
                    if(-1 != fullUrl.IndexOf("?"))
                    {
                        fullUrl = fullUrl.Substring(0, fullUrl.IndexOf("?"));
                    }
                    SPListItem item = (SPListItem)Utilities.GetCachedSPContent(m_evaAttr.WebObj, fullUrl, Utilities.SPUrlListItem);
                    if (item != null)
                    {
                        m_evaAttr.Action = "Edit";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
                        SPEEvalAttrHepler.SetObjEvalAttr(item, m_evaAttr);
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DecodeNewRestApi:", null, ex);
            }
            return false;
        }

        // Ex: Convert "http://qapf1-sp19/basicsite/_api/web/lists(@appId)/recycle?@appId='8bf74251-7b7c-4124-91da-126e1511528b'" to
        // "http://qapf1-sp19/basicsite/_api/web/lists('8bf74251-7b7c-4124-91da-126e1511528b')/recycle"
        public static bool DecodeSpecificUrl(HttpRequest request, ref string url)
        {
            bool bRet = false;
            url = url.ToLower();
            if (-1 != url.IndexOf("?"))
            {
                url = url.Substring(0, url.IndexOf("?"));
            }
            if (!string.IsNullOrEmpty(url) && -1 != url.IndexOf("@") && request.QueryString != null)
            {
                Dictionary<string, string> dicValues = new Dictionary<string, string>();
                foreach (string key in request.QueryString.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && -1 != key.IndexOf("@"))
                    {
                        dicValues.Add(key.ToLower(), request.QueryString[key].ToLower());
                    }
                }
                foreach (string key in dicValues.Keys)
                {
                    url = url.Replace(key, dicValues[key]);
                }
                bRet = true;
            }

            return bRet;
        }

        // Don't care some rest api cases.
        private bool CheckCareRestApi(string url)
        {
            if (-1 != url.IndexOf(WebTail[0], StringComparison.OrdinalIgnoreCase)
                || -1 != url.IndexOf(WebTail[1], StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(PostSpecUrl[1], StringComparison.OrdinalIgnoreCase))
            {
                foreach (string cell in PostDontCare)
                {
                    if (-1 != url.IndexOf(cell, StringComparison.OrdinalIgnoreCase))
                    {
                        return false; // Don't care this Rest Api url.
                    }
                }
                return true;
            }
            return false;
        }

        // decode url and method, if need it will decode body. Then set m_evaAttr attributes for evaluation.
        public bool DecodeRestApiRequest()
        {
            string rawUrl = m_request.RawUrl;
            rawUrl = Globals.UrlDecode(rawUrl);
#if SP2019
            if (DecodeNewRestApi(rawUrl))
            {
                return true;
            }
#endif
            DecodeSpecificUrl(m_request, ref rawUrl);
            if (-1 != rawUrl.IndexOf(RestAPISymbel[2], StringComparison.OrdinalIgnoreCase))
            {
                return Decode2010RestApi(rawUrl);
            }
            if (!CheckCareRestApi(rawUrl))
            {
                return false; // Don't care these rest api.
            }

            bool bRet = DecodeRestApiUrl(rawUrl);
            if (bRet)
            {
                DecodeMethod(rawUrl);
                CheckIgnoreCase(rawUrl);
            }
            return bRet;
        }

        private void CheckIgnoreCase(string rawUrl)
        {
            if (m_evaAttr.ItemObj != null && (m_evaAttr.PolicyAction == CETYPE.CEAction.Write || m_evaAttr.PolicyAction == CETYPE.CEAction.Delete))
            {
                // Ignore some edit/delete list item cases, do it in event with correct denied message.
                if (!rawUrl.Contains("checkout"))
                {
                    m_evaAttr.Action = "UNKNOWN_ACTION";
                }
            }
        }

        public bool Decode2010RestApi(string url)
        {
            int ind = url.IndexOf(RestAPISymbel[2], StringComparison.OrdinalIgnoreCase);
            if (-1 != ind)
            {
                // SP2010 rest api service.
                SPWeb evalWeb = m_evaAttr.WebObj;
                if (evalWeb != null)
                {
                    if (ind + RestAPISymbel[2].Length < url.Length)
                    {
                        string listName = url.Substring(ind + RestAPISymbel[2].Length);
                        try
                        {
                            SPList evalList = evalWeb.Lists[listName]; // It will happen exception when listName is not matched.
                            if (evalList != null)
                            {
                                SPEEvalAttrHepler.SetObjEvalAttr(evalList, m_evaAttr);
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                    SPEEvalAttrHepler.SetObjEvalAttr(evalWeb, m_evaAttr); ;
                    return true;
                }
            }
            return false;
        }

        // Decode mobile list access in SP2016.
        public object DecodeMobilelList(SPList list)
        {
            object evalObj = list;
            try
            {
                // example rawUrl: "/_api/web/GetList('/dlib1')/RenderListDataAsStream".
                SPListItem item = null;
                if (list != null)
                {
                    BodyData objectData = null;
                    if (m_request.ContentLength > 0)
                    {
                        objectData = DecodeBody();
                        if (objectData != null && !String.IsNullOrEmpty(objectData.FolderServerRelativeUrl))
                        {
                            string fullUrl = list.ParentWeb.Site.MakeFullUrl(objectData.FolderServerRelativeUrl);
                            item = (SPListItem)Utilities.GetCachedSPContent(list.ParentWeb, fullUrl, Utilities.SPUrlListItem);
                            if (item != null)  // item , folder and document set case
                            {
                                evalObj = item;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return evalObj;
        }

        // decode the rest api request url to sharepoint url and collect the information for Evaluation.
        private bool DecodeRestApiUrl(string url)
        {
            try
            {
                int ind = -1;
                if (-1 != (ind = url.IndexOf(RestAPISymbel[0], StringComparison.OrdinalIgnoreCase)))
                {
                    url = url.Substring(ind + RestAPISymbel[0].Length - 1);
                }
                else if (-1 != (ind = url.IndexOf(RestAPISymbel[1], StringComparison.OrdinalIgnoreCase)))
                {
                    url = url.Substring(ind + RestAPISymbel[1].Length - 1);
                }

                RestApiEvalObject restApiEval = new RestApiEvalObject(m_request, m_evaAttr.WebObj);
                object evalObj = null;
                bool bRet = restApiEval.Run(url, ref evalObj, ref m_version);
                if (bRet && evalObj != null)
                {
                    if (evalObj is SPWeb)
                    {
                        SPWeb evalWeb = evalObj as SPWeb;
                        SetWebAttrs(evalWeb);
                    }
                    else
                    {
                        // Decode mobile list access in SP2016. (item, document set, folder)
                        if (url.EndsWith("/renderlistdataasstream") && evalObj is SPList)
                        {
                            SPList list = evalObj as SPList;
                            evalObj = DecodeMobilelList(list);
                        }
                        SPEEvalAttrHepler.SetObjEvalAttr(evalObj, m_evaAttr);
                    }
                    m_evaAttr.FileVersion = m_version;
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DecodeRestApiUrl:", null, ex);
            }
            return false;
        }

        private RESTAPIMETHOD GetMethod(string method)
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.GET;
            }
            else if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.POST;
            }
            else if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.PUT;
            }
            else if (method.Equals("MERGE", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.MERGE;
            }
            else if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.DELETE;
            }
            else if (method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            {
                return RESTAPIMETHOD.PATCH;
            }
            else
            {
                return RESTAPIMETHOD.UNKNOWN;
            }
        }

        private void SetWebAttrs(SPWeb web)
        {
            BodyData data = DecodeBody();
            if (data != null && data.__metadata != null)
            {
                string type = data.__metadata.type;
                string title = data.title;
                if (type != null && title != null)
                {
                    if (type.Equals(SPListType[0], StringComparison.OrdinalIgnoreCase)
                        || type.Equals(SPListType[1], StringComparison.OrdinalIgnoreCase))
                    {
                        m_evaAttr.ObjEvalUrl = m_evaAttr.ObjEvalUrl + "/" + title;
                        m_evaAttr.ObjName = title;
                        m_evaAttr.ObjTitle = title;
                        m_evaAttr.ObjDesc = data.description;
                        //check if BaseTemplate is Decument library type.
                        if (Globals.IsLibraryTemplateID(data.BaseTemplate.ToString()))
                        {
                            m_evaAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                        }
                        else
                        {
                            m_evaAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                        }
                        m_evaAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                        m_evaAttr.Action = "Create";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
                        return;
                    }
                    else if (data.url != null && (type.Equals(SPWebType[0], StringComparison.OrdinalIgnoreCase)
                        || type.Equals(SPWebType[1], StringComparison.OrdinalIgnoreCase)))
                    {
                        String url = data.url.StartsWith("/") ? data.url : "/" + data.url;
                        m_evaAttr.ObjEvalUrl = m_evaAttr.ObjEvalUrl + url;
                        m_evaAttr.ObjName = title;
                        m_evaAttr.ObjTitle = title;
                        m_evaAttr.ObjDesc = data.description;
                        m_evaAttr.Action = "Create";
                        m_evaAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                        m_evaAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
                        return;
                    }
                }
            }

            SPEEvalAttrHepler.SetObjEvalAttr(web, m_evaAttr);
        }

        public void DecodeMethod(string url)
        {
            if (m_method == RESTAPIMETHOD.GET)
            {
                m_evaAttr.Action = "READ";
                m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
            }
            else if (m_method == RESTAPIMETHOD.POST)
            {
                string xHttpMethod = m_request.Headers["X-HTTP-Method"];
                bool bDelete = false;
                if (xHttpMethod != null)
                {
                    if (xHttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                    {
                        m_evaAttr.Action = "DELETE";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Delete;
                        bDelete = true;
                    }
                }
                if (url.EndsWith(PostSpecUrl[0], StringComparison.OrdinalIgnoreCase)
                    || url.EndsWith(PostSpecUrl[4], StringComparison.OrdinalIgnoreCase))
                {
                    m_evaAttr.Action = "DELETE";
                    m_evaAttr.PolicyAction = CETYPE.CEAction.Delete;
                    bDelete = true;
                }

                if (!bDelete)
                {
                    bool bOpenAction = false;
                    foreach (string openUrl in PostOpenUrl)
                    {
                        if (-1 != url.IndexOf(openUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            bOpenAction = true;
                            break;
                        }
                    }
                    if (bOpenAction)
                    {
                        m_evaAttr.Action = "Open";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
                    }
                    else
                    {
                        m_evaAttr.Action = "EDIT";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
                    }
                }
            }
            else if (m_method == RESTAPIMETHOD.PUT || m_method == RESTAPIMETHOD.MERGE || m_method == RESTAPIMETHOD.PATCH)
            {
                m_evaAttr.Action = "EDIT";
                m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
            }
            else if (m_method == RESTAPIMETHOD.DELETE)
            {
                m_evaAttr.Action = "DELETE";
                m_evaAttr.PolicyAction = CETYPE.CEAction.Delete;
            }
            else
            {
                m_evaAttr.Action = "UNKNOWN_ACTION";
            }
        }

        // just supprot xml and json format body. Decode body using Deserialize.
        private BodyData DecodeBody()
        {
            try
            {
                BodyData dataObject = null;
                string contType = m_request.ContentType;
                Stream inputStream = m_request.InputStream;
                byte[] contentBuf = new byte[inputStream.Length];
                long _oldPos = inputStream.Seek(0, SeekOrigin.Current);
                inputStream.Read(contentBuf, 0, (int)inputStream.Length);
                String strBody = Globals.UrlDecode(Encoding.UTF8.GetString(contentBuf));
                inputStream.Seek(_oldPos, SeekOrigin.Begin);
                if (strBody != null && contType != null)
                {
                    // Deserialize json body to object
                    if (-1 != contType.IndexOf(ContentType[0], StringComparison.OrdinalIgnoreCase))
                    {
                        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        dataObject = (BodyData)serializer.Deserialize<BodyData>(strBody);
                        if ((dataObject == null || dataObject.__metadata == null)
                            && -1 != strBody.IndexOf(BodyTail[0], StringComparison.OrdinalIgnoreCase))
                        {
                            SiteBodyData siteObj = serializer.Deserialize<SiteBodyData>(strBody);
                            dataObject = siteObj.parameters;
                        }
                    }
                    // Deserialize xml body to object
                    else if (-1 != contType.IndexOf(ContentType[1], StringComparison.OrdinalIgnoreCase))
                    {
                        Stream stream = m_request.InputStream;
                        XmlSerializer serializer = new XmlSerializer(typeof(BodyData));
                        dataObject = (BodyData)serializer.Deserialize(stream);
                        if ((dataObject == null || dataObject.__metadata == null)
                            && -1 != strBody.IndexOf(BodyTail[0], StringComparison.OrdinalIgnoreCase))
                        {
                            serializer = new XmlSerializer(typeof(SiteBodyData));
                            SiteBodyData siteObj = (SiteBodyData)serializer.Deserialize(inputStream);
                            dataObject = siteObj.parameters;
                        }
                        inputStream.Seek(_oldPos, SeekOrigin.Begin);
                    }

                    // 'query' metadata is read action.
                    if ((dataObject == null || dataObject.__metadata == null) &&
                        - 1 != strBody.IndexOf(BodyTail[1], StringComparison.OrdinalIgnoreCase))
                    {
                        m_evaAttr.Action = "READ";
                        m_evaAttr.PolicyAction = CETYPE.CEAction.Read;
                    }
                }

                return dataObject;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DecodeBody:", null, ex);
            }
            return null;
        }
    }

    public class RestAPIPQuery
    {
        public static string[] StrQuery = { "/_vti_bin/client.svc/ProcessQuery", "/_vti_bin/client.svc" };
        private HttpRequest m_request;
        private string m_RewUrl;
        private string m_title;
        private string m_templateType;
        private string m_description;
        private SPEEvalAttr m_evaAttr;
        public RestAPIPQuery(HttpRequest request)
        {
            m_request = request;
            m_RewUrl = Globals.UrlDecode(request.RawUrl);
            m_title = null;
            m_templateType = null;
            m_description = null;
            m_evaAttr = SPEEvalAttrs.Current();
        }

        public bool IfNeedEvalQuery()
        {
            bool bNeed = false;
            try
            {
                if (m_RewUrl.EndsWith(StrQuery[0], StringComparison.OrdinalIgnoreCase)
                    && m_request.ContentLength != 0
                    && -1 != m_request.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase))
                {
                    Stream stream = m_request.InputStream;
                    Encoding encoding = m_request.ContentEncoding;
                    bNeed = NeedEvalStream(stream, encoding);
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during IfNeedEvalQuery:", null, ex);
            }
            return bNeed;
        }

        public bool NeedEvalStream(Stream stream, Encoding encoding)
        {
            bool bAdd = false;
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
                foreach (XmlNode child in objPath.ChildNodes)
                {
                    if (child.Name.Equals("Method", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (XmlAttribute attr in child.Attributes)
                        {
                            if (attr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                && attr.Value.Equals("Add", StringComparison.OrdinalIgnoreCase))
                            {
                                bAdd = true;
                                break;
                            }
                        }
                        if (bAdd && child["Parameters"] != null)
                        {
                            XmlNode parameter = child["Parameters"]["Parameter"];
                            if (parameter != null)
                            {
                                foreach (XmlNode property in parameter.ChildNodes)
                                {
                                    if (property.Name.Equals("Property", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (XmlAttribute propAttr in property.Attributes)
                                        {
                                            if (propAttr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                                && propAttr.Value.Equals("Title", StringComparison.OrdinalIgnoreCase))
                                            {
                                                m_title = property.InnerXml;
                                                break;
                                            }
                                            else if (propAttr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                                && propAttr.Value.Equals("TemplateType", StringComparison.OrdinalIgnoreCase))
                                            {
                                                m_templateType = property.InnerXml;
                                                break;
                                            }
                                            else if (propAttr.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                                                && propAttr.Value.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                            {
                                                m_description = property.InnerXml;
                                                break;
                                            }
                                        }
                                    }
                                    if (m_title != null && m_templateType != null && m_description != null)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return false;
        }

        public void SetEvalAttrs()
        {
            m_evaAttr.Action = "NEW";
            m_evaAttr.PolicyAction = CETYPE.CEAction.Write;
            string reqestUrl = m_evaAttr.RequestURL;
            string webUrl = reqestUrl.Substring(0, reqestUrl.IndexOf(StrQuery[1]));
            SPWeb web = (SPWeb)Utilities.GetCachedSPContent(null, webUrl, Utilities.SPUrlWeb);
            m_evaAttr.WebObj = web;
            m_evaAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            if (m_templateType != null && Globals.IsLibraryTemplateID(m_templateType))
            {
                m_evaAttr.ObjEvalUrl = webUrl + "/" + m_title;
                m_evaAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
            }
            else
            {
                m_evaAttr.ObjEvalUrl = webUrl + "/" + "Lists" + "/" + m_title;
                m_evaAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
            }
            m_evaAttr.ObjTitle = m_title;
            m_evaAttr.ObjName = m_title;
            m_evaAttr.ObjDesc = m_description;
        }
    }

    class SegmentData
    {
        public string Segment;
        public List<string> ListParam;
        public SegmentData()
        {
            Segment = "";
            ListParam = new List<string>();
        }
        public SegmentData(string strSegment)
        {
            Segment = strSegment;
            ListParam = new List<string>();
        }
    }

    class RestApiEvalObject
    {
        private SPWeb m_web;
        private HttpRequest m_request;
        private List<SegmentData> m_listSegments;
        private object m_evalObj;
        private int m_nIndSegment;
        private string m_itemVersion;

        public RestApiEvalObject(HttpRequest request, SPWeb web)
        {
            m_web = web;
            m_request = request;
            m_listSegments = new List<SegmentData>();
            m_evalObj = null;
            m_nIndSegment = 0;
            m_itemVersion = "";
        }

        public bool Run(string url, ref object evalObject, ref string itemVersion)
        {
            try
            {
                if (m_web != null)
                {
                    if(SplitUrl(url, m_listSegments))
                    {
                        WebRun(m_web);
                        evalObject = m_evalObj;
                        itemVersion = m_itemVersion;
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestApiEvalObject Run:", null, ex);
            }

            return false;
        }

        // Split parameters and segment from the url
        private bool SplitUrl(string url, List<SegmentData> listSegment)
        {
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    url = url.ToLower();
                    int indBegin = -1;
                    int indEnd = -1;
                    while (true)
                    {
                        indBegin = url.IndexOf("(");
                        if (-1 != indBegin)
                        {
                            indEnd = url.IndexOf(")/");
                            if (-1 == indEnd && url.EndsWith(")"))
                            {
                                indEnd = url.Length - 1;
                            }
                            if (-1 != indEnd)
                            {
                                string strParam = url.Substring(indBegin + 1, indEnd - indBegin - 1);
                                string leftUrl = url.Substring(0, indBegin);
                                string[] arrSegments = leftUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < arrSegments.Length; i++)
                                {
                                    SegmentData segmentData = new SegmentData(arrSegments[i]);
                                    if (i == arrSegments.Length - 1)
                                    {
                                        segmentData.ListParam = SplitParamters(strParam);
                                    }
                                    listSegment.Add(segmentData);
                                }
                                if (indEnd == url.Length - 1)
                                {
                                    break;
                                }
                                url = url.Substring(indEnd + 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            string rightUrl = url;
                            string[] arrSegments = rightUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < arrSegments.Length; i++)
                            {
                                SegmentData segmentData = new SegmentData(arrSegments[i]);
                                listSegment.Add(segmentData);
                            }
                            break;
                        }
                    }
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private List<string> SplitParamters(string strParam)
        {
            List<string> listParam = new List<string>();
            if (!string.IsNullOrEmpty(strParam))
            {
                try
                {
                    while (true)
                    {
                        strParam = strParam.Trim();
                        int indString = strParam.IndexOf("'");
                        if (-1 != indString)
                        {
                            string leftParam = strParam.Substring(0, indString);
                            int indSplit = leftParam.LastIndexOf(",");
                            if (-1 != indSplit)
                            {
                                leftParam = leftParam.Substring(0, indSplit);
                                string[] arrParam = leftParam.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                listParam.AddRange(arrParam);
                            }
                            int indEnd = strParam.IndexOf("'", indString + 1);
                            if (-1 != indEnd)
                            {
                                string cellParam = strParam.Substring(indString, indEnd - indString + 1);
                                listParam.Add(cellParam);
                                strParam = strParam.Substring(indEnd + 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            string[] arrParam = strParam.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            listParam.AddRange(arrParam);
                            break;
                        }
                    };
                }
                catch
                {
                }
            }
            for (int i = 0; i < listParam.Count; i++)
            {
                listParam[i] = ConvertParameter(listParam[i]);
            }
            return listParam;
        }

        // Convert "decodeurl='/share documents/f1/aa.docx/'" to "/share documents/f1/aa.docx"
        private string ConvertParameter(string strParam)
        {
            if (!string.IsNullOrEmpty(strParam))
            {
                strParam = strParam.Trim(); // Remove the empty char in first and end.
                if (strParam.StartsWith("'"))
                {
                    strParam = strParam.Trim('\'');
                }
                else
                {
                    int ind = strParam.IndexOf("=");
                    if (-1 != ind)
                    {
                        strParam = strParam.Substring(ind + 1);
                        strParam = strParam.Trim();
                    }
                    strParam = strParam.Trim('\'');
                }
                strParam = strParam.TrimEnd('/');
            }
            return strParam;
        }

        private void WebRun(SPWeb web)
        {
            if (web != null)
            {
                m_evalObj = web;
            }
            try
            {
                if (m_listSegments.Count > m_nIndSegment && web != null)
                {
                    SegmentData segmentData = m_listSegments[m_nIndSegment];
                    m_nIndSegment++;
                    string strSegment = segmentData.Segment;

                    if (!string.IsNullOrEmpty(strSegment) && strSegment.Equals("web") && m_listSegments.Count > m_nIndSegment)
                    {
                        segmentData = m_listSegments[m_nIndSegment];
                        strSegment = segmentData.Segment;
                        m_nIndSegment++;
                    }

                    if (!string.IsNullOrEmpty(strSegment))
                    {
                        string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";
                        if (strSegment.Equals("webs"))
                        {
                            WebCollectionRun(web.Webs);
                        }
                        else if (strSegment.Equals("lists"))
                        {
                            if (!string.IsNullOrEmpty(strParam))
                            {
                                ListRun(web.Lists[new Guid(strParam)]);
                            }
                            else
                            {
                                ListCollectionRun(web.Lists);
                            }
                        }
                        else if (strSegment.Equals("folders"))
                        {
                            FolderCollectionRun(web.Folders);
                        }
                        else if (strSegment.Equals("rootfolder"))
                        {
                            FolderRun(web.RootFolder);
                        }
                        else if (strSegment.Equals("parentweb"))
                        {
                            WebRun(web.ParentWeb);
                        }
                        else if (strSegment.Equals("siteuserinfolist"))
                        {
                            ListRun(web.SiteUserInfoList);
                        }
                        else if (strSegment.Equals("getsubwebsforcurrentuser"))
                        {
                            WebCollectionRun(web.GetSubwebsForCurrentUser());
                        }
                        else if (strSegment.Equals("getfilebyid"))
                        {
                            FileRun(web.GetFile(new Guid(strParam)));
                        }
                        else if (strSegment.Equals("getfilebyserverrelativeurl"))
                        {
                            if (string.IsNullOrEmpty(strParam))
                            {
                                strParam = ConvertParameter(m_request.QueryString["serverrelativeurl"]);
                            }
                            FileRun(web.GetFile(strParam));
                        }
                        else if (strSegment.Equals("getfolderbyid"))
                        {
                            FolderRun(web.GetFolder(new Guid(strParam)));
                        }
                        else if (strSegment.Equals("getfolderbyserverrelativeurl"))
                        {
                            if(string.IsNullOrEmpty(strParam))
                            {
                                strParam = ConvertParameter(m_request.QueryString["serverrelativeurl"]);
                            }
                            FolderRun(web.GetFolder(strParam));
                        }
                        else if (strSegment.Equals("getcatalog"))
                        {
                            SPListTemplateType templateType = (SPListTemplateType)(int.Parse(strParam));
                            SPList list = web.GetCatalog(templateType);
                            ListRun(list);
                        }
#if SP2016 || SP2019
                        else if (strSegment.Equals("defaultdocumentlibrary"))
                        {
                            ListRun(web.DefaultDocumentLibrary());
                        }
                        else if (strSegment.Equals("getlist"))
                        {
                            ListRun(web.GetList(strParam));
                        }
                        else if (strSegment.Equals("getfilebyguesturl"))
                        {
                            FileRun(web.GetFileByGuestUrl(strParam));
                        }
                        else if (strSegment.Equals("getfilebylinkingurl"))
                        {
                            FileRun(web.GetFileByLinkingUrl(strParam));
                        }
                        else if (strSegment.Equals("getfilebyid"))
                        {
                            FileRun(web.GetFile(new Guid(strParam)));
                        }
                        else if (strSegment.Equals("getfolderbyid"))
                        {
                            FolderRun(web.GetFolder(new Guid(strParam)));
                        }
                        else if (strSegment.Equals("getfolderbyserverrelativepath"))
                        {
                            FolderRun(web.GetFolder(strParam));
                        }
                        else if (strSegment.Equals("getfilebyserverrelativepath"))
                        {
                            FileRun(web.GetFile(strParam));
                        }
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RestApiEvalObject WebRun:", null, ex);
            }
        }

        private void WebCollectionRun(SPWebCollection webColl)
        {
        }

        private void ListRun(SPList list)
        {
            if (list != null)
            {
                m_evalObj = list;
            }
            if (m_listSegments.Count > m_nIndSegment && list != null)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if(!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("rootfolder"))
                    {
                        FolderRun(list.RootFolder);
                    }
                    else if (strSegment.Equals("parentweb"))
                    {
                        WebRun(list.ParentWeb);
                    }
                    else if (strSegment.Equals("getitembyid") || strSegment.Equals("getitembystringid"))
                    {
                        ListItemRun(list.GetItemById(int.Parse(strParam)));
                    }
                    else if (strSegment.Equals("getitems"))
                    {
                        ListItemCollectionRun(list.Items);
                    }
                    else if(strSegment.Equals("items"))
                    {
                        if (!string.IsNullOrEmpty(strParam))
                        {
                            ListItemRun(list.GetItemById(int.Parse(strParam)));
                        }
                        else
                        {
                            ListItemCollectionRun(list.Items);
                        }
                    }
                    else if (strSegment.Equals("renderextendedlistformdata"))
                    {
                        ListItemRun(list.GetItemById(int.Parse(strParam)));
                    }
                }
            }
        }

        private void ListCollectionRun(SPListCollection listColl)
        {
            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("getbyid"))
                    {
                        ListRun(listColl[new Guid(strParam)]);
                    }
                    else if (strSegment.Equals("getbytitle"))
                    {
                        ListRun(listColl.TryGetList(strParam));
                    }
                    else if (strSegment.Equals("ensuresitepageslibrary"))
                    {
                        ListRun(listColl.EnsureSitePagesLibrary());
                    }
                    else if (strSegment.Equals("ensuresiteassetslibrary"))
                    {
                        ListRun(listColl.EnsureSiteAssetsLibrary());
                    }
#if SP2019
                    else if (strSegment.Equals("ensureclientrenderedsitepageslibrary"))
                    {
                        ListRun(listColl.EnsureClientRenderedSitePagesLibrary());
                    }
#endif
                }
            }
        }

        private void ListItemRun(SPListItem item)
        {
            if (item != null)
            {
                m_evalObj = item;
            }
            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("parentlist"))
                    {
                        ListRun(item.ParentList);
                    }
                    else if (strSegment.Equals("file"))
                    {
                        FileRun(item.File);
                    }
                    else if (strSegment.Equals("folder"))
                    {
                        FolderRun(item.Folder);
                    }
                    else if (strSegment.Equals("version"))
                    {
                        if (!string.IsNullOrEmpty(strParam))
                        {
                            m_itemVersion = strParam;
                        }
                    }
                }
            }
        }

        private void ListItemCollectionRun(SPListItemCollection itemColl)
        {
            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("getbyid") || strSegment.Equals("getbystringid"))
                    {
                        ListItemRun(itemColl.GetItemById(int.Parse(strParam)));
                    }
                }
            }
        }

        private void FolderRun(SPFolder folder)
        {
            if (folder != null)
            {
                Object evalObj = Globals.GetListOrItemFromSPFolder(m_web, folder);
                if (evalObj != null)
                {
                    m_evalObj = evalObj;
                }
            }

            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("parentfolder"))
                    {
                        FolderRun(folder.ParentFolder);
                    }
                    else if (strSegment.Equals("files"))
                    {
                        if (!string.IsNullOrEmpty(strParam))
                        {
                            FileRun(folder.Files[strParam]);
                        }
                        else
                        {
                            FileCollectionRun(folder.Files);
                        }
                    }
                    else if (strSegment.Equals("folders"))
                    {
                        if (!string.IsNullOrEmpty(strParam))
                        {
                            FolderRun(folder.SubFolders[strParam]);
                        }
                        else
                        {
                            FolderCollectionRun(folder.SubFolders);
                        }
                    }
                }
            }
        }

        private void FolderCollectionRun(SPFolderCollection folderColl)
        {
            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("getbyurl"))
                    {
                        FolderRun(folderColl[strParam]);
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

            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
               // string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("listitemallfields"))
                    {
                        ListItemRun(file.ListItemAllFields);
                    }
                }
            }
        }

        private void FileCollectionRun(SPFileCollection fileColl)
        {
            if (m_listSegments.Count > m_nIndSegment)
            {
                SegmentData segmentData = m_listSegments[m_nIndSegment];
                m_nIndSegment++;
                string strSegment = segmentData.Segment;
                string strParam = segmentData.ListParam.Count > 0 ? segmentData.ListParam[0] : "";

                if (!string.IsNullOrEmpty(strSegment))
                {
                    if (strSegment.Equals("getbyurl"))
                    {
                        FileRun(fileColl[strParam]);
                    }
                }
            }
        }
    }
};
