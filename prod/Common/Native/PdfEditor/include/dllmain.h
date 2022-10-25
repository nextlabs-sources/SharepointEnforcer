// dllmain.h : Declaration of module class.

class CPdfEditorModule : public CAtlDllModuleT< CPdfEditorModule >
{
public :
	DECLARE_LIBID(LIBID_PdfEditorLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_PDFEDITOR, "{FCF8A7BF-201A-4E05-B61C-6731530798E1}")
};

extern class CPdfEditorModule _AtlModule;
