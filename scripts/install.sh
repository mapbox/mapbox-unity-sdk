#! /bin/sh

set -eu

df -h

BASE_URL=https://download.unity3d.com/download_unity
HASH=472613c02cf7
VERSION=2017.1.0f3

download() {
  file=$1
  url="$BASE_URL/$HASH/$package"
  localFile=`basename "$package"`

  if [ ! -f "$localFile" ]; then echo "Downloading $url"  && curl -o "$localFile" "$url"; fi
}

install() {
  package=$1
  download "$package"

  echo "Installing "`basename "$package"`
  sudo installer -dumplog -package `basename "$package"` -target /
}

# See $BASE_URL/$HASH/unity-$VERSION-$PLATFORM.ini for complete list
# of available packages, where PLATFORM is `osx` or `win`

install "MacEditorInstaller/Unity-$VERSION.pkg"
wait
install "MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg"
wait
install "MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-$VERSION.pkg"
