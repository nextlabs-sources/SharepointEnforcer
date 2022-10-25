using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextLabs.Diagnostic
{
	static class CommonTools
	{
		public static bool TrimSpecifyTopFolderFiles(string strFolderPath, int nMaxFileCount, string searchPattern)
		{
			bool bRet = false;
			try
			{
				if ((String.IsNullOrEmpty(strFolderPath)) || (0 > nMaxFileCount))
				{
					NLLogger.OutputLog(LogLevel.Debug, "Parameters error during trim directory:[{0}] with max file count:[{1}]", new object[] { strFolderPath, nMaxFileCount });
				}
				else
				{
					if (Directory.Exists(strFolderPath))
					{
						string[] szFiles = Directory.GetFiles(strFolderPath, searchPattern);
						if (nMaxFileCount >= szFiles.Length)
						{
							// Not overflow, no need trimming
						}
						else
						{
							if (0 == nMaxFileCount)
							{
								// Delete all files
								foreach (string strFilePath in szFiles)
								{
									MyDeleteFile(strFilePath);
								}
							}
							else
							{
								// Record file last write time and index
								List<KeyValuePair<long, int>> lsFileLastWriteTimes = new List<KeyValuePair<long, int>>();
								for (int i = 0; i < szFiles.Length; ++i)
								{
									string strFilePath = szFiles[i];
									DateTime dtFile = File.GetLastWriteTime(strFilePath);

									lsFileLastWriteTimes.Add(new KeyValuePair<long, int>(dtFile.Ticks, i));
								}
								// Sort files with last write time
								lsFileLastWriteTimes.Sort((first, second) => { return (first.Key == second.Key) ? 0 : ((first.Key > second.Key) ? 1 : -1); });

								// Delete early files
								int nNeedTrimmedFileCount = lsFileLastWriteTimes.Count - nMaxFileCount;
								int nDeleteCount = 0;
								foreach (KeyValuePair<long, int> pariItem in lsFileLastWriteTimes)
								{
									if (nDeleteCount < nNeedTrimmedFileCount)
									{
										string strFilePath = szFiles[pariItem.Value];
										MyDeleteFile(strFilePath);
									}
									else
									{
										// Enough
										break;
									}
									++nDeleteCount;
								}
							}
						}
						bRet = true;
					}
					else
					{
						bRet = false;
						NLLogger.OutputLog(LogLevel.Debug, "Trim directory:[{0}] with max file count:[{1}] failed, the folder do not exist", new object[] { strFolderPath, nMaxFileCount });
					}
				}
			}
			catch (Exception ex)
			{
				NLLogger.OutputLog(LogLevel.Debug, "Exception during trim directory:[{0}] with max file count:[{1}], Message:[{2}]", new object[] { strFolderPath, nMaxFileCount, ex.Message });
			}
			return bRet;
		}
		public static bool MyDeleteFile(string strFilePath)
		{
			bool bRet = true;
			try
			{
				File.Delete(strFilePath);
			}
			catch (Exception ex)
			{
				bRet = false;
				NLLogger.OutputLog(LogLevel.Debug, "Try to delete file:[{0}] but exception with message:[{1}]", new object[] { strFilePath, ex.Message });
			}
			return bRet;
		}

		public static string GetRegStringValue(string strRegKey, string strRegItemKey, string strDefaultValue)
		{
			string strValueRet = strDefaultValue;
			RegistryKey Software_key = Registry.LocalMachine.OpenSubKey(strRegKey, false);
			if (Software_key == null)
			{
				// Using default value
			}
			else
			{
				object ReglogInstallDir = Software_key.GetValue(strRegItemKey);
				if (ReglogInstallDir == null)
				{
					// Using default value
				}
				else
				{
					strValueRet = Convert.ToString(ReglogInstallDir);
				}
			}
			return strValueRet;
		}

		public static void MakeStandardFolderPath(ref string strFolderPathRef)
		{
			if (!String.IsNullOrEmpty(strFolderPathRef))
			{
				if ('\\' == strFolderPathRef[strFolderPathRef.Length - 1])
				{
					// OK
				}
				else
				{
					strFolderPathRef += '\\';
				}
			}
		}
	}
}
