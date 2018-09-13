# Mapbox-unity-sdk
### For Unity 2017.1.2+

**AR support requires Unity 2017.3+, Android 7+ (Nougat), iOS 11.3**

Find the [AR specific README here](README-AR.md).

If AR support is not needed these subfolders of `sdkproject/Assets/` maybe deleted:
* MapboxAR
* UnityARInterface
* GoogleARCore
* UnityARKitPlugin

(for 5.4x compatible versions, please use [this commit](https://github.com/mapbox/mapbox-unity-sdk/releases/tag/Last-official-Unity5x-support))

Tools for using Mapbox APIs with C# / Unity. If you'd like to contribute to the project, [read CONTRIBUTING.md](CONTRIBUTING.md).

This repo contains:
- Unity specific tools and libraries for processing Mapbox data
  - Example projects using Mapbox Maps SDK for Unity
  - DocFX project for generating API documentation
  - Written manuals and guides

# Getting started

## Versioned SDK (easy, current stable release)

* Download `unitypackage` from https://www.mapbox.com/unity-sdk/#download
* If you've installed the SDK before, delete `Assets/Mapbox` folder from your project
* Within Unity: `Assets -> Import Package -> Custom Package... -> All -> Import`, wait :smirk:

## From this Repository (advanced, latest development)

**Downloading the repo as a `zip` does not work!**

```
git clone git@github.com:mapbox/mapbox-unity-sdk.git
cd mapbox-unity-sdk
```

Windows: `update-mapbox-unity-sdk-core.bat`

Linux/Mac: `./update-mapbox-unity-sdk-core.sh`


# Documentation
Documentation is generated using DocFX from this repo and is hosted at: https://www.mapbox.com/mapbox-unity-sdk/.

# Building a Unity Package
To build a Unity Package for import into your own project from the included `sdkproject`:
1. Select `Mapbox` folder in the project view.
2. Right-click and choose `Export Package...`.

![screen shot 2017-05-26 at 1 14 01 pm](https://cloud.githubusercontent.com/assets/23202691/26509552/7b536a6c-4216-11e7-9f50-b4b461fa73b8.png)

3. Uncheck `Include Dependencies`.

![screen shot 2017-05-26 at 1 14 55 pm](https://cloud.githubusercontent.com/assets/23202691/26509585/9d9677c2-4216-11e7-82ae-c34d150d6d5c.png)

4. Click `Export` and choose a location.
<!--<
