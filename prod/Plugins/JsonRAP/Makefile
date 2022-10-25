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

PROJECT=JsonRAP.csproj
TARGETS_SP=speplugin
TARGETS_SP2019_POST=sp2019_post
TARGETS_SP2016_POST=sp2016_post
TARGET_FILE_NAME=JsonRAP.dll
TARGET_FILE_NAME_PDB=JsonRAP.pdb
PLUGIN_NAME=JsonRap

TARGETENVARCH=AnyCPU

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

BIN_DIR=$(BUILDTYPE)_dotnet

PROJECT_CONFIG_SP2019=sp2019$(BUILDTYPE)
COMPILE_OUT_DIR_2019=Bin/$(PROJECT_CONFIG_SP2019)_$(TARGETENVARCH)

PROJECT_CONFIG_SP2016=sp2016$(BUILDTYPE)
COMPILE_OUT_DIR_2016=Bin/$(PROJECT_CONFIG_SP2016)_$(TARGETENVARCH)

# Makefile.vs contains target all which depends on $(TARGETS_SP)
include $(NLBUILDROOT)/build/Makefile.vs

VERSION_STR=$(VERSION_MAJOR_SPE).$(VERSION_MINOR_SPE).$(VERSION_MAINTENANCE_SPE)

# Bin/Plugins/2016/pluginName/files ==> Bin/Plugins/pluginName-sp2016.zip
.PHONY: sp2019_post
sp2019_post:
	echo "NLPLUGIN_BASE_FOLDER=$(NLPLUGIN_BASE_FOLDER)"
	mkdir -p $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2019)/$(PLUGIN_NAME)
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)/JsonRAP.dll $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2019)/$(PLUGIN_NAME)/
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)/TDFFileAnalyser.dll $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2019)/$(PLUGIN_NAME)/
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)/TDFConfig.xml $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2019)/$(PLUGIN_NAME)/
	cd $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2019)/$(PLUGIN_NAME) &&  zip -r $(NLPLUGIN_BASE_FOLDER)/$(PLUGIN_NAME)-$(PROJECT_CONFIG_SP2019)-$(VERSION_FOUR_STR).zip *

.PHONY: sp2016_post
sp2016_post:
	echo "NLPLUGIN_BASE_FOLDER=$(NLPLUGIN_BASE_FOLDER)"
	mkdir -p $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2016)/$(PLUGIN_NAME)
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)/JsonRAP.dll $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2016)/$(PLUGIN_NAME)/
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)/TDFFileAnalyser.dll $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2016)/$(PLUGIN_NAME)/
	cp -f $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)/TDFConfig.xml $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2016)/$(PLUGIN_NAME)/
	cd $(NLPLUGIN_BASE_FOLDER)/$(PROJECT_CONFIG_SP2016)/$(PLUGIN_NAME) &&  zip -r $(NLPLUGIN_BASE_FOLDER)/$(PLUGIN_NAME)-$(PROJECT_CONFIG_SP2016)-$(VERSION_FOUR_STR).zip *
