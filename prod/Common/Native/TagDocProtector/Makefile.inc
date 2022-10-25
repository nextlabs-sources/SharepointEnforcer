
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


COMPILER=VS2008
PLATFORMSDK=7.0

RTLIBTYPE=dynamic

SUBTARGET  = TagDocProtector

SRC = \
	src/FileTagging.cpp \
	src/log.cpp \
	src/stdafx.cpp \
	src/TagDocProtector.cpp \
	src/TagProtector.cpp \
	src/TagDocProtector.rc \
	src/TagDocProtector_i.c \
	src/FileEncryptIgnore.cpp

RCSRC = src/TagDocProtector.rc

# .idl file must be placed in SRC directory.
IDL_SRC = src/TagDocProtector.idl

INCPATH += \
	./include \
	./idlgen \
	$(NLBUILDROOT)/prod/Common/Archive/include

LIBPATH += \
	$(NLBUILDROOT)/prod/Common/Archive/Binary/release_win_$(TARGETENVARCH)

ifeq ($(TARGETENVARCH), x64)
	LIBS = comsuppw resattrlib resattrmgr
else
	# Empyt or x86
	LIBS = comsuppw resattrlib32 resattrmgr32
endif
