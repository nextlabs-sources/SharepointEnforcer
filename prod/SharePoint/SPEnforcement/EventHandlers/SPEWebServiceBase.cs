using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using NextLabs.Common;
using Microsoft.SharePoint;
using System.Diagnostics;
namespace NextLabs.SPE.WebSvcEntitlement
{
    public class SPEWebSvcResBuilder
    {
        public Method m_WebServiceKey;
        public XmlDocument m_Document;
        public XmlNamespaceManager m_xmlmgr;
        public string JsonContentString=string.Empty;
        public void Init(Method _WebServiceKey, XmlDocument _Document, XmlNamespaceManager _xmlmgr)
        {
            m_WebServiceKey = _WebServiceKey;
            m_Document = _Document;
            m_xmlmgr = _xmlmgr;
        }
        public void InitJson(Method _WebServiceKey, string JsonContentString)
        {
            m_WebServiceKey = _WebServiceKey;
            this.JsonContentString = JsonContentString;
        }
        public virtual String BuildResourceString()
        {
            return null;
        }
        public virtual String GetResType() { return null; }
    }
    public class SPEWebSvcResBuildFromAttribute : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                int index = _urlkey.IndexOf("=");
                if (index != -1)
                {
                    String _urlkey1 = _urlkey.Substring(index + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_urlkey1, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {//Two url style refer to a resource depends it is list or doc lib.
                     // List:       http://nico-w08/_layouts/listform.aspx?PageType=4&amp;ListId=%7B5F09ABC0-6A9C-403C-B360-FDD33DC1BAFD%7D&amp;ID=2
                     // Doc lib:    http://nico-w08/Shared%20Documents/Blue%20hills.jpg
                        string strUrl = null;
                        if (_nodelist[0].Value != null)
                        {
                            strUrl= _nodelist[0].Value;
                        }
                        else 
                        {
                            strUrl= _nodelist[0].InnerText.ToString();
                        }
                        //TODO:
                        strUrl = Globals.UrlDecode(strUrl);
                        if (strUrl.IndexOf(".aspx?",StringComparison.OrdinalIgnoreCase) ==-1)
                            return strUrl;
                        SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                        string strListGuid=null;
                        int iPosListId = strUrl.IndexOf("ListId=", StringComparison.OrdinalIgnoreCase);
                        if (iPosListId != -1)
                        {
                            string strListId=strUrl.Substring(iPosListId+7);
                            int iEndPosListId=strListId.IndexOf("&");
                            if(iEndPosListId==-1)
                                strListGuid=strListId;
                            else
                            {
                                strListGuid=strListId.Substring(0,iEndPosListId);
                            }
                            strListGuid = Globals.UrlDecode(strListGuid);
                        }
                        else
                            return strUrl;
                        
                        string strItemId=null;
                        int iPosItemId=strUrl.IndexOf("&ID=",StringComparison.OrdinalIgnoreCase);
                        if(iPosItemId!=-1)
                        {
                            strItemId=strUrl.Substring(iPosItemId+4);
                            int iEndPosItemId=strItemId.IndexOf("&");
                            if(iEndPosItemId!=-1)
                                strItemId=strItemId.Substring(0,iEndPosItemId);
                        }
                        else
                            return strUrl;
                        SPWeb spWeb = _SPEEvalAttr.WebObj;
                        //uriRes.Query
                        SPList spList = _SPEEvalAttr.WebObj.Lists[new Guid(strListGuid)];
                        if(spList==null)
                            return strUrl;
                        
                        strUrl = Globals.ConstructListUrl(spWeb, spList);
                        SPListItem spItem = spList.GetItemById(Convert.ToInt32(strItemId));
                        if (spItem != null)
                        {
                            strUrl = strUrl + "/" + spItem.Title;
                            _SPEEvalAttr.ItemObj = spItem;
                        }
                        return strUrl;

                    }
                    else
                    {
                        //for bug:30914
                        //for document-->send to <?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><CopyIntoItemsLocal xmlns="http://schemas.microsoft.com/sharepoint/soap/"><SourceUrl>http://pf1-w08-sps08/denyedit/2.txt</SourceUrl><DestinationUrls><string>http://pf1-w08-sps08/11/2.txt</string></DestinationUrls></CopyIntoItemsLocal></soap:Body></soap:Envelope>
                        try
                        {
                            XmlNode node = m_Document.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0];
                            return node.InnerText;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFromSiteRelativePath : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            SPWeb spWeb = _SPEEvalAttr.WebObj;
            if (spWeb == null)
                return null;
            string siteUrl = spWeb.Site.Url;
            Uri uriSite = new Uri(siteUrl);
            string strHostUrl = null;
            if (uriSite.Port == 80)
                strHostUrl = uriSite.Scheme + "://" + uriSite.Host;
            else
                strHostUrl = uriSite.Scheme + "://" + uriSite.Host + ":" + uriSite.Port;

            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                int index = _urlkey.IndexOf("=");
                if (index != -1)
                {
                    String _urlkey1 = _urlkey.Substring(index + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_urlkey1, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        if (_nodelist[0].Value != null)
                        {
                            return strHostUrl + _nodelist[0].Value;
                        }
                        else
                        {
                            return strHostUrl + _nodelist[0].InnerText.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildForImageLib : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = _urlkey.Split(new String[] { " " }, StringSplitOptions.None);
                String url1 = null;
                String url2 = null;
                int index1 = _Words[0].IndexOf("=");
                if (index1 != -1)
                {
                    String _url1key = _Words[0].Substring(index1 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                int index2 = _Words[1].IndexOf("=");
                if (index2 != -1)
                {
                    String _url2key = _Words[1].Substring(index2 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url2key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url2 = _nodelist[0].InnerText;
                    }
                }
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                String url = _SPEEvalAttr.WebObj.Url + "/" + url1 + "/" + url2;
                return url;
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildForListGuid : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = _urlkey.Split(new String[] { " " }, StringSplitOptions.None);
                String url1 = null;
                int index1 = _Words[0].IndexOf("=");
                if (index1 != -1)
                {
                    String _url1key = _Words[0].Substring(index1 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                string GUID = url1.Replace("{","");
                GUID = GUID.Replace("}", "");
           
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                SPList List = null;

                try
                {
                    Guid guidList =  new Guid(GUID);
                    List = _SPEEvalAttr.WebObj.Lists[guidList];
                }
                catch
                {
                    List = _SPEEvalAttr.WebObj.Lists[GUID];
                }        
                String url = _SPEEvalAttr.ObjEvalUrl = Globals.ConstructListUrl(_SPEEvalAttr.WebObj, List);
				

                //sometimes, GeListItems may refer to a folder/document set--mobile office
                //ios
                string strFolder = null; 
                XmlNodeList _nodelistFolders = m_Document.DocumentElement.GetElementsByTagName("Folder");
                if (_nodelistFolders.Count > 0)
                {
                    strFolder = _nodelistFolders[0].FirstChild.Value;
                }
                
                if (string.IsNullOrEmpty(strFolder))//android
                {
                    _nodelistFolders = m_Document.DocumentElement.GetElementsByTagName("Value");
                    foreach (XmlNode node in _nodelistFolders)
                    {      
                        if (node.InnerText.StartsWith(List.Title + "/"))
                        {
                            strFolder = node.InnerText; 
                            break;
                        }      
                    }
                }

                if (!string.IsNullOrEmpty(strFolder))
                {
                    url = _SPEEvalAttr.WebObj.Url + "/" + strFolder;
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                }

                return url;
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFrom3UrlComponents : SPEWebSvcResBuilder 
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = _urlkey.Split(new String[] { " " }, StringSplitOptions.None);
                //String url1 = null;
                String url2 = null;
                String url3 = null;
                int index1 = _Words[0].IndexOf("=");
                if (index1 != -1)
                {
                    String _url1key = _Words[0].Substring(index1 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        //url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                int index2 = _Words[1].IndexOf("=");
                if (index2 != -1)
                {
                    String _url2key = _Words[1].Substring(index2 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url2key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url2 = _nodelist[0].FirstChild.Value.ToLower();
                        url2 = url2.Replace("{", "");
                        url2 = url2.Replace("}", "");
                    }
                }

                 int index3 = _Words[2].IndexOf("=");
                if (index3 != -1)
                {
                    String _url3key = _Words[2].Substring(index3 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url3key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url3 = _nodelist[0].FirstChild.Value;
                    }
                }
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                SPWeb mSPWeb=_SPEEvalAttr.WebObj;
                string WebUrl = _SPEEvalAttr.WebUrl;
                SPList mSPList=mSPWeb.Lists[new Guid(url2)];

				SPListItem lItem = null;
                try
                {             
                    lItem = mSPList.GetItemById(int.Parse(url3));
					if (lItem != null)
					{
	                    return  (WebUrl.EndsWith("/") ? WebUrl : WebUrl + "/") + lItem.Url;
					}					
                }
                catch
                {
				}
				
				if (lItem == null)
				{
                    foreach(SPListItem msplistitem in mSPList.Items)
                    {
                        if (msplistitem.ID.ToString() == url3)
                        {
                          return    WebUrl+"/"+msplistitem.Url.ToString();
                        }
                    }
                }


            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFromListName : SPEWebSvcResBuilder
    {

        public override String BuildResourceString()
        {

            String _urlkey = m_WebServiceKey.urlkey;

            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String url2 = null;
                int index2 = _urlkey.IndexOf("=");
                if (index2 != -1)
                {
                    String _url2key = _urlkey.Substring(index2 + 1);

                    XmlNodeList _nodelist = m_Document.SelectNodes(_url2key, m_xmlmgr);

                    if (_nodelist.Count > 0)
                    {

                        url2 = _nodelist[0].FirstChild.Value.ToLower();

                        url2 = url2.Replace("{", "");

                        url2 = url2.Replace("}", "");

                    }

                }
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();

                SPWeb mSPWeb = _SPEEvalAttr.WebObj;

                string WebUrl = _SPEEvalAttr.WebUrl;

                SPList mSPList = mSPWeb.Lists[new Guid(url2)];

                return WebUrl +"/Lists"+ "/" + mSPList.Title;
            }
            return null;
        
        }

    }
    public class SPEWebSvcResBuildForItemGuidUrl : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = _urlkey.Split(new String[] { " " }, StringSplitOptions.None);
                String url1 = null;
                String url2 = null;
                int index1 = _Words[0].IndexOf("=");
                if (index1 != -1)
                {
                    String _url1key = _Words[0].Substring(index1 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                int index2 = _Words[1].IndexOf("=");
                if (index2 != -1)
                {
                    String _url2key = _Words[1].Substring(index2 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url2key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url2 = _nodelist[0].FirstChild.Value;
                    }
                }

                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                SPWeb mSPWeb = _SPEEvalAttr.WebObj;
                string WebUrl = _SPEEvalAttr.WebUrl;
                SPList mSPList = mSPWeb.Lists[new Guid(url1)];
                foreach (SPListItem msplistitem in mSPList.Items)
                {
                    if (msplistitem.ID.ToString() == url2)
                    {
                        return WebUrl + "/" + msplistitem.Url.ToString();
                    }
                }
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFromJSON : SPEWebSvcResBuilder
     {
        public override String BuildResourceString()
         {
             String _urlkey = m_WebServiceKey.urlkey;
             if (!string.IsNullOrEmpty(_urlkey) && !string.IsNullOrEmpty(JsonContentString))
             {
                 string guidValue = "";
                 string splistitemid = "";
                 JsonContentString=JsonContentString.Replace("{", "");
                 JsonContentString = JsonContentString.Replace("}", "");
                 string[] First = JsonContentString.ToString().Split(',');
                 for (int i = 0;i<First.Length; i++)
                 {
                     string[] result = First[i].Split(':');
                     if (result[0] == "\""+_urlkey+"\"") 
                     {
                         guidValue = result[1];
                         break;
                     };
                 }
                 if (guidValue != null)
                 {
                     string[] STR = guidValue.Split('&');
                     for (int i = 0; i < STR.Length; i++) 
                     {
                         if (guidValue.IndexOf("d=F") > 0)
                         {
                             string[] urlstring = guidValue.Split('m');
                             splistitemid = urlstring[urlstring.Length - 2];
                             break;
                         }
                     }
                     SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                     SPFile mSPFile=_SPEEvalAttr.WebObj.GetFile(new Guid(splistitemid));
                     return _SPEEvalAttr.WebObj.Url.ToString() +"/"+ mSPFile.Url.ToString();
                 }
                 return null;
             }
             return null;
         }

     }

    public class SPEWebSvcResBuildForClientSvcFromIdentity : SPEWebSvcResBuilder 
    {
        string strObjType = null;
        public override string GetResType()
        {
            return strObjType;
        }
        public override String BuildResourceString()
        {
            String strUrlKey = m_WebServiceKey.urlkey;
            String strReturnUrl = null;
            if (strUrlKey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = strUrlKey.Split(new String[] { " " }, StringSplitOptions.None);
                String url1 = null;
                int iPos = _Words[0].IndexOf("=");
                if (iPos != -1)
                {
                    String _url1key = _Words[0].Substring(iPos + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                int iPosWeb = url1.IndexOf("web:", StringComparison.OrdinalIgnoreCase);
                int iPosList = url1.IndexOf("list:", StringComparison.OrdinalIgnoreCase);
                int iPosFile = url1.IndexOf("file:", StringComparison.OrdinalIgnoreCase);
                int iPosFolder = url1.IndexOf("folder:", StringComparison.OrdinalIgnoreCase);
                string guidWeb = null, guidList = null, strFileUrl = null, guidFolder = null;
                if (iPosWeb != -1)
                    guidWeb = url1.Substring(iPosWeb + 4, 36);
                if (iPosList != -1)
                    guidList = url1.Substring(iPosList + 5, 36);
                if (iPosFile != -1)
                    strFileUrl = url1.Substring(iPosFile + 5);
                // Add by George, for bug 30218 with library type.
                else if (iPosFolder != -1)
                    guidFolder = url1.Substring(iPosFolder + 7, 36);
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                SPWeb spWeb = _SPEEvalAttr.WebObj;
                if (spWeb!=null&&spWeb.ID.ToString().ToLower() == guidWeb.ToLower())
                {
                    strObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                    strReturnUrl = spWeb.Url.ToString();
                }
                else
                {
                    SPSite mSPSite = _SPEEvalAttr.SiteObj;
                    if (mSPSite == null)
                        mSPSite = _SPEEvalAttr.WebObj.Site;
                    if (mSPSite != null)
                    {
                     //modify by roy  avoid native memory leak
                        using (SPWeb urlWeb = mSPSite.OpenWeb())
                        {
                            foreach (SPWeb mspweb in urlWeb.Webs)
                            {
                                try
                                {
                                    if (mspweb.ID.ToString().ToLower() == guidWeb.ToLower())
                                    {
                                        strObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;
                                        strReturnUrl = mspweb.Url.ToString();
                                        break;
                                    }
                                }
                                finally
                                {
                                    if (mspweb != null)
                                        mspweb.Dispose();
                                }
                            }
                         } // SPWeb object urlWeb.Dispose() automatically called.

                    }
                    else
                        return null;
                }
                if (iPosList != -1)
                {
                    SPList spList = null;
                    try
                    {
                       Guid guid = new Guid(guidList);
                       spList = spWeb.Lists[guid];
                    }
                    catch
                    {
                        try
                        {
                            spList = spWeb.Lists[guidList]; // get list item by name
                        }
                        catch
                        { 
                        }
                    }
                    if (spList != null)
                    {
                        strObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                        strReturnUrl = Globals.ConstructListUrl(spWeb, spList);
                    }
                }
                if (iPosFile != -1)
                {
                    string strSiteUrl = spWeb.Site.Url.ToString();
                    Uri uriSite = new Uri(strSiteUrl);
                    string strHostUrl = null;
                    if(uriSite.Port==80)
                        strHostUrl=uriSite.Scheme + "://" + uriSite.Host;
                    else
                        strHostUrl=uriSite.Scheme + "://" + uriSite.Host+":"+uriSite.Port;
                    strReturnUrl = strHostUrl + strFileUrl;
                    strObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                }
                else if (iPosFolder != -1)
                {
                    Guid guid = new Guid(guidFolder);
                    SPFolder folder = spWeb.GetFolder(guid);
                    string folerUrl = folder.Url.StartsWith("/") ? folder.Url : "/" + folder.Url;
                    strReturnUrl = spWeb.Url + folerUrl;
                    strObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                }
                return strReturnUrl;
            }
            return null;
        }
    }

    public class SPEGuIDUpdateListUrl : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                String[] _Words = _urlkey.Split(new String[] { " " }, StringSplitOptions.None);
                String url1 = null;
                int index1 = _Words[0].IndexOf("=");
                if (index1 != -1)
                {
                    String _url1key = _Words[0].Substring(index1 + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_url1key, m_xmlmgr);
                    if (_nodelist.Count > 0)
                    {
                        url1 = _nodelist[0].FirstChild.Value;
                    }
                }
                int index = url1.IndexOf("list:", StringComparison.OrdinalIgnoreCase);
                string guid=null;
                if(index!=-1)
                    guid = url1.Substring(index + 5, 36);
                else
                {
                    index = url1.IndexOf(":web:", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                        guid = url1.Substring(index + 5, 36);
                }
                if (guid == null)
                    return null;
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                SPWeb mSPWeb = _SPEEvalAttr.WebObj;
                SPList mSPList = mSPWeb.Lists[new Guid(guid)];
                return  Globals.ConstructListUrl(_SPEEvalAttr.WebObj, mSPList);
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFromWebObj : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            String _urlkey = m_WebServiceKey.urlkey;
            if (_urlkey != null && m_WebServiceKey != null && m_Document != null)
            {
                if (_urlkey == "##")
                {
                    SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
                    SPWeb mSPWeb = _SPEEvalAttr.WebObj;
                    return mSPWeb.Url.ToString();
                }
            }
            return null;
        }
    }

    public class SPEWebSvcResBuildFromUrl : SPEWebSvcResBuilder
    {
        public override String BuildResourceString()
        {
            if (m_WebServiceKey != null && m_WebServiceKey.urlkey != null && m_Document != null)
            {
                String _urlkey = m_WebServiceKey.urlkey;
                int index = _urlkey.IndexOf("=");
                if (index != -1)
                {
                    String _urlkey1 = _urlkey.Substring(index + 1);
                    XmlNodeList _nodelist = m_Document.SelectNodes(_urlkey1, m_xmlmgr);
                    if (_nodelist != null && _nodelist.Count > 0)
                    {
                        return _nodelist[0].FirstChild.Value;
                    }
                }
            }
            return null;
        }
    }
}
