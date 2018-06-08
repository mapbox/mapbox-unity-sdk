@ECHO OFF
SETLOCAL

SET SDK_PATH=sdkproject\Assets\Mapbox\Core

ECHO deleting existing plugins
IF EXIST %SDK_PATH%\Plugins\Mapbox RD /Q /S %SDK_PATH%\Plugins\Mapbox
IF %ERRORLEVEL% NEQ 0 ECHO error during removal of existing Mapbox plugins && EXIT /B 1
IF EXIST %SDK_PATH%\Plugins\ThirdParty RD /Q /S %SDK_PATH%\Plugins\ThirdParty
IF %ERRORLEVEL% NEQ 0 ECHO error during removal of existing third party plugins && EXIT /B 1

REM file not to copy
ECHO project.json>>x.txt
ECHO project.lock.json>>x.txt
ECHO .csproj>>x.txt
ECHO .snk>>x.txt
ECHO packages.config>>x.txt
ECHO AssemblyInfoVersion.cs>>x.txt
ECHO SharedAssemblyInfo.cs>>x.txt
ECHO \Properties\>>x.txt
ECHO \Documentation\>>x.txt
ECHO \Mono\>>x.txt
ECHO \Bench\>>x.txt
ECHO \DemoConsoleApp\>>x.txt
ECHO \VectorTiles.Tests\>>x.txt
ECHO \VerifyNetFrameworkVersion\>>x.txt

ECHO ---- copying vector-tile-cs
xcopy /S /R /E /Y dependencies\vector-tile-cs\* %SDK_PATH%\Plugins\Mapbox\vector-tile-cs\ /EXCLUDE:x.txt
IF %ERRORLEVEL% NEQ 0 ECHO error during copying vector-tile-cs && EXIT /B 1

ECHO ---- copying Mapbox.IO.Compression
xcopy /S /R /E /Y dependencies\Mapbox.IO.Compression-unity\* %SDK_PATH%\Plugins\ThirdParty\Mapbox.IO.Compression\ /EXCLUDE:x.txt
IF %ERRORLEVEL% NEQ 0 ECHO error during copying Mapbox.IO.Compression && EXIT /B 1

ECHO ---- copying Mapbox.Json
xcopy /S /R /E /Y dependencies\Mapbox.Json\* %SDK_PATH%\Plugins\ThirdParty\Mapbox.Json\ /EXCLUDE:x.txt
IF %ERRORLEVEL% NEQ 0 ECHO error during copying Mapbox.Json && EXIT /B 1

ECHO copying aux files
COPY /Y utils\link.xml %SDK_PATH%\Plugins\
IF %ERRORLEVEL% NEQ 0 ECHO error during copying link.xml && EXIT /B 1


REM clean up and delete temporary directory
IF EXIST x.txt DEL /F x.txt
IF %ERRORLEVEL% NEQ 0 ECHO could not delete temporary xcopy config: x.txt && EXIT /B 1

ECHO DONE!
