ifeq ($(NLBUILDROOT),)
  $(error NLBUILDROOT undefined.)
endif

ifeq ($(NLEXTERNALDIR),)
  $(error NLEXTERNALDIR undefined.)
endif

ifeq ($(NLEXTERNALDIR2),)
  $(error NLEXTERNALDIR2 undefined.)
endif

#
# Variables
#

PROJECT=wehInstaller.csproj
TARGETS_SP=cscommon
TARGET_FILE_NAME=wehInstaller.exe
TARGET_FILE_NAME_PDB=wehInstaller.pdb

# Only build AnyCpu
TARGETENVARCH=AnyCPU

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

BIN_DIR=$(BUILDTYPE)_dotnet
COMPILE_OUT_DIR=Bin/$(BUILDTYPE)_$(TARGETENVARCH)

# Makefile.vs contains target all which depends on $(TARGETS_SP)
include $(NLBUILDROOT)/build/Makefile.vs
