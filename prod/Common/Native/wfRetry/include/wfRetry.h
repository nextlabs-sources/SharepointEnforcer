// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the WFRETRY_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// WFRETRY_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef WFRETRY_EXPORTS
#define WFRETRY_API __declspec(dllexport)
#else
#define WFRETRY_API __declspec(dllimport)
#endif
#include <string>
#include <vector>
#include <windows.h>
#include <time.h>
typedef struct _FILEENTRY
{
	std::wstring	strFullName;
	//std::wstring	strRelativeName;//Relative to the source directory;
	BOOL			bEncSucceeded;	//TRUE means succeeded
	_FILEENTRY()
	{
		bEncSucceeded=FALSE;
	};
	//friend bool operator == (_FILEENTRY*lhs,const std::wstring& rhsRelName)
	//{
	//	return lhs->strRelativeName==rhsRelName;
	//};
}FILEENTRY,*LPFILEENTRY;
typedef std::vector<LPFILEENTRY> FILELIST;

extern "C" WFRETRY_API int PluginEntry( void** /*in_context*/ ) ;
extern "C" WFRETRY_API int PluginUnload( void* /*in_context*/ );
