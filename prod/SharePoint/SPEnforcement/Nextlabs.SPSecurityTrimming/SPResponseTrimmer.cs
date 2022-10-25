using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.Common;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using NextLabs.Diagnostic;

namespace Nextlabs.SPSecurityTrimming
{
    public class SearchTrimmer
    {
        private string m_pagename;

        public SearchTrimmer(string pageName)
        {
            m_pagename = pageName;
        }

        public string Run(string strInput)
        {
            string strFinal = strInput;
            if (-1 != strInput.IndexOf("</html>", StringComparison.OrdinalIgnoreCase))
            {
                SPResponseFilterTrimmer _ResponseFilterTrimmer = new SPResponseFilterTrimmer();
                _ResponseFilterTrimmer.PageName = m_pagename;
                _ResponseFilterTrimmer.DoTrimming(ref strFinal);
            }
            return strFinal;
        }
    }

    public class SPResponseFilterTrimmer : ITrimmer
    {
        static private FileSystemWatcher g_Configwatcher = null;
        static private List<string> g_filters = null;
        static private bool g_output = false;
        static private List<string> g_resultfilters = null;
        static private string g_enforcemessage = null;
        static private string g_noitemmessage = null;
        private string m_pagename = null;

        public string PageName
        {
            get { return m_pagename; }
            set { m_pagename = value; }
        }

        public SPResponseFilterTrimmer()
        {
            m_action = CETYPE.CEAction.Read;
            if (g_filters == null)
                FileWatchList();
        }

        private CETYPE.CEAction m_action;
        public void ActionType(CETYPE.CEAction action)
        {
            m_action = action;
        }

        private static void LoadCofig()
        {
            if (g_filters == null)
            {
                g_filters = new List<string>();
            }
            else
            {
                g_filters.Clear();
            }
            if (g_resultfilters == null)
            {
                g_resultfilters = new List<string>();
            }
            else
            {
                g_resultfilters.Clear();
            }
            string _filepath = Globals.GetSPEPath();
            _filepath += "config\\FastSearch.filter";
            using (FileStream fs = new FileStream(_filepath, FileMode.Open, FileAccess.Read))
            {
                string _spFilecontent = null;
                if (fs != null)
                {
                    byte[] _filecontent = new byte[fs.Length];
                    fs.Read(_filecontent, 0, (int)fs.Length);
                    _spFilecontent = System.Text.Encoding.ASCII.GetString(_filecontent);
                    fs.Close();
                    using (Stream InputStream = new MemoryStream(_filecontent))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(InputStream);
                        XmlNodeList nodes = doc.DocumentElement.SelectNodes("SearchPage");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            if (nodes[i].ChildNodes[0].NodeType == XmlNodeType.Text)
                            {
                                String _xmltxt = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
                                _xmltxt += nodes[i].OuterXml;
                                byte[] InputBuffer = Encoding.Default.GetBytes(_xmltxt);
                                using (Stream _InputStream = new MemoryStream(InputBuffer))
                                {
                                    XmlDocument _doc = new XmlDocument();
                                    _doc.Load(_InputStream);
                                    string _pagename = nodes[i].ChildNodes[0].Value;
                                    XmlNodeList RegularNode = _doc.DocumentElement.GetElementsByTagName("PageRegular");
                                    if (RegularNode != null && RegularNode.Count > 0)
                                    {
                                        for (int j = 0; j < RegularNode.Count; j++)
                                        {
                                            if (RegularNode[j].FirstChild.NodeType == XmlNodeType.Text)
                                            {
                                                string _pageregular = RegularNode[j].FirstChild.Value;
                                                _pageregular = _pageregular.Replace("(", "<");
                                                _pageregular = _pageregular.Replace(")", ">");
                                                g_filters.Add(_pagename + " " + _pageregular);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        XmlNodeList nodes1 = doc.DocumentElement.SelectNodes("OutputResponse");
                        if (nodes1.Count > 0 && nodes1[0].ChildNodes[0].NodeType == XmlNodeType.Text)
                        {
                            string _node = nodes1[0].ChildNodes[0].Value;
                            if (_node.Equals("true"))
                                g_output = true;
                        }
                        XmlNodeList nodes2 = doc.DocumentElement.SelectNodes("ResultFilter");
                        if (nodes2.Count > 0 && nodes2[0].ChildNodes[0].NodeType == XmlNodeType.Text)
                        {
                            for (int i = 0; i < nodes2.Count; i++)
                            {
                                if (nodes2[i].ChildNodes[0].NodeType == XmlNodeType.Text)
                                {
                                    string _resultfilter = nodes2[0].ChildNodes[0].Value;
                                    _resultfilter = _resultfilter.Replace("(", "<");
                                    _resultfilter = _resultfilter.Replace(")", ">");
                                    g_resultfilters.Add(_resultfilter);
                                }
                            }
                        }
                        XmlNodeList nodes3 = doc.DocumentElement.SelectNodes("EnforceMessage");
                        if (nodes3.Count > 0 && nodes3[0].ChildNodes[0].NodeType == XmlNodeType.Text)
                        {
                            g_enforcemessage = nodes3[0].ChildNodes[0].Value;
                            g_enforcemessage = g_enforcemessage.Replace("(", "<");
                            g_enforcemessage = g_enforcemessage.Replace(")", ">");
                        }
                        XmlNodeList nodes4 = doc.DocumentElement.SelectNodes("NoItemMessage");
                        if (nodes4.Count > 0 && nodes4[0].ChildNodes[0].NodeType == XmlNodeType.Text)
                        {
                            g_noitemmessage = nodes4[0].ChildNodes[0].Value;
                            g_noitemmessage = g_noitemmessage.Replace("(", "<");
                            g_noitemmessage = g_noitemmessage.Replace(")", ">");
                        }
                    }
                }
            }
        }

        private static void filterwatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if ((e.ChangeType == WatcherChangeTypes.Changed
                || e.ChangeType == WatcherChangeTypes.Created)
                && e.Name.Equals("FastSearch.filter", StringComparison.OrdinalIgnoreCase))
                LoadCofig();
        }

        private static void FileWatchList()
        {
            if (g_Configwatcher == null)
                g_Configwatcher = new FileSystemWatcher();
            String _filepath = Globals.GetSPEPath();
            _filepath += "config\\";
            g_Configwatcher.Path = _filepath;

            g_Configwatcher.Filter = "*.filter";
            g_Configwatcher.Changed += new FileSystemEventHandler(filterwatcher_Changed);
            g_Configwatcher.Created += new FileSystemEventHandler(filterwatcher_Changed);
            g_Configwatcher.EnableRaisingEvents = true;
            g_Configwatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastAccess
                                  | NotifyFilters.LastWrite | NotifyFilters.Size;
            LoadCofig();
        }

        public bool DoTrimming()
        {
            return true;
        }

        public void DoTrimming(ref string _response)
        {
            if (g_output)
                OutputResonpseData(_response);
            if (g_filters != null)
            {
                string[] _filters = g_filters.ToArray();
                MatchCollection result_matches = null;
                if (g_resultfilters != null)
                {
                    foreach (string _resultfilter in g_resultfilters)
                    {
                        Regex result_reg = new Regex(_resultfilter);
                        result_matches = result_reg.Matches(_response);
                        if (result_matches != null && result_matches.Count > 0)
                            break;
                    }
                }
                foreach (string _filter in g_filters)
                {
                    int index = _filter.IndexOf(" ");
                    string _flterstring = _filter;
                    string pagename = null;
                    if (index != -1)
                    {

                        pagename = _flterstring.Substring(0, index);
                        _flterstring = _flterstring.Substring(index + 1);
                    }
                    if (m_pagename != null && !string.IsNullOrEmpty(pagename))
                    {
                        if (!m_pagename.Equals(pagename, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!string.IsNullOrEmpty(_flterstring))
                    {
                        Regex reg = new Regex(_flterstring);
                        MatchCollection matches = reg.Matches(_response);
                        if (matches.Count > 0)
                        {
                            string _urlfilter = "href={1}[\\W]*?[\"]{1}[\\s\\S]*?[\"]{1}";
                            Regex reg1 = new Regex(_urlfilter);
                            int _denyresult = 0;
                            foreach (Match _match in matches)
                            {
                                bool allow = true;
                                string _content = _match.ToString();
                                MatchCollection matches1 = reg1.Matches(_content);
                                if (matches1.Count > 0)
                                {
                                    _content = matches1[0].ToString();
                                    _content = _content.Replace("href=\"", "");
                                    _content = _content.Replace("\"", "");
                                    if (!String.IsNullOrEmpty(_content))
                                    {
                                        Object evaTarget = null;
                                        HttpContext _Context = HttpContext.Current;
                                        SPWeb _web = SPControl.GetContextWeb(_Context);
                                        if (_content.StartsWith("http://") || _content.StartsWith("https://"))
                                        {
                                            using (SPHttpUrlParser parser = new SPHttpUrlParser(_content))
                                            {
                                                parser.Parse();
                                                evaTarget = parser.ParsedObject;
                                            }
                                            if (evaTarget != null)
                                            {
                                                string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(evaTarget);
                                                EvaluationBase evaObj = EvaluationFactory.CreateInstance(evaTarget,
                                                        m_action, objUrl, _Context.Request.UserHostAddress, "ResposeFilter Trimmer", _web.CurrentUser);
                                                allow = evaObj.Run();
                                            }
                                            else
                                            {
                                                ExternalLinkEvaluation evaObj = new ExternalLinkEvaluation(m_action, _content,
                                                    _Context.Request.UserHostAddress, _web.CurrentUser.Name, _web.CurrentUser.Sid, "Search Result Trimmer");
                                                allow = evaObj.Run();
                                            }
                                            if (!allow)
                                            {
                                                _response = _response.Replace(_match.ToString(), "");
                                                _denyresult++;
                                            }
                                        }
                                        else if (_content.StartsWith("file://"))
                                        {
                                            FileShareEvaluation evaObj = new FileShareEvaluation(m_action, _content,
                                                _Context.Request.UserHostAddress, _web.CurrentUser.Name, _web.CurrentUser.Sid, "Search Result Trimmer");
                                            allow = evaObj.Run();
                                            if (!allow)
                                            {
                                                _response = _response.Replace(_match.ToString(), "");
                                                _denyresult++;
                                            }
                                        }
                                    }
                                }
                            }
                            if (result_matches != null && result_matches.Count > 0)
                            {
                                string _resultstring = result_matches[0].ToString();
                                if (_denyresult >= matches.Count)
                                {
                                    _resultstring = _resultstring + g_noitemmessage;
                                }
                                else
                                {
                                    _resultstring = _resultstring + g_enforcemessage;
                                }
                                _response = _response.Replace(result_matches[0].ToString(), _resultstring);
                            }
                        }
                    }
                }
            }
        }

        private void OutputResonpseData(string _response)
        {
            FileStream fs = null;
            try
            {
                string _filepath = Globals.GetSPEPath();
                _filepath += "Logs\\OutputResponse.log";
                fs = new FileStream(_filepath, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
                _response += "\r\n";
                byte[] _bytes = System.Text.UTF8Encoding.UTF8.GetBytes(_response);
                if (fs != null && fs.Length <= 1024 * 1024 * 3)
                {
                    fs.Write(_bytes, 0, _bytes.Length);
                    fs.Close();
                    fs = null;
                }
                else if (fs != null)
                {
                    fs.Close();
                    fs = new FileStream(_filepath, FileMode.CreateNew | FileMode.Truncate, FileAccess.Write);
                    if (fs != null)
                    {
                        fs.Write(_bytes, 0, _bytes.Length);
                        fs.Close();
                        fs = null;
                    }
                }
            }
            catch
            {
                if (fs != null)
                    fs.Close();
            }
        }

    }

    public class PageTrimmer
    {
        private SPWeb m_web;
        private SPList m_list;
        private string m_remoteAddr;
        PageFilterType m_pageType;
        public PageTrimmer(SPWeb web, SPList list, string remoteAddr, PageFilterType pageType)
        {
            m_web = web;
            m_list = list;
            m_remoteAddr = remoteAddr;
            m_pageType = pageType;
        }

        public string Run(string strInput)
        {
            string finalJson = strInput;
            using (SPSecurityTrimmingManager manager = new SPSecurityTrimmingManager(m_web.Site))
            {
                bool bSecrityTrimming = manager.CheckSecurityTrimming();
                if (bSecrityTrimming)
                {
                    string strBegin = "\"quickLaunch\":";
                    string key = "\"url\":";
                    finalJson = DataTrimming(strInput, strBegin, key);
                    strBegin = "\"topNav\":";
                    finalJson = DataTrimming(finalJson, strBegin, key);
#if SP2019
                    if (-1 != finalJson.IndexOf("_spNewsData=", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trimming for home page news data.
                        strBegin = "_spNewsData=";
                        key = "\"AbsoluteUrl\":";
                        finalJson = DataTrimming(finalJson, strBegin, key);
                    }
#endif
                    SPEEvalAttr evalAttr = SPEEvalAttrs.Current();
                    if (m_pageType == PageFilterType.ViewEdit)
                    {
                        finalJson = RemoveRssImg(finalJson);
                    }
                    else if (m_pageType == PageFilterType.Tasks)
                    {
                        strBegin = "\"Row\" :";
                        key = "\"id\":";
                        finalJson = DataTrimming(finalJson, strBegin, key);
                    }
                }
            }
            return finalJson;
        }

        public string RemoveRssImg(string strInput)
        {
            string strBegin = "<a";
            string strEnd = "</a>";
            string finalJson = strInput;
            int ind = strInput.IndexOf("images/rss.gif", StringComparison.OrdinalIgnoreCase);
            if (-1 != ind)
            {
                string strLeft = strInput.Substring(0, ind);
                int indBegin = strLeft.LastIndexOf(strBegin, StringComparison.OrdinalIgnoreCase);
                int indEnd = strInput.IndexOf(strEnd, ind, StringComparison.OrdinalIgnoreCase);
                finalJson = strLeft.Substring(0, indBegin) + strInput.Substring(indEnd + strEnd.Length);
            }
            return finalJson;
        }


        public string DataTrimming(string jsonText, string strBegin, string key)
        {
            string finalJson = jsonText;
            bool bDeny = false;
            try
            {
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
                        PageEvaluation pageEvaluator = new PageEvaluation(m_web, m_list, m_remoteAddr, m_pageType);
                        pageEvaluator.Run(listNodeData);
                        List<string> dataList = new List<string>();
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
                            string endData = string.Join(",", dataList.ToArray());
                            finalJson = finalJson.Replace(metaData, endData);
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

        private void JsonSplit(string metaData, List<TrimNodeData> listNodeData, string key)
        {
            string endData = metaData;
            List<string> datas = new List<string>();
            string strBegin = "\"Children\":";
            string strEmptyBegin = "\"Children\":[]";
            string navKey = "\"url\":";
            TrimmerGlobal.SplitMetaData("{", "}", metaData, datas);
            foreach (string data in datas)
            {
                if (-1 != data.IndexOf(strBegin, StringComparison.OrdinalIgnoreCase) && -1 == data.IndexOf(strEmptyBegin, StringComparison.OrdinalIgnoreCase))
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
    }

    public class PageEvaluation
    {
        private SPWeb m_web;
        private SPList m_list;
        private string m_remoteAddr;
        private string m_userId;
        PageFilterType m_pageType;
        public PageEvaluation(SPWeb web, SPList list, string remoteAddr, PageFilterType pageType)
        {
            m_web = web;
            m_list = list;
            m_remoteAddr = remoteAddr;
            m_userId = m_web.CurrentUser.LoginName;
            m_pageType = pageType;
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
                NLLogger.OutputLog(LogLevel.Debug, "Exception during PageEvaluation:", null, ex);
            }
            return true;
        }

        public void SetEvalAttrBytrimId(TrimNodeData nodeData, EvaluationMultiple mulEval)
        {
            try
            {
                object obj = null;
                string trimId = nodeData.trimId;
                if (m_web != null && !string.IsNullOrEmpty(trimId))
                {
                    if (m_pageType == PageFilterType.Unknown)
                    {
                        string fullUrl = trimId.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? trimId : m_web.Site.MakeFullUrl(trimId);
                        using (SPHttpUrlParser parser = new SPHttpUrlParser(fullUrl))
                        {
                            parser.Parse();
                            obj = parser.ParsedObject;
                        }
                    }
                    else if (m_pageType == PageFilterType.Tasks)
                    {
                        if (m_list != null)
                        {
                            obj = m_list.GetItemById(int.Parse(trimId));
                        }
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
}
