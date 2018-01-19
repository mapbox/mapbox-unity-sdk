# experimental-ARInterface
Experimental cross-platform framework for mobile AR shown as part of ["So You Think You Can Augment Reality?" talk](https://youtu.be/oNekBgognFE) at Unite Austin 2017.  Also see [blog post](https://blogs.unity3d.com/2017/11/01/experimenting-with-multiplayer-arcore-and-arkit-jump-in-with-sample-code/).

## Prerequisites

* [Unity 2017.2.0f3](https://store.unity.com/download?ref=update) release or later
* Unity ARKit plugin on [BitBucket](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin) or from the [Asset Store](https://www.assetstore.unity3d.com/en/#!/content/92515)
* Unity ARCore plugin setup [following these steps](https://developers.google.com/ar/develop/unity/getting-started) 

To use either ARKit or ARCore, their associated prerequisites must also be satisfied.

For your convenience, this repo contains the current snapshots of both Unity ARKit plugin and Unity ARCore plugin.

## ARInterface

We would like to emphasize that the ARInterface that we demonstrated at Unite Austin is **experimental**: we developed it within two weeks with the explicit aim of developing some cross-platform demos for the talk, and as such it has not taken into account any of the underlying Unity requirements or existing APIs. At Unity, we are working on native cross-platform bindings for multiple AR platforms for next year. Until that is officially released, this C# API may be a useful stopgap for developing ARCore and ARKit applications.

Developing an AR application for ARKit or ARCore usually means interacting with platform-specific APIs and components. ARInterface abstracts several of the commonalities into a separate layer, which means if the developer writes code that talks to ARInterface, then it will filter down to the correct platform-specific call.

Initially this interface allows cross-platform work across ARKit and ARCore, but it should be easy enough to implement for other AR platforms with similar functionality. 

## Current API
To take a look at the API, examine `ARInterface.cs` in the project. Here follows a description and detail of the methods in this interface:

Firstly we have the calls to start and stop the AR session:

       public abstract bool StartService(Settings settings);
       public abstract void StopService();

The `Settings` parameter lets you choose to enable any one or more of point cloud creation, light estimation and plane detection in the session.

Next we have the basic AR functionality of world tracking by keeping track of the AR device position and orientation in the real world:

        public bool TryGetPose(ref Pose pose);

`Pose` here describes the position and the rotation of the device camera, and is usually used to move a Unity camera just like the device.

Then we have the plane events delegates:

        public static Action<BoundedPlane> planeAdded;
        public static Action<BoundedPlane> planeUpdated;
        public static Action<BoundedPlane> planeRemoved;
        
These allow you to keep track of the planes that are detected during the AR session, whenever they are added, updated or removed.

To get the real scene that you want to augment in AR, you need to render the video from the camera as the background for a camera in the scene:

       public abstract void SetupCamera(Camera camera);

You pass in the Unity camera that you want to have display the video background and the implementation will set it up for you.

For the background rendering, we also need the display transform which allows the shader to rotate the resulting background texture according to the orientation and screen size of the device:

		public abstract Matrix4x4 GetDisplayTransform ();


There is a call to get the detected point cloud:

       public abstract bool TryGetPointCloud(ref PointCloud pointCloud);


`PointCloud` has a list of `Vector3` that describes where the points are in world coordinates.

AR platforms usually return an estimate of the lighting in the scene:

        public abstract LightEstimate GetLightEstimate();

`LightEstimate` contains both an `ambientIntensity` and an `ambientColorTemperature`.

In some cases, you want to actually read the values of the pixels from the camera captured image (of your real surroundings), and for that you would use:

        public abstract bool TryGetCameraImage(ref CameraImage cameraImage);

`CameraImage` contains the actual pixels captured in a format accessible from the CPU.

## ARKitInterface and ARCoreInterface

These are the concrete implementations of the ARInterface to handle the ARKit and ARCore platforms respectively. Their implementations use the underlying plugin to carry out the functionality they require. You can take a look at these to see how you would possibly extend this to other platforms.

## AREditorInterface: ARInterface

One new thing we can achieve with ARInterface is the ability to derive an editor interface from it, and program it to allow us to replicate an AR environment in the editor. In our example, our simulated AR environment generates two planes in specific places in the scene: one after 1 second and the other after another 1 second. It also generates a point cloud of 20 random points in the scene. The code for this is in `AREditorInterface.cs`, and you can make your simulated environment as elaborate or detailed as needed.

Now we can interact with this environment as if we were moving the device through it, and debug and iterate on any interactions with it since it is in the editor. To move the camera through the environment, use the `WASD` keys. To orient the camera, use the mouse with the right mouse button pressed.  

Since we are within the editor, we can change any GameObject parameters via the inspector and see instant changes. We can also change the code and rerun immediately as the compile time of a change in a single file is almost instantaneous in-editor. In addition, we can set breakpoints in our code and step through it to figure out why something is not working as expected.

This tool has proven invaluable to us while developing new examples and porting examples from other platforms: **use it to iterate quickly without even the need for an AR device!**

## ARRemoteInterface: ARInterface

One of the most popular tools that we released in conjunction with the ARKit plugin is the **ARKit Remote**. This tool has two parts: the actual remote app that you install on your AR device, and a component you place in your scene so that it gets the ARKit data from the remote app, providing a simulation of it in the Editor. This allowed developers to iterate and debug in the Editor, similar to the EditorInterface, but in this case they were getting the actual data of their real world surroundings.  

The popularity of the ARKit Remote meant that when the ARCore preview was released, many developers asked for a similar tool for ARCore. With the cross platform ARInterface, creating the tool for ARCore was made much easier by implementing similar functionality to the existing ARKit Remote via the interface.

To use this workflow, you first build the RemoteDevice scene (found in the `Assets/UnityARinterface/ARRemote` folder) and install it to an actual device that supports either ARKit or ARCore. Make sure that you check the **“Development Build”** checkbox in the `Build Settings` dialog when building (this enables the `PlayerConnection` mechanism that we use to communicate between the remote app and the Editor).

Next you take the AR scene you want to iterate on in the editor and create an `ARRemoteEditor` component on the `ARRoot` GameObject. Disable the `ARController` component on the same GameObject if one exists. Next run the remote app that you installed on the device, and connect to it from the Console “Connected Player” menu. Now press “Play” in the Editor and you should get a Game View with a “Start Remote AR Session” button on the top of the screen. Press it and you should now have the AR device sending its AR data across to the Editor. You are now ready to iterate in the Editor with real AR data from device.   

## Ported examples

We ported a number of examples from the ARKit plugin over to use ARInterface, which allows us to use them on ARCore as well. In addition, it gives us the ability to use AREditorInterface and ARRemoteInterface as well. In fact, the porting of these examples were made way simpler and faster because we could iterate on them in the Editor using AREditorInterface.
  
### Focus Square example

A key component of Apple’s [AR Human Interaction Guidelines](https://developer.apple.com/ios/human-interface-guidelines/technologies/augmented-reality/) is an indicator of where you can place your virtual content in your environment. This was our implementation of that indicator which we called `Focus Square`.

Porting this example to ARInterface was pretty straightforward as it did not have much in the way of platform specific code, except for the HitTest. We decided to use a raycast against the generated plane GameObjects in the scene instead of a HitTest: this would effectively give us a HitTest against a plane considering its extents, and would also work on Editor as a bonus.  

The rest of the AR session and camera is set up by the `ARController` which is part of the code drop that helps you setup your scene for use in AR.
  
### AR Ballz example

The UnityARBallz example was a fun demo that was used to show physics interactions with flat surfaces in AR. The example has two modes. In one, you create balls on planes that have been detected in your environment when you touch that plane via the screen. The second mode made you move the balls away from wherever you were touching on the plane via the screen. 

In porting this example, we replaced the HitTests that were used to place and move the balls with the editor friendly version described above, since we were only interested in doing the HitTest against the extents of the planes you had already detected.

Another change we made, which was not strictly needed for the port, was to make the interaction that made the balls move work much better by using force vectors from your finger position rather than collision dynamics. This was an instance where the EditorInterface came in handy to iterate on the parameters in the Inspector.

You can try both of these examples in the project in Editor, on Remotes or on actual ARKit and ARCore devices. We look forward to porting more of the examples over to this interface as an exercise.

## ScaledContent

In our Unite session, we talked about scaled content and why you would want it. We also discussed why you would not want to scale the content itself, but use "camera-tricks" to do the scaling. We showed two options for scaling content – one of them uses one camera, while the other uses two cameras. 

Within this code release, you can look at the implementation of the one camera solution under `Assets/UnityARInterface/Examples/ScaledContent`.

## Shared multiplayer experience

Another common request from game developers was to allow people with different devices to be able to play the same game in the same space. To cater to this request, we wanted to create a shared multiplayer experience to demonstrate how it could be done. ARInterface, along with the other utilities we created, helped smooth out this development experience.

We started with the TANKS! Networking Demo project that is available from the Asset Store. This turned out to be a good start since the project is a simple one which allows you to play a multiplayer game between various mobile devices. The main thing missing was to make it work as an AR experience, and share that experience across multiple devices.

The existing project worked by using this flow: LobbyScene -> CompleteMainScene

This flow used the Lobby scene to do matchmaking of the users on the different devices, and then forwarded the matched users to play together in the main scene.

To make our modifications, we first got rid of the CameraRig that the main scene had, since we were going to create a camera that was going to be controlled by the AR device and not by the movement of the tanks. Then we changed the flow of the game by adding a new scene at the beginning of the flow:  ARSetupScene -> LobbyScene -> CompleteMainScene

In this case, we set up the AR session and camera in the AR setup scene. Then we pass control over to the Lobby scene, making sure that we make the AR session and camera stay in the scene when we load the subsequent scenes.

The AR setup scene itself uses plane detection to find a good place to create the game. Both devices will look at the same table surface, and use its center to show where the level should appear. Since both use a real world table center and orientation to sync up their coordinate systems, the shared level is created in the same spot according to both devices.

We then consider the size of the plane and the size of the level in Tanks! to figure out what scale we should use on the level content using the mechanism described in the scaled content section. We then use this scale for the Tanks! level we are going to display for both players. We also made some changes to be able to directly interact with the Tanks by screen touch and dragging.

To try this out, in Build Settings, select ARSetupScene, LobbyScene and CompleteMainScene and build those scenes out to your device.

## Keep on augmenting
We hope the talk and this release of the code associated with it will inspire you to look at developing some exciting new features and interactions for your own AR applications. Please show us what you do with it [@jimmy jam jam](https://twitter.com/jimmy_jam_jam). Any questions? Please contact us on the [Unity forums](https://forum.unity.com/forums/ar-vr-xr-discussion.80/). 




