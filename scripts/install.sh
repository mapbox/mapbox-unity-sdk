#! /bin/sh

set -eu

df -h

BASE_URL=https://download.unity3d.com/download_unity
HASH=$1
VERSION=$2

download() {
  file=$1
  url="$BASE_URL/$HASH/$file"
  localFile=`basename "$file"`

  if [ ! -f "$localFile" ]; then echo "Downloading $url"  && curl -o "$localFile" "$url"; fi
}

install() {
  package=$1

  echo "Installing "`basename "$package"`
  sudo installer -dumplog -package `basename "$package"` -target /
}

# See $BASE_URL/$HASH/unity-$VERSION-$PLATFORM.ini for complete list
# of available packages, where PLATFORM is `osx` or `win`

if [ ! -f "Unity-Mac.pkg" ]; then cd .. && cd project && install "Unity-Mac.pkg"; else download "MacEditorInstaller/Unity-$VERSION.pkg" && install "MacEditorInstaller/Unity-$VERSION.pkg"; fi
wait
if [ ! -f "UnitySetup-Windows-Support-for-Editor.pkg" ]; then cd .. && cd project && install "UnitySetup-Windows-Support-for-Editor.pkg"; else download "MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg" && install "MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg"; fi
wait
if [ ! -f "UnitySetup-iOS-Support-for-Editor.pkg" ]; then cd .. && cd project && install "UnitySetup-iOS-Support-for-Editor.pkg"; else download "MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-$VERSION.pkg" && install "MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-$VERSION.pkg"; fi
