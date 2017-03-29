# mapbox-sdk-unity

Tools for using Mapbox APIs with C# / Unity. If you'd like to contribute to the project, [read CONTRIBUTING.md](https://github.com/mapbox/mapbox-sdk-unity/blob/master/CONTRIBUTING.md).
Tools for using Mapbox with Unity.

## Build `unitypackage`

### On AppVeyor

- change version of `MapboxSDKforUnityCore` in `packages.config` to the one needed
- push
  - AppVeyor creates the `unitypackage` and it can be downloaded from the `Artifacts` tab, eg https://ci.appveyor.com/project/Mapbox/mapbox-sdk-unity/build/1.0.215/artifacts

### Locally (currently Windows only)

- run `update-mapbox-unity-sdk-core.bat`:
  - eg `update-mapbox-unity-sdk-core.bat 1.0.0-alpha07`
  - this write a new `packages.config`
  - install the package
  - copies its content into `sdkproject`
- run `UnityPackager.exe unitypackage.config sdkproject\Assets\ mapbox-unity-sdk_LATEST.unitypackage`
