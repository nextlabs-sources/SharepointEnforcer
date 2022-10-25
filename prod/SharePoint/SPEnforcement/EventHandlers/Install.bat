
@SET FEATURESDIR="c:\program files\common files\microsoft shared\web server extensions\12\TEMPLATE\FEATURES"
@SET STSADM="c:\program files\common files\microsoft shared\web server extensions\12\bin\stsadm"

REM  Map your VS8 path to V:\  by using "subst v: your Visual Studio 8 path"
@SET GACUTIL="V:\SDK\v2.0\Bin\gacutil.exe"

%STSADM% -o uninstallfeature -filename  NextLabsSPEnforcer\Feature.xml -force
%STSADM% -o uninstallfeature -filename  NextLabsSPFeatureEnforcer\Feature.xml -force
IISRESET

Echo installing or updating ItemAuditing.dll in GAC
%GACUTIL% /if bin\debug\NextLabs.SPEnforcer.dll
copy "SPSecurityEnforcer.config" "C:\WINDOWS\ASSEMBLY\GAC_MSIL\NextLabs.SPEnforcer\1.0.2600.3601__5ef8e9c15bdfa43e\SPSecurityEnforcer.config"

REM Echo Copying files
xcopy /e /y FEATURES\* %FEATURESDIR%

%STSADM% -o installfeature -filename  NextLabsSPEnforcer\Feature.xml -force 
%STSADM% -o installfeature -filename  NextLabsSPFeatureEnforcer\Feature.xml -force

IISRESET
