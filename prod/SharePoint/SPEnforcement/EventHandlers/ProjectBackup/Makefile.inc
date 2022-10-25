
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


CSSUBTARGET = NextLabs.SPEnforcer
COMPILER=VS2013
# Define the objects of the target module
SRC = \
 	FeatureHandler.cs \
	ItemHandler.cs	\
	ListHandler.cs \
	Log.cs \
	NextLabs.HttpModule.cs \
	HttpSoapModule.cs \
	WebSiteHandler.cs \
	AssemblyInfo.cs \
	Global_Utils.cs \
	CALogViewerPage.cs \
	ContentAnalysisObligation.cs \
	ContentAnalysisPage.cs \
	FileContentAnalysis.cs \
	ListContentAnalysisWorker.cs \
	ListItemContentAnalysis.cs \
	CASchedule.cs \
	CASchedulePage.cs \
	UploadSyncObject.cs

# Define the custom header include path
CSINCPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \

# Define the custom library path
CSLIBPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi \
	../../Nextlabs.Diagnostic/${LIBDIR} \
	../../NextLabs.Common/NextLabs.Common/${LIBDIR}


CSLIBS = \
	Microsoft.SharePoint.dll NextLabs.CSCInvoke.dll System.Data.dll NextLabs.Diagnostic.dll NextLabs.Common.dll microsoft.sharepoint.portal.dll



