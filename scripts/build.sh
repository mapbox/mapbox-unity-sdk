#! /bin/sh

set -eu

project="sdkproject"

echo "Attempting to build $project for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
-batchmode \
-nographics \
-silent-crashes \
-logFile "$(pwd)/windows-build.log" \
-projectPath "$(pwd)/${project}" \
-buildWindowsPlayer "$(pwd)/Build/windows/${project}.exe" \
-stackTraceLogType Full \
-quit

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
-batchmode \
-nographics \
-silent -crashes \
-logFile "$(pwd)/mac-build.log" \
-projectPath "$(pwd)/${project}" \
-buildOSXUniversalPlayer "$(pwd)/Build/osx/${project}.app" \
-stackTraceLogType Full \
-quit

echo "Attempting to build $project for iOS"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
-batchmode \
-nographics \
-silent -crashes \
-logFile "$(pwd)/ios-build.log" \
-projectPath "$(pwd)/${project}" \
-buildTarget iOS \
-stackTraceLogType Full \
-quit

echo 'Logs from build'
cat "$(pwd)/windows-build.log"
cat "$(pwd)/mac-build.log"
cat "$(pwd)/ios-build.log"


echo 'Attempting to zip builds'
zip -r $(pwd)/Build/mac.zip $(pwd)/Build/osx/
zip -r $(pwd)/Build/windows.zip $(pwd)/Build/windows/
