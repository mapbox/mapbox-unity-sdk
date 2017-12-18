# Mapbox-unity-sdk
### For Unity 2017.1+  

(for 5.4x compatible versions, please use [this commit](https://github.com/mapbox/mapbox-unity-sdk/releases/tag/Last-official-Unity5x-support))

Tools for using Mapbox APIs with C# / Unity. If you'd like to contribute to the project, [read CONTRIBUTING.md](CONTRIBUTING.md).

This repo contains:
- Unity specific tools and libraries for processing Mapbox data
  - Example projects using Mapbox Unity SDK
  - DocFX project for generateing API documentation
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

# Dependencies
This project includes git submodule dependencies outlined in [this gitmodules](https://github.com/mapbox/mapbox-unity-sdk/blob/develop/.gitmodules) file.

To install/update the dependencies after `git clone`/`git pull` run `update-mapbox-unity-sdk-core.bat` or `update-mapbox-unity-sdk-core.sh` (depending on your OS).

These repos are actively developed and maintained:
- https://github.com/mapbox/vector-tile-cs

**NOTE: As of May 16, 2017, the https://github.com/mapbox/mapbox-sdk-cs repo has been merged into this repo. These source files are now located here: `/sdkproject/Assets/Mapbox/Core/mapbox-sdk-cs`. Core `cs` changes will be backported on a case-by-case basis.**

# Building a Unity Package
To build a Unity Package for import into your own project from the included `sdkproject`:
1. Select `Mapbox` folder in the project view.
2. Right-click and choose `Export Package...`.

![screen shot 2017-05-26 at 1 14 01 pm](https://cloud.githubusercontent.com/assets/23202691/26509552/7b536a6c-4216-11e7-9f50-b4b461fa73b8.png)

3. Uncheck `Include Dependencies`.

![screen shot 2017-05-26 at 1 14 55 pm](https://cloud.githubusercontent.com/assets/23202691/26509585/9d9677c2-4216-11e7-82ae-c34d150d6d5c.png)

4. Click `Export` and choose a location.
