@echo off

set OrgErrorLevel=%ErrorLevel%
echo Begin execute common CPP post event, backup OrgErrorLevel=%OrgErrorLevel%
set ErrorLevel=0

rem ProjectType, ProductRoot, CompileOutDir, ProjectConfiguration, ProjectPlatformTarget

rem ProjectType: EXE, DLL, LIB
set ProjectType=%~1

rem ProductRoot, it is NLBUILDROOT
set ProductRoot=%~2

rem CompileOutDir compile output folder path, like Release_win_x86\
set CompileOutDir=%~3

rem Build unify out dir, like Release, Debug
set ProjectConfiguration=%~4

rem Build unify out dir, like x86, x64
set ProjectPlatform=%~5

set ProductUnifyOutDir=%ProjectConfiguration%_win_%ProjectPlatformTarget%

echo ProductRoot=%ProductRoot%
echo CompileOutDir=%CompileOutDir%
echo ProjectType=%ProjectType%
echo ProjectConfiguration=%ProjectConfiguration%
echo ProjectPlatform=%ProjectPlatform%
echo ProductUnifyOutDir=%ProductUnifyOutDir%

if /i %ProjectPlatform% EQU Win32 (
    set ProjectPlatformTarget=x86
)
echo ProjectPlatformTarget=%ProjectPlatformTarget%


rem Create out folders. Donot suggest using mkdir -p parameter, this maybe cover a mistake
if not exist "%ProductRoot%/Bin" mkdir "%ProductRoot%/Bin"
if not exist "%ProductRoot%/Bin/%ProductUnifyOutDir%" mkdir "%ProductRoot%/Bin/%ProductUnifyOutDir%"

rem Must output files: .exe, .pdb, others is option
if %ErrorLevel% NEQ 0 (
    echo CommandError, try to make output directory failed, please check. ProductRoot=%ProductRoot%, ProductUnifyOutDir=%ProductUnifyOutDir%
) else (
    if %ProjectType% EQU EXE (
        call:CopyCppExeProjectFiles "%CompileOutDir%" "%ProductRoot%/Bin/%ProductUnifyOutDir%"
    ) else if %ProjectType% EQU DLL (
        call:CopyCppDllProjectFiles "%CompileOutDir%" "%ProductRoot%/Bin/%ProductUnifyOutDir%"
    ) else if %ProjectType% EQU DLL (
        call:CopyCppLibProjectFiles "%CompileOutDir%" "%ProductRoot%/Bin/%ProductUnifyOutDir%"
    ) else (
        echo Unknown project type ProjectType=%ProjectType%
        set ErrorLevel 100
    )
)

goto End

rem .xml, .ini, .h .dll, .lib. .exe, .pdb
:CopyCppExeProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    echo Begin copy from %SourcePath% to %DestinationPath%
    rem Options output files, ignore error level
    echo EXE CSharp project options copy: copy /y "%SourcePath%/*.ini/xml/h/dll/lib" "%DestinationPath%"
    copy /y "%SourcePath%/*.ini" "%DestinationPath%"
    copy /y "%SourcePath%/*.xml" "%DestinationPath%"
    copy /y "%SourcePath%/*.h" "%DestinationPath%"
    copy /y "%SourcePath%/*.dll" "%DestinationPath%"
    copy /y "%SourcePath%/*.lib" "%DestinationPath%"
    set "ErrorLevel=0"
    rem Must output files
    echo EXE CSharp project must copy: copy /y "%SourcePath%/*.exe/pdb" "%DestinationPath%"
    copy /y "%SourcePath%/*.exe" "%DestinationPath%"
    copy /y "%SourcePath%/*.pdb" "%DestinationPath%"
    echo End copy from %SourcePath% to %DestinationPath%
goto:eof

:CopyCppDllProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    rem Options output files, ignore error level
    echo DLL CSharp project option copy: copy /y "%SourcePath%/*.ini/xml/exe" "%DestinationPath%"
    copy /y "%SourcePath%/*.ini" "%DestinationPath%"
    copy /y "%SourcePath%/*.xml" "%DestinationPath%"
    copy /y "%SourcePath%/*.exe" "%DestinationPath%"
    set "ErrorLevel=0"
    rem Must output files
    echo DLL CSharp project Must copy: copy /y "%SourcePath%/*.h/lib/dll/pdb" "%DestinationPath%"
    copy /y "%SourcePath%/*.h" "%DestinationPath%"
    copy /y "%SourcePath%/*.lib" "%DestinationPath%"
    copy /y "%SourcePath%/*.dll" "%DestinationPath%"
    copy /y "%SourcePath%/*.pdb" "%DestinationPath%"
goto:eof

:CopyCppLibProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    rem Options output files, ignore error level
    echo DLL CSharp project option copy: copy /y "%SourcePath%/*.ini/xml/dll/exe" "%DestinationPath%"
    copy /y "%SourcePath%/*.ini" "%DestinationPath%"
    copy /y "%SourcePath%/*.xml" "%DestinationPath%"
    copy /y "%SourcePath%/*.dll" "%DestinationPath%"
    copy /y "%SourcePath%/*.exe" "%DestinationPath%"
    set "ErrorLevel=0"
    rem Must output files
    echo DLL CSharp project Must copy: copy /y "%SourcePath%/*.h/lib/pdb" "%DestinationPath%"
    copy /y "%SourcePath%/*.h" "%DestinationPath%"
    copy /y "%SourcePath%/*.lib" "%DestinationPath%"
    copy /y "%SourcePath%/*.pdb" "%DestinationPath%"
goto:eof


:End
if %ErrorLevel% NEQ 0 (
    echo Execute common CPP post event failed with error level %ErrorLevel%, OrgErrorLevel=%OrgErrorLevel%
) else (
    echo Execute common CPP post event success, revert OrgErrorLevel=%OrgErrorLevel%
    set ErrorLevel=%OrgErrorLevel%
)
