

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Mon Jul 20 17:50:05 2020
 */
/* Compiler settings for .\src\PdfEditor.idl:
    Oicf, W1, Zp8, env=Win64 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __PdfEditor_i_h__
#define __PdfEditor_i_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IPdfHeaderFooter_FWD_DEFINED__
#define __IPdfHeaderFooter_FWD_DEFINED__
typedef interface IPdfHeaderFooter IPdfHeaderFooter;
#endif 	/* __IPdfHeaderFooter_FWD_DEFINED__ */


#ifndef __PdfHeaderFooter_FWD_DEFINED__
#define __PdfHeaderFooter_FWD_DEFINED__

#ifdef __cplusplus
typedef class PdfHeaderFooter PdfHeaderFooter;
#else
typedef struct PdfHeaderFooter PdfHeaderFooter;
#endif /* __cplusplus */

#endif 	/* __PdfHeaderFooter_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __IPdfHeaderFooter_INTERFACE_DEFINED__
#define __IPdfHeaderFooter_INTERFACE_DEFINED__

/* interface IPdfHeaderFooter */
/* [unique][helpstring][nonextensible][dual][uuid][object] */ 


EXTERN_C const IID IID_IPdfHeaderFooter;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("31EE0057-12B9-47AE-AC1A-C5C2E7F69C50")
    IPdfHeaderFooter : public IDispatch
    {
    public:
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE AddHeaderText( 
            /* [in] */ BSTR filePath,
            /* [in] */ BSTR headerText,
            /* [in] */ BSTR position,
            /* [in] */ LONG left,
            /* [in] */ LONG height) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPdfHeaderFooterVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IPdfHeaderFooter * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IPdfHeaderFooter * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IPdfHeaderFooter * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IPdfHeaderFooter * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IPdfHeaderFooter * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IPdfHeaderFooter * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IPdfHeaderFooter * This,
            /* [in] */ DISPID dispIdMember,
            /* [in] */ REFIID riid,
            /* [in] */ LCID lcid,
            /* [in] */ WORD wFlags,
            /* [out][in] */ DISPPARAMS *pDispParams,
            /* [out] */ VARIANT *pVarResult,
            /* [out] */ EXCEPINFO *pExcepInfo,
            /* [out] */ UINT *puArgErr);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *AddHeaderText )( 
            IPdfHeaderFooter * This,
            /* [in] */ BSTR filePath,
            /* [in] */ BSTR headerText,
            /* [in] */ BSTR position,
            /* [in] */ LONG left,
            /* [in] */ LONG height);
        
        END_INTERFACE
    } IPdfHeaderFooterVtbl;

    interface IPdfHeaderFooter
    {
        CONST_VTBL struct IPdfHeaderFooterVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPdfHeaderFooter_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IPdfHeaderFooter_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IPdfHeaderFooter_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IPdfHeaderFooter_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IPdfHeaderFooter_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IPdfHeaderFooter_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IPdfHeaderFooter_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#define IPdfHeaderFooter_AddHeaderText(This,filePath,headerText,position,left,height)	\
    ( (This)->lpVtbl -> AddHeaderText(This,filePath,headerText,position,left,height) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IPdfHeaderFooter_INTERFACE_DEFINED__ */



#ifndef __PdfEditorLib_LIBRARY_DEFINED__
#define __PdfEditorLib_LIBRARY_DEFINED__

/* library PdfEditorLib */
/* [helpstring][version][uuid] */ 


EXTERN_C const IID LIBID_PdfEditorLib;

EXTERN_C const CLSID CLSID_PdfHeaderFooter;

#ifdef __cplusplus

class DECLSPEC_UUID("CC455E65-8132-4DB3-AA48-C400B70FE95C")
PdfHeaderFooter;
#endif
#endif /* __PdfEditorLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


