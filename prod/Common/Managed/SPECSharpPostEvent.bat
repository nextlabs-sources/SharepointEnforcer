@echo off

set OrgErrorLevel=%ErrorLevel%
echo Begin execute common CSharp post event, backup OrgErrorLevel=%OrgErrorLevel%
set ErrorLevel=0

rem ProjectType, ProjectConfiguration, CompileOutDir, NeedCopyToBaseDotnetDir, NeedCopyToSPVersionDir

rem --------------Parameters-------------------------
rem ProjectType: EXE, DLL
set ProjectType=%~1

rem ProjectConfiguration: Release, Debug, SP2016Debug, SP2019Release
set ProjectConfiguration=%~2

rem CompileOutDir compile output folder path, like Release_AnyCpu\
set CompileOutDir=%~3

rem NeedCopyToBaseDotnetDir, 0|1, need copy to release_dotnet, release_dotnet
set NeedCopyToBaseDotnetDir=%~4

rem NeedCopyToSPVersionDir, 0|1, need copy to release_dotnet\SP2016Release, release_dotnet\SP2019Rlease
set NeedCopyToSPVersionDir=%~5

rem NeedCopyToProjectConfigurationDir, 0|1, need copy to release_dotnet\%ProjectConfiguration%
set NeedCopyToProjectConfigurationDir=%~6

echo ProjectType=%ProjectType%
echo ProjectConfiguration=%ProjectConfiguration%
echo CompileOutDir=%CompileOutDir%
echo NeedCopyToBaseDotnetDir=%NeedCopyToBaseDotnetDir%
echo NeedCopyToSPVersionDir=%NeedCopyToSPVersionDir%
echo NeedCopyToProjectConfigurationDir=%NeedCopyToProjectConfigurationDir%
rem ----------------End-----------------------

rem --------------Standard veriables-------------------------
set ProjectReleaseOrDebug=Release
echo %ProjectConfiguration% | findstr /i "Debug" > nul && set ProjectReleaseOrDebug=Debug

set ProductRoot=%NLBUILDROOT%
set ProductRootBin=%ProductRoot%\Bin
set BaseDotnetDir=%ProjectReleaseOrDebug%_Dotnet

echo NLBUILDROOT=%NLBUILDROOT%
echo ProductRoot=%ProductRoot%
echo ProductRootBin=%ProductRootBin%
echo ProjectReleaseOrDebug=%ProjectReleaseOrDebug%
echo BaseDotnetDir=%BaseDotnetDir%
rem ----------------End-----------------------

rem --------------Do copy-------------------------

rem Create out folders. Donot suggest using mkdir -p parameter, this maybe cover a mistake
echo ErrorLevel=%ErrorLevel%
if not exist "%ProductRootBin%" mkdir "%ProductRootBin%"
if not exist "%ProductRootBin%\%BaseDotnetDir%" mkdir "%ProductRootBin%\%BaseDotnetDir%"


rem Do file copy
if %ErrorLevel% NEQ 0 (
    echo make root dir "%ProductRootBin%" or "%ProductRootBin%\%BaseDotnetDir%" failed, please check
    goto End;
) else (
    rem Here using an array to store the folders and then do copy is better
    if %NeedCopyToBaseDotnetDir% NEQ 0 (
        call:CopyCSharpProjectFiles "%CompileOutDir%" "%ProductRootBin%\%BaseDotnetDir%"
        if %ErrorLevel% NEQ 0 (
            set ErrorLevel=10
            goto End;
        )
    )

    if %NeedCopyToProjectConfigurationDir% NEQ 0 (
        call:CopyCSharpProjectFiles "%CompileOutDir%"  "%ProductRootBin%\%BaseDotnetDir%\%ProjectConfiguration%"
        if %ErrorLevel% NEQ 0 (
            set ErrorLevel=20
            goto End;
        )
    )

    if %NeedCopyToSPVersionDir% NEQ 0 (
        rem SP2016
        call:CopyCSharpProjectFiles "%CompileOutDir%" "%ProductRootBin%\%BaseDotnetDir%\SP2016%ProjectReleaseOrDebug%"
        if %ErrorLevel% NEQ 0 (
            set ErrorLevel=20
            goto End;
        )

        rem SP2019
        call:CopyCSharpProjectFiles "%CompileOutDir%" "%ProductRootBin%\%BaseDotnetDir%\SP2019%ProjectReleaseOrDebug%"
        if %ErrorLevel% NEQ 0 (
            set ErrorLevel=30
            goto End;
        )
    )
)
rem ----------------End-----------------------

goto End;

rem ----------------Functions-----------------------
:CopyCSharpProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    echo Begin copy from %SourcePath% to %DestinationPath%
    if not exist "%DestinationPath%" mkdir "%DestinationPath%"
    rem Must output files: .exe, .pdb, others is option
    if %ErrorLevel% NEQ 0 (
        echo CommandError, try to make output directory failed, please check. ProjectType=%ProjectType%, SourcePath=%SourcePath%, DestinationPath=%DestinationPath%
        set ErrorLevel=200
    ) else (
        if %ProjectType% EQU EXE (
            call:CopyCSharpExeProjectFiles "%SourcePath%" "%DestinationPath%"
        ) else if %ProjectType% EQU DLL (
            call:CopyCSharpDllProjectFiles "%SourcePath%" "%DestinationPath%"
        ) else (
            echo Unknown project type ProjectType=%ProjectType%
            set ErrorLevel=100
        )
    )
    echo End copy from %SourcePath% to %DestinationPath%
goto:eof

:CopyCSharpExeProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    echo Begin copy from %SourcePath% to %DestinationPath%
    rem Options output files, ignore error level: .xml, .ini
    echo EXE CSharp project options copy: copy /y "%SourcePath%\*.ini\xml\dll" "%DestinationPath%\"
    copy /y "%SourcePath%\*.ini" "%DestinationPath%\"
    copy /y "%SourcePath%\*.xml" "%DestinationPath%\"
    copy /y "%SourcePath%\*.dll" "%DestinationPath%\"
    echo EXE CSharp project option config copy: copy /y "%SourcePath%\Config\*.ini\xml" "%DestinationPath%\"
    copy /y "%SourcePath%\Config\*.ini" "%DestinationPath%\Config\"
    copy /y "%SourcePath%\Config\*.xml" "%DestinationPath%\Config\"
    set "ErrorLevel=0"
    rem Must output files
    echo EXE CSharp project must copy: copy /y "%SourcePath%\*.exe\pdb" "%DestinationPath%\"
    copy /y "%SourcePath%\*.exe" "%DestinationPath%\"
    copy /y "%SourcePath%\*.pdb" "%DestinationPath%\"
    echo End copy from %SourcePath% to %DestinationPath%
goto:eof

:CopyCSharpDllProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    rem Options output files, ignore error level: .xml, .ini
    echo DLL CSharp project option copy: copy /y "%SourcePath%\*.ini\xml" "%DestinationPath%\"
    copy /y "%SourcePath%\*.ini" "%DestinationPath%\"
    copy /y "%SourcePath%\*.xml" "%DestinationPath%\"
    echo DLL CSharp project option config copy: copy /y "%SourcePath%\Config\*.ini\xml" "%DestinationPath%\"
    copy /y "%SourcePath%\Config\*.ini" "%DestinationPath%\"
    copy /y "%SourcePath%\Config\*.xml" "%DestinationPath%\"
    set "ErrorLevel=0"
    rem Must output files
    echo DLL CSharp project Must copy: copy /y "%SourcePath%\*.dll\pdb" "%DestinationPath%\"
    copy /y "%SourcePath%\*.dll" "%DestinationPath%\"
    copy /y "%SourcePath%\*.pdb" "%DestinationPath%\"
goto:eof
rem ----------------End-----------------------


:End
if %ErrorLevel% NEQ 0 (
    echo Execute common CSharp post event failed with error level %ErrorLevel%, OrgErrorLevel=%OrgErrorLevel%
) else (
    echo Execute common CSharp post event success, revert OrgErrorLevel=%OrgErrorLevel%
    set ErrorLevel=%OrgErrorLevel%
)
