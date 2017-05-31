# Mapbox-unity-sdk
### For Unity 5.4 and above 

Tools for using Mapbox APIs with C# / Unity. If you'd like to contribute to the project, [read CONTRIBUTING.md](CONTRIBUTING.md).

This repo contains:
  - Unity specific tools and libraries for processing Mapbox data
  - Example projects using Mapbox Unity SDK
  - DocFX project for generateing API documentation
  - Written manuals and guides

# Documentation
Documentation is generated using DocFX from this repo and is hosted at: https://www.mapbox.com/mapbox-unity-sdk/.

# Dependencies
This project includes git submodule dependcies outlined in [this gitmodules](https://github.com/mapbox/mapbox-unity-sdk/blob/develop/.gitmodules) file.

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
