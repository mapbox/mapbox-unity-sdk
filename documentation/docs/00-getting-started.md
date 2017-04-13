# Getting Started

* [Get a Mapbox API token](01-mapbox-api-token.md)
* [Check out known issues](02-known-issues.md)
* [Check out C# API documentation](https://mapbox.github.io/mapbox-unity-sdk/api/index.html)

## Download Unity Package

* Go to https://www.mapbox.com/unity/
* Click `Download the SDK`
* Assets—>Import package—>Custom Package **OR**
* Double-click downloaded `.unityPackage`

## From Source

* `git clone https://github.com/mapbox/mapbox-unity-sdk.git`

## Configure API Access

Copy your token from: https://www.mapbox.com/studio/account/tokens/.

Click Mapbox—>Configure Access from the Unity Editor menu. Paste your token into the `Token` field. Assuming your token is valid, it will save to a text file in `StreamingAssets/MapboxAccess.text`. This file is ignored from the git repo to prevent contributors from accidentally comitting personal tokens. At runtime, the contents of this file will be read and cached for `FileSource` request purposes. See `MapboxAccess.cs` for details.

Now that your access has been configured, check out [the included examples](https://mapbox.github.io/mapbox-unity-sdk/docs/03-examples.html).

## Issues

If you run into any issues or have any feedback, please reach out to us at unity-beta@mapbox.com or check for issues in our repo: https://github.com/mapbox/mapbox-unity-sdk/issues.