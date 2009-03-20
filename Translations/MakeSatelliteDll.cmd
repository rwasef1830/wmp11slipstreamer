@Echo Off
setlocal enableextensions
If "%7"=="" goto usage
goto execute

:usage
Echo ** Resource DLL build script - by 3psil0N
Echo.
Echo %~nx0 [resourceDescFile] [strongNameKey] [assemblyName] 
Echo       [assemblyVersion] [culture] [companyName] [outputDir]
Echo.
Echo [resourceDescFile]: Describes resources to embed. Format per line:
Echo pathToResFile/namespace/baseName
SET ExitCode=2
goto end

:execute
goto setvars

:setvars
set resourceDescFile=%~1
set strongNameKey=%~2
set assemblyName=%~3
set assemblyVersion=%~4
set culture=%~5
set companyName=%~6
set outputDir=%~7
set resgenPath=%ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin\ResGen.exe
set alPath=%ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin\al.exe
set gsarPath=gsar.exe
set tempGenPath=%Temp%\%~n0%RANDOM%.tmp
goto checkDependencies

:checkDependencies
If not exist "%resgenPath%" goto resgenNotFound
If not exist "%alPath%" goto alNotFound
If not exist "%gsarPath%" goto gsarNotFound
goto parseDescFile

:resgenNotFound
Echo ** Could not locate RESGEN (Resource Generator) on this machine.
Echo ** Please edit this script to point to the location of this utility.
SET ExitCode=1
goto end

:alNotFound
Echo ** Could not locate AL (Assembly Linker) on this machine.
Echo ** Please edit this script to point to the location of this utility.
SET ExitCode=1
goto end

:gsarNotFound
Echo ** Could not locate GSAR (Global search and replace) on this machine.
Echo ** Please edit this script to point to the location of this utility.
SET ExitCode=1
goto end

:parseDescFile
If not exist "%resourceDescFile%" goto descFileNotFound 
If exist "%tempGenPath%" rd /s /q "%tempGenPath%"
FOR /F "usebackq tokens=1,2,3 delims=/" %%a IN (`type "%resourceDescFile%"`) DO CALL :genResFile "%%a" "%%b" "%%c"
goto genSatellite

:genSatellite
set tempCommandBuffer=%tempGenPath%\commandBuffer.txt
If exist "%tempCommandBuffer%" del "%tempCommandBuffer%"
Echo "%alPath%" /nologo /company:"%companyName%" /culture:"%culture%" /keyfile:"%strongNameKey%" /target:library /title:"%assemblyName% %culture% resources" /version:"%assemblyVersion%" /out:"%outputDir%\%assemblyName%.resources.dll" > "%tempCommandBuffer%"
FOR %%i IN ("%tempGenPath%\*.resources") DO Echo /embed:"%%i" >> "%tempCommandBuffer%"
gsar -s:x0d:x0a -r:x20 -o "%tempCommandBuffer%" 1>NUL
If errorlevel=1 PAUSE
FOR /F "usebackq tokens=*" %%i IN (`type %tempCommandBuffer%`) Do %%i
If errorlevel=1 PAUSE
del /Q "%tempCommandBuffer%"
goto end

:genResFile
set currResPath=%~1
set currNamespace=%~2
set currBaseName=%~3
If not exist "%tempGenPath%" mkdir "%tempGenPath%"
"%resgenPath%" "%currResPath%" "%tempGenPath%\%currNamespace%.%currBaseName%.%culture%.resources" 1>NUL
If errorlevel=1 PAUSE
goto :EOF

:descFileNotFound
Echo ** The resource description file could not be found.
SET ExitCode=1
goto end

:end
endlocal
EXIT /B %ExitCode%
