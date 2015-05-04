@echo off
cd %~dp0

SETLOCAL
SET NUGET_FOLDER=%LocalAppData%\NuGet
SET CACHED_NUGET=%NUGET_FOLDER%\NuGet.exe

IF EXIST %CACHED_NUGET% goto getnuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST %NUGET_FOLDER% md %NUGET_FOLDER%
@powershell -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CACHED_NUGET%'"

:getnuget
IF EXIST build\NuGet.exe goto run
IF NOT EXIST build md build
copy %CACHED_NUGET% build\NuGet.exe > nul

:run
IF EXIST artifacts rd artifacts /s /q
md artifacts
build\NuGet.exe pack FAKEX.nuspec -o artifacts -version "%BUILD_VERSION%" -NoPackageAnalysis