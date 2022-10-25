using System;
using Microsoft.SharePoint;
using System.Security.Principal;
using NextLabs.Common;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Web;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    class ItemVersionControl
    {
        public static  PreContentAnalysisEventDelegate PreContentAnalysisEventHandler = null;
        public static  PostContentAnalysisEventDelegate PostCAFun = null;

        //on updating event, we just cancel the action according to conditions.
        //on updating event, we can't get listItem
        public static void OnItemAdding(SPItemEventProperties properties, SPWeb web, SPList list, SPListItem listitem, IPrincipal PrincipalUser, string clientIpAddr)
        {
            // George: do nothing in this.
        }

        //on ItemAdded
        public static void OnItemAdded(SPItemEventProperties properties, SPWeb web, SPList list, SPListItem listItem, IPrincipal PrincipalUser, string clientIpAddr)
        {
            try
            {
                if (listItem.File != null && listItem.File.Length == 0)
                {
                    // Do nothing for empty file.
                    return;
                }
                if (Globals.CheckListProperty(list, Globals.strLibraryProcessUploadPropName))
                {
                    ListItemContentAnalysis listItemCA = new ListItemContentAnalysis(listItem, clientIpAddr, PrincipalUser, false);
                    listItemCA.PostContentAnalysisFunc = PostCAFun;
                    listItemCA.Run();
                    bool bDenied = listItemCA.CADenied;
                    if (bDenied)
                    {
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = Globals.EnforcementMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during OnItemAdded:", null, ex);
            }
        }

        //on updating event, we just cancel the action according to conditions.
        public static void OnItemUpdating(SPItemEventProperties properties, SPWeb web, SPList list, SPListItem listitem, IPrincipal PrincipalUser, string clientIpAddr)
        {
            // George: do nothing in this.
        }

        public static void OnItemUpdated(SPItemEventProperties properties, SPWeb web, SPList list, SPListItem listItem, IPrincipal PrincipalUser, string clientIpAddr)
        {
            if (Globals.CheckListProperty(list, Globals.strLibraryProcessUploadPropName))
            {
                ListItemContentAnalysis listItemCA = new ListItemContentAnalysis(properties.ListItem, clientIpAddr, PrincipalUser, false);
                listItemCA.PostContentAnalysisFunc = PostCAFun;
                listItemCA.Run();
                if (listItemCA.CADenied)
                {
                    properties.Status = SPEventReceiverStatus.CancelWithError;
                    properties.ErrorMessage = Globals.EnforcementMessage;
                }
            }
        }
    }
}
