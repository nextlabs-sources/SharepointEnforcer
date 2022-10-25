VERSION_PRODUCT		= $(VERSION_PRODUCT_SPE)
VERSION_MAJOR		= $(VERSION_MAJOR_SPE)
VERSION_MINOR		= $(VERSION_MINOR_SPE)
VERSION_MAINTENANCE = $(VERSION_MAINTENANCE_SPE)


CSSUBTARGET = Nextlabs.Entitlement.Wizard
COMPILER=VS2013
# Define the objects of the target module
SRC = \
	CompletionControl.cs \
	CompletionControl.designer.cs \
	ExtendedTreeNode.cs \
	Resources\CommonUIStrings.Designer.cs \
	Resources\CommonUIStrings.en_US.Designer.cs \
	SiteCollectionDeploymentTargetsControl.cs \
	SiteCollectionDeploymentTargetsControl.designer.cs \
	FinishedControl.cs \
	FinishedControl.Designer.cs \
	DeploymentTargetsControl.cs \
	DeploymentTargetsControl.designer.cs \
	EULAControl.cs \
	EULAControl.designer.cs \
	ILog.cs \
	InstallConfiguration.cs \
	InstallerControl.cs \
	InstallerForm.cs \
	InstallerForm.designer.cs \
	InstallException.cs \
	InstallOptions.cs \
	InstallProcessControl.cs \
	InstallProcessControl.designer.cs \
	LogManager.cs \
	Program.cs \
	Properties\AssemblyInfo.cs \
	Properties\Resources.Designer.cs \
	Properties\Settings.Designer.cs \
	RepairControl.cs \
	RepairControl.Designer.cs \
	SystemCheckControl.cs \
	SystemCheckControl.Designer.cs \
	UpgradeControl.cs \
	UpgradeControl.Designer.cs \
	WelcomeControl.cs \
	WelcomeControl.Designer.cs \
	CommonUIStrings.Designer.cs \
	CommonUIStrings.en_US.Designer.cs


# Define the custom header include path
CSINCPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \

# Define the custom library path
CSLIBPATH = \
	$(NLBUILDROOT)/prod/pep/spe/build.output/dependencies \
	E:/bintool/SharePoint/12/Isapi

CSLIBS = \
	Microsoft.SharePoint.dll



