# Getting Started

* [Get a Mapbox API token](01-mapbox-api-token.md)
* [Check out known issues](02-known-issues.md)
* [Check out C# API documentation](https://mapbox.github.io/mapbox-unity-sdk/api/index.html)

## Download `unitypackage`

* Go to https://www.mapbox.com/unity/
* Click `Download the SDK`
* ....

## From Source

* `git clone https://github.com/mapbox/mapbox-sdk-unity.git`
* ...

## Configure API Access

Copy your token from: https://www.mapbox.com/studio/account/tokens/.

Click Mapbox—>Configure Access from the Unity Editor menu. Paste your token into the `Token` field. Assuming your token is valid, it will save to a text file in `StreamingAssets/MapboxAccess.text`. This file is ignored from the git repo to prevent contributors from accidentally comitting personal tokens. At runtime, the contents of this file will be read and cached for `FileSource` request purposes.

Now that your access has been configured, check out [the included examples](https://mapbox.github.io/mapbox-unity-sdk/docs/03-examples.html).

## Attribution

All uses of Mapbox’s custom maps and data must attribute both Mapbox and the appropriate data providers. Mapbox’s custom design is copyrighted and our data sources require attribution. This requirement extends to all plan levels.

For your convenience, we have included a prefab called “Attribution.” You must include the the Mapbox wordmark and attribution notice on any map that uses the Mapbox Unity SDK. We provide with the SDK an Attribution prefab that includes all required information. This prefab utilizes UGUI for integration and customization. You may adjust the position of the Mapbox wordmark and attribution notice (pivots and anchors of the rect transform), but they must remain visible on the map. You may also change the background color (transparent by default) of the rect transform and the text color of the text attribution notice to best match your design aesthetics, but all information must be clearly legible. You may not otherwise alter the Mapbox wordmark or text attribution notice. If you wish to otherwise relocate or to remove the Mapbox wordmark, please contact our sales team to discuss options available under our Enterprise plans. Read more on our website: 

https://www.mapbox.com/help/attribution/.

## Issues

If you run into any issues or have any feedback, please reach out to us at unity-beta@mapbox.com or check for issues in our repo: https://github.com/mapbox/mapbox-unity-sdk/issues.