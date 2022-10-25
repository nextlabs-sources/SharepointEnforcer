
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


CSTARGET = CE_SPAdmin
COMPILER=VS2013
# Define the objects of the target module
SRC = \
 	AssemblyInfo.cs \
	ISpeAdminCommand.cs \
	SPEAdmin.cs \
	SPSecurityTrimmingCommand.cs \
	SearchResultTrimmingCommand.cs \
	SPECommand.cs \
	ContentAnalysisCommand.cs

CSFLAGS = /unsafe

# Define the custom header include path
CSINCPATH = \
	E:/bintool/SharePoint/12/Isapi/

# Define the custom library path
CSLIBPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	../../Nextlabs.SPSecurityTrimming/Nextlabs.SPSecurityTrimming/${LIBDIR}

CSLIBS = \
	Microsoft.SharePoint.dll NextLabs.CSCInvoke.dll System.Data.dll Nextlabs.SPSecurityTrimming.dll \
	Microsoft.Office.Server.dll Microsoft.Office.Server.Search.dll



