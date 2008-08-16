@Echo Off
set ProjectDir=%~1
set TargetName=%~3
set TargetPath=%ProjectDir%%~2%TargetName%
set KeyPath=%ProjectDir%%~4
If "%4"=="" goto error
Echo ** Performing post-build tasks...
Echo ** Embedding Vista manifest...
mt.exe -nologo -manifest "%ProjectDir%%TargetName%.manifest" -outputresource:"%TargetPath%;#1"
If %errorlevel% gtr 0 set lasterror=%errorlevel%
If defined lasterror goto exitonerror
Echo ** Re-signing executable...
sn.exe -q -R "%TargetPath%" "%KeyPath%"
If %errorlevel% gtr 0 set lasterror=%errorlevel%
If defined lasterror goto exitonerror
goto normalexit

:exitonerror
Exit /B %lasterror%

:error
Echo Incorrect number of parameters !
Exit /B 2

:normalexit
Exit /B 0