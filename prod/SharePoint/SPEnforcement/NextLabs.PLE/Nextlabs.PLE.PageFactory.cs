using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using NextLabs.PLE.Log;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace Nextlabs.PLE.PageModule
{

    class RunReportPageResource
    {
        public static bool IsRunReportPage(HttpRequest Request)
        {
            try
            {
                if (Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    int index = Request.FilePath.LastIndexOf("/");
                    String real_request = Request.FilePath.Substring(0, index);
                    if (real_request.EndsWith("runreport.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during RunReportPageResource IsRunReportPage:", null, ex);
            }
            return false;
        }
    }

    class PageResourceFactory
    {

        public const string PLE_ATTR_SP_PAGE_TYPE = "page_type";
        public const string PLE_ATTR_SP_PAGE_TYPE_APPLICATION = "application page";//Page that have _layouts format
        public const string PLE_ATTR_SP_PAGE_TYPE_NORMAL = "normal page";
        public static String HttpModule_ReBuildURL(String _FullURL, String _PathURL, String _AddOnPath)
        {
            _FullURL = Globals.UrlDecode(_FullURL);
            if (_FullURL != null && _PathURL != null && _AddOnPath != null)
            {
                if (_FullURL.EndsWith(_PathURL, StringComparison.OrdinalIgnoreCase))//That's ok, no tail fix
                {
                    return _FullURL;
                }
                else //That is not good, it means the url has a tail
                {
                    int index = _FullURL.LastIndexOf(_PathURL, StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        int length = _PathURL.Length;
                        String _ReBuildURL = _FullURL.Substring(0, index + length);
                        String _QueryString = _FullURL.Substring(index + _AddOnPath.Length, _FullURL.Length - index - _AddOnPath.Length);
                        return _ReBuildURL + _QueryString;
                    }
                    else
                    {
                        return _FullURL;
                    }

                }
            }
            return _FullURL;
        }

        private static bool IsListFromUrlFormat(HttpRequest Request, URLAnalyser.UrlSPType _spType)
        {
            try
            {
                int index = Request.FilePath.LastIndexOf("/");
                String list_path = Request.FilePath.Substring(0, index);
                SPList _list = (SPList)Utilities.GetCachedSPContent(SPControl.GetContextWeb(HttpContext.Current), list_path, Utilities.SPUrlList);
                //If it's format is list and can get a list, then it is a list indeed
                if (_spType == URLAnalyser.UrlSPType.DOC_LIB
                    || _spType == URLAnalyser.UrlSPType.OTHER_LIST)
                {
                    if (_list != null)
                        return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during PageResourceFactory IsListFromUrlFormat:", null, ex);
            }
            return false;
        }

        public static IPageResource create(HttpRequest Request, HttpApplication source)
        {
            try
            {
                if (!Request.FilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                {
                    if (RunReportPageResource.IsRunReportPage(Request))
                    {
                        return new SitePageResource();
                    }
                    else
                        return null;
                }


                URLAnalyser _URLAnalyser = new URLAnalyser();
                //Use a rebuild url to analyse to fix bug 9973, modified by William 20090914
                String _NewUrl = HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
                Uri _RebuildUrl = new Uri(_NewUrl);
                URLAnalyser.UrlSPType _spType = _URLAnalyser.getSPTypeFromUrl(_RebuildUrl);
                SPList Listobj = null;
                bool isLayoutFolder = false;
                string siteUrl = null;
                try
                {
                    string[] segments = _RebuildUrl.Segments;
                    char[] slashChArr = { '/' };
                    string listName = Globals.UrlDecode
                                (segments[segments.Length - 2].
                                    TrimEnd(slashChArr));
                    HttpApplication application = (HttpApplication)source;
                    HttpContext context = application.Context;
                    /*
                     *  SPControl.GetContextWeb(context),if this method to get the SPWeb object ,
                     *  when the object use getlist method,there is wrong,so change this to SPWeb.OpenWeb method.
                     *  the other,when visited url is xxx/xxx/upload.aspx,the first httpmethod is get,somehow it
                     *  will impact the second step that is submit.
                     */
                    if (Request.UrlReferrer != null
                        && (Request.Url.LocalPath.EndsWith("upload.aspx", StringComparison.OrdinalIgnoreCase)
                        || (Request.Url.ToString().IndexOf("_layouts/advsetng.aspx") > 0)
                        || (Request.Url.ToString().IndexOf("_layouts/15/advsetng.aspx") > 0)
                        || listName.EndsWith("_layouts", StringComparison.OrdinalIgnoreCase)
                        || listName.EndsWith("15", StringComparison.OrdinalIgnoreCase)))
                    {
                        siteUrl = Request.UrlReferrer.ToString();
                    }
                    else
                    {
                        //bear fix custom issus,bug id 24510
                        if (Request.Url.AbsoluteUri.Contains("_layouts/"))
                        {
                            isLayoutFolder = true;
                            if (Request.UrlReferrer != null)
                            {
                                if (Request.UrlReferrer.ToString().Contains("_layouts/"))
                                {
                                    siteUrl = Request.UrlReferrer.ToString();
                                }
                                else
                                {
                                    siteUrl = Request.Url.AbsoluteUri;
                                }
                            }
                            else
                            {
                                siteUrl = Request.Url.AbsoluteUri;
                            }
                        }
                        else
                        {
                            siteUrl = Request.Url.AbsoluteUri;
                        }
                    }
                    using (SPSite siteObj = new SPSite(siteUrl))
                    {
                        using (SPWeb webObj = siteObj.OpenWeb())
                        {
                            //For some page, e.g. useredit.aspx, it is a setting page for a site. following line will lead to exception.
                            //Though the exception be handled, but it will still lead to some error just like bug 22504.
                            //So for these specific, following line should be avoid to execute
                            if (listName.EndsWith("_layouts", StringComparison.OrdinalIgnoreCase) == false
#if SP2013
                            &&listName.EndsWith("15",StringComparison.OrdinalIgnoreCase)==false
#else
 && listName.EndsWith("16", StringComparison.OrdinalIgnoreCase) == false
#endif
)
                            {
                                if (!isLayoutFolder)
                                {
                                    Listobj = webObj.Lists[listName];
                                }
                            }
                            else
                            {
                                string listGuid = Request.QueryString["List"];
                                if (listGuid != null)
                                {
                                    if (!listGuid.StartsWith("{"))
                                    {
                                        listGuid = "{" + listGuid + "}";
                                    }
                                    Guid _listGuid = new Guid(listGuid);
                                    Listobj = webObj.Lists[_listGuid];
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                if (Listobj != null && Listobj.BaseTemplate == SPListTemplateType.DiscussionBoard)
                {
                    if (string.IsNullOrEmpty(Request.QueryString["RootFolder"]))
                    {
                        //if the root folder is null, this discussion is being added from discussion list
                        //this request should be enforced as an page resource
                        return new PortletPageResource();
                    }
                    return new PorletItemPageResource();
                }
                else
                {
                    if ((PortletPageResource.IsPortletPageResource(Request))
                        || IsListFromUrlFormat(Request, _spType))
                    {

                        return new PortletPageResource();
                    }
                    else if ((PorletItemPageResource.IsPorletItemPageResource(Request))
                        || _spType == URLAnalyser.UrlSPType.DOC_LIB_ITEM || _spType == URLAnalyser.UrlSPType.OTHER_LIST_ITEM)
                    {

                        return new PorletItemPageResource();
                    }
                    else if ((SitePageResource.IsSitePageResource(Request))
                        || _spType == URLAnalyser.UrlSPType.SITE || _spType == URLAnalyser.UrlSPType.WEB)
                    {

                        return new SitePageResource();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (System.Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during IPageResource create:", null, ex);
            }
            return null;
        }
    }


    public class URLAnalyser
    {
        public String ConvertSPBaseType2PolicySubtype(String basetype, String Type)
        {
            switch (basetype)
            {
                case "DocumentLibrary":
                    if (Type == "PortletItem")
                    {
                        return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                    }
                    else
                    {
                        return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    }

                case "DiscussionBoard":
                case "GenericList":
                case "Issue":
                case "Survey":
                case "UnspecifiedBaseType":
                case "Unused":
                default:
                    if (Type == "PortletItem")
                    {
                        return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                    }
                    else
                    {
                        return CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                    }
            }
        }

        public String ConvertSPType2PolicyType(String type)
        {
            switch (type)
            {
                case "Microsoft.SharePoint.SPWeb":
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE;

                case "Microsoft.SharePoint.SPDocumentLibrary":
                case "Microsoft.SharePoint.SPList":
                default:
                    return CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            }
        }

        public String ConvertAspxPath(String RelativePath, HttpRequest Request, String Type)
        {
            try
            {
                String _ExeFilePath = Request.CurrentExecutionFilePath;
                String _PageFile = null;
                String _ReturnPath = null;

                if (_ExeFilePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                {
                    if (Type.Equals("Site", StringComparison.OrdinalIgnoreCase))
                    {
                        String _LayoutStr = "/_layouts";
                        String _CatalogStr = "/_catalogs";
                        //Use a rebuild url to fix bug 9973, modified by William 20090914
                        String Reuqest_Path = PageResourceFactory.HttpModule_ReBuildURL(Request.Url.AbsoluteUri, Request.FilePath, Request.Path);
                        if (_ExeFilePath.StartsWith(_LayoutStr, StringComparison.OrdinalIgnoreCase)
                            || _ExeFilePath.StartsWith(_CatalogStr, StringComparison.OrdinalIgnoreCase))
                        {
                            int index = _ExeFilePath.LastIndexOf("/");
                            int length = _ExeFilePath.Length;
                            _PageFile = _ExeFilePath.Substring(index + 1, length - index - 1);
                            if (RelativePath.EndsWith("/"))
                                _ReturnPath = RelativePath + _PageFile;
                            else
                                _ReturnPath = RelativePath + "/" + _PageFile;
                        }
                        else
                        {
                            _ReturnPath = Reuqest_Path;
                        }
                    }
                    else
                    {
                        if (_ExeFilePath.StartsWith("/Pages", StringComparison.OrdinalIgnoreCase))
                        {
                            int length = _ExeFilePath.Length;
                            //Fix bug 10012 detect the "/pages" string
                            if (RelativePath.EndsWith("/Pages", StringComparison.OrdinalIgnoreCase))
                            {
                                _PageFile = _ExeFilePath.Substring(7, length - 7);
                            }
                            else
                            {
                                _PageFile = _ExeFilePath.Substring(1, length - 1);
                            }
                        }
                        else
                        {
                            if (Type == "Site")
                            {
                                String _LayoutStr = "/_layouts";
                                if (_ExeFilePath.StartsWith(_LayoutStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    _PageFile = _ExeFilePath.Remove(0, _LayoutStr.Length + 1);
                                }
                                else
                                {
                                    _PageFile = _ExeFilePath;
                                }
                            }
                            else
                            {
                                int index = _ExeFilePath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                                int length = _ExeFilePath.Length;
                                _PageFile = _ExeFilePath.Substring(index + 1, length - index - 1);
                            }
                        }
                        if (RelativePath.EndsWith("/"))
                            _ReturnPath = RelativePath + _PageFile;
                        else
                            _ReturnPath = RelativePath + "/" + _PageFile;
                        //These code delete the /_catalogs keyword
                        String _SplitWord = "/_catalogs";
                        if (_ExeFilePath.StartsWith(_SplitWord, StringComparison.OrdinalIgnoreCase))
                        {
                            int _index = _ReturnPath.IndexOf(_SplitWord, StringComparison.OrdinalIgnoreCase);
                            _ReturnPath = _ReturnPath.Remove(_index, _SplitWord.Length);
                        }
                    }
                }
                else if (RunReportPageResource.IsRunReportPage(Request))
                {
                    int index = Request.FilePath.LastIndexOf("/");
                    _ReturnPath = Request.FilePath.Substring(0, index);
                }
                return _ReturnPath;
            }
            catch (System.Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during URLAnalyser URLAnalyser:", null, ex);
            }
            return null;
        }

        public enum UrlSPType
        {
            SITE,
            WEB,
            DOC_LIB,
            OTHER_LIST,
            DOC_LIB_ITEM,
            OTHER_LIST_ITEM,
            NOT_SURE
        }

        private bool isStrInArrayNoCase(String s, String[] arr)
        {
            foreach (string s2 in arr)
            {
                if (s.Equals(s2, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // See if the passed dir name is a SharePoint built-in dir for sites.
        // Trailing slash is allowed.
        private bool isDirSPSiteBuiltinDir(String dirName)
        {
            string[] SPSiteBuiltinDirs = { "Pages" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPSiteBuiltinDirs);
        }

        // See if the passed dir name is a SharePoint built-in dir for doc
        // libs.  Trailing slash is allowed.
        private bool isDirSPDocLibBuiltinDir(String dirName)
        {
            string[] SPDocLibBuiltinDirs = { "Forms" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPDocLibBuiltinDirs);
        }

        // See if the passed dir name is a SharePoint built-in dir for
        // non-doc-lib lists.  Trailing slash is allowed.
        private bool isDirSPOtherListBuiltinDir(String dirName)
        {
            string[] SPOtherListBuiltinDirs = { "Lists" };
            char[] slashChArr = { '/' };

            return isStrInArrayNoCase(dirName.TrimEnd(slashChArr),
                                      SPOtherListBuiltinDirs);
        }

        //
        // The three SharePoint standard content page lists below are found in
        // the article titled "Windows SharePoint Services Default Master
        // Pages" under the "Windows SharePoint Services 3.0" section in the
        // MSDN web site.  The article lists the following:
        //
        // - default.aspx
        // - AllItems.aspx, DispForm.aspx, NewForm.aspx, and EditForm.aspx: for
        //   all lists
        // - Upload.aspx and Webfldr.aspx: for all document libraries
        //
        private String[] SPStdWebContentPages =
        {
            "default.aspx","Home.aspx"
        };

        private String[] SPStdDocLibContentPages =
        {
            "Upload.aspx", "Webfldr.aspx"
        };

        private String[] SPStdListContentPages =
        {
            "AllItems.aspx", "DispForm.aspx", "NewForm.aspx", "EditForm.aspx"
        };

        private String[] SPStdListItemQueryPages =
        {
            "DispForm.aspx", "EditForm.aspx"
        };

        // See if the passed file name is a SharePoint standard content page
        // for sites and webs.
        private bool isFileSPStdWebContentPage(String fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdWebContentPages);
        }

        // See if the passed file name is a SharePoint standard content page
        // for document libraries.
        private bool isFileSPStdDocLibContentPage(String fileName)
        {
            return (isStrInArrayNoCase(fileName, SPStdDocLibContentPages) ||
                    isFileSPStdListContentPage(fileName));
        }

        // See if the passed file name is a SharePoint standard content page
        // for lists.
        private bool isFileSPStdListContentPage(String fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdListContentPages);
        }

        // See if the passed file name is a SharePoint standard item query page
        // for lists.
        private bool isFileSPStdListItemQueryPage(String fileName)
        {
            return isStrInArrayNoCase(fileName, SPStdListItemQueryPages);
        }

        // See if the passed file name may be a content page for lists.
        private bool isFileMaybeListContentPage(String fileName)
        {
            return !fileName.Contains("/") &&
                fileName.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase);
        }

        // See if the passed query is for a list item
        private bool isQueryListItem(String query)
        {
            return query.Contains("?ID=") || query.Contains("&ID=");
        }

        // See if the passed URL represents a site.
        private bool isUrlSite(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Pages/default.aspx
            return (len >= 3 &&
                    isDirSPSiteBuiltinDir(segments[len - 2]) &&
                    isFileSPStdWebContentPage(segments[len - 1]));
        }

        // See if the passed URL represents a web.
        private bool isUrlWeb(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]<web>/default.aspx
            // where web cannot be "Pages"
            return (len >= 3 &&
                    !isDirSPSiteBuiltinDir(segments[len - 2]) &&
                    isFileSPStdWebContentPage(segments[len - 1]));
        }

        // See if the passed URL represents a document library.
        private bool isUrlDocLib(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]<doclib>/Forms/<aspx>[<query>]
            // where <doclib> cannot be "Lists", excluding the case when <aspx>
            // is a standard item query page and <query> contains ID.
            return (len >= 4 &&
                    !isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isDirSPDocLibBuiltinDir(segments[len - 2]) &&
                    isFileMaybeListContentPage(segments[len - 1]) &&
                    !(isFileSPStdListItemQueryPage(segments[len - 1]) &&
                      isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a non-doc-lib list.
        private bool isUrlOtherList(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Lists/<list>/<aspx>[<query>]
            // where <list> can be "Forms", excluding the case when <aspx> is a
            // standard item query page and <query> contains ID.
            return (len >= 4 &&
                    isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isFileMaybeListContentPage(segments[len - 1]) &&
                    !(isFileSPStdListItemQueryPage(segments[len - 1]) &&
                      isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a doc lib item *for sure*.
        //
        // Returns:     true if we are sure that the URL is a doc lib item.
        //              false if we are not sure whether or not the URL is a
        //              doc lib item.
        private bool isUrlDocLibItemForSure(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format we are detecting here is
            // <scheme>://<host>/[...]<doclib>/Forms/<aspx><query>
            // where <doclib> cannot be "Lists", <aspx> is a standard item
            // query page and query contains ID.
            //
            // The format we are not detecting here is
            // <scheme>://<host>/[...]<doclib>/[...]<file>
            //
            // Thus, even if we don't detect a match here, the URL might still
            // be a doc lib item.
            return (len >= 4 &&
                    !isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    isDirSPDocLibBuiltinDir(segments[len - 2]) &&
                    (isFileSPStdListItemQueryPage(segments[len - 1]) &&
                     isQueryListItem(Url.Query)));
        }

        // See if the passed URL represents a non-doc-lib list item.
        public bool isUrlOtherListItem(Uri Url)
        {
            string[] segments = Url.Segments;
            int len = segments.Length;

            // The format is
            // <scheme>://<host>/[...]Lists/<list>/<aspx><query>
            // where <list> can be "Forms", <aspx> is a standard item query
            // page and <query> contains ID.
            return (len >= 4 &&
                    isDirSPOtherListBuiltinDir(segments[len - 3]) &&
                    (isFileSPStdListItemQueryPage(segments[len - 1]) &&
                     isQueryListItem(Url.Query)));
        }

        public UrlSPType getSPTypeFromUrl(Uri Url)
        {
            if (isUrlSite(Url))
                return UrlSPType.SITE;
            else if (isUrlWeb(Url))
                return UrlSPType.WEB;
            else if (isUrlDocLib(Url))
                return UrlSPType.DOC_LIB;
            else if (isUrlOtherList(Url))
                return UrlSPType.OTHER_LIST;
            else if (isUrlDocLibItemForSure(Url))
                return UrlSPType.DOC_LIB_ITEM;
            else if (isUrlOtherListItem(Url))
                return UrlSPType.OTHER_LIST_ITEM;
            else
                return UrlSPType.NOT_SURE;
        }

    }
}


