# Contributing code

If you want to contribute:

1. Ensure that existing [pull requests](https://github.com/mapbox/mapbox-unity-sdk/pulls) and [issues](https://github.com/mapbox/mapbox-unity-sdk/issues) don’t already cover your contribution or question.

2. Pull requests are gladly accepted. We require code reviews before merging PRs. When your tests pass, tag a project contributor (for example, @isiyu, @BergWerkGIS, @brnky, or @david-rhodes) and request a review.

3. Please adhere to our [coding style](CODING-STYLE.md).

# Requirements and installation

#### Mac OS

*Coming soon.*

[Xamarin Studio](https://www.xamarin.com/download) is the recommended IDE.

#### Linux

*Coming soon.*

####  Windows

*Coming soon.*

# Updating the Mapbox Unity SDK Core

This project includes git submodule dependencies. These dependencies are actively developed and maintained:

- https://github.com/mapbox/vector-tile-cs
- https://github.com/mapbox/mapbox-sdk-cs

Therefore, changes under `mapbox-sdk-unity/sdkproject/Assets/Mapbox/Core/Plugins` and `mapbox-sdk-unity/sdkproject/Assets/Mapbox/Core/mapbox-sdk-cs` should never be committed directly to this repo. Instead they should be made in their corresponding submodule repos and updated in the Unity project via a file transfer executable.

To update the Mapbox Unity SDK Core, run the following command from the repo root:

OSX
```
./update-mapbox-unity-sdk-core.sh
```

Windows
```
update-mapbox-unity-sdk-core.bat
```

This process copies releavant files from `mapbox-sdk-unity/dependencies` to `mapbox-sdk-unity/sdkproject/Assets/Mapbox/Core/`.

# Generating documentation

Documentation for the the Mapbox Unity SDK is automatically generated from XML headers in code.

*Instructions for generating documentation are coming soon.*

# Code of conduct

Everyone is invited to participate in Mapbox’s open source projects and public discussions: we want to create a welcoming and friendly environment. Harassment of participants or other unethical and unprofessional behavior will not be tolerated in our spaces. The [Contributor Covenant](http://contributor-covenant.org) applies to all projects under the Mapbox organization and we ask that you please read [the full text](http://contributor-covenant.org/version/1/2/0/).

You can learn more about our open source philosophy on [mapbox.com](https://www.mapbox.com/about/open/).
