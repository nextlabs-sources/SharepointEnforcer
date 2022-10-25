using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NextLabs.CSCInvoke;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
namespace NextLabs.Common
{
    public class EvaluatorProperties
    {
        public const string CacheHintIndicationKey = "ce::request_cache_hint";

        static Dictionary<string, string> FixedAttrNameMap = new Dictionary<string,string>();

        static private void Init()
        {
            if (FixedAttrNameMap.Count == 0)
            {
                FixedAttrNameMap.Add("vti_title", CETYPE.CEAttrKey.CE_ATTR_SP_TITLE);
            }
        }


        public EvaluatorProperties()
        {
        }

        public void ConstructForItem(SPListItem item, ref List<KeyValuePair<string, string>> properties)
        {
            EvaluatorProperties.Init();

            String objName = "";
            String objTitle = "";

            try
            {
                objName = item.Name;
            }
            catch (Exception)
            {
            }

            if (String.IsNullOrEmpty(objName))
            {
                try
                {
                    objName = item.DisplayName;
                }
                catch (Exception)
                {
                }
            }

            try
            {
                objTitle = item.Title;
            }
            catch (Exception)
            {
            }

            if (String.IsNullOrEmpty(objName))
            {
                objName = objTitle;
            }
            if (String.IsNullOrEmpty(objTitle))
            {
                objTitle = objName;
            }

            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_NAME, objName);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_TITLE, objTitle);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE, CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_ITEM);
            properties.Add(keyValue);

            if (item.ParentList.BaseType == SPBaseType.DocumentLibrary)
                keyValue = new KeyValuePair<string, string>(
                    CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM);
            else
                keyValue = new KeyValuePair<string, string>(
                    CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CacheHintIndicationKey, "yes");
            properties.Add(keyValue);

            string size = Globals.GetItemFileSizeStr(item);
            if (size.Equals("0")) size = "1";
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_FILE_SIZE, size);
            properties.Add(keyValue);

            RetrieveAttrsFromItemProperties(item.Properties, item.ParentList.Fields, ref properties);
            RetrieveAttrsFromItemFields(item, ref properties);

            #region Ensure created_by and modified_by is correct;
            //Step 1: Get created_by and modified_by from SPItem
            string created_by = Globals.GetItemCreatedBySid(item);
            string modified_by = Globals.GetItemModifiedBySid(item);

            //Step 2: Try get "document modified by" and "document created by" from ItemFields
            string docCreatedBy = null, docModifiedBy = null;
            int iDocCreatedBy = -1, iDocModifiedBy = -1;
            for (int i = 0; i < properties.Count; i++)
            {
                if (iDocCreatedBy >= 0 && iDocModifiedBy >= 0)
                {
                    break;
                }
                var keyValuePair = properties[i];
                if (keyValuePair.Key.Equals("document created by", StringComparison.OrdinalIgnoreCase))
                {
                    docCreatedBy = keyValuePair.Value;
                    iDocCreatedBy = i;
                }
                else if (keyValuePair.Key.Equals("document modified by", StringComparison.OrdinalIgnoreCase))
                {
                    docModifiedBy = keyValuePair.Value;
                    iDocModifiedBy = i;
                }
            }

            //Step 3: if created_by is not valid, try get sid from "document created by"
            if (iDocCreatedBy != -1 && !string.IsNullOrEmpty(docCreatedBy))
            {
                if (docCreatedBy.EndsWith(created_by, StringComparison.OrdinalIgnoreCase))
                {
                    string sid = UserSid.GetUserSid(item.Web, docCreatedBy);
                    created_by = sid;
                }
            }
            properties.Add(new KeyValuePair<string, string>(CETYPE.CEAttrKey.CE_ATTR_SP_CREATED_BY, created_by));

            //Step 4: if modified_by is not valid, try get sid from "document modified by"
            if (iDocModifiedBy != -1 && !String.IsNullOrEmpty(docModifiedBy))
            {
                if (docModifiedBy.EndsWith(modified_by, StringComparison.OrdinalIgnoreCase))
                {
                    string sid = UserSid.GetUserSid(item.Web, docModifiedBy);
                    modified_by = sid;
                }
            }
            properties.Add(new KeyValuePair<string, string>(CETYPE.CEAttrKey.CE_ATTR_SP_MODIFIED_BY, modified_by));
            #endregion
        }

        public void ConstructForList(SPList list, ref List<KeyValuePair<string, string>> properties)
        {
            EvaluatorProperties.Init();

            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_NAME, list.Title);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_TITLE, list.Title);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_DESC, list.Description);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE, CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET);
            properties.Add(keyValue);

            if (list.BaseType != SPBaseType.DocumentLibrary)
                keyValue = new KeyValuePair<string, string>(
                    CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST);
            else
                keyValue = new KeyValuePair<string, string>(
                    CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CacheHintIndicationKey, "yes");
            properties.Add(keyValue);
        }

        public void ConstructForWeb(SPWeb web, ref List<KeyValuePair<string, string>> properties)
        {
            EvaluatorProperties.Init();

            string webName = web.Name;
            string webTitle = web.Title;
            if (String.IsNullOrEmpty(webName))
                webName = webTitle;
            if (String.IsNullOrEmpty(webTitle))
                webTitle = webName;
            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_NAME, webName);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_TITLE, webTitle);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_DESC, web.Description);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE, CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_SITE);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_SITE);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CacheHintIndicationKey, "yes");
            properties.Add(keyValue);
        }

        public void ConstructForWebPart(Control webpart, SPWeb web, ref List<KeyValuePair<string, string>> properties)
        {
            EvaluatorProperties.Init();
            System.Web.UI.WebControls.WebParts.WebPart WebPart = webpart as System.Web.UI.WebControls.WebParts.WebPart;
            SPList list = null;

            if (WebPart is ListViewWebPart)
            {
                ListViewWebPart lvwp = WebPart as ListViewWebPart;
                list = web.Lists[new Guid(lvwp.ListName)];
            }
            else if (WebPart is XsltListViewWebPart)
            {
                XsltListViewWebPart xsltlvWP = WebPart as XsltListViewWebPart;
                list = web.Lists[new Guid(xsltlvWP.ListName)];
            }
            KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_NAME, WebPart.Title);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_TITLE, WebPart.Title);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_DESC, WebPart.Description);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_TYPE, CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET);
            properties.Add(keyValue);
            keyValue = new KeyValuePair<string, string>(
                CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, "webpart");
            properties.Add(keyValue);

            if (list != null)
            {
                if (list.BaseType != SPBaseType.DocumentLibrary)
                    keyValue = new KeyValuePair<string, string>(
                        CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST);
                else
                    keyValue = new KeyValuePair<string, string>(
                        CETYPE.CEAttrKey.CE_ATTR_SP_RESOURCE_SUBTYPE, CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY);
                properties.Add(keyValue);
            }

            keyValue = new KeyValuePair<string, string>(
                "webpart type", WebPart.GetType().ToString());
            properties.Add(keyValue);

            string wpid = "";
            string wpid_guid = "";
            if (WebPart.ID != null)
            {
                wpid = WebPart.ID;
                try
                {
                    wpid_guid = wpid.Substring(2).Replace("_", "-");
                    Guid wpguid = new Guid(wpid_guid);
                    wpid = wpid_guid;
                }
                catch (Exception)
                {
                }
            }

            keyValue = new KeyValuePair<string, string>(
                "webpart id", wpid);
            properties.Add(keyValue);

            keyValue = new KeyValuePair<string, string>(
                CacheHintIndicationKey, "yes");
            properties.Add(keyValue);
        }

        private string AttrNameMapConvert(string attr, SPFieldCollection fields)
        {
            if (FixedAttrNameMap.ContainsKey(attr))
            {
                // This attr is a fixed attrs.  Use the hard-coded name
                // supplied by the SDK.
                return FixedAttrNameMap[attr];
            }
            else
            {
                // This attr is a custom attr.  Since SPFieldConnection can
                // only be indexed by display name but not internal name, and
                // we want to convert internal name to display name, we have
                // to do a brute-force search through the fields using the
                // internal name.
                foreach (SPField f in fields)
                {
                    if (f.InternalName == attr)
                    {
                        // A field with matching internal name is found.
                        return f.Title;
                    }
                }

                return attr;
            }
        }

        public void RetrieveAttrsFromItemProperties(Hashtable itemProperties, SPFieldCollection listFields, ref List<KeyValuePair<string, string>> properties)
        {
            foreach (DictionaryEntry de in itemProperties)
            {
                if (de.Value != null && !string.IsNullOrEmpty(de.Value.ToString()))
                {
                    string key = AttrNameMapConvert(de.Key.ToString(), listFields);
                    //if prefilterResList !=null,means prefilter is enable
                    if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(key.ToLower()))
                    {
                        continue;
                    }
                    string value = de.Value.ToString();
                    if (key.Equals("Created", StringComparison.OrdinalIgnoreCase)
                        || key.Equals("Modified", StringComparison.OrdinalIgnoreCase))
                    {
                        value = EvaluatorProperties.ConvertTime(value);
                    }

                    if (key.Equals("file size", StringComparison.OrdinalIgnoreCase)
                        && value.Equals("0"))
                    {
                        value = "1";
                    }

                    KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(key, value);
                    properties.Add(keyValue);
                }
            }
        }

        public void RetrieveAttrsFromItemFields(SPListItem item, ref List<KeyValuePair<string, string>> properties)
        {
            SPFieldCollection fieldCollection = item.Fields;
            foreach (SPField f in fieldCollection)
            {
                object attr_obj = null;
                try
                {
                    attr_obj=item[f.InternalName];
                }
                catch (Exception)
                {
                }
                if(attr_obj==null)
                    continue;
                //if prefilterResList !=null,means prefilter is enable
                if (SPEEvalAttrs.prefilterResList != null && !SPEEvalAttrs.prefilterResList.Contains(f.Title.ToLower()))
                {
                    continue;
                }
                string attr_value = null;
                string attr_name = f.ToString().ToLower();
                if (attr_obj != null)
                    attr_value = attr_obj.ToString().ToLower();
                if (!string.IsNullOrEmpty(attr_value))
                {
                    if (f.Type == SPFieldType.Calculated || f.Type == SPFieldType.Lookup)
                    {
                        int index = attr_value.IndexOf(";#");
                        if (index != -1)
                        {
                            index += 2;
                            attr_value = attr_value.Substring(index, attr_value.Length - index);
                        }
                    }
                    else if (f.Type == SPFieldType.URL)
                    {
                        int index = attr_value.IndexOf(", ");
                        if (index != -1)
                        {
                            index += 2;
                            attr_value = attr_value.Substring(index, attr_value.Length - index);
                        }
                    }
                    else if (f.Type == SPFieldType.Invalid && f.TypeDisplayName.Equals("Managed Metadata", StringComparison.OrdinalIgnoreCase))
                    {
                        int index = attr_value.LastIndexOf("|");
                        if (index != -1)
                        {
                            attr_value = attr_value.Substring(0, index);
                        }
                    }
                    if (attr_name == "created"
                        || attr_name == "modified")
                    {
                        attr_value = EvaluatorProperties.ConvertTime(attr_value);
                    }

                    //Fix bug 8968, convert all "description" to "desc", Addeb by William 20090323
                    if (attr_name == "description")
                        attr_name = "desc";
                    //added by chellee for the bug 8998
                    if ((f.FieldTypeDefinition != null) && (f.FieldTypeDefinition.TypeDisplayName.ToString() == "Person or Group"))
                    {
                        if (attr_value.IndexOf(";#") != -1)
                        {
                            string[] arr = attr_value.Split('#');
                            attr_value = arr[1];
                        }
                    }

                    if (attr_name.Equals("file size", StringComparison.OrdinalIgnoreCase)
                        && attr_value.Equals("0"))
                    {
                        attr_value = "1";
                    }

                    KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(attr_name, attr_value);
                    properties.Add(keyValue);
                }
            }
        }

        public static string ConvertTime(string orignal)
        {
            DateTime past = DateTime.Now;
            try
            {
                past = DateTime.Parse(orignal);
            }
            catch
            {
                try
                {
                    past =new DateTime(long.Parse(orignal));
                }
                catch
                {
                }
            }
            
            DateTime time = past.ToUniversalTime();
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc);

            double span = (time - refTime).TotalMilliseconds;

            if (span >= 0.0)
            {
                // Round down to the millisecond.
                return ((UInt64)span).ToString();
            }
            else
            {
                // Return the earliest time that we can.
                return "0";
            }
        }

        /* Returns a string representing the number of milliseconds since
         * 1/1/1970 00:00:00 UTC */
        static public string ConvertDataTime2Str(DateTime date)
        {
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0,
                                            DateTimeKind.Utc);

            double span = (date - refTime).TotalMilliseconds;

            if (span >= 0.0)
            {
                // Round down to the millisecond.
                return ((UInt64)span).ToString();
            }
            else
            {
                // Return the earliest time that we can.
                return "0";
            }
        }
    }
}

