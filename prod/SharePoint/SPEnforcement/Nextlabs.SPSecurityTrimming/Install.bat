
@SET FEATURESDIR="c:\program files\common files\microsoft shared\web server extensions\12\TEMPLATE\FEATURES"
@SET STSADM="c:\program files\common files\microsoft shared\web server extensions\12\bin\stsadm"

@SET GACUTIL="c:\program files\microsoft sdks\windows\v6.0A\bin\gacutil.exe"

%STSADM% -o uninstallfeature -filename Nextlabs.SPSecurityTrimming\Feature.xml -force
IISRESET

Echo installing or updating ItemAuditing.dll in GAC
%GACUTIL% /if bin\debug\Nextlabs.SPSecurityTrimming.dll

REM Echo Copying files
xcopy /e /y FEATURES\* %FEATURESDIR%

%STSADM% -o installfeature -filename Nextlabs.SPSecurityTrimming\Feature.xml -force 

IISRESET
