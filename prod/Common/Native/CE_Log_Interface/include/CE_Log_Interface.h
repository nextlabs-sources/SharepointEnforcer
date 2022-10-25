#ifndef _CE_LOG_INTERFACE_HEADER_H
#define _CE_LOG_INTERFACE_HEADER_H

#define LIBEXPORT_API extern "C" __declspec(dllexport)
#include <string>
using namespace std;
LIBEXPORT_API void CE_Log_Init(BYTE * file);
LIBEXPORT_API void CE_Log_SetLevel(int Level);
//This function is used to tag the specified tags to the file we want
LIBEXPORT_API int CE_Log(int lv, BYTE * msg);

#endif