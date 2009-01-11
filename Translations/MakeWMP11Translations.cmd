@Echo off
:start
setlocal enableextensions
goto setvars

:setvars
set companyName=Boooggy and n7Epsilon
set strongNameKey=..\WMP11Slipstreamer\WMP11SlipstreamerKey.snk
set assemblyName=WMP11Slipstreamer
set assemblyVersion=1.1.0.0
set tempGenPath=%Temp%\%~n0%Random%.tmp
goto executeLoop

:executeLoop
If exist "%tempGenPath%" rd /s /q "%tempGenPath%"
FOR /F "usebackq tokens=5" %%i IN (`dir .\Source ^| FIND "<DIR>"`) DO (
	If not "%%i"=="." If not "%%i"==".." CALL :genResource "%%i"
)
If exist "%tempGenPath%" rd /s /q "%tempGenPath%"
goto end

:genResource
set currCulturePath=%~dp0Source\%~1
set currCulture=%~n1
set currOutputDir=%~dp0Output\%currCulture%
If not exist "%tempGenPath%" mkdir "%tempGenPath%"
If exist "%currOutputDir%" rd /s /q "%currOutputDir%"
mkdir "%currOutputDir%"
set currResDescPath=%tempGenPath%\resDesc.txt
del /Q "%currResDescPath%" 1>NUL 2>NUL 
(Echo %currCulturePath%\ParsersMsg.resx/Epsilon.n7Framework.Epsilon.Parsers/ParsersMsg
Echo %currCulturePath%\SlipstreamersMsg.resx/Epsilon.n7Framework.Epsilon.Slipstreamers/SlipstreamersMsg
Echo %currCulturePath%\Msg.resx/Epsilon.WMP11Slipstreamer.Localization/Msg
) >> "%currResDescPath%"
CALL MakeSatelliteDll.cmd "%currResDescPath%" "%strongNameKey%" "%assemblyName%" "%assemblyVersion%" "%currCulture%" "%companyName%" "%currOutputDir%"
del /Q "%currResDescPath%" 1>NUL
goto :EOF

:end
endlocal
