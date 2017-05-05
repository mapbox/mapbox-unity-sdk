#!/usr/bin/env bash

set -eu

echo "fetching submodules..."
git submodule update --init --recursive

SDK_PATH=sdkproject/Assets/Mapbox/Core

echo "deleting existing mapbox-sdk-cs..."
if [ -d "$SDK_PATH/mapbox-sdk-cs" ]; then rm -rf $SDK_PATH/mapbox-sdk-cs; fi

echo "deleting existing plugins..."
if [ -d "$SDK_PATH/Plugins/Mapbox" ]; then rm -rf $SDK_PATH/Plugins/Mapbox; fi
if [ -d "$SDK_PATH/Plugins/ThirdParty" ]; then rm -rf $SDK_PATH/Plugins/ThirdParty; fi

# exclude copying these files
echo "*project.json" >> x.txt
echo "*project.lock.json" >> x.txt
echo "*.csproj" >> x.txt
echo "*.snk" >> x.txt
echo "*packages.config" >> x.txt
echo "*AssemblyInfoVersion.cs" >> x.txt
echo "*SharedAssemblyInfo.cs" >> x.txt
echo "Properties" >> x.txt
echo "Documentation" >> x.txt
echo "Mono" >> x.txt
echo "Bench" >> x.txt
echo "DemoConsoleApp" >> x.txt
echo "VectorTiles.Tests" >> x.txt
echo "VerifyNetFrameworkVersion" >> x.txt

echo "copying mapbox-sdk-cs..."
mkdir -p $SDK_PATH/mapbox-sdk-cs
rsync -av --exclude-from=x.txt ./dependencies/mapbox-sdk-cs/src/ $SDK_PATH/mapbox-sdk-cs/

echo "copying vector-tile-cs..."
mkdir -p $SDK_PATH/Plugins/Mapbox/vector-tile-cs/
rsync -av --exclude-from=x.txt ./dependencies/vector-tile-cs/src/ $SDK_PATH/Plugins/Mapbox/vector-tile-cs/

echo "copying Mapbox.IO.Compression..."
mkdir -p $SDK_PATH/Plugins/ThirdParty/Mapbox.IO.Compression/
rsync -av --exclude-from=x.txt ./dependencies/Mapbox.IO.Compression-unity/src/Mapbox.IO.Compression.Shared/ $SDK_PATH/Plugins/ThirdParty/Mapbox.IO.Compression/

echo "copying Mapbox.Json..."
mkdir -p $SDK_PATH/Plugins/ThirdParty/Mapbox.Json/
rsync -av --exclude-from=x.txt ./dependencies/Mapbox.Json/ $SDK_PATH/Plugins/ThirdParty/Mapbox.Json/

echo "copying Triangle.NET..."
mkdir -p $SDK_PATH/Plugins/ThirdParty/Triangle.NET/
rsync -av --exclude-from=x.txt ./dependencies/triangle.net-uwp/Triangle.NET/Triangle/ $SDK_PATH/Plugins/ThirdParty/Triangle.NET/

echo "copying aux files..."
cp -v ./utils/link.xml $SDK_PATH/Plugins/

echo "cleaning up..."
rm x.txt

echo "DONE"
