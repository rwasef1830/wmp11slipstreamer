@Echo Off
If "%1"=="" goto usage
If "%2"=="" goto usage
If "%3"=="" goto usage
If "%4"=="" goto usage
goto execute

:usage
Echo %~nx0 [strongNameKey] [baseName] [assemblyName] [assemblyVersion]
goto end

:execute
If not defined companyName SET companyName=Company
set assemblyVersion=%~4
Echo.
Echo ** 3psil0N Resource Dll Compiler
Echo ** Compiling resource dlls from "Source" subfolder
Echo ** Original .resx file must be in Localization subfolder of parent.
Echo.
Echo * Company: "%companyName%"
Echo * Satellite assembly version: "%assemblyVersion%"
Echo.
PAUSE
Echo.
If not exist "%~1" goto snkNotFound
FOR /R "%~dp0\Source" %%i in (*.resx) DO call :compileResDll "%%i" "%~1" "%~2" "%~3"
Echo.
Echo ** All done!
goto end

:snkNotFound
Echo Strong name key file not found.
goto end

:compileResDll
set resXPath=%~1
set cultureName=%~n1
set outputDir=%~dp0\Output\%cultureName%
set snkFile=%~2
set baseName=%~3
set assemblyName=%~4

Echo Compiling resource dll: %cultureName%
If exist "%outputDir%" rd /s /q "%outputDir%"
mkdir "%outputDir%"
FOR /F "usebackq tokens=2" %%i IN (`type "%~dp0\..\EntryPoint.cs" ^| find "namespace"`) do set resourcesBaseName=%%i
If not defined resourcesBaseName Echo Cannot find namespace && goto :EOF
set outputResourcesPath=%outputDir%\%resourcesBaseName%.Localization.%baseName%.%cultureName%.resources
"%ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin\ResGen.exe" "%resXPath%" "%outputResourcesPath%" 1>NUL
If errorlevel = 1 PAUSE
set description=%assemblyName% %cultureName% resources
"%WinDir%\Microsoft.NET\Framework\v2.0.50727\al.exe" /nologo /embed:"%outputResourcesPath%" /company:"%companyName%" /culture:"%cultureName%" /keyfile:"%snkFile%" /target:library /title:"%description%" /version:"%assemblyVersion%" /out:"%outputDir%\%assemblyName%.resources.dll"
If errorlevel = 1 PAUSE
del "%outputResourcesPath%"
goto :EOF

:end
