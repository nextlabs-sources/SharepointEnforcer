param($log = 'trace')

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
$logSymbol = ''
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
$SDKWrapperCLSID = '{5DB9F41D-6BDB-49C3-BBB4-20A7D83E92F3}'
$regDefaulValue = '(default)'
$regCLSIDPath = 'Registry::HKEY_CLASSES_ROOT\CLSID'
$regInprocServer32Name= 'InprocServer32'
$tagDocCLSID = '{6EC4BB1F-3F73-4799-BC98-A3DF9AE23A0B}'
$sdkWrapperCLSID = $SDKWrapperCLSID

$Bin = 'bin\'
$NextLabsPath = 'C:\Program Files\NextLabs'
$SDKWrapperDll = 'SDKWrapper.dll'
$TagDocProtectorDll = 'TagDocProtector.dll'
$sharePointEnforcerInstallPath = $NextLabsPath + '\SharePoint Enforcer'
$sharePointEnforcerInstallDir = $sharePointEnforcerInstallPath + '\'
$SDKWrapper = $sharePointEnforcerInstallDir + $Bin + $SDKWrapperDll
$TagDocProtector = $sharePointEnforcerInstallDir + $Bin + $TagDocProtectorDll
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

$RegisterComDLLScript = {
	param($serverAddr)
	$log = $using:log
	$logSymbol = $using:logSymbol
	$MapColor2Int = $using:MapColor2Int
	$PrintLogLevel = $using:PrintLogLevel
	$SDKWrapperCLSID = $using:SDKWrapperCLSID
	$regDefaulValue = $using:regDefaulValue
	$regCLSIDPath = $using:regCLSIDPath
	$regInprocServer32Name= $using:regInprocServer32Name
	$tagDocCLSID = $using:tagDocCLSID
	$sdkWrapperCLSID = $using:sdkWrapperCLSID
	$Bin = $using:Bin
	$NextLabsPath = $using:NextLabsPath
	$sharePointEnforcerInstallPath = $using:sharePointEnforcerInstallPath
	$sharePointEnforcerInstallDir = $using:sharePointEnforcerInstallDir
	$SDKWrapper = $using:SDKWrapper
	$TagDocProtector = $using:TagDocProtector
	# check dll, xml, and so on whether is installed, we check com dll is register or not
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
			}else{	
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can find COM TagDocProtector.dll CLSID:{0} in server:{1} registry, but can not find dll path in that registry key' -f $tagDocCOMCLSID, $server)
			}
			if($key_sdkwrapper -ne $null){
				$value_sdkwrapper = $key_sdkwrapper.$defaultValue
				if($value_sdkwrapper -eq $pathSdkWrapper){
					PrintLog -Loglevel $PrintLogLevel.info -Msg ('register COM SDKWrapper.dll success in server:{0}, COM dll path is:{1}, COM CLSID is:{2}' -f $server, $value_sdkwrapper, $sdkWrapperCOMCLSID)
				}else{
					PrintLog -Loglevel $PrintLogLevel.error -Msg ('register COM SDKWrapper.dll fail in server:{0}, COM CLSID is:{1}' -f $server, $sdkWrapperCOMCLSID)
				}
			}else{
				PrintLog -Loglevel $PrintLogLevel.error -Msg ('can find COM SDKWrapper.dll CLSID:{0}, but can not find dll path in it' -f $sdkWrapperCOMCLSID)
			}
		}
		Catch{
		}
		Finally{
		
		}
	} 
	IsDLLRegisterCOMSuccess -defaultValue $regDefaulValue -CLSIDPath $regCLSIDPath -InprocServer32Name $regInprocServer32Name -tagDocCOMCLSID $tagDocCLSID -sdkWrapperCOMCLSID $sdkWrapperCLSID -pathTagDoc $TagDocProtector -pathSdkWrapper $SDKWrapper -server $serverAddr
};



GetEffectiveFarmMachines | ForEach-Object {InvokeRemoteCommand -serverAddress $_ -excuteScript $RegisterComDLLScript}


