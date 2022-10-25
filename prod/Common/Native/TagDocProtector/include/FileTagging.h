#ifndef __FILE_TAGGING_H
#define __FILE_TAGGING_H
#include <string>
#include <list>
#include <map>
using namespace std;
#include "resattrmgr.h"

class CFileTagging
{
public:
	//CFileTagging();
	CFileTagging(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser, int nMillisecond, int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear,BOOL bLastAttribute);

	CFileTagging(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser);

	~CFileTagging();

public:
	LPWSTR GetFullURL(void) { return (LPWSTR)m_strFullURL.c_str(); }
	LPWSTR GetRemoteUser(void) { return (LPWSTR)m_strRemoteUser.c_str(); }
	int GetMillisecond(void) { return m_nMillisecond; }
	int GetYear(void) { return m_nYear; }
	int GetMonth(void) { return m_nMonth; }
	int GetDay(void) { return m_nDay; }
	int GetHour(void) { return m_nHour; }
	int GetMinute(void) { return m_nMinute; }
	int GetSecond(void) { return m_nSecond; }

	int AddAttributes(LPCWSTR lpwzName, LPCWSTR lpwzValue);
	int AddTags(LPCWSTR lpwzFileName);
	int ClearAllTags(LPCWSTR lpwzFileName);
	bool IsCE_Tags(WCHAR * string);
	bool IsCEDefault_Tags(WCHAR * string);

	BOOL GetTagsCount(LPCWSTR lpwzFileName, LONG* lCount);
	BOOL GetTag(LPCWSTR lpwzFileName, int lInd, LPWSTR tagName, LPWSTR tagValue);
	BOOL CheckInitStatus();

private:
	void DestoryNLRms();
	BOOL TagOnNormalFile(LPCWSTR lpwzFileName);
	bool Is64WOW();
	bool InitResattr();
    void SetTagToVecTags(wstring wstrName, wstring wstrValue);

private:
	std::wstring m_strFullURL;
	std::wstring m_strRemoteUser;
	int m_nMillisecond;
	int m_nSecond;
	int m_nMinute;
	int m_nHour;
	int m_nDay;
	int m_nMonth;
	int m_nYear;

	BOOL m_bInit; 
	ResourceAttributeManager *m_pMgr;
	ResourceAttributes *m_pAttributes;
	ResourceAttributes *m_pOrgAttrs;

	map<wstring, wstring> m_mapTags;
} ;

class CFileTaggingList
{
public:
	CFileTaggingList();
	~CFileTaggingList();

public:
	void AddFileTaggingItem(CFileTagging *pFileTagging);
	CFileTagging *GetFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser, int nID);
	CFileTagging *GetFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser);
	void RemoveFileTaggingItem(LPWSTR lpszFullURL, LPWSTR lpszRemoteUser,CFileTagging * _inputFileTagging);
	void CFileTaggingList::RemoveTimeOutTaggingItem(int nMillisecond,int nSecond,
							int nMinute,int nHour,
							int nDay,int nMonth,int nYear);

private:
	std::list<CFileTagging *> m_listFileTagging;
	CRITICAL_SECTION m_csMutex;
} ;

#endif // __FILE_TAGGING_H