# =============================================================================
# Top-level makefile include by makefiles that wrap around VisualStudio projects
# =============================================================================

include $(NLBUILDROOT)/build/Makefile.ver

ifeq ($(ProgramW6432), )
	ENV_OS=x86
	PROGRAM_FILES_X86=C:/Program Files
else
	ENV_OS=x64
	PROGRAM_FILES_X86=C:/Program Files (x86)
endif

OFFICIALCERT=0
SIGNTOOL_OFFICIAL_TOOL=$(PROGRAM_FILES_X86)/Windows Kits/8.0/bin/x64/signtool.exe

ifeq ($(NLDEVBUILD), TRUE)
	SIGNTOOL_OFFICIAL_ARGS=sign /f ${NLEXTERNALDIR}/buildtools/signtool/NextLabsDebug.pfx /p 123blue! /n "Nextlabs Debug"
else
	SIGNTOOL_OFFICIAL_ARGS=sign /ac c:/release/bin/DigiCertAssuredIDRootCA.cer /f c:/release/bin/NextLabs.pfx /p IiVf1itvOrqJ /n "NextLabs Inc." /fd sha256 /tr http://timestamp.digicert.com
endif
SIGNTOOL_OFFICIAL='$(SIGNTOOL_OFFICIAL_TOOL)' $(SIGNTOOL_OFFICIAL_ARGS)


MSVSIDE=x:/common7/IDE/devenv.exe

ifeq ($(TARGETENVARCH),)
	TARGETENVARCH=x86
endif

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

ifeq ($(BIN_DIR),)
	BIN_DIR=$(BUILDTYPE)_win_$(TARGETENVARCH)
endif

BUILDOUTPUTDIR=$(NLBUILDROOT)/bin/$(BIN_DIR)


ifeq ($(VERSION_BUILD), )
	VERSION_BUILD=$(shell date +"%y.%j.%H%M")DX-$(HOSTNAME)-$(USERNAME)-$(shell date +"%Y.%m.%d-%H:%M")
endif

BUILD_LOGFILE=$(BUILDTYPE)_build.log
ifeq ($(COMPILE_OUT_DIR),)
	COMPILE_OUT_DIR=Bin/$(BUILDTYPE)_$(TARGETENVARCH)
endif

ifneq ($(PROJECT_CONFIG_SP2019),)
	BUILD_LOGFILE_2019=$(PROJECT_CONFIG_SP2019)_build.log
	ifeq ($(COMPILE_OUT_DIR_2019),)
		COMPILE_OUT_DIR_2019=Bin/$(PROJECT_CONFIG_SP2019)
	endif
endif

ifneq ($(PROJECT_CONFIG_SP2016),)
	BUILD_LOGFILE_2016=$(PROJECT_CONFIG_SP2016)_build.log
	ifeq ($(COMPILE_OUT_DIR_2016),)
		COMPILE_OUT_DIR_2016=Bin/$(PROJECT_CONFIG_SP2016)
	endif
endif

ifeq ($(RCSRC),)
	RCSRC=AssemblyInfo.cs
	ifneq ($(RCSRC), $(wildcard $(RCSRC)))
		RCSRC=Properties/AssemblyInfo.cs
	endif
endif

VERSION_FOUR_STR=$(VERSION_MAJOR_SPE).$(VERSION_MINOR_SPE).$(VERSION_MAINTENANCE_SPE).$(BUILD_NUMBER)

$(info --------------------------------------------------------------------------)
$(info [Targets])
$(info TARGETS_SP=$(TARGETS_SP))
$(info PROJECT=$(PROJECT))
$(info PROJECT_SP2019=$(PROJECT_SP2019))
$(info PROJECT_SP2016=$(PROJECT_SP2016))
$(info PROJECT_CONFIG_SP2019=$(PROJECT_CONFIG_SP2019))
$(info PROJECT_CONFIG_SP2016=$(PROJECT_CONFIG_SP2016))
$(info OFFICIALCERT=$(OFFICIALCERT))
$(info RCSRC=$(RCSRC))
$(info [Parameters])
$(info BUILDTYPE=$(BUILDTYPE))
$(info TARGETENVARCH=$(TARGETENVARCH))
$(info BIN_DIR=$(BIN_DIR))
$(info NLPLUGIN_BASE_FOLDER=$(NLPLUGIN_BASE_FOLDER))
$(info COMPILE_OUT_DIR=$(COMPILE_OUT_DIR))
$(info COMPILE_OUT_DIR_2019=$(COMPILE_OUT_DIR_2019))
$(info COMPILE_OUT_DIR_2016=$(COMPILE_OUT_DIR_2016))
$(info NLBUILDROOT=$(NLBUILDROOT))
$(info NLEXTERNALDIR=$(NLEXTERNALDIR))
$(info NLEXTERNALDIR2=$(NLEXTERNALDIR2))
$(info BUILDOUTPUTDIR=$(BUILDOUTPUTDIR))
$(info [VERSION])
$(info PRODUCT=$(VERSION_PRODUCT_SPE))
$(info BUILD_NUMBER=$(BUILD_NUMBER))
$(info VERSION_BUILD=$(VERSION_BUILD))
$(info RELEASE=$(VERSION_MAJOR_SPE).$(VERSION_MINOR_SPE).$(VERSION_MAINTENANCE_SPE))
$(info VERSION_FOUR_STR=$(VERSION_FOUR_STR))
$(info ---------------------------------------------------------------------------)

.PHONY: all
all: $(TARGETS_SP)

.PHONY: cscommon
cscommon: versionInfo_cscommon spcompile_cscommon

.PHONY: spcommon
spcommon: spcompile

.PHONY: spenforcement
spenforcement: spcompile

.PHONY: CompileSPCommon
CompileSPCommon: spcompile

.PHONY: spedeploy
spedeploy: spcompile

.PHONY: speplugin
speplugin: spcompile

.PHONY: spcompile
spcompile: spcompile_sp2019 spcompile_sp2016

.PHONY: spcompile_sp2019
spcompile_sp2019: versionInfo_sp2019 $(TARGETS_SP2019_PRE) sp2019 $(TARGETS_SP2019_POST)

.PHONY: spcompile_sp2016
spcompile_sp2016: versionInfo_sp2016 $(TARGETS_SP2016_PRE) sp2016 $(TARGETS_SP2016_POST)


.PHONY: versionInfo_cscommon
versionInfo_cscommon:
	echo "Begin update version RCSRC=$(RCSRC)"
	@if [ "$(RCSRC)" != "" ]; then \
		perl $(NLBUILDROOT)/build/updateVersionInfo_csproj_base.pl "$(RCSRC)" "$(VERSION_PRODUCT_SPE) (Common)" $(VERSION_MAJOR_SPE) $(VERSION_MINOR_SPE) $(VERSION_MAINTENANCE_SPE) $(BUILD_NUMBER) 1; \
		echo " --- Modified version file ---" ; \
		egrep "FILEVERSION|PRODUCTVERSION|CompanyName|FileDescription|FileVersion|LegalCopyright|ProductName|ProductVersion" $(RCSRC) ; \
	fi
	echo "End update version RCSRC=$(RCSRC)"

.PHONY: versionInfo_sp2019
versionInfo_sp2019:
	echo "Begin update version RCSRC=$(RCSRC)"
	@if [ "$(RCSRC)" != "" ]; then \
		perl $(NLBUILDROOT)/build/updateVersionInfo_csproj_base.pl "$(RCSRC)" "$(VERSION_PRODUCT_SPE) (2019)" $(VERSION_MAJOR_SPE) $(VERSION_MINOR_SPE) $(VERSION_MAINTENANCE_SPE) $(BUILD_NUMBER) 1; \
		echo " --- Modified version file ---" ; \
		egrep "FILEVERSION|PRODUCTVERSION|CompanyName|FileDescription|FileVersion|LegalCopyright|ProductName|ProductVersion" $(RCSRC) ; \
	fi
	echo "End update version RCSRC=$(RCSRC)"

.PHONY: versionInfo_sp2016
versionInfo_sp2016:
	echo "Begin update version RCSRC=$(RCSRC)"
	@if [ "$(RCSRC)" != "" ]; then \
		perl $(NLBUILDROOT)/build/updateVersionInfo_csproj_base.pl "$(RCSRC)" "$(VERSION_PRODUCT_SPE) (2016)" $(VERSION_MAJOR_SPE) $(VERSION_MINOR_SPE) $(VERSION_MAINTENANCE_SPE) $(BUILD_NUMBER) 1; \
		echo " --- Modified version file ---" ; \
		egrep "FILEVERSION|PRODUCTVERSION|CompanyName|FileDescription|FileVersion|LegalCopyright|ProductName|ProductVersion" $(RCSRC) ; \
	fi
	echo "End update version RCSRC=$(RCSRC)"


.PHONY: spcompile_cscommon
spcompile_cscommon:
	@echo ""
	@echo "Building $(PROJECT) ($(BUILDTYPE)) for NextLabs SharePoint Enforcer"
	rm -rf $(BUILD_LOGFILE)
	$(MSVSIDE) $(PROJECT) /build "$(BUILDTYPE)|$(TARGETENVARCH)" /out $(BUILD_LOGFILE) ; \
	COMPILE_STATUS=$$? ;									\
	if [ -f $(BUILD_LOGFILE) ] ; then						\
		echo "[[DUMP BEGIN - $(BUILD_LOGFILE)]]" ;			\
		cat $(BUILD_LOGFILE) ;								\
		echo "[[DUMP END - $(BUILD_LOGFILE)]]" ;			\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE)" ;			\
	fi ;													\
	exit $$COMPILE_STATUS

	echo "Do signature for file $(COMPILE_OUT_DIR)/$(TARGET_FILE_NAME)"
	@if [ $(OFFICIALCERT) -ne 0 ]; then								\
		echo $(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR)/$(TARGET_FILE_NAME) ;	\
		$(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR)/$(TARGET_FILE_NAME) ;	\
	fi
	@if [ ! -d $(BUILDOUTPUTDIR) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR) ;						\
	fi
	cp -f $(COMPILE_OUT_DIR)/$(TARGET_FILE_NAME) $(BUILDOUTPUTDIR);
	cp -f $(COMPILE_OUT_DIR)/$(TARGET_FILE_NAME_PDB) $(BUILDOUTPUTDIR);


.PHONY: sp2019
sp2019:
	@echo ""
	@echo "Building $(PROJECT) ($(PROJECT_CONFIG_SP2019)) for NextLabs SharePoint Enforcer"
	rm -rf  $(BUILD_LOGFILE_2019)
	$(MSVSIDE) $(PROJECT) /build "$(PROJECT_CONFIG_SP2019)|AnyCPU" /out $(BUILD_LOGFILE_2019) ; \
	COMPILE_STATUS=$$? ;									\
	if [ -f $(BUILD_LOGFILE_2019) ] ; then				\
		echo "[[DUMP BEGIN - $(BUILD_LOGFILE_2019)]]" ;	\
		cat $(BUILD_LOGFILE_2019) ;						\
		echo "[[DUMP END - $(BUILD_LOGFILE_2019)]]" ;		\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE_2019)" ;	\
	fi ;													\
	exit $$COMPILE_STATUS

	echo "Do signature for file $(COMPILE_OUT_DIR_2019)/$(TARGET_FILE_NAME)"
	@if [ $(OFFICIALCERT) -ne 0 ]; then								\
		echo $(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR_2019)/$(TARGET_FILE_NAME) ;	\
		$(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR_2019)/$(TARGET_FILE_NAME) ;	\
	fi

	@if [ ! -d $(BUILDOUTPUTDIR) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR) ;						\
	fi
	@if [ ! -d $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019) ;						\
	fi

	cp -f $(COMPILE_OUT_DIR_2019)/$(TARGET_FILE_NAME) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019);
	cp -f $(COMPILE_OUT_DIR_2019)/$(TARGET_FILE_NAME_PDB) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019);

.PHONY: sp2016
sp2016:
	@echo ""
	@echo "Building $(PROJECT) ($(PROJECT_CONFIG_SP2016)) for NextLabs SharePoint Enforcer"
	rm -rf  $(BUILD_LOGFILE_2016)
	$(MSVSIDE) $(PROJECT) /build "$(PROJECT_CONFIG_SP2016)|AnyCPU" /out $(BUILD_LOGFILE_2016) ; \
	COMPILE_STATUS=$$? ;									\
	if [ -f $(BUILD_LOGFILE_2016) ] ; then				\
		echo "[[DUMP BEGIN - $(BUILD_LOGFILE_2016)]]" ;	\
		cat $(BUILD_LOGFILE_2016) ;						\
		echo "[[DUMP END - $(BUILD_LOGFILE_2016)]]" ;		\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE_2016)" ;	\
	fi ;													\
	exit $$COMPILE_STATUS

	echo "Do signature for file $(COMPILE_OUT_DIR_2016)/$(TARGET_FILE_NAME)"
	@if [ $(OFFICIALCERT) -ne 0 ]; then								\
		echo $(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR_2016)/$(TARGET_FILE_NAME) ;	\
		$(SIGNTOOL_OFFICIAL) $(COMPILE_OUT_DIR_2016)/$(TARGET_FILE_NAME) ;	\
	fi

	@if [ ! -d $(BUILDOUTPUTDIR) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR) ;						\
	fi
	@if [ ! -d $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016) ;						\
	fi

	cp -f $(COMPILE_OUT_DIR_2016)/$(TARGET_FILE_NAME) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016);
	cp -f $(COMPILE_OUT_DIR_2016)/$(TARGET_FILE_NAME_PDB) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016);


.PHONY: clean
clean:
	@if [ -e ./Makefile ]; then							\
		rm -rf bin release debug sp2019release sp2019debug sp2016release sp2016debug obj* *.suo *.ncb *.pdb *.log; \
	fi
