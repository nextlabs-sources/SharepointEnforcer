param($log = 'trace', $wspName)
if ((Get-PSSnapin "Microsoft.SharePoint.PowerShell" -ErrorAction SilentlyContinue) -eq $null) { 
    Add-PSSnapin "Microsoft.SharePoint.PowerShell" #must after param()
}
#############################################################################user can edit
$sharePointEnforcerInstallPath = 'C:\Program Files\Nextlabs\SharePoint Enforcer'
$sharePointRootFolder = 'C:\Program Files\Common Files\microsoft shared\Web Server Extensions\16'
$solutionName = 'nextlabs.deployment.wsp'
$logSymbol = ''
$allWebApplicationScope = 'AllWebApplications'


#aspx files need to restore
$aspxFilesUnderSharePointRootLayouts=@(
'viewlsts.aspx'
)

#############################################################################user can edit


#############################################################################user cannot edit
$xPathDir = '/configuration/system.webServer/modules'
$name = 'NextLabs.HttpEnforcer.HttpEnforcerModule'
$type = 'NextLabs.HttpEnforcer.HttpEnforcerModule, NextLabs.SPEnforcer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=5ef8e9c15bdfa43e'
$key = 'add'


$NextlabsEntitlementBasicFeatureId = 'ddf3439c-65aa-443b-8973-b87b003c0254'
$sharePointEnforcerInstallDir = $sharePointEnforcerInstallPath + '\'
$Bin = 'bin\'
$sharePointEnforcerInstallDirPath = $sharePointEnforcerInstallDir + $Bin
$sharePointLayOutsFolder = $sharePointRootFolder + '\TEMPLATE\LAYOUTS'

$SDKWrapperDll = 'SDKWrapper.dll'
$TagDocProtectorDll = 'TagDocProtector.dll'
$boostdatetimeDll = 'boost_date_time-vc140-mt-x64-1_67.dll'
$ceLogInterfaceDll = 'CE_Log_Interface.dll'
$jsoncppDll = 'jsoncpp.dll'
$LIBEAY32Dll = 'LIBEAY32.dll'
$policyEngineDll = 'policy_engine.dll'
$SSLEAY32Dll = 'SSLEAY32.dll'
$cscinvokeDll = 'NextLabs.CSCInvoke.dll'
$ceSPServiceDll = 'ceSPService.dll'
$DiagnosticDll = 'NextLabs.Diagnostic.dll'
$PLEDll = 'NextLabs.PLE.dll'
$SPEConfigModuleDll = 'Nextlabs.SPEConfigModule.dll'
$SPEnforcerDll = 'NextLabs.SPEnforcer.dll'
$SPSecurityTrimmingDll = 'Nextlabs.SPSecurityTrimming.dll'
$CommonDll = 'NextLabs.Common.dll'
$QueryCloudAZSDKDll = 'QueryCloudAZSDK.dll'
$DeploymentDll = 'NextLabs.DePloyment.dll'


$TagDocProtector = $sharePointEnforcerInstallDirPath + $TagDocProtectorDll

$SDKWrapper = $sharePointEnforcerInstallDirPath + $SDKWrapperDll






$regSharePointEnforcerPath = 'HKLM:\SoftWare\NextLabs\Compliant Enterprise\Sharepoint Enforcer'
$pathFeatureManager = '_layouts/15/FeatureManager/FeatureManager.aspx'
$pathFeatureController = '_layouts/15/FeatureManager/FeatureController.aspx'
$innerxmlData = '<system.web><httpRuntime maxRequestLength="2097151" executionTimeout="86400" /></system.web>'
$maxLength = '2097151'
$executionTime = '86400'
$sharePointConfigControllerValue = '<location path="{0}"><system.web><httpRuntime maxRequestLength="{1}" executionTimeout="{2}" /></system.web></location>' -f $pathFeatureController,$maxLength,$executionTime
$sharePointConfigManagerValue = '<location path="{0}"><system.web><httpRuntime maxRequestLength="{1}" executionTimeout="{2}" /></system.web></location>' -f $pathFeatureManager,$maxLength,$executionTime
$selectedPath = 'configuration'
$ownerFeatureController = 'NextLabs.FeatureController.aspx'
$ownerFeatureManager = 'NextLabs.FeatureManager.aspx'
$nameController = 'location[@path="{0}"]' -f $pathFeatureController
$nameManager = 'location[@path="{0}"]' -f $pathFeatureManager
$TagDocProtector = $sharePointEnforcerInstallDir + 'bin\TagDocProtector.dll'
$SDKWrapper = $sharePointEnforcerInstallDir + 'bin\SDKWrapper.dll'
$comUnRegister = 'regsvr32 /u /s '
$comRegister = 'regsvr32 /s '
$registerTagDocProtectorCommand = $comRegister + ('"{0}"' -f $TagDocProtector)
$unRegisterTagDocProtectorCommand = $comUnRegister + ('"{0}"' -f $TagDocProtector)
$registerSDKWrapperCommand = $comRegister + ('"{0}"' -f $SDKWrapper)
$unRegisterSDKWrapperCommand = $comUnRegister + ('"{0}"' -f $SDKWrapper)



$MapColor2Int= @{
'cyan' = 1
'magenta' = 2
'red' = 3
'green' = 4
'yellow' = 5
'white' = 6
}
$PrintLogLevel = @{
critical = 'cyan'
warn = 'magenta'
error = 'red'
info = 'green'
debug = 'yellow'
trace= 'white'
}

$Message = @{
WspExist = 'wsp already exist'
ItemChildsContainsItemNameExist = 'exist in: '
ItemChildsContainsItemNameNotExist = 'not exist in: '
CreateNxlItem = 'successful create:'
InvokeRemoteCommand = 'invoke-command '
UploadSPSolution = 'add-spslution and solution path:'
InstallSPSolutionSleep = 'after add wsp, will sleep for 5 s'
InstallSPSolution = 'after add wsp, will install wsp'
InstallSPSolutionNotDeployedYet = 'wsp has not be added yet'
WaitForSolutionNotInstalledYet = $solutionName +  ' has not been installed in farm'
WaitForSolutionNotInstalledComplete = $solutionName + ' has been installed in farm'
SolutionNotExist = 'we cannot find solution from : '
}
#############################################################################user cannot edit



#############################################################################functions definition
function IsLogLevelPrintOut($level){
    $bPrintOut = $false
	$colorLevelSetting = $PrintLogLevel[$log]
    $levelSettingInt = $MapColor2Int[$colorLevelSetting]
	$inputLevelInt = $MapColor2Int[$level]
	Try{
	    $levelSettingInt.gettype()
		$inputLevelInt.gettype()
	    if($levelSettingInt -ge $inputLevelInt){
	        $bPrintOut = $true
	    }
	}
	Catch{
		Write-Host $Error[0] -ForegroundColor red
		if($Error[0] -like $valueErrorReason){
		    Write-Host 'MapColor2Int Set Error' -ForegroundColor red
		}
	}
	return $bPrintOut
}

function PrintLog($loglevel, $Msg){
    $printMsg = $logSymbol + $Msg + $logSymbol
    if((IsLogLevelPrintOut -level $loglevel) -eq $true){
	    Write-Host $printMsg -ForegroundColor $loglevel
	}   
}
function IsFileExist($filePath){
    $b_fileExist = $false
	$Error.clear()
    Try{
	    $b_fileExist = test-path -path $filePath
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('test-path ' + $filePath+ ' success')
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsFileExist fail:' + $Error[0])
		$b_fileExist = $false
	}
	Finally{
	    $Error.clear()
	}
	return $b_fileExist
}

function Init{
    if([String]::IsNullOrEmpty($wspName)){
    }
    else{	
		$solutionName = $wspName
		$wspFileExtension = [System.IO.Path]::GetExtension($solutionName)
	    if($wspFileExtension -ne '.wsp'){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('your input wsp name:' + $wspName + ' is wrong format')
        }
	}
}
function GetCentralAdminUrl(){
	$spAdminUrl = $null
	$Error.clear()
	Try{
		$spAdminUrl = Get-SPWebApplication -includecentraladministration | where {$_.IsAdministrationWebApplication} | Select -ExpandProperty URL
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('Get Central Admin Url:' + $spAdminUrl)
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('Get Central Admin Url fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $spAdminUrl
}

function GetCentralAdminApp(){
	$centralAdminUrl = GetCentralAdminUrl
	$webAdminApp = $null
	$Error.clear()
	Try{
		if($centralAdminUrl -ne $null){
			$webAdminApp = Get-SPWebApplication $centralAdminUrl
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetCentralAdminApp fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $webAdminApp
}


function RemoveMutiInfoFromWebConfig($owner1, $owner2){
    $sharePointCentralAdminApp = GetCentralAdminApp
	$Error.clear()
	Try{
		$webModifications = $sharePointCentralAdminApp.WebConfigModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$webModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$config1 = $sharePointCentralAdminApp.WebConfigModifications | Where-Object {$_.Owner -eq $owner1}
		$config2 = $sharePointCentralAdminApp.WebConfigModifications | Where-Object {$_.Owner -eq $owner2}
		$result1 = $sharePointCentralAdminApp.WebConfigModifications.Remove($config1)
		$result2 = $sharePointCentralAdminApp.WebConfigModifications.Remove($config2)
		$sharePointCentralAdminApp.Update()
		$sharePointCentralAdminApp.Parent.ApplyWebConfigModifications()
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('remove {0} from CA web.config, and result is {1}; remove {2} from CA web.config, and result is {3}' -f $owner1, $result1, $owner2, $result2)
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$webModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('remove from CA web.config fail:{0}' -f $Error[0])
	}
	Finally{
		$Error.clear()
	}
}

function RemoveInfoFromWebConfig($ownerName){
    $sharePointCentralAdminApp = GetCentralAdminApp
	$Error.clear()
	Try{
		$webModifications = $sharePointCentralAdminApp.WebConfigModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$webModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$config = $sharePointCentralAdminApp.WebConfigModifications | Where-Object {$_.Owner -eq $ownerName}
		
		if($config -eq $null){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('deleted thing is null')
		}
		$result = $sharePointCentralAdminApp.WebConfigModifications.Remove($config)
		

		PrintLog -Loglevel $PrintLogLevel.info -Msg ('remove {0} from CA web.config, and result is:{1}' -f $ownerName, $result)
		$sharePointCentralAdminApp.Update()
		$sharePointCentralAdminApp.Parent.ApplyWebConfigModifications()
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
		$webModifications
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('remove {0} from CA web.config fail:{1}' -f $ownerName, $Error[0])
	}
	Finally{
		$Error.clear()
	}
}
function StartRegSvrProcess {
  param(
    [string]$computername,
    [string]$command
  )
  $result = $null
  $Error.clear()
  Try{
	$result = ([WMICLASS]"\\$computername\ROOT\CIMV2:win32_process").Create($command)
  }
  Catch{
  	PrintLog -Loglevel $PrintLogLevel.error -Msg ('StartRegSvrProcess fail:' + $Error[0])
  }
  Finally{
	$Error.clear()
	if($result -ne $null){
		if($result.ReturnValue -eq 0){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('success ' + $command + ' in computer:' + $computername + ', process id is:' + $result.ProcessId)
		}
		elseif($result.ReturnValue -eq 2){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', reason is: Access Denied')
		}
		elseif($result.ReturnValue -eq 3){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', reason is: Insufficient Privilege')
		}
		elseif($result.ReturnValue -eq 8){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', reason is: Unknown failure')
		}
		elseif($result.ReturnValue -eq 9){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', reason is: Path Not Found')
		}
		elseif($result.ReturnValue -eq 21){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', reason is: Invalid Parameter')
		}
	}
	else{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('fail ' + $command + ' in computer:' + $computername + ', return value is null')
	}
  }
}


function GetCAHostName(){
	$Error.clear()
	$CAHostMachineName = $null
	$centralAdminUrl = GetCentralAdminUrl
	if($centralAdminUrl -eq $null){
		return $centralAdminUrl
	}
	Try{
		[Uri]$sharePointCentralAdminUrl = $centralAdminUrl
		$hostMachineName = $sharePointCentralAdminUrl.Host.ToString()
		$CAHostMachineName = $hostMachineName
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('central admin host name is:{0}' -f $CAHostMachineName)
	}
	Catch{
	}
	Finally{
	}
	return $CAHostMachineName
}

function GetAdminWebConfigPath(){
	$webConfigPath = $null
	$Error.clear()
	Try{
		$webAdminApp = GetCentralAdminApp
		#default
		$zone = $webAdminApp.AlternateUrls[0].UrlZone 
		$iisSettings = $webAdminApp.IisSettings[$zone]
		$webConfigFilePath = $iisSettings.Path.ToString() + "\web.config"
		if([String]::IsNullOrEmpty($webConfigFilePath) -eq $false){
			$webConfigFileName = Split-Path $webConfigFilePath -leaf
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('web.config file name is:' + $webConfigFileName)
			if($webConfigFileName -eq 'web.config'){
				$webConfigPath = $webConfigFilePath
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config file path is:' + $webConfigPath)
			}
		}
	}
	Catch{
	}
	Finally{
		$Error.clear()
	}
	return $webConfigPath
}

function GetWebConfigPath($sharepointWebappUrl){
	$webConfigPath = $null
	$Error.clear()
	Try{
		$webapp = get-spwebapplication -id $sharepointWebappUrl
		$zone = $webapp.AlternateUrls[0].UrlZone
		$iisSettings = $webapp.IisSettings[$zone]
		$webConfigFilePath = $iisSettings.Path.ToString() + "\web.config"
		if([String]::IsNullOrEmpty($webConfigFilePath) -eq $false){
			$webConfigFileName = Split-Path $webConfigFilePath -leaf
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('web.config file name is:' + $webConfigFileName)
			if($webConfigFileName -eq 'web.config'){
				$webConfigPath = $webConfigFilePath
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config file path is:' + $webConfigPath)
			}
		}
	}
	Catch{
	}
	Finally{
		$Error.clear()
	}
	return $webConfigPath
}



function GetAdminWebConfigPathLocal(){
	$webConfigPath = $null
	$iisPrefix = 'IIS:\Sites\'
	$Error.clear()
	Try{
		$spAdminUrl = [Microsoft.SharePoint.Administration.SPAdministrationWebApplication]::Local.URL
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('sharpoint admin url is:' + $spAdminUrl)
		$webAdminApp = Get-SPWebApplication $spAdminUrl
		$psPathValue = $iisPrefix + $webAdminApp.DisplayName
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('sharpoint web admin app name is:' + $webAdminApp.DisplayName)
		$tmpWebconfigPathProperty = Get-WebConfigFile -PSPath $psPathValue
		$webconfigPathProperty = $tmpWebconfigPathProperty.pspath
		$tmpWebConfigPath = $webconfigPathProperty.Replace('::', '*')
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('web application app web.config path is:' + $tmpWebConfigPath)
		$arrayWebConfigPath = $tmpWebConfigPath.Split('*')
		$count = $arrayWebConfigPath.Count
		if($count -gt 1){
			$webConfigFilePath = $arrayWebConfigPath[-1]
			if([String]::IsNullOrEmpty($webConfigFilePath) -eq $false){
				$webConfigFileName = Split-Path $webConfigFilePath -leaf
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('web.config file name is:' + $webConfigFileName)
				if($webConfigFileName -eq 'web.config'){
					$webConfigPath = $webConfigFilePath
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config file path is:' + $webConfigPath)
				}
			}
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetAdminWebConfigPath fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $webConfigPath
}


function GetXmlDataFromWebConfig($webConfigPath){
	[xml]$xmlData = $null
	$Error.clear()
	Try{
		if(IsFileExist -filePath $webConfigPath){
			$xmlData = [xml](get-content $webConfigPath -Encoding UTF8)
			$xmlType = $xmlData.gettype()
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetXmlDataFromWebConfig fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $xmlData
}

function RemoveContentFromWebConfig($value, $xmlData, $webConfig){
	$Error.clear()
	Try{
		$path = "/configuration/location[@path='{0}']" -f $value
		write-host $path
		$node = $xmlData.SelectSingleNode($path)
		$r = $node.ParentNode.RemoveChild($node)
		$xmlData.Save($webConfig)
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveContentFromWebConfig fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}

}

function DuplicatesCount($xmlData, $value){
	[int]$count = 0
	$Error.clear()
	Try{
		$xmldata.configuration.location | ForEach-Object{ if($_.path  -eq $value) {$count = $count + 1} }
	}
	Catch{	
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('DuplicatesCount fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $count
} 



function ClearAddedContent($count, $content, $xmlDataContent, $webConfigPath){
	while($count -gt 0){
		RemoveContentFromWebConfig -value $content -xmlData $xmlDataContent -webConfig $webConfigPath
		$count = $count - 1
	}
}


function ClearAllAddedContent($xmldata, $pathFeatureController, $pathFeatureManager, $configFilePath){
	$Controllernums = DuplicatesCount -xmlData $xmldata -value $pathFeatureController
	$Managernums = DuplicatesCount -xmlData $xmldata -value $pathFeatureManager
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('controller num is:' + $Controllernums + ' ,manager num is:' + $Managernums)
	ClearAddedContent -count $Controllernums -content $pathFeatureController -xmlDataContent $xmldata -webConfigPath $configFilePath
	ClearAddedContent -count $Managernums -content $pathFeatureManager -xmlDataContent $xmldata -webConfigPath $configFilePath
}




function BeforeDeleteSolutionLocal($FeatureController, $FeatureManager){
	#get CA web.config path
	$configFilePath = GetAdminWebConfigPath
	#get web.config content of xml
	$xmldata = GetXmlDataFromWebConfig -webConfigPath $configFilePath
	#clear
	ClearAllAddedContent -xmldata $xmldata -pathFeatureController $FeatureController -pathFeatureManager $FeatureManager -configFilePath $configFilePath
}


function BeforeDeleteSolution($ownerController, $ownerManager){
	#RemoveInfoFromWebConfig -ownerName $ownerController
	#RemoveInfoFromWebConfig -ownerName $ownerManager
	RemoveMutiInfoFromWebConfig -owner1 $ownerController -owner2 $ownerManager
}

#delete registry
function IsWspExistInSharePoint{
	PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function IsWspExistInSharePoint'
	$IsExist = $false
	$Error.clear()
	Try{
		$spsolutions = get-spsolution
		foreach($s in $spsolutions){
			if($s.Name -eq $solutionName){
				$IsExist = $true
				break
			}
		}
	    if($IsExist -eq $false){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('wsp:' + $solutionName + ' does not exist in sharepoint farm')
			Exit
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsWspExistInSharePoint fail:' + $Error[0])
		Exit
	}
	Finally{
		$Error.clear()
	}
}
function IsWspDeployedToFalse{
    $result = $false
	$Error.clear()
	Try{
		$solutions = get-spsolution
		foreach($ss in $solutions){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ($ss.lastoperationdetails | Out-String)
			if(($ss.Name -eq $solutionName) -and ($ss.Deployed -eq $False) -and ($ss.JobExists -eq $false)){
				$result = $true
				break
			}
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsWspDeployedToFalse fail:' + $Error[0])
		Exit
	}
	Finally{
		$Error.clear()
	}
	return $result
}
function IsWspDeployedInSharePoint{
    $IsDeployed = $false
	$Error.clear()
	Try{
		$spsolutions = get-spsolution
		foreach($s in $spsolutions){
			if(($s.Name -eq $solutionName) -and ($s.Deployed -eq $true)){
				$IsDeployed = $true
				break
			}	
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsWspDeployedInSharePoint fail:' + $Error[0])
		Exit
	}
	Finally{
		$Error.clear()
	}
	return $IsDeployed
}

function UninstallSolution($slnName, $webApplicationScope){
	$Error.clear()
	Try{
		if($webApplicationScope -eq $allWebApplicationScope){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('uninstalling ' + $slnName + ' from sharepoint')
			Uninstall-SPSolution -identity nextlabs.deployment.wsp -AllWebApplications
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('UninstallSolution fail:' + $Error[0])
		Exit
	}
	Finally{
		$Error.clear()
	}
}

function AbleToUninstallSolution{
	$CanUninstallWsp = IsWspDeployedInSharePoint
	if($CanUninstallWsp -eq $true){
		UninstallSolution -slnName $solutionName -webApplicationScope $allWebApplicationScope
	}
}

function RemoveSolution($slnName){
	$Error.clear()
	Try{
		Remove-SPSolution -identity $slnName
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('remove-spslution:' + $slnName + ' from sharepoint success')
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('remove-spslution ' + $slnName + ' fail:' + $Error[0])
		Exit
	}
	Finally{
		$Error.clear()
	}
}

function WaitToRemoveSolution(){
	$deployed = $false
	while ($deployed -eq $false){
		PrintLog -Loglevel $PrintLogLevel.info -Msg 'after uninstall wsp, will sleep 5 s'
		sleep -s 5
		$deployed = IsWspDeployedToFalse
		if($deployed -eq $true){
			RemoveSolution -slnName $solutionName
		}
	}
}



function GetActivedWebapplications($solutionId, $featureId){
	$Error.clear()
	[string[]]$activedWebappUrls = @()
	Try{
		$s = get-spsolution -id $solutionId
		foreach($w in $s.deployedwebapplications){
			   if($w.Features[$featureId] -ne $null){
				$activedWebappUrls += $w.url
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('actived enforcement webapplication uri is:' + $w.url)
			}
		   }
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetActivedWebapplications fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return ,$activedWebappUrls
}




$DeleteContentFromCAWebConfig = {
	param($serverAddr, $centralAdminFilePath)
	$logSymbol = $using:logSymbol
	$MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
    $log = $using:log
	$solutionName = $using:solutionName
	$pathFeatureManager = $using:pathFeatureManager
	$pathFeatureController = $using:pathFeatureController
	$innerxmlData = $using:innerxmlData
	$maxLength = $using:maxLength
	$executionTime = $using:executionTime
	$sharePointConfigControllerValue = $using:sharePointConfigControllerValue
	$sharePointConfigManagerValue = $using:sharePointConfigManagerValue
	$selectedPath = $using:selectedPath
	$ownerFeatureController = $using:ownerFeatureController
	$ownerFeatureManager = $using:ownerFeatureManager
	$nameController = $using:nameController
	$nameManager = $using:nameManager
	function IsLogLevelPrintOut($level){
		$bPrintOut = $false
		$colorLevelSetting = $PrintLogLevel[$log]
		$levelSettingInt = $MapColor2Int[$colorLevelSetting]
		$inputLevelInt = $MapColor2Int[$level]
		Try{
			$levelSettingInt.gettype()
			$inputLevelInt.gettype()
			if($levelSettingInt -ge $inputLevelInt){
				$bPrintOut = $true
			}
		}
		Catch{
			Write-Host $Error[0] -ForegroundColor red
			if($Error[0] -like $valueErrorReason){
				Write-Host 'MapColor2Int Set Error' -ForegroundColor red
			}
		}
		return $bPrintOut
	}

	function PrintLog($loglevel, $Msg){
		$printMsg = $logSymbol + $Msg + $logSymbol
		if((IsLogLevelPrintOut -level $loglevel) -eq $true){
			Write-Host $printMsg -ForegroundColor $loglevel
		}   
	}
	

	
	
	
	
	function GetXmlDataFromWebConfig($webConfigPath){
		[xml]$xmlData = $null
		$Error.clear()
		Try{
			if(IsFileExist -filePath $webConfigPath){
				$xmlData = [xml](get-content $webConfigPath -Encoding UTF8)
				$xmlType = $xmlData.gettype()
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetXmlDataFromWebConfig fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
		return $xmlData
	}

	function AddContentToWebConfig($xmlData, $value, $innerxml, $webConfig){
		$Error.clear()
		Try{
			$newNode = $xmlData.CreateElement('location')
			$newNode.SetAttribute('path',$value)
			$newNode.InnerXml = $innerxml
			$xmlData.DocumentElement.AppendChild($newNode)
			$xmlData.Save($webConfig)
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('AddContentToWebConfig fail:' + $Error[0])
			Exit
		}
		Finally{
			$Error.clear()
		}
	}
	function RemoveContentFromWebConfig($value, $xmlData, $webConfig){
		$Error.clear()
		Try{
			$path = "/configuration/location[@path='{0}']" -f $value
			$node = $xmlData.SelectSingleNode($path)
			$r = $node.ParentNode.RemoveChild($node)
			$xmlData.Save($webConfig)
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveContentFromWebConfig fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}

	}

	function DuplicatesCount($xmlData, $value){
		[int]$count = 0
		$Error.clear()
		Try{
			$xmldata.configuration.location | ForEach-Object{ if($_.path  -eq $value) {$count = $count + 1} }
		}
		Catch{	
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('DuplicatesCount fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
		return $count
	} 

	function ClearAddedContent($count, $content, $xmlDataContent, $webConfigPath){
		while($count -gt 0){
			RemoveContentFromWebConfig -value $content -xmlData $xmlDataContent -webConfig $webConfigPath
			$count = $count - 1
		}
	}
	
	function ModifyNumberToString($num){
		[string]$value = $null
		if($num -lt 10){
			$value = '0' + $num.ToString()
		}else{
			$value = $num.ToString()
		}
		return $value
	}
	function GetWebConfigFileHashName{
		$webName = 'web_'
		$fileName = '.bak'
		$date = Get-Date
		$year = $date.Year
		$month = $date.Month
		$day = $date.Day
		$hour = $date.Hour
		$minute = $date.Minute
		$second = $date.Second
		$y = ModifyNumberToString -num $year
		$m = ModifyNumberToString -num $month
		$d = ModifyNumberToString -num $day
		$h = ModifyNumberToString -num $hour
		$min = ModifyNumberToString -num $minute
		$s = ModifyNumberToString -num $second
		$datehashcode = $webName + $y + '_' + $m + '_' + $d + '_' + $h + '_' + $min + '_' + $second + $fileName #web_2021_09_07_22_52_20.bak
	    PrintLog -Loglevel $PrintLogLevel.info -Msg ('date hashcode is:' + $datehashcode)
		return $datehashcode
    }
	function GetFolderPath($filePath){
		$folderPath = $null
		$Error.clear()
		Try{
			$fileProp = Get-Item $filePath
			$folderPath = $fileProp.Directory.FullName + '\'
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('get folder is:' + $folderPath)
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetFolderPath fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
		return $folderPath
	}
	function BackUpWebConfigFile($webConfigFilePath){
		$newCreatedFileName = GetWebConfigFileHashName
		$sourceFilePath = $webConfigFilePath
		$newCreatedFolder = GetFolderPath -filePath $sourceFilePath
		$newCreatedFilePath = $newCreatedFolder + $newCreatedFileName
		$Error.clear()
		Try{
			Copy-Item $sourceFilePath $newCreatedFilePath
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('BackUpWebConfigFile fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	function ClearAllAddedContent($xmldata, $pathFeatureController, $pathFeatureManager, $configFilePath){
		$Controllernums = DuplicatesCount -xmlData $xmldata -value $pathFeatureController
		$Managernums = DuplicatesCount -xmlData $xmldata -value $pathFeatureManager
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('controller num is:' + $Controllernums + ' ,manager num is:' + $Managernums)
		ClearAddedContent -count $Controllernums -content $pathFeatureController -xmlDataContent $xmldata -webConfigPath $configFilePath
		ClearAddedContent -count $Managernums -content $pathFeatureManager -xmlDataContent $xmldata -webConfigPath $configFilePath
}
	function IsFileExist($filePath){
		$b_fileExist = $false
		$Error.clear()
		Try{
			$b_fileExist = test-path -path $filePath
			#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('test-path ' + $filePath+ ' success')
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsFileExist fail:' + $Error[0])
			$b_fileExist = $false
		}
		Finally{
			$Error.clear()
		}
		return $b_fileExist
	}
	
	
	function EditWebConfig($FeatureController, $FeatureManager, $inXml, $configFilePath){
		$xmldata = GetXmlDataFromWebConfig -webConfigPath $configFilePath
		#clear
		ClearAllAddedContent -xmldata $xmldata -pathFeatureController $FeatureController -pathFeatureManager $FeatureManager -configFilePath $configFilePath
		#add
		AddContentToWebConfig -xmlData $xmldata -value $FeatureController -innerxml $inXml -webConfig $configFilePath
		AddContentToWebConfig -xmlData $xmldata -value $FeatureManager -innerxml $inXml -webConfig $configFilePath
	}
	
	function DeleteWebConfig($FeatureController, $FeatureManager, $configFilePath){
		#get web.config content of xml
		$xmldata = GetXmlDataFromWebConfig -webConfigPath $configFilePath
		#clear
		ClearAllAddedContent -xmldata $xmldata -pathFeatureController $FeatureController -pathFeatureManager $FeatureManager -configFilePath $configFilePath
	}
	
	
	$centralAdminHost = $null
	PrintLog -Loglevel $PrintLogLevel.info ('configFilPath is :' + $centralAdminFilePath)
	if(IsFileExist -filePath $centralAdminFilePath){
		$centralAdminHost = $serverAddr
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config exists in {0}' -f $centralAdminHost)
		DeleteWebConfig -FeatureController $pathFeatureController -FeatureManager $pathFeatureManager -configFilePath $centralAdminFilePath
	}
	else{
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config not exists in {0}' -f $serverAddr)
	}
};

#delete registry and restore aspx files
$RegDeleteSPEItemScript = {
	param($serverAddr)
	$regSharePointEnforcerPath = $using:regSharePointEnforcerPath
	$MapColor2Int = $using:MapColor2Int
	$PrintLogLevel = $using:PrintLogLevel
	$Message = $using:Message
	$log = $using:log
	$logSymbol = $using:logSymbol
	$aspxFilesUnderSharePointRootLayouts = $using:aspxFilesUnderSharePointRootLayouts
	$sharePointLayOutsFolder = $using:sharePointLayOutsFolder
	$sharePointEnforcerInstallPath = $using:sharePointEnforcerInstallPath
	$sharePointEnforcerInstallDirPath = $using:sharePointEnforcerInstallDirPath
	
	$SDKWrapperDll = $using:SDKWrapperDll
	$TagDocProtectorDll = $using:TagDocProtectorDll
	$boostdatetimeDll = $using:boostdatetimeDll
	$ceLogInterfaceDll = $using:ceLogInterfaceDll
	$jsoncppDll = $using:jsoncppDll
	$LIBEAY32Dll = $using:LIBEAY32Dll
	$policyEngineDll = $using:policyEngineDll
	$SSLEAY32Dll = $using:SSLEAY32Dll

	$sharePointEnforcerBinDllNames=@(
	$boostdatetimeDll,
	$ceLogInterfaceDll,
	$jsoncppDll,
	$LIBEAY32Dll,
	$policyEngineDll,
	$SSLEAY32Dll,
	$TagDocProtectorDll,
	$SDKWrapperDll
	)
	
	
	function IsLogLevelPrintOut($level){
		$bPrintOut = $false
		$colorLevelSetting = $PrintLogLevel[$log]
		$levelSettingInt = $MapColor2Int[$colorLevelSetting]
		$inputLevelInt = $MapColor2Int[$level]
		Try{
			$levelSettingInt.gettype()
			$inputLevelInt.gettype()
			if($levelSettingInt -ge $inputLevelInt){
				$bPrintOut = $true
			}
		}
		Catch{
			Write-Host $Error[0] -ForegroundColor red
			if($Error[0] -like $valueErrorReason){
				Write-Host 'MapColor2Int Set Error' -ForegroundColor red
			}
		}
		return $bPrintOut
	}

	function PrintLog($loglevel, $Msg){
		$printMsg = $logSymbol + $Msg + $logSymbol
		if((IsLogLevelPrintOut -level $loglevel) -eq $true){
			Write-Host $printMsg -ForegroundColor $loglevel
		}   
	}

	function IsFileExist($filePath){
		$b_fileExist = $false
		$Error.clear()
		Try{
			$b_fileExist = test-path -path $filePath
			#PrintLog -Loglevel $PrintLogLevel.trace -Msg ($filePath + ' exist?:' + $b_fileExist)
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsFileExist fail:' + $Error[0])
			$b_fileExist = $false
		}
		Finally{
			$Error.clear()
		}
		return $b_fileExist
	}
	function RemoveSPEFromRegistry($itemPath){
		$Error.clear()
		PrintLog -Loglevel $PrintLogLevel.trace -Msg 'enter function RemoveSPEFromRegistry'
		Try{
			remove-item -path $itemPath -Force
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('RemoveSPEFromRegistry :' + $serverAddr + ':' + $itemPath + ' success')
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveSPEFromRegistry :' + $serverAddr + ':' + $itemPath + ' fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	
	function GetDllUsedProcess($ModuleNameToFind, $needKill, $server, $ModulePath){
		$Error.clear()
		Try{
			foreach ($p in Get-Process){
				foreach ($m in $p.modules){
					if ( $m.FileName -match $ModuleNameToFind){
						PrintLog -Loglevel $PrintLogLevel.info -Msg ('{0} is used in server:{1} by process:{2}, process Id is:{3}' -f $m.FileName, $server, $p.Name, $p.id)
						if($needKill){
							Stop-Process -Id $p.Id -Force
							PrintLog -Loglevel $PrintLogLevel.info -Msg ('killing process:{0}, Id:{1}' -f $p.Name, $p.id)
						}
					}
				}
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetDllUsedProcess fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}

	#check dll being used by which programe and kill it
	function TestSPEEnvAboutDll($dllDirPath, [string[]]$dllNames, $hostName, $needCheckDllUsedByProcess, $needKillProcess){
		foreach($dll in $dllNames){
			$dllPath = $dllDirPath + $dll
			if(IsFileExist -filePath $dllPath){
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('dll:{0} exist in server:{1}' -f $dllPath, $hostName)
				if($needCheckDllUsedByProcess -eq $true){
					GetDllUsedProcess -ModuleNameToFind $dll -needKill $needKillProcess -server $hostName -ModulePath $dllPath
				}          
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('dll:{0} does not exist in server:{1}, donot care it' -f $dllPath, $hostName)
			}
		}
	}
	
	function DeleteFiles($source, $hostName){
		$Error.clear()
		Try{
			if(IsFileExist -filePath $source){
				Remove-Item $source -Recurse -force
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('delete files from:{0} in server:{1} success' -f $source, $hostName)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('delete files from:{0} in server:{1} fail:{2}' -f $source, $hostName, $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	
	
	function ModifyNumberToString($num){
		[string]$value = $null
		if($num -lt 10){
			$value = '0' + $num.ToString()
		}else{
			$value = $num.ToString()
		}
		return $value
	}
	function GetApsxFileHashName($aspxName){
		$newFileName = $aspxName.Replace('.', '_')
		$fileName = '.bak'
		$date = Get-Date
		$year = $date.Year
		$month = $date.Month
		$day = $date.Day
		$hour = $date.Hour
		$minute = $date.Minute
		$second = $date.Second
		$y = ModifyNumberToString -num $year
		$m = ModifyNumberToString -num $month
		$d = ModifyNumberToString -num $day
		$h = ModifyNumberToString -num $hour
		$min = ModifyNumberToString -num $minute
		$s = ModifyNumberToString -num $second
		#$datehashcode = $newFileName + '_' +$y + '_' + $m + '_' + $d + '_' + $h + '_' + $min + '_' + $second + $fileName #viewlsts_aspx_2021_09_07_22_52_20.bak
		$datehashcode = $newFileName + $fileName
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('aspx date hashcode is:' + $datehashcode)
		return $datehashcode
	}
	
	function RenameAspxFileName($oldAspxFileName, $folderPath, $serverName){
		$Error.clear()
		Try{
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function RenameAspxFileName')
			$aspxFileExtension = [System.IO.Path]::GetExtension($oldAspxFileName)
			if($aspxFileExtension -eq '.bak'){
				$aspxFileName = [System.IO.Path]::GetFileNameWithoutExtension($oldAspxFileName)
				$newAspxFileName = $aspxFileName.Replace('_', '.')
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('new aspx name is:{0}' -f $newAspxFileName)
				$oldAspxFilePath = $folderPath + '\' + $oldAspxFileName
				if(IsFileExist -filePath $oldAspxFilePath){
					rename-item $oldAspxFilePath -newname $newAspxFileName
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('success restore file:{0} from file:{1} in server:{2}' -f $newAspxFileName, $oldAspxFileName, $serverName)
				}
				else{
					PrintLog -loglevel $PrintLogLevel.error -Msg ('aspx file:{0} does not exist in path:{1} in server:{2}' -f $oldAspxFileName, $oldAspxFilePath, $serverName)
				}
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('{0} does not end with .bak, it ends with:{1}' -f $oldAspxFileName, $aspxFileExtension)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('RenameAspxFileName fail:{0} in server:{1}' -f $Error[0], $serverName)
		}
		Finally{
			$Error.clear()
		}
	}
	

	#XXX_aspx.bak -> XXX.aspx
	function RestoreAspxFiles($fileName, $folderPath, $hostName){
		RenameAspxFileName -oldAspxFileName $fileName -folderPath $folderPath -serverName $hostName
	}
	
	#delete registry about SharePoint Enforcer
	$b_speExistInRegistry = IsFileExist -filePath $regSharePointEnforcerPath
	if($b_speExistInRegistry){
	    PrintLog -Loglevel $PrintLogLevel.trace -Msg ($regSharePointEnforcerPath + 'exist, will operate delete action')
		RemoveSPEFromRegistry -itemPath $regSharePointEnforcerPath
	}
	else{
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ($regSharePointEnforcerPath + ' does not exist, do nothing')
	}
	
	#restore aspx files
	foreach($f in $aspxFilesUnderSharePointRootLayouts){
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('will restore {0}' -f $f)
		$inputHashName = GetApsxFileHashName -aspxName $f
		RestoreAspxFiles -fileName $inputHashName -folderPath $sharePointLayOutsFolder -hostName $serverAddr
	}

	
	
	#check bin dll is used or not, if used kill process
	TestSPEEnvAboutDll -dllDirPath $sharePointEnforcerInstallDirPath -dllNames $sharePointEnforcerBinDllNames -hostName $serverAddr -needCheckDllUsedByProcess $true -needKillProcess $true
	
	DeleteFiles -source $sharePointEnforcerInstallPath -hostName $serverAddr
};

function InvokeRemoteCommand($serverAddress, $excuteScript){
    PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function InvokeRemoteCommand, serverAddress:' + $serverAddress)
    $Error.clear()
	Try{
	    PrintLog -Loglevel $PrintLogLevel.trace -Msg ('beforce ' + $Message.InvokeRemoteCommand + $serverAddress)
	    $r = Invoke-Command -computerName $serverAddress -ScriptBlock $excuteScript -ArgumentList $serverAddress -ErrorAction Stop
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ($Message.InvokeRemoteCommand + $serverAddress + ' fail:' + $Error[0])
    }
	Finally{
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('after ' + $Message.InvokeRemoteCommand + $serverAddress)
		$Error.clear()
	}
}

function GetInvalidFarmMachines{
	$invalidServers = @()	
	$Error.clear()
	Try{
	    $servers = get-spserver -ErrorAction Stop
	    foreach($server in $servers){
	        #if($server.Role -ne 'Invalid'){
			if(($server.Role -ne 'Invalid') -and ($server.Role -ne 'Search')){
		        $invalidServers += $server.Address
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('getinvalid server:' + $server.Address)
		    }
	    }
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetInvalidFarmMachines fail:' + $Error[0])
	}
	Finally{
	    $Error.clear()
	}
	return $invalidServers
}

function IsSharePointFarmSingleServerFarm{
	$Error.clear()
	$bSingleServerFarm = $false
	Try{
		$serverInfo = [Microsoft.SharePoint.Administration.SPServer]::Local
		if($serverInfo.Role -eq 'SingleServerFarm'){
			$bSingleServerFarm = $true
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsSharePointFarmSingleServerFarm fails:{0}' -f $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $bSingleServerFarm
}

function excuteScriptOnRemoteFarmMachines($script){
    PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function excuteScriptOnRemoteFarmMachines'
    GetInvalidFarmMachines | ForEach-Object {InvokeRemoteCommand -serverAddress $_ -excuteScript $script}
}


$sptimerv4Restart = {
	restart-service sptimerv4
}

$iisResetScript = {
    param($serverAddr)
	$MapColor2Int = $using:MapColor2Int
	$PrintLogLevel = $using:PrintLogLevel
	$log = $using:log
	$logSymbol = $using:logSymbol
	function IsLogLevelPrintOut($level){
		$bPrintOut = $false
		$colorLevelSetting = $PrintLogLevel[$log]
		$levelSettingInt = $MapColor2Int[$colorLevelSetting]
		$inputLevelInt = $MapColor2Int[$level]
		Try{
			$levelSettingInt.gettype()
			$inputLevelInt.gettype()
			if($levelSettingInt -ge $inputLevelInt){
				$bPrintOut = $true
			}
		}
		Catch{
			Write-Host $Error[0] -ForegroundColor red
			if($Error[0] -like $valueErrorReason){
				Write-Host 'MapColor2Int Set Error' -ForegroundColor red
			}
		}
		return $bPrintOut
	}

	function PrintLog($loglevel, $Msg){
		$printMsg = $logSymbol + $Msg + $logSymbol
		if((IsLogLevelPrintOut -level $loglevel) -eq $true){
			Write-Host $printMsg -ForegroundColor $loglevel
		}   
	}
	$Error.clear()
	Try{
		iisreset
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('iisreset ' + $serverAddr + ' success')
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('iisreset ' + $serverAddr + ' fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}

};


$UnRegisterComDLLScript = {
	param($serverAddr)
	$logSymbol = $using:logSymbol
    $MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
    $log = $using:log
	$TagDocProtector = $using:TagDocProtector
	$SDKWrapper = $using:SDKWrapper
	function IsLogLevelPrintOut($level){
		$bPrintOut = $false
		$colorLevelSetting = $PrintLogLevel[$log]
		$levelSettingInt = $MapColor2Int[$colorLevelSetting]
		$inputLevelInt = $MapColor2Int[$level]
		Try{
			$levelSettingInt.gettype()
			$inputLevelInt.gettype()
			if($levelSettingInt -ge $inputLevelInt){
				$bPrintOut = $true
			}
		}
		Catch{
			Write-Host $Error[0] -ForegroundColor red
			if($Error[0] -like $valueErrorReason){
				Write-Host 'MapColor2Int Set Error' -ForegroundColor red
			}
		}
		return $bPrintOut
	}
	function PrintLog($loglevel, $Msg){
		$printMsg = $logSymbol + $Msg + $logSymbol
		if((IsLogLevelPrintOut -level $loglevel) -eq $true){
			Write-Host $printMsg -ForegroundColor $loglevel
		}   
	}
	function IsFileExist($filePath){
		$b_fileExist = $false
		$Error.clear()
		Try{
			$b_fileExist = test-path -path $filePath
			#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('test-path ' + $filePath+ ' success')
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsFileExist fail:' + $Error[0])
			$b_fileExist = $false
		}
		Finally{
			$Error.clear()
		}
		return $b_fileExist
	}

	function UnRegisterComDLL($dllPath, $serverName){
	    $Error.clear()
	    Try{
			if(IsFileExist -filePath $dllPath){
		        #$r = regsvr32 $dllPath #this not work
				&'regsvr32.exe' /u $dllPath
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('UnRegistering COM dll:{0} in server:{1}' -f $dllPath, $serverName)
		    }
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find COM dll:{0} in server:{1}' -f $dllPath, $serverName)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('UnRegister ComDLL fail:{0}, in server:{1}' -f $Error[0], $serverName)
		}
	}
	
	UnRegisterComDLL -dllPath $TagDocProtector -serverName $serverAddr
	UnRegisterComDLL -dllPath $SDKWrapper -serverName $serverAddr
};

$EditRemoteWebconfig = {
	param($serverAddr, $configFilePaths)
	$logSymbol = $using:logSymbol
	$log = $using:log
	$xPathDir = $using:xPathDir
	$name = $using:name
	$type = $using:type
	$key = $using:key
	$NextlabsEntitlementBasicFeatureId = $using:NextlabsEntitlementBasicFeatureId
	$MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
	
	function IsLogLevelPrintOut($level){
		$bPrintOut = $false
		$colorLevelSetting = $PrintLogLevel[$log]
		$levelSettingInt = $MapColor2Int[$colorLevelSetting]
		$inputLevelInt = $MapColor2Int[$level]
		Try{
			$levelSettingInt.gettype()
			$inputLevelInt.gettype()
			if($levelSettingInt -ge $inputLevelInt){
				$bPrintOut = $true
			}
		}
		Catch{
			Write-Host $Error[0] -ForegroundColor red
			if($Error[0] -like $valueErrorReason){
				Write-Host 'MapColor2Int Set Error' -ForegroundColor red
			}
		}
		return $bPrintOut
	}

	function PrintLog($loglevel, $Msg){
		$printMsg = $logSymbol + $Msg + $logSymbol
		if((IsLogLevelPrintOut -level $loglevel) -eq $true){
			Write-Host $printMsg -ForegroundColor $loglevel
		}   
	}

	function IsFileExist($filePath){
		$b_fileExist = $false
		$Error.clear()
		Try{
			$b_fileExist = test-path -path $filePath
			#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('test-path ' + $filePath+ ' success')
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('IsFileExist fail:' + $Error[0])
			$b_fileExist = $false
		}
		Finally{
			$Error.clear()
		}
		return $b_fileExist
	}
		
	function DuplicatesCount($xPathRoot, $xmlData, $value, $server){
		[int]$count = 0
		$xpath = '{0}/add' -f $xPathRoot
		
		$Error.clear()
		Try{
			$xmldata.SelectNodes($xpath) | ForEach-Object{ if($_.name  -eq $value) {$count = $count + 1} }
		}
		Catch{	
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('DuplicatesCount in server:{0} fail:{1}' -f $server, $Error[0])
		}
		Finally{
			$Error.clear()
		}
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('count is:{0}' -f $count)
		return $count
	}


	function GetXmlDataFromWebConfig($webConfigPath, $server){
		[xml]$xmlData = $null
		$Error.clear()
		Try{
			if(IsFileExist -filePath $webConfigPath){
				$xmlData = [xml](get-content $webConfigPath -Encoding UTF8)
				$xmlType = $xmlData.gettype()
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetXmlDataFromWebConfig in server:{0}, fail:{1}' -f $server, $Error[0])
		}
		Finally{
			$Error.clear()
		}
		return $xmlData
	}

	function RemoveContentFromWebConfig($xPathRoot, $value, $xmlData, $webConfig, $server){
		$xpath =  "{0}/add[@name='{1}']" -f $xPathRoot, $value
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('RemoveContentFromWebConfig xpath is:{0} in server:{1}' -f $xpath, $server)
		$Error.clear()
		Try{
			$node = $xmlData.SelectSingleNode($xpath) 
			if($node -ne $null){
				$r = $node.ParentNode.RemoveChild($node)
				$xmlData.Save($webConfig)
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveContentFromWebConfig, this node is null')
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveContentFromWebConfig fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	foreach($p in $configFilePaths){
		if(IsFileExist -filePath $p){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('get path from script:{0}' -f $p)
			$xmldata = GetXmlDataFromWebConfig -webConfigPath $p -server $serverAddr
			$count = DuplicatesCount -xPathRoot $xPathDir -xmlData $xmldata -value $name -server $serverAddr
			if($count -gt 0){
				RemoveContentFromWebConfig -xPathRoot $xPathDir -value $name -xmlData $xmldata -webConfig $p -server $serverAddr
			}
		}
		else{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('{0} does not exist in server:{1}' -f $p, $serverAddr)
		}
	}	
};

function MakeSureSPRelatedServiceRunning($serviceName){

	$servers = GetInvalidFarmMachines
	foreach($server in $servers){
		$service = get-service -computername $server $serviceName
		if($service -ne $null){
			if($service.Status -eq 'Running'){
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('service:{0} status is running in server:{1}' -f $serviceName, $server)
			}
			elseif($service.Status -eq 'Stopped'){
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('service:{0} status is stopped in server:{1}, will try to start service' -f $serviceName, $server)
				set-service -computername $server -status running -name $serviceName
			}
		}
		else{	
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not get service:{0} status in server:{1}' -f $serviceName, $server)
		}
	}
}

#############################################################################functions definition

#main
Init

$webconfigFilePath = GetAdminWebConfigPath

GetInvalidFarmMachines | ForEach-Object {Invoke-Command -computerName $_ -ScriptBlock $DeleteContentFromCAWebConfig -ArgumentList $_, $webconfigFilePath}

[string[]]$activedWebapps = GetActivedWebapplications -solutionId $solutionName -featureId $NextlabsEntitlementBasicFeatureId
[string[]]$activedWebconfigs = @()
foreach($webapp in $activedWebapps){
	$path = GetWebConfigPath -sharepointWebappUrl $webapp
	$activedWebconfigs += $path
}

PrintLog -Loglevel $PrintLogLevel.info -Msg ('activedWebconfigs count is:{0}, type is:{1}' -f $activedWebconfigs.count, $activedWebconfigs.gettype())


GetInvalidFarmMachines | ForEach-Object {Invoke-Command -computerName $_ -ScriptBlock $EditRemoteWebconfig -ArgumentList $_, $activedWebconfigs}


excuteScriptOnRemoteFarmMachines -script $iisResetScript #some dll will be loading by another program, so we need reset internet information service

excuteScriptOnRemoteFarmMachines -script $UnRegisterComDLLScript

#make sure SPAdminV4 service running
MakeSureSPRelatedServiceRunning -serviceName 'SPAdminV4'
#make sure SPTimerV4 service running
MakeSureSPRelatedServiceRunning -serviceName 'SPTimerV4'

IsWspExistInSharePoint
AbleToUninstallSolution
WaitToRemoveSolution
excuteScriptOnRemoteFarmMachines -script $RegDeleteSPEItemScript

#single sharepoint farm
if(IsSharePointFarmSingleServerFarm){

}

pause


