VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


CSSUBTARGET = NextLabs.Deployment
COMPILER=VS2013
# Define the objects of the target module
SRC = \
	CommonLib.cs \
	FeatureManagerPage.aspx.cs \
	Features\NextLabs.Entitlement.Basic\NextLabs.Entitlement.Basic.EventReceiver.cs \
	Properties\AssemblyInfo.cs \
	SearchResultTrimmingPage.aspx.cs \
	StatusReport.aspx.cs


# Define the custom header include path
CSINCPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \

# Define the custom library path
CSLIBPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi \
	../Nextlabs.Diagnostic/${LIBDIR} \
	../NextLabs.Common/NextLabs.Common/${LIBDIR} \
	../sync_handlers/EventHandlers/${LIBDIR} \
	../Nextlabs.SPSecurityTrimming/Nextlabs.SPSecurityTrimming/${LIBDIR}


CSLIBS = \
	Microsoft.SharePoint.dll Microsoft.Office.Server.Search.dll Microsoft.SharePoint.Security.dll\
	NextLabs.SPEnforcer.dll Nextlabs.SPSecurityTrimming.dll NextLabs.Diagnostic.dll NextLabs.Common.dll



