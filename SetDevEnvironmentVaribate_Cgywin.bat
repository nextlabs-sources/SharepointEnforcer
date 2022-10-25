@echo off

echo Please run as administrator

rem NLEXTERNALDIR need set up in advance
rem Environment variables: WORKSPACE, NLBUILDROOT, BUID_OUTPUT, BUILD_NUMBER

set BAT_DIR=%~dp0
set BAT_DIR=%BAT_DIR:~0,-1%
set BAT_DIR=%BAT_DIR:\=/%

set WORKSPACE_NEW=%BAT_DIR%
set NLBUILDROOT_NEW=%BAT_DIR%
set BUID_OUTPUT_NEW=%BAT_DIR%/output
set BUILD_NUMBER_NEW=1001
set VERSION_BUILD_NEW=1001

setx /m WORKSPACE %WORKSPACE_NEW%
setx /m NLBUILDROOT %NLBUILDROOT_NEW%
setx /m BUID_OUTPUT %BUID_OUTPUT_NEW%
setx /m BUILD_NUMBER %BUILD_NUMBER_NEW%
setx /m VERSION_BUILD %VERSION_BUILD_NEW%


echo BAT_DIR=%BAT_DIR%
echo WORKSPACE FROM %WORKSPACE% TO %WORKSPACE_NEW%
echo NLBUILDROOT FROM %NLBUILDROOT% TO %NLBUILDROOT_NEW%
echo BUID_OUTPUT FROM %BUID_OUTPUT% TO %BUID_OUTPUT_NEW%
echo BUILD_NUMBER FROM %BUILD_NUMBER% TO %BUILD_NUMBER_NEW%
echo VERSION_BUILD FROM %VERSION_BUILD% TO %VERSION_BUILD_NEW%


echo -----------------------
echo Please open the new dev.exe process to do compile
echo If the command windown cannot auto exit, please close it manually
echo -----------------------

rem set VC_VARS_ALL_BAT=%VS140COMNTOOLS%
rem set VC_VARS_ALL_BAT=%VC_VARS_ALL_BAT:Tools=IDE%devenv.exe

rem mkdir "%BAT_DIR%\.git\hooks"
rem copy /y "%BAT_DIR%\pre-commit" "%BAT_DIR%\.git\hooks\pre-commit"

pause