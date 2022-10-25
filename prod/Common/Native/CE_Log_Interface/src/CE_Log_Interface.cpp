// CE_Log_Interface.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "CE_Log_Interface.h"
#include "celog.h"
#include "celog_policy_file.hpp"

#ifdef _MANAGED
#pragma managed(push, off)
#endif
CELog * g_mylog;
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	hModule;
	ul_reason_for_call;
	lpReserved;
    return TRUE;
}
bool Is64WOW()
{
	if(8 == sizeof(char *))
		return true;
	else
		return false;
}

LIBEXPORT_API void CE_Log_Init(BYTE * file)
{
	
	HKEY   hKEY;
	char   *data_Set= "Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\";
	long   ret0=(::RegOpenKeyExA(HKEY_LOCAL_MACHINE,data_Set,   0,   KEY_READ,   &hKEY));
	HMODULE re = 0;
	if(ret0!=ERROR_SUCCESS)
	{
		OutputDebugStringA("Can not Read the Reg Key of SPE #1");
	}
	else
	{
		BYTE  * owner_Get=new   BYTE[80];
		DWORD   type_1=REG_SZ;  
		DWORD   cbData_1=80;
		long   ret1=::RegQueryValueExA(hKEY,   "InstallDir",   NULL,&type_1,   owner_Get,   &cbData_1);     
		
		if(ret1==ERROR_SUCCESS)
		{
			char path[300];
			memcpy(path,owner_Get,300);			
			std::string strCommonPath = path;
			std::string strLib;
			std::string strMgr;
			if(Is64WOW())
			{
				strLib = strCommonPath + "Common\\bin64\\celog.dll";
			}
			else
			{
				strLib = strCommonPath + "Common\\bin32\\celog32.dll";
			}
			re = LoadLibraryA(strLib.c_str());
			OutputDebugStringA("LoadLibraryA");
			OutputDebugStringA(strLib.c_str());
		}
		else
		{
			OutputDebugStringA("Can not Read the Reg Key of SPE #2");
		}
	}
	if(re != NULL)
	{
		OutputDebugStringA("Module of celog.dll load ok");
	}
	if(g_mylog == NULL)
	{
		g_mylog = new CELog();
	}
	else
	{
		delete g_mylog;
		g_mylog = new CELog();
	}
	if(g_mylog != NULL)
	{
		OutputDebugStringA("g_mylog SetPolicy");
		g_mylog->SetPolicy( new CELogPolicy_File((char *)file) );
		g_mylog->Enable();
		g_mylog->SetLevel(7);
	}
	else
	{
		OutputDebugStringA("g_mylog malloc failed");
	}
}

LIBEXPORT_API void CE_Log_SetLevel(int Level)
{
	if(g_mylog != 0)
	{
		g_mylog->SetLevel(Level);
	}
}


LIBEXPORT_API int CE_Log(int lv, BYTE * msg)
{
	if(g_mylog != 0)
	{
		return g_mylog->Log(lv,(char *)msg);
	}
	return 0;
}


#ifdef _MANAGED
#pragma managed(pop)
#endif

