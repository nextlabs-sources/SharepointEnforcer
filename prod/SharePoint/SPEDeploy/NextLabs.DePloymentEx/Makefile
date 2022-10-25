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

# cp -f $(PROJECT_CONFIG_SP2016)/NextLabs.Entitlement.wsp $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)

PROJECT=NextLabs.Deployment.csproj
TARGETS_SP=spedeploy
TARGETS_SP2019_POST=sp2019_post
TARGETS_SP2016_POST=sp2016_post
TARGET_FILE_NAME=NextLabs.Deployment.dll
TARGET_FILE_NAME_PDB=NextLabs.Deployment.pdb

TARGETENVARCH=AnyCPU

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

BIN_DIR=$(BUILDTYPE)_dotnet

PROJECT_CONFIG_SP2019=sp2019$(BUILDTYPE)
COMPILE_OUT_DIR_2019=Bin/$(PROJECT_CONFIG_SP2019)_$(TARGETENVARCH)

PROJECT_CONFIG_SP2016=sp2016$(BUILDTYPE)
COMPILE_OUT_DIR_2016=Bin/$(PROJECT_CONFIG_SP2016)_$(TARGETENVARCH)

SPEWSP_PACKAGE_NAME=NextLabs.Entitlement.wsp
SPEWSP_PACKAGE_FULL_PATH_2016=$(COMPILE_OUT_DIR_2016)/$(SPEWSP_PACKAGE_NAME)
SPEWSP_PACKAGE_FULL_PATH_2019=$(COMPILE_OUT_DIR_2019)/$(SPEWSP_PACKAGE_NAME)

# Makefile.vs contains target all which depends on $(TARGETS_SP)
include $(NLBUILDROOT)/build/Makefile.vs


.PHONY: sp2019_post
sp2019_post:
	if [ -f $(SPEWSP_PACKAGE_FULL_PATH_2019) ] ; then						\
		echo "Begin copy wsp package from $(SPEWSP_PACKAGE_FULL_PATH_2019) to $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)";	\
		cp -f $(SPEWSP_PACKAGE_FULL_PATH_2019) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019);					\
		echo "End copy wsp package from $(SPEWSP_PACKAGE_FULL_PATH_2019) to $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)";	\
	fi

.PHONY: sp2016_post
sp2016_post:
	if [ -f $(SPEWSP_PACKAGE_FULL_PATH_2016) ] ; then						\
		echo "Begin copy wsp package from $(SPEWSP_PACKAGE_FULL_PATH_2016) to $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)";	\
		cp -f $(SPEWSP_PACKAGE_FULL_PATH_2016) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016);					\
		echo "End copy wsp package from $(SPEWSP_PACKAGE_FULL_PATH_2016) to $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)";	\
	fi
