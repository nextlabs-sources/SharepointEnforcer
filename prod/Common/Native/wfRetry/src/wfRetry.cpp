// wfRetry.cpp : Defines the entry point for the DLL application.
//
#include "stdafx.h"
#include "wfRetry.h"
#include <string>
#include <fstream>
#include <tlhelp32.h>
#ifdef _MANAGED
#pragma managed(push, off)
#endif
#define REG_KEY_COMPLIANT_ENTERPRISE    L"SOFTWARE\\NextLabs\\Enterprise DLP\\SharePoint Workflow Policy Assistant"
#define REG_KEY_INSTALL					L"InstallDir"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
    return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif


static HANDLE				   ghThreadRetry = NULL;
static HANDLE                  ghSvcStopEvent = NULL;
void FindFilesInDir(std::wstring &strRoot,FILELIST& vecFiles)
{
	std::wstring fname;
	std::wstring filePathName;
	std::wstring tmpPath;
	std::wstring localRoot=strRoot;
	FILEENTRY* pFileEntry=NULL;
	if( localRoot[localRoot.length() -1] != L'\\' )
	{
		localRoot+=L"\\";
	}
	filePathName=localRoot;
	filePathName+=L"*";

	WIN32_FIND_DATA fd;
	ZeroMemory(&fd, sizeof(WIN32_FIND_DATA));
	HANDLE hSearch;
	hSearch = FindFirstFile(filePathName.c_str(), &fd);
	//Is directory
	if(hSearch==INVALID_HANDLE_VALUE)
		return ;
	if( (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		&& wcscmp(fd.cFileName, L".") && wcscmp(fd.cFileName, L"..") ) 
	{
		tmpPath=localRoot;
		tmpPath+=fd.cFileName;
		//FindFilesInDir(tmpPath, vecFiles);
	}
	else if( wcscmp(fd.cFileName, L".") && wcscmp(fd.cFileName, L"..") )
	{
		pFileEntry=new FILEENTRY;
		fname=localRoot+fd.cFileName;
		pFileEntry->strFullName=fname;
		vecFiles.push_back(pFileEntry);
	}

	BOOL bSearchFinished = FALSE;
	while( !bSearchFinished )
	{
		if( FindNextFile(hSearch, &fd) )
		{
			if( (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				&& wcscmp(fd.cFileName, L".") && wcscmp(fd.cFileName, L"..") ) 
			{
				tmpPath=localRoot;
				tmpPath+=fd.cFileName;
				//FindFilesInDir(tmpPath, vecFiles);
			}
			else if( wcscmp(fd.cFileName, L".") && wcscmp(fd.cFileName, L"..") )
			{
				pFileEntry=new FILEENTRY;
				fname=localRoot+fd.cFileName;
				pFileEntry->strFullName=fname;
				vecFiles.push_back(pFileEntry);
			}
		}
		else
		{
			if( GetLastError() == ERROR_NO_MORE_FILES ) //Normal Finished
			{
				bSearchFinished = TRUE;
			}
			else
				bSearchFinished = TRUE; //Terminate Search
		}
	}
	FindClose(hSearch);
}
BOOL   GetTokenByName(HANDLE   &hToken,LPWSTR   lpName) 
{ 
	if(!lpName) 
	{ 
		return   FALSE; 
	} 
	HANDLE                   hProcessSnap   =   NULL;   
	BOOL                       bRet             =   FALSE;   
	PROCESSENTRY32   pe32             =   {0};   

	hProcessSnap   =   CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS,   0); 
	if   (hProcessSnap   ==   INVALID_HANDLE_VALUE)   
		return   (FALSE);   

	pe32.dwSize   =   sizeof(PROCESSENTRY32);   

	if   (Process32First(hProcessSnap,   &pe32))   
	{ 
		do   
		{ 
			if(!_wcsnicmp(pe32.szExeFile,lpName,wcslen(lpName))) 
			{ 
				HANDLE   hProcess   =   OpenProcess(PROCESS_QUERY_INFORMATION, 
					FALSE,pe32.th32ProcessID); 
				bRet   =   OpenProcessToken(hProcess,TOKEN_ALL_ACCESS,&hToken); 
				CloseHandle   (hProcessSnap);   
				return   (bRet); 
			}  
		}   
		while   (Process32Next(hProcessSnap,   &pe32));   
		bRet   =   TRUE;   
	}   
	else   
		bRet   =   FALSE; 

	CloseHandle   (hProcessSnap);   
	return   (bRet); 
} 
unsigned int __stdcall SvcInit(void*)
{
	OutputDebugString(L"wfRetry: The Workflow Retry module start to run!");
	LONG rstatus;
	HKEY hKey = NULL; 

	rstatus = RegOpenKeyExW(HKEY_LOCAL_MACHINE,
		REG_KEY_COMPLIANT_ENTERPRISE,
		0,KEY_QUERY_VALUE,&hKey);
	if( rstatus != ERROR_SUCCESS )
	{
		OutputDebugString(L"wfRetry: Failed to open register key Compliant Enterprise!");
		return 0;
	}
	WCHAR wzWorkflowAdapterInstallPath[MAX_PATH];                 /* InstallDir */
	DWORD dwSize = sizeof(wzWorkflowAdapterInstallPath);

	rstatus = RegQueryValueExW(hKey,REG_KEY_INSTALL,NULL,NULL,(LPBYTE)wzWorkflowAdapterInstallPath,&dwSize);
	RegCloseKey(hKey);
	if( rstatus != ERROR_SUCCESS )
	{
		OutputDebugString(L"wfRetry: Failed to query register key InstallPath!");
		return 0;
	}

	WCHAR wzRetryFolder[MAX_PATH];
	swprintf_s(wzRetryFolder, L"%s\\%s", wzWorkflowAdapterInstallPath, L"bin\\Retry");

	std::wstring wstrRetryFolderMsg=L"wfRetry: ";
	wstrRetryFolderMsg+=wzRetryFolder;
	OutputDebugString(wstrRetryFolderMsg.c_str());
	// Create an event. The control handler function, SvcCtrlHandler,
	// signals this event when it receives the stop control code.

	ghSvcStopEvent = CreateEvent(
		NULL,    // default security attributes
		TRUE,    // manual reset event
		FALSE,   // not signaled
		NULL);   // no name

	if ( ghSvcStopEvent == NULL)
	{
		OutputDebugString(L"wfRetry: Failed to CreateEvent SvcStopEvent!");
		return 0;
	}
	std::wstring wstrRetryFolder=wzRetryFolder;
	FILELIST vecFiles;
	for(;;)
	{
		DWORD dwEvent = WaitForSingleObject(ghSvcStopEvent, 2000);
		if(dwEvent == WAIT_TIMEOUT)
		{
			FILELIST::iterator itVecFile=vecFiles.begin();
			for(itVecFile;itVecFile!=vecFiles.end();itVecFile++)
			{
				delete (*itVecFile);
			}
			vecFiles.clear();
			FindFilesInDir(wstrRetryFolder,vecFiles);
			if(vecFiles.size())
			{
				FILELIST::iterator it=vecFiles.begin();
				for(;it!=vecFiles.end();it++)//Loop for every retry-file
				{
					std::wfstream file;
					file.open((*it)->strFullName.c_str(),std::ios_base::in);
					if(file.is_open())
					{
						std::wstring wstrLine;
						std::getline(file,wstrLine);
						std::wstring::size_type pos=wstrLine.find(L' ');
						if(pos!=std::wstring::npos)
						{
							time_t lCurrTime=0;
							time(&lCurrTime);
							std::wstring wstrTime=wstrLine.substr(0,pos);
							std::wstring wstrCmdLine=wstrLine.substr(pos+1);
							long lTimeReservered=_wtol(wstrTime.c_str());
							if(lTimeReservered<lCurrTime)
							{
								OutputDebugString(L"wfRetry: Start a retry session...");
								STARTUPINFO startupInfo;
								ZeroMemory(&startupInfo,sizeof(startupInfo));
								startupInfo.cb	=sizeof(STARTUPINFO);
								startupInfo.wShowWindow =SW_HIDE;
								startupInfo.lpDesktop=L"winsta0\\default";
								HANDLE   hToken=NULL; 
								if(!GetTokenByName(hToken, L"EXPLORER.EXE")) 
								{ 
									continue; 
								} 

								PROCESS_INFORMATION processInfo;
								WCHAR wzCmdLine[4*1024]=L"";
								wcscpy_s(wzCmdLine,4*1024-1,wstrCmdLine.c_str());

								BOOL   bRet   = FALSE;
								if(hToken)
								{
									OutputDebugString(L"wfRetry: Launch workflowadapter.exe through CreateProcessAsUser");
									bRet=CreateProcessAsUser(hToken,NULL,wzCmdLine,NULL,NULL, 
										FALSE,NORMAL_PRIORITY_CLASS,NULL,NULL,&startupInfo,&processInfo); 
									CloseHandle(hToken);
								}
								else
								{
									OutputDebugString(L"wfRetry: Launch workflowadapter.exe through CreateProcess");
									bRet=CreateProcess(NULL,wzCmdLine,NULL,NULL,FALSE,NORMAL_PRIORITY_CLASS,NULL,NULL,&startupInfo,&processInfo);
								}

								if(bRet)
								{
									DWORD dwExitCode=-1;
									DWORD dwWaitRet=WaitForSingleObject(processInfo.hProcess,INFINITE);

									if(dwWaitRet==WAIT_OBJECT_0)
									{
										if(GetExitCodeProcess(processInfo.hProcess,&dwExitCode))
										{
											if(dwExitCode==0)
												OutputDebugString(L"wfRetry: Succeeded");
											else
												OutputDebugString(L"wfRetry: failed");
										}
									}
									else if(dwWaitRet==WAIT_TIMEOUT)
									{
										OutputDebugString(L"wfRetry: Time out");
									}
									CloseHandle(processInfo.hProcess);
									CloseHandle(processInfo.hThread);
									file.close();
									DeleteFile((*it)->strFullName.c_str());
								}
								else
								{
									std::wstring wstrInfo=L"wfRetry: Failed to create process for file ";
									wstrInfo+=(*it)->strFullName;
									wstrInfo+=L". Try it later!";
									OutputDebugString(wstrInfo.c_str());
								}
							}
						}
						else
						{
							std::wstring wstrInfo=L"wfRetry: The content of the file ";
							wstrInfo+=(*it)->strFullName;
							wstrInfo+=L" is incorrect!";
							OutputDebugString(wstrInfo.c_str());
							file.close();
							::DeleteFile((*it)->strFullName.c_str());
						}

						
						file.close();
					}
					file.clear();
				}//end of Loop for every retry-file
				
			}
		}
		else
		{
			OutputDebugString(L"wfRetry: The Workflow retry module stoped!");
			return 0;
		}
	}
	return 0;
}
extern "C" WFRETRY_API int PluginEntry( void** /*in_context*/ ) 
{
	OutputDebugString(L"wfRetry: The PluginEntry of wfRetry being called...");
	ghThreadRetry = (HANDLE)_beginthreadex(NULL, 0, SvcInit, NULL, 0, NULL);
	return 1;
}

extern "C" WFRETRY_API int PluginUnload( void* /*in_context*/ )
{
	
	if(ghSvcStopEvent != NULL)
	{
		SetEvent(ghSvcStopEvent);
	}
	if(ghThreadRetry != NULL)
	{
		::WaitForSingleObject(ghThreadRetry, 10000);
		::CloseHandle(ghThreadRetry);
	}
	OutputDebugString(L"wfRetry: Workflow Retry thread stopped");
	return 1;
}
