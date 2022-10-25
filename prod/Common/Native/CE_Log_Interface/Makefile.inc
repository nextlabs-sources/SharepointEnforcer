VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)

COMPILER=VS2008
PLATFORMSDK=7.0

RTLIBTYPE = dynamic
SUBTARGET = CE_Log_Interface

SRC = \
    src/stdafx.cpp  \
    src/CE_Log_Interface.cpp

RCSRC = src/CE_Log_Interface.rc

INCPATH += \
	./include \
	$(NLBUILDROOT)/prod/Common/Archive/include \
	$(NLEXTERNALDIR)/boost/boost_1_45_0
