#!/usr/bin/env bash
sudo su jenkins
./update-mapbox-unity-sdk-core.sh
sudo /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "${WORKSPACE}/sdkproject" -quit -batchmode -executeMethod CreateBuild.BuildNow -stackTraceLogType Full -logFile "MapboxBuildLog.txt"
sudo cp  -r "${WORKSPACE}/Build/" "${JENKINS_HOME}/jobs/${JOB_NAME}/builds/${BUILD_NUMBER}"
sudo rm -rf "${WORKSPACE}/Build/"