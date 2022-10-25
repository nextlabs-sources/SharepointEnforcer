using System;
using System.IO;
using System.Web;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using NextLabs.Common;
using System.Web.Script.Serialization;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public enum FilterType
    {
        AuthorTrimmer,
        EditorTrimmer,
        CsomTrimmer,
        FieldIdTrimmer,
        MobileTrimmer,
        PostTrimmer,
        RestApiTrimmer,
        SoapTrimmer,
        SearchTrimmer,
        SPServiceTrimmer,
        PageTrimmer,
        Unknown
    }

    public enum PageFilterType
    {
        Tasks,
        Calendar,
        ViewEdit,
        Unknown
    }


    public class TrimmerGlobal
    {
        // Check the input string is json format or not.
        public static bool CheckJsonFormat(string jsonString)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                object obj = serializer.Deserialize(jsonString, typeof(object));
                if (obj != null)
                {
                    return true;
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during CheckJsonFormat:", null, ex);
            }
            return false;
        }

        // Get pair string "{1{2}3}" from "{1{2}3}4{5}".
        public static bool GetPairString(string left, string right, string data, ref string endData)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right) || string.IsNullOrEmpty(data))
            {
                return false;
            }
            int indRight = 0;
            int indLeft = data.IndexOf(left);
            bool bMatch = false;
            if (-1 != indLeft)
            {
                int indBegin = indLeft;
                while (true)
                {
                    indLeft = data.IndexOf(left, indLeft + 1);
                    indRight = data.IndexOf(right, indRight + 1);
                    if ((-1 != indRight) && (-1 == indLeft || indLeft > indRight))
                    {
                        bMatch = true;
                        break;
                    }
                    else if (-1 == indLeft || -1 == indRight)
                    {
                        break;
                    }
                }
                if (bMatch && 0 <= (indRight - indBegin - left.Length))
                {
                    endData = data.Substring(indBegin + left.Length, indRight - indBegin - left.Length);
                    return true;
                }
            }
            return false;
        }


        public static bool SplitMetaData(string left, string right, string metaData, List<string> dataList)
        {
            while (true)
            {
                string data = "";
                if (GetPairString(left, right, metaData, ref data))
                {
                    int beginInd = metaData.IndexOf(data) - left.Length; // Get the left index.
                    if (beginInd >= 0)
                    {
                        int endInd = metaData.IndexOf(",", beginInd + data.Length); // Get the right index.
                        string endData = "";
                        if (endInd > 0)
                        {
                            endData = metaData.Substring(beginInd, endInd - beginInd);
                        }
                        else
                        {
                            endData = metaData.Substring(beginInd);
                        }
                        if (!string.IsNullOrEmpty(endData))
                        {
                            dataList.Add(endData);
                            metaData = metaData.Replace(endData, "");
                            continue;
                        }
                    }
                }
                break;
            }
            return true;
        }

        // Get value "5" from "{...,"ID":"5""...}" by key "ID".
        public static string GetPairValue(string metaData, string key)
        {
            string value = "";
            int ind = metaData.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (0 < ind)
            {
                int beginInd = metaData.IndexOf("\"", ind + key.Length);
                if (0 < beginInd)
                {
                    int endInd = metaData.IndexOf("\"", beginInd + 1);
                    if (0 < endInd)
                    {
                        value = metaData.Substring(beginInd + 1, endInd - beginInd - 1);
                    }
                }
            }

            return value;
        }
    }


    public class ResponseFilters
    {
        private static IDictionary<int, KeyValuePair<HttpResponse, ResponseFilter>> ResponseFilterPool = new SortedList<int, KeyValuePair<HttpResponse, ResponseFilter>>();
        private static object syncRoot = new Object();

        public ResponseFilters()
        {

        }

        public static ResponseFilter Current(HttpResponse response)
        {
            if (ResponseFilterPool == null)
                ResponseFilterPool = new SortedList<int, KeyValuePair<HttpResponse, ResponseFilter>>();
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (ResponseFilterPool.ContainsKey(ssid) && ResponseFilterPool[ssid].Key.Equals(response))
                return ResponseFilterPool[ssid].Value;
            else
            {
                ResponseFilter newResponseFilter = new ResponseFilter(response);
                response.Filter = newResponseFilter; // Replace the response filter.
                lock (syncRoot)
                {
                    KeyValuePair<HttpResponse, ResponseFilter> keyValue = new KeyValuePair<HttpResponse, ResponseFilter>(response, newResponseFilter);
                    ResponseFilterPool[ssid] = keyValue;
                    return newResponseFilter;
                }
            }
        }

        public static void ClearCurrent()
        {
            int ssid = Thread.CurrentThread.ManagedThreadId;
            if (ResponseFilterPool.ContainsKey(ssid))
            {
                ResponseFilterPool.Remove(ssid);
            }
        }
    }

    public class ResponseFilter : FilterBase
    {
        private static string NavigationRest = "/navigation/MenuState";
        private HttpResponse m_response;
        private List<FilterType> m_listFilterTypes;
        private List<byte> responseBuilder;
        private Encoding m_encoding;
        private bool m_bWrited;

        private PageFilterType m_pageType;
        public PageFilterType PageType
        {
            set { m_pageType = value; }
        }
        private HttpRequest m_request;
        public HttpRequest Request
        {
            set { m_request = value; }
        }
        private SPWeb m_web;
        public SPWeb Web
        {
            set { m_web = value; }
        }
        private SPList m_list;
        public SPList List
        {
            set { m_list = value; }
        }
        private object m_evalObj;
        public object EvalObj
        {
            set { m_evalObj = value; }
        }
        private string m_key;
        public string Key
        {
            set { m_key = value; }
        }
        private string m_remoteAddr;
        public string RemoteAddr
        {
            set { m_remoteAddr = value; }
        }
        private string m_pageName;
        public string PageName
        {
            set { m_pageName = value; }
        }
        private RestAPiEvaluation m_restApiEval;
        public RestAPiEvaluation RestApiEval
        {
            set { m_restApiEval = value; }
        }
        private AuthorEvaluation m_authorEval;
        public AuthorEvaluation AuthorEval
        {
            set { m_authorEval = value; }
        }
        private SpServiceType m_serviceType;
        public SpServiceType ServiceType
        {
            set { m_serviceType = value; }
        }

        public ResponseFilter(HttpResponse response)
        {
            m_response = response;
            m_encoding = response.ContentEncoding != null ? response.ContentEncoding : System.Text.UTF8Encoding.UTF8;
            responseStream = response.Filter; // store the old stream.
            m_listFilterTypes = new List<FilterType>();
            responseBuilder = new List<byte>();
            m_pageType = PageFilterType.Unknown;
            m_bWrited = false;
        }

        public void AddFilterType(FilterType filterType)
        {
            if (!m_listFilterTypes.Contains(filterType))
            {
                m_listFilterTypes.Add(filterType);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] repsonseBuffer = new byte[count];
            Array.ConstrainedCopy(buffer, offset, repsonseBuffer, 0, count);
            responseBuilder.AddRange(repsonseBuffer);

            // Case: "_api/navigation/MenuState", do it in write method.
            if (m_listFilterTypes.Contains(FilterType.RestApiTrimmer) && m_request != null && !string.IsNullOrEmpty(m_request.RawUrl)
                && -1 != m_request.RawUrl.IndexOf(NavigationRest, StringComparison.OrdinalIgnoreCase))
            {
                byte[] metaData = responseBuilder.ToArray();
                string strResponse = m_encoding.GetString(metaData);
                if (strResponse.EndsWith("}") && TrimmerGlobal.CheckJsonFormat(strResponse))
                {
                    RestApiTrimmer RestApiTrimmer = new RestApiTrimmer(m_response, m_restApiEval);
                    string strFinal = RestApiTrimmer.Run(strResponse);
                    byte[] data = m_encoding.GetBytes(strFinal);
                    m_response.Filter = responseStream;
                    responseStream.Write(data, 0, data.Length);
                    m_bWrited = true;
                }
            }
        }

        public override void Close()
        {
            if (m_bWrited)
            {
                // Ignore "_api/navigation/MenuState" in this, do it in write method.
                base.Close();
                return;
            }

            byte[] metaData = responseBuilder.ToArray();
            if (metaData.Length > 0)
            {
                try
                {
                    string strResponse = m_encoding.GetString(metaData);
                    if (!string.IsNullOrEmpty(strResponse))
                    {
                        string strFinal = Run(strResponse);
                        if (!strResponse.Equals(strFinal))
                        {
                            byte[] data = m_encoding.GetBytes(strFinal);
                            m_response.Filter = responseStream;
                            responseStream.Write(data, 0, data.Length);
                            base.Close();
                            return;
                        }
                    }
                }
                catch
                {
                }
                m_response.Filter = responseStream;
                responseStream.Write(metaData, 0, metaData.Length);
            }
            base.Close();
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            foreach (FilterType filterType in m_listFilterTypes)
            {
                try
                {
                    switch (filterType)
                    {
                        case FilterType.AuthorTrimmer:
                            AuthorTrimmer AuthorTrimmer = new AuthorTrimmer(m_response, m_authorEval);
                            strFinal = AuthorTrimmer.Run(strFinal);
                            break;
                        case FilterType.EditorTrimmer:
                            EditorTrimmer EditorTrimmer = new EditorTrimmer(m_request, m_response, m_web);
                            strFinal = EditorTrimmer.Run(strFinal);
                            break;
                        case FilterType.CsomTrimmer:
                            CsomTrimmer CsomTrimmer = new CsomTrimmer(m_web);
                            strFinal = CsomTrimmer.Run(strFinal);
                            break;
                        case FilterType.FieldIdTrimmer:
                            FieldIdTrimmer fieldFilter = new FieldIdTrimmer(m_key);
                            strFinal = fieldFilter.Run(strFinal);
                            break;
                        case FilterType.MobileTrimmer:
                            MobileTrimmer MobileTrimmer = new MobileTrimmer(m_request, m_response, m_web);
                            strFinal = MobileTrimmer.Run(strFinal);
                            break;
                        case FilterType.PostTrimmer:
                            PostTrimmer PostTrimmer = new PostTrimmer(m_web);
                            strFinal = PostTrimmer.Run(strFinal);
                            break;
                        case FilterType.RestApiTrimmer:
                            RestApiTrimmer RestApiTrimmer = new RestApiTrimmer(m_response, m_restApiEval);
                            strFinal = RestApiTrimmer.Run(strFinal);
                            break;
                        case FilterType.SearchTrimmer:
                            SearchTrimmer SearchTrimmer = new SearchTrimmer(m_pageName);
                            strFinal = SearchTrimmer.Run(strFinal);
                            break;
                        case FilterType.SoapTrimmer:
                            SoapTrimmer SoapTrimmer = new SoapTrimmer(m_response, m_web, m_evalObj);
                            strFinal = SoapTrimmer.Run(strFinal);
                            break;
                        case FilterType.SPServiceTrimmer:
                            SPServiceTrimmer serviceFilter = new SPServiceTrimmer(m_response, m_web, m_remoteAddr, m_list, m_serviceType);
                            strFinal = serviceFilter.Run(strFinal);
                            break;
                        case FilterType.PageTrimmer:
                            PageTrimmer pageFilter = new PageTrimmer(m_web, m_list, m_remoteAddr, m_pageType);
                            strFinal = pageFilter.Run(strFinal);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during ResponseFilter:", null, ex);
                }
            }
            return strFinal;
        }
    }
}
