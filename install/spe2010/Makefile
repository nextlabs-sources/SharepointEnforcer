ifeq ($(NLBUILDROOT),)
  $(error NLBUILDROOT undefined.)
endif

ifeq ($(NLEXTERNALDIR),)
  $(error NLEXTERNALDIR undefined.)
endif

ifeq ($(NLEXTERNALDIR2),)
  $(error NLEXTERNALDIR2 undefined.)
endif

ifeq ($(BUILDTYPE),)
	BUILDTYPE=dev
endif

ifeq ($(VERSION_BUILD_SHORT),)
	VERSION_BUILD_SHORT=1
endif

include $(NLBUILDROOT)/build/Makefile.ver

PRODUCT_DIR_NAME=spe2010
VERSION_PRODUCT=$(VERSION_PRODUCT_SPE)
VERSION_STR=$(VERSION_MAJOR_SPE).$(VERSION_MINOR_SPE).$(VERSION_MAINTENANCE_SPE)
ISM_TEMPLATE_FILE_NAME=SharePointEnforcer-2010.ism
ISM_RELEASE=msi
ISM_PRODUCT_CONFIG=64bit
MSI_FILE_BASE_NAME32=SharePointEnforcer-2010-32-$(VERSION_STR)-$(VERSION_BUILD_SHORT)
MSI_FILE_BASE_NAME64=SharePointEnforcer-2010-64-$(VERSION_STR)-$(VERSION_BUILD_SHORT)
MSI_FILE_NAME=$(MSI_FILE_BASE_NAME64).msi
PACKAGE_DIR_NAME=package64
PACKAGE_FILE=SharePointEnforcer-2010-64-$(VERSION_STR)-$(VERSION_BUILD_SHORT)-$(shell date +"%Y%m%d%H%M").zip

include $(NLBUILDROOT)/install/scripts/Makefile.install
