#############################################################################SPEC
# .\deploy.ps1 -log 'debug' -wspPath 'C:\your.wsp'
# .\deploy.ps1
# .\deploy.ps1 -log 'debug'
# .\deploy.ps1 -log 'trace' -option $false
#############################################################################SPEC 
param($log = 'trace', $wspPath, $option = $true, $jpc = $false, $help = $false) #option == $true means install-spsolution -allwebapplication, $false means install-solution -webapplication 'http://***'

$winserver2016detail = 'Microsoft Windows Server 2016'
$winserver2019detail = 'Microsoft Windows Server 2019'
$winserver2022detail = 'Microsoft Windows Server 2022'
$winserver2016 = 'win2016'
$winserver2019 = 'win2019'
$winserver2022 = 'win2022'
$winserverunknown = 'unknown'
$winversion= @{
	$winserver2016 = 1
	$winserver2019 = 2
	$winserver2022 = 3
	$winserverunknown = 4
}

function get-windowsversion{
	$osversion = $winversion[$winserverunknown]
	$Error.clear()
	Try{
		$s = Get-CimInstance -ClassName Win32_OperatingSystem | select Caption
		$serverName = $s.Caption
		if($serverName.StartsWith($winserver2016detail,'CurrentCultureIgnoreCase')){
			$osversion = $winversion[$winserver2016]
		}elseif($serverName.StartsWith($winserver2019detail,'CurrentCultureIgnoreCase')){
			$osversion = $winversion[$winserver2019]
		}elseif($serverName.StartsWith($winserver2022detail,'CurrentCultureIgnoreCase')){
			$osversion = $winversion[$winserver2022]
		}
	}
	Catch{
	}
	Finally{
		$Error.clear()
	}
	return $osversion
}

function is-win2022{
	$isWin2022 = $false
	$osv = get-windowsversion	 
	if($osv -eq $winversion[$winserver2022]){
		$isWin2022 = $true
	}
	return $isWin2022
}


if(is-win2022){
	Write-Host "in win2022, do not need Add-PSSnapin to execute sharepoint command" -ForegroundColor yellow
}else{
	if ((Get-PSSnapin "Microsoft.SharePoint.PowerShell" -ErrorAction SilentlyContinue) -eq $null) { 
		Add-PSSnapin "Microsoft.SharePoint.PowerShell" #must after param()
	}
}





#############################################################################user can edit
$enableJPCwithoutCEPC = $jpc
$sharePointRootFolder = 'C:\Program Files\Common Files\microsoft shared\Web Server Extensions\16'
$NextLabsPath = 'C:\Program Files\NextLabs'

$sharePointEnforcerInstallPath = $NextLabsPath + '\SharePoint Enforcer'
$sharePointEnforcerInstallDir = $sharePointEnforcerInstallPath + '\'

#aspx files need to be backup
$aspxFilesUnderSharePointRootLayouts=@(
'viewlsts.aspx'
)


$commonLibrary32Dlls=@(
'cesdk32.dll',
'RESATTRLIB32.DLL',
'celog32.dll',
'zlibwapi32.dll',
'nl_sysenc_lib32.dll',
'zlib1.DLL',
'freetype6.dll',
'PODOFOLIB.DLL',
'libtiff.DLL',
'pdflib32.dll',
'RESATTRMGR32.DLL'
)

$commonLibrary64Dlls=@(
'cesdk.dll',
'RESATTRLIB.DLL',
'celog.dll',
'zlibwapi.dll',
'nl_sysenc_lib.dll',
'PODOFOLIB.DLL',
'RESATTRMGR.DLL'
)


$policyControllerDlls=@(
'cebrain.dll',
'cecem.dll',
'ceconn.dll',
'ceeval.dll',
'celog.dll',
'cemarshal50.dll',
'cepepman.dll',
'cetransport.dll'
)

$policyControllerFiles=@(
'bundle.bin'
)

$CommonLibrary = 'CommonLibrary\'
$PolicyControllerLibrary = 'PolicyControllerLibrary\'
$CommonName = 'Common'
$PolicyControllerName = 'Policy Controller'


$solutionName = 'nextlabs.deployment.wsp'
$CustomDenyPageEnabled = 'false'                                                                                                       #true
$logTimeoutMs = '5000'
$PEPerfLogEnabled = 'true'                                                                                                            #true
$PolicyDefaultBehavior = 'Allow'                                                                                                       #Deny
$PolicyDefaultTimeout = '1388'
$PreFilterEnabled = 'false'
$ProductCode = '{C667C3F4-7FAF-43D6-ADCA-8E3E4E4C4AF8}'                                                                                #{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}
$ProductVersion = '2021.11.24'                                                                                                            #year.month
$SuppressEditPropertiesForm = 'true'                                                                                                   #false
$TamperResistanceConfigDir = 'config\tamper_resistance\'                                                                               #relative path
$logSymbol = ''
#############################################################################user can edit


#############################################################################user can't edit
#$sharePointMappedFolder = 'C:\Program Files\Common Files\microsoft shared\Web Server Extensions\16\TEMPLATE\LAYOUTS\SharePoint Enforcer'
$sharePointLayOutsFolder = $sharePointRootFolder + '\TEMPLATE\LAYOUTS'
$sharePointMappedFolder = $sharePointLayOutsFolder + '\SharePoint Enforcer'
$GacMsilDir = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\'
$comUnRegister = 'regsvr32 /u /s '
$comRegister = 'regsvr32 /s '
$Bin = 'bin\'
$Bin32 = 'bin32\'
$Bin64 = 'bin64\'

$sharePointEnforcerInstallDirPath = $sharePointEnforcerInstallDir + $Bin

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


$TagDocProtector = $sharePointEnforcerInstallDir + $Bin + $TagDocProtectorDll

$SDKWrapper = $sharePointEnforcerInstallDir + $Bin + $SDKWrapperDll


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

$sharePointEnforcerGacMsilDllNames=@(
$cscinvokeDll,
$ceSPServiceDll,
$DiagnosticDll,
$PLEDll,
$SPEConfigModuleDll,
$SPEnforcerDll,
$SPSecurityTrimmingDll,
$CommonDll,
$QueryCloudAZSDKDll,
$DeploymentDll
)




$registerTagDocProtectorCommand = $comRegister + ('"{0}"' -f $TagDocProtector)
$unRegisterTagDocProtectorCommand = $comUnRegister + ('"{0}"' -f $TagDocProtector)
$registerSDKWrapperCommand = $comRegister + ('"{0}"' -f $SDKWrapper)
$unRegisterSDKWrapperCommand = $comUnRegister + ('"{0}"' -f $SDKWrapper)
$currentdirpath = Get-Location
$solutionPath = $currentdirpath.Path + '\' + $solutionName
$solutionId = '6c15412b-290c-49ac-bd38-9b0ad852973b'


$clsid = 'CLSID\'
$regClassesRoot = 'ClassesRoot'
$SDKWrapperCLSID = '{5DB9F41D-6BDB-49C3-BBB4-20A7D83E92F3}'
$inprocServerName = '\InprocServer32'
$SDKWrapperCLSIDPath = $clsid + $SDKWrapperCLSID + $inprocServerName


$regDefaulValue = '(default)'
$regCLSIDPath = 'Registry::HKEY_CLASSES_ROOT\CLSID'
$regInprocServer32Name= 'InprocServer32'
$tagDocCLSID = '{6EC4BB1F-3F73-4799-BC98-A3DF9AE23A0B}'
$sdkWrapperCLSID = $SDKWrapperCLSID


$regSoftWareName = 'SoftWare'
$regNextLabsName = 'NextLabs'
$regCompliantEnterPriseName = 'Compliant Enterprise'
$regSharePointEnforcerName = 'SharePoint Enforcer'
$regCommonLibraryName = 'CommonLibraries'
$regPolicyControllerName = 'Policy Controller'




$regSoftWarePath = 'HKLM:\' + $regSoftWareName
$regNextLabsPath = $regSoftWarePath + '\' + $regNextLabsName
$regCompliantEnterPrisePath = $regNextLabsPath + '\' + $regCompliantEnterPriseName
$regSharePointEnforcerPath = $regCompliantEnterPrisePath + '\' + $regSharePointEnforcerName
$regCommonLibraryPath = $regNextLabsPath + '\' + $regCommonLibraryName
$regPolicyControllerPath = $regCompliantEnterPrisePath + '\' + $regPolicyControllerName





#'HKLM:\SoftWare\NextLabs\Compliant Enterprise\SharePoint Enforcer'
$SPERegPath = $regSharePointEnforcerPath
$regInstallDir = 'InstallDir'
$regPolicyControllerDir = 'PolicyControllerDir'




$CEPCInstallDir = 'C:\Program Files\NextLabs\Policy Controller\'
$valueErrorReason = 'You cannot call a method on a null-valued expression'
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
#############################################################################user can't edit






#############################################################################define log system
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
#############################################################################define log system




#############################################################################define value to be written into registry 
$RegistryValueDictionary = @{
'CustomDenyPageEnabled' = $CustomDenyPageEnabled
'InstallDir' = $sharePointEnforcerInstallDir
'logTimeoutMs' = $logTimeoutMs
'PEPerfLogEnabled' = $PEPerfLogEnabled
'PolicyDefaultBehavior' = $PolicyDefaultBehavior
'PolicyDefaultTimeout(ms)' = $PolicyDefaultTimeout
'PreFilterEnabled' = $PreFilterEnabled
'ProductCode' = $ProductCode
'ProductVersion' = $ProductVersion
'SuppressEditPropertiesForm' = $SuppressEditPropertiesForm
'TamperResistanceConfigDir' = $CEPCInstallDir + $TamperResistanceConfigDir
}

$RegistryCommonLibraryDictionary = @{
'InstallDir' = $NextLabsPath + '\' + $CommonName + '\'
}

$RegistryPolicyControllerDictionary = @{
'InstallDir' = $NextLabsPath + '\'
'PolicyControllerDir' = $NextLabsPath + '\' + $PolicyControllerName + '\'

}


#############################################################################define value to be written into registry 



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



function InitSolutionPath($solution, $wsp){
	$bMatch = $true
	if([String]::IsNullOrEmpty($wsp)){
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('input wspPath is empty, so we use current folder wsp:{0}' -f $solution)
		if((IsFileExist -filePath $solution) -eq $false){
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('in current folder {0}, we can not find wsp' -f $solution)
			return $null
		}
		else{
			return $solution
		}
	}
	else{
		$wspFileName = Split-Path $wsp -leaf	
		$wspFileExtension = [System.IO.Path]::GetExtension($wspFileName)
	    if($wspFileExtension -eq '.wsp'){
			$m = '^\\[^\\].*$'
			if($wsp -match $m){
				$wspRelativePath = $wsp
				$p = pwd
				$currentFolder = $p.Path
				$wsp = $currentFolder + $wspRelativePath
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('get relative wsp path:{0}, absolute wsp path:{1}' -f $wspRelativePath, $wsp)
			}
			else{
				$bMatch = $false
			}
		
	        if((IsFileExist -filePath $wsp) -eq $false){
				if($bMatch -eq $false){
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('{0} can not match {1}' -f $wsp, $m)
				}
		        PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find wsp :{0} by input wspPath' -f $wsp)
				return $null
	        }
			else{
			    PrintLog -Loglevel $PrintLogLevel.info -Msg ('get wsp path: {0} from input wspPath' -f $wsp)
				return $wsp
			}
		}
		else{
		    PrintLog -Loglevel $PrintLogLevel.error -Msg ('there is not file name is wsp, and error wspPath is:{0}' -f $wsp)
			return $null
		}
	}
}


#global variable $wspPath and $solutionPath, we need initialize solution path
function Init{	
    if([String]::IsNullOrEmpty($wspPath)){
        $wspPath = $solutionPath
	    if((IsFileExist -filePath $wspPath) -eq $false){
		    PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find:{0} by wspPath' -f $wspPath)
	        Exit
	    }
		else{
		    PrintLog -Loglevel $PrintLogLevel.info -Msg ('wsp path is:' + $wspPath)
		}
    }
    else{	
		$wspFileName = Split-Path $wspPath -leaf	
		$wspFileExtension = [System.IO.Path]::GetExtension($wspFileName)
	    if($wspFileExtension -eq '.wsp'){
		    $global:solutionPath = $wspPath
	        if((IsFileExist -filePath $global:solutionPath) -eq $false){
		        PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find:{0} by solutionPAth' -f $global:solutionPath)
	            Exit
	        }
			else{
			    PrintLog -Loglevel $PrintLogLevel.info -Msg ('solution path is:' + $global:solutionPath)
			}
		}
		else{
		    PrintLog -Loglevel $PrintLogLevel.error -Msg ('there is not file name is wsp, and wspPath is:{0}' -f $wspPath)
		    Exit
		}
    }
}
$sptimerv4Restart = {
	restart-service sptimerv4
}
function GetCentralAdminUrl(){
	$spAdminUrl = $null
	$Error.clear()
	Try{
		$spAdminUrl = Get-SPWebApplication -includecentraladministration | where {$_.IsAdministrationWebApplication} | Select -ExpandProperty URL
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('Get Central Admin Url:' + $spAdminUrl)
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



function AddInfoToCAWebConfig($controllerValue, $managerValue, $path, $ownerController, $ownerManager, $managerName, $controllerName){
	$sharePointCentralAdminApp = GetCentralAdminApp
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
	$sharePointCentralAdminApp.WebConfigModifications
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('---------------------------------------------------------------------------------------------------')
	$sharePointCentralAdminApp.WebConfigModifications.Clear()
	$Error.clear()
    Try{
		#FeatureController
		$configMod1 = New-Object Microsoft.SharePoint.Administration.SPWebConfigModification
		$configMod1.Name = $controllerName
		$configMod1.Owner = $ownerController
		$configMod1.Type = 0
		$configMod1.Path = $path
		$configMod1.Sequence = 0
		$configMod1.Value = $controllerValue
		$result = $sharePointCentralAdminApp.WebConfigModifications.Add($configMod1)

		#FeatureManager
		$configMod2 = New-Object Microsoft.SharePoint.Administration.SPWebConfigModification
		$configMod2.Name = $managerName
		$configMod2.Owner = $ownerManager
		$configMod2.Type = 0
		$configMod2.Path = $path
		$configMod2.Sequence = 0
		$configMod2.Value = $managerValue
		$webModifications = $sharePointCentralAdminApp.WebConfigModifications
		$result = $sharePointCentralAdminApp.WebConfigModifications.Add($configMod2)

		#update and push modifications
		$sharePointCentralAdminApp.Update()
		$sharePointCentralAdminApp.Parent.ApplyWebConfigModifications()
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('AddInfoToCAWebConfig fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
}

function GetWebApplicationUrls{
	$Error.clear()
	$urls = @()
	Try{
		$webapp = get-spwebapplication
		foreach($i in $webapp.Url){
			$urls += $i
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetWebApplicationUrls fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return ,$urls
}

function GetLocalServerName{
	#$serverName = [System.Net.Dns]::GetHostName()
	$serverName = $env:COMPUTERNAME
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('current server name is:' + $serverName)
	return $serverName
}

function GetCentralAdminServerName{
	$Error.clear()
	Try{
		$localAdmin = [Microsoft.SharePoint.Administration.SPWebServiceInstance]::LocalAdministration
		#if($localAdmin.TypeName -eq 'Central Administration') -and ($localAdmin.Status -eq 'Online'){
		#}
	}
	Catch{
	}
	Finally{
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
	PrintLog -Loglevel $PrintLogLevel.trace -Msg ('date hashcode is:' + $datehashcode)
	return $datehashcode
}



function GetAdminWebConfigPath(){
	$webConfigPath = $null
	$Error.clear()
	Try{
		$spAdminUrl = Get-SPWebApplication -includecentraladministration | where {$_.IsAdministrationWebApplication} | Select -ExpandProperty URL
		$webAdminApp = Get-SPWebApplication $spAdminUrl
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


#web.config and SPEdeploy.ps1 are in one same computer
function GetAdminWebConfigPathLocalMachine(){
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

function AddContentToWebConfig($xmlData, $value, $innerxml, $webConfig){
	$Error.clear()
	Try{
		$newNode = $xmlData.CreateElement('location')
		$newNode.SetAttribute('path',$value)
		$newNode.InnerXml = $innerxml
		$r = $xmlData.DocumentElement.AppendChild($newNode)
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

function GetFolderPath($filePath){
	$folderPath = $null
	$Error.clear()
	Try{
		$fileProp = Get-Item $filePath
		$folderPath = $fileProp.Directory.FullName + '\'
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('get folder is:' + $folderPath)
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetFolderPath fail:' + $Error[0])
	}
	Finally{
		$Error.clear()
	}
	return $folderPath
}

function BackUpWebConfigFile(){
	$newCreatedFileName = GetWebConfigFileHashName
	$sourceFilePath = GetAdminWebConfigPath
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
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('controller num is:' + $Controllernums + ' ,manager num is:' + $Managernums)
	ClearAddedContent -count $Controllernums -content $pathFeatureController -xmlDataContent $xmldata -webConfigPath $configFilePath
	ClearAddedContent -count $Managernums -content $pathFeatureManager -xmlDataContent $xmldata -webConfigPath $configFilePath
}


function BeforeDeploySolutionLocal($FeatureController, $FeatureManager, $inXml){
	#get CA web.config path
	$configFilePath = GetAdminWebConfigPath
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('CA web.config file path is:' + $configFilePath)
	#backup CA web.config
	BackUpWebConfigFile
	#get web.config content of xml
	$xmldata = GetXmlDataFromWebConfig -webConfigPath $configFilePath
	#clear
	ClearAllAddedContent -xmldata $xmldata -pathFeatureController $FeatureController -pathFeatureManager $FeatureManager -configFilePath $configFilePath
	#add
	AddContentToWebConfig -xmlData $xmldata -value $FeatureController -innerxml $inXml -webConfig $configFilePath
	AddContentToWebConfig -xmlData $xmldata -value $FeatureManager -innerxml $inXml -webConfig $configFilePath
}

function BeforeDeleteSolutionLocal($FeatureController, $FeatureManager){
	#get CA web.config path
	$configFilePath = GetAdminWebConfigPath
	#get web.config content of xml
	$xmldata = GetXmlDataFromWebConfig -webConfigPath $configFilePath
	#clear
	ClearAllAddedContent -xmldata $xmldata -pathFeatureController $FeatureController -pathFeatureManager $FeatureManager -configFilePath $configFilePath
}


function BeforeDeploySolution($spControllerValue, $spManagerValue, $pathLocation, $controllerOwnerName, $managerOwnerName, $featureControllerName, $featureManagerName){
    AddInfoToCAWebConfig -controllerValue $spControllerValue -managerValue $spManagerValue -path $pathLocation -ownerController $controllerOwnerName -ownerManager $managerOwnerName -managerName $featureManagerName -controllerName $featureControllerName
}
function BeforeDeleteSolution($controllerOwnerName, $managerOwnerName){
	RemoveInfoFromWebConfig -owenerName $controllerOwnerName
	RemoveInfoFromWebConfig -owenerName $managerOwnerName
}


$RegistSPEScript = { #start RegistSPEScript

    param($serverAddr)

    #in script, cannot access local variable unless use 'using'
	$enableJPCwithoutCEPC = $using:enableJPCwithoutCEPC
	$logSymbol = $using:logSymbol
    $regSoftWareName = $using:regSoftWareName
    $regNextLabsName = $using:regNextLabsName
    $regCompliantEnterPriseName = $using:regCompliantEnterPriseName
    $regSharePointEnforcerName = $using:regSharePointEnforcerName
	$regCommonLibraryName = $using:regCommonLibraryName
	$regPolicyControllerName = $using:regPolicyControllerName
    $regSoftWarePath = $using:regSoftWarePath
    $regNextLabsPath = $using:regNextLabsPath
    $regCompliantEnterPrisePath = $using:regCompliantEnterPrisePath
    $regSharePointEnforcerPath = $using:regSharePointEnforcerPath
	$regCommonLibraryPath = $using:regCommonLibraryPath
	$regPolicyControllerPath = $using:regPolicyControllerPath
	
    $SPERegPath = $using:SPERegPath
    $CEPCInstallDir = $using:CEPCInstallDir
    $valueErrorReason = $using:valueErrorReason
    $MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
    $log = $using:log
    $RegistryValueDictionary = $using:RegistryValueDictionary

	$RegistryCommonLibraryDictionary = $using:RegistryCommonLibraryDictionary
	$RegistryPolicyControllerDictionary = $using:RegistryPolicyControllerDictionary
	
	$GacMsilDir = $using:GacMsilDir
	$sharePointEnforcerInstallDirPath = $using:sharePointEnforcerInstallDirPath
	$SDKWrapperDll = $using:SDKWrapperDll
	$TagDocProtectorDll = $using:TagDocProtectorDll
	$boostdatetimeDll = $using:boostdatetimeDll
	$ceLogInterfaceDll = $using:ceLogInterfaceDll
	$jsoncppDll = $using:jsoncppDll
	$LIBEAY32Dll = $using:LIBEAY32Dll
	$policyEngineDll = $using:policyEngineDll
	$SSLEAY32Dll = $using:SSLEAY32Dll
	$cscinvokeDll = $using:cscinvokeDll
	$ceSPServiceDll = $using:ceSPServiceDll
	$DiagnosticDll = $using:DiagnosticDll
	$PLEDll = $using:PLEDll
	$SPEConfigModuleDll = $using:SPEConfigModuleDll
	$SPEnforcerDll = $using:SPEnforcerDll
	$SPSecurityTrimmingDll = $using:SPSecurityTrimmingDll
	$CommonDll = $using:CommonDll
	$QueryCloudAZSDKDll = $using:QueryCloudAZSDKDll
	$DeploymentDll = $using:DeploymentDll

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

	$sharePointEnforcerGacMsilDllNames=@(
	$cscinvokeDll,
	$ceSPServiceDll,
	$DiagnosticDll,
	$PLEDll,
	$SPEConfigModuleDll,
	$SPEnforcerDll,
	$SPSecurityTrimmingDll,
	$CommonDll,
	$QueryCloudAZSDKDll,
	$DeploymentDll
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
	
	function GetSpecificRegValueByPathAndName($path, $name){
		$value = $null
		Try{
			$value = get-itempropertyvalue -path $path -name $name
		}
		Catch{
			#PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetSpecificRegValueByPathAndName fail:' + $Error[0])
		}
		Finally{
			$Error.Clear()
		}
		return $value
	}
	
    function SetNxlItemproperty($ItemPath, $ItemName, $ItemType, $ItemValue){
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function SetNxlItemproperty'
        $Error.clear()
        set-itemproperty -path $ItemPath -name $ItemName -Type $ItemType -value $ItemValue -ErrorAction SilentlyContinue    #[Microsoft.Win32.RegistryValueKind]::DWORD
        if($?){
	        PrintLog -Loglevel $PrintLogLevel.info -Msg ('set registry value ' + $serverAddr + ':' + $ItemPath + '\' + $ItemName + ' success')
        }
        else{
	        PrintLog -Loglevel $PrintLogLevel.error -Msg ('set registry value ' + $serverAddr + ':' + $ItemPath + '\' + $ItemName + ' fail:' + $Error[0])
        }
        $Error.clear()
    }
    function CreateNxlItem($Type, $ItemPath){
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function CreateNxlItem, itempath:' + $ItemPath)
        $Error.clear() 
        $result = $true
        Try{
			if($Type -eq 'String'){
				$r = New-Item -itemType String $ItemPath -ErrorAction Stop    			    #$r = New-Item -itemType String -path $ItemPath -ErrorAction Stop
				PrintLog -Loglevel $PrintLogLevel.debug -Msg ('Create, Path:' + $ItemPath + ' success')
			}
        }
        Catch{
	        PrintLog -Loglevel $PrintLogLevel.error -Msg ($Message.CreateNxlItem + $ItemPath + ':' + $Error[0])
	        $result = $false
        }
        Finally{
	        $Error.clear()
        }
        return $result
    }

    function GetNxlChildItemNames($regiditItempath){
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function GetNxlChildItemNames, regiditItempath:' + $regiditItempath)
        $b_getChild = $true 
        $Error.clear()
        Try{
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('Get-ChildItem -path ' + $regiditItempath +  ' -name')
	        Get-ChildItem -path $regiditItempath -name #return value0
        }
        Catch{
	        $b_getChild = $false
	        PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetNxlChildItemNames fail:' + $Error[0])
        }
        Finally{
	        $Error.clear()
        }
        return $b_getChild                      #return value1
    }
	
	function IsItemChildsContainsItemName($ItemName, $ItemDirPath){
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function IsItemChildsContainsItemName, ItemName:' + $ItemName + ' ItemDirPath:' + $ItemDirPath)
	    $getchildResults = GetNxlChildItemNames -regiditItempath $ItemDirPath
	    if($getchildResults[-1] -eq $true){
		    $ItemChilds = $getchildResults[0..($getchildResults.Count - 2)]
		    if($ItemChilds -contains $ItemName){
	            PrintLog -Loglevel $PrintLogLevel.trace -Msg ($ItemName + $Message.ItemChildsContainsItemNameExist + $ItemDirPath)
	            return $true
	        }
	        else{
	            PrintLog -Loglevel $PrintLogLevel.trace -Msg ($ItemName + $Message.ItemChildsContainsItemNameNotExist + $ItemDirPath)
	            return $false
	        }
	    }
    }
	
    function WriteSharePointEnforcerPathInfoToRemoteFarmRegistry{
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WriteSharePointEnforcerPathInfoToRemoteFarmRegistry'
        if((IsItemChildsContainsItemName -ItemName $regNextLabsName -ItemDirPath $regSoftWarePath) -eq $false){
	        CreateNxlItem -Type 'String' -ItemPath $regNextLabsPath 
	    }
	    if((IsItemChildsContainsItemName -ItemName $regCompliantEnterPriseName -ItemDirPath $regNextLabsPath) -eq $false){
	        CreateNxlItem -Type 'String' -ItemPath $regCompliantEnterPrisePath
	    }
	    if((IsItemChildsContainsItemName -ItemName $regSharePointEnforcerName -ItemDirPath $regCompliantEnterPrisePath) -eq $false){
	        CreateNxlItem -Type 'String' -ItemPath $regSharePointEnforcerPath
	    }
    }

    function WriteSharePointEnforcerInstallInformationToRemoteFarmRegistry{
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WriteSharePointEnforcerInstallInformationToRemoteFarmRegistry'
		$Error.clear()
		Try{
		    $RegistryValueDictionary.keys | ForEach-Object{
				if($_ -eq 'PolicyDefaultTimeout(ms)'){
					SetNxlItemproperty -ItemPath $SPERegPath -ItemName $_ -ItemType DWord -ItemValue $RegistryValueDictionary[$_]
				}
				else{
					SetNxlItemproperty -ItemPath $SPERegPath -ItemName $_ -ItemType String -ItemValue $RegistryValueDictionary[$_]
				}
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('WriteSharePointEnforcerInstallInformationToRemoteFarmRegistry fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
    }
	
	function WriteStringInformationsToRemoteFarmRegistry($regPath, $regValues, $forceUpdate){
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WriteStringInformationsToRemoteFarmRegistry'
		$Error.clear()
		Try{
			$regValues.keys | ForEach-Object{
				$value = GetSpecificRegValueByPathAndName -path $regPath -name $_
				if($value -eq $null){
					PrintLog -Loglevel $PrintLogLevel.trace -Msg ('{0} does not exist in path:{1}, need create value in it' -f $_, $regPath)
					SetNxlItemproperty -ItemPath $regPath -ItemName $_ -ItemType String -ItemValue $regValues[$_]				
				}
				else{
					if($forceUpdate -eq $true){
						SetNxlItemproperty -ItemPath $regPath -ItemName $_ -ItemType String -ItemValue $regValues[$_]
					}
				}
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('WriteStringInformationsToRemoteFarmRegistry fail:' + $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	
	
	
	function WriteCommonLibraryPathInfoToRemoteFarmRegistry{
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WriteCommonLibraryPathInfoToRemoteFarmRegistry'
		if((IsItemChildsContainsItemName -ItemName $regCommonLibraryName -ItemDirPath $regNextLabsPath) -eq $false){
			CreateNxlItem -Type 'String' -ItemPath $regCommonLibraryPath
		}	
	}
		
	function WritePolicyControllerPathInfoToRemoteFarmRegistry{
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WritePolicyControllerPathInfoToRemoteFarmRegistry'
		if((IsItemChildsContainsItemName -ItemName $regPolicyControllerName -ItemDirPath $regCompliantEnterPrisePath) -eq $false){
			CreateNxlItem -Type 'String' -ItemPath $regPolicyControllerPath
		}
	}
	

    function WriteSPE2RemoteRegedit{
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function WriteSPE2RemoteRegedit'
		#write SPE to regedit
        WriteSharePointEnforcerPathInfoToRemoteFarmRegistry
	    WriteSharePointEnforcerInstallInformationToRemoteFarmRegistry
		if($enableJPCwithoutCEPC -eq $true){
			#write common library to regedit
			WriteCommonLibraryPathInfoToRemoteFarmRegistry
			WriteStringInformationsToRemoteFarmRegistry -regPath $regCommonLibraryPath -regValues $RegistryCommonLibraryDictionary   -forceUpdate $false
			#write Policy Controller to regedit
			WritePolicyControllerPathInfoToRemoteFarmRegistry
			WriteStringInformationsToRemoteFarmRegistry -regPath $regPolicyControllerPath -regValues $RegistryPolicyControllerDictionary -forceUpdate $false	
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
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('Before deploy our solution, already exist dll:{0} in server:{1}' -f $dllPath, $hostName)
				if($needCheckDllUsedByProcess -eq $true){
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('killing process which used this dll:{0}' -f $dllPath)
					GetDllUsedProcess -ModuleNameToFind $dll -needKill $needKillProcess -server $hostName -ModulePath $dllPath
				}          
			}
			else{
			}
		}
	}
	
	
	#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'before function WriteSPE2RemoteRegedit invoked'
    WriteSPE2RemoteRegedit
	#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'after function WriteSPE2RemoteRegedit invoked'
	
	#TestSPEEnvAboutDll -dllDirPath $sharePointEnforcerInstallDirPath -dllNames $sharePointEnforcerBinDllNames -hostName $serverAddr -needCheckDllUsedByProcess $true -needKillProcess $true
	#TestSPEEnvAboutDll -dllDirPath $GacMsilDir -dllNames $sharePointEnforcerGacMsilDllNames -hostName $serverAddr -needCheckDllUsedByProcess $false -needKillProcess $false
	
	#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'after invoke WriteSPE2RemoteRegedit'
};#end RegistSPEScript

#backup web.config and viewlsts.aspx
$BackUpEditWebConfig = {
	param($serverAddr, $centralAdminFilePath)
	$logSymbol = $using:logSymbol
	$sharePointLayOutsFolder = $using:sharePointLayOutsFolder
	$aspxFilesUnderSharePointRootLayouts = $using:aspxFilesUnderSharePointRootLayouts
	$MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
    $log = $using:log
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
			$r = $xmlData.DocumentElement.AppendChild($newNode)
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
	
	function BackUpAspxFile($aspxFileName, $folderPath, $serverHostName){
		$createdFileName = GetApsxFileHashName -aspxName $aspxFileName
		$sourcePath = $folderPath + '\' + $aspxFileName
		$destPath = $folderPath + '\' + $createdFileName
		$Error.clear()
		Try{
			if(IsFileExist -filePath $sourcePath){
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('exist aspx file:{0} in path:{1} in server:{2}' -f $aspxFileName, $sourcePath, $serverHostName)
				Copy-Item $sourcePath $destPath
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('when backup viewlsts.aspx, can not find:{0} in path:{1} in server:{2}' -f $aspxFileName, $folderPath, $serverHostName)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('BackUpAspxFile fail in server{0}, fail info:{1}' -f $serverHostName, $Error[0])
		}
		Finally{
			$Error.clear()
		}
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
		BackUpWebConfigFile -webConfigFilePath $centralAdminFilePath
		EditWebConfig -FeatureController $pathFeatureController -FeatureManager $pathFeatureManager -inXml $innerxmlData -configFilePath $centralAdminFilePath
		#DeleteWebConfig -FeatureController $pathFeatureController -FeatureManager $pathFeatureManager -configFilePath $centralAdminFilePath
	}
	else{
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('web.config not exists in {0}' -f $serverAddr)
	}
	
	#backup aspx
	#$viewlstsName = 'viewlsts.aspx'
	#BackUpAspxFile -aspxFileName $viewlstsName -folderPath $sharePointLayOutsFolder -serverHostName $serverAddr
	foreach($aspxfileName in $aspxFilesUnderSharePointRootLayouts){
		BackUpAspxFile -aspxFileName $aspxfileName -folderPath $sharePointLayOutsFolder -serverHostName $serverAddr
	}
};

function InvokeRemoteCommand($serverAddress, $excuteScript){
    #PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function InvokeRemoteCommand, serverAddress:' + $serverAddress)
    $Error.clear()
	Try{
	    #PrintLog -Loglevel $PrintLogLevel.trace -Msg ('beforce ' + $Message.InvokeRemoteCommand + $serverAddress)
	    $r = Invoke-Command -computerName $serverAddress -ScriptBlock $excuteScript -ArgumentList $serverAddress -ErrorAction Stop
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ($Message.InvokeRemoteCommand + $serverAddress + ' fail:' + $Error[0])
    }
	Finally{
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('after ' + $Message.InvokeRemoteCommand + $serverAddress)
		$Error.clear()
	}
}

function GetEffectiveFarmMachines{
	$invalidServers = @()	
	$Error.clear()
	Try{
	    $servers = get-spserver -ErrorAction Stop
	    foreach($server in $servers){
	        #if($server.Role -ne 'Invalid'){
			if(($server.Role -ne 'Invalid') -and ($server.Role -ne 'Search')){
		        $invalidServers += $server.Address
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('get effective server:' + $server.Address)
		    }
	    }
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetEffectiveFarmMachines fail:' + $Error[0])
	}
	Finally{
	    $Error.clear()
	}
	#return $invalidServers
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
function excuteScriptOnRemoteFarmMachines($WriteSPEIntoRemoteFarmRegistry){
    #PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function excuteScriptOnRemoteFarmMachines'
    GetEffectiveFarmMachines | ForEach-Object {InvokeRemoteCommand -serverAddress $_ -excuteScript $WriteSPEIntoRemoteFarmRegistry}
}

function IsSolutionUploaded{
    $Error.clear()
	#PrintLog -Loglevel $PrintLogLevel.trace -Msg 'in function IsSolutionUploaded'
    $result = $false
	Try{
	    $solutions = get-spsolution -ErrorAction Stop
        foreach($ss in $solutions){
            if($ss.Name -eq $solutionName){
	            $result = $true
		        break
	        }
        }
	}
	Catch{
	    PrintLog -Loglevel $PrintLogLevel.error -Msg ('get-spsolution fail:' + $Error[0])
	}
	Finally{
	    $Error.clear()
	}
	return $result
}

function UploadSPSolution($nxlSolutionPath){  
    $Error.clear()
	$result = $true
	$b_solutionUploadedStatus = IsSolutionUploaded

    Try{
		if($b_solutionUploadedStatus -eq $true){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ($solutionName + ' has already added')
			return
		}
	    $r = add-spsolution $nxlSolutionPath -ErrorAction Stop
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('add-spsolution success, solution name:' + $r.Name + ' ,SolutionId:' + $r.SolutionId + ' ,Deployed:' + $r.Deployed)
	}
    Catch{   
        PrintLog -Loglevel $PrintLogLevel.error -Msg ('add-spsolution fail:' + $Error[0])
        $result = $false		
    }
	Finally{
		if($result -eq $false){
			Exit
		}
		$Error.clear()
	}
}


function InstallSPSolutionEx($nxlSolutionName, $installOption, $selectedWebAppUrl){
	$Error.clear()
	Try{
		$alreadyInstalled = $false
		#PrintLog -Loglevel $PrintLogLevel.trace -Msg ('in function InstallSPSolutionEx, selectedWebAppUrl is:{0}' -f $selectedWebAppUrl)
		$r = install-spsolution -identity $nxlSolutionName -WebApplication $selectedWebAppUrl -GACDeployment -ErrorAction Stop
		while($alreadyInstalled -eq $false){
			$solutionInfo = GetSpecificSolutionInfo -name $solutionName
			if (($solutionInfo[-1] -eq $true) -And ($solutionInfo[0].Deployed -eq $true)){
				#urls where nextlabs.deployment.wsp deployed
				$deployedUrls = @()
				foreach($d in $solutionInfo.deployedwebapplications.Url){
					$deployedUrls += $d
				}
				if($deployedUrls -contains $selectedWebAppUrl){
					$alreadyInstalled = $true
				}
			}
		}
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('install {0} success' -f $nxlSolutionName)
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('InstallSPSolutionEx fail:{0}' -f $Error[0])
	}
	Finally{
		$Error.clear()
	}
}
function InstallSPSolution($nxlSolutionName, $installOption, $selectedWebAppUrl){
    $solutionUploadedStatus = $false
    while ($solutionUploadedStatus -eq $false){
		PrintLog -Loglevel $PrintLogLevel.info -Msg $Message.InstallSPSolutionSleep
        sleep -s 5
	    $b_solutionUploadedStatus = IsSolutionUploaded
	    if($b_solutionUploadedStatus -eq $true){
	        $solutionUploadedStatus = $true
		    #install wsp
			PrintLog -Loglevel $PrintLogLevel.info -Msg $Message.InstallSPSolution
			$Error.clear()
			Try{
				if($installOption -eq $true){
					$r = install-spsolution -identity $nxlSolutionName -allwebapplication -GACDeployment -ErrorAction Stop
				}
				elseif($installOption -eq $false){
					if($selectedWebAppUrl -ne $null){
						$r = install-spsolution -identity $nxlSolutionName -webapplication $selectedWebAppUrl -GACDeployment -ErrorAction Stop
					}
				}
			}
			Catch{
			    PrintLog -Loglevel $PrintLogLevel.error -Msg ('install-spsolution ' + $nxlSolutionName + ' fail:' + $Error[0])
				$solutionUploadedStatus = $true
				Exit
			}
			Finally{
			    $Error.clear()
			}
	    }
	    else{
			PrintLog -Loglevel $PrintLogLevel.info -Msg $Message.InstallSPSolutionNotDeployedYet
	    }
    }
}



function GetSpecificSolutionInfo($name){
    $result = $true
	$Error.clear()
	Try{
	    Get-SPSolution -Identity $name -ErrorAction Stop #return value0
	}
    Catch{
	    $result = $false
		#PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetSpecificSolutionInfo fail:' + $Error[0])
	}
	Finally{
	    $Error.clear()
	}
	return $result #return value1
}


#if bNeedDeployedWebapps is true, s can not be null
#if bNeedDeployedWebapps is false, s can be null
function GetSelectedWebapplicationUrl($bNeedDeployedWebapps, $s){
	$selectedUrl = $null
	[string[]]$webApplicationUrls = GetWebApplicationUrls
	[string[]]$spWebUrlsToDisplay = @()
    [string[]]$notdeployedUrls = @()
    Try{
        if($bNeedDeployedWebapps -eq $true){
#            $s = get-spsolution -id $solutionName
            [string[]]$deployedUrls = @()
		    foreach($d in $s.deployedwebapplications.Url){
			    $deployedUrls += $d
		    }
            $notdeployedUrls = $webApplicationUrls | Where {$deployedUrls -NotContains $_}
            for($u = 0; $u -lt $notdeployedUrls.Count; $u = $u + 1){
			    $sequence = $u.ToString()
			    $strUrl =  $sequence + '.' + $notdeployedUrls[$u]
			    $spWebUrlsToDisplay += $strUrl
		    }
        }
        elseif($bNeedDeployedWebapps -eq $false){
        	for($u = 0; $u -lt $webApplicationUrls.Count; $u = $u + 1){
			    $sequence = $u.ToString()
			    $strUrl =  $sequence + '.' + $webApplicationUrls[$u]
			    $spWebUrlsToDisplay += $strUrl
		    }
        }
        PrintLog -Loglevel $PrintLogLevel.info -Msg ('-------------------webapplication urls-------------------')
		#PrintLog -Loglevel $PrintLogLevel.info -Msg ($spWebUrlsToDisplay -join '     ')
		#write-output $spWebUrlsToDisplay
		$stringTooutput = $spWebUrlsToDisplay | Out-String
		PrintLog -Loglevel $PrintLogLevel.info -Msg ($stringTooutput)
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('-------------------webapplication urls-------------------')
		$regIntMatch = '^[0-9]\d*$'
        [string[]]$spWebUrls = @()
        if($bNeedDeployedWebapps -eq $false){
            $spWebUrls = $webApplicationUrls
        }
        elseif($bNeedDeployedWebapps -eq $true){
            $spWebUrls = $notdeployedUrls
        }
		if($spWebUrls.Count -gt 0){
            if($bNeedDeployedWebapps -eq $false){
                $webappUrl = Read-Host 'input webapplication URL or sequence of url to deploy solution'
            }
            elseif($bNeedDeployedWebapps -eq $true){
                $webappUrl = Read-Host 'input webapplication url or sequence of url which not deployed in this solution'
            }
			if([String]::IsNullOrEmpty($webappUrl) -eq $false){
				#$selectedUrl = $null
				if($webappUrl -match $regIntMatch){
					$index = [int]$webappUrl
					if(($index -lt $spWebUrls.Count)){
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('sequence you input is:{0}, and the array count:{1}' -f $index, $spWebUrls.Count)
						$UrlInArray = $spWebUrls[$index]
						if($spWebUrls -contains $UrlInArray){
							$selectedUrl = $UrlInArray
						}
					}
					else{
						PrintLog -Loglevel $PrintLogLevel.info -Msg ('sequence you input is:{0}, is larger than array count:{1}' -f $index, $spWebUrls.Count)
					}
				}
				elseif($spWebUrls -contains $webappUrl){
					$selectedUrl = $webappUrl
				}
			}
		}
    }
    Catch{
        PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetSelectWebAppUrlEx fails:' + $Error[0])
    }
    Finally{
        $Error.clear()
    }
    return $selectedUrl
}



function WaitForSolutionInstalled{
	#PrintLog -Loglevel $PrintLogLevel.info -Msg 'enter in function WaitForSolutionInstalled'
    $deployed = $false
	$Error.clear()
    while ($deployed -eq $false) {
		PrintLog -Loglevel $PrintLogLevel.info -Msg 'after install wsp, will sleep 5 s'
        sleep -s 5
		$sln = GetSpecificSolutionInfo -name $solutionName
		Try{
			if($sln -eq $null){
				continue
			}
			PrintLog -Loglevel $PrintLogLevel.info -Msg ($sln[0].lastoperationdetails | Out-String)
			
			
			$jobName = "*solution-deployment*$solutionName*"
			$job = Get-SPTimerJob | ?{ $_.Name -like $jobName }
			if ($job -eq $null) {
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('Timer job about installing: {0} not found' -f $solutionName)
			}
			else{
				$jobFullName = $job.Name
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('job: {0} about installing:{1} exists' -f $solutionName, $jobFullName)
				#Write-Host -NoNewLine "Waiting to finish job $JobFullName"
				#while ((Get-SPTimerJob $JobFullName) -ne $null) {
				#	Write-Host -NoNewLine .
				#	Start-Sleep -Seconds 2
				#}
				#Write-Host  "Finished waiting for job.."
			}
			
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('install-solution result: ' + $sln[-1] + ' ,solution name: ' + $sln[0].name + ' ,solution deployed: ' + $sln[0].Deployed + ' ,solution jobexist: ' + $sln[0].JobExists)
		    if (($sln[-1] -eq $true) -And ($sln[0].Deployed -eq $true) -And ($sln[0].JobExists -eq $false)) {
				$deployed = $true
				PrintLog -Loglevel $PrintLogLevel.info -Msg ($Message.WaitForSolutionNotInstalledComplete + ', solution name:' + $sln[0].name + ' ,solution id:' + $sln[0].solutionid)
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.info -Msg $Message.WaitForSolutionNotInstalledYet
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('WaitForSolutionInstalled error:' + $Error[0])
			Exit
		}
		Finally{
		    $Error.clear()
		}
    }
	#PrintLog -Loglevel $PrintLogLevel.info -Msg 'leave in function WaitForSolutionInstalled'
	return $deployed
}

#try to judge whether machine has deployed wsp solution
$RegisterComDLLScript = {
	param($serverAddr)
	
	$enableJPCwithoutCEPC = $using:enableJPCwithoutCEPC
	$sharePointRootFolder = $using:sharePointRootFolder #'C:\Program Files\Common Files\microsoft shared\Web Server Extensions\16'
	$NextLabsPath = $using:NextLabsPath
	
	
	$sharePointLayOutsFolder= $using:sharePointLayOutsFolder
	$sharePointMappedFolder = $using:sharePointMappedFolder
	$sharePointEnforcerInstallPath = $using:sharePointEnforcerInstallPath #'C:\Program Files\NextLabs\SharePoint Enforcer'
	$sharePointEnforcerInstallDir = $using:sharePointEnforcerInstallDir

	
    $logSymbol = $using:logSymbol
    $valueErrorReason = $using:valueErrorReason
    $MapColor2Int = $using:MapColor2Int
    $PrintLogLevel = $using:PrintLogLevel
    $log = $using:log
	$TagDocProtector = $using:TagDocProtector
	$SDKWrapper = $using:SDKWrapper
	
	$regDefaulValue = $using:regDefaulValue
	$regCLSIDPath = $using:regCLSIDPath
	$regInprocServer32Name	= $using:regInprocServer32Name
	$tagDocCLSID = $using:tagDocCLSID
	$sdkWrapperCLSID = $using:sdkWrapperCLSID
	
	$commonLibrary32Dlls = $using:commonLibrary32Dlls
	$commonLibrary64Dlls = $using:commonLibrary64Dlls
	$policyControllerDlls = $using:policyControllerDlls
	$policyControllerFiles = $using:policyControllerFiles
	
	$CommonLibrary = $using:CommonLibrary
	$PolicyControllerLibrary = $using:PolicyControllerLibrary
	$CommonName = $using:CommonName
	$PolicyControllerName = $using:PolicyControllerName
	
	$Bin = $using:Bin
	$Bin32 = $using:Bin32
	$Bin64 = $using:Bin64
	
	$regCommonLibraryPath = $using:regCommonLibraryPath
	$regPolicyControllerPath = $using:regPolicyControllerPath
	$regInstallDir = $using:regInstallDir
	$regPolicyControllerDir = $using:regPolicyControllerDir
	
	$CommonBin32SourceFolder = $sharePointLayOutsFolder + '\' + $CommonLibrary + $Bin32
	$CommonBin64SourceFolder = $sharePointLayOutsFolder + '\' + $CommonLibrary + $Bin64
	$PolicyControllerBinSourceFolder =$sharePointLayOutsFolder + '\' + $PolicyControllerLibrary + $Bin
	$PolicyControllerSourceFolder = $sharePointLayOutsFolder + '\' + $PolicyControllerLibrary
	
	$CommonBin32DestFolder = $NextLabsPath + '\' + $CommonName + '\' + $Bin32
	$CommonBin64DestFolder = $NextLabsPath + '\' + $CommonName + '\' + $Bin64
	$PolicyControllerBinDestFolder = $NextLabsPath + '\' + $PolicyControllerName + '\' + $Bin
	$PolicyControllerDestFolder = $NextLabsPath + '\' + $PolicyControllerName + '\'
	
	
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
	#for test
	#$TagDocProtector = 'C:\Users\Administrator.QAPF1\Desktop\test\TagDocProtector.dll'
	#$SDKWrapper = 'C:\Users\Administrator.QAPF1\Desktop\test\SDKWrapper.dll'
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
	function GetSpecificRegValueByPathAndName($path, $name){
		$value = $null
		Try{
			$value = get-itempropertyvalue -path $path -name $name
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('GetSpecificRegValueByPathAndName fail:' + $Error[0])
		}
		Finally{
			$Error.Clear()
		}
		return $value
	}
	function RemoveEndSymbolInString($s){
		$Error.Clear()
		$value = $null
		Try{
			if($s -ne $null -and $s[-1] -eq '\'){
				$value = $s.SubString(0,$s.Length-1)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('RemoveEndSymbolInString fail:{0}' -f $Error[0])
		}
		Finally{
			if($value -eq $null){
				$value = $s
			}
			$Error.Clear()
		}
		return $value
	}
	
	function IsEnvViriableExistInSystemPath($value){
		$result = $false
		Try{
			$s = [Environment]::GetEnvironmentVariable("Path", "Machine")
			$values = $s.split(';')
			foreach($v in $values){
				if($v -eq $value){
					$result = $true
					break
				}
			}
		}
		Catch{
		}
		Finally{
			$Error.clear()
		}
		return $result
	}
	
	function SetEnvVariableToSystemPath($value){
		if([String]::IsNullOrEmpty($value)){
		}
		else{
			$valueTobeWritten = $null
			if($value.startswith(";")){
				$valueTobeWritten = $value
			}
			else{
				$valueTobeWritten = ';' + $value
			}
			#;C:PS\policy\bin64
			[Environment]::SetEnvironmentVariable("Path", $env:Path + $valueTobeWritten, "Machine")	
		}
	}
    function RegisterComDLL($dllPath, $serverName){
	    $Error.clear()
	    Try{
			if(IsFileExist -filePath $dllPath){
		        #$r = regsvr32 $dllPath #this not work
				&'regsvr32.exe' $dllPath
				PrintLog -Loglevel $PrintLogLevel.info -Msg ('Registering COM dll:{0} in server:{1}' -f $dllPath, $serverName)
				
		    }
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find COM dll:{0} in server:{1}' -f $dllPath, $serverName)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('Register ComDLL fail:{0}, in server:{1}' -f $Error[0], $serverName)
		}
	}
	function IsDLLRegisterCOMSuccess($defaultValue, $CLSIDPath, $InprocServer32Name, $tagDocCOMCLSID, $sdkWrapperCOMCLSID, $pathTagDoc, $pathSdkWrapper, $server){
	
		Try{
			$regtagdoc = '{0}\{1}\{2}' -f $CLSIDPath, $tagDocCOMCLSID, $InprocServer32Name
			$regsdkwrapper = '{0}\{1}\{2}' -f $CLSIDPath, $sdkWrapperCOMCLSID, $InprocServer32Name
			
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('regtagdoc is:{0} in server:{1}' -f $regtagdoc, $server)
			PrintLog -Loglevel $PrintLogLevel.trace -Msg ('regsdkwrapper is:{0} in server:{1}' -f $regsdkwrapper, $server)
			
			$key_tagdoc = get-itemproperty $regtagdoc
			$key_sdkwrapper = get-itemproperty $regsdkwrapper
			$key_tagdoc
			$key_sdkwrapper
			if($key_tagdoc -ne $null){
				$value_tagdoc = $key_tagdoc.$defaultValue
				if($value_tagdoc -eq $pathTagDoc){
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('register COM success in server:{0}, COM dll path is:{1}, COM CLSID is:{2}' -f $server, $value_tagdoc, $tagDocCOMCLSID)
				}
				else{
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('register COM TagDocProtector.dll fail in server:{0}, COM CLSID is:{1}' -f $server, $tagDocCOMCLSID)
				}
			}
			else{	
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can find COM TagDocProtector.dll CLSID:{0} in server:{1} registry, but can not find dll path in that registry key' -f $tagDocCOMCLSID, $server)
			}
			if($key_sdkwrapper -ne $null){
				$value_sdkwrapper = $key_sdkwrapper.$defaultValue
				if($value_sdkwrapper -eq $pathSdkWrapper){
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('register COM SDKWrapper.dll success in server:{0}, COM dll path is:{1}, COM CLSID is:{2}' -f $server, $value_sdkwrapper, $sdkWrapperCOMCLSID)
				}
				else{
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('register COM SDKWrapper.dll fail in server:{0}, COM CLSID is:{1}' -f $server, $sdkWrapperCOMCLSID)
				}
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can find COM SDKWrapper.dll CLSID:{0}, but can not find dll path in it' -f $sdkWrapperCOMCLSID)
			}
		}
		Catch{
		}
		Finally{
		
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
	
	function CopyFoldersFromSharePointFolderToNextLabsFolder($sourceFolder, $destFolder, $dlls, $hostName){
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('sourcefolder is:{0}, destFolder is:{1} in server:{2}' -f $sourceFolder, $destFolder, $hostName)
		$Error.clear()
		Try{
			foreach($dll in $dlls){
				$source = $sourceFolder + $dll
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('will copy files from {0} to dest folder:{1} in server:{2}' -f $source, $destFolder, $hostName)
				if(IsFileExist -filePath $source){#source exist then copy files to dest
					$dest = $destFolder + $commonbin32
					if(IsFileExist -filePath $dest){
						#file exists, do nothing
						PrintLog -Loglevel $PrintLogLevel.trace -Msg ('dest:{0} already exist in server:{1}, do nothing in copy files function' -f $dest, $hostName)
					}
					else{
						copy-item $source $dest
						PrintLog -Loglevel $PrintLogLevel.trace -Msg ('copy files from {0} to {1} in server:{2}' -f $source, $dest, $hostName)
					}
				}
				else{
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('source:{0} does not exist in server:{1}' -f $source, $hostName)
				}
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('copy files from {0} to {1} in server:{2} fail, {3}' -f $source, $dest, $hostName, $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	
	
	function CopyFilesFromSharePointRootToNextLabs($source, $dest, $hostName){
		$Error.clear()
		Try{
			if(IsFileExist -filePath $source){
				if(IsFileExist -filePath $dest){
					DeleteFiles -source $dest -hostName $hostName
				}
				copy-item $source $dest -recurse -force
				PrintLog -Loglevel $PrintLogLevel.trace -Msg ('copy files from:{0} to:{1} in server:{2} success' -f $source, $dest, $hostName)
			}
			else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('folder:{0} does not exist in server:{1}' -f $source, $hostName)
			}
		}
		Catch{
			PrintLog -Loglevel $PrintLogLevel.error -Msg ('copy files from:{0} to:{1} in server:{2} fail:{3}' -f $source, $dest, $hostName, $Error[0])
		}
		Finally{
			$Error.clear()
		}
	}
	
	function IsSystem64bit{
		$value = [Environment]::Is64BitOperatingSystem
		return $value
	}

	#copy files from sharepointroot folder to nextlabs folder
	CopyFilesFromSharePointRootToNextLabs -source $sharePointMappedFolder -dest $sharePointEnforcerInstallPath -hostName $serverAddr
	
	
	if($enableJPCwithoutCEPC -eq $true){
		#here, need copy dlls from wsp map folder to C:\Programe Files\NextLabs\Common to make jpc work without isntalling cepc
		#add cesdk.dll or cesdk32.dll folder to Env path to make dllimport can find them	
		$commonDestFolderFromRegedit1 = GetSpecificRegValueByPathAndName -path $regCommonLibraryPath -name $regInstallDir
		$policyControllerDestFolderFromRegedit1 = GetSpecificRegValueByPathAndName -path $regPolicyControllerPath -name $regPolicyControllerDir
		$TempcommonDestFolderFromRegedit = RemoveEndSymbolInString -s $commonDestFolderFromRegedit1
		$TemppolicyControllerDestFolderFromRegedit = RemoveEndSymbolInString -s $policyControllerDestFolderFromRegedit1
		
		$commonDestFolderFromRegedit = $TempcommonDestFolderFromRegedit + '\'
		$policyControllerDestFolderFromRegedit = $TemppolicyControllerDestFolderFromRegedit + '\'
		
		PrintLog -Loglevel $PrintLogLevel.trace -Msg ('commonDestFolderFromRegedit is:{0}, policyControllerDestFolderFromRegedit is:{1} in server:{2}' -f $commonDestFolderFromRegedit, $policyControllerDestFolderFromRegedit, $serverAddr)
			
		if(IsSystem64bit){
			if($TempcommonDestFolderFromRegedit -ne $null){
				$CommonBin64DestFolder = $commonDestFolderFromRegedit + $Bin64
			}
			#add 64 bit common to env path
			
			if(IsEnvViriableExistInSystemPath -value $CommonBin64DestFolder){
			}
			else{
				SetEnvVariableToSystemPath -value $CommonBin64DestFolder
			}

			
			if(IsFileExist -filePath $CommonBin64DestFolder){
				CopyFoldersFromSharePointFolderToNextLabsFolder -sourceFolder $CommonBin64SourceFolder -destFolder $CommonBin64DestFolder -dlls $commonLibrary64Dlls -hostName $serverAddr
			}
			else{
				CopyFilesFromSharePointRootToNextLabs -source $CommonBin64SourceFolder -dest $CommonBin64DestFolder -hostName $serverAddr
			}
		}
		else{
			if($TempcommonDestFolderFromRegedit -ne $null){
				$CommonBin32DestFolder = $commonDestFolderFromRegedit + $Bin32
			}
			
			if(IsEnvViriableExistInSystemPath -value $CommonBin32DestFolder){
			}
			else{
				SetEnvVariableToSystemPath -value $CommonBin32DestFolder
			}
			#add 32 bit common to env path
			if(IsFileExist -filePath $CommonBin32DestFolder){
				CopyFoldersFromSharePointFolderToNextLabsFolder -sourceFolder $CommonBin32SourceFolder -destFolder $CommonBin32DestFolder -dlls $commonLibrary32Dlls -hostName $serverAddr
			}
			else{
				CopyFilesFromSharePointRootToNextLabs -source $CommonBin32SourceFolder -dest $CommonBin32DestFolder -hostName $serverAddr
			}
		}
		
		if($TemppolicyControllerDestFolderFromRegedit -ne $null){
			$PolicyControllerBinDestFolder = $policyControllerDestFolderFromRegedit + $Bin
		}
		if(IsFileExist -filePath $PolicyControllerBinDestFolder){
			CopyFoldersFromSharePointFolderToNextLabsFolder -sourceFolder $PolicyControllerBinSourceFolder -destFolder $PolicyControllerBinDestFolder -dlls $policyControllerDlls -hostName $serverAddr
		}
		else{
			CopyFilesFromSharePointRootToNextLabs -source $PolicyControllerBinSourceFolder -dest $PolicyControllerBinDestFolder -hostName $serverAddr
		}
		
		if(IsEnvViriableExistInSystemPath -value $PolicyControllerBinDestFolder){
		}
		else{
			SetEnvVariableToSystemPath -value $PolicyControllerBinDestFolder
		}
		#CopyFoldersFromSharePointFolderToNextLabsFolder -sourceFolder $PolicyControllerSourceFolder -destFolder $PolicyControllerDestFolder -dlls $policyControllerFiles -hostName $serverAddr
	}
	
	DeleteFiles -source $sharePointMappedFolder -hostName $serverAddr
	
	RegisterComDLL -dllPath $TagDocProtector -serverName $serverAddr
	RegisterComDLL -dllPath $SDKWrapper -serverName $serverAddr
	
	IsDLLRegisterCOMSuccess -defaultValue $regDefaulValue -CLSIDPath $regCLSIDPath -InprocServer32Name $regInprocServer32Name -tagDocCOMCLSID $tagDocCLSID -sdkWrapperCOMCLSID $sdkWrapperCLSID -pathTagDoc $TagDocProtector -pathSdkWrapper $SDKWrapper -server $serverAddr

};#end RegisterComDLLScript


function CheckCOMDLLRegister($root, $clsid, $pathSdkWrapper){
	$Error.clear()
	Try{
		$servers = GetEffectiveFarmMachines
		foreach($server in $servers){
			$rootkey = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey($root,$server)
			if($rootkey -ne $null){
				$subkey = $rootkey.OpenSubKey($clsid)
				if($subkey -eq $null){
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('in server {0}, can not get subkey' -f $server)
					return
				}
				if($subkey.GetValue('') -eq $pathSdkWrapper){
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('COM SDKWrapper.dll success wirtten in regestry by server:{0}' -f $server)
				}
				else{
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('COM SDKWrapper.dll fail wirtten in regestry by server:{0}' -f $server)
				}
			}
			else{	
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('check server {0} registry, find {1} is empty' -f $server, $root)
			}
		}
	}
	Catch{
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('CheckCOMDLLRegister fail:{0}' -f $Error[0])
	}
	Finally{
		$Error.clear()
	}
}


function MakeSureSPRelatedServiceRunning($serviceName){

	$servers = GetEffectiveFarmMachines
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


#run after solution deployed, solution already deployed
function DeploySolution($solutionName){
	$solutionInfo = GetSpecificSolutionInfo -name $solutionName
	if (($solutionInfo[-1] -eq $true) -And ($solutionInfo[0].Deployed -eq $true)){
		$selectedUrl = $null
		$selectedUrl = GetSelectedWebapplicationUrl -bNeedDeployedWebapps $true -s $solutionInfo
		PrintLog -Loglevel $PrintLogLevel.info -Msg ('url which you input is:{0}' -f $selectedUrl)
		if($selectedUrl -ne $null){
			InstallSPSolutionEx -nxlSolutionName $slnName -installOption $false -selectedWebAppUrl $selectedUrl
		}
		else{
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('selected url which you input is null, and selected url is:{0}' -f $selectedUrl)
		}
		Exit
	}
	else{
		#PrintLog -Loglevel $PrintLogLevel.info -Msg ('will deploy{0}' -f $slnName)
	}
}


function Main($FeatureControllerPath, $FeatureManagerPath, $iXml, $SPEScript, $slnPath, $slnName, $regSDKWrapperCommand, $regTagDocProtectorCommand, $pathWsp, $classRoot, $sdkWrapperClsidPath, $sdkWrapperPath, $slnId, $deployOption){
	if($help -eq $true){
		Write-Host "-log ===>critical:cyan, warn:magenta, error:red, info:green, debug:yellow, trace:white" -ForegroundColor yellow
		Write-Host "-wspPath ===>input your wsp path such as c:\wsp\your.wsp" -ForegroundColor yellow
		Write-Host "-option ===>if you want deploy wsp to allwebapplication you can set option to true or use default" -ForegroundColor yellow
		Write-Host "-jpc ===>if you want just use jpc to enforcer, you can set $true, in that case all relative dlls will be copied into spe folder" -ForegroundColor yellow
		Exit
	}
	DeploySolution -solutionName $slnName

	#solution not deployed yet, user must choose an option to install solution
	
	#Init
	$solutionPackagePath = InitSolutionPath -solution $slnPath -wsp $pathWsp
	PrintLog -Loglevel $PrintLogLevel.info -Msg ('wspPath is: {0}, here if wspPath is empty, it means we can not find wsp' -f $solutionPackagePath)
	
	if($solutionPackagePath -eq $null){
		PrintLog -Loglevel $PrintLogLevel.error -Msg ('can not find:{0} by wspPath, and exit deploy' -f $solutionPackagePath)
		Exit
	}
	
	$webconfigFilePath = GetAdminWebConfigPath
	
	GetEffectiveFarmMachines | ForEach-Object {Invoke-Command -computerName $_ -ScriptBlock $BackUpEditWebConfig -ArgumentList $_, $webconfigFilePath}
	
	#write registry
	excuteScriptOnRemoteFarmMachines -WriteSPEIntoRemoteFarmRegistry $SPEScript

	
	#make sure SPAdminV4 service running
	MakeSureSPRelatedServiceRunning -serviceName 'SPAdminV4'
	#make sure SPTimerV4 service running
	MakeSureSPRelatedServiceRunning -serviceName 'SPTimerV4'
	
	#add wsp
	UploadSPSolution -nxlSolutionPath $solutionPackagePath
	
	#install wsp
	if($deployOption -eq $false){
		$selectedUrl = GetSelectedWebapplicationUrl -bNeedDeployedWebapps $false -s $null
		
		if($selectedUrl -ne $null){
			InstallSPSolution -nxlSolutionName $slnName -installOption $deployOption -selectedWebAppUrl $selectedUrl	
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('first install:{0} to webapplication:{1}' -f $slnName, $selectedUrl)
		}
		else{
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('first install, your input url {0} cause some errors' -f $selectedUrl)
		}
	}
	elseif($deployOption -eq $true){
		InstallSPSolution -nxlSolutionName $slnName -installOption $deployOption -selectedWebAppUrl $null
	}

	#waiting for deploy completed
	$result = WaitForSolutionInstalled
	if($result -eq $false){
		PrintLog -Loglevel $PrintLogLevel.error -Msg 'installing wsp occurs some errors'
		Exit
	}
	
	excuteScriptOnRemoteFarmMachines -WriteSPEIntoRemoteFarmRegistry $RegisterComDLLScript
	
	if(IsSharePointFarmSingleServerFarm){

	}
	
	
	$solutionInfo = get-spsolution -id $slnId
	
	if($solutionInfo -ne $null){
		PrintLog -Loglevel $PrintLogLevel.info -Msg ($solutionInfo.lastoperationdetails | Out-String)
		PrintLog -Loglevel $PrintLogLevel.info -Msg ($solutionInfo.DeployedWebApplications | Out-String)
		#PrintLog -Loglevel $PrintLogLevel.info -Msg ($solutionInfo.lastoperationresult | Out-String)
		if($solutionInfo.lastoperationresult -eq [Microsoft.SharePoint.Administration.SPSolutionOperationResult]::DeploymentSucceeded){
			PrintLog -Loglevel $PrintLogLevel.info -Msg ('deploy {0} successfully!' -f $slnName)
		}
	}
}

Main -FeatureControllerPath $pathFeatureController -FeatureManagerPath $pathFeatureManager -iXml $innerxmlData -SPEScript $RegistSPEScript -slnPath $solutionPath -slnName $solutionName -regSDKWrapperCommand $registerSDKWrapperCommand -regTagDocProtectorCommand $registerTagDocProtectorCommand  -pathWsp $wspPath -classRoot $regClassesRoot -sdkWrapperClsidPath $SDKWrapperCLSIDPath -sdkWrapperPath $SDKWrapper -slnId $solutionId -deployOption $option
pause