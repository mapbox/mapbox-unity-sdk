**An API token is required to access Mapbox services.**

## Signup

 [Sign up for a free account.](https://www.mapbox.com/studio/signup/)

## Configure API Access in Unity

Copy your token from: https://www.mapbox.com/studio/account/tokens/.

Click Mapbox—>Configure Access from the Unity Editor menu. Paste your token into the `Token` field. Assuming your token is valid, it will save to a text file in `StreamingAssets/MapboxAccess.text`. This file is ignored from the git repo to prevent contributors from accidentally comitting personal tokens. At runtime, the contents of this file will be read and cached for `FileSource` request purposes. See `MapboxAccess.cs` for details.

Now that your access has been configured, check out [the included examples](https://www.mapbox.com/mapbox-unity-sdk/docs/03-examples.html).