
VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


COMPILER=VS2013

CSSUBTARGET = NextLabs.PLE

# Define the objects of the target module
SRC = \
 	AssemblyInfo.cs \
	NextLabs.PLE.Log.cs \
	NextLabs.PLE.Utils.cs \
	NextLabs.PLE.HttpModule.cs	\
	Nextlabs.PLE.PageFactory.cs \
	NextLabs.PLE.PageResource.cs \
	NextLabs.PLE.AdminPageLogs.cs




# Define the custom header include path
CSINCPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi

# Define the custom library path
CSLIBPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi \
	../../NextLabs.Common/NextLabs.Common/${LIBDIR} \
	../../Nextlabs.Diagnostic/${LIBDIR} \
	../../Nextlabs.SPEConfigModule/Nextlabs.SPEConfigModule/${LIBDIR} \
	../../sync_handlers/EventHandlers/${LIBDIR} \
	../../Nextlabs.Diagnostic/${LIBDIR}

CSLIBS = \
	Microsoft.SharePoint.dll NextLabs.CSCInvoke.dll System.Data.dll NextLabs.Diagnostic.dll NextLabs.SPEnforcer.dll NextLabs.Common.dll Nextlabs.SPEConfigModule.dll




