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


ILDASMPARAM=/out:
ILASMIL=QueryCloudAZSDK.il
ILASMPARA=/dll /key=
ILASMRESPARA=/resource:QueryCloudAZSDK.res
QCAZSDK=QueryCloudAZSDK.dll
QCAZSDKSRC=W:/bin/release_win_x64/QueryCloudAZSDK.dll
COMSNK=Common.snk

#try to import library from SDKWrapper.dll
SDKWSRC=W:/bin/release_win_x64/SDKWrapper.dll
SDKWDST=SDKWrapper.dll
SDKWLib=/out:SDKWrapperLib.dll
SDKWCopy=$(NLBUILDROOT)/bin/release_win_x64/SDKWrapper.dll
TLB=O:/bin/TlbImp.exe
ILDASMTOOL=O:/bin/ildasm.exe
ILASMTOOL=Q:/ilasm.exe


MACH= /machine:x64
KEY=/keyfile:Common.snk

#try to import library from PdfEditor.dll
PDFESRC=$(NLBUILDROOT)/bin/release_win_x64/PdfEditor.dll
PDFEDST=PdfEditor.dll
PDFELib=/out:PdfEditorLib.dll
TLB=O:/bin/TlbImp.exe
MACH= /machine:x64
KEY=/keyfile:Common.snk


$(info TLB=$(TLB))
$(info SDKWSRC=$(SDKWSRC))
$(info SDKWDST=$(SDKWDST))
$(info SDKWLib=$(SDKWLib))

#
# Variables
#

PROJECT_SP2019=NextLabs.Common_sp2019.csproj
PROJECT_SP2016=NextLabs.Common_sp2016.csproj
TARGETS_SP=sp2019 sp2016

ifeq ($(TARGETENVARCH),)
	TARGETENVARCH=x86
endif

ifneq ($(BUILDTYPE), release)
	BUILDTYPE=debug
endif

PROJECT_CONFIG_SP2019=sp2019$(BUILDTYPE)
PROJECT_CONFIG_SP2016=sp2016$(BUILDTYPE)
BIN_DIR=$(BUILDTYPE)_dotnet
BUILD_LOGFILE_SP2019=$(PROJECT_CONFIG_SP2019)_build.log
BUILD_LOGFILE_SP2016=$(PROJECT_CONFIG_SP2016)_build.log

# Makefile.vs contains target all which depends on $(TARGETS_SP)
include $(NLBUILDROOT)/build/Makefile.vs


$(info --------------------------------------------------------------------------)
$(info [Project])
$(info PROJECT_SP2019=$(PROJECT_SP2019))
$(info PROJECT_SP2016=$(PROJECT_SP2016))
$(info PROJECT_CONFIG_SP2019=$(PROJECT_CONFIG_SP2019))
$(info PROJECT_CONFIG_SP2016=$(PROJECT_CONFIG_SP2016))
$(info [Parameters])
$(info TARGETS_SP=$(TARGETS_SP))
$(info BUILDTYPE=$(BUILDTYPE))
$(info NLBUILDROOT=$(NLBUILDROOT))
$(info NLEXTERNALDIR=$(NLEXTERNALDIR))
$(info NLEXTERNALDIR2=$(NLEXTERNALDIR2))
$(info BUILDOUTPUTDIR=$(BUILDOUTPUTDIR))
$(info BIN_DIR=$(BIN_DIR))
$(info ---------------------------------------------------------------------------)

.PHONY: sp2019
sp2019:

#tlbimp.exe 

	@if [ ! -f Common.snk ] ; then	\
		echo " Common.snk is not exist!";		\
	else											\
		echo " Common.snk is existed!";		\
	fi

	@if [ ! -f $(SDKWSRC) ] ; then	\
		echo "Error, Need to compile SDKWrapper.dll first!";				\
	else															\
		cp -f $(SDKWSRC) $(SDKWDST);\
	fi

	$(TLB) $(SDKWDST) $(MACH) $(KEY) $(SDKWLib)

	cp -f $(SDKWDST) $(SDKWCopy)
	
	cp -f SDKWrapperLib.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)
	
	@if [ ! -f SDKWrapperLib.dll ] ; then	\
		echo "Error, tlbimp SDKWrapper.dll failed!";		\
	fi


	@if [ ! -f $(QCAZSDKSRC) ] ; then	\
		echo "Error, Need to compile QueryCloudAZSDK.dll first!";				\
	else															\
		cp -f $(QCAZSDKSRC) $(QCAZSDK);		\
	fi

	$(ILDASMTOOL) $(QCAZSDK) $(ILDASMPARAM)$(ILASMIL)

	
	$(ILASMTOOL) $(ILASMIL) $(ILASMPARA)$(COMSNK) $(ILASMRESPARA)

	cp -f $(QCAZSDK) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)
	
	@if [ ! -f $(QCAZSDK) ] ; then	\
		echo "Error, copy QueryCloudAZSDK.dll failed!";		\
	fi


	@if [ ! -f $(PDFESRC) ] ; then	\
		echo "Error, Need to compile PdfEditor.dll first!";				\
	else															\
		cp -f $(PDFESRC) $(PDFEDST) ;		\
	fi
	$(TLB) $(PDFEDST) $(MACH) $(KEY) $(PDFELib)
	
	cp -f PdfEditorLib.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)
	
	@if [ ! -f PdfEditorLib.dll ] ; then	\
		echo "Error, tlbimp PdfEditorLib.dll failed!";		\
	fi
	
	@echo ""
	@echo "Building $(PROJECT_SP2019) ($(BUILDTYPE)) for NextLabs SharePoint Enforcer"
	rm -rf  $(BUILD_LOGFILE_SP2019)
	#regsvr32 "$(BUILDOUTPUTDIR)/../release_win_x86/SDKWrapper32.dll" /s
	$(MSVSIDE) $(PROJECT_SP2019) /build "$(PROJECT_CONFIG_SP2019)|AnyCPU" /out $(BUILD_LOGFILE_SP2019) ; \
	COMPILE_STATUS=$$? ;									\
	if [ -f $(BUILD_LOGFILE_SP2019) ] ; then				\
		echo "[[DUMP BEGIN - $(BUILD_LOGFILE_SP2019)]]" ;	\
		cat $(BUILD_LOGFILE_SP2019) ;						\
		echo "[[DUMP END - $(BUILD_LOGFILE_SP2019)]]" ;		\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE_SP2019)" ;	\
	fi ;													\
	exit $$COMPILE_STATUS
	@if [ $(OFFICIALCERT) -ne 0 ]; then										\
		echo $(SIGNTOOL_OFFICIAL) $(PROJECT_CONFIG_SP2019)/NextLabs.Common.dll ;	\
		$(SIGNTOOL_OFFICIAL) $(PROJECT_CONFIG_SP2019)/NextLabs.Common.dll ;	\
	fi
	@if [ ! -d $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019) ;						\
	fi
	cp -f $(PROJECT_CONFIG_SP2019)/Nextlabs.Common.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)
	cp -f $(PROJECT_CONFIG_SP2019)/Nextlabs.Common.pdb $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)
		
.PHONY: sp2016
sp2016:

#tlbimp.exe 

	@if [ ! -f Common.snk ] ; then	\
		echo " Common.snk is not exist!";		\
	else											\
		echo " Common.snk is existed!";		\
	fi

	@if [ ! -f $(SDKWSRC) ] ; then	\
		echo "Error, Need to compile SDKWrapper.dll first!";				\
	else															\
		cp -f $(SDKWSRC) $(SDKWDST) ;		\
	fi

	$(TLB) $(SDKWDST) $(MACH) $(KEY) $(SDKWLib)

	cp -f $(SDKWDST) $(SDKWCopy)

	cp -f SDKWrapperLib.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)
	
	@if [ ! -f SDKWrapperLib.dll ] ; then	\
		echo "Error, tlbimp SDKWrapper.dll failed!";		\
	fi

	@if [ ! -f $(QCAZSDKSRC) ] ; then	\
		echo "Error, Need to compile QueryCloudAZSDK.dll first!";				\
	else															\
		cp -f $(QCAZSDKSRC) $(QCAZSDK) ;		\
	fi

	$(ILDASMTOOL) $(QCAZSDK) $(ILDASMPARAM)$(ILASMIL)

	$(ILASMTOOL) $(ILASMIL) $(ILASMPARA)$(COMSNK) $(ILASMRESPARA)

	cp -f $(QCAZSDK) $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)
	

	@if [ ! -f $(QCAZSDK) ] ; then	\
		echo "Error, copy QueryCloudAZSDK.dll failed!";		\
	fi
	

	@if [ ! -f $(PDFESRC) ] ; then	\
		echo "Error, Need to compile PdfEditor.dll first!";				\
	else															\
		cp -f $(PDFESRC) $(PDFEDST) ;		\
	fi
	$(TLB) $(PDFEDST) $(MACH) $(KEY) $(PDFELib)
	
	cp -f PdfEditorLib.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)
	
	@if [ ! -f PdfEditorLib.dll ] ; then	\
		echo "Error, tlbimp PdfEditorLib.dll failed!";		\
	fi
	
	@echo ""
	@echo "Building $(PROJECT_SP2016) ($(BUILDTYPE)) for NextLabs SharePoint Enforcer"
	rm -rf  $(BUILD_LOGFILE_SP2016)
	#regsvr32 "$(BUILDOUTPUTDIR)/../release_win_x86/SDKWrapper32.dll" /s
	$(MSVSIDE) $(PROJECT_SP2016) /build "$(PROJECT_CONFIG_SP2016)|AnyCPU" /out $(BUILD_LOGFILE_SP2016) ; \
	COMPILE_STATUS=$$? ;									\
	if [ -f $(BUILD_LOGFILE_SP2016) ] ; then				\
		echo "[[DUMP BEGIN - $(BUILD_LOGFILE_SP2016)]]" ;	\
		cat $(BUILD_LOGFILE_SP2016) ;						\
		echo "[[DUMP END - $(BUILD_LOGFILE_SP2016)]]" ;		\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE_SP2016)" ;	\
	fi ;													\
	exit $$COMPILE_STATUS
	@if [ $(OFFICIALCERT) -ne 0 ]; then										\
		echo $(SIGNTOOL_OFFICIAL) $(PROJECT_CONFIG_SP2016)/NextLabs.Common.dll ;	\
		$(SIGNTOOL_OFFICIAL) $(PROJECT_CONFIG_SP2016)/NextLabs.Common.dll ;	\
	fi
	@if [ ! -d $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016) ]; then					\
		mkdir -p $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016) ;						\
	fi
	cp -f $(PROJECT_CONFIG_SP2016)/Nextlabs.Common.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)
	cp -f $(PROJECT_CONFIG_SP2016)/Nextlabs.Common.pdb $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)

.PHONY: clean
clean:
	@if [ -e ./Makefile.inc ]; then							\
		rm -rf  $(BUILD_LOGFILE_SP2019) \
		rm -rf  $(BUILD_LOGFILE_SP2016) \
		rm -rf release debug sp2019release sp2019debug sp2016release sp2016debug obj* *.suo *.ncb *.pdb $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)/NextLabs.Common.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2019)/NextLabs.Common.pdb $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)/NextLabs.Common.dll $(BUILDOUTPUTDIR)/$(PROJECT_CONFIG_SP2016)/NextLabs.Common.pdb;	\
	fi
