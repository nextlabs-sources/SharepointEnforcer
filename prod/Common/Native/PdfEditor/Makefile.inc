
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)

COMPILER=VS2008
PLATFORMSDK=7.0

RTLIBTYPE=dynamic

SUBTARGET=PdfEditor

#Define the objects of the target module
SRC+= \
 	src/stdafx.cpp \
	src/dllmain.cpp \
	src/PdfHeaderFooter.cpp \
	src/PdfEditor.cpp \
	src/PdfEditor_i.c

RCSRC = src/PdfEditor.rc

IDL_SRC = src/PdfEditor.idl

INCPATH += \
	./idlgen \
	./include \
	$(NLEXTERNALDIR)/podofo-0.8.1/podofo \
	$(NLEXTERNALDIR)/podofo-0.8.1/src

ifeq ($(TARGETENVARCH), x64)
	LIBPATH += \
		$(NLEXTERNALDIR)/podofo-0.8.1/release_lib_x64_vs2008
else
	# Empyt or x86
	LIBPATH += \
		$(NLEXTERNALDIR)/podofo-0.8.1/release_lib_win32_vs2008
endif

LIBS = PoDoFoLib
