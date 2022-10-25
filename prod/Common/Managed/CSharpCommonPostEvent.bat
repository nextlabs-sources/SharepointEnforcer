rem ProjectType, ProductRoot, CompileOutDir, ProductUnifyOutDir, NeedCopyToSPVersionArchiveDir

rem ProjectType: EXE, DLL
set ProjectType=%~1

rem ProductRoot, it is NLBUILDROOT
set ProductRoot=%~2

rem CompileOutDir compile output folder path, like Release_AnyCpu\
set CompileOutDir=%~3

rem Build unify out dir, like Release_Dotnet\
set ProductUnifyOutDir=%~4

rem echo NLBUILDROOT=%NLBUILDROOT%
echo ProductRoot=%ProductRoot%
echo CompileOutDir=%CompileOutDir%
echo ProjectType=%ProjectType%
echo ProductUnifyOutDir=%ProductUnifyOutDir%

rem Create out folders. Donot suggest using mkdir -p parameter, this maybe cover a mistake
if not exist "%ProductRoot%/Bin" mkdir "%ProductRoot%/Bin"
if not exist "%ProductRoot%/Bin/%ProductUnifyOutDir%" mkdir "%ProductRoot%/Bin/%ProductUnifyOutDir%"

rem Must output files: .exe, .pdb, others is option
if %ErrorLevel% NEQ 0 (
    echo CommandError, try to make output directory failed, please check. ProductRoot=%ProductRoot%, ProductUnifyOutDir=%ProductUnifyOutDir%
) else (
    if %ProjectType% EQU EXE (
        call:CopyCSharpExeProjectFiles "%CompileOutDir%" "%ProductRoot%/Bin/%ProductUnifyOutDir%"
    ) else if %ProjectType% EQU DLL (
        call:CopyCSharpDllProjectFiles "%CompileOutDir%" "%ProductRoot%/Bin/%ProductUnifyOutDir%"
    ) else (
        echo Unknown project type ProjectType=%ProjectType%
        set ErrorLevel 100
    )
)
echo Output.props PostBuildEvent end with error level %ErrorLevel%

if %ErrorLevel% NEQ 0 (
    goto Error;
) else (
    goto End;
)

:CopyCSharpExeProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    echo Begin copy from %SourcePath% to %DestinationPath%
    rem Options output files, ignore error level: .xml, .ini
    echo EXE CSharp project options copy: copy /y "%SourcePath%/*.ini/xml/dll" "%DestinationPath%"
    copy /y "%SourcePath%/*.ini" "%DestinationPath%"
    copy /y "%SourcePath%/*.xml" "%DestinationPath%"
    copy /y "%SourcePath%/*.dll" "%DestinationPath%"
    set "ErrorLevel=0"
    rem Must output files
    echo EXE CSharp project must copy: copy /y "%SourcePath%/*.exe/pdb" "%DestinationPath%"
    copy /y "%SourcePath%/*.exe" "%DestinationPath%"
    copy /y "%SourcePath%/*.pdb" "%DestinationPath%"
    echo End copy from %SourcePath% to %DestinationPath%
goto:eof

:CopyCSharpDllProjectFiles
    set SourcePath=%~1
    set DestinationPath=%~2
    rem Options output files, ignore error level: .xml, .ini
    echo DLL CSharp project option copy: copy /y "%SourcePath%/*.ini/xml" "%DestinationPath%"
    copy /y "%SourcePath%/*.ini" "%DestinationPath%"
    copy /y "%SourcePath%/*.xml" "%DestinationPath%"
    set "ErrorLevel=0"
    rem Must output files
    echo DLL CSharp project Must copy: copy /y "%SourcePath%/*.dll/pdb" "%DestinationPath%"
    copy /y "%SourcePath%/*.dll" "%DestinationPath%"
    copy /y "%SourcePath%/*.pdb" "%DestinationPath%"
goto:eof

:Error
echo Execute common CSharp post event failed

:End

echo Execute common CSharp post event end.