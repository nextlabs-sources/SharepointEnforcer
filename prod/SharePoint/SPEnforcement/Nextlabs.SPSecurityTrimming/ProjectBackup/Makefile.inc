
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


COMPILER=VS2013

CSSUBTARGET = Nextlabs.SPSecurityTrimming

# Define the objects of the target module
SRC+= \
 	AspMenuItemTrimmer.cs \
	AssemblyInfo.cs	\
	ControlEnumerator.cs \
	FeatureReceiver.cs \
	ITrimmer.cs \
	PageEventHook.cs \
	SmtGridViewRowTrimmer.cs \
	SMTreeViewNodeTrimmer.cs \
	SPListItemTrimmer.cs \
	SPListItemTrimmer2010.cs \
	SPSearchResultTrimmer.cs \
	SPSecurityTrimmingControl.cs \
	SPSecurityTrimmingModule.cs \
	SPSecurityTrimmingPage.cs \
	TreeViewNodeTrimmer.cs \
	ViewListPageTrimmer.cs \
	WebPartTrimmer.cs \
	SPSecurityTrimmingManager.cs \
	SPListTrimmingPage.cs



CSFLAGS+= /unsafe /define:TRACE

# Define the custom header include path
CSINCPATH+= \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi

# Define the custom library path
CSLIBPATH+= \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi \
	../../Nextlabs.Diagnostic/${LIBDIR} \
	../../NextLabs.Common/NextLabs.Common/${LIBDIR} \
	../../sync_handlers/EventHandlers/${LIBDIR}

CSLIBS+= \
	Microsoft.SharePoint.dll \
	Microsoft.SharePoint.Publishing.dll \
	Microsoft.Office.Server.dll \
	Microsoft.SharePoint.ApplicationPages.dll\
	Microsoft.Office.Server.Search.dll\
	NextLabs.CSCInvoke.dll \
	System.Data.dll	\
	log4net.dll \
	NextLabs.Diagnostic.dll \
	NextLabs.Common.dll \
	NextLabs.SPEnforcer.dll



