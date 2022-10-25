#ifndef FILE_ENCRYPT_IGNORE_H
#define FILE_ENCRYPT_IGNORE_H
#include <list>

typedef struct _tagEncryptFileInfo 
{
	std::wstring strFullUrl;
	std::wstring strRemoteUser;
	DWORD        dwTicks;
}EncryptFileInfo, *PEncryptFileInfo;

class CFileEncryptIgnore
{
public:
	~CFileEncryptIgnore();

private:
	CFileEncryptIgnore();
	CFileEncryptIgnore(const CFileEncryptIgnore&){}

public:
	static CFileEncryptIgnore* GetInstance()
	{
		static CFileEncryptIgnore* pFileIgnore = NULL;
		if (NULL == pFileIgnore)
		{
			pFileIgnore = new CFileEncryptIgnore();
		}
		return pFileIgnore;
	}

public:
	size_t AddFileIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks);
	size_t RemoveFileIgnore(EncryptFileInfo* pFileInfo);
	size_t RemoveFileIgnore(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks);
	EncryptFileInfo* GetIgnoreFile(const WCHAR* wzFullUrl, const WCHAR* wzRemoteUser, DWORD dwTicks);
private:
	void LockIgnoreList();
	void UnlokIgnoreList();

private:
	std::list<EncryptFileInfo*>  m_listFileIgnore;
	CRITICAL_SECTION          m_csListIgnore;
};


#endif