using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using NextLabs.Common;
using Microsoft.SharePoint;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint.WebControls;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public class SPE_PRJSETNG_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "Site Content & Structure ->Site -> General Setting"
            // URL Query: ?Source=/testsite2/_Layouts/sitemanager.aspx?SmtContext=SPWeb:42d79b12-9f92-4d90-9a9b-6c3e5498933c:&SmtContextExpanded=True&Filter=1&pgsz=100&vrmode=False
            _SPEEvalAttr.Action = "SITE SETTING:GENERAL";
            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
            //fix bug 8404, remove /_layouts/xxx
            int layout_index = _SPEEvalAttr.ObjEvalUrl.IndexOf("/_layouts");
            if (layout_index > 0)
            {
              _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.ObjEvalUrl.Remove(layout_index);
            }
            if (_SPEEvalAttr.WebObj != null)
            {
                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
            }
            return false;
        }
    }

    public class SPE_SITEMANAGER_ASPX_Module : SPEModuleBase
    {
        private bool OpenWebList(SPEEvalAttr evalAttr, string strArgment)
        {
            string strFlag = "SPList:";
            int guidLegth = 36;
            int ind = strArgment.IndexOf(strFlag, StringComparison.OrdinalIgnoreCase);
            if (-1 != ind)
            {
                string listId = strArgment.Substring(ind + strFlag.Length, guidLegth);
                SPList list = evalAttr.WebObj.Lists[new Guid(listId)];
                if (list != null)
                {
                    evalAttr.Action = "Open";
                    evalAttr.PolicyAction = CETYPE.CEAction.Read;
                    SPEEvalAttrHepler.SetObjEvalAttr(list, evalAttr);
                    return false;
                }
            }
            else
            {
                strFlag = "SPWeb:";
                ind = strArgment.IndexOf(strFlag, StringComparison.OrdinalIgnoreCase);
                if (-1 != ind)
                {
                    string webId = strArgment.Substring(ind + strFlag.Length, guidLegth);
                    SPWeb web = evalAttr.WebObj.Webs[new Guid(webId)];
                    if (web != null)
                    {
                        evalAttr.Action = "Open";
                        evalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(web, evalAttr);
                        return false;
                    }
                }
            }
            return true;
        }
        public override bool DoSPEProcess()
        {
            SPWeb _srcWebObj = null;
            SPWeb _dstWebObj = null;
            try
            {
                SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();

                // __EVENTARGUMENT=Goto$SPList:63cf8cac-726e-4a90-a4b5-866b96c665c7?SPWeb:17da8993-f271-4765-8e34-05a56b08f716:
                string strArgment = m_Request.Form["__EVENTARGUMENT"];
                if (!string.IsNullOrEmpty(strArgment) && -1 != strArgment.IndexOf("Goto", StringComparison.OrdinalIgnoreCase))
                {
                    strArgment = Globals.UrlDecode(strArgment);
                    return OpenWebList(_SPEEvalAttr, strArgment);
                }

                // it is "Site Content & Structures -> Delete"
                // URL Query: ?Source=/_layouts/settings.aspx&Filter=1

                // It could be in here, depends on WHERE you delete
                // From Left Pane
                String _paramObjId = Globals.UrlDecode(m_Request.Form["TVEcbDeleteTarget"]);
                String _paramWebId = null;
                String _paramListId = null;
                // From Right Pane

                if (string.IsNullOrEmpty(_paramObjId))
                {
                    _paramObjId = Globals.UrlDecode(m_Request.Form["OLEcbDeleteTarget"]);
                }
                if (string.IsNullOrEmpty(_paramObjId))
                {
                    string content1 = Globals.UrlDecode(m_Request.Form["__EVENTARGUMENT"]);
                    string content2 = Globals.UrlDecode(m_Request.Form["SmtDeleteOrRecycle"]);
                    if ((content1 != null && content1.Equals("SmtDelete", StringComparison.OrdinalIgnoreCase)) ||
                        (content2 != null && content1.Equals("Are you sure you want to delete the selected items?", StringComparison.OrdinalIgnoreCase)))
                    {
                        _paramObjId = Globals.UrlDecode(m_Request.Form["SMItem"]);
                    }
                    // fix bug8296 by derek -->
                    else if ((content1 != null && content1.StartsWith("Copy",
                                         StringComparison.OrdinalIgnoreCase)) ||
                        (content1 != null && content1.StartsWith("Move",
                                         StringComparison.OrdinalIgnoreCase)))
                    {
                        string _source = Globals.UrlDecode(m_Request.Form["LroSource"]);
                        string _destination = Globals.UrlDecode(m_Request.Form["LroDestination"]);

                        if (String.IsNullOrEmpty(_source))
                        {
                            _source = Globals.UrlDecode(m_Request.Form["SMItem"]);
                        }

                        if (String.IsNullOrEmpty(_destination))
                        {
                            _destination = content1.Substring(content1.IndexOf('$'));
                        }

                        int _srcItemIndex = _source.IndexOf("SPListItem:");
                        int _srcListIndex = _source.IndexOf("SPList:");
                        int _srcWebIndex = _source.IndexOf("SPWeb:");

                        //Herbert Add
                        int _dstFolderIndex = _destination.IndexOf("SPFolder:");
                        //Add end

                        int _dstListIndex = _destination.IndexOf("SPList:");
                        int _dstWebIndex = _destination.IndexOf("SPWeb:");

                        SPList _srcListObj = null;
                        SPList _dstListObj = null;
                        SPFolder _dstFolderObj = null;

                        if (_srcWebIndex >= 0)
                        {
                            _paramWebId = _source.Substring(_srcWebIndex + 6, 36);
                            SPSite _siteObj = SPControl.GetContextSite(HttpContext.Current);
                            Guid _webGuid = new Guid(_paramWebId);

                            try
                            {
                                _srcWebObj = _siteObj.OpenWeb(_webGuid);
                            }
                            catch
                            {
                                _srcWebObj = null;
                            }

                            if (_srcListIndex >= 0 && _srcWebObj != null)
                            {
                                _paramListId = _source.Substring(_srcListIndex + 7, 36);
                                try
                                {
                                    _srcListObj = (SPList)Utilities.GetCachedSPContent(_srcWebObj, _paramListId, Utilities.SPUrlListID);
                                }
                                catch
                                {
                                    _srcListObj = null;
                                }
                            }
                        }
                        if (_dstWebIndex >= 0)
                        {
                            _paramWebId = _destination.Substring(_dstWebIndex + 6, 36);
                            SPSite _siteObj = SPControl.GetContextSite(HttpContext.Current);
                            Guid _webGuid = new Guid(_paramWebId);

                            try
                            {
                                _dstWebObj = _siteObj.OpenWeb(_webGuid);
                            }
                            catch
                            {
                                _dstWebObj = null;
                            }

                            if (_dstListIndex >= 0 && _dstWebObj != null)
                            {
                                _paramListId = _destination.Substring(_dstListIndex + 7, 36);
                                try
                                {
                                    _dstListObj = (SPList)Utilities.GetCachedSPContent(_dstWebObj, _paramListId, Utilities.SPUrlListID);
                                }
                                catch
                                {
                                    _dstListObj = null;
                                }
                            }
                            else if (_dstFolderIndex >= 0 && _dstWebObj != null)
                            {
                                String _paramFolderId = _destination.Substring(_dstFolderIndex + 9, 36);
                                Guid _FolderGuid = new Guid(_paramFolderId);
                                try
                                {
                                    _dstFolderObj = _dstWebObj.GetFolder(_FolderGuid);
                                }
                                catch
                                {
                                    _dstFolderObj = null;
                                }
                            }
                        }

                        // Document Related Copy/Move
                        if (_srcListObj != null && ((_dstListObj != null) || (_dstFolderObj != null)))
                        {
                            if (_srcItemIndex < 0)
                            {
                                // List Related Copy/Move
                            }
                            else
                            {
                                int _indexLen = _source.Substring(_srcItemIndex).IndexOf('?') - 11;

                                if (_indexLen > 0)
                                    _paramObjId = _source.Substring(_srcItemIndex + 11, _indexLen);
                                else
                                    _paramObjId = _source.Substring(_srcItemIndex + 11);
                                try
                                {
                                    _SPEEvalAttr.ItemObj = _srcListObj.GetItemById(int.Parse(_paramObjId));
                                }
                                catch
                                {
                                    _SPEEvalAttr.ItemObj = null;
                                }

                                if (_SPEEvalAttr.ItemObj != null)
                                {
                                    if (content1.StartsWith("Copy", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.Action = "COPY DOCUMENT";
                                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                    }
                                    else
                                    {
                                        _SPEEvalAttr.Action = "MOVE DOCUMENT";
                                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                    }
                                    //Fix bug 8787, use _SPEEvalAttr.ItemObj.Title to replace _SPEEvalAttr.ItemObj.Url
                                    _SPEEvalAttr.ObjEvalUrl = Globals.ConstructListUrl(_srcWebObj, _srcListObj) +
                                            '/' + CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "title");
                                    if (_dstListObj != null)
                                    {
                                        _SPEEvalAttr.ObjTargetUrl = Globals.ConstructListUrl(_dstWebObj, _dstListObj);
                                    }
                                    if (_dstFolderObj != null)
                                    {
                                        _SPEEvalAttr.ObjTargetUrl = Globals.ConstructFolderUrl(_dstWebObj, _dstFolderObj);
                                    }
                                    _SPEEvalAttr.ObjTargetUrl += "/" + CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "name");
                                    _SPEEvalAttr.BeforeUrl = _SPEEvalAttr.ObjEvalUrl;
                                    _SPEEvalAttr.AfterUrl = _SPEEvalAttr.ObjTargetUrl;
                                    _SPEEvalAttr.ObjName = CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "name");
                                    _SPEEvalAttr.ObjTitle = CommonVar.GetSPListItemContent(_SPEEvalAttr.ItemObj, "title");
                                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM;
                                    //Fix bug 8730, assign the listObj a value in order to get the SPFiled value, added by William
                                    _SPEEvalAttr.ListObj = _srcListObj;
                                    if (_srcListObj.GetType().ToString().Equals("Microsoft.SharePoint.SPDocumentLibrary", StringComparison.OrdinalIgnoreCase))
                                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM;
                                    else
                                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                                }
                            }
                        }
                    } // <-- fix bug8296 by derek
                }

                // Process Delete action in site manager
                if (!string.IsNullOrEmpty(_paramObjId))
                {
                    EvaluateSiteManagerObjectDeleting(HttpContext.Current, _paramObjId);
                    return true;
                }
                else
                {
                    ProcessTreeView(_SPEEvalAttr);
                }
            }
            catch
            {
            }
            finally
            {
                if (_srcWebObj != null)
                    _srcWebObj.Dispose();
                if (_dstWebObj != null)
                    _dstWebObj.Dispose();
            }
            return false;
        }

        // Content and struct tree view (George fix bug48166:site/list/library)
        private bool ProcessTreeView(SPEEvalAttr _SPEEvalAttr)
        {
            bool bRet = false;
            SPWeb web = null;
            bool bOpenWeb = false;
            try
            {
                string eventArgument = Globals.UrlDecode(m_Request.Form["__EVENTARGUMENT"]);
                string eventTarget = Globals.UrlDecode(m_Request.Form["__EVENTTARGET"]);
                string webFlag = "SPWeb:";
                string listFlag = "SPList:";
                if (_SPEEvalAttr.WebObj != null && -1 != eventArgument.IndexOf(webFlag, StringComparison.OrdinalIgnoreCase) && -1 != eventTarget.IndexOf("TreeView", StringComparison.OrdinalIgnoreCase))
                {
                    SPList list = null;
                    int ind = eventArgument.LastIndexOf(webFlag, StringComparison.OrdinalIgnoreCase);
                    if (-1 != ind)
                    {
                        string strGuid = eventArgument.Substring(ind + webFlag.Length, 36);
                        if (!string.IsNullOrEmpty(strGuid))
                        {
                            if (strGuid.Equals(_SPEEvalAttr.WebObj.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                web = _SPEEvalAttr.WebObj;
                            }
                            else
                            {
                                web = _SPEEvalAttr.WebObj.Site.OpenWeb(new Guid(strGuid));
                                bOpenWeb = true;
                            }
                        }
                    }
                    ind = eventArgument.LastIndexOf(listFlag, StringComparison.OrdinalIgnoreCase);
                    if (-1 != ind && web != null)
                    {
                        string strGuid = eventArgument.Substring(ind + listFlag.Length, 36);
                        if (!string.IsNullOrEmpty(strGuid))
                        {
                            list = web.Lists[new Guid(strGuid)];
                        }
                    }
                    if (list != null)
                    {
                        _SPEEvalAttr.Action = "Tree view list";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(list, _SPEEvalAttr);
                        bRet = true;
                    }
                    else if (web != null)
                    {
                        _SPEEvalAttr.Action = "Tree view web";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        SPEEvalAttrHepler.SetObjEvalAttr(web, _SPEEvalAttr);
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during ProcessTreeView:", null, ex);
            }
            finally
            {
                if (bOpenWeb && web != null)
                {
                    web.Dispose();
                }
            }
            return bRet;
        }

        private void EvaluateSiteManagerObjectDeleting(HttpContext context, String paramObjId)
        {
            SPSite _siteObj = null;
            SPList _listObj = null;

            Guid _webGuid;
            String _paramWebId = null;
            String _paramListId = null;

            String m_obj_name = "";
            String m_obj_title = "";
            String m_obj_type = "";
            String m_obj_description = "";
            String m_obj_subtype = "";
            String m_obj_referrerurl = null;
            // derek bug8612
            String m_obj_targeturl = null;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            CETYPE.CEAction m_policy_action = (CETYPE.CEAction)(-1);

            int nextIndex = paramObjId.IndexOf(',');
            String _paramObjId = paramObjId;
            String _remainObjId = "";
            if (nextIndex > 0)
            {
                _paramObjId = paramObjId.Substring(0, nextIndex);
                _remainObjId = paramObjId.Substring(nextIndex + 1);
            }

            while (!String.IsNullOrEmpty(_paramObjId))
            {
                int webIndex = _paramObjId.IndexOf("SPWeb:");
                int listIndex = _paramObjId.IndexOf("SPList:");
                int itemIndex = _paramObjId.IndexOf("SPListItem:");
                // At this level, we only care about the Web / List delete
                // if we see an ITEM (SPListItem) in the Request, we will skip the whole thing
                // and let the item-delete sync handler to handle it. Otherwise, a policy that
                // only applies to portlet will get enforced when user delete the item (bug 5000)
                if ((webIndex != -1) && itemIndex < 0)
                {
                    _paramWebId = _paramObjId;
                    _paramWebId = _paramWebId.Substring(webIndex + 6, 36);
                    _siteObj = SPControl.GetContextSite(context);
                    _webGuid = new Guid(_paramWebId);
                    using (SPWeb _webObj = _siteObj.OpenWeb(_webGuid))
                    {
                        if (_webObj != null)
                        {
                            if (listIndex != -1)
                            {
                                _paramListId = _paramObjId;
                                _paramListId = _paramListId.Substring(listIndex + 7, 36);
                                _listObj = (SPList)Utilities.GetCachedSPContent(_webObj, _paramListId, Utilities.SPUrlListID);
                                if (_listObj != null)
                                {
                                    m_policy_action = CETYPE.CEAction.Delete;
                                    m_obj_referrerurl = Globals.ConstructListUrl(_webObj, _listObj);
                                    m_obj_name = CommonVar.GetSPListContent(_listObj, "title");
                                    m_obj_type = Globals.ConvertSPType2PolicyType(_listObj.GetType().ToString());
                                    m_obj_description = CommonVar.GetSPListContent(_listObj, "description");
                                    m_obj_subtype = Globals.ConvertSPBaseType2PolicySubtype(CommonVar.GetSPListContent(_listObj, "basetype"));

                                }
                            }
                            else if (_paramObjId.StartsWith("SPWeb:", StringComparison.OrdinalIgnoreCase) && _webObj.WebTemplateId == 1)
                            {
                                m_policy_action = CETYPE.CEAction.Delete;
                                m_obj_referrerurl = CommonVar.GetSPWebContent(_webObj, "url");
                                m_obj_name = _webObj.Name;
                                m_obj_title = CommonVar.GetSPWebContent(_webObj, "title");
                                m_obj_type = Globals.ConvertSPType2PolicyType(_webObj.GetType().ToString());
                                m_obj_description = CommonVar.GetSPWebContent(_webObj, "description");
                                m_obj_subtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE;

                            }
                            else
                            {
                                nextIndex = _remainObjId.IndexOf(',');
                                if (nextIndex > 0)
                                {
                                    _paramObjId = _remainObjId.Substring(0, nextIndex);
                                    _remainObjId = _remainObjId.Substring(nextIndex + 1);
                                }
                                else
                                {
                                    _paramObjId = _remainObjId;
                                    _remainObjId = "";
                                }
                                continue;
                            }
                            // This is something that we want to check. Do Policy Check here.
                            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;
                            string[] emptyArray = new string[0];
                            string[] propertyArray = new string[5 * 2];
                            string policyName = null;
                            string policyMessage = null;
                            propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                            propertyArray[0 * 2 + 1] = m_obj_name;
                            propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                            propertyArray[1 * 2 + 1] = m_obj_title;
                            propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                            propertyArray[2 * 2 + 1] = m_obj_description;
                            propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_RESOURCE_TYPE;
                            propertyArray[3 * 2 + 1] = m_obj_type;
                            propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                                CE_ATTR_SP_RESOURCE_SUBTYPE;
                            propertyArray[4 * 2 + 1] = m_obj_subtype;
                            string sid = _webObj.CurrentUser.Sid;
                            //to fix bug 9973 rebuild the url. modified by William 20090914
                            m_obj_referrerurl = Globals.HttpModule_ReBuildURL(m_obj_referrerurl, context.Request.FilePath, context.Request.Path);
                            response = Globals.CallEval(m_policy_action,
                                                        m_obj_referrerurl,
                                                        m_obj_targeturl,  // derek bug8612
                                                        ref propertyArray,
                                                        ref emptyArray,
                                                        _SPEEvalAttr.RemoteAddr,
                                                        _SPEEvalAttr.LogonUser,
                                                        sid,
                                                        ref policyName,
                                                        ref policyMessage,
                                                        null,
                                                        null,
                                                        Globals.HttpModuleName,
                                                        CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                                        _webObj,
                                                        null);
                            if (response == CETYPE.CEResponse_t.CEDeny)
                            {
                                //to fix 8108 and 8393 and the same problem, we use spweb.url as the backurl.
                                //if the denied url is current site url, we go up one level
                                String backurl = "";
                                if (_webObj != null)
                                {
                                    string site_url = CommonVar.GetSPWebContent(_webObj, "url");
                                    backurl = site_url;
                                    if (site_url == m_obj_referrerurl)
                                    {
                                        int index1 = backurl.LastIndexOf("/");
                                        if (index1 > 0)
                                        {
                                            string url = site_url.Remove(index1);
                                            if (!url.EndsWith("sites"))
                                            {
                                                backurl = site_url.Remove(index1);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        backurl = site_url;
                                    }
                                }
                                else
                                {
                                    backurl = m_obj_referrerurl;
                                    int index1 = backurl.IndexOf("_layouts");

                                    if (index1 > 0)
                                    {

                                        backurl = backurl.Remove(index1);

                                    }
                                }

                                String serverurl = m_obj_referrerurl;
                                int index = serverurl.IndexOf("_layouts");

                                if (index > 0)
                                {
                                    serverurl = serverurl.Remove(index);
                                }

                                String httpserver = serverurl;
                                index = httpserver.IndexOf("http://");
                                if (index >= 0)
                                {
                                    httpserver = httpserver.Remove(index, 7);
                                }
                                index = httpserver.IndexOf("/");
                                if (index > 0)
                                {
                                    httpserver = httpserver.Remove(index);
                                }
                                httpserver = "http://" + httpserver;

                                string msg = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);

                                if (!CustomDenyPageSwitch.IsEnabled())
                                {
                                    blockRequest(context.ApplicationInstance, context.Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                                }
                                else
                                {
                                    string strWebUrl = Utilities.GetWebsiteUrl();
                                    if (string.IsNullOrEmpty(strWebUrl))
                                    {
                                        if (_SPEEvalAttr.WebObj != null)
                                        {
                                            strWebUrl = _SPEEvalAttr.WebObj.Url;
                                        }
                                    }

                                    if (string.IsNullOrEmpty(strWebUrl))
                                    {
                                        blockRequest(context.ApplicationInstance, context.Response, Globals.GetDenyPageHtml(httpserver, backurl, msg));
                                    }
                                    else
                                    {
                                        context.Response.Redirect(strWebUrl + "/_layouts/error-template/DenyPage.aspx?loginName=" + HttpUtility.UrlEncode(_SPEEvalAttr.LoginName) + "&resouceID=" + HttpUtility.UrlEncode(_SPEEvalAttr.ObjEvalUrl) + "&policyMessage=" + HttpUtility.UrlEncode(msg), false);
                                        context.ApplicationInstance.CompleteRequest();
                                    }
                                }
                                return;
                            }
                        }
                    }
                }

                nextIndex = _remainObjId.IndexOf(',');
                if (nextIndex > 0)
                {
                    _paramObjId = _remainObjId.Substring(0, nextIndex);
                    _remainObjId = _remainObjId.Substring(nextIndex + 1);
                }
                else
                {
                    _paramObjId = _remainObjId;
                    _remainObjId = "";
                }
            }
        }

        private void blockRequest(HttpApplication app, HttpResponse Response, String StatusDescription)
        {
            Response.StatusCode = 403;
            Response.ContentType = "text/html";
            Response.Write(StatusDescription);

            // 2009/03/10 ayuen:
            // Don't call Response.Flush() here, because calling it causes
            // Response.End() below to generate ThreadAbortException in some
            // cases.  I don't understand why the problem happens, and I don't
            // understand why removing the call fixes the problem either.
            app.CompleteRequest();
        }

    }

    public class SPE_POLICY_ASPX_Module : SPE_POLICYBASE_Module
    {
    }

    public class SPE_POLICYCTS_ASPX_Module : SPE_POLICYBASE_Module
    {
    }

    public class SPE_POLICYBASE_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Title, description and navigation"
            // URL Query: ?List={BB7B6393-07A4-4CF4-9AF7-EA7AD95EA864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:TITLE,DESCRIPTION,NAVIGATION";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_LISTGENERALSETTINGS_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Title, description and navigation"
            // URL Query: ?List={BB7B6393-07A4-4CF4-9AF7-EA7AD95EA864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:TITLE,DESCRIPTION,NAVIGATION";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_ADVSETNG_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Advanced settings"
            // URL Query: ?List={BB7B6393-07A4-4CF4-9AF7-EA7AD95EA864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:ADVANCED";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    // fix bug for 8599 added by herbert 09.02.05
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_ADDGALLERY_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String _Cmd = m_Request.Form["Task"];
            String _Title = m_Request.Form["Title"];
            String _Description = m_Request.Form["Description"];
            String _ListTemplateID = m_Request.Form["ListTemplateType"];
            if (_Cmd != null)
            {
                if (_Cmd.Equals("CreateList", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.Action = "LIST CREATE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                    if (_ListTemplateID != null && Globals.IsLibraryTemplateID(_ListTemplateID))
                    {
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _Title;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                    }
                    else
                    {
                        _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + "Lists" + "/" + _Title;
                        _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                    }
                }
                else if (_Cmd.Equals("CreateSite", StringComparison.OrdinalIgnoreCase))
                {
                    _SPEEvalAttr.Action = "SITE CREATE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                    _SPEEvalAttr.ObjName = _Title;
                    _SPEEvalAttr.ObjTitle = _Title;
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _Title;
                    _SPEEvalAttr.ObjDesc = _Description;
                    _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                        CE_ATTR_SP_TYPE_VAL_SITE;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.
                        CE_ATTR_SP_SUBTYPE_VAL_SITE;
                }
                _SPEEvalAttr.ObjTitle = _SPEEvalAttr.ObjName = _Title;
            }
            return false;
        }
    }

    public class SPE_NEW_ASPX_Module : SPE_NEWBASE_Module
    {
    }

    public class SPE_SLNEW_ASPX_Module : SPE_NEWBASE_Module
    {
    }

    public class SPE_NEWTRANSLATIONMANAGEMENT_ASPX_Module : SPE_NEWBASE_Module
    {
    }


    public class SPE_NEWBASE_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String _Cmd = m_Request.Form["Cmd"];
            String _Title = m_Request.Form["Title"];
            String _Description = m_Request.Form["Description"];
            String _ListTemplateID = m_Request.Form["ListTemplate"];
            if (_Cmd != null && _Cmd.Equals("NewList", StringComparison.OrdinalIgnoreCase))
            {
                _SPEEvalAttr.Action = "LIST CREATE";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                if (_ListTemplateID != null && Globals.IsLibraryTemplateID(_ListTemplateID))
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _Title;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                }
                else
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + "Lists" + "/" + _Title;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                }
                _SPEEvalAttr.ObjName = _Title;
                _SPEEvalAttr.ObjTitle = _Title;
                _SPEEvalAttr.ObjDesc = _Description;
                _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            }
            return false;
        }
    }

    public class SPE_CREATELISTPICKERPAGE_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String _Cmd = m_Request.Form["__EVENTTARGET"];
            if (_Cmd != null && _Cmd.Equals("ctl00$PlaceHolderRteDialogBody$btnOk", StringComparison.OrdinalIgnoreCase))
            {
                _SPEEvalAttr.Action = "LIST CREATE";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                String _Title = m_Request.Form["ctl00$PlaceHolderRteDialogBody$ListNameText"];
                String _ListTemplate = m_Request.Form["ctl00$PlaceHolderRteDialogBody$SelectedListTemplate"];
                if (_ListTemplate != null &&
                    (_ListTemplate.Equals("Document Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Record Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Form Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Wiki Page Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Picture Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Asset Library", StringComparison.OrdinalIgnoreCase)
                    || _ListTemplate.Equals("Slide Library", StringComparison.OrdinalIgnoreCase)))
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _Title;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                }
                else
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + "Lists" + "/" + _Title;
                    _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                }
                _SPEEvalAttr.ObjName = _Title;
                _SPEEvalAttr.ObjTitle = _Title;
                _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
            }
            return false;
        }
    }

    public class SPE_LISTENABLETARGETING_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Audience targeting settings"
            // URL Query: ?List={bb7b6393-07a4-4cf4-9af7-ea7ad95ea864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:AUDIENCE TARTGETING";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_LISTSYNDICATION_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // URL Query: ?List={bb7b6393-07a4-4cf4-9af7-ea7ad95ea864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:RSS SETTING";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                   SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_MANAGEITEMSCHEDULING_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Manage item scheduling"
            // URL Query: ?List={c75fcfea-3f97-4aa3-a4f4-a445b03c334b}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:MANAGE ITEM SCHEDULING";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }
    public class SPE_LISTEDIT_ASPX_Module : SPE_LISTEDITBASE_Module
    {
    }

    public class SPE_SURVEDIT_ASPX_Module : SPE_LISTEDITBASE_Module
    {
    }

    public class SPE_CHECKIN_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr evalAttr = SPEEvalAttrs.Current();

            string strFileName = m_Request.QueryString["filename"];
            string strCheckOut=m_Request.QueryString["checkout"];
            if (evalAttr != null&&evalAttr.WebObj!=null)
            {
                evalAttr.ObjEvalUrl = strFileName;
                evalAttr.ItemObj = (SPListItem)Utilities.GetCachedSPContent(evalAttr.WebObj, strFileName, Utilities.SPUrlListItem);

                if (evalAttr.ItemObj != null)
                {
                    evalAttr.ListObj = evalAttr.ItemObj.ParentList;
                    if (strCheckOut == null)
                    {
                        evalAttr.Action = "CheckIn";
                    }
                    else
                    {
                        if (strCheckOut.StartsWith("true", StringComparison.OrdinalIgnoreCase))
                        {
                            evalAttr.Action = "CheckOut";
                        }
                        else
                        {
                            evalAttr.Action = "CheckIn";
                        }
                    }
                    evalAttr.PolicyAction = CETYPE.CEAction.Write;

                    SPEEvalAttrHepler.SetObjEvalAttr(evalAttr.ItemObj, evalAttr);
                }
            }
            return false;
        }
	}

    public class SPE_RATINGSSETTINGS_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "List -> List Settings -> Rating settings"
            // URL Query: ?List={BB7B6393-07A4-4CF4-9AF7-EA7AD95EA864}
            String _paramListId = m_Request.QueryString["List"];
            if (_paramListId != null)
            {
                _SPEEvalAttr.Action = "LIST SETTING:Rating settings";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;

        }
    }

    public class SPE_LISTEDITBASE_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "Edit/Delete List/Library"
            // URL Query: ?List={4AD3FE5B-E744-4CD0-8170-43FFEC524BAD}
            String _paramListId = m_Request.QueryString["List"];
            String _listEVENTARGUMENT = Globals.UrlDecode(m_Request.Form["__EVENTARGUMENT"]);
            if (_paramListId != null && _listEVENTARGUMENT.Equals("Delete", StringComparison.OrdinalIgnoreCase))
            {
                _SPEEvalAttr.Action = "LIST SETTING:DELETE";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _paramListId, Utilities.SPUrlListID);
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_NEWSBWEB_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            string objname = "";
            string objdesp = "";
            string objtitle = "";
            try
            {
                NameValueCollection reqForm = m_Request.Form;
                foreach(string strKey in reqForm.Keys)
                {
                    if (!string.IsNullOrEmpty(strKey))
                    {
                        if (-1 != strKey.IndexOf("TxtCreateSubwebName", StringComparison.OrdinalIgnoreCase))
                        {
                            objname = reqForm[strKey];
                        }
                        else if (-1 != strKey.IndexOf("TxtCreateSubwebTitle", StringComparison.OrdinalIgnoreCase))
                        {
                            objtitle = reqForm[strKey];
                        }
                        else if (-1 != strKey.IndexOf("TxtCreateSubwebDescription", StringComparison.OrdinalIgnoreCase))
                        {
                            objdesp = reqForm[strKey];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during DoSPEProcess:", null, ex);
            }
            if (!string.IsNullOrEmpty(objname) && !string.IsNullOrEmpty(objtitle))
            {
                _SPEEvalAttr.Action = "SITE CREATE";
                _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                _SPEEvalAttr.ObjName = objname;
                int ind = _SPEEvalAttr.ObjEvalUrl.IndexOf("/_layouts/");

                if (ind != -1)
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.ObjEvalUrl.Substring(0, ind + 1) + objname;
                }
                else
                {
                    _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.ObjEvalUrl + objname;
                }
                _SPEEvalAttr.ObjTitle = objtitle;
                _SPEEvalAttr.ObjDesc = objdesp;
                _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.
                    CE_ATTR_SP_TYPE_VAL_SITE;
                _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.
                    CE_ATTR_SP_SUBTYPE_VAL_SITE;
            }
            return false;
        }
    }

    public class SPE_DELETEWEB_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            string obj_referrerurl = _SPEEvalAttr.ObjEvalUrl;
            string webname = "";
            if (_SPEEvalAttr.WebObj != null && obj_referrerurl != null)
            {
                string subWebName = m_Request.QueryString["Subweb"];

                if (!string.IsNullOrEmpty(subWebName))
                {
                    SPWeb currentWeb = _SPEEvalAttr.WebObj.Webs[webname];
                    if (currentWeb != null)
                    {
                        _SPEEvalAttr.Action = "DELETE SITE";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                        SPEEvalAttrHepler.SetObjEvalAttr(currentWeb, _SPEEvalAttr);
                    }
                }
                else
                {
                    _SPEEvalAttr.Action = "DELETE SITE";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                }
            }
            return false;
        }
    }


    //#############################################################
    //GET
    public class SPE_DISCUSSIONS_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String _RootFolder = m_Request.QueryString["RootFolder"];
            if (_RootFolder != null)
            {
                SPList _curList = null;
                SPListItem _curItem = null;
                string[] splitBuf = _RootFolder.Split(new Char[] { '/' });
                int splitCount = splitBuf.Length;

                string _ListName = splitBuf[splitCount - 2];
                string _FileName = splitBuf[splitCount - 1];
                try
                {
                    _curList = _SPEEvalAttr.WebObj.GetList(_SPEEvalAttr.RequestURL_path);
                }
                catch
                {
                }
                if (_curList == null)
                {
                    foreach (SPList list in _SPEEvalAttr.WebObj.Lists)
                    {
                        if (list.Title == _ListName)
                        {
                            _curList = list;
                            break;
                        }
                    }
                }

                foreach (SPListItem item in _curList.Folders)
                {
                    if (_curList.BaseTemplate == SPListTemplateType.DiscussionBoard)
                    {
                        try
                        {
                            if (item["BaseName"].ToString() == _FileName)
                            {
                                _curItem = item;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        if (item.DisplayName == _FileName)
                        {
                            _curItem = item;
                            break;
                        }
                    }
                }
                if (null != _curList && null != _curItem)
                {
                    // we get current list and current item
                    // fill the parameters
                    _SPEEvalAttr.Action = "READ";
                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                    SPEEvalAttrHepler.SetObjEvalAttr(_curItem, _SPEEvalAttr);
                }
            }
            return false;
        }
    }

    public class SPE_FORMSERVER_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            String _paramUrl = m_Request.Params["XmlLocation"];
            String _paramSource = m_Request.Params["Source"];
            if (_paramUrl != null && _paramSource != null)
            {
                String _itemName = _paramUrl.Substring(_paramUrl.LastIndexOf("/") + 1);
                _paramSource = Globals.UrlDecode(_paramSource);
                foreach (SPList _list in _SPEEvalAttr.WebObj.Lists)
                {
                    if (_paramSource.EndsWith(_list.DefaultViewUrl))
                    {
                        _SPEEvalAttr.ListObj = _list;
                    }
                }
                if (_SPEEvalAttr.ListObj != null)
                {
                    SPListItem litem = null;
                    try
                    {
                        string sourceUrl = m_Request.QueryString["SourceUrl"];

                        if (_SPEEvalAttr.WebObj != null)
                        {
                            litem = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, sourceUrl, Utilities.SPUrlListItem);

                            if (litem != null)
                            {
                                _SPEEvalAttr.ItemObj = litem;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during DoSPEProcess get list item obj directly:", null, ex);
                    }

                    if (litem == null)
                    {
                        //use spquery for searching an item
                        try
                        {
                            SPQuery query = new SPQuery();
                            SPListItemCollection spListItems;
                            query.RowLimit = 2000; // Only select the top 2000.
                            query.ViewAttributes = "Scope=\"Recursive\"";
                            string format = "<Where><Eq><FieldRef Name=\"FileLeafRef\" /><Value Type=\"File\">{0}</Value></Eq></Where>";
                            query.Query = String.Format(format, _itemName);
                            spListItems = _SPEEvalAttr.ListObj.GetItems(query);
                            if (spListItems.Count > 0 && _SPEEvalAttr.ListObj.ItemCount > 0)
                                _SPEEvalAttr.ItemObj = spListItems[0];
						}
						catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Debug, "Exception during DoSPEProcess get list item use spquery:", null, ex);
                        }

                        //use loop as backup way
                        if (_SPEEvalAttr.ItemObj == null)
                        {
                            foreach (SPListItem _item in _SPEEvalAttr.ListObj.Items)
                            {
                                if (_item.Name.Equals(_itemName))
                                {
                                    _SPEEvalAttr.ItemObj = _item;
                                    break;
                                }
                            }
                        }
                    }

                    if (_SPEEvalAttr.ItemObj != null)
                    {
                        SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        _SPEEvalAttr.Action = "READ";
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;

                    }
                }
            }
            return false;
        }
    }

    public class SPE_DOWNLOAD_ASPX_Module : SPEModuleBase
    {
        public override bool DoSPEProcess()
        {
            bool bRet = false;
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();
            // it is "Send To" -> "Download a Copy".
            // URL Query: ?SourceUrl=/downloadsite1/doclib1/foo.txt&Source=http://sps2k7-01/downloadsite1/doclib1/Forms/AllItems.aspx&FldUrl=
            string sourceUrl = m_Request.QueryString["SourceUrl"];
            if (!String.IsNullOrEmpty(sourceUrl))
            {
                SPListItem litem = null;
                try
                {
                    if (_SPEEvalAttr.WebObj != null)
                    {
                        litem = (SPListItem)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, sourceUrl, Utilities.SPUrlListItem);

                        if (litem != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(litem, _SPEEvalAttr);
                            _SPEEvalAttr.Action = "SEND TO:DOWNLOAD A COPY";
                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during cannot get object from url directly:", null, ex);
				}
                if (litem == null)
				{
                    try
                    {
                        char[] slashChArr = { '/' };
                        string[] segments = sourceUrl.Split(slashChArr);
                        if (segments.Length >= 2)
                        {
                            string docLibName = segments[segments.Length - 2];
                            string docName = segments[segments.Length - 1];
                            // Try to find the list under this web.

                            //William delete this
                            //_SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.Lists[docLibName];
                            /***William add this for bug7724****/
                            //The reason for bug 7724 is that sub folder is not a list, so we must use the upper url
                            //to get a list object
                            try
                            {
                                _SPEEvalAttr.ListObj = _SPEEvalAttr.WebObj.GetList(sourceUrl);
                            }
                            catch
                            {
                            }
                            if (_SPEEvalAttr.ListObj == null)
                            {
                                int seg_offset = 2;
                                for (seg_offset = 2; _SPEEvalAttr.ListObj == null && seg_offset < segments.Length; seg_offset++)
                                {
                                    docLibName = segments[segments.Length - seg_offset];
                                    if (_SPEEvalAttr.ListObj == null)
                                    {
                                        //For bug 10341, the list should be get comparing it's RootFolderName
                                        foreach (SPList f in _SPEEvalAttr.WebObj.Lists)
                                        {
                                            if (f != null)
                                            {
                                                if (docLibName != null && docLibName.Equals(f.RootFolder.Name, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _SPEEvalAttr.ListObj = f;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // Check list type.
                            if (_SPEEvalAttr.ListObj != null && _SPEEvalAttr.ListObj.BaseType ==
                                SPBaseType.DocumentLibrary)
                            {
                                if (_SPEEvalAttr.ItemObj == null)
                                {
                                    //use spquery for searching an item
                                    SPQuery query = new SPQuery();
                                    SPListItemCollection spListItems;
                                    query.RowLimit = 2000; // Only select the top 2000.
                                    query.ViewAttributes = "Scope=\"Recursive\"";
                                    string format = "<Where><Eq><FieldRef Name=\"FileLeafRef\" /><Value Type=\"File\">{0}</Value></Eq></Where>";
                                    query.Query = String.Format(format, docName);
                                    spListItems = _SPEEvalAttr.ListObj.GetItems(query);
                                    if (spListItems.Count > 0 && _SPEEvalAttr.ListObj.ItemCount > 0)
                                    {
                                        foreach (SPListItem tmpSPListItem in spListItems)
                                        {
                                            if (sourceUrl.EndsWith(tmpSPListItem.Url, StringComparison.OrdinalIgnoreCase))
                                            {
                                                _SPEEvalAttr.ItemObj = tmpSPListItem;
                                                break;
                                            }
                                        }
                                        if (_SPEEvalAttr.ItemObj == null)
                                        {
                                            _SPEEvalAttr.ItemObj = spListItems[0];
                                        }


                                    }
                                    // Try to find item in list.
                                    //use the loop way for backup
                                    if (_SPEEvalAttr.ItemObj == null)
                                    {
                                        foreach (SPListItem item in _SPEEvalAttr.ListObj.Items)
                                        {
                                            if (item.Name == docName)
                                            {
                                                _SPEEvalAttr.ItemObj = item;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (_SPEEvalAttr.ItemObj != null)
                                {

                                    _SPEEvalAttr.Action = "SEND TO:DOWNLOAD A COPY";
                                    _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                    SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        bRet = true;
                        // No such doc lib exists under this web.
                        NLLogger.OutputLog(LogLevel.Debug, "Exception during DoSPEProcess:", null, ex);
                    }
                }
            }
            return bRet;
        }
    }

}
