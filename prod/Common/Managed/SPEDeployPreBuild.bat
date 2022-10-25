@echo off

set OrgErrorLevel=%ErrorLevel%
echo Begin execute deploy prebuild event, backup OrgErrorLevel=%OrgErrorLevel%
set ErrorLevel=0

rem --------------Parameters-------------------------
rem ProjectType: EXE, DLL
set ProjectType=%~1
rem ProjectConfiguration: Release, Debug, SP2016Debug, SP2019Release
set ProjectConfiguration=%~2
rem CompileOutDir compile output folder path, like Release_AnyCpu\
set CompileOutDir=%~3

set ConfigurationName=%~4

set ProjectDir=%~5

echo ProjectDir is: %ProjectDir%

echo ConfigurationName is: %ConfigurationName%
set "sharepointVersion=sp"
set "SPConfig_NAME_LOWER=%ConfigurationName%"
for %%i in (a b c d e f g h i j k l m n o p q r s t u v w x y z) do call set SPConfig_NAME_LOWER=%%SPConfig_NAME_LOWER:%%i=%%i%%
echo after change from upper to lower, sharepoint configuration name is: %SPConfig_NAME_LOWER%


set "sharepointVersion2013=sp2013"
set "sharepointVersion2016=sp2016"
set "sharepointVersion2019=sp2019"

echo %SPConfig_NAME_LOWER% | findstr %sharepointVersion2013% >nul && (set "sharepointVersion=sp2013") || echo not sp2013
echo %SPConfig_NAME_LOWER% | findstr %sharepointVersion2016% >nul && (set "sharepointVersion=sp2016")|| echo not sp2016
echo %SPConfig_NAME_LOWER% | findstr %sharepointVersion2019% >nul && (set "sharepointVersion=sp2019") || echo not sp2019

echo get sharepoint configuration name is: %sharepointVersion%

set "NxlBuildRoot=%NLBUILDROOT%"
set "NextLabSBuildRoot=%NxlBuildRoot:/=\%"
echo NextLabSBuildRoot path is: %NextLabSBuildRoot%
set "configName=\configuration"
set "configurationPath=%NextLabSBuildRoot%%configName%"
echo configuration path is: %configurationPath%
set "ProjectSolutionBinName=\SharePointRoot.BIN"

set "policyControllerName=\PolicyControllerLibrary"
set "commonName=\CommonLibrary"
set "binaryName=\bin"
set "binaryName32=\bin32"
set "binaryName64=\bin64"
set "bindleName=\bundle.bin"



echo nextlabs.depolyment.wsp package referenced dll should be added into wsp !
set ProjectReleaseOrDebug=Release
echo %ProjectConfiguration% | findstr /i "Debug" > nul && set ProjectReleaseOrDebug=Debug

set ProductRoot=%NextLabSBuildRoot%
set ProductRootBin=%ProductRoot%\Bin
set PackagedGacFolder=%ProductRootBin%\PackegedGacDll
set BaseDotnetDir=%ProjectReleaseOrDebug%_Dotnet
set SP2016ReleaseOrDebugFolder=%ProductRootBin%\%BaseDotnetDir%\SP2016%ProjectReleaseOrDebug%
set SP2019ReleaseOrDebugFolder=%ProductRootBin%\%BaseDotnetDir%\SP2019%ProjectReleaseOrDebug%

set "CE_SPAdmin=CE_SPAdmin.exe"
set "IrmSettingTool=IrmSettingTool.exe"
set "SharepointConfigModifier=SharepointConfigModifier.exe"

set "SPEName=\SharePoint Enforcer"
set "ProjectSolutionResourcesName=\%sharepointVersion%\Resources"
set "ProjectSolutionImages=\SharePointRoot.TEMPLATE.IMAGES"
set "ProjectSolutionLayouts=\%sharepointVersion%\pages"


set "_Basic=NextLabs.Entitlement.Basic"
set "Basic=\%_Basic%"
set "_EventReceiver=NextLabs.Entitlement.EventReceiver"
set "EventReceiver=\%_EventReceiver%"

set "ProjectSolutionBINPath=%configurationPath%%ProjectSolutionBinName%"

set "ProjectSolutionSPEPath=%ProjectSolutionBINPath%%SPEName%"
set "ProjectSolutionCommonPath=%ProjectSolutionBINPath%%commonName%"
set "ProjectSolutionPCPath=%ProjectSolutionBINPath%%policyControllerName%"
set "ProjectSolutionBindlePath=%ProjectSolutionPCPath%%bindleName%"

set "ProjectSolutionPCBinPath=%ProjectSolutionPCPath%%binaryName%"
set "ProjectSolutionCommonBin32Path=%ProjectSolutionCommonPath%%binaryName32%"
set "ProjectSolutionCommonBin64Path=%ProjectSolutionCommonPath%%binaryName64%"





set "ProjectSolutionResourcesPath=%configurationPath%%ProjectSolutionResourcesName%"
set "ProjectSolutionImagesPath=%configurationPath%%ProjectSolutionImages%"
set "ProjectSolutionLayoutsPath=%configurationPath%%ProjectSolutionLayouts%"
set "ProjectBasicPath=%configurationPath%%Basic%"
set "ProjectEventReceiverPath=%configurationPath%%EventReceiver%"




set "DeploymentName=\prod\SharePoint\SPEDeploy\NextLabs.DeploymentEx\"
set "DeploymentDirPath=%NextLabSBuildRoot%%DeploymentName%"
set "DeploymentPath=%ProjectDir%"

echo Deployment path is: %DeploymentPath%

set "BinName=\BIN1"
set "ImagesName=\Images"
set "LayoutsName=Layouts"
set "ResourcesName=\Resources"
set "Resources1Name=\Resources1"
set "featuremanagerName=\featuremanager"






if %sharepointVersion% EQU sp2013 (
	echo sp2013 todo
rem todo
) else if %sharepointVersion% EQU sp2016 (
	echo make dir to restore sp2016 referenced dlls which to be added into wsp package
	if exist %SP2016ReleaseOrDebugFolder% (
		echo make sure that %PackagedGacFolder% exist if %SP2016ReleaseOrDebugFolder% exist
		if not exist %PackagedGacFolder% md %PackagedGacFolder%
		xcopy /s/e/y %SP2016ReleaseOrDebugFolder% %PackagedGacFolder%

		echo copy from %SP2016ReleaseOrDebugFolder%\%CE_SPAdmin% to %ProjectSolutionSPEPath%%binaryName%
		echo copy from %SP2016ReleaseOrDebugFolder%\%IrmSettingTool% to %ProjectSolutionSPEPath%%binaryName%
		echo copy from %SP2016ReleaseOrDebugFolder%\%SharepointConfigModifier% to %ProjectSolutionSPEPath%%binaryName%
		
		
		xcopy /y "%SP2016ReleaseOrDebugFolder%\%CE_SPAdmin%" "%ProjectSolutionSPEPath%%binaryName%"
		xcopy /y "%SP2016ReleaseOrDebugFolder%\%IrmSettingTool%" "%ProjectSolutionSPEPath%%binaryName%"
		xcopy /y "%SP2016ReleaseOrDebugFolder%\%SharepointConfigModifier%" "%ProjectSolutionSPEPath%%binaryName%"
	)

) else if %sharepointVersion% EQU sp2019 (
	echo make dir to restore sp2019 referenced dlls which to be added into wsp package
	if exist %SP2019ReleaseOrDebugFolder% (
		echo make sure that %PackagedGacFolder% exist if %SP2019ReleaseOrDebugFolder% exist
		if not exist %PackagedGacFolder% md %PackagedGacFolder%
		xcopy /s/e/y %SP2019ReleaseOrDebugFolder% %PackagedGacFolder% 
		
		echo copy from %SP2019ReleaseOrDebugFolder%\%CE_SPAdmin% to %ProjectSolutionSPEPath%%binaryName%
		echo copy from %SP2019ReleaseOrDebugFolder%\%IrmSettingTool% to %ProjectSolutionSPEPath%%binaryName%
		echo copy from %SP2019ReleaseOrDebugFolder%\%SharepointConfigModifier% to %ProjectSolutionSPEPath%%binaryName%
		
		xcopy /y "%SP2019ReleaseOrDebugFolder%\%CE_SPAdmin%" "%ProjectSolutionSPEPath%%binaryName%"
		xcopy /y "%SP2019ReleaseOrDebugFolder%\%IrmSettingTool%" "%ProjectSolutionSPEPath%%binaryName%"
		xcopy /y "%SP2019ReleaseOrDebugFolder%\%SharepointConfigModifier%" "%ProjectSolutionSPEPath%%binaryName%"
	)
) else (
	echo something erros, sharepoint version is %sharepointVersion%
)







echo SharePoint Enforcer source path is: %ProjectSolutionSPEPath%
echo SharePoint Enforcer dest path is: %DeploymentPath%%LayoutsName%%SPEName%
xcopy /s/i/e/q/y/f "%ProjectSolutionSPEPath%" "%DeploymentPath%%LayoutsName%%SPEName%"

echo Policy Controller bin source path is: %ProjectSolutionPCBinPath%
echo Policy Controller bin dest path is: %DeploymentPath%%LayoutsName%%policyControllerName%%binaryName%
xcopy /s/e/y %ProjectSolutionPCBinPath% %DeploymentPath%%LayoutsName%%policyControllerName%%binaryName%

echo Common bin32 source path is: %ProjectSolutionCommonBin32Path%
echo Common bin32 source dest is: %DeploymentPath%%LayoutsName%%commonName%%binaryName32%
xcopy /s/e/y %ProjectSolutionCommonBin32Path% %DeploymentPath%%LayoutsName%%commonName%%binaryName32%

echo Common bin64 source path is: %ProjectSolutionCommonBin64Path%
echo Common bin64 dest path is: %DeploymentPath%%LayoutsName%%commonName%%binaryName64%
xcopy /s/e/y %ProjectSolutionCommonBin64Path% %DeploymentPath%%LayoutsName%%commonName%%binaryName64%

echo bindle.bin source path is: %ProjectSolutionBindlePath%
echo bindle.bin dest path is: %DeploymentPath%%LayoutsName%%policyControllerName%%bindleName%
copy %ProjectSolutionBindlePath% %DeploymentPath%%LayoutsName%%policyControllerName%


xcopy /s/e/y %ProjectSolutionResourcesPath% %DeploymentPath%%ResourcesName%
xcopy /s/e/y %ProjectSolutionResourcesPath% %DeploymentPath%%Resources1Name%
xcopy /s/e/y %ProjectSolutionImagesPath% %DeploymentPath%%ImagesName%
xcopy /s/e/y %ProjectSolutionLayoutsPath% %DeploymentPath%%LayoutsName%
xcopy /s/i/e/q/y/f %ProjectBasicPath% %DeploymentPath%%LayoutsName%%featuremanagerName%%Basic%
xcopy /s/i/e/q/y/f %ProjectEventReceiverPath% %DeploymentPath%%LayoutsName%%featuremanagerName%%EventReceiver%




