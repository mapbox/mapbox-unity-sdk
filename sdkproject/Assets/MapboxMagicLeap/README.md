# README-MagicLeap

These are MagicLeap compatible examples built using the Mapbox Unity SDK. For faster setup, download the [MagicMaps unitypackage](www.mapbox.com/ar) (recommended).

## Magic Leap setup
Follow Magic Leap's [setup instructions for Unity](https://creator.magicleap.com/learn/guides/sdk-unity-installing-and-configuring)
  1. Install the [Magic Leap Package Manager](https://creator.magicleap.com/downloads/lumin-sdk/overview).
  1. Install the [Unity developer preview](https://unity3d.com/partners/magicleap). `2018.1.6f1 MLTP7` at the time of writing. 
  1. Manually import Magicleap’s Unity SDK v0.16.0 to the project. 
     - By default, the Package Manager saves the unitypackage to `~/MagicLeap/tools/unity/v0.16.0/`. You can also press the `Open folder` button in the package manager.
     - Once complete, you should see an `Assets/MagicLeap` folder. If you don't, namespace errors may occur.
     - In this repository, `.gitignore` is already setup for `sdkproject/Assets/MagicLeap`

## When Copying/Cloning this Repo
This section isn’t necessary for those importing the [MagicMaps unitypackage](www.mapbox.com/ar) (recommended).
1. Delete these folders:
   - `MapboxAR`
   - `UnityARInterface`
   - `UnityARKitPlugin`
   - `GoogleARCore`
1. Run `./update-mapbox-unity-sdk-core.sh`
1. Navigate to `Mapbox > Core > Plugins > ThirdParty`
3. Enable the `Lumin` platform on for `Mapbox.IO.Compression` and `Mapbox.Json`


## Project Setup
1. Configure your [Mapbox Access Token](https://www.mapbox.com/install/unity/permission/)
2. In `File > Build Settings`, select the Lumin OS target
2. Toggle `Sign package` on
3. Add SDK path to `Lumin SDK Location`
   - Default: `/Users/<you>/MagicLeap/mlsdk/<vxx.xx.xx>`
3. Add `MapboxMagicLeap/Scenes/MagicLeap-Terrain` to the Scenes in Build.
4. Click `Player Settings…` and change `Bundle identifier` to something else (like `com.company.yourname`)
5. Scroll down to `Publishing Settings` and add your certificate to `ML Certificate`
   - Get your certificate and private key [here](https://creator.magicleap.com/dashboard/8167cd1f-9248-413c-9bcd-e6b2502f2f5f/certificates).
   - Private key must be in the same folder.
6. In `Publish Settings`, enable these default privileges:
   1. `Internet`
   2. `LocalAreaNetowrk`
   3. `WorldReconstruction`
   4. `ControllerPose`

## Zero-Iterate

1. `Magic Leap > Enable zero iteration` in unity (restart required)
2. Plug in and turn on your device.
2. `Start device` in MagicLeap remote
3. Press play in Unity

## Deploying

1. `Build Settings > Build and Run` from Unity to deploy immediately when the device is plugged in and on.
