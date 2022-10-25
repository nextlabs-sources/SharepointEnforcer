
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


CSTARGET = IrmSettingTool
COMPILER=VS2013
# Define the objects of the target module
SRC = \
 	AssemblyInfo.cs \
	IrmSettingTool.cs	\
	NxtIrmListSettings.cs



# Define the custom header include path
CSINCPATH = \
	../../../../../prods/SPE/build.output/dependencies
# Define the custom library path
CSLIBPATH = \
	../../../../../prods/SPE/build.output/dependencies

CSLIBS = \
	Microsoft.SharePoint.dll System.Data.dll



