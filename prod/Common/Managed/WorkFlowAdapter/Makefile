# Notes: To each Visual Studio compilation output. We chain a few lines of scripts
# together. In doing so, Make executes all of them in one process. We want the exit
# status of this process to be the exit status of code compilation. So we add this
# line of code at the end. Note that there is no line continuation character "\" at
# the end of this line because this is the end of a chain of commands to be executed
# in one shell process. However, the line before this should have a "\".
#		exit $$COMPILE_STATUS
#
# On the other hand, if we put MSVC call within a if statement where the whole compilation
# binary signing and copying to distribution directory are all in one line. That means
# the command "exit $$COMPILE_STATUS" cannot be the last commnand to be executed in a
# process. In this case, we need to check exit status before calling "exit $$COMPILE_STATUS".
#		if [ $$COMPILE_STATUS -ne 0 ]; then		\
#			exit $$COMPILE_STATUS ;				\
#		fi ;									\
#

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

PROJECT=WorkFlowAdapter.csproj
TARGETS_SP=cscommon
TARGET_FILE_NAME=WorkFlowAdapter.exe
TARGET_FILE_NAME_PDB=WorkFlowAdapter.pdb

# Only build AnyCpu
TARGETENVARCH=AnyCPU

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

BIN_DIR=$(BUILDTYPE)_dotnet
COMPILE_OUT_DIR=Bin/$(BUILDTYPE)_$(TARGETENVARCH)

# Makefile.vs contains target all which depends on $(TARGETS_SP)
include $(NLBUILDROOT)/build/Makefile.vs

