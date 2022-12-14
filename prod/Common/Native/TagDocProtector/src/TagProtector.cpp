// TagProtector.cpp : Implementation of CTagProtector

#include "stdafx.h"
#include "TagProtector.h"
#include "log.h"
#include "FileEncryptIgnore.h"
#include <stdio.h>
#include <Aclapi.h>

#pragma comment(lib,"mscoree.lib")

// CTagProtector
// Global variables
HINSTANCE g_hInstance = 0;
long      g_cLocks    = 0;
const WCHAR wzKey[] = L"I am protected";
const WCHAR wzSignedIL[] = L"SignedIL";
const WCHAR wzServerID[] = L"ServerID";
const WCHAR wzLicenses[] = L"Licenses";
const WCHAR wzRightsTemplate[] = L"RightsTemplate";
const WCHAR wzListGuid[] = L"ListGuid";
const WCHAR wzContent[] = L"Content";
const WCHAR wzRightsMask[] = L"RightsMask";
const WCHAR wzRequestingUser[] = L"RequestingUser";
const WCHAR wzURL[] = L"URL";
const WCHAR wzPolicyTitle[] = L"PolicyTitle";
const WCHAR wzPolicyDescription[] = L"PolicyDescription";
const WCHAR wzOfflineDays[] = L"OfflineDays";
const int STREAM_MAX = 32;
const WCHAR wzEULPrefix[] = L"EUL-";
#define STACK_RW_BUF_SIZE (8192)

#ifndef Unreferenced
#define Unreferenced(x)    ((void)x)
#endif

#ifndef cElements
#define cElements(ary) (sizeof(ary) / sizeof(ary[0]))
#endif

#define GUIDLEN 38 //{00000000-0000-0000-0000-000000000000}

CFileTaggingList g_listFileTagging;

static BOOL bUsersPermissonSet = FALSE;

#define NEW_FILE_PATH 1024
#define IRM_ERROR_MESSAGE L"IRM protected failed, Nextlabs IRM module clear the content, please check IT Admin about this issue."
/*-----------------------------------------------------------------------------
    CTagProtector::CTagProtector
-----------------------------------------------------------------------------*/
CTagProtector::CTagProtector() 
    : m_uRefCount(1),
      m_langid(1033)
{
	CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ::InterlockedIncrement(&g_cLocks);
	
	//call GetInstance to protect create two objects in different threads.
	CFileEncryptIgnore::GetInstance();
}

/*-----------------------------------------------------------------------------
    CTagProtector::~CTagProtector
-----------------------------------------------------------------------------*/
CTagProtector::~CTagProtector() 
{
	CoUninitialize();		
    ::InterlockedDecrement(&g_cLocks);
}

/*-----------------------------------------------------------------------------
    CTagProtector::QueryInterface
-----------------------------------------------------------------------------*/
HRESULT CTagProtector::QueryInterface(const IID& riid, void** ppvObj)
{
    if (riid == IID_IUnknown || riid == IID_I_IrmProtector)
        {
        *ppvObj = (I_IrmProtector*)this;
        AddRef();
        return S_OK;
        }
    *ppvObj = 0;
    return E_NOINTERFACE;
}

/*-----------------------------------------------------------------------------
    CTagProtector::AddRef
-----------------------------------------------------------------------------*/
unsigned long CTagProtector::AddRef()
{
    return ++m_uRefCount;
}

/*-----------------------------------------------------------------------------
    CTagProtector::Release
-----------------------------------------------------------------------------*/
unsigned long CTagProtector::Release()
{
    if (--m_uRefCount != 0)
        return m_uRefCount;
    delete this;
    return 0;
}

/*-----------------------------------------------------------------------------
    CTagProtector::HrInit
------------------------------------------------------------------------------*/
STDMETHODIMP CTagProtector::HrInit(BSTR *pbstrProduct, DWORD *pdwVersion, BSTR *pbstrExtensions, BOOL *pfUseRMS)
{
	if ( pbstrExtensions == NULL || pfUseRMS == NULL)
	{
		DPW((L"CTagProtector::HrInit, Something is null \n"));
		return E_INVALIDARG;
	}        

    *pfUseRMS        = false; //set to "false" to use the non-RMS infrastructure

	HKEY hKeyTagProtector = NULL;
	LONG lResult = 0;
	WCHAR wzKeyValue[1024];memset(wzKeyValue, 0, sizeof(wzKeyValue));
	DWORD dwValueLen = 1023;
	DWORD dwValueType = 0;

	lResult = RegOpenKeyExW(HKEY_LOCAL_MACHINE, 
		L"SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\16.0\\IrmProtectors\\TagDocProtector", 
		0, KEY_READ, &hKeyTagProtector);
	if (lResult != ERROR_SUCCESS || hKeyTagProtector == NULL)
	{
		lResult = RegOpenKeyExW(HKEY_LOCAL_MACHINE, 
			L"SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\15.0\\IrmProtectors\\TagDocProtector", 
			0, KEY_READ, &hKeyTagProtector);
	}


	if (lResult == ERROR_SUCCESS && hKeyTagProtector)
	{
		dwValueLen = 1023;
		RegQueryValueExW(hKeyTagProtector, L"Product", NULL, 
			&dwValueType, (LPBYTE)wzKeyValue, &dwValueLen);
		*pbstrProduct    = SysAllocString(wzKeyValue);

		dwValueLen = 1023;
		RegQueryValueExW(hKeyTagProtector, L"Version", NULL, 
			&dwValueType, (LPBYTE)wzKeyValue, &dwValueLen);
		*pdwVersion    = (DWORD)_wtoi(wzKeyValue);

		dwValueLen = 1023;
		RegQueryValueExW(hKeyTagProtector, L"Extensions", NULL, 
			&dwValueType, (LPBYTE)wzKeyValue, &dwValueLen);
		*pbstrExtensions    = SysAllocString(wzKeyValue);

		RegCloseKey(hKeyTagProtector);
		hKeyTagProtector = NULL;
		DPW((L"CTagProtector::HrInit Read Reg value \n"));
	}
	else
	{
		*pbstrProduct    = SysAllocString(PRODUCT_NAME);
		*pdwVersion      = 1;
		*pbstrExtensions = SysAllocString(L"doc,dot,xls,xlt,xla,ppt,pot,pps,xps,docx,docm,dotx,dotm,xlsx,xlsm,xlsb,xltx,xltm,xlam,pptx,pptm,potx,potm,thmx,ppsx,ppsm,pdf,tiff,tif,nxl");
	}

    return S_OK;
}

/*-----------------------------------------------------------------------------
    CTagProtector::HrIsProtected
-----------------------------------------------------------------------------*/
HRESULT CTagProtector::HrIsProtected(ILockBytes *pilbInput, DWORD *pdwResult)
{
    DPW((L"CTagProtector::HrIsProtected \n"));	
	HRESULT hr = S_OK;
    if (pilbInput == NULL || pdwResult == NULL)
    {
        hr = E_INVALIDARG;
        return hr;
    }
	else
		*pdwResult=MSOIPI_RESULT_UNPROTECTED;
	return hr;
}

/*-----------------------------------------------------------------------------
    CTagProtector::HrSetLangId
-----------------------------------------------------------------------------*/
STDMETHODIMP CTagProtector::HrSetLangId(LANGID langid)
{
    m_langid = langid;
    return S_OK;
}

/*-----------------------------------------------------------------------------
    CTagProtector::HrProtectRMS
-----------------------------------------------------------------------------*/
HRESULT CTagProtector::HrProtectRMS(ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfoRMS *piid, DWORD *pdwStatus)
{
	pdwStatus;piid;pilbOutput;pilbInput;
    DPW((L"CTagProtector::HrProtectRMS \n"));
	return E_FAIL;
}

/*-----------------------------------------------------------------------------
    CTagProtector::HrUnprotectRMS
	Use RMS to decrypt
-----------------------------------------------------------------------------*/
HRESULT CTagProtector::HrUnprotectRMS(ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfoRMS *piid, DWORD *pdwStatus)
{
	pdwStatus;piid;pilbOutput;pilbInput;
    DPW((L"CTagProtector::HrUnprotectRMS \n"));
	return E_FAIL;
}

#define cElements(ary) (sizeof(ary) / sizeof(ary[0]))

/*-----------------------------------------------------------------------------
    CTagProtector::HrProtect
------------------------------------------------------------------------------*/
HRESULT CTagProtector::HrProtect(ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfo *piid, DWORD *pdwStatus)
{
    OutputDebugString(L"CTagProtector::HrProtect");	
	HRESULT hr = S_OK;
    WCHAR wzFullURL[NEW_FILE_PATH + 1]; memset(wzFullURL, 0, sizeof(wzFullURL));
    WCHAR wzRemoteUser[256]; memset(wzRemoteUser, 0, sizeof(wzRemoteUser));
    WCHAR wzGUID[256]; memset(wzGUID, 0, sizeof(wzGUID));
	BSTR bstr = NULL;
    if (pilbInput == NULL || pilbOutput == NULL || piid == NULL || pdwStatus == NULL)
    {
        DPW((L"HrProtect: Something is null \n"));
		hr = E_INVALIDARG;
        return hr;
    }
	hr = piid->HrGetListGuid(&bstr);
	if(SUCCEEDED(hr))
	{
        swprintf_s(wzGUID, 255, L"%s", bstr);
		::SysFreeString(bstr);bstr=NULL;
	}

	BOOL bIsSystem=FALSE;
	hr = piid->HrGetRequestingUser(&bstr,&bIsSystem);
	if(SUCCEEDED(hr))
	{
        swprintf_s(wzRemoteUser, 255, L"%s", bstr);
		::SysFreeString(bstr);bstr=NULL;
	}

	hr=piid->HrGetURL(&bstr);
	if(SUCCEEDED(hr))
	{
        swprintf_s(wzFullURL, NEW_FILE_PATH, L"%s", bstr);
		::SysFreeString(bstr);bstr=NULL;
	}
	STATSTG statStgIn;
	hr=pilbInput->Stat(&statStgIn,STATFLAG_DEFAULT);
	if(SUCCEEDED(hr) && statStgIn.pwcsName != NULL)
	{
		//get full url
        wcscat_s(wzFullURL, NEW_FILE_PATH, L"/");
        wcscat_s(wzFullURL, NEW_FILE_PATH, statStgIn.pwcsName);
		CoTaskMemFree(statStgIn.pwcsName);

		//tag file
		EncryptFileInfo* pIgnoreFileInfo = CFileEncryptIgnore::GetInstance()->GetIgnoreFile(wzFullURL, wzRemoteUser, GetTickCount());
		if (NULL == pIgnoreFileInfo)// NULL means not in the ignore list
		{
			//mark the process result, if any of the process failed. we didn't do the next process, and mark the content of the file to "failed"
			BOOL bProcessSuccessed = TRUE;

			CFileTagging *pTempFileTagging = new CFileTagging(wzFullURL, wzRemoteUser);
			if(pTempFileTagging != NULL && pTempFileTagging->CheckInitStatus())
			{
				//save file to temp
				WCHAR wzTmpFilePath[NEW_FILE_PATH + 1];
				memset(wzTmpFilePath, 0, sizeof(wzTmpFilePath));
				GetTempFilePath(wzFullURL, wzTmpFilePath, NEW_FILE_PATH);
				CreateTempFile(wzTmpFilePath, pilbInput);
				BSTR bstrTempFile = SysAllocString(wzTmpFilePath);

				//tag
				CFileTagging *pFileTagging = g_listFileTagging.GetFileTaggingItem(wzFullURL, wzRemoteUser);
				if (pFileTagging)
				{
					{
						RevertThreadToken revertToken;
						pFileTagging->AddTags(wzTmpFilePath);
					}

					g_listFileTagging.RemoveFileTaggingItem(wzFullURL, wzRemoteUser, pFileTagging);
					pFileTagging = NULL;
				}
				
				//output
				if (bProcessSuccessed)
				{
					HRESULT hr = ExtractFromTempFile(wzTmpFilePath, pilbOutput);	
					if (hr != S_OK)
					{
						OutputErrorInfo(IRM_ERROR_MESSAGE, pilbOutput);
					}
				}

				//delete temp file
				DeleteFileW(wzTmpFilePath);
				*pdwStatus = MSOIPI_STATUS_PROTECT_SUCCESS;
				SysFreeString(bstrTempFile);
				bstrTempFile = NULL;
				delete pTempFileTagging;
			}
		}
		else
		{
			CFileEncryptIgnore::GetInstance()->RemoveFileIgnore(pIgnoreFileInfo);
		}
		return S_OK;

	} //if SUCCEEDED(hr)
	else
	{
		CopyLockBytes(pilbInput, pilbOutput);
	}

	return S_OK;
}


HRESULT CTagProtector::CreateTempFile(WCHAR * _TmpFilePath,ILockBytes *pilbInput)
{
	HRESULT hr = S_OK;
	if(_TmpFilePath != NULL)
	{
		FILE * fhandle=NULL;
		errno_t err= _wfopen_s(&fhandle,_TmpFilePath,L"w+b");
		if(err==0&&fhandle != NULL)
		{
			STATSTG statStgIn;
			hr=pilbInput->Stat(&statStgIn,STATFLAG_DEFAULT);
			if(SUCCEEDED(hr))
			{
				BYTE * _in = new BYTE[statStgIn.cbSize.QuadPart]; 	
				memset(_in,0x00,statStgIn.cbSize.QuadPart);
				ULARGE_INTEGER ulOffset = { 0 };
				ulOffset.QuadPart = 0;
				ULONG cbRead = 0;
				pilbInput->ReadAt(ulOffset,_in,statStgIn.cbSize.LowPart,&cbRead);
				fwrite(_in,sizeof(BYTE),statStgIn.cbSize.QuadPart,fhandle);
				fclose(fhandle);
				fhandle = NULL;
				delete[] _in;
				_in = NULL;
				return S_OK;
			}
			fclose(fhandle);
			fhandle = NULL;
		}
	}
	return E_FAIL;
}

HRESULT CTagProtector::ExtractFromTempFile(WCHAR * _TmpFilePath,ILockBytes *pilbOutput)
{
	//HRESULT hr = S_OK;
	if(_TmpFilePath != NULL)
	{
		FILE * fhandle =NULL;
		errno_t err= _wfopen_s(&fhandle,_TmpFilePath,L"r+b");
		if(err==0 && fhandle != NULL)
		{
			fseek(fhandle,0,SEEK_END);
			int file_size = ftell(fhandle);
			fseek(fhandle,0,SEEK_SET);
			BYTE * _out = new BYTE[file_size];
			memset(_out,0x00,file_size);
			fread(_out,sizeof(BYTE),file_size,fhandle);
			ULARGE_INTEGER ulOffset = { 0 };
			ulOffset.QuadPart = 0;
			ULONG cbWritten = 0;
			pilbOutput->WriteAt(ulOffset, _out, (ULONG)file_size, &cbWritten);
			fclose(fhandle);
			fhandle = NULL;
			delete[] _out;
			_out = NULL;
			return S_OK;
		}
		else
		{
			DPW((L"Open file failed, lastError=%d, path=%s \n", GetLastError(), _TmpFilePath));
		}
	}
	return E_FAIL;
}



/*-----------------------------------------------------------------------------
    CTagProtector::HrUnprotect
------------------------------------------------------------------------------*/
HRESULT CTagProtector::HrUnprotect(ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfo *piid, DWORD *pdwStatus)
{
	pdwStatus;piid;pilbOutput;pilbInput;
    DPW((L"CTagProtector::HrUnprotect \n"));
	return S_OK;
}


HRESULT CTagProtector::CopyLockBytes(ILockBytes*pilbInput,ILockBytes*pilbOutput)
{
	HRESULT hr=S_OK;
	BYTE rgbBuffer[STACK_RW_BUF_SIZE];
    ULARGE_INTEGER ulOffset = { 0 };

	ulOffset.QuadPart = 0;

	while (true)
	{
        ULONG cbRead = cElements(rgbBuffer);
        ULONG cbWritten = 0;
        hr = pilbInput->ReadAt(ulOffset,rgbBuffer, cbRead, &cbRead);
        if (FAILED(hr))
		{
            break;
		}
        if (cbRead == 0)
		{
            break;
		}
        hr = pilbOutput->WriteAt(ulOffset, rgbBuffer, cbRead, &cbWritten);
        if (FAILED(hr))
		{
            break;
		}
        if (cbRead != cbWritten)
        {
            hr = STG_E_CANTSAVE;
            break;
        }
        ulOffset.QuadPart += cbRead;
	}
	return hr;
}


BOOL CTagProtector::GetTempFilePath(LPWSTR lpwzFileName, LPWSTR lpwzTempFilePath, DWORD dwTempLen)
{

	if (!lpwzFileName || !lpwzTempFilePath || !dwTempLen)
		return FALSE;

	std::wstring strAppDataDir = GetAppDataDirectory();

	WCHAR* lpwzExtension = wcsrchr(lpwzFileName, L'.');

    WCHAR wzTmpName[NEW_FILE_PATH + 1] = { 0 };
    swprintf_s(wzTmpName, NEW_FILE_PATH, L"%d-%d%s", GetTickCount(), GetCurrentThreadId(), lpwzExtension ? lpwzExtension : L"");
	
	swprintf_s(lpwzTempFilePath, dwTempLen, L"%s%s%s", strAppDataDir.c_str(),
		strAppDataDir[strAppDataDir.length()-1]==L'\\' ? L"" : L"\\",  wzTmpName);
	
	return TRUE;
}

DWORD AddUsersAllControl(LPTSTR pszObjName)
{
    DWORD dwRes = 0;
    PACL pOldDACL = NULL, pNewDACL = NULL;
    PSECURITY_DESCRIPTOR pSD = NULL;
    EXPLICIT_ACCESS ea;
    PSID pSid = NULL;
    if (NULL == pszObjName)
        return ERROR_INVALID_PARAMETER;

    // Get a pointer to the existing DACL.
    dwRes = GetNamedSecurityInfo(pszObjName, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pOldDACL, NULL, &pSD);
    if (ERROR_SUCCESS == dwRes) 
    {
        // Get "Users" SID.
        SID_IDENTIFIER_AUTHORITY authNt = SECURITY_NT_AUTHORITY;
        AllocateAndInitializeSid(&authNt, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0, &pSid);

        // Initialize an EXPLICIT_ACCESS structure for the new ACE. 
        ZeroMemory(&ea, sizeof(EXPLICIT_ACCESS));
        ea.grfAccessMode = GRANT_ACCESS;
        ea.grfAccessPermissions = GENERIC_ALL;
        ea.grfInheritance = CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE;
        ea.Trustee.TrusteeType = TRUSTEE_IS_GROUP;
        ea.Trustee.TrusteeForm = TRUSTEE_IS_SID;
        ea.Trustee.ptstrName = (LPTSTR)pSid;

        // Create a new ACL that merges the new ACE into the existing DACL.
        dwRes = SetEntriesInAcl(1, &ea, pOldDACL, &pNewDACL);
        if (ERROR_SUCCESS == dwRes)
        {
            // Attach the new ACL as the object's DACL.
            dwRes = SetNamedSecurityInfo(pszObjName, SE_FILE_OBJECT,
                DACL_SECURITY_INFORMATION,
                NULL, NULL, pNewDACL, NULL);
        }
    }

    if (pSD != NULL)
        LocalFree((HLOCAL)pSD);
    if (pNewDACL != NULL)
        LocalFree((HLOCAL)pNewDACL);

    return dwRes;
}

std::wstring CTagProtector::GetAppDataDirectory()
{
	WCHAR wzPath[MAX_PATH + 1] = { 0 };

	BOOL bRet = SHGetSpecialFolderPath(NULL, wzPath, CSIDL_COMMON_APPDATA, FALSE);
	if (!bRet)
	{
		return L"";
	}

	PathAppend(wzPath, PRODUCT_NAME);
	DWORD dwRet = GetFileAttributesW(wzPath);
	if (dwRet == INVALID_FILE_ATTRIBUTES)
	{
		CreateDirectoryW(wzPath, NULL);
	}
    if(!bUsersPermissonSet)
    {
        AddUsersAllControl(wzPath);
        bUsersPermissonSet = TRUE;
    }
	return wzPath;
}

BOOL CTagProtector::StrEndWith(const WCHAR* wstrSrc, const WCHAR* wstrFind)
{
	if (wstrSrc == NULL || wstrFind == NULL)
	{
		return FALSE;
	}

	if (wcslen(wstrFind) == 0)
	{
		return FALSE;
	}

	//
	const WCHAR* pStrFind = StrStrW(wstrSrc, wstrFind);

	if ((pStrFind != NULL) &&
		(wcslen(pStrFind) == wcslen(wstrFind)))
	{
		return TRUE;
	}

	return FALSE;
}

void CTagProtector::OutputErrorInfo(const WCHAR* wszErrorInfo, ILockBytes*pilbOutput)
{
	if (NULL != wszErrorInfo)
	{
		ULARGE_INTEGER ulOffset = { 0 };
		ulOffset.QuadPart = 0;
		ULONG cbWritten = 0;
		pilbOutput->WriteAt(ulOffset, wszErrorInfo, (ULONG)(wcslen(wszErrorInfo) * sizeof(wszErrorInfo[0])), &cbWritten);
	}	
}