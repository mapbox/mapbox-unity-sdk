# Contributing code

If you want to contribute code:

1. Ensure that existing [pull requests](https://github.com/mapbox/mapbox-sdk-unity/pulls) and [issues](https://github.com/mapbox/mapbox-sdk-unity/issues) don’t already cover your contribution or question.

2. Pull requests are gladly accepted. We require code reviews before merging PRs. When your tests pass, tag a project contributor (for example, @tmpsantos or @BergWerkGIS) and request a review.

# Requirements and installation

#### Mac OS

Use [Mono](http://www.mono-project.com/) to compile the SDK and to run executables. [Xamarin Studio](https://www.xamarin.com/download) is the recommended IDE.

#### Linux

Coming soon.

####  Windows

Coming soon.

# Updating the Mapbox Unity SDK Core

Changes under `mapbox-sdk-unity/sdkproject/Assets/Mapbox/Core/` should never be committed directly to this repo. Instead they should be made in https://github.com/mapbox/mapbox-sdk-unity-core, and updates brought in via the generated nuget package. 
The version of the Mapbox Core SDK can be found in the `packages.config` file at the root of the repo.

To update the Mapbox Unity SDK Core, run the following command from the repo root. 
```
./update-mapbox-unity-sdk-core.sh [version number]
```
- requires the latest version of the [nuget cli](https://docs.nuget.org/ndocs/guides/install-nuget) 
- to update nuget cli to latest via `nuget update -self`
- version number coresponds to published nuget package versions https://www.nuget.org/packages/MapboxSDKforUnityCore/
- `update-mapbox-unity-sdk-core.sh` has been tested on Windows, Ubuntu, and OSX

Once finished, you will need to `git add` any changes and/or files that were added in the update.

# Generating documentation

Documentation for the the Mapbox Unity SDK is automatically generated from XML headers in code. *Instructions for generating documentation are coming soon*.

# Code of conduct

Everyone is invited to participate in Mapbox’s open source projects and public discussions: we want to create a welcoming and friendly environment. Harassment of participants or other unethical and unprofessional behavior will not be tolerated in our spaces. The [Contributor Covenant](http://contributor-covenant.org) applies to all projects under the Mapbox organization and we ask that you please read [the full text](http://contributor-covenant.org/version/1/2/0/).

You can learn more about our open source philosophy on [mapbox.com](https://www.mapbox.com/about/open/).
