// TagDocProtector.cpp : Implementation of DLL Exports.


#include "stdafx.h"
#include "resource.h"
#include "TagDocProtector.h"
#include "log.h"
#include "FileTagging.h"
#include "FileEncryptIgnore.h"
#include <atlbase.h>
#define TAGPROTECTOR_API extern "C" __declspec(dllexport)
//#define TAGPROTECTOR_EXPORTS
//#ifdef TAGPROTECTOR_EXPORTS
//#define TAGPROTECTOR_API __declspec(dllexport)
//#else
//#define TAGPROTECTOR_API __declspec(dllimport)
//#endif
extern CFileTaggingList g_listFileTagging;

TAGPROTECTOR_API void TagProtector_ClearTagParam(int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear);
TAGPROTECTOR_API void TagProtector_AddTagParam(BYTE *pURL, int nUrlLen, 
							BYTE *pRemoteUser, int nRemoteUserLen,
							BYTE *pKeyName, int nKeyNameLen,
							BYTE *pKeyValue, int nKeyValueLen,
							BOOL bLastAttribute,
							int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear);

TAGPROTECTOR_API int TagProtector_GetTagsCount(WCHAR* wzFullUrl, WCHAR* wzRemoteUser);

TAGPROTECTOR_API void TagProtector_GetTag(WCHAR* fullUrl, WCHAR* remoteUser, int iInd, LPWSTR tagName, LPWSTR tagValue);


class CTagDocProtectorModule : public CAtlDllModuleT< CTagDocProtectorModule >
{
public :
	DECLARE_LIBID(LIBID_TagDocProtectorLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_TagDOCPROTECTOR, "{74FF900D-E9D2-489E-AEDE-725BE5153C55}")
};

CTagDocProtectorModule _AtlModule;


#ifdef _MANAGED
#pragma managed(push, off)
#endif

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	hInstance;
    return _AtlModule.DllMain(dwReason, lpReserved); 
}

#ifdef _MANAGED
#pragma managed(pop)
#endif




// Used to determine whether the DLL can be unloaded by OLE
STDAPI DllCanUnloadNow(void)
{
    return _AtlModule.DllCanUnloadNow();
}


// Returns a class factory to create an object of the requested type
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
    return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}


// DllRegisterServer - Adds entries to the system registry
STDAPI DllRegisterServer(void)
{
    // registers object, typelib and all interfaces in typelib
    HRESULT hr = _AtlModule.DllRegisterServer();
	return hr;
}


// DllUnregisterServer - Removes entries from the system registry
STDAPI DllUnregisterServer(void)
{
	HRESULT hr = _AtlModule.DllUnregisterServer();
	return hr;
}

 bool GetFileTaggingFromList(WCHAR* fullUrl, WCHAR* remoteUser, CFileTagging **pOutFileTagging)
{
	CFileTagging *pFileTagging = g_listFileTagging.GetFileTaggingItem(fullUrl, remoteUser);
	if (pFileTagging == NULL)
	{
		pFileTagging = new CFileTagging(fullUrl, remoteUser);
		if(pFileTagging != NULL && pFileTagging->CheckInitStatus())
		{
			DPW((L"pFileTagging has been created. \n"));
			g_listFileTagging.AddFileTaggingItem(pFileTagging);
		}
	}
	if(pFileTagging != NULL && pFileTagging->CheckInitStatus())
	{
		*pOutFileTagging = pFileTagging;
		return true;
	}
	return false;
}

TAGPROTECTOR_API void TagProtector_ClearTagParam(int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear)
{
	g_listFileTagging.RemoveTimeOutTaggingItem(nMillisecond,nSecond,
							nMinute,nHour,
							nDay,nMonth,nYear);
}

TAGPROTECTOR_API void TagProtector_AddTagParam(BYTE *pURL, int nUrlLen, 
							BYTE *pRemoteUser, int nRemoteUserLen,
							BYTE *pKeyName, int nKeyNameLen,
							BYTE *pKeyValue, int nKeyValueLen,
							BOOL bLastAttribute,
							int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear)
{
	DPA(("TagProtector_AddTagParam"));
	//LPSTR lpszFullUrl = NULL;
	LPWSTR lpwzFullUrl = NULL;
	//LPSTR lpszRemoteUser = NULL;
	LPWSTR lpwzRemoteUser = NULL;
	LPWSTR lpwzKeyName = NULL;
	LPWSTR lpwzKeyValue = NULL;
	//lpszFullUrl = new char[nUrlLen+1];
	lpwzFullUrl = new WCHAR[nUrlLen+1];
	//memset(lpszFullUrl, 0, nUrlLen+1);
	memset(lpwzFullUrl, 0, (nUrlLen+1)*sizeof(WCHAR));
	memcpy(lpwzFullUrl, pURL, nUrlLen*2);
	//MultiByteToWideChar(CP_ACP, 0, lpszFullUrl, -1, lpwzFullUrl, nUrlLen+1);
	//lpszRemoteUser = new char[nRemoteUserLen+1];
	lpwzRemoteUser = new WCHAR[nRemoteUserLen+1];
	//memset(lpszRemoteUser, 0, nRemoteUserLen+1);
	memset(lpwzRemoteUser, 0, (nRemoteUserLen+1)*sizeof(WCHAR));
	memcpy(lpwzRemoteUser, pRemoteUser, nRemoteUserLen*2);
	//MultiByteToWideChar(CP_ACP, 0, lpszRemoteUser, -1, lpwzRemoteUser, nRemoteUserLen+1);
	lpwzKeyName = new WCHAR[nKeyNameLen+1];
	memset(lpwzKeyName, 0, (nKeyNameLen+1)*sizeof(WCHAR));
	memcpy(lpwzKeyName, pKeyName, nKeyNameLen*2);

	lpwzKeyValue = new WCHAR[nKeyValueLen+1];
	memset(lpwzKeyValue, 0, (nKeyValueLen+1)*sizeof(WCHAR));
	memcpy(lpwzKeyValue, pKeyValue, nKeyValueLen*2);

	DPW((L"TagProtector_AddTagParam: URL=%s RemoteUser=%s \n",lpwzFullUrl, lpwzRemoteUser));
	CFileTagging *pFileTagging = g_listFileTagging.GetFileTaggingItem(lpwzFullUrl, lpwzRemoteUser, nMillisecond);
	DPA(("TagProtector_AddTagParam %d %d %d %d %d %d %d",nMillisecond,nSecond,nMinute,nHour,nDay,nMonth,nYear));
	if (pFileTagging == NULL)
	{
		pFileTagging = new CFileTagging(lpwzFullUrl, lpwzRemoteUser, nMillisecond, nSecond,
							nMinute,nHour,
							nDay,nMonth,nYear,bLastAttribute);
		DPA(("pFileTagging has been created : %d \n",pFileTagging));
		if(pFileTagging != NULL && pFileTagging->CheckInitStatus())
		{
			g_listFileTagging.AddFileTaggingItem(pFileTagging);
		}
	}
	if(pFileTagging != NULL && pFileTagging->CheckInitStatus())
	{
		DPW((L"AddAttributes->lpszKeyName : %s \n",lpwzKeyName));
		DPW((L"AddAttributes->lpszKeyValue : %s \n",lpwzKeyValue));
		pFileTagging->AddAttributes(lpwzKeyName, lpwzKeyValue);
	}

	delete[] lpwzKeyValue;
	delete[] lpwzKeyName;
	delete[] lpwzRemoteUser;
	delete[] lpwzFullUrl;
}


TAGPROTECTOR_API int TagProtector_AddFileEncryptIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, int nTicks)
{	
	int nCount = (int)CFileEncryptIgnore::GetInstance()->AddFileIgnore(wzFullUrl, wzRemoteUser, nTicks);
	return nCount;
}

TAGPROTECTOR_API int TagProtector_RemoveFileEncryptIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, int nTicks)
{
	int nCount = (int)CFileEncryptIgnore::GetInstance()->RemoveFileIgnore(wzFullUrl, wzRemoteUser, nTicks);
	return nCount;
}

TAGPROTECTOR_API int TagProtector_GetTagsCount(WCHAR* fullUrl, WCHAR* remoteUser)
{
	DPW((L"TagProtector_GetTagsCount URL=%s RemoteUser=%s \n", fullUrl, remoteUser));
	LONG lCount = 0;
	CFileTagging *pFileTagging = NULL;
	if(GetFileTaggingFromList(fullUrl, remoteUser, &pFileTagging))
	{
		pFileTagging->GetTagsCount(fullUrl, &lCount);
	}
	return (int)lCount;
}
 
TAGPROTECTOR_API void TagProtector_GetTag(WCHAR* fullUrl, WCHAR* remoteUser, int iInd, LPWSTR tagName, LPWSTR tagValue)
{
	DPW((L"TagProtector_GetTag URL=%s RemoteUser=%s Index=%d \n", fullUrl, remoteUser, iInd));
	CFileTagging *pFileTagging = NULL;
	if(GetFileTaggingFromList(fullUrl, remoteUser, &pFileTagging))
	{
		pFileTagging->GetTag(fullUrl, iInd, tagName, tagValue);
	}
}