@Echo Off
setlocal
PUSHD %dp0
set entriesfile=entries_wmp11_combined.ini
set dirs=xp xpmce xp64 2k3
set passfile=pass.txt
set destination=embed

:readpasses
FOR /F "tokens=1,2" %%i IN (%passfile%) DO ((set entrieskey=%%i) && (set infkey=%%j))
Echo.
Echo/Entries: (%entrieskey%)
Echo/INFs:    (%infkey%)
Echo.
Echo ** Compressing INFs...
for %%i IN (%dirs%) DO (call cab "%%i\%%i.cab" "%%i\*.inf" "%%i\*.acm")
Echo ** Deleting repos...
del "%destination%\repos*."
Echo ** Encrypting entries file...
AESCrypt /E /in:"%entriesfile%" /out:"%destination%\repository1" /pass:"%entrieskey%"
Echo ** Encrypting CABs...
AESCrypt /E /in:"xp\xp.cab" /pass:"%infkey%" /out:"%destination%\repository3"
AESCrypt /E /in:"xpmce\xpmce.cab" /pass:"%infkey%" /out:"%destination%\repository4"
AESCrypt /E /in:"2k3\2k3.cab" /pass:"%infkey%" /out:"%destination%\repository5"
AESCrypt /E /in:"xp64\xp64.cab" /pass:"%infkey%" /out:"%destination%\repository7"
for %%i IN (%dirs%) DO (del %%i\%%i.cab)
:end
endlocal
POPD