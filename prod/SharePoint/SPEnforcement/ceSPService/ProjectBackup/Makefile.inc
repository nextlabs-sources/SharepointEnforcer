
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)



COMPILER=VS2013

CSSUBTARGET = ceSPService

# Define the objects of the target module
SRC+= \
 	AssemblyInfo.cs \
	ceSPService.asmx.cs

CSFLAGS+= /define:TRACE

# Define the custom header include path
CSINCPATH+= \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \


# Define the custom library path
CSLIBPATH+= \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies

CSLIBS+= \
	Microsoft.SharePoint.dll Microsoft.Office.Server.dll \
	System.Data.dll



