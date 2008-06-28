@Echo Off
set satelliteVer=1.0.0.0
set company=boooggy and n7Epsilon

If /I "%~1"=="" goto usage
If /I "%~2"=="" goto usage
If /I "%~3"=="" goto usage
If /I "%~4"=="" goto usage
goto work

:usage
Echo.
Echo %~nx0 [resFile] [strongNameKeyFile] [culture] [MainAssemblyNameOnly]
goto end

:work
If not exist "%~1" goto errResNotFound
If not exist "%~2" goto errSnkNotFound
goto confirm

:errResNotFound
Echo Resource file could not be found.
goto end

:errSnkNotFound
Echo Strong name key file could not be found.
goto end

:confirm
set assemblyName=%~4
set culture=%~3
set snk=%~2
set res=%~1
Echo.
Echo Culture: %culture%
Echo Strong Name Key File: "%snk%"
Echo Resources File: "%res%"
set output=%CD%\%culture%\%assemblyName%.resources.dll
Echo Output File: "%output%"
Echo.
set /p Response=Do you want to continue ? [Yes, No] 
If /I "%Response%"=="Y" goto buildAssembly
goto end

:buildAssembly
Echo.
"%WinDir%\Microsoft.NET\Framework\v2.0.50727\al.exe" /embed:"%res%" /culture:%culture% /keyfile:"%snk%" /version:"%satelliteVer%" /out:"%output%" /description:"%assemblyName% %culture% resources" /company:"%company%"

:end