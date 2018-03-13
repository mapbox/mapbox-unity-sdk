
![unity-repo-banner_preview](https://user-images.githubusercontent.com/4824060/34123144-9a78d322-e3fc-11e7-936f-d5e526ca8520.png)


# Mapbox Unity SDK + UnityARInterface (supporting ARKit and ARCore) For World Scale AR Experiences
A place to create/learn with Unity, ARKit/ARCore and Mapbox!

**Note: This library is experimental and will change. It is published to gather feedback from the community. Please use with caution and open issues for any problems you see or missing features that should be added.**

We'd love to have you [contribute](CONTRIBUTING.md) to the project!

Also, see our related [iOS library](https://github.com/mapbox/mapbox-arkit-ios).

## What is it?

Check out [this presentation](https://youtu.be/vRmTn25xm7Q) for reference.

## ARKit specific checks

One limitation of ARKit is that it does not have any knowledge of where in the world a session was started (or ended), nor does it know the True North alignment with the real world. ARKit is great for location positional tracking, but suffers over distance, or when tracking is poor. My testing has shown that ARKit's position begins to drift after just a few meters and becomes very noticeable after 50 meters (average accumulated drift ~10 meters with GPS accuracy 5 meters).

This project aims to inform ARKit of global positioning using location data from the phone, which will enable Unity developers to "anchor" augmented reality experiences to real world coordinates. Given the sheer volume of data that [Mapbox](http://www.mapbox.com) provides, the possibilities are endless.

For additional inspiration and reference, please [see this library for iOS](https://github.com/ProjectDent/ARKit-CoreLocation). Concepts and potential solutions should be similar, though implementation will vary greatly.

*Please note: while it is possible to use Mapbox to display 3D maps on your coffee table, this project is aimed at building "world scale" maps and augmented reality experiences.*

#### Examples

- [Using maps and location services with ARKit](https://blog.mapbox.com/using-maps-and-location-services-with-arkit-a1980903ca96) (iOS)
- [Building Occlusion](https://twitter.com/davidrhodester/status/892501191875190784)
- [Directions](https://twitter.com/davidrhodester/status/893197138368241664)
- [Pedestrian Navigation](https://youtu.be/vRmTn25xm7Q)

## In this Repository

- Example scenes to get you started: 
  - `AutomaticWorldSynchronization`
  - `ManualWorldSynchronization`
- Prefabs for working with Mapbox, location, and ARKit
- Debug utilities
- [Unity ARInterface](https://github.com/Unity-Technologies/experimental-ARInterface)
  - Unity ARKit plugin
  - Google ARCore plugin Preview 2
  - ARInterface


## Requirements

- Unity 2017.3+
- iOS 11.3+
- Xcode 9.3+
- iOS device that supports ARKit (iPhone 6S or later, iPad (2017) or later)
- Android device with Android 7 (Nougat) or later that [supports ARCore](https://developers.google.com/ar/discover/) (Google Pixel, Pixel XL, Pixel 2, Pixel 2 XL, Samsung Galaxy S8). Install the APK from here: https://github.com/google-ar/arcore-android-sdk/releases

## Known Issues

- Location permissions on Android must be enabled manually after installing application! [See here for related information](https://github.com/google-ar/arcore-unity-sdk/issues/14#issuecomment-330403879).
- AR Tracking state is not yet exposed in the ARInterface
- Focus square has rendering issues on iOS and Android

## Usage

1. [Configure your Mapbox API token.](https://www.mapbox.com/mapbox-unity-sdk/docs/01-mapbox-api-token.html)
2. Open the `AutomaticWorldSynchronization` scene **OR** drag `WorldAlignmentKit` prefab into your scene.
3. Tune parameters (read more below--I recommend testing with defaults, first):
   1. `DeviceLocationProvider` 
   2. `SimpleAutomaticSynchronizationContextBehaviour`
   3. `AverageHeadingAlignmentStrategy`
4. Build.
5. When the scene loads, ensure you find at least one ARKit anchor (blue AR plane).
6. Begin walking. Try walking in a straight line to assist in calculating an accurate heading. How far you need to walk before getting a good `Alignment` depends on your settings.

## How it Works

If you're not familiar with the Mapbox Unity SDK, it may help to check out the built-in examples. You should be familiar with building a map and using `LocationProvider`. For brevity, I will assume you know how these components work.

All relevant AR World Alignment components live in `Mapbox/Unity/AR`.

At the core, we use a `SimpleAutomaticSynchronizationContext` to align our map (world) to where the AR camera thinks it is in the world. This context uses the AR position deltas (a vector) and GPS position deltas (mapped to Unity coordinates) to calculate an angle. This angle represents the ARKit offset from True North. *Note: I could have used `LocationService` compass heading, but I've found it's often far more inaccurate than these manual calculations.*

### ISynchronizationContext

##### OnAlignmentAvailable

This event is sent when the context has successfully calculated an alignment for the world. This alignment can be used to manipulate a root transform so that it appears to be aligned with the AR camera.

### SimpleAutomaticSynchronizationContext

Due to ARKit positional drift over distance, we need to constantly refresh our offset (and potentially our heading). To do so, we should consider the range at which ARKit begins to visually drift, as well as the accuracy of the reported location (GPS). You can think of this relationship as a venn diagram. 

![venn](https://user-images.githubusercontent.com/23202691/29192184-b1406776-7ddd-11e7-8a89-889e9fa8040b.png)

The center of the circles represent reported ARKit and GPS positions, respectively. The radius of the circles represent "accuracy." The intersection represents a potentially more accurate position than either alone can provide. The bias value represents where inside that "intersection" we want to be.

As previously mentioned, we use the delta between position updates to calculate a heading offset. Generally (depending on GPS accuracy), I've found this to be far more reliable and accurate than compass data.

![angle](https://user-images.githubusercontent.com/23202691/29192186-b32acfea-7ddd-11e7-94cd-cbb26acf977f.gif)

The end result of a successful synchronization is an `Alignment`, which offers a rotation and position offset. These values are meant to be used to modify the transform of a `world root object`. We have to do this because ARKit's camera should not be modified directly.

##### UseAutomaticSynchronizationBias

Attempt to compute the bias (see below) based on GPS accuracy and `ArTrustRange`.

##### SynchronizationBias

How much to compensate for drift using location data (1 = full compensation). This is only used if you are not using `UseAutomaticSynchronizationBias`.

##### MinimumDeltaDistance

The minimum distance that BOTH gps and ar delta vectors must differ before new nodes can be added. This is to prevent micromovements in AR from being registered if GPS is bouncing around.

##### ArTrustRange

This represents the radius for which you trust ARKit's positional tracking, relative to the last alignment. Think of it as `accuracy`, but for AR position.

##### AddSynchronizationNodes(Location gpsNode, Vector3 arNode)

You can think of a synchronization node as a comparison of ARKit and location data. You are essentially capturing the position of both "anchors" at the same time. We use this information to compute our `Alignment`. 

### SimpleAutomaticSynchronizationContextBehaviour

This class is mostly a monobehaviour wrapper around the context itself, which allows you to specify settings in the inspector. However, it also has knowledge of when ARAnchors are added, so as to offset the `Alignment` height based on a detected anchor height.

This class is also responisble for listening to location updates from the `LocationProvider` and adding synchronization nodes (gps + ar positions) to the context. **Important: GPS positions must be converted to Unity coordinate space before adding to the context!**

Lastly, this object needs an `AbstractAlignmentStrategy` which is used to determine how an `Alignment` should be processed. For example, you can snap, lerp, or filter and then lerp a transform (such as the `WorldRoot`). I've had the best success and most stable results using the `AverageHeadingAlignmentStrategy`.

### ManualSynchronizationContextBehaviour

This example context relies on a `TransformLocationProvider` that is a child of a camera responsible for drawing a top-down map. You can use touch input to drag (one finger) and rotate (two fingers) the camera to manually position and orient yourself relative to the map (your target location is represented with the red arrow in the example scene). On the release of a touch, the alignment will be created.

*Note: This implementation does not attempt to compensate for ARKit-related drift over time!*

### AverageHeadingAlignmentStrategy

This `AlignmentStrategy` is responsible for averaging the previous alignment headings to determine a better heading match, over time. Additionally, it will not use `Alignments` with reported rotations outside of some threshold to reposition the world root transform. This is important because sometimes a GPS update is wrong and gives us a bad heading. If we were to offset our map  with this heading, our AR object would appear to be misaligned with the real world.

*Note: this implementation is actually a bit of a hack. Ideally, filtering of this type should be done directly in an `ISynchronizationContext`. I've used this approach in the interest of time and to keep the example context as simple as possible.*

##### MaxSamples

How many heading samples we should average. This is a moving average, which means we will prune earlier heading values when we reach this maxmimum. 

##### IgnoreAngleThreshold

We will not use any alignments that report a heading outside of our average plus this threshold to position or rotate our map root. This helps create a more stable environment.

##### LerpSpeed

When we get a new alignment (that should not be dismissed), this value represents how quickly we will interpolate from our previous world root alignment to our new world root alignment. Smaller values mean the transition will appear more subtle.

### DeviceLocationProvider

You will need to experiment with various `DesiredAccuracyInMeters` and `UpdateDistanceInMeters` settings. I recommend keeping your update distance on the higher side to prevent unnecssary alignment computation. The tradeoff, of course, is that you may begin to drift. Which value you use depdends entirely on your application.

## Limitations

While I have done extensive testing "on the ground," I've been in limited, specific locations, with ideal GPS accuracy. I make no guarantees that what is currently provided in this library will solve your problems or work in your area (please help me make it better).

There are various `TODO` and `FIXME` tasks scattered around in the `Mapbox.Unity.Ar` namespace. Please take a look at these to get a better idea of where I think there are some shortcomings. In general, my implementation so far is quite naive. Hopefully the community can help improve this with new context implementations or more sophisticated algorithms/filters.

Solving for UX is not an easy matter. Manual calibration works great, but is not user-friendly (or immune to human error). Automatic calibration works, but still has shortcomings, such as requiring the user to walk *x* meters before acquiring a workable alignment.

There's a giant `Log` button. Use this log to help diagnose issues. If you're seeing lots of red lines (or the alignment just doesn't seem to be working), then something is wrong. Search the C# solution to see what may be the cause of those. If you want, log your own data there, too! You can also use the map toggle to show your paths (AR = red, GPS = blue). If you are aligned properly, the two paths should nearly be on top of one another.

Other issues to note:

- ARKit tracking state is not really used to infuence this alignment process. If you lose tracking, fail to find anchors, background the application, etc., you will need to start a new session and calibrate again.

## What about Mapbox?

With access to Mapbox geospatial data, you can easily augment the AR experience to great effect. Here are some ideas:

- Use the `road` layer to construct navigation meshes for your zombie apocalypse simulation
- Use the `building` layer to occlude AR elements or anchor AR billboards on building facades
- Use Mapbox Directions API to perform realtime navigation and overlay the routes in AR
- Geofence your AR experiences based on `landuse` or custom data (or procedurally place gameobjects based on type)
- Show points of interest (POI) above or on real places
- Use global elevation data to more accurately plant AR objects on the ground (especially useful for distant objects—imagine geotagging something on a cliff or in a valley)
- Use various `label` layers to show street and water names
- Use Geocoding to find out what's nearby and show that information
- Use `building` layer to raycast against for gameplay purposes (ARKit cannot detect vertical planes, but a building could subsitute for this)
- Capture data/input from users at runtime and upload with Mapbox Datasets (use these datasets to generate a tileset to show on a map or to use for logic at runtime)
- Using the above: create world persistence that everyone experiences simultaneously (multiplayer with local **and** global context)
- Build indoor navigation networks and use buildings footprints for geofencing purposes (when a building is entered, disable GPS tracking and switch to ARKit tracking—we also likely known which entrance was used)

## Looking to the Future

What can we do to improve automatic alignment? Here are several ideas:

- Use compass data to augment the angles we calculate. Confirm or more heavily weight a computed angle if it is similar to the compass. *Note: this likely requires the device to be facing the direction it is moving in.*
- Use weighted moving averages or linear regression of location and AR position to find more optimal alignments. Location updates with poor accuracy will have very little influence on the overall alignment. AR position updates with poor tracking state or lots of cumulative drift will have very little weight. Time will affect both, such that more recent updates are weighted more heavily.
- Using the above, weighted heading values will also help improve position offset. This is because we use the heading to "rotate" our GPS location.
- Store last known "good" alignment on application background and use that as a restore point until you successfully find a new alignment.
- We could try to ignore GPS entirely up to a certain cumulative AR distance, barring some complication (tracking state changed). This may lead to longer stretches of properly "anchored" AR elements, relative to the AR camera.
- Use smaller `DeviceLocationProvider` `UpdateDistanceInMeters` to more quickly find an initial alignment. Increase this value one calibration is achieved. 
- Ignore alignments immediately after ARKit is determined to have taken drastic turns. Local tracking is far better suited for measuring sharp turns than GPS.
- Completely filter out poor GPS results. Drift could become problematic, but this may be ideal compared to a very inaccurate GPS update.
- Is ARKit drift fairly consistent (across devices, assuming enough resources available to properly track)? If so, we can project our position along the delta vector to compensate for that drift. GPS would be used rarely, in this instance (and that's a good thing for a "stable" world).



