using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.Common;
using Microsoft.SharePoint.Publishing.Internal.WebControls;


namespace Nextlabs.SPSecurityTrimming
{
    class SmtGridViewRowTrimmer : ITrimmer
    {
        private HttpContext Context;
        private GridViewRowCollection m_rows;
        private EvaluationMultiple m_mulEval;
        private List<SmtGridViewRowInfo> smtGridViewRowCache;

        private SPSite m_Site;
        private SPWeb m_Web;
        private SPList m_List;
        private string m_ListItemUrl;
        private bool m_bIgnoreTrimming;
        private bool m_bAnyAllowItem;

        public SmtGridViewRowTrimmer(HttpContext context, GridViewRowCollection rows)
        {
            Context = context;
            m_rows = rows;

            m_Site = null;
            m_List = null;
            m_ListItemUrl = null;

            m_Web = SPControl.GetContextWeb(Context);
            TrimmingEvaluationMultiple.NewEvalMult(m_Web, ref m_mulEval);
            smtGridViewRowCache = new List<SmtGridViewRowInfo>();
            m_bAnyAllowItem = false;
            m_bIgnoreTrimming = false;
        }
        public bool GetAnyAllowItem()
        {
            return m_bAnyAllowItem;
        }
        public bool DoTrimming()
        {            
            try
            {
                if (TrimmingEvaluationMultiple.IsPCConnected() && m_mulEval != null)
                {
                    foreach (GridViewRow row in m_rows)
                    {
                        string href = "";
                        foreach (TableCell cell in row.Cells)
                        {
                            href = ParseHrefFromTableCell(cell);

                            if (!String.IsNullOrEmpty(href))
                            {
                                break;
                            }
                        }

                        if (!String.IsNullOrEmpty(href))
                        {
                            MultipleEval(row, href); 
                        }
                    }

                    bool bRun = m_mulEval.run();
                    if (bRun)
                    {
                        SmtGridViewRowTrimming(); 
                    }

                    m_mulEval.ClearRequest();
                    m_mulEval = null;
                    return true;
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("SmtGridViewRowTrimmer::MultipleTrimming Exception: " + exp.Message);
            }

            return false;
        }

        public bool MultipleEval(GridViewRow rowControl, string script)
        {
            try
            {
                int idRequest = 0;
                string srcName = null;
                string[] srcAttr = null;

                m_Site = SPControl.GetContextSite(Context);
                m_Web = SPControl.GetContextWeb(Context);
                ParseScriptUrl(script);

                if (m_bIgnoreTrimming)
                    return false;

                Object evaTarget = null;
                SPHttpUrlParser parser = null;
                if (!String.IsNullOrEmpty(m_ListItemUrl))
                {
                    evaTarget = Utilities.GetCachedSPContent(m_Web, m_ListItemUrl, Utilities.SPUrlListItem);
                    if (evaTarget == null)
                    {
                        using (parser = new SPHttpUrlParser(m_ListItemUrl))
                        {
                            parser.Parse();
                            evaTarget = parser.ParsedObject;
                        }
                    }
                }
                else if (m_List != null)
                    evaTarget = m_List;
                else if (m_Web != null)
                    evaTarget = m_Web;

                if (evaTarget != null)
                {
                    string objUrl = NextLabs.Common.Utilities.ConstructSPObjectUrl(evaTarget);
                    SPWeb web = SPControl.GetContextWeb(Context);
                    string remoteAddress = Context.Request.UserHostAddress;
                    string userId = web.CurrentUser.LoginName;
                    EvaluationBase evaObj = EvaluationFactory.CreateInstance(evaTarget,
                        CETYPE.CEAction.Read, objUrl, remoteAddress, "SmtGridView Row Trimmer", m_Web.CurrentUser);
                    if (evaObj != null)
                    {
                        string guid = evaObj.ReConstructUrl();
                        bool bAllow = false;
                        bool bExisted = TrimmingEvaluationMultiple.QueryEvaluationResultCache(userId, remoteAddress, guid, ref bAllow);
                        if (bExisted)
                        {
                            if (!bAllow)
                            {
                                rowControl.Visible = false;
                            }
                            else
                            {
                                m_bAnyAllowItem = true;
                            }
                        }
                        else
                        {
                            Globals.GetSrcNameAndSrcAttr(evaTarget, objUrl, Context, ref srcName, ref srcAttr);
                            m_mulEval.SetTrimRequest(evaTarget, srcName, srcAttr, out idRequest);
                            SmtGridViewRowInfo info = new SmtGridViewRowInfo(rowControl, idRequest, guid);
                            smtGridViewRowCache.Add(info);
                        }
                    }
                }
            }

            catch (Exception exp)
            {
                Debug.WriteLine("SmtGridViewRowTrimmer::MultipleTrimming Exception: " + exp.Message);
            }

            return true;
        }

        private void ParseScriptUrl(string script)
        {
            if (!String.IsNullOrEmpty(script))
            {
                if (script.StartsWith("javascript: __doPostBack("))
                {
                    ParsePostBackScript(script);
                }
                else if (script.StartsWith("javascript:SmtEcbNavigateUrl("))
                {
                    ParseSmtEcbNavigateUrl(script);
                }
                else
                    m_bIgnoreTrimming = true;
            }
        }

        // Samples:
        // javascript: __doPostBack('ObjectList1','Goto$SPList:4435a4b0-ef67-4388-8252-65b627f1274f?SPWeb:9b602d1e-a72c-47bb-b2da-2d9a0cbc7a36:')
        // javascript: __doPostBack('ObjectList1','Goto$Area:?SPWeb:ad3db677-1df3-48be-ba92-88b5e6307db3:')
        private void ParsePostBackScript(string script)
        {
            string[] parameters = script.Split(new char[] { '\'', ',', '$', ':', '/', '?' }, StringSplitOptions.RemoveEmptyEntries);
            string webGuid = "";
            string listGuid = "";
            string folderGuid = "";

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] == "SPWeb")
                {
                    i++;
                    webGuid = parameters[i];
                }
                else if (parameters[i] == "SPList")
                {
                    i++;
                    listGuid = parameters[i];
                }
                else if (parameters[i] == "SPFolder")
                {
                    i++;
                    folderGuid = parameters[i];
                }
            }
            if (!String.IsNullOrEmpty(webGuid))
            {
                SPWeb web = m_Site.OpenWeb(new Guid(webGuid));
                if(web!=null)
                {
                    SPEEvalAttrs.Current().AddDisposeWeb(web);
                    if (!String.IsNullOrEmpty(listGuid))
                    {
                        m_List = web.Lists[new Guid(listGuid)];
                    }

                    if (!String.IsNullOrEmpty(folderGuid))
                    {
                        // Ignore trim folder here
                        //m_bIgnoreTrimming = true;
                        SPFolder folder = web.GetFolder(new Guid(folderGuid));
                        String mm = "";
                        if (web.ServerRelativeUrl.EndsWith("/"))
                            mm = web.ServerRelativeUrl + folder.Url;
                        else
                            mm = web.ServerRelativeUrl + "/" + folder.Url;
                        m_ListItemUrl = m_Site.MakeFullUrl(mm);
                    }
                    if (m_List == null || m_ListItemUrl == null)
                    {
                        m_Web = web;
                    }
                }
            }
            else
                m_bIgnoreTrimming = true; 
        }

        // Samples:
        // javascript:SmtEcbNavigateUrl('\u002fDocs\u002fTestDocLib2\u002fjames.garfield18.txt')
        // javascript:SmtEcbNavigateUrl('\u002fDocs\u002fLists\u002fAnnouncements\u002fDispForm.aspx?ID=5&Source=%2F%5Flayouts%2Fsitemanager%2Easpx%3FSmtContext%3DSPList%3A9c7b56c4%2Daddf%2D4d47%2Db8ab%2Df249d4864192%3FSPWeb%3Aafa33028%2D65dc%2D4a5c%2Da63c%2D98a32a8afa85%3A%26SmtContextExpanded%3DTrue%26Filter%3D1%26pgsz%3D100%26vrmode%3DFalse%26lvn%3DAll%20items')
        private void ParseSmtEcbNavigateUrl(string script)
        {
            string[] parameters = script.Split(new char[] { '\'' }, StringSplitOptions.RemoveEmptyEntries);
            string rawItemUrl = parameters[1];
            rawItemUrl = rawItemUrl.Replace("\\u0028", "(");
            rawItemUrl = rawItemUrl.Replace("\\u0029", ")");
            rawItemUrl = rawItemUrl.Replace("\\u002b", "+");
            rawItemUrl = rawItemUrl.Replace("\\u002527", "'");
            m_ListItemUrl = m_Site.MakeFullUrl(rawItemUrl.Replace("\\u002f", "/"));
        }

        private string ParseHrefFromTableCell(TableCell cell)
        {
            string href = "";

            if (!String.IsNullOrEmpty(cell.Text))
            {
                string key = "href=\"";
                int startPos = cell.Text.IndexOf(key);
                if (startPos > 0)
                {
                    int endPos = cell.Text.IndexOf("\"", startPos + key.Length);
                    if (endPos > 0)
                    {
                        href = cell.Text.Substring(startPos + key.Length, endPos - startPos - key.Length);
                    }
                }
            }

            return href;
        }

        private void SmtGridViewRowTrimming()
        {
            SPWeb web = SPControl.GetContextWeb(Context);
            string remoteAddress = Context.Request.UserHostAddress;
            string userId = web.CurrentUser.LoginName;
            DateTime evalTime = DateTime.Now;
            string guid = null;
            bool bAllow = true;
            foreach (SmtGridViewRowInfo cache in smtGridViewRowCache)
            {
                bAllow = m_mulEval.GetTrimEvalResult(cache.ID);
                guid = cache.guid;
                TrimmingEvaluationMultiple.AddEvaluationResultCache(userId, remoteAddress, guid, bAllow, evalTime);
                if (!bAllow)
                {                  
                    cache.rowControl.Visible = false;
                }
                else
                {                    
                    m_bAnyAllowItem = true;
                }
            }            
        }


    }
}
