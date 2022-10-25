//
// msoipi.h - Microsoft IRM Protector Interface
//
// Copyright (C) 2004 Microsoft Corporation
//
// Version 1.1
//
// THIS IS PRELIMINARY DOCUMENTATION AND SUBJECT TO CHANGE.
// LAST UPDATED: February 3, 2006

#ifndef _MSOIPI_H
#define _MSOIPI_H

#include <objbase.h>

//****************************************************************************
//    I_IrmCrypt
//****************************************************************************


// {598F03E8-948A-425e-B7B2-2D613CEDFFEF}
DEFINE_GUID(IID_I_IrmCrypt, 
0x598f03e8, 0x948a, 0x425e, 0xb7, 0xb2, 0x2d, 0x61, 0x3c, 0xed, 0xff, 0xef);

//****************************************************************************
//    I_IrmPolicyInfoRMS
//****************************************************************************

// {175EF0A4-8EB8-49ac-9049-F40EC69EC0A7}
DEFINE_GUID(IID_I_IrmPolicyInfoRMS, 
0x175ef0a4, 0x8eb8, 0x49ac, 0x90, 0x49, 0xf4, 0xe, 0xc6, 0x9e, 0xc0, 0xa7);

//****************************************************************************
//    I_IrmPolicyInfo
//****************************************************************************

// Rights for HrGetRightsMask
#define rightNone     0x0000
#define rightView     0x0001
#define rightEdit     0x0002
#define rightSave     0x0004
#define rightExtract  0x0008
#define rightPrint    0x0010
#define rightVBA      0x0020 //Not used in SharePoint
#define rightAdmin    0x0040
#define rightForward  0x0080 //Not used in SharePoint
#define rightReply    0x0100 //Not used in SharePoint
#define rightReplyAll 0x0200 //Not used in SharePoint
#define rightAll      0xFFFF //Not used in SharePoint


// {2CDC48E9-DB49-47E6-8487-A2EA1FCE292F}
DEFINE_GUID(IID_I_IrmPolicyInfo, 
0x2cdc48e9, 0xdb49, 0x47e6, 0x84, 0x87, 0xa2, 0xea, 0x1f, 0xce, 0x29, 0x2f);

//****************************************************************************
//    I_IrmProtector
//****************************************************************************
#undef  INTERFACE
#define INTERFACE   I_IrmProtector


//HrIsProtected possible results:
#define MSOIPI_RESULT_UNKNOWN           0 //I cannot tell.  As me in HrProtect/HrUnprotect
#define MSOIPI_RESULT_PROTECTED         1 //pilbInput is protected
#define MSOIPI_RESULT_UNPROTECTED       2 //Input is unprotected
#define MSOIPI_RESULT_NOT_MY_FILE       3 //Not my file. As in an XML file that is not an InfoPath form.

// Status:
#define MSOIPI_STATUS_UNKNOWN              0
#define MSOIPI_STATUS_PROTECT_SUCCESS      1
#define MSOIPI_STATUS_UNPROTECT_SUCCESS    2
#define MSOIPI_STATUS_ALREADY_PROTECTED    3 //If called to protect and file is already protected
#define MSOIPI_STATUS_CANT_PROTECT         4 //If protect fails
#define MSOIPI_STATUS_ALREADY_UNPROTECTED  5 //If called to unprotect and file is already unprotected
#define MSOIPI_STATUS_CANT_UNPROTECT       6 //If unprotect fails
#define MSOIPI_STATUS_NOT_OWNER            7 //If the ServerId doesn't have an EUL that allows unprotection
//Unused 8
#define MSOIPI_STATUS_NOT_MY_FILE          9 //If the protector is passed a file it cannot identify
#define MSOIPI_STATUS_FILE_CORRUPT        10 //If the parts of the file that make this an IRM file are corrupt
#define MSOIPI_STATUS_PLATFORM_IRM_FAILED 11 //If a call back into the platform for RMS APIs fails
#define MSOIPI_STATUS_BAD_INSTALL         12 //If the protector isn't installed properly

// {FCFBC0AC-672B-452d-80E5-40652503D96E}
DEFINE_GUID( IID_I_IrmProtector, 0xfcfbc0ac, 0x672b, 0x452d, 0x80, 0xe5, 0x40, 0x65, 0x25, 0x03, 0xd9, 0x6e);


#endif
