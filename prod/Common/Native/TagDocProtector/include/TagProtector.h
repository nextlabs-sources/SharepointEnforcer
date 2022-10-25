// TagProtector.h : Declaration of the CTagProtector
/****************************************************************************************************************/
/* Protects/Unprotects .doc files 
When protecting, creates a storage structure with the following streams:
Key ( "I am protected"): Indicates that given storage was protected this protector
SignedIL ("SignedIL") This stream contains the signed IL to be used during decryption
ServerID ("ServerID"): Contains the user information 
RightsTemplate("RightsTemplate"):
ListGuid("ListGuid")
Content("Content"): Stream contains encrypted content

and a storage:
Licenses("Licenses"): This storage consists of the licenses (EULs) for this user
/****************************************************************************************************************/

#pragma once
#include "TagDocProtector.h"
#include "FileTagging.h"
#include "resource.h"       // main symbols
#include <comsvcs.h>
#include "stdafx.h"
#include <atlbase.h>
#include <atlcom.h>
#include "Globals.h"
#include "strsafe.h"
#define KM_HASH_LEN 32
#define KM_KEY_LEN 32
#define KM_MAX_KEYSTORE_NAME 16

#define DEFAULT_KM_TIMEOUT (5*1000)


typedef enum _CEResult_t {
	CE_RESULT_SUCCESS = 0,  /**< Success */
	CE_RESULT_GENERAL_FAILED = -1,  /**< General failure */
	CE_RESULT_CONN_FAILED = -2,  /**< Connection failed */
	CE_RESULT_INVALID_PARAMS = -3,  /**< Invalid parameter(s) */
	CE_RESULT_VERSION_MISMATCH = -4,  /**< Version mismatch */
	CE_RESULT_FILE_NOT_PROTECTED = -5,  /**< File not protected */
	CE_RESULT_INVALID_PROCESS = -6,  /**< Invalid process */
	CE_RESULT_INVALID_COMBINATION = -7,  /**< Invalid combination */
	CE_RESULT_PERMISSION_DENIED = -8,  /**< Permission denied */
	CE_RESULT_FILE_NOT_FOUND = -9,  /**< File not found */
	CE_RESULT_FUNCTION_NOT_AVAILBLE = -10, /**< Function not available */
	CE_RESULT_TIMEDOUT = -11, /**< Timed out */
	CE_RESULT_SHUTDOWN_FAILED = -12, /**< Shutdown failed */
	CE_RESULT_INVALID_ACTION_ENUM = -13, /**< */
	CE_RESULT_EMPTY_SOURCE = -14, /**< Empty source */
	CE_RESULT_MISSING_MODIFIED_DATE = -15, /**< */
	CE_RESULT_NULL_CEHANDLE = -16, /**< NULL or bad connection handle */
	CE_RESULT_INVALID_EVAL_ACTION = -17, /**< */
	CE_RESULT_EMPTY_SOURCE_ATTR = -18, /**< */
	CE_RESULT_EMPTY_ATTR_KEY = -19, /**< */
	CE_RESULT_EMPTY_ATTR_VALUE = -20, /**< */
	CE_RESULT_EMPTY_PORTAL_USER = -21, /**< */
	CE_RESULT_EMPTY_PORTAL_USERID = -22, /**< */
	CE_RESULT_MISSING_TARGET = -23, /**< Missing target */
	CE_RESULT_PROTECTION_OBJECT_NOT_FOUND = -24, /**< Object not found */
	CE_RESULT_NOT_SUPPORTED = -25, /**< Not supported */
	CE_RESULT_SERVICE_NOT_READY = -26, /**< Not ready */
	CE_RESULT_SERVICE_NOT_FOUND = -27, /**< Not foudn */
	CE_RESULT_INSUFFICIENT_BUFFER = -28, /**< I find your lack of space, disturbing */
	CE_RESULT_ALREADY_EXISTS = -29, /**< Tried to create somethat that already exists */
	CE_RESULT_APPLICATION_AUTH_FAILED = -30  /**< Consumer Authentication failed */
} CEResult_t;

// CTagProtector
class ATL_NO_VTABLE CTagProtector :public I_IrmProtector,
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CTagProtector, &CLSID_TagProtector>
	
{
public:
	CTagProtector();

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_TagPROTECTOR)

DECLARE_NOT_AGGREGATABLE(CTagProtector)

BEGIN_COM_MAP(CTagProtector)
	COM_INTERFACE_ENTRY(I_IrmProtector)
	//COM_INTERFACE_ENTRY(IDispatch)
END_COM_MAP()




// ITagProtector
public:	    
    ~CTagProtector();

public:
    // I_IrmProtector
    __override STDMETHOD(HrInit)           (BSTR *pbstrProduct, DWORD *pdwVersion, BSTR *pbstrExtensions, BOOL *pfUseRMS);
    __override STDMETHOD(HrIsProtected)    (ILockBytes *pilbInput, DWORD *pdwResult);
    __override STDMETHOD(HrSetLangId)      (LANGID langid);
    __override STDMETHOD(HrProtectRMS)     (ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfoRMS *piid, DWORD *pdwStatus);
    __override STDMETHOD(HrUnprotectRMS)   (ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfoRMS *piid, DWORD *pdwStatus);
    __override STDMETHOD(HrProtect)        (ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfo *piid, DWORD *pdwStatus);
    __override STDMETHOD(HrUnprotect)      (ILockBytes *pilbInput, ILockBytes *pilbOutput, I_IrmPolicyInfo *piid, DWORD *pdwStatus);

protected:
     //CBaseProtected required stuff
    __override WCHAR *WzRegKey();
	HRESULT CopyLockBytes(ILockBytes*pilbInput,ILockBytes*pilbOutput);
	BOOL GetTempFilePath(LPWSTR lpwzFileName, LPWSTR lpwzTempFilePath, DWORD dwTempLen);
	HRESULT CTagProtector::CreateTempFile(WCHAR * _TmpFilePath,ILockBytes *pilbInput);
	HRESULT CTagProtector::ExtractFromTempFile(WCHAR * _TmpFilePath,ILockBytes *pilbOutput);

	BOOL StrEndWith(const WCHAR* wstrSrc, const WCHAR* wstrFind);
	std::wstring GetAppDataDirectory();
	void OutputErrorInfo(const WCHAR* wszErrorInfo, ILockBytes*pilbOutput);
protected:
    unsigned int m_uRefCount;
    LANGID       m_langid;
};

OBJECT_ENTRY_AUTO(__uuidof(TagProtector), CTagProtector)


class RevertThreadToken
{
public:
	RevertThreadToken(){
		m_hThreadToken = NULL;

		HANDLE hThreadToken = NULL;
		bool bOpenToken = OpenThreadToken(GetCurrentThread(), TOKEN_READ | TOKEN_IMPERSONATE , true, &hThreadToken);
		if (hThreadToken)
		{
			m_hThreadToken = hThreadToken;
			RevertToSelf();
		}
		
	}

	~RevertThreadToken(){
		if (m_hThreadToken)
		{
			SetThreadToken(NULL, m_hThreadToken);
		}
	}
private:
    HANDLE m_hThreadToken;
};