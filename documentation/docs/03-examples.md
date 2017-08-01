# Included Examples

For each example, associated scripts and resources can be found in the same root directory as the scene itself.

## Globe (v.1.1.0)

*Globe.unity*

This example uses a `GlobeTileProvider` to fetch the entire world bounds. Additionally, a `FlatSphereTerrainFactory` is used to spherically project terrain vertices to a sphere (radius).

## TerracedWorld (v.1.1.0)

*TerracedWorld.unity*

This example uses the `mapbox.mapbox-terrain-v2` data layer with a `VectorTileFactory` to generate contoured terrain (also relevant: `HeightModifier` and `PolygonMeshModifier`).

## Playground

These examples demonstrate how to request specific Mapbox data using our C# library.

### Forward Geocoder

*ForwardGeocoder.unity*

A forward geocoding request will fetch GeoJSON from a place name query. A new request is sent every time the user input field is edited.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#geocoding) for more information.

### Reverse Geocoder

*ReverseGeocoder.unity*

A reverse geocoding request will fetch GeoJSON from a location query. The location query string must be in the format of `latitude, longitude`. A new request is sent every time the user input field is edited.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#geocoding) for more information.

### Directions

*Directions.unity*

Enter a start location query (eg. "San Francisco, CA"), this is a forward geocode request.

Enter a destination query (eg. "Los Angeles, CA"), this is also a forward geocode request.

When the geocode requests have been completed, a directions request is executed. A new request is sent every time the start or destination user input fields are edited.

Directions results will be logged to the UI when they are available (in the form of JSON).

### Raster Tile

*RasterTile.unity*

Uses a forward geocoder request to fetch a styled raster tile from a Map object. A new request is sent whenever the zoom or style are changed, or when the user input field is edited.

See: https://www.mapbox.com/help/define-style/

See: https://www.mapbox.com/api-documentation/#retrieve-raster-tiles-from-styles

### Vector Tile

*VectorTile.unity*

Uses a forward geocoder request to fetch GeoJSON from a vector tile. A new request is sent whenever the user input field is edited.

In this example, the result is GeoJSON with a feature collection.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#retrieve-features-from-vector-tiles) for more information.

## Mesh Generation Basics

*MeshGeneration.unity*

This example demonstates the basics of the Mapbox Unity SDK `MapVisualization` framework. Specifically, `TerrainFactory`, `MapImageFactory`, and `MeshFactory` are used together to generate a layered map.

`MapController` acts as the entry point. Here, you can specify the map center (`LatLng`) and range. Range `X`, `Y`, `Z`, `W` corresponds to the number of tiles (`int`) for North, East, South, and West, respectively.

See `TerrainFactory.asset` to customize the base ground mesh. This can either be `Flat` (no elevation), or modified with the [Mapbox Global Elevation Layer](https://www.mapbox.com/blog/terrain-rgb/). The `Resolution` property specifies how vertices each tile's generated plane will have. **Note: this factory is needed if you plan to also use a `MapImageFactory` (for texture application purposes).**

See `MapImageFactory.asset` to customize the raster `MapId` you would like to use. Select `Custom` `Map Type` to use your own Mapbox Studio `Style URL`. 

See `MeshFactory.asset` to see how specific layers are extracted from vector tiles. In this case, we are generating meshes for both `building` and `road`. Therefore, each layer has a `VectorLayerVisualizer` responsible for handling that layer's specific data (such as geometry).

**New in v1.1.0:*
Buildings contain a `FeatureSelectionDetector` and a `HighlightFeature` to show how colliders can be used to select/interact with buildings and show vector feature data.

## Mesh Generataion Pois

*PoiGeneration.unity*

With the exception of a `PoiVisualizer ` (`PoiDemoPoiVisualizer`) being added to the `MeshFactory`, this example is identical to `Mesh Generation Basics`.

`PoiDemoPoiVisualizer.asset` allows you to override which prefab to spawn for each `po_label` contained in the vector tile. This prefab should have a component that implements `ILabelVisualizationHelper` attached to it. This exists to inject feature data into (such as label and `Maki` icon).

**New in v1.0.0*

Added 3d POI objects in addition to the 2d POI objects to demonstrate that you can easily map latitude longitude to unity coordinates.

## Mesh Generation Styles

*StylingDemoMeshGeneration.unity*

This example demonstrates how to use `TypeFilters` to filter specific features for processing. In this case, we have chosen to exclude `schools` from mesh generation. Additionally, you can use `ModifierStacks` to further customize specific features (to color banks differently, for example).

*New in v1.1.0

Using the `SpawnInsideModifier` to randomly distribute "bushes" inside of `park` `landuse` geometry (see `VectorLayerVisualizer`).

## Drive

*Drive.unity*

This example demonstrates how to utilize [Mapbox Traffic](https://www.mapbox.com/vector-tiles/mapbox-traffic-v1/) and [Mapbox Directions](https://www.mapbox.com/api-documentation/#directions) data within the Mapbox Unity SDK `MapVisualization` framework.

`DirectionsHelper` is responsible for passing `Transform` positions to the `DirectionsFactory`, as waypoints, in the form of `Latitude/Longitude`.  You can use up to 25 waypoints. See `DriveDirectionFactory.asset` to analyze how the generated route is rendered.

Please see `DriveTrafficVisualizer.asset` to analyze how we styled low, moderate, heavy, and severe traffic congestion. Each congestion feature uses a `ModifierStack` to customize its generated appearance (such as height, width, and material/color).

The ground layer was generated with a `flat` `TerrainFactory` and a `MapImageFactory` (for raster tiles) with the Mapbox Dark style applied. 

To understand 3D building generation, please see `Mesh Generation Basics`. One particular difference in this example, however, is the use of a `MergedModifierStack` for `DriveBuildingVisualizer.asset`. This `ModifierStack` is responsible for merging buildings during generation. This optimization reduces the number of transforms and draw calls in the scene, vastly improving the final frame rate.

## Slippy Vector Terrain

*SlippyDemo.unity*

This example demonstrates one way to create a [slippy map](http://wiki.openstreetmap.org/wiki/Slippy_Map). The `Slippy` component attached to the `MapController` game object is responsible for requesting new tiles as needed, based on the position of the camera relative to the map. This is achieved using `raycasting` and a dictionary of known (requested and fetched) tiles.

Use W, A, S, D keyboard controls to navigate the map at runtime.

Please see `Mesh Generation Basics` to understand how features are customized. 

## Voxels

*VoxelWorld.unity*

This Minecraft-inspired example demonstrates a less traditional way to consume Mapbox data for maps or world construction.

`VoxelTile` is responsible for fetching both a styled raster tile and a `mapbox.terrain-rgb` (global elevation) tile. The styled raster pixels are sampled to determine which voxels to generate, via the `VoxelFetcher`. This is achieved using a `nearest color` formula. The elevation tile pixels are sampled to determine where to vertically place the voxels.

`Zoom`: what [zoom level](http://wiki.openstreetmap.org/wiki/Zoom_levels) to request the tiles at.

`Elevation Multiplier`: use to exaggerate the real-world height.

`Voxel Depth Padding`: determine how many voxels to spawn below the designated height. This helps fill holes in environments with extreme elevation variations.

`Tile Width in Voxels`: How many voxels across each tile will generate. This will affect the detail of the world. Raster textures are downsampled according to this value.

`Voxel Batch Count`: How many voxels to spawn at once. Keep this number low to prevent locking the main thread during construction. 

Please read [the blog post](https://www.mapbox.com/blog/how-to-minecraft-unity/) describing how this was made for more information. 

## LocationProvider

*LocationProvider.unity*

This example is to demonstrate how to:

- Build a map for your current (device) location 
- Update a virtual player's position and rotation based on a real or mock location and heading
- Use mock location providers to test in the Unity editor
- Convert between unity world space<—>earth space (latitude, longitude)

The `LocationProvider` game object in this scene has three children. Each child corresponds to a specific type of `ILocationProvider`. Please [read more about LocationProviders](https://mapbox.github.io/mapbox-unity-sdk/api/unity/Mapbox.Unity.Location.html).

The `MapController` game object has a `BuildMapAtLocation` component attached to it. This component is responsible for overriding the default center point of the `MapController` component, using the DefaultLocationProvider's location. In the Unity editor, this is the `EditorLocationProvider`—intended for mocking. On device, this is the `DeviceLocationProvider`—intended for real world location updates.

To change the location for the map in the Editor, change `EditorLocationProvider`'s `LatitudeLongitude` field on the `Editor` game object. You can use the embedded `Search` button in the inspector to search for a place or address. The default location for this scene is the Metreon, in San Francisco, CA. 

**Note: It is important that the `MapController` component be disabled to begin with.**

Press play and observe the map being constructed. Click on the `Player` game object and note the attached components: `PositionWithLocationProvider` and `RotateWithLocationProvider`. These are responsible for updating the transform's position and rotation based on a specified `ILocationProvider`. Again, in the `EditorLocationProvider`, search for `Yerba Buena Gardens` and select the top result. Watch as the player's position updates!

If you check `Use Transform Location Provider` for `PositionWithLocationProvider` and `RotateWithLocationProvider`, the mock `ILocationProvider` will be represented by the `Transform` game object. Press play once more with this toggle checked for both components. In the scene view, move and rotate the `Transform` game object and observe as the `Player` tries to follow that target. It is important to note that the location returned by the `TransformLocationProvider` is actually converted from the transform's world position to latitude, longitude. This is what that conversion looks like: 

```cs
return _targetTransform.GetGeoPosition(MapController.ReferenceTileRect.Center, MapController.WorldScaleFactor);
```

If you build to device, you should see a familiar map and can observe the player update with your own location. Because the camera is a child of `Player`, you should always be centered on the map.