# Contributing code

If you want to contribute:

1. Ensure that existing [pull requests](https://github.com/mapbox/mapbox-unity-sdk/pulls) and [issues](https://github.com/mapbox/mapbox-unity-sdk/issues) don’t already cover your contribution or question.
2. Please see [known issues](https://www.mapbox.com/mapbox-unity-sdk/docs/02-known-issues.html) for contrubition ideas.
3. Pull requests are gladly accepted. We require code reviews before merging PRs. When your tests pass, tag a project contributor (for example, @isiyu, @BergWerkGIS, @brnky, or @david-rhodes) and request a review.
4. Please adhere to our [coding style](CODING-STYLE.md).

# Requirements and installation

#### Mac OS

*Coming soon.*

[Xamarin Studio](https://www.xamarin.com/download) is the recommended IDE.

#### Linux

*Coming soon.*

####  Windows

*Coming soon.*

# Updating the Mapbox Maps SDK for Unity Core

This project includes git submodule dependencies. These dependencies are actively developed and maintained:

- https://github.com/mapbox/vector-tile-cs

**NOTE: As of May 16, 2017, the https://github.com/mapbox/mapbox-sdk-cs repo has been merged into this repo. These source files are now located here: `/sdkproject/Assets/Mapbox/Core/mapbox-sdk-cs`. Core `cs` changes will be backported on a case-by-case basis.**

- Changes under `/sdkproject/Assets/Mapbox/Core/Plugins` should never be committed directly to this repo. Instead they should be made in their corresponding submodule repos and updated in the Unity project via a file transfer executable.
- Changes under `sdkproject/Assets/Mapbox/Core/mapbox-sdk-cs` can be made directly to this repo, but **please avoid any reference to Unity APIs**. This will help enable a smooth backport to the `cs` repository.

To update the Mapbox Maps SDK for Unity Core, run the following command from the repo root:

OSX
```
./update-mapbox-unity-sdk-core.sh
```

Windows
```
update-mapbox-unity-sdk-core.bat
```

This process copies releavant files from `mapbox-sdk-unity/dependencies` to `mapbox-sdk-unity/sdkproject/Assets/Mapbox/Core/`.

# Contributing from your own project

If you would prefer to make changes to the SDK from within your own Unity project, rather than the built-in `sdkproject`, we recommend that you symlink `sdkproject/Assets/Mapbox` to `your-project/Assets/Mapbox`.

# Generating documentation

Documentation for the the Mapbox Maps SDK for Unity is automatically generated from XML headers in code.

*Instructions for generating documentation are coming soon.*

# Code of conduct

Everyone is invited to participate in Mapbox’s open source projects and public discussions: we want to create a welcoming and friendly environment. Harassment of participants or other unethical and unprofessional behavior will not be tolerated in our spaces. The [Contributor Covenant](http://contributor-covenant.org) applies to all projects under the Mapbox organization and we ask that you please read [the full text](http://contributor-covenant.org/version/1/2/0/).

You can learn more about our open source philosophy on [mapbox.com](https://www.mapbox.com/about/open/).
