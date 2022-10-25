@echo off

set OrgErrorLevel=%ErrorLevel%
echo Begin execute deploy postbuild event, backup OrgErrorLevel=%OrgErrorLevel%
set ErrorLevel=0


rem SolutionDir, like D:\SPE\prod\
set SolutionDir=%~1

set "BinName=\BIN1"
set "ImagesName=\Images"
set "LayoutsName=\Layouts"
set "ResourcesName=\Resources"
set "Resources1Name=\Resources1"
set "SharePointEnforcerName=\SharePoint Enforcer"
set "fearturemanagerName=\featuremanager"

set "BIN1Path=%SolutionDir%%BinName%"
set "ImagesPath=%SolutionDir%%ImagesName%"
set "LayoutsPath=%SolutionDir%%LayoutsName%"
set "SharePointEnforcerPath=%BIN1Path%%SharePointEnforcerName%"

set "imagesfearturemanagerPath=%ImagesPath%%fearturemanagerName%"
set "layoutsfearturemanager=%LayoutsPath%%fearturemanagerName%"

set "asmx=\*.asmx"
set "aspx=\*.aspx"
set "cs=\*.cs"
set "xml=\*.xml"
set "ico=\*.ico"
set "jpg=\*.jpg"

@REM rmdir /s/q %SharePointEnforcerPath%
@REM rmdir /s/q %imagesfearturemanagerPath%
@REM rmdir /s/q %layoutsfearturemanager%

@REM del %LayoutsPath%%asmx% %LayoutsPath%%aspx% %LayoutsPath%%cs% %LayoutsPath%%xml% %LayoutsPath%%ico% %LayoutsPath%%jpg%
