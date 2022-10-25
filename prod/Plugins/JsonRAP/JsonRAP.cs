using Microsoft.SharePoint;
using NextLabs.PreAuthAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Xml.Serialization;
using NextLabs.Diagnostic;

namespace JsonRAP
{
    class JsonRAP : IGetPreAuthAttributes
    {
        #region Const/Readonly values
        private const string g_kstrNxlFieldName = "Nxl_{C6BED4B4-2FE7-464F-BE24-AE19F7D7BFE8}";
        private const string g_kstrResourceCEAttrKey_JosnHeader = "jsonheader"; // new resource attribute key as a part of Pre-Auth Plugin
        private const string g_kstrJsonHeaderEmptyFlag = "json_header_empty";
        #endregion

        #region Constructors
        static JsonRAP()
        {
            try
            {
                NLLogger.OutputLog(LogLevel.Debug, "Load Json rap plugin, begin init\n", null);

                string configPath = null;
                RegistryKey Software_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Sharepoint Enforcer", false);
                object ReglogInstallDir = null;
                if (Software_key != null)
                {
                    ReglogInstallDir = Software_key.GetValue("InstallDir");
                }
                if (ReglogInstallDir != null)
                {
                    configPath = ReglogInstallDir.ToString() + "Config\\TDFConfig.xml";
                }
                if (!string.IsNullOrEmpty(configPath))
                {
                    Nextlabs.TDFFileAnalyser.TDFXHeaderExtralConfig obTDFXHeaderExtralConfigIns = Nextlabs.TDFFileAnalyser.TDFXHeaderExtralConfig.GetInstance();
                    obTDFXHeaderExtralConfigIns.Init(configPath);
                }

                NLLogger.OutputLog(LogLevel.Debug, "Load Json rap plugin, end init\n", null);
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during load TDFConfig.xml failed\n", null, ex);
            }
        }
        #endregion

        #region Implement interface: IGetPreAuthAttributes
        public int GetCustomAttributes(object spUser, object spSource, object SPFile, string strAction, Dictionary<string, string> userPair, Dictionary<string, string> dicSrcPair, Dictionary<string, string> dstPair, int nobjecttype, int nTimeout)
        {
            int iResult = 0;
            try
			{
				// Base check, for uploading case(adding, updating, attachment updating), we cannot get the json header info from the exist column or the exist file
				// We need using the new file(uploading file) info to get the newest json header
				// These newest json header is processed in PrepareFileAttributesDuringUpload during uploading
				if ((null != dicSrcPair) && (dicSrcPair.ContainsKey(g_kstrResourceCEAttrKey_JosnHeader)))
				{
					string strJsonHeader = dicSrcPair[g_kstrResourceCEAttrKey_JosnHeader];
					NLLogger.OutputLog(LogLevel.Info, "JsonRAP: Currently the source attribute pair already contains the json header info, no need process in current interface. HeaderInfo:[{0}], action:[{1}]\n", new object[] { strJsonHeader, strAction });

					if (String.Equals(g_kstrJsonHeaderEmptyFlag, strJsonHeader, StringComparison.OrdinalIgnoreCase))
					{
						dicSrcPair.Remove(g_kstrResourceCEAttrKey_JosnHeader);
					}
					return iResult;
				}

				if (spSource is SPWeb)
				{
					NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: spSource is SPWeb, action:[{0}], no need process", new object[] { strAction });
				}
				else if (spSource is SPList)
				{
					NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: spSource is SPList, action:[{0}], no need process", new object[] { strAction });
				}
				else if (spSource is SPListItem)
				{
					NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: This is SPListItem with action:[{0}], begin process", new object[] { strAction });

                    SPListItem spListItem = (SPListItem)spSource;
					if (null == spListItem)
					{
						iResult = 1;
						NLLogger.OutputLog(LogLevel.Error, "JsonRAP: The SPListItem object:[{0}] is NULL, logic error, please check\n", new object[] { spListItem });
					}
					else
					{
						// 1. Try to get json header info from nxl field
						string strJsonHeader = SPCommon.GetFieldStringValueFromListItem(spListItem, g_kstrNxlFieldName, "");
						if (String.IsNullOrEmpty(strJsonHeader))
						{
							// 2. Try to get json header info from file content
							SPFile spFileObject = spListItem.File;
							if (null == spFileObject)
							{
								NLLogger.OutputLog(LogLevel.Warn, "JsonRAP: The item:[{0}] in list:[{1}] file object:[{2}] is NULL, list base type:[{3}]\n", new object[] { spListItem.Name, spListItem.ParentList.Title, spListItem.File, spListItem.ParentList.BaseType });
							}
							else
							{
								try
								{
									// Get header info from file content
									using (var sr = new StreamReader(spFileObject.OpenBinaryStream()))
									{
										strJsonHeader = Nextlabs.TDFFileAnalyser.TDFXHeaderExtral.GetXmlHeaderFromTDFFileByStreamReader(sr, spFileObject.Name, true);
										if (String.IsNullOrEmpty(strJsonHeader))
										{
											strJsonHeader = g_kstrJsonHeaderEmptyFlag;
											NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: The item:[{0}] file json header info is empty\n", new object[] { spFileObject.Name });
										}
									}
									strJsonHeader = strJsonHeader.ToLower();

									// Set header info into nxl field, ignore return value
									// if the json header analysis success but the field update failed this do not effective current action enforcement result
									SetHeaderInfoIntoNxlField(spListItem, strJsonHeader);
								}
								catch (Exception ex)
								{
									NLLogger.OutputLog(LogLevel.Debug, "Exception during GetCustomAttributes for item:[{0}] in list:[{1}], Message:[{2}], StackTrace:[{3}]\n", new object[] { spListItem.Name, spListItem.ParentList.Title, ex.Message, ex.StackTrace });
								}
							}
						}
						else
						{
							strJsonHeader = strJsonHeader.ToLower();
						}

						if (String.IsNullOrEmpty(strJsonHeader))
						{
							iResult = 1;
                            NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: Failed get item:[{0}] json header info[{1}]\n", new object[] { spListItem.Name, strJsonHeader });
                        }
						else
						{
							NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: Success get item:[{0}] json header info[{1}]\n", new object[] { spListItem.Name, strJsonHeader });

							if (String.Equals(g_kstrJsonHeaderEmptyFlag, strJsonHeader, StringComparison.OrdinalIgnoreCase))
							{
								// ignore, empty flags
							}
							else
							{
								if (null == dicSrcPair)
								{
									dicSrcPair = new Dictionary<string, string>();
									NLLogger.OutputLog(LogLevel.Warn, "JsonRAP: The pass in source attribute pair object is null in item:[{0}]. Auto generate a new one, this maybe a logic error, please check\n", new object[] { spListItem.Name });
								}
								dicSrcPair.Add(g_kstrResourceCEAttrKey_JosnHeader, strJsonHeader);
							}
							iResult = 0;
						}
					}
					NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: This is SPListItem with action:[{0}] with result:[{1}], end process", new object[] { strAction, iResult });
				}
				else
				{
					NLLogger.OutputLog(LogLevel.Debug, "JsonRAP: spSource:[{0}] is unknown, action:[{0}], no need process", new object[] { spSource, strAction });
				}
			}
            catch (Exception ex)
			{
                NLLogger.OutputLog(LogLevel.Debug, "Exception during get extral custom attributes info, action:[{0}]", new object[] { strAction }, ex);
            }
            return iResult;
        }

        public bool PrepareFileAttributesDuringUpload(SPItemEventProperties spItemEventProperties, string strFilePath, ref List<KeyValuePair<string, string>> lsPrepareExtralAttributesRef)
        {
            bool bRet = false;

            SPEventReceiverType emEventType = ((null == spItemEventProperties) ? SPEventReceiverType.InvalidReceiver : spItemEventProperties.EventType);
            NLLogger.OutputLog(LogLevel.Info, "JsonRAP: begin process prepare filter attribute, EventProperties:[{0}], Event:[{1}], file path:[{2}]\n", new object[] { spItemEventProperties, emEventType.ToString(), strFilePath });
            try
            {
                if (null == spItemEventProperties)
                {
                    NLLogger.OutputLog(LogLevel.Info, "JsonRAP: begin process prepare filter attribute, EventProperties:[{0}] is null with path:[{1}], logic error, please check\n", new object[] { spItemEventProperties, strFilePath });
                }
                else
                {
                    if ((SPEventReceiverType.ItemAdding == emEventType) || (SPEventReceiverType.ItemUpdating == emEventType))
                    {
                        bool bNxlFieldEstablished = EstablishAndUpdateNxlField(spItemEventProperties.List.Fields);
                        NLLogger.OutputLog(LogLevel.Info, "JsonRAP: in prepare filter attribute with event:[{0}] force establish nxl field with result:[{1}]\n", new object[] { emEventType.ToString(), bNxlFieldEstablished });
                    }

                    if (String.IsNullOrEmpty(strFilePath) || (!File.Exists(strFilePath)))
                    {
                        NLLogger.OutputLog(LogLevel.Info, "JsonRAP: Current file path:[{0}] is empty or not exist in prepare file attribute for event:[{1}]\n", new object[] { strFilePath, emEventType.ToString() });
                    }
                    else
                    {
                        // Get header info from file content
                        string strJsonHeader = Nextlabs.TDFFileAnalyser.TDFXHeaderExtral.GetXmlHeaderFromTDFFileByFilePath(strFilePath, true);
                        if (String.IsNullOrEmpty(strJsonHeader))
                        {
                            strJsonHeader = g_kstrJsonHeaderEmptyFlag;
                        }
                        strJsonHeader = strJsonHeader.ToLower();
                        lsPrepareExtralAttributesRef.Add(new KeyValuePair<string, string>(g_kstrResourceCEAttrKey_JosnHeader, strJsonHeader));

                        // Set header info into nxl field
                        bRet = SetHeaderInfoIntoNxlField(spItemEventProperties, strJsonHeader);
                        NLLogger.OutputLog(LogLevel.Error, "Set file:[{0}] header info into NXL field with result:[{1}] in prepare file attributtes in event:[{2}]\n", new object[] { strFilePath, bRet, emEventType.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during PrepareFileAttributesDuringUpload in event:[{0}]\n", new object[] { emEventType }, ex);
            }
            NLLogger.OutputLog(LogLevel.Info, "JsonRAP: end process prepare filter attribute, EventProperties:[{0}], Event:[{1}], file path:[{2}]\n", new object[] { spItemEventProperties, emEventType.ToString(), strFilePath });
            return bRet;
        }
        #endregion

        #region Inner tools
        private bool EstablishAndUpdateNxlField(SPFieldCollection spFields)
        {
            return SPCommon.EstablishAndUpdateSpecifyField(spFields, g_kstrNxlFieldName, true, true, true, true, false, false, false, false, false, false);
        }
        private bool SetHeaderInfoIntoNxlField(SPItemEventProperties spItemEventProperties, string strJsonHeader)
        {
            bool bRet = false;
            try
            {
				bRet = EstablishAndUpdateNxlField(spItemEventProperties?.List?.Fields);
				if (bRet)
				{
					SPCommon.AddFieldStringValueBySPItemEventProperties(spItemEventProperties, g_kstrNxlFieldName, strJsonHeader);
				}
				else
				{
					NLLogger.OutputLog(LogLevel.Error, "Establish NXL column:[{0}] failed, please check", new object[] { g_kstrNxlFieldName });
				}
			}
            catch (Exception ex)
			{
                NLLogger.OutputLog(LogLevel.Error, "Exception during set header info into NXL column:[{0}] in field:[{1}] by item event properties, please check", new object[] { g_kstrNxlFieldName, spItemEventProperties?.List?.Title }, ex);
            }
            return bRet;
        }
        private bool SetHeaderInfoIntoNxlField(SPListItem spListItem, string strJsonHeader)
        {
            bool bRet = false;
            try
			{
				bRet = EstablishAndUpdateNxlField(spListItem.Fields);
				if (bRet)
				{
					SPCommon.AddFieldStringValueBySPListItem(spListItem, g_kstrNxlFieldName, strJsonHeader);
				}
				else
				{
					NLLogger.OutputLog(LogLevel.Debug, "Exception during establish NXL column:[{0}] in field:[{1}], please check", new object[] { g_kstrNxlFieldName, spListItem?.Name });
				}
			}
            catch (Exception ex)
			{
                NLLogger.OutputLog(LogLevel.Error, "Exception during set header info into NXL column:[{0}] failed:[{1}] by list item, please check", new object[] { g_kstrNxlFieldName, spListItem?.Name }, ex);
            }
            return bRet;
        }
        #endregion
    }
}

#if false
// Backup tools
        static public Encoding GetEncoding(string filePath)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }
        static private string converToJSON(XmlDocument doc)
        {
            String json = null;
            try
            {
                json = JsonConvert.SerializeXmlNode(doc);
                Trace.WriteLine("JSONRAP: Json string |" + json);
            }
            catch (Exception e)
            {
                Trace.TraceError("JSONRAP: Json Conversion Error " + e.Message);
            }
            return json;
        }
#endif

#if false
    //string xmlString = sr.ReadToEnd();
    //try
    //{
    //    System.Diagnostics.Trace.WriteLine("xmlString:" + xmlString);
    //    System.Diagnostics.Trace.WriteLine("fileObject.Name:" + fileObject.Name);
    //    var fileNameIndex = fileObject.Name.LastIndexOf(".xml");
    //    string fileName = fileObject.Name.Remove(fileNameIndex);
    //    System.Diagnostics.Trace.WriteLine("fileName:" + fileName);
    //    var index = xmlString.LastIndexOf(fileName);
    //    System.Diagnostics.Trace.WriteLine("index:" + index);
    //    if (index > -1)
    //    {
    //        xmlString = xmlString.Remove(index);
    //    }
    //}
    //catch (Exception ex)
    //{
    //    Trace.WriteLine("GetCustomAttributes get xml error:" + ex.ToString());
    //}


    //System.Diagnostics.Trace.WriteLine("xmlString:" + xmlString);
    //string json = null;
    //try
    //{
    //    XmlDocument doc = new XmlDocument();
    //    doc.LoadXml(xmlString);
    //    json = converToJSON(doc);
    //}
    //catch (Exception ex)
    //{
    //    Trace.WriteLine("GetCustomAttributes convert to json error:" + ex.ToString());
    //}
    //srcPair.Add(RESOURCEATTRKEY, json);
#endif

#if false
    /*var startTime = DateTime.Now;
    Trace.WriteLine("--------------------------------------GetFileContent enter:---------------------------------"+ startTime);
    const string RESOURCEATTRKEY = "jsonheader";
    int result = 0;
    Encoding encoding = GetEncoding(filePath);
    using (var sr = new StreamReader(filePath, encoding))
    {
        string xmlString = sr.ReadToEnd();
        try
        {
            System.Diagnostics.Trace.WriteLine("xmlString:" + xmlString);
            string fileName = Path.GetFileName(filePath);
            var guidIndex = fileName.LastIndexOf("-");
            var fileNameIndex = fileName.LastIndexOf(".xml");
            fileName = fileName.Substring(guidIndex + 1, fileNameIndex - guidIndex-1);
            System.Diagnostics.Trace.WriteLine("fileName:" + fileName);
            var index = xmlString.LastIndexOf(fileName);
            System.Diagnostics.Trace.WriteLine("index:" + index);
            if (index > -1)
            {
                xmlString = xmlString.Remove(index);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("GetFileContent get xml error:" + ex.ToString());
        }


        System.Diagnostics.Trace.WriteLine("xmlString:" + xmlString);
        string json = null;
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            json = converToJSON(doc);
        }
        catch (Exception ex)
        {
            Trace.WriteLine("GetCustomAttributes convert to json error:" + ex.ToString());
        }
        if (!string.IsNullOrEmpty(json))
        {
            jsonHeader.Add(RESOURCEATTRKEY);
            jsonHeader.Add(json);
        }
    }
    Trace.WriteLine("--------------------------------------GetFileContent end:---------------------------------" + DateTime.Now.Subtract(startTime).TotalMilliseconds);

    return result;*/
#endif