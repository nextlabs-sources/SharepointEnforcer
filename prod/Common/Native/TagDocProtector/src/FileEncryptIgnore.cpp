#include<stdafx.h>
#include<FileEncryptIgnore.h>
#include "log.h"

CFileEncryptIgnore::CFileEncryptIgnore()
{
	InitializeCriticalSection(&m_csListIgnore);
}

CFileEncryptIgnore::~CFileEncryptIgnore()
{
	LockIgnoreList();

	std::list<EncryptFileInfo*>::iterator itFileInfo = m_listFileIgnore.begin();
	while (itFileInfo != m_listFileIgnore.end())
	{
		delete *itFileInfo;
		itFileInfo++;
	}
	m_listFileIgnore.clear();

	UnlokIgnoreList();

	DeleteCriticalSection(&m_csListIgnore);
}

size_t CFileEncryptIgnore::AddFileIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks)
{
	if (wzFullUrl == NULL || wzRemoteUser == NULL)
	{
		return 0;
	}
	else
	{
		DPW((L"TagProtector::AddFileIgnore,Url=%s, user=%s \n", wzFullUrl, wzRemoteUser));
		EncryptFileInfo* pNewFileInfo = new EncryptFileInfo();
		pNewFileInfo->strFullUrl = wzFullUrl;
		pNewFileInfo->strRemoteUser = wzRemoteUser;
		pNewFileInfo->dwTicks = dwTicks;

		size_t nListCount = 0;

		LockIgnoreList();

		m_listFileIgnore.push_back(pNewFileInfo);
		nListCount = m_listFileIgnore.size();

		UnlokIgnoreList();
		

		return nListCount;
	}
}

size_t CFileEncryptIgnore::RemoveFileIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks)
{
	if (wzFullUrl == NULL || wzRemoteUser == NULL)
	{
		return 0;
	}
	else
	{
		
	    EncryptFileInfo* pIgnoreFile = GetIgnoreFile(wzFullUrl, wzRemoteUser, dwTicks);
		if (pIgnoreFile)
		{
			return RemoveFileIgnore(pIgnoreFile);
		}
	}

	return 0;

}

size_t CFileEncryptIgnore::RemoveFileIgnore(EncryptFileInfo* pFileInfo)
{

	WCHAR wzLog[1000];
	wsprintf(wzLog, L"TagProtector::RemoveFileIgnore,Url=%s, user=%s", pFileInfo->strFullUrl.c_str(), pFileInfo->strRemoteUser.c_str());
	OutputDebugString(wzLog);

	size_t nCount = 0;
	LockIgnoreList();

	delete pFileInfo;
	m_listFileIgnore.remove(pFileInfo);
	nCount = m_listFileIgnore.size();

	UnlokIgnoreList();

	return nCount;
}

 EncryptFileInfo* CFileEncryptIgnore::GetIgnoreFile(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks)
{
	EncryptFileInfo* pEncryptFileInfo = NULL;

	LockIgnoreList();

	std::list<EncryptFileInfo*> lstDeleteItem;

	std::list<EncryptFileInfo*>::iterator itFileInfo = m_listFileIgnore.begin();
	while (itFileInfo != m_listFileIgnore.end())
	{
	    EncryptFileInfo* pFile = *itFileInfo;
 		if (_wcsicmp(pFile->strFullUrl.c_str(), wzFullUrl) == 0  &&
 			_wcsicmp(pFile->strRemoteUser.c_str(), wzRemoteUser) == 0 )
		{
			if (pFile->dwTicks + 1 * 60 * 1000 < dwTicks)
			{
                //timeout, delete it
				lstDeleteItem.push_back(pEncryptFileInfo);
			}
			else
			{
				pEncryptFileInfo = pFile;
				break;
			}
			
		}

		itFileInfo++;
	}

	//delete timeout items
	itFileInfo = lstDeleteItem.begin();
	while (itFileInfo != lstDeleteItem.end())
	{
		RemoveFileIgnore(*itFileInfo);
		itFileInfo++;
	}

	UnlokIgnoreList();

	return pEncryptFileInfo;
}

void CFileEncryptIgnore::LockIgnoreList()
{
	EnterCriticalSection(&m_csListIgnore);
}

void CFileEncryptIgnore::UnlokIgnoreList()
{
	LeaveCriticalSection(&m_csListIgnore);
}