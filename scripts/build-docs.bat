@ECHO OFF
SETLOCAL
SET EL=0
ECHO ~~~~~~~~~~~~~~~~~~~ %~f0 ~~~~~~~~~~~~~~~~~~~

SET SERVE=

:NEXT-ARG
IF '%1'=='' GOTO ARGS-DONE
IF '%1'=='--serve' SET SERVE=--serve
SHIFT
GOTO NEXT-ARG
:ARGS-DONE

SET PATH=C:\Program Files\7-Zip;%PATH%
SET PATH=%CD%\docfx;%PATH%

WHERE 7z >NUL
IF %ERRORLEVEL% NEQ 0 (ECHO 7zip not found && GOTO ERROR)

IF NOT EXIST docfx.zip powershell -NoProfile -ExecutionPolicy unrestricted Invoke-WebRequest https://github.com/dotnet/docfx/releases/download/v2.14.1/docfx.zip -OutFile docfx.zip
IF %ERRORLEVEL% NEQ 0 (ECHO could not download docfx && GOTO ERROR)

IF NOT EXIST docfx 7z x docfx.zip -aoa -o%CD%\docfx | %windir%\system32\find "ing archive"
IF %ERRORLEVEL% NEQ 0 (ECHO could not extract docfx && GOTO ERROR)

docfx documentation\docfx_project\docfx.json %SERVE%
IF %ERRORLEVEL% NEQ 0 (ECHO could not create docs && GOTO ERROR)

:ERROR
SET EL=%ERRORLEVEL%
ECHO ~~~~~~~~~~~~~~~~~~~ ERROR %~f0 ~~~~~~~~~~~~~~~~~~~
ECHO ERRORLEVEL^: %EL%

:DONE
EXIT /b %EL%