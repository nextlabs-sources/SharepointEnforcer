using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.SharePoint;
using NextLabs.Common;
using System.Web;
using System.Diagnostics;
using System.Collections;
using Microsoft.SharePoint.Administration;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public class stSPCMNData
    {
        public stSPCMNData()
        {
            strClassName = string.Empty;
            strClassValues = string.Empty;
            strCompareTo = string.Empty;
            strRecepients = string.Empty;
            strSubject = string.Empty;
            strBody = string.Empty;
            bSetParentSite = false;
            ALstrClassValues = new ArrayList();
            bComp2Top = false;


            strClassValue4Comp = string.Empty;//maybe top or parent
            nFinalIndex = -1;
        }
        //init data from obligation
        public string strClassName;
        public string strClassValues;
        public string strCompareTo;
        public string strRecepients;
        public string strSubject;
        public string strBody;
        public bool bSetParentSite;
        //processed data
        public bool bComp2Top;
        public ArrayList ALstrClassValues;
        public string strClassValue4Comp;//maybe top or parent
        public int nFinalIndex;
    }

    public class stRtData
    {
        public string m_strFile;
        public string m_strSite;
        public string m_strParentSite;
        public string m_strTopSite;

    }

    public class CNMWorker
    {

        public const string g_strCNMObligationName = "SP_CLASSIFICATION_MISMATCH_NOTIFICATION";

        private SPEReport m_Report;
        private string m_WebURL;
        private string m_UserName;
        private string m_UserSid;
        private SPUserToken m_UserToken;
        private string m_WebGuid;
        private string m_ClientIP;
        private bool m_bStartAtOnce;

        private int m_nUserID;
        List<stSPCMNData> m_lSPCMNData;
        stRtData m_ststrReportData;//report data, only record the parameters appears for the first time for batch mode.


        public CNMWorker(string webUrl, SPUserToken user, string webGuid, string ip, bool bStartAtOnce=false)
        {
            m_bStartAtOnce = bStartAtOnce;
            m_Report = null;
            m_WebURL = webUrl;
            m_UserToken = user;
            m_WebGuid = webGuid;
            m_ClientIP = ip;
            m_lSPCMNData = new List<stSPCMNData>();
            m_ststrReportData = new stRtData();
        }

        public CNMWorker(string webUrl, string userName, string userSid, string listGuid, string ip, string strID)
        {
            m_nUserID = int.Parse(strID);
            m_Report = null;
            m_WebURL = webUrl;
            m_UserName = userName;
            m_UserSid = userSid;
            m_UserToken = null;
            m_WebGuid = listGuid;
            m_ClientIP = ip;
            m_bStartAtOnce = false;
            m_lSPCMNData = new List<stSPCMNData>();
            m_ststrReportData = new stRtData();
        }

         /*nType: 1-[FILE], 2-[SITE], 3-[PARENTSITE], 4-[TOPSITE]
         *
         *
         */
        private void RecordParametersFirstTime(int nType, string strValue)
        {
            switch (nType)
            {
                case 1:
                    if (string.IsNullOrEmpty(m_ststrReportData.m_strFile))
                        m_ststrReportData.m_strFile = strValue;
                    break;
                case 2:
                    if (string.IsNullOrEmpty(m_ststrReportData.m_strSite))
                        m_ststrReportData.m_strSite = strValue;
                    break;
                case 3:
                    if (string.IsNullOrEmpty(m_ststrReportData.m_strParentSite))
                        m_ststrReportData.m_strParentSite = strValue;
                    break;
                case 4:
                    if (string.IsNullOrEmpty(m_ststrReportData.m_strTopSite))
                        m_ststrReportData.m_strTopSite = strValue;
                    break;
                default:
                    break;
            }
        }

        private string ReplaceKeywords(string strInput)
        {
            string strRet = strInput;
            strRet = strRet.Replace("[FILE]", m_ststrReportData.m_strFile);
            strRet = strRet.Replace("[SITE]", m_ststrReportData.m_strSite);
            strRet = strRet.Replace("[PARENTSITE]", m_ststrReportData.m_strParentSite);
            strRet = strRet.Replace("[TOPSITE]", m_ststrReportData.m_strTopSite);
            return strRet;
        }

        private bool RunWeb(SPWeb web, stSPCMNData cmndata)
        {
            bool bRet = false;
            try
            {
                string strState = Globals.GetSiteProperty(web, Globals.strSiteProcessStatePropName);
                if (string.IsNullOrEmpty(strState) || !strState.Equals(Globals.strSiteCNMStatePropValue_Processing))
                {
                    Globals.SetSiteProperty(web, Globals.strSiteProcessStatePropName, Globals.strSiteCNMStatePropValue_Processing);

                    string strClassName4Comp = cmndata.strClassName;
                    string strClassValue4Comp = string.Empty;

                    RecordParametersFirstTime(4, web.Url);//record topsite
                    int nIndexClassValue4Comp = -1;
                    if (cmndata.bComp2Top)//compare to top
                    {
                        strClassValue4Comp = cmndata.strClassValue4Comp;
                        if (string.IsNullOrEmpty(strClassValue4Comp))
                        {
                            Globals.SetSiteProperty(web, Globals.strSiteProcessStatePropName, Globals.strSiteCNMStatePropValue_Idle);
                            return false;
                        }

                        nIndexClassValue4Comp = cmndata.ALstrClassValues.IndexOf(strClassValue4Comp.ToLower());
                        if (nIndexClassValue4Comp <= 0)
                        {
                            Globals.SetSiteProperty(web, Globals.strSiteProcessStatePropName, Globals.strSiteCNMStatePropValue_Idle);
                            return false;
                        }
                    }
                    else //compare to parent
                    {
                        strClassValue4Comp = Globals.GetSiteProperty(web, strClassName4Comp);
                        if (!string.IsNullOrEmpty(strClassValue4Comp))
                        {
                            nIndexClassValue4Comp = cmndata.ALstrClassValues.IndexOf(strClassValue4Comp.ToLower());
                        }
                    }

                    int nFinalIndex = nIndexClassValue4Comp;
                    if (web.Webs.Count > 0)
                    {
                        RecordParametersFirstTime(3, web.Url);//record parent site
                        foreach (SPWeb spw in web.Webs)
                        {
                            try
                            {
                                RunWeb(spw, cmndata);
                            }
                            catch
                            {
                            }
                            finally
                            {
                                spw.Dispose();
                            }
                        }
                    }

                    if (nIndexClassValue4Comp > 0)
                    {
                        if (web.Lists.Count > 0)
                        {
                            RecordParametersFirstTime(2, web.Url);// record web
                            foreach (SPList list in web.Lists)
                            {
                                bRet = true;
                                for (int i = nIndexClassValue4Comp - 1; i >= 0; i--)
                                {
                                    string fieldValue = cmndata.ALstrClassValues[i].ToString();
                                    if (CheckListFieldAndReport(list, strClassName4Comp, fieldValue))
                                    {
                                        if (i < nFinalIndex) nFinalIndex = i;
                                    }
                                }
                            }
                        }

                        if (nFinalIndex < nIndexClassValue4Comp)
                        {//need to set parent web to nFinalIndex
                            string strParentValue = Globals.GetSiteProperty(web, strClassName4Comp).ToLower();
                            int nParentIndex = cmndata.ALstrClassValues.IndexOf(strParentValue);
                            if (cmndata.bSetParentSite && (nParentIndex > nFinalIndex || nParentIndex == -1))
                            {
                                Globals.SetSiteProperty(web, strClassName4Comp, cmndata.ALstrClassValues[nFinalIndex].ToString().ToLower());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, $"Exception,url:{ web.Url} ,during RunWeb:" , null, ex);
            }
            finally
            {
                Globals.SetSiteProperty(web, Globals.strSiteProcessStatePropName, Globals.strSiteCNMStatePropValue_Idle);
            }

            return bRet;
        }

        private bool CheckListFieldAndReport(SPList list, string fieldName, string fieldValue)
        {
            bool bRet = false;
            try
            {
                SPField field = GetSPField(list, fieldName);
                if (field != null)
                {
                    SPQuery query = new SPQuery();
                    SPListItemCollection spListItems;
                    query.ViewAttributes = "Scope=\"Recursive\"";
                    string format = "<Where><Eq><FieldRef Name={0} /><Value Type=\"text\">{1}</Value></Eq></Where>";////text?
                    query.Query = String.Format(format, field.InternalName, fieldValue);
                    spListItems = list.GetItems(query);
                    if (spListItems != null && spListItems.Count > 0) // have some mismatch
                    {
                        bRet = true;
                        RecordParametersFirstTime(1, spListItems[0].Url);//record file
                        SPBaseType baseType = list.BaseType;
                        foreach (SPListItem item in spListItems)
                        {
                            if (baseType == SPBaseType.DocumentLibrary)
                            {
                                WriteReportRecord(list.ParentWeb.Url + "/" + item.Url, fieldValue);
                            }
                            else //processing for list
                            {
                                string listItemUrl = string.Empty;
                                string defaultViewUrl = list.DefaultViewUrl;
                                int lastSlashIndex = defaultViewUrl.LastIndexOf('/');
                                if (lastSlashIndex != -1)
                                {
                                    listItemUrl = list.ParentWeb.Site.MakeFullUrl(defaultViewUrl.Remove(lastSlashIndex)) + '/' + item.Title;
                                }
                                WriteReportRecord(listItemUrl, fieldValue);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, $"Exception,after query url:{ list.RootFolder.ServerRelativeUrl} ,during CheckListFieldAndReport:", null, ex);
            }
            return bRet;
        }

        private SPField GetSPField(SPList list, string fieldName)
        {
            SPField filed = null;
            try
            {
                if (list.Fields.ContainsField(fieldName))
                {
                    filed = list.Fields.GetField(fieldName);
                }
            }
            catch
            { }
            return filed;
        }

        private bool GetCMNObligation(SPWeb web)
        {
            SPUser user = null;
            if (m_nUserID > 0 && !m_bStartAtOnce)
            {
                using (SPSite site = Globals.GetValidSPSite(m_WebURL, m_UserToken, HttpContext.Current))
                {
                    using (SPWeb w = site.OpenWeb())
                    {
                    	user = w.AllUsers.GetByID(m_nUserID);
                    }
                }
            }

            SPWebEvaluation evaluator = null;
            if (m_bStartAtOnce)
            {
                evaluator = new SPWebEvaluation(web, CETYPE.CEAction.Upload, web.Url, m_ClientIP, "Classification Notification", web.CurrentUser);
            }
            else
            {
                evaluator = new SPWebEvaluation(web, CETYPE.CEAction.Upload, web.Url, m_ClientIP, "Classification Notification", user);
            }
            evaluator.CheckCache = false;
            evaluator.Run();
            List<Obligation> obList = evaluator.GetObligations();
            try
            {
                if (obList != null)
                {
                    foreach (Obligation ob in obList)
                    {
                        if (ob.Name.Equals(g_strCNMObligationName))
                        {
                            stSPCMNData obCMN = new stSPCMNData();
                            foreach (var attr in ob.Attributes)
                            {
                                switch (attr.Key)
                                {
                                    case "Classification Name":
                                        obCMN.strClassName = attr.Value;
                                        break;
                                    case "Classification Values":
                                        if (!string.IsNullOrEmpty(attr.Value))
                                        {
                                            obCMN.strClassValues = attr.Value;
                                            string[] strArr = attr.Value.Split(';');
                                            for (int i = 0; i < strArr.Length; i++)
                                            {
                                                if (!string.IsNullOrEmpty(strArr[i]))
                                                {
                                                    obCMN.ALstrClassValues.Add(strArr[i].ToLower());
                                                }
                                            }
                                        }
                                        break;
                                    case "Compare with":
                                        obCMN.strCompareTo = attr.Value;
                                        if (attr.Value.Equals("Parent Site"))
                                        {
                                            obCMN.bComp2Top = false;
                                            obCMN.strClassValue4Comp = string.Empty;
                                        }
                                        else
                                        {
                                            obCMN.bComp2Top = true;
                                            obCMN.strClassValue4Comp = Globals.GetSiteProperty(web, obCMN.strClassName);
                                        }

                                        break;
                                    case "Recipients":
                                        obCMN.strRecepients = attr.Value;
                                        obCMN.strRecepients.Replace(';', ',');
                                        break;
                                    case "Email Subject":
                                        obCMN.strSubject = attr.Value;
                                        break;
                                    case "Email Body":
                                        obCMN.strBody = attr.Value;
                                        break;
                                    case "Set Parent Site Classification":
                                        obCMN.bSetParentSite = attr.Value.Equals("Yes");
                                        break;

                                    default:
                                        break;
                                }
                            }
                            obCMN.nFinalIndex = -1; // Just init it.
                            m_lSPCMNData.Add(obCMN);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, $"Exception while get obligations:", null, ex);
            }

            return m_lSPCMNData.Count > 0;
        }

        private void WriteReportRecord(string strUrl, string strClassValue)
        {
            List<string> lTemp = new List<string>();
            lTemp.Add(strUrl);
            lTemp.Add(strClassValue);
            m_Report.WriteToCSVFile(lTemp);
        }

        public void WorkerRun(object obj)
        {
            using (SPSite site = Globals.GetValidSPSite(m_WebURL, m_UserToken, HttpContext.Current))
            {
                AutoResetEvent autoEvent = null;
                using (SPWeb web = site.OpenWeb())
                {
                    try
                    {
                        autoEvent = obj as AutoResetEvent;
                        if (GetCMNObligation(web))
                        {
                            foreach (stSPCMNData cmndata in m_lSPCMNData)
                            {
                                SPSecurity.RunWithElevatedPrivileges(delegate()
                                {
                                    m_Report = new SPEReport(web);
                                    m_Report.OpenOrCreateReport();
                                    WriteReportRecord('[' + web.Url + ']', Globals.GetSiteProperty(web, cmndata.strClassName));
                                    string strAdditionalReceivers = string.Empty;
                                    strAdditionalReceivers = Globals.GetFullControlUsersEmail(web);
                                    m_Report.InitReportHeader(cmndata.strClassName, cmndata.strClassValues, cmndata.strCompareTo, cmndata.bSetParentSite, strAdditionalReceivers + ',' + cmndata.strRecepients);
                                });
                                RunWeb(web, cmndata);
                                if (m_Report != null)
                                {
                                    m_Report.FinishReport(ReplaceKeywords(cmndata.strSubject), ReplaceKeywords(cmndata.strBody));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        NLLogger.OutputLog(LogLevel.Debug, "Exception in worker run:" + m_WebURL, null, ex);
                    }
                    finally
                    {
                        autoEvent.Set();
                    }
                }
            }
        }
    }
}
