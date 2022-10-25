
#include "stdafx.h"
#include "log.h"
#include "FileTagging.h"
#include "TagProtector.h"
#include <string>
#include "comutil.h"
#include <algorithm>
#include <cctype>

typedef int (*CreateAttributeManagerType)(ResourceAttributeManager **mgr);
typedef int (*AllocAttributesType)(ResourceAttributes **attrs);
typedef int (*ReadResourceAttributesWType)(ResourceAttributeManager *mgr, const WCHAR *filename, ResourceAttributes *attrs);
typedef int (*GetAttributeCountType)(const ResourceAttributes *attrs);
typedef void (*FreeAttributesType)(ResourceAttributes *attrs);
typedef void (*CloseAttributeManagerType)(ResourceAttributeManager *mgr);
typedef void (*AddAttributeWType)(ResourceAttributes *attrs, const WCHAR *name, const WCHAR *value);
typedef void (*AddAttributeAType)(ResourceAttributes *attrs, const CHAR *name, const CHAR *value);
typedef const WCHAR *(*GetAttributeNameType)(const ResourceAttributes *attrs, int index);
typedef const WCHAR * (*GetAttributeValueType)(const ResourceAttributes *attrs, int index);
typedef int (*WriteResourceAttributesWType)(ResourceAttributeManager *mgr, const WCHAR *filename, ResourceAttributes *attrs);
typedef int (*WriteResourceAttributesAType)(ResourceAttributeManager *mgr, const CHAR *filename, ResourceAttributes *attrs);
typedef int (*RemoveResourceAttributesWType)(ResourceAttributeManager *mgr, const WCHAR *filename, ResourceAttributes *attrs);


static CreateAttributeManagerType lfCreateAttributeManager = NULL;
static AllocAttributesType lfAllocAttributes = NULL;
static ReadResourceAttributesWType lfReadResourceAttributesW = NULL;
static GetAttributeCountType lfGetAttributeCount = NULL;
static FreeAttributesType lfFreeAttributes = NULL;
static CloseAttributeManagerType lfCloseAttributeManager = NULL;
static AddAttributeWType lfAddAttributeW = NULL;
static AddAttributeAType lfAddAttributeA = NULL;
static GetAttributeNameType lfGetAttributeName = NULL;
static GetAttributeValueType lfGetAttributeValue = NULL;
static WriteResourceAttributesWType lfWriteResourceAttributesW = NULL;
static WriteResourceAttributesAType lfWriteResourceAttributesA = NULL;
static RemoveResourceAttributesWType lfRemoveResourceAttributesW = NULL;

const WCHAR kSeperator1 = 0x01;
const WCHAR kSeperator2 = 0x02;

CFileTagging::CFileTagging(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser, int nMillisecond, int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear,BOOL bLastAttribute)
{
	if (lpszFullURL)
		m_strFullURL = lpszFullURL;
	if (lpszRemoteUser)
		m_strRemoteUser = lpszRemoteUser;

	m_nMillisecond = nMillisecond;
	m_nSecond = nSecond;
	m_nMinute = nMinute;
	m_nHour = nHour;
	m_nDay = nDay;
	m_nMonth = nMonth;
	m_nYear = nYear;
	m_pMgr = NULL;
	m_pAttributes = NULL;
	m_pOrgAttrs = NULL;
	m_bInit = InitResattr();
    if(m_bInit)
    {
	    lfCreateAttributeManager(&m_pMgr);
    }
}

CFileTagging::CFileTagging(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser)
{
	if (lpszFullURL)
		m_strFullURL = lpszFullURL;
	if (lpszRemoteUser)
		m_strRemoteUser = lpszRemoteUser;

	m_nMillisecond = 0;
	m_nSecond = 0;
	m_nMinute = 0;
	m_nHour = 0;
	m_nDay = 0;
	m_nMonth = 0;
	m_nYear = 0;
	m_pMgr = NULL;
	m_pAttributes = NULL;
	m_pOrgAttrs = NULL;
	m_bInit = InitResattr();
    if(m_bInit)
    {
	    lfCreateAttributeManager(&m_pMgr);
    }
}

CFileTagging::~CFileTagging()
{
	if (m_pAttributes)
	{
		lfFreeAttributes(m_pAttributes);
		m_pAttributes = NULL;
	}	
	if (m_pMgr)
	{
		lfCloseAttributeManager(m_pMgr);
		m_pMgr = NULL;
	}
	if (m_pOrgAttrs)
	{
		lfFreeAttributes(m_pOrgAttrs);
		m_pOrgAttrs = NULL;
	}	
}


bool CFileTagging::Is64WOW()
{
	if(8 == sizeof(char *))
		return true;
	else
		return false;
}

BOOL CFileTagging::CheckInitStatus()
{
	return m_bInit;
}

bool CFileTagging::InitResattr()
{
	static bool bInit = false;
	if(!bInit)
	{
		DPW((L"InitResattr \n"));	
		HKEY   hKEY;
		char   *data_Set= "Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\";
		::RegOpenKeyExA(HKEY_LOCAL_MACHINE,data_Set,   0,   KEY_READ,   &hKEY);
		BYTE  * owner_Get=new   BYTE[300];
		DWORD   type_1=REG_SZ;  
		DWORD   cbData_1=300;
		long   ret1=::RegQueryValueExA(hKEY,   "InstallDir",   NULL,&type_1,   owner_Get,   &cbData_1);     
		if(ret1!=ERROR_SUCCESS)
			return false;
		{
			char path[300];
			memcpy(path,owner_Get,300);			
			std::string strCommonPath = path;
			std::string strLib;
			std::string strMgr;
			if(Is64WOW())
			{
				strLib = strCommonPath + "Common\\bin64\\resattrlib.dll";
				strMgr = strCommonPath + "Common\\bin64\\resattrmgr.dll";
			}
			else
			{
				strLib = strCommonPath + "Common\\bin32\\resattrlib32.dll";
				strMgr = strCommonPath + "Common\\bin32\\resattrmgr32.dll";
			}

			DPA(("CFileTagging::strLib:"));
			DPA((strLib.c_str()));
			DPA(("CFileTagging::strMgr:"));
			DPA((strMgr.c_str()));

			HMODULE hModLib = (HMODULE)LoadLibraryA(strLib.c_str());
			HMODULE hModMgr = (HMODULE)LoadLibraryA(strMgr.c_str());

			if( !hModLib || !hModMgr)
			{
				DPA(("Load Failed"));
				return false;
			}

			lfCreateAttributeManager = (CreateAttributeManagerType)GetProcAddress(hModMgr, "CreateAttributeManager");
			lfAllocAttributes = (AllocAttributesType)GetProcAddress(hModLib, "AllocAttributes");
			lfReadResourceAttributesW = (ReadResourceAttributesWType)GetProcAddress(hModMgr, "ReadResourceAttributesW");
			lfGetAttributeCount = (GetAttributeCountType)GetProcAddress(hModLib, "GetAttributeCount");
			lfFreeAttributes = (FreeAttributesType)GetProcAddress(hModLib, "FreeAttributes");
			lfCloseAttributeManager = (CloseAttributeManagerType)GetProcAddress(hModMgr, "CloseAttributeManager");
			lfAddAttributeW = (AddAttributeWType)GetProcAddress(hModLib, "AddAttributeW");
			lfAddAttributeA = (AddAttributeAType)GetProcAddress(hModLib, "AddAttributeA");
			lfGetAttributeName = (GetAttributeNameType)GetProcAddress(hModLib, "GetAttributeName");
			lfGetAttributeValue = (GetAttributeValueType)GetProcAddress(hModLib, "GetAttributeValue");
			lfWriteResourceAttributesW = (WriteResourceAttributesWType)GetProcAddress(hModMgr, "WriteResourceAttributesW");
			lfWriteResourceAttributesA = (WriteResourceAttributesAType)GetProcAddress(hModMgr, "WriteResourceAttributesA");
			lfRemoveResourceAttributesW = (RemoveResourceAttributesWType)GetProcAddress(hModMgr, "RemoveResourceAttributesW");

			if( !(lfCreateAttributeManager && lfAllocAttributes &&
				lfReadResourceAttributesW && lfGetAttributeCount &&
				lfFreeAttributes && lfCloseAttributeManager && lfAddAttributeW &&
				lfGetAttributeName && lfGetAttributeValue &&
				lfWriteResourceAttributesW) )
			{
				DPA(("failed to get resattrlib/resattrmgr functions\r\n"));
				return false;
			}
			bInit = true;
		}	
	}

	return bInit;
}

int CFileTagging::AddAttributes(LPCWSTR lpwzName, LPCWSTR lpwzValue)
{
	int iRet = 0;
    wstring wstrName = lpwzName;
    wstring wstrValue = lpwzValue;
    transform(wstrName.begin(), wstrName.end(), wstrName.begin(), tolower); // Tag key "to-lower".
    m_mapTags[wstrName] = wstrValue;
	return iRet;
}

BOOL CFileTagging::TagOnNormalFile(LPCWSTR lpwzFileName) // overwrite the tag with same key.(case-insensitively)
{
    BOOL bRet = FALSE;
    if (m_pMgr != NULL)
	{
        wstring wstrName = L"";
	    wstring wstrValue = L"";
        ResourceAttributes *pAttrs = NULL;
        lfAllocAttributes(&pAttrs);
        if(pAttrs != NULL)
        {
            lfReadResourceAttributesW(m_pMgr, lpwzFileName, pAttrs); // Read old tags in file.
            int nCount = lfGetAttributeCount(pAttrs);
            for(int i = 0; i < nCount; ++i)
            {
                wstrName = (WCHAR*)lfGetAttributeName(pAttrs, i);
                transform(wstrName.begin(), wstrName.end(), wstrName.begin(), tolower); // Tag key "to-lower".
                map<wstring, wstring>::iterator mapItar = m_mapTags.find(wstrName);
                if(m_mapTags.end() == mapItar) // Don't find
                {
		            wstrValue = (WCHAR*)lfGetAttributeValue(pAttrs, i);
                    m_mapTags.insert(pair<wstring, wstring>(wstrName, wstrValue));
                }
            }

            lfFreeAttributes(pAttrs);
            lfAllocAttributes(&pAttrs); // re-alloc to write tags.
            map<wstring, wstring>::iterator mapItar = m_mapTags.begin();
            for(; mapItar != m_mapTags.end(); ++mapItar)
	        {
		        wstrName = mapItar->first;
		        wstrValue = mapItar->second;
		        lfAddAttributeW(pAttrs, wstrName.c_str(), wstrValue.c_str());
	        }
	        ClearAllTags(lpwzFileName);
	        bRet = lfWriteResourceAttributesW(m_pMgr, lpwzFileName, pAttrs); // Write new tags to file.        
            lfFreeAttributes(pAttrs);
        }
	    m_mapTags.clear();
    }
    return bRet;
}

bool CFileTagging::IsCE_Tags(WCHAR * string)
{	
	const WCHAR * wcCE_SPPrefix = L"CE_DocTagPrefix";
	const WCHAR * wcCE_SPVersion = L"CE_DocTagVersion";
	int ret = _wcsicmp(string,wcCE_SPPrefix);
	if(!ret)
		return true;
	ret = _wcsicmp(string,wcCE_SPVersion);
	if(!ret)
		return true;
	return false;
}

bool CFileTagging::IsCEDefault_Tags(WCHAR * string)
{
	const WCHAR * wcCE_SP = L"CE SP";	
	for(int i = 0;i < 5;i ++)
	{
		if(string[i] != wcCE_SP[i] || wcCE_SP[i] == 0)
			return false;
	}
	return true;
}


int CFileTagging::ClearAllTags(LPCWSTR lpwzFileName)
{
    int iRet = 0;
    ResourceAttributes* pAttrs = NULL;
    lfAllocAttributes(&pAttrs);
    if(pAttrs != NULL && m_pMgr != NULL)
    {
        lfReadResourceAttributesW(m_pMgr, lpwzFileName, pAttrs);
        iRet = lfRemoveResourceAttributesW(m_pMgr, lpwzFileName, pAttrs);
    }
    if(pAttrs != NULL)
    {
        lfFreeAttributes(pAttrs);
    }
	return iRet;
}

int CFileTagging::AddTags(LPCWSTR lpwzFileName)
{
	int iRet = 0;
	if (lpwzFileName == NULL || wcslen(lpwzFileName) < 4 || m_mapTags.size() == 0)	
	return -1;

	iRet = TagOnNormalFile(lpwzFileName);

	DPW((L"TagProtector: try to tag the file of [%s], result is [%d].\n", lpwzFileName, iRet));

	return iRet;
}

BOOL CFileTagging::GetTagsCount(LPCWSTR lpwzFileName, LONG* lCount)
{
	BOOL bRet = TRUE; 
	BSTR bstrInputFile = SysAllocString(lpwzFileName);
	 
	if(m_pOrgAttrs == NULL)
	{
		lfAllocAttributes(&m_pOrgAttrs);
	}
	if(m_pOrgAttrs != NULL && m_pMgr != NULL)
	{
		lfReadResourceAttributesW(m_pMgr, lpwzFileName, m_pOrgAttrs);
		int attrcount = lfGetAttributeCount(m_pOrgAttrs);
		*lCount = attrcount;
	}

    SysFreeString(bstrInputFile);
	return bRet;
}

BOOL CFileTagging::GetTag(LPCWSTR lpwzFileName, int lInd, LPWSTR tagName, LPWSTR tagValue)
{
	BOOL bRet = TRUE; 
	BSTR bstrInputFile = SysAllocString(lpwzFileName);
	WCHAR* name = NULL;
	WCHAR* value = NULL;

	if(m_pOrgAttrs == NULL)
	{
		LONG lCount = 0;
		bRet = GetTagsCount(lpwzFileName, &lCount);
	}
	name = (WCHAR*)lfGetAttributeName(m_pOrgAttrs, lInd);
	value = (WCHAR*)lfGetAttributeValue(m_pOrgAttrs, lInd);

	if(bRet && name != NULL && value != NULL)
	{
		size_t nameLen = wcslen(name) + 1;
		size_t valueLen = wcslen(value) + 1;
		memcpy(tagName, name, nameLen * sizeof(WCHAR));
		memcpy(tagValue, value, valueLen * sizeof(WCHAR));
	}

    SysFreeString(bstrInputFile);
	return bRet;
}

CFileTaggingList::CFileTaggingList()
{
	InitializeCriticalSection(&m_csMutex);
}

CFileTaggingList::~CFileTaggingList()
{
	std::list<CFileTagging *>::iterator iter;

	EnterCriticalSection(&m_csMutex);

	for (iter = m_listFileTagging.begin(); iter != m_listFileTagging.end(); iter++)
	{
		if ((*iter) != NULL)
		{
			delete (*iter);
			(*iter) = NULL;
		}
	}

	m_listFileTagging.clear();

	LeaveCriticalSection(&m_csMutex);

	DeleteCriticalSection(&m_csMutex);
}

void CFileTaggingList::AddFileTaggingItem(CFileTagging *pFileTagging)
{
	EnterCriticalSection(&m_csMutex);
	DPW((L"GetFileTaggingItem: size =%d \n", m_listFileTagging.size()));
	m_listFileTagging.push_front(pFileTagging);
	DPW((L"GetFileTaggingItem: size =%d \n", m_listFileTagging.size()));
	LeaveCriticalSection(&m_csMutex);
}

CFileTagging *CFileTaggingList::GetFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser, int nMillisecond)
{
	std::list<CFileTagging *>::iterator iter;
	CFileTagging *RepFileTagging = NULL;
	CFileTagging *pFileTagging = NULL;

	if (!lpszFullURL || !lpszRemoteUser)
		return NULL;

	EnterCriticalSection(&m_csMutex);

	for (iter = m_listFileTagging.begin(); iter != m_listFileTagging.end(); iter++)
	{
		pFileTagging = (CFileTagging *)(*iter);
		if (!_wcsicmp(pFileTagging->GetFullURL(), lpszFullURL) 
			&& !_wcsicmp(pFileTagging->GetRemoteUser(), lpszRemoteUser)
			&& pFileTagging->GetMillisecond() == nMillisecond)
		{
			DPW((L"lpszFullURL=%s \n",lpszFullURL));
			DPW((L"lpszRemoteUser=%s \n",lpszRemoteUser));
			DPW((L"pFileTagging->GetFullURL() :%s \n",pFileTagging->GetFullURL()));
			DPW((L"pFileTagging->GetRemoteUser() :%s \n",pFileTagging->GetRemoteUser()));
			RepFileTagging = pFileTagging;
			break;
		}
	}

	LeaveCriticalSection(&m_csMutex);

	return RepFileTagging;
}

CFileTagging *CFileTaggingList::GetFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser)
{
	std::list<CFileTagging *>::iterator iter;
	CFileTagging *pFileTagging = NULL;
	CFileTagging *RepFileTagging = NULL;
	if (!lpszFullURL || !lpszRemoteUser)
		return NULL;

	EnterCriticalSection(&m_csMutex);
	DPW((L"GetFileTaggingItem: size =%d \n", m_listFileTagging.size()));
	for (iter = m_listFileTagging.begin(); iter != m_listFileTagging.end(); iter++)
	{
		pFileTagging = (CFileTagging *)(*iter);
		DPW((lpszFullURL));
		DPW((lpszRemoteUser));
		DPW((pFileTagging->GetFullURL()));
		DPW((pFileTagging->GetRemoteUser()));
		if (!_wcsicmp(pFileTagging->GetFullURL(), lpszFullURL) 
			&& !_wcsicmp(pFileTagging->GetRemoteUser(), lpszRemoteUser))
		{
			DPW((L"Got pFileTagging! \n"));
			RepFileTagging = pFileTagging;
			break;
		}
	}

	LeaveCriticalSection(&m_csMutex);

	return RepFileTagging;
}
void CFileTaggingList::RemoveTimeOutTaggingItem(int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear)
{
	nSecond;nMillisecond;
	std::list<CFileTagging *>::iterator iter;
	CFileTagging *pFileTagging = NULL;
	DPW((L"CFileTaggingList: RemoveTimeOutTaggingItem \n"));
	EnterCriticalSection(&m_csMutex);
	for (iter = m_listFileTagging.begin(); iter != m_listFileTagging.end(); )
	{
		pFileTagging = (CFileTagging *)(*iter);
		if(nMinute != pFileTagging->GetMinute() || nHour != pFileTagging->GetHour()
			|| nDay != pFileTagging->GetDay() || nMonth != pFileTagging->GetMonth()
			|| nYear != pFileTagging->GetYear())
		{
			DPW((L"CFileTaggingList:delete pFileTagging \n"));
			delete pFileTagging; pFileTagging = NULL;
			iter = m_listFileTagging.erase(iter);
		}
		else
		{
			++iter;
		}
	}
	LeaveCriticalSection(&m_csMutex);
}

void CFileTaggingList::RemoveFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser,CFileTagging * _inputFileTagging)
{
	std::list<CFileTagging *>::iterator iter;
	CFileTagging *pFileTagging = NULL;
	DPW((L"CFileTaggingList: RemoveFileTaggingItem \n"));
	DPW((L"CFileTaggingList: RemoveFileTaggingItem lpszFullURL=%s \n",lpszFullURL));
	DPW((L"CFileTaggingList: RemoveFileTaggingItem lpszRemoteUser=%s \n",lpszRemoteUser));
	if (!lpszFullURL || !lpszRemoteUser)
		return;
	EnterCriticalSection(&m_csMutex);
	for (iter = m_listFileTagging.begin(); iter != m_listFileTagging.end(); iter++)
	{
		pFileTagging = (CFileTagging *)(*iter);
		DPW((L"CFileTaggingList: pFileTagging->GetFullURL() :%s \n",pFileTagging->GetFullURL()));
		DPW((L"CFileTaggingList: pFileTagging->GetRemoteUser() :%s \n",pFileTagging->GetRemoteUser()));
		DPW((L"CFileTaggingList: pFileTagging: %d \n",pFileTagging));
		DPW((L"CFileTaggingList: _inputFileTagging: %d \n",_inputFileTagging));
		if (!_wcsicmp(pFileTagging->GetFullURL(), lpszFullURL) 
			&& !_wcsicmp(pFileTagging->GetRemoteUser(), lpszRemoteUser))
		{
			
			DPW((L"CFileTaggingList:cmp Succeed \n"));
			if(pFileTagging == _inputFileTagging)
			{
				DPW((L"CFileTaggingList:pFileTagging == _inputFileTagging \n"));
				break;
			}
		}
	}
	if (iter != m_listFileTagging.end())
	{
		DPW((L"CFileTaggingList:delete pFileTagging \n"));
		delete pFileTagging; pFileTagging = NULL;
		m_listFileTagging.erase(iter);
	}
	{
		DPW((L"CFileTaggingList: size %d \n",m_listFileTagging.size()));
	}
	LeaveCriticalSection(&m_csMutex);
}