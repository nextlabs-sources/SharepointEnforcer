using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using System.Web.UI.WebControls.WebParts;

namespace NextLabs.Common
{
    public class EvaluationFactory
    {
        public static EvaluationBase CreateInstance(Object obj, CETYPE.CEAction action, String url, String host, String module, SPUser user)
        {
            if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                return new SPListItemEvaluation(item, action, url, host, module, user);
            }
            else if (obj is SPList)
            {
                SPList list = obj as SPList;
                return new SPListEvaluation(list, action, url, host, module, user);
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                return new SPListEvaluation(view.ParentList, action, url, host, module, user);
            }
            else if (obj is SPWeb)
            {
                SPWeb web = obj as SPWeb;
                return new SPWebEvaluation(web, action, url, host, module, user);
            }

            return null;
        }

        public static EvaluationBase CreateInstance(Object obj, CETYPE.CEAction action, String url, String host, String module, String username, String userSid)
        {
            if (obj is SPListItem)
            {
                SPListItem item = obj as SPListItem;
                return new SPListItemEvaluation(item, action, url, host, module, username, userSid);
            }
            else if (obj is SPList)
            {
                SPList list = obj as SPList;
                return new SPListEvaluation(list, action, url, host, module, username, userSid);
            }
            else if (obj is SPView)
            {
                SPView view = obj as SPView;
                return new SPListEvaluation(view.ParentList, action, url, host, module, username, userSid);
            }
            else if (obj is SPWeb)
            {
                SPWeb web = obj as SPWeb;
                return new SPWebEvaluation(web, action, url, host, module, username, userSid);
            }

            return null;
        }
    }
}
