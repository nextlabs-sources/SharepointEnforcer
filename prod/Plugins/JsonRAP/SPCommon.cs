using Microsoft.SharePoint;

using NextLabs.Diagnostic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonRAP
{
    static class SPCommon
    {
		public static SPField MyGetSPField(SPFieldCollection spFields, string strFieldName)
		{
			SPField spFiledRet = null;
			try
			{
				if (null == spFields)
				{
					// Ignore
				}
				else
				{
					if (spFields.ContainsField(strFieldName))
					{
						spFiledRet = spFields.GetField(strFieldName);
					}
					else
					{
						// Field not exist
					}
				}
			}
			catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Debug, "Exception during get filed:[{0}] from list:[{1}]\n", new object[] { strFieldName, spFields?.List.Title }, ex);
			}
			return spFiledRet;
		}

		public static bool AddFieldStringValueBySPListItem(SPListItem spListItem, string strColumnName, string strColumnValue)
        {
			bool bRet = false;
			try
			{
				if ((null == spListItem) || (null == spListItem.Fields) || String.IsNullOrEmpty(strColumnName) || String.IsNullOrEmpty(strColumnValue))
				{
					NLLogger.OutputLog(LogLevel.Error, "Parameters error SPListItem:[{0}] during add field:[{1}] value:[{2}], please check\n", new object[] { spListItem, strColumnName, strColumnValue });
				}
				else
				{
					SPField spField = MyGetSPField(spListItem.Fields, strColumnName);
					if (null == spField)
					{
						// Column not exist
						NLLogger.OutputLog(LogLevel.Error, "Try to add column value but the column not exist, SPListItem:[{0}] field:[{1}] value:[{2}], please check\n", new object[] { spListItem, strColumnName, strColumnValue });
					}
					else
					{
						try
						{
							spField.ParseAndSetValue(spListItem, strColumnValue);
						}
						catch (Exception ex)
						{
							NLLogger.OutputLog(LogLevel.Error, "Exception during parse and set value into field:[{0}] value:[{1}]\n", new object[] { strColumnName, strColumnValue }, ex);
						}
						bRet = true;
					}
				}
			}
            catch (Exception ex)
			{
                NLLogger.OutputLog(LogLevel.Error, "Exception during add field:[{0}] value:[{1}]\n", new object[] { strColumnName, strColumnValue }, ex);
            }
            return bRet;
        }

        public static void AddFieldStringValueBySPItemEventProperties(SPItemEventProperties spItemEventProperties, string strColumnName, string strColumnValue)
        {
            try
			{
				spItemEventProperties.AfterProperties[strColumnName] = strColumnValue;
			}
			catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Error, "Exception during add field:[{0}] value:[{1}] by item event properties\n", new object[] { strColumnName, strColumnValue }, ex);
			}
		}

        public static string GetFieldStringValueFromListItem(SPListItem spListItem, string strColumnName, string strDefaultValue)
        {
            string strFieldValue = strDefaultValue;
            try
			{
				if ((null == spListItem) || String.IsNullOrEmpty(strColumnName))
				{
					NLLogger.OutputLog(LogLevel.Error, "Parameters error during get field:[{0}] value from list:[{1}] , please check\n", new object[] { strColumnName, spListItem });
				}
				else
				{
					SPField spField = MyGetSPField(spListItem.Fields, strColumnName);
					if (null == spField)
					{
						// Column not exist
						NLLogger.OutputLog(LogLevel.Error, "Try to get column value but the column not exist, SPListItem:[{0},{1}] field:[{2}], please check\n", new object[] { spListItem, spListItem.Name, strColumnName });
					}
					else
					{
						try
						{
							var obField = spListItem[strColumnName];
							if (null == obField)
							{
								NLLogger.OutputLog(LogLevel.Debug, "Current column:[{0}] do not exist in list item:[{1},{2}], using default value\n", new object[] { strColumnName, spListItem, spListItem.Name, strDefaultValue });
							}
							else
							{
								strFieldValue = obField.ToString();
							}
						}
						catch (Exception ex)
						{
							NLLogger.OutputLog(LogLevel.Error, "Failed to get field:[{0}] in item:[{1}], Message:[{2}]\n", new object[] { strColumnName, spListItem.Name, ex.Message });
						}
					}
				}
			}
            catch (Exception ex)
			{
                NLLogger.OutputLog(LogLevel.Error, "Exception during to get column value in SPListItem:[{0},{1}] field:[{2}]\n", new object[] { spListItem, (null==spListItem ? "" : spListItem.Name), strColumnName }, ex);
            }
            return strFieldValue;
        }

        public static bool EstablishAndUpdateSpecifyField(SPFieldCollection spFields, string strColumnName, bool bNeedWithPrivilege, bool bReadOnly, bool bReadOnlyEnforced, bool bHidden,
            bool bShowInDisplayForm, bool bShowInEditForm, bool bShowInListSettings, bool bShowInNewForm, bool bShowInVersionHistory, bool bShowInViewForms)
        {
            bool bRet = true;
            try
			{
				if (bNeedWithPrivilege)
				{
					SPSecurity.RunWithElevatedPrivileges(delegate ()
					{
						bRet = InnerEstablishAndUpdateSpecifyField(spFields, strColumnName, bReadOnly, bReadOnlyEnforced, bHidden, bShowInDisplayForm, bShowInEditForm, bShowInListSettings, bShowInNewForm, bShowInVersionHistory, bShowInViewForms);
					});
				}
				else
				{
					bRet = InnerEstablishAndUpdateSpecifyField(spFields, strColumnName, bReadOnly, bReadOnlyEnforced, bHidden, bShowInDisplayForm, bShowInEditForm, bShowInListSettings, bShowInNewForm, bShowInVersionHistory, bShowInViewForms);
				}
			}
            catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Error, "Exception during establish and update field:[{0}] value\n", new object[] { strColumnName }, ex);
			}
			return bRet;
        }

        private static bool InnerEstablishAndUpdateSpecifyField(SPFieldCollection spFields, string strColumnName, bool bReadOnly, bool bReadOnlyEnforced, bool bHidden,
            bool bShowInDisplayForm, bool bShowInEditForm, bool bShowInListSettings, bool bShowInNewForm, bool bShowInVersionHistory, bool bShowInViewForms)
        {
            bool bRet = true;
            try
			{
				if (null == spFields)
				{
					bRet = false;
				}
				else
				{
					SPField spFiled = MyGetSPField(spFields, strColumnName);
					if (null == spFiled)
					{
						// ReadOnlyEnforced, only system can change the column value
						string newField = $"<Field Type=\"Note\" DisplayName=\"{strColumnName}\" Name=\"{strColumnName}\" Required=\"FALSE\" DefaultValue=\"\" " +
												$" ReadOnly=\"{bReadOnly}\" ReadOnlyEnforced=\"{bReadOnlyEnforced}\" " +
												$" Hidden=\"{bHidden}\" " +
												$" ShowInDisplayForm=\"{bShowInDisplayForm}\" " +
												$" ShowInEditForm=\"{bShowInEditForm}\" " +
												$" ShowInListSettings=\"{bShowInListSettings}\" " +
												$" ShowInNewForm=\"{bShowInNewForm}\" " +
												$" ShowInVersionHistory=\"{bShowInVersionHistory}\" " +
												$" ShowInViewForms=\"{bShowInViewForms}\" " +
										   "/>";
						spFields.AddFieldAsXml(newField);
					}
					else
					{
						bool bNeedUpdate = false;
						if ((spFiled.Hidden != bHidden) ||
							(spFiled.ShowInDisplayForm != bShowInDisplayForm) ||
							(spFiled.ShowInEditForm != bShowInEditForm) ||
							(spFiled.ShowInListSettings != bShowInListSettings) ||
							(spFiled.ShowInNewForm != bShowInNewForm) ||
							(spFiled.ShowInVersionHistory != bShowInVersionHistory) ||
							(spFiled.ShowInViewForms != bShowInViewForms) ||
							(spFiled.ReadOnlyField != bReadOnly)
						   )
						{
							bNeedUpdate = true;
						}

						if (bNeedUpdate)
						{
							// Force make sure the field hidden attribute is the same as the invoke set
							// But you need note that: this update do not changed the view, if you want to update the view, you need process on SPView object
							if (spFiled.Hidden != bHidden)
							{
								SetCanToggleHidden(spFiled, true);
								spFiled.Hidden = bHidden;
							}

							spFiled.ShowInDisplayForm = bShowInDisplayForm;
							spFiled.ShowInEditForm = bShowInEditForm;
							spFiled.ShowInListSettings = bShowInListSettings;
							spFiled.ShowInNewForm = bShowInNewForm;
							spFiled.ShowInVersionHistory = bShowInVersionHistory;
							spFiled.ShowInViewForms = bShowInViewForms;
							spFiled.ReadOnlyField = bReadOnly;

							try
							{
								spFiled.Update();
							}
							catch (Exception ex)
							{
								NLLogger.OutputLog(LogLevel.Debug, "Failed to update an exist column:[{0}] settings with error:[{1}], ignore this error in logic\n", new object[] { strColumnName, ex.Message });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Error, "Exception during inner establish and update field:[{0}]\n", new object[] { strColumnName }, ex);
			}
            return bRet;
        }
        private static void SetCanToggleHidden(SPField spField, bool bCanHidden)
        {
			try
			{
				if (null != spField)
				{
					Type type = spField.GetType();

					MethodInfo mi = type.GetMethod("SetFieldBoolValue", BindingFlags.NonPublic | BindingFlags.Instance);
					mi.Invoke(spField, new object[] { "CanToggleHidden", bCanHidden });
				}
			}
			catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Error, "Exception set field:[{0}] toggle hidden attribute value\n", new object[] { spField?.InternalName }, ex);
			}
        }
    }
}
