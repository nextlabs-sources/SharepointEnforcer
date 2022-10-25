using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;
using Microsoft.SharePoint;
using NextLabs.Common;
using System.Security.Principal;
using System.Linq;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    /// <summary>
    /// Provides methods to trap events that occur for lists.
    /// These registered entities is notified of list schema events,
    /// supporting list schema operations and the addition or removal of
    /// content types.
    /// </summary>
    public class ListHandler : SPListEventReceiver
    {
        #region Constructor
        private HttpContext _currentContext;
        public ListHandler()
        {
            _currentContext = HttpContext.Current;
        }
        #endregion

        #region FieldAddingWithEnforcer
        /// <summary>
        /// Occurs when adding a field to a List.
        /// </summary>
        private bool EnforceFieldAdding(SPListEventProperties properties, ref string ErrorMessage)
        {
            string loginName = properties.Web.CurrentUser.LoginName;
            IPrincipal PrincipalUser = null;
            string clientIpAddr = WebRemoteAddressMap.GetRemoteAddress(loginName, properties.Web.Url, ref PrincipalUser);

            #region add prefilter
            string action = Globals.SPECommon_ActionConvert(CETYPE.CEAction.Write);
            var noMatch = PolicyEngineModule.PrefilterEngineMatch(properties.Web, PrincipalUser, loginName, action);
            if (noMatch)
            {
                //donnot query pc,
                NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                return true;
            }
            #endregion

            string sid = properties.Web.CurrentUser.Sid;
            SPList list = properties.List;
            SPBaseType baseType = list.BaseType;
            string[] emptyArray = new string[0];
            string policyName = null;
            string policyMessage = null;
            CETYPE.CEResponse_t response = CETYPE.CEResponse_t.CEAllow;

            if (baseType == SPBaseType.UnspecifiedBaseType)
            {
                NLLogger.OutputLog(LogLevel.Debug, "FieldAdding: Unsupported list type: " + baseType);
                // Always ALLOW.
            }
            else
            {
                // Library -> Settings -> Create Column
                // List -> ettings -> Create Column
                string listUrl = Globals.ConstructListUrl(properties.Web, list);

                string[] propertyArray = new string[5 * 2];
                // We use the list title as the name.
                propertyArray[0 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_NAME;
                propertyArray[0 * 2 + 1] = list.Title;
                propertyArray[1 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_TITLE;
                propertyArray[1 * 2 + 1] = list.Title;
                propertyArray[2 * 2 + 0] = CETYPE.CEAttrKey.CE_ATTR_SP_DESC;
                propertyArray[2 * 2 + 1] = list.Description;
                propertyArray[3 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_RESOURCE_TYPE;
                propertyArray[3 * 2 + 1] = CETYPE.CEAttrVal.
                    CE_ATTR_SP_TYPE_VAL_PORTLET;
                propertyArray[4 * 2 + 0] = CETYPE.CEAttrKey.
                    CE_ATTR_SP_RESOURCE_SUBTYPE;
                propertyArray[4 * 2 + 1] =
                    (baseType == SPBaseType.DocumentLibrary ?
                     CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY :
                     CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST);

                // Call eval.

                response = Globals.CallEval(CETYPE.CEAction.Write,
                                            listUrl,
                                            null,
                                            ref propertyArray,
                                            ref emptyArray,
                                            clientIpAddr,
                                            loginName,
                                            sid,
                                            ref policyName,
                                            ref policyMessage,
                                            null,
                                            null,
                                            Globals.EventHandlerName,
                                            CETYPE.CENoiseLevel_t.CE_NOISE_LEVEL_USER_ACTION,
                                            properties.Web,
                                            PrincipalUser);
            }

            if (response == CETYPE.CEResponse_t.CEAllow) return true;
            else
            {
                ErrorMessage = NextLabs.Common.Utilities.GetDenyString(policyName, policyMessage);
                return false;
            }
        }

        public override void FieldAdding(SPListEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;

            string ErrorMessage = "";
            //TO check if it's sharepoint interal field.
            bool isBuildIn = properties.FieldName.Equals("_dlc_DocId") || properties.FieldName.Equals("_dlc_DocIdUrl")
                || properties.FieldName.Equals("_dlc_DocIdPersistId") || properties.FieldName.Equals("Title");
            if (isBuildIn || EnforceFieldAdding(properties, ref ErrorMessage))
            {
                base.FieldAdding(properties);
            }
            else
            {
                properties.Status = SPEventReceiverStatus.CancelWithError;
                properties.ErrorMessage = ErrorMessage;
            }

            this.EventFiringEnabled = true;
        }
        #endregion

        #region OtherListFieldEvents
        /// <summary>
        /// Log the other List Events type and information. No need to enforce them now.
        /// </summary>
        public override void FieldUpdating(SPListEventProperties properties)
        {
            var args = new EventHandlerEventArgs(properties, _currentContext);
            EventHelper.Instance.OnBeforeEventExecuting(this, args);
            if (args.Cancel)
                return;

            this.EventFiringEnabled = false;

            string ErrorMessage = "";
            //TO check if it's sharepoint interal field.
            bool isBuildIn = properties.FieldName.Equals("_dlc_DocId") || properties.FieldName.Equals("_dlc_DocIdUrl")
                || properties.FieldName.Equals("_dlc_DocIdPersistId") || properties.FieldName.Equals("Title");
            if (isBuildIn || EnforceFieldAdding(properties, ref ErrorMessage))
            {
                base.FieldUpdating(properties);
            }
            else
            {
                properties.Status = SPEventReceiverStatus.CancelWithError;
                properties.ErrorMessage = ErrorMessage;
            }

            this.EventFiringEnabled = true;

        }
        #endregion
    }
}
