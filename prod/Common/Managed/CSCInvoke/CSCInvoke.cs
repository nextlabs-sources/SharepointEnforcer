//
// All sources, binaries and HTML pages (C) copyright 2007 by NextLabs Inc. 
// San Mateo CA, Ownership remains with NextLabs Inc, 
// All rights reserved worldwide. 
//
//
// NextLabs Compliant Enterprise SDK in .NET
// 
// <-------------------------------------------------------------------------->

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
namespace CETYPE
{
    public enum CEResult_t
    {
        CE_RESULT_SUCCESS = 0,
        CE_RESULT_GENERAL_FAILED = -1,
        CE_RESULT_CONN_FAILED = -2,
        CE_RESULT_INVALID_PARAMS = -3,
        CE_RESULT_VERSION_MISMATCH = -4,
        CE_RESULT_FILE_NOT_PROTECTED = -5,
        CE_RESULT_INVALID_PROCESS = -6,
        CE_RESULT_INVALID_COMBINATION = -7,
        CE_RESULT_PERMISSION_DENIED = -8,
        CE_RESULT_FILE_NOT_FOUND = -9,
        CE_RESULT_FUNCTION_NOT_AVAILBLE = -10,
        CE_RESULT_TIMEDOUT = -11,
        CE_RESULT_SHUTDOWN_FAILED = -12, 
        CE_RESULT_INVALID_ACTION_ENUM = -13, 
        CE_RESULT_EMPTY_SOURCE = -14, 
        CE_RESULT_MISSING_MODIFIED_DATE = -15, 
        CE_RESULT_NULL_CEHANDLE = -16,
        CE_RESULT_INVALID_EVAL_ACTION = -17, 
        CE_RESULT_EMPTY_SOURCE_ATTR = -18, 
        CE_RESULT_EMPTY_ATTR_KEY = -19, 
        CE_RESULT_EMPTY_ATTR_VALUE = -20, 
        CE_RESULT_EMPTY_PORTAL_USER = -21, 
        CE_RESULT_EMPTY_PORTAL_USERID = -22, 
        CE_RESULT_MISSING_TARGET = -23, 
        CE_RESULT_PROTECTION_OBJECT_NOT_FOUND = -24,
        CE_RESULT_THREAD_NOT_INITIALIZED = -33 /**< Thread is not initialized, If the connect is invalid the CESDK will return with this error*/
    };

    public enum CEResponse_t
    {
        CEDeny = 0,
        CEAllow = 1
    };

    public enum CENoiseLevel_t 
    {
        CE_NOISE_LEVEL_MIN = 0,
        CE_NOISE_LEVEL_SYSTEM = 1,
        CE_NOISE_LEVEL_APPLICATION = 2,
        CE_NOISE_LEVEL_USER_ACTION = 3,
        CE_NOISE_LEVEL_MAX = 4
    };
    
    public enum CEAction
    {
        Unknown = 0,
        Read = 1 << 0,
        Delete = 1 << 1,
        Move = 1 << 2,
        Copy = 1 << 3,
        Write = 1 << 4,
        Rename = 1 << 5,
        ChangeAttrFile = 1 << 6,
        ChangeSecFile = 1 << 7,
        PrintFile = 1 << 8,
        PasteFile = 1 << 9,
        EmailFile = 1 << 10,
        ImFile = 1 << 11,
        Export = 1 << 12,
        Import = 1 << 13,
        CheckIn = 1 << 14,
        CheckOut = 1 << 15,
        Attach = 1 << 16,
        Run = 1 << 17,
        Reply = 1 << 18,
        Forward = 1 << 19,
        NewEmail = 1 << 20,
        AVD = 1 << 21,
        Meeting = 1 << 22,
        ProcessTerminate = 1 << 23,
        WmShare = 1 << 24,
        WmRecord = 1 << 25,
        WmQuestion = 1 << 26,
        WmVoice = 1 << 27,
        WmVideo = 1 << 28,
        WmJoin = 1 << 29,
        View = 1 << 30,
        Upload = 1 << 31
    }
    public struct CEApplication
    {
        public string appName;
        public string appPath;
        public string appURL;

        public CEApplication(string n, string p, string u)
        {
            appName = n;
            appPath = p;
            appURL = u;
        }
    }
    public struct CEUser
    {
        public string userName;
        public string userID;

        public CEUser(string n, string i)
        {
            userName = n;
            userID = i;
        }
    }

    public class CEAttrKey
    {
        public const string CE_ATTR_MODIFIED_DATE = "modified_date";
        public const string CE_ATTR_SP_NAME = "name";
        public const string CE_ATTR_SP_TITLE = "title";
        public const string CE_ATTR_SP_DESC = "desc";
        public const string CE_ATTR_SP_RESOURCE_TYPE = "type";
        public const string CE_ATTR_SP_RESOURCE_SUBTYPE = "sub_type";
        public const string CE_ATTR_SP_CREATED_BY = "created_by";
        public const string CE_ATTR_SP_MODIFIED_BY = "modified_by";
        public const string CE_ATTR_SP_DATE_CREATED = "created";
        public const string CE_ATTR_SP_DATE_MODIFIED = "modified";
        public const string CE_ATTR_SP_FILE_SIZE = "file_size";

        public const string CE_ATTR_OBLIGATION_COUNT  = "CE_ATTR_OBLIGATION_COUNT";
        public const string CE_ATTR_OBLIGATION_NAME   = "CE_ATTR_OBLIGATION_NAME";
        public const string CE_ATTR_OBLIGATION_POLICY = "CE_ATTR_OBLIGATION_POLICY";
        public const string CE_ATTR_OBLIGATION_VALUE  = "CE_ATTR_OBLIGATION_VALUE";
    }

    public class CEAttrVal
    {
        public const string CE_ATTR_SP_TYPE_VAL_SITE        = "site";
        public const string CE_ATTR_SP_TYPE_VAL_PORTLET     = "portlet";
        public const string CE_ATTR_SP_TYPE_VAL_ITEM        = "item";
        public const string CE_ATTR_SP_SUBTYPE_VAL_SITE     = "site";
        public const string CE_ATTR_SP_SUBTYPE_VAL_LIST     = "list";
        public const string CE_ATTR_SP_SUBTYPE_VAL_LIBRARY  = "library";
        public const string CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM = "list item";
        public const string CE_ATTR_SP_SUBTYPE_VAL_LIBRARY_ITEM="library item";

        public const string CE_OBLIGATION_NOTIFY            = "CE::NOTIFY";
    }



    public struct CEResource

    {

        public string resourceName;

        public string resourceType;



        public CEResource(string n, string t)

        {

            resourceName = n;

            resourceType = t;

        }

    }

}
namespace NextLabs.CSCInvoke
{
    //Define the layout of CE SDK type
    //CEString 
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class CEString
    {
        public string buf;
        public int length;
    }
    //CEString end

    //Declare CE SDK C API for 32 bit cesdk.dll
    public class CESDKAPI_Signature32
    {
        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CECONN_Initialize(
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString binaryPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString pdpHost,
          [Out]out IntPtr connectHandle,
          [In]int timeout_in_millisec);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CECONN_Close(
         [In]IntPtr handle,
         [In]int timeout_in_millisec);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckPortal(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName, 
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID, 
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckFile(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckMsgAttachment(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In]int numRecipients,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] recipients,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] userAttributes,
          [In]int numUserAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] appAttributes,
          [In]int numAppAttributes,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);



        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckResources(
          [In]IntPtr handle,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceType,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetType,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] userAttributes,
          [In]int numUserAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] appAttributes,
          [In]int numAppAttributes,
          [In]int numRecipients,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] recipients,
          [In]uint ipAddress,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk32.dll")]
        public static extern int CSCINVOKE_CEEVALUATE_GetString(
          [In]IntPtr ptr,
          [In]int index,
          [Out]out IntPtr strPtr);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t
          CSCINVOKE_CEEVALUATE_FreeStringArray(
          [In]IntPtr ptr,
          [In]int num);

        [DllImport("cesdk32.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CELOGGING_LogObligationData(
          [In]IntPtr handle,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString logIdentifier,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString assistantName,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] attributes,
          [In]int numAttributes);
    }

    //Declare CE SDK C API for 64-bit cesdk.dll
    public class CESDKAPI_Signature64
    {
        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CECONN_Initialize(
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString binaryPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString pdpHost,
          [Out]out IntPtr connectHandle,
          [In]int timeout_in_millisec);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CECONN_Close(
         [In]IntPtr handle,
         [In]int timeout_in_millisec);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckPortal(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckFile(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckMsgAttachment(
          [In]IntPtr handle,
          [In]CETYPE.CEAction operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceFile,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In]int numRecipients,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] recipients,
          [In]uint ipAddress,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] userAttributes,
          [In]int numUserAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] appAttributes,
          [In]int numAppAttributes,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);



        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckResources(
          [In]IntPtr handle,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString operation,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString sourceType,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] sourceAttributes,
          [In]int numSourceAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString targetType,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] targetAttributes,
          [In]int numTargetAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString userID,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] userAttributes,
          [In]int numUserAttributes,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appName,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appPath,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString appURL,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] appAttributes,
          [In]int numAppAttributes,
          [In]int numRecipients,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] recipients,
          [In]uint ipAddress,
          [In]bool performObligation,
          [In]CETYPE.CENoiseLevel_t noiseLevel,
          [Out]out IntPtr enforcement_ob,
          [Out]out CETYPE.CEResponse_t enforcement_result,
          [Out]out int numEnforcements,
          [In] int timeout_in_millisec);

        [DllImport("cesdk.dll")]
        public static extern int CSCINVOKE_CEEVALUATE_GetString(
          [In]IntPtr ptr,
          [In]int index,
          [Out]out IntPtr strPtr);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t
          CSCINVOKE_CEEVALUATE_FreeStringArray(
          [In]IntPtr ptr,
          [In]int num);

        [DllImport("cesdk.dll")]
        public static extern CETYPE.CEResult_t CSCINVOKE_CELOGGING_LogObligationData(
          [In]IntPtr handle,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString logIdentifier,
          [In, MarshalAs(UnmanagedType.LPStruct)]CEString assistantName,
          [In, MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.LPTStr)]string[] attributes,
          [In]int numAttributes);
    }

    public class CESDKAPI_Signature
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpFileName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32.dll")]
        private static extern bool IsWow64Process(IntPtr hProcess, ref bool bIsWow64);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        private static int KEY_READ = 0x20019;
        private static int KEY_WOW64_64KEY = 0x100;
        private static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
        private static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        private static extern int RegQueryValueEx(
            UIntPtr hKey,
            string lpValueName,
            int lpReserved,
            out uint lpType,
            System.Text.StringBuilder lpData,
            //ref string lpData,
            ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW", SetLastError = true)]
        private static extern int RegOpenKeyEx(
            UIntPtr hKey,
            string subKey,
            int ulOptions,
            int samDesired,
            out UIntPtr hResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RegCloseKey(UIntPtr hKey);

        private static bool bIs64BitCESDK;

        public static CETYPE.CEResult_t CSCINVOKE_CECONN_Initialize(
          CEString appName,
          CEString binaryPath,
          CEString userName,
          CEString userID,
          CEString pdpHost,
          out IntPtr connectHandle,
          int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CECONN_Initialize(appName, binaryPath, userName, userID, pdpHost,out connectHandle, timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CECONN_Initialize(appName, binaryPath, userName, userID, pdpHost,out connectHandle, timeout_in_millisec);
        }

        public static CETYPE.CEResult_t CECONN_Close(
         IntPtr handle,
         int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CECONN_Close(handle, timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CECONN_Close(handle, timeout_in_millisec);
        
        }

        public static CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckPortal(
          IntPtr handle,
          CETYPE.CEAction operation,
          CEString sourceURL,
          string[] sourceAttributes,
          int numSourceAttributes,
          CEString targetURL,
          string[] targetAttributes,
          int numTargetAttributes,
          uint ipAddress,
          CEString userName,
          CEString userID,
          bool performObligation,
          CETYPE.CENoiseLevel_t noiseLevel,
          out IntPtr enforcement_ob,
          out CETYPE.CEResponse_t enforcement_result,
          out int numEnforcements,
          int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_CheckPortal(handle,
                    operation, sourceURL,
                    sourceAttributes,
                    numSourceAttributes,
                    targetURL,
                    targetAttributes,
                    numTargetAttributes,
                    ipAddress, userName,
                    userID,
                    performObligation,
                    noiseLevel,
                    out enforcement_ob,
                    out enforcement_result,
                    out numEnforcements,
                    timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_CheckPortal(handle,
                    operation, sourceURL,
                    sourceAttributes,
                    numSourceAttributes,
                    targetURL,
                    targetAttributes,
                    numTargetAttributes,
                    ipAddress, userName,
                    userID,
                    performObligation,
                    noiseLevel,
                    out enforcement_ob,
                    out enforcement_result,
                    out numEnforcements,
                    timeout_in_millisec);
        }

        public static CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckFile(
          IntPtr handle,
          CETYPE.CEAction operation,
          CEString sourceFile,
          string[] sourceAttributes,
          int numSourceAttributes,
          CEString targetFile,
          string[] targetAttributes,
          int numTargetAttributes,
          uint ipAddress,
          CEString userName,
          CEString userID,
          CEString appName,
          CEString appPath,
          CEString appURL,
          bool performObligation,
          CETYPE.CENoiseLevel_t noiseLevel,
          out IntPtr enforcement_ob,
          out CETYPE.CEResponse_t enforcement_result,
          out int numEnforcements,
          int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_CheckFile(handle, operation,
                    sourceFile, sourceAttributes, numSourceAttributes,
                    targetFile, targetAttributes, numTargetAttributes,
                    ipAddress, userName, userID, appName, appPath, appURL, performObligation,
                    noiseLevel, out enforcement_ob, out enforcement_result,
                    out numEnforcements, timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_CheckFile(handle, operation,
                    sourceFile, sourceAttributes, numSourceAttributes,
                    targetFile, targetAttributes, numTargetAttributes,
                    ipAddress, userName, userID, appName, appPath, appURL, performObligation,
                    noiseLevel, out enforcement_ob, out enforcement_result,
                    out numEnforcements, timeout_in_millisec);
        }

        public static CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckMsgAttachment(
          IntPtr handle,
          CETYPE.CEAction operation,
          CEString sourceFile,
          string[] sourceAttributes,
          int numSourceAttributes,
          int numRecipients,
          string[] recipients,
          uint ipAddress,
          CEString userName,
          CEString userID,
          string[] userAttributes,
          int numUserAttributes,
          CEString appName,
          CEString appPath,
          CEString appURL,
          string[] appAttributes,
          int numAppAttributes,
          bool performObligation,
          CETYPE.CENoiseLevel_t noiseLevel,
          out IntPtr enforcement_ob,
          out CETYPE.CEResponse_t enforcement_result,
          out int numEnforcements,
          int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_CheckMsgAttachment(handle, operation,
                    sourceFile, sourceAttributes, numSourceAttributes, numRecipients,
                    recipients, ipAddress, userName, userID, userAttributes, numUserAttributes,
                    appName, appPath, appURL, appAttributes, numAppAttributes, performObligation,
                    noiseLevel, out enforcement_ob, out enforcement_result, out numEnforcements, timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_CheckMsgAttachment(handle, operation,
                    sourceFile, sourceAttributes, numSourceAttributes, numRecipients,
                    recipients, ipAddress, userName, userID, userAttributes, numUserAttributes,
                    appName, appPath, appURL, appAttributes, numAppAttributes, performObligation,
                    noiseLevel, out enforcement_ob, out enforcement_result, out numEnforcements, timeout_in_millisec);
        }
        public static CETYPE.CEResult_t CSCINVOKE_CEEVALUATE_CheckResources(
          IntPtr handle,
          CEString operation,
          CEString sourceName,
          CEString sourceType,
          string[] sourceAttributes,
          int numSourceAttributes,
          CEString targetName,
          CEString targetType,
          string[] targetAttributes,
          int numTargetAttributes,
          CEString userName,
          CEString userID,
          string[] userAttributes,
          int numUserAttributes,
          CEString appName,
          CEString appPath,
          CEString appURL,
          string[] appAttributes,
          int numAppAttributes,
          int numRecipients,
          string[] recipients,
          uint ipAddress,
          bool performObligation,
          CETYPE.CENoiseLevel_t noiseLevel,
          out IntPtr enforcement_ob,
          out CETYPE.CEResponse_t enforcement_result,
          out int numEnforcements,
          int timeout_in_millisec)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_CheckResources(handle, operation,
                    sourceName, sourceType, sourceAttributes, numSourceAttributes,
                    targetName, targetType, targetAttributes, numTargetAttributes,
                    userName, userID, userAttributes, numUserAttributes,
                    appName, appPath, appURL, appAttributes, numAppAttributes,
                    numRecipients, recipients, ipAddress, performObligation, noiseLevel,
                    out enforcement_ob, out enforcement_result,out numEnforcements, timeout_in_millisec);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_CheckResources(handle, operation,
                    sourceName, sourceType, sourceAttributes, numSourceAttributes,
                    targetName, targetType, targetAttributes, numTargetAttributes,
                    userName, userID, userAttributes, numUserAttributes,
                    appName, appPath, appURL, appAttributes, numAppAttributes,
                    numRecipients, recipients, ipAddress, performObligation, noiseLevel,
                    out enforcement_ob, out enforcement_result, out numEnforcements, timeout_in_millisec);
        }


        public static int CSCINVOKE_CEEVALUATE_GetString(
          IntPtr ptr,
          int index,
          out IntPtr strPtr)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_GetString(ptr, index, out strPtr);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_GetString(ptr, index, out strPtr);
        }

        public static CETYPE.CEResult_t
          CSCINVOKE_CEEVALUATE_FreeStringArray(
          IntPtr ptr,
          int num)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CEEVALUATE_FreeStringArray(ptr, num);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CEEVALUATE_FreeStringArray(ptr, num);
        }

        public static CETYPE.CEResult_t CSCINVOKE_CELOGGING_LogObligationData(
          IntPtr handle,
          CEString logIdentifier,
          CEString assistantName,
          string[] attributes,
          int numAttributes)
        {
            if (bIs64BitCESDK)
                return CESDKAPI_Signature64.CSCINVOKE_CELOGGING_LogObligationData(handle, logIdentifier, assistantName, attributes, numAttributes);
            else
                return CESDKAPI_Signature32.CSCINVOKE_CELOGGING_LogObligationData(handle, logIdentifier, assistantName, attributes, numAttributes);
        }
        public static bool Is64BitOS()
        {
            if (IntPtr.Size == 8)
                return true;
            else
            {
                IntPtr hModule = GetModuleHandle("kernel32.dll");
                if (hModule == IntPtr.Zero)
                    return false;

                IntPtr hProc = GetProcAddress(hModule, "IsWow64Process");
                if (hProc == IntPtr.Zero)
                    return false;

                bool bIsWow64 = false;
                if (IsWow64Process(GetCurrentProcess(), ref bIsWow64))
                    return bIsWow64;

                return false;
            }
        }

        public static bool GetCommonLibrariesBin(ref string strBin)
        {
            bool b64bitOS = Is64BitOS();
            int samDesired = KEY_READ;
            if (IntPtr.Size == 4 && b64bitOS == true)
                samDesired |= KEY_WOW64_64KEY;

            if (ReadRegKey(HKEY_LOCAL_MACHINE, "Software\\Nextlabs\\CommonLibraries\\", "InstallDir", ref strBin, samDesired) == false)
                return false;


            if (strBin.EndsWith("\\") == false && strBin.EndsWith("/") == false)
                strBin += "\\";
            if (IntPtr.Size == 8)
                strBin += "bin64\\";
            else
                strBin += "bin32\\";
            return true;
        }

        public static bool ReadRegKey(UIntPtr rootkey, string keypath, string valueName, ref string keyvalue, int samDesired)
        {
            UIntPtr hKey;
            if (RegOpenKeyEx(rootkey, keypath, 0, samDesired, out hKey) == 0)
            {
                uint size = 1024;
                uint type;
                StringBuilder keyBuffer = new StringBuilder(1024);
                if (RegQueryValueEx(hKey, valueName, 0, out type, keyBuffer, ref size) == 0)
                    keyvalue = keyBuffer.ToString();

                RegCloseKey(hKey);
                return true;
            }
            return false;
        }
    
        static CESDKAPI_Signature()
        {
            bIs64BitCESDK = (IntPtr.Size==8);
            string strCommonLibBin=null;
            if (GetCommonLibrariesBin(ref strCommonLibBin) == true)
            {
                if (bIs64BitCESDK)
                    strCommonLibBin += "cesdk.dll";
                else
                    strCommonLibBin += "cesdk32.dll";
                IntPtr hModule=LoadLibrary(strCommonLibBin);
                if (hModule == IntPtr.Zero)
                {
                    System.Diagnostics.Trace.WriteLine("Fail to load CESDK with the path:" + strCommonLibBin);
                }
            }
        }
    }
    //Define CE SDK .NET 
    public class CESDKAPI
    {
        /* ------------------------------------------------------------------------
         * CECONN_Initialize()
         *
         * Initializes the connection between the client to the Policy Decision
         * Point Server.
         * 
         * Arguments : 
         *             app (INPUT): the application assoicate with the client PEP
         *             user: to identify a user in the application
         *             pdpHost (INPUT): Name of PDP host. If it is NULL, it means the
         *                              local machine.
         *             timeout_in_millisec (INPUT): Desirable timeout in milliseconds 
         *             for this RPC
         *             connectHandle (OUTPUT): connection handle for subsequent call
         * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
         * ------------------------------------------------------------------------
         */
        public static CETYPE.CEResult_t CECONN_Initialize(CETYPE.CEApplication app,
                                                          CETYPE.CEUser user,
                                                          string pdpHost,
                                                          out IntPtr connectHandle,
                                                          int timeout_in_millisec)
        {
            CEString ces_appName = new CEString();
            if (app.appName != null)
            {
                ces_appName.length = app.appName.Length;
                ces_appName.buf = app.appName;
            }
            else
            {
                ces_appName.length = 0;
                ces_appName.buf = null;
            }

            CEString ces_binaryPath = new CEString();
            if (app.appPath != null)
            {
                ces_binaryPath.length = app.appPath.Length;
                ces_binaryPath.buf = app.appPath;
            }
            else
            {
                ces_binaryPath.length = 0;
                ces_binaryPath.buf = null;
            }

            CEString ces_userName = new CEString();
            if (user.userName != null)
            {
                ces_userName.length = user.userName.Length;
                ces_userName.buf = user.userName;
            }
            else
            {
                ces_userName.length = 0;
                ces_userName.buf = null;
            }

            CEString ces_userID = new CEString();
            if (user.userID != null)
            {
                ces_userID.length = user.userID.Length;
                ces_userID.buf = user.userID;
            }
            else
            {
                ces_userID.length = 0;
                ces_userID.buf = null;
            }

            CEString ces_pdpHost = new CEString();
            if (pdpHost != null)
                ces_pdpHost.length = pdpHost.Length;
            else
                ces_pdpHost.length = 0;
            ces_pdpHost.buf = pdpHost;

            Thread.BeginThreadAffinity();
            CETYPE.CEResult_t result = CESDKAPI_Signature.CSCINVOKE_CECONN_Initialize(
                        ces_appName,
                        ces_binaryPath,
                        ces_userName,
                        ces_userID,
                        ces_pdpHost,
                        out connectHandle,
                        timeout_in_millisec);
            Thread.EndThreadAffinity();
            return result;
        }

        /* ------------------------------------------------------------------------
         * CECONN_Close()
         *
         * Close the connection between the client and the Policy Decision
         * Point Server.
         * 
         * Arguments : handle (INPUT): connection handle from the CONN_initialize API
         *             timeout_in_millisec (INPUT): Desirable timeout in milliseconds 
         *             for this RPC
         *             
         * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
         * ------------------------------------------------------------------------
         */
        public static CETYPE.CEResult_t CECONN_Close(IntPtr handle,
                                    int timeout_in_millisec)
        {
            Thread.BeginThreadAffinity();
            CETYPE.CEResult_t result = CESDKAPI_Signature.CECONN_Close(handle,
                                                                       timeout_in_millisec);
            Thread.EndThreadAffinity();
            return result;
        }

        /* ------------------------------------------------------------------------
         * CEEVALUATE_CheckPortal()
         *
         * Ask the Policy Decision Point Server to evaluate the operation.
         *
         * Arguments : 
         * handle (INPUT): Handle from the CONN_Initialize()
         * Operation (INPUT): Operation on the file
         * sourceURL (INPUT): the URL to the source resource
         * targetURL (INPUT): the URL to the target resource
         * performObligation (INPUT): Perform the obligation defined by the policy 
         *                            (e.g. logging / email)
         * sourceAttributes	(INPUT): Associate attributes of the source. This is
         *   a string array in the order of "key-1""value-1""key-2""value-2"...
         * targetAttributes	(INPUT): Associate attributes of the target. This is
         *   a string array in the order of "key-1""value-1""key-2""value-2"...
         * noiseLevel	(INPUT): Desirable noise level to be used for this evaluation
         * ipAddress (INPUT): For Sharepointe, the ip address of client machine
         * this evaluation
         * user (INPUT): to identify the user who accesses the URL
         * enforcement_ob (OUTPUT): the resulted enforcement obligation  from 
         *   the policy decision point server. This is a string array in the order of 
         *   "key-1""value-1""key-2""value-2"...
         * enforcement_result (OUTPUT): the resulted enforcement integer decision.
         * timeout_in_millisec (INPUT): Desirable timeout in milliseconds for 
         *                              this RPC
         *
         * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
         * ------------------------------------------------------------------------
         */
        public static CETYPE.CEResult_t CEEVALUATE_CheckPortal(IntPtr handle,
                                    CETYPE.CEAction operation,
                                    string sourceURL,
                                    ref string[] sourceAttributes,
                                    string targetURL,
                                    ref string[] targetAttributes,
                                    uint ipAddress,
                                    CETYPE.CEUser user,
                                    bool performObligation,
                                    CETYPE.CENoiseLevel_t noiseLevel,
                                    out string[] enforcement_obligation,
                                    out CETYPE.CEResponse_t enforcement_result,
                                    int timeout_in_millisec)
        {
            CEString ces_sourceURL = new CEString();
            if (sourceURL != null)
                ces_sourceURL.length = sourceURL.Length;
            else
                ces_sourceURL.length = 0;
            ces_sourceURL.buf = sourceURL;

            CEString ces_targetURL = new CEString();
            if (targetURL != null)
                ces_targetURL.length = targetURL.Length;
            else
                ces_targetURL.length = 0;
            ces_targetURL.buf = targetURL;

            CEString ces_userName = new CEString();
            if (user.userName != null)
            {
                ces_userName.length = user.userName.Length;
                ces_userName.buf = user.userName;
            }
            else
            {
                ces_userName.length = 0;
                ces_userName.buf = null;
            }

            CEString ces_userID = new CEString();
            if (user.userID != null)
            {
                ces_userID.length = user.userID.Length;
                ces_userID.buf = user.userID;
            }
            else
            {
                ces_userID.length = 0;
                ces_userID.buf = null;
            }

            IntPtr enforcement_ob;
            int numEnforcements;

            CETYPE.CEResult_t result;
            Thread.BeginThreadAffinity();
            result = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_CheckPortal(handle,
                                    operation,
                                    ces_sourceURL,
                                    sourceAttributes,
                                    (sourceAttributes != null) ? sourceAttributes.Length : 0,
                                    ces_targetURL,
                                    targetAttributes,
                                    (targetAttributes != null) ? targetAttributes.Length : 0,
                                    ipAddress,
                                    ces_userName,
                                    ces_userID,
                                    performObligation,
                                    noiseLevel,
                                    out enforcement_ob,
                                    out enforcement_result,
                                    out numEnforcements,
                                    timeout_in_millisec);
            Thread.EndThreadAffinity();

            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                numEnforcements = 0;
                enforcement_obligation = null;
                return result;
            }

            enforcement_obligation = new string[numEnforcements];
            int strLen;
            IntPtr strPtr;
            for (int i = 0; i < numEnforcements; i++)
            {
                strLen = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_GetString(enforcement_ob,
                                             i, out strPtr);
                if(strLen != 0)
                    enforcement_obligation[i] = Marshal.PtrToStringAuto(strPtr, strLen);
                else

                    enforcement_obligation[i] = null;
            }
            CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_FreeStringArray(enforcement_ob,
                                        numEnforcements);
            return result;
        }
        /* ------------------------------------------------------------------------
        * CEEVALUATE_CheckFile()
        *
        * Ask the Policy Decision Point Server to evaluate the operation on file.
        *
        * Arguments : 
        * handle (INPUT): Handle from the CONN_Initialize()
        * Operation (INPUT): Operation on the file
        * sourceFile (INPUT): the source file.
        * targetFile (INPUT): the target file.
        * performObligation (INPUT): Perform the obligation defined by the policy 
        *                            (e.g. logging / email)
        * sourceAttributes	(INPUT): Associate attributes of the source. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * targetAttributes	(INPUT): Associate attributes of the target. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * noiseLevel	(INPUT): Desirable noise level to be used for this evaluation
        * ipAddress (INPUT): For Sharepointe, the ip address of client machine
        * this evaluation
        * user (INPUT): to identify the user who accesses the files.
        * app (INPUT): to identify the application that does the operation. 
        * enforcement_ob (OUTPUT): the resulted enforcement obligation  from 
        *   the policy decision point server. This is a string array in the order of 
        *   "key-1""value-1""key-2""value-2"...
        * enforcement_result (OUTPUT): the resulted enforcement integer decision.
        * timeout_in_millisec (INPUT): Desirable timeout in milliseconds for 
        *                              this RPC
        *
        * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
        * ------------------------------------------------------------------------
        */
        public static CETYPE.CEResult_t CEEVALUATE_CheckFile(IntPtr handle,
                                    CETYPE.CEAction operation,
                                    string sourceFile,
                                    ref string[] sourceAttributes,
                                    string targetFile,
                                    ref string[] targetAttributes,
                                    uint ipAddress,
                                    CETYPE.CEUser user,
                                    CETYPE.CEApplication app,
                                    bool performObligation,
                                    CETYPE.CENoiseLevel_t noiseLevel,
                                    out string[] enforcement_obligation,
                                    out CETYPE.CEResponse_t enforcement_result,
                                    int timeout_in_millisec)
        {
            CEString ces_source = new CEString();
            if (sourceFile != null)
                ces_source.length = sourceFile.Length;
            else
                ces_source.length = 0;
            ces_source.buf = sourceFile;

            CEString ces_target = new CEString();
            if (targetFile != null)
                ces_target.length = targetFile.Length;
            else
                ces_target.length = 0;
            ces_target.buf = targetFile;

            //Compose user name
            CEString ces_userName = new CEString();
            if (user.userName != null)
            {
                ces_userName.length = user.userName.Length;
                ces_userName.buf = user.userName;
            }
            else
            {
                ces_userName.length = 0;
                ces_userName.buf = null;
            }

            //Compose user ID
            CEString ces_userID = new CEString();
            if (user.userID != null)
            {
                ces_userID.length = user.userID.Length;
                ces_userID.buf = user.userID;
            }
            else
            {
                ces_userID.length = 0;
                ces_userID.buf = null;
            }

            //Compose application name
            CEString ces_appName = new CEString();
            if (app.appName != null)
            {
                ces_appName.length = app.appName.Length;
                ces_appName.buf = app.appName;
            }
            else
            {
                ces_appName.length = 0;
                ces_appName.buf = null;
            }

            //Compose application path
            CEString ces_appPath = new CEString();
            if (app.appPath != null)
            {
                ces_appPath.length = app.appPath.Length;
                ces_appPath.buf = app.appPath;
            }
            else
            {
                ces_appPath.length = 0;
                ces_appPath.buf = null;
            }

            //Compose application URL
            CEString ces_appURL = new CEString();
            if (app.appURL != null)
            {
                ces_appURL.length = app.appURL.Length;
                ces_appURL.buf = app.appURL;
            }
            else
            {
                ces_appURL.length = 0;
                ces_appURL.buf = null;
            }

            IntPtr enforcement_ob;
            int numEnforcements;

            CETYPE.CEResult_t result;
            Thread.BeginThreadAffinity();
            result = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_CheckFile(handle,
                                    operation,
                                    ces_source,
                                    sourceAttributes,
                                    (sourceAttributes != null) ? sourceAttributes.Length : 0,
                                    ces_target,
                                    targetAttributes,
                                    (targetAttributes != null) ? targetAttributes.Length : 0,
                                    ipAddress,
                                    ces_userName,
                                    ces_userID,
                                    ces_appName,
                                    ces_appPath,
                                    ces_appURL,
                                    performObligation,
                                    noiseLevel,
                                    out enforcement_ob,
                                    out enforcement_result,
                                    out numEnforcements,
                                    timeout_in_millisec);
            Thread.EndThreadAffinity();

            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                numEnforcements = 0;
                enforcement_obligation = null;
                return result;
            }

            enforcement_obligation = new string[numEnforcements];
            int strLen;
            IntPtr strPtr;
            for (int i = 0; i < numEnforcements; i++)
            {
                strLen = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_GetString(enforcement_ob,
                                             i, out strPtr);

                if (strLen != 0)

                    enforcement_obligation[i] = Marshal.PtrToStringAuto(strPtr, strLen);

                else

                    enforcement_obligation[i] = null;
            }
            CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_FreeStringArray(enforcement_ob,
                                        numEnforcements);
            return result;
        }

        /* ------------------------------------------------------------------------
        * CEEVALUATE_CheckMessageAttachment()
        *
        * Ask the Policy Decision Point Server to evaluate the operation on sending
        * a message with attachment.
        *
        * Arguments : 
        * handle (INPUT): Handle from the CONN_Initialize()
        * Operation (INPUT): Operation on the file
        * sourceFile (INPUT): the source file.
        * performObligation (INPUT): Perform the obligation defined by the policy 
        *                            (e.g. logging / email)
        * sourceAttributes	(INPUT): Associate attributes of the source. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * recipients (INPUT): the string array of the message recipients
        * noiseLevel	(INPUT): Desirable noise level to be used for this evaluation
        * ipAddress (INPUT): For Sharepointe, the ip address of client machine
        * this evaluation
        * user (INPUT): to identify the user who accesses the files.
        * userAttributes	(INPUT): Associate attributes of the user. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * app (INPUT): to identify the application that does the operation. 
        * appAttributes	(INPUT): Associate attributes of the application. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * enforcement_ob (OUTPUT): the resulted enforcement obligation  from 
        *   the policy decision point server. This is a string array in the order of 
        *   "key-1""value-1""key-2""value-2"...
        * enforcement_result (OUTPUT): the resulted enforcement integer decision.
        * timeout_in_millisec (INPUT): Desirable timeout in milliseconds for 
        *                              this RPC
        *
        * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
        * ------------------------------------------------------------------------
        */
        public static CETYPE.CEResult_t CEEVALUATE_CheckMessageAttachment(IntPtr handle,
                                    CETYPE.CEAction operation,
                                    string sourceFile,
                                    ref string[] sourceAttributes,
                                    ref string[] recipients,
                                    uint ipAddress,
                                    CETYPE.CEUser user,
                                    ref string[] userAttributes,
                                    CETYPE.CEApplication app,
                                    ref string[] appAttributes,
                                    bool performObligation,
                                    CETYPE.CENoiseLevel_t noiseLevel,
                                    out string[] enforcement_obligation,
                                    out CETYPE.CEResponse_t enforcement_result,
                                    int timeout_in_millisec)
        {
            CEString ces_source = new CEString();
            if (sourceFile != null)
                ces_source.length = sourceFile.Length;
            else
                ces_source.length = 0;
            ces_source.buf = sourceFile;

            //Compose user name
            CEString ces_userName = new CEString();
            if (user.userName != null)
            {
                ces_userName.length = user.userName.Length;
                ces_userName.buf = user.userName;
            }
            else
            {
                ces_userName.length = 0;
                ces_userName.buf = null;
            }

            //Compose user ID
            CEString ces_userID = new CEString();
            if (user.userID != null)
            {
                ces_userID.length = user.userID.Length;
                ces_userID.buf = user.userID;
            }
            else
            {
                ces_userID.length = 0;
                ces_userID.buf = null;
            }

            //Compose application name
            CEString ces_appName = new CEString();
            if (app.appName != null)
            {
                ces_appName.length = app.appName.Length;
                ces_appName.buf = app.appName;
            }
            else
            {
                ces_appName.length = 0;
                ces_appName.buf = null;
            }

            //Compose application path
            CEString ces_appPath = new CEString();
            if (app.appPath != null)
            {
                ces_appPath.length = app.appPath.Length;
                ces_appPath.buf = app.appPath;
            }
            else
            {
                ces_appPath.length = 0;
                ces_appPath.buf = null;
            }

            //Compose application URL
            CEString ces_appURL = new CEString();
            if (app.appURL != null)
            {
                ces_appURL.length = app.appURL.Length;
                ces_appURL.buf = app.appURL;
            }
            else
            {
                ces_appURL.length = 0;
                ces_appURL.buf = null;
            }

            IntPtr enforcement_ob;
            int numEnforcements;

            CETYPE.CEResult_t result;
            Thread.BeginThreadAffinity();
            result = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_CheckMsgAttachment(handle,
                                    operation,
                                    ces_source,
                                    sourceAttributes,
                                    (sourceAttributes != null) ? sourceAttributes.Length : 0,
                                    (recipients != null) ? recipients.Length : 0,
                                    recipients,
                                    ipAddress,
                                    ces_userName,
                                    ces_userID,
                                    userAttributes,
                                    (userAttributes != null) ? userAttributes.Length : 0,
                                    ces_appName,
                                    ces_appPath,
                                    ces_appURL,
                                    appAttributes,
                                    (appAttributes != null) ? appAttributes.Length : 0,
                                    performObligation,
                                    noiseLevel,
                                    out enforcement_ob,
                                    out enforcement_result,
                                    out numEnforcements,
                                    timeout_in_millisec);
            Thread.EndThreadAffinity();

            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                numEnforcements = 0;
                enforcement_obligation = null;
                return result;
            }

            enforcement_obligation = new string[numEnforcements];
            int strLen;
            IntPtr strPtr;
            for (int i = 0; i < numEnforcements; i++)
            {
                strLen = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_GetString(enforcement_ob,
                                             i, out strPtr);

                if (strLen != 0)

                    enforcement_obligation[i] = Marshal.PtrToStringAuto(strPtr, strLen);

                else

                    enforcement_obligation[i] = null;
            }
            CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_FreeStringArray(enforcement_ob,
                                        numEnforcements);
            return result;
        }





        /* ------------------------------------------------------------------------
        * CEEVALUATE_CheckResources()
        *
        * Ask the Policy Controller to evaluate the operation on resources.
        *
        * Arguments : 
        * handle (INPUT): Handle from the CONN_Initialize()
        * Operation (INPUT): Operation on the file
        * source (INPUT): the source resource.
        * sourceAttributes	(INPUT): Associate attributes of the source. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * target (INPUT): the target resource.
        * targetAttributes	(INPUT): Associate attributes of the target. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * user (INPUT): to identify the user who accesses the files.
        * userAttributes	(INPUT): Associate attributes of the user. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * app (INPUT): to identify the application that does the operation. 
        * appAttributes	(INPUT): Associate attributes of the application. This is
        *   a string array in the order of "key-1""value-1""key-2""value-2"...
        * recipients (INPUT): the string array of the recipients for the case of messaging. 
        * ipAddress (INPUT): For Sharepointe, the ip address of client machine
        * performObligation (INPUT): Perform the obligation defined by the policy 
        *                            (e.g. logging / email)
        * noiseLevel	(INPUT): Desirable noise level to be used for this evaluation
        * enforcement_ob (OUTPUT): the resulted enforcement obligation  from 
        *   the policy decision point server. This is a string array in the order of 
        *   "key-1""value-1""key-2""value-2"...
        * enforcement_result (OUTPUT): the resulted enforcement integer decision.
        * timeout_in_millisec (INPUT): Desirable timeout in milliseconds for 
        *                              this RPC
        *
        * Note:
        *   Resource names entered in "source" and "target" are different from 
        *   attributes called "name" in "sourceAttributes" and "targetAttributes".
        *   The former is copied into CE::ID, and is used by Policy Controller as
        *     an internal identification number.
        *   The latter is matched against resource.*.name in PQL.
        *   If you try to match a resource name against resource.*.name, you need to
        *     set "name" in "sourceAttributes" or "targetAttributes" explicitly.
        *
        * Return: return CETYPE.CE_RESULT_SUCCESS if the call succeeds.
        * ------------------------------------------------------------------------
        */

        public static CETYPE.CEResult_t CEEVALUATE_CheckResources(IntPtr handle,
                                    string operation,
                                    CETYPE.CEResource source,
                                    ref string[] sourceAttributes,
                                    CETYPE.CEResource target,
                                    ref string[] targetAttributes,
                                    CETYPE.CEUser user,
                                    ref string[] userAttributes,
                                    CETYPE.CEApplication app,
                                    ref string[] appAttributes,
                                    ref string[] recipients,
                                    uint ipAddress,
                                    bool performObligation,
                                    CETYPE.CENoiseLevel_t noiseLevel,
                                    out string[] enforcement_obligation,
                                    out CETYPE.CEResponse_t enforcement_result,
                                    int timeout_in_millisec)
        {
            //compose operation     
            CEString ces_operation = new CEString();
            if (operation != null)
                ces_operation.length = operation.Length;
            else
                ces_operation.length = 0;
            ces_operation.buf = operation;
            
            //compose source.resource_name
            CEString ces_source_name = new CEString();
            if (source.resourceName != null)
                ces_source_name.length = source.resourceName.Length;
            else
                ces_source_name.length = 0;
            ces_source_name.buf = source.resourceName;

            //compose source.resource_type
            CEString ces_source_type = new CEString();
            if (source.resourceType != null)
                ces_source_type.length = source.resourceType.Length;
            else
                ces_source_type.length = 0;
            ces_source_type.buf = source.resourceType;

            //compose target.resource_name
            CEString ces_target_name = new CEString();
            if (target.resourceName != null)
                ces_target_name.length = target.resourceName.Length;
            else
                ces_target_name.length = 0;
            ces_target_name.buf = target.resourceName;

            //compose target.resource_type
            CEString ces_target_type = new CEString();
            if (target.resourceType != null)
                ces_target_type.length = target.resourceType.Length;
            else
                ces_target_type.length = 0;
            ces_target_type.buf = target.resourceType;

            //Compose user name
            CEString ces_userName = new CEString();
            if (user.userName != null)
            {
                ces_userName.length = user.userName.Length;
                ces_userName.buf = user.userName;
            }
            else
            {
                ces_userName.length = 0;
                ces_userName.buf = null;
            }

            //Compose user ID
            CEString ces_userID = new CEString();
            if (user.userID != null)
            {
                ces_userID.length = user.userID.Length;
                ces_userID.buf = user.userID;
            }
            else
            {
                ces_userID.length = 0;
                ces_userID.buf = null;
            }

            //Compose application name
            CEString ces_appName = new CEString();
            if (app.appName != null)
            {
                ces_appName.length = app.appName.Length;
                ces_appName.buf = app.appName;
            }
            else
            {
                ces_appName.length = 0;
                ces_appName.buf = null;
            }

            //Compose application path
            CEString ces_appPath = new CEString();
            if (app.appPath != null)
            {
                ces_appPath.length = app.appPath.Length;
                ces_appPath.buf = app.appPath;
            }
            else
            {
                ces_appPath.length = 0;
                ces_appPath.buf = null;
            }

            //Compose application URL
            CEString ces_appURL = new CEString();
            if (app.appURL != null)
            {
                ces_appURL.length = app.appURL.Length;
                ces_appURL.buf = app.appURL;
            }
            else
            {
                ces_appURL.length = 0;
                ces_appURL.buf = null;
            }

            IntPtr enforcement_ob;
            int numEnforcements;

            CETYPE.CEResult_t result;
            Thread.BeginThreadAffinity();
            result = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_CheckResources(handle,
                                    ces_operation,
                                    ces_source_name,
                                    ces_source_type,
                                    sourceAttributes,
                                    (sourceAttributes != null) ? sourceAttributes.Length : 0,
                                    ces_target_name,
                                    ces_target_type,
                                    targetAttributes,
                                    (targetAttributes != null) ? targetAttributes.Length : 0,
                                    ces_userName,
                                    ces_userID,
                                    userAttributes,
                                    (userAttributes != null) ? userAttributes.Length : 0,
                                    ces_appName,
                                    ces_appPath,
                                    ces_appURL,
                                    appAttributes,
                                    (appAttributes != null) ? appAttributes.Length : 0,
                                    (recipients != null) ? recipients.Length : 0,
                                    recipients,
                                    ipAddress,
                                    performObligation,
                                    noiseLevel,
                                    out enforcement_ob,
                                    out enforcement_result,
                                    out numEnforcements,
                                    timeout_in_millisec);
            Thread.EndThreadAffinity();

            if (result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
            {
                numEnforcements = 0;
                enforcement_obligation = null;
                return result;
            }

            enforcement_obligation = new string[numEnforcements];
            int strLen;
            IntPtr strPtr;
            for (int i = 0; i < numEnforcements; i++)
            {
                strLen = CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_GetString(enforcement_ob,
                                             i, out strPtr);
                if (strLen != 0)
                    enforcement_obligation[i] = Marshal.PtrToStringAuto(strPtr, strLen);
                else
                    enforcement_obligation[i] = null;
            }
            CESDKAPI_Signature.CSCINVOKE_CEEVALUATE_FreeStringArray(enforcement_ob,
                                        numEnforcements);
            return result;
        }
        
        /* ------------------------------------------------------------------------
         /*! CELOGGING_LogObligationData
          *
          * \brief This assistant logging obligation. This function will be called by the Policy Assistant 
          * (or by multiple Policy Assistants).
          * 
          * \param logIdentifier (in): Taken from the obligation information.  Note that this is actually a long integer, 
          * \param obligationName: The name of the obligation (e.g. "CE Encryption Assistant"
          * \param attributes (in): These are unstructured key/value pairs representing information that this particular 
          * Policy Assistant would like presented in the log. Currently, only the first three attributes will be assigned the fields in the log.
          *
          * \return Result of logging.
          *
          * \sa CELOGGING_LogObligationData
          */
        public static CETYPE.CEResult_t CELOGGING_LogObligationData(IntPtr handle,
                                                                    string logIdentifier,
                                                                    string assistantName,
                                                                    ref string[] attributes)
        {
            //logIdentifier
            CEString ces_logIdentifier = new CEString();
            if (logIdentifier != null)
                ces_logIdentifier.length = logIdentifier.Length;
            else
                ces_logIdentifier.length = 0;
            ces_logIdentifier.buf = logIdentifier;

            //assistantName
            CEString ces_assistantName = new CEString();
            if (assistantName != null)
                ces_assistantName.length = assistantName.Length;
            else
                ces_assistantName.length = 0;
            ces_assistantName.buf = assistantName;

            CETYPE.CEResult_t result;
            Thread.BeginThreadAffinity();
            result = CESDKAPI_Signature.CSCINVOKE_CELOGGING_LogObligationData(handle,
                                    ces_logIdentifier,
                                    ces_assistantName,
                                    attributes,
                                    (attributes != null) ? attributes.Length : 0);
            Thread.EndThreadAffinity();

            return result;
        }
    }
}

