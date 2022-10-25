// PdfHeaderFooter.h : Declaration of the CPdfHeaderFooter

#pragma once
#include "resource.h"       // main symbols

#include "PdfEditor_i.h"

// CPdfHeaderFooter

class ATL_NO_VTABLE CPdfHeaderFooter :
	public CComObjectRootEx<CComMultiThreadModel>,
	public CComCoClass<CPdfHeaderFooter, &CLSID_PdfHeaderFooter>,
	public IDispatchImpl<IPdfHeaderFooter, &IID_IPdfHeaderFooter, &LIBID_PdfEditorLib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
public:
	CPdfHeaderFooter()
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_PDFHEADERFOOTER)


BEGIN_COM_MAP(CPdfHeaderFooter)
	COM_INTERFACE_ENTRY(IPdfHeaderFooter)
	COM_INTERFACE_ENTRY(IDispatch)
END_COM_MAP()



	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

public:
    /** Add header text in each page of PDF file.
     *  \param filePath  the path of pdf file.
     *  \param headerText  the text need to be add to header.(The limited length fo text is 255)
     *  \param position  alignment of the text line (include "Ledft", "Right", "Center").
     *  \param left  the percentage number of page width, means the position that header text start from the left.
     *  \param height  the size of font.
     */
	STDMETHOD(AddHeaderText)(BSTR filePath, BSTR headerText, BSTR position, LONG left, LONG height);

private:

	bool AddText(BSTR filePath, BSTR headerText, BSTR position, LONG left, LONG height, bool bHeader);
};

OBJECT_ENTRY_AUTO(__uuidof(PdfHeaderFooter), CPdfHeaderFooter)
