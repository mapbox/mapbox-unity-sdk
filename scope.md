# Project scope

The goal of the Unity API is to make it easy for game developers to build beautiful location based games. The API should be flexible enough to allow creative developers to use location data in novel ways, but easy enough to prototype a game environment with a map in a couple minutes.

The API will include a core C# library for requesting data from Mapbox APIS, and then a higher-level set of components designed to work specifically with Unity. 

There are four parts to the Unity SDK: core API support, utilities, Unity components, and custom inspectors:

## Core API support

The Unity SDK will provide an interface to the following [Mapbox web services](https://www.mapbox.com/api-documentation/). I separated services into two priority levels. Priority level 1 is the base level of support we need to release the Unity SDK. Priority level 2 features are those that would be necessary to make the SDK feel complete and up to par with our SDKs on other platforms. 

### Vector tilesets
_Priority level 1_

To support vector tileset resources, the SDK will need to:

- Dynamically fetch vector tiles based on a given map center point.
- Decode vector tiles and store them as geometries usable by Unity developers.
- Position data from tiles in 2D Unity game space
- Position data from tiles in spherical 3D game space.  

First version of vector tile support can leave the rendering implementation to the user of the SDK.

Documentation: https://www.mapbox.com/api-documentation/#retrieve-tiles

### Raster tilesets
_Priority level 2_

Much of the work we do to support vector tiles will be applicable to raster tiles. The main difference between vector tiles and raster tiles is that raster tiles would need to be used as textures. We could also add support for 3D terrain meshes based on raster elevation data.

Documentation: https://www.mapbox.com/api-documentation/#retrieve-tiles

### Static maps
_Priority level 2_

Make it easy for a developer to request and apply a map texture of a fixed size to a game object.

Documentation: https://www.mapbox.com/api-documentation/#static

### Datasets
_Priority level 2_

Provide methods for both requesting data from a dataset and for posting changes to a dataset based on game objects.

Documentation: https://www.mapbox.com/api-documentation/#datasets

### Geocoding
_Priority level 1_

Provide methods for requesting and storing forward geocode and reverse geocode results. Support all optional parameters.

Will use callback functions like `GetForwardGeocode(GeocoderOptions, callback())` where `GeocoderOptions` is an object that includes the geocode query and all options, and callback is a function that receives the geocode response object. The response object can be used to do things like position points on the map or fill a UI template with search results.

Documentation: https://www.mapbox.com/api-documentation/#geocoding

### Directions
_Priority level 1_

Provide a method for requesting and storing directions results. Support all optional parameters.

Will use callback functions like `GetDirections(DirectionsOptions, callback())` where `DirectionsOptions` is an object that includes the directions query and all options, and callback is a function that receives the directions response object. The response object can then be used to do things like build a path on the map or to fill a UI template with instructions.

Documentation: https://www.mapbox.com/api-documentation/#directions

### Map matching
_Priority level 2_

Provide a method for sending user location and getting a match back. Support all optional parameters. Useful for snapping user position to road path. 

Documentation: https://www.mapbox.com/api-documentation/#map-matching

## Utilities
_Priority level 1_

The SDK should include a library of utilities for working with spatial data in Unity. Utilities include:

- Classes for spatial data structures like geojson, latlng, bounding box.
- Convert Lat/Lng to Unity X/Y and back.
- Convert Lat/Lng to Spherical Unity X/Y/Z and back.
- Utility to make it easy to reset game center point to [avoid making game space too big](http://davenewson.com/posts/2013/unity-coordinates-and-scales.html).

This is not a complete list. There are many directions we could go from here. For example, do developers want utilities for procedurally generating game content from vector tiles? Do developers want to be able to do turf-like data manipulation, or will the Unity API itself provide enough tools for manipulating game objects made from spatial data?

## Mapbox prefab with custom inspectors
_Priority level 1_

We will need to include a primary "Mapbox" Monobehavior prefab that uses the Mapbox SDK and stores data on itself, so developers can conveniently use the SDK without needing to code.

Developers work on Unity games in two different ways. Through scripting C#, or through the [inspector panel](https://docs.unity3d.com/Manual/UsingTheInspector.html). We should add [custom inspector editing panels](https://docs.unity3d.com/Manual/editor-CustomEditors.html) for all supported tools and resources.

## Drop-in Unity components
_Priority level 2_

Once we have a core library for working with Mapbox resources and tools, we should build customizable drop-in components on top of the core library. The details of which components we build still needs to be defined. Here are some ideas:

- Drop-in traditional slippy map (done).
- Marker component to work with geocoding API. Configure the marker to use 2d or spherical space, set a scale on it, and pass it a location and it places itself in the right position.
- Route line component to work with directions API. Configure the line's scale, whether it's 2d/spherical space, it's style, then pass it starting and ending point and it'll draw a line.
- Mini-map component to work with static map API. Pass Mapbox style URL, size, and marker properties to generate a 2D mini map with markers.
- User interface components for geocoding and routing. Customizable input boxes, submit buttons, results displays.
- A library of meshes, materials, and textures to use with our vector tile renderer to quickly prototype scenes.
