# Included Examples

For each example, associated scripts and resources can be found in the same root directory as the scene itself.

### Playground

These examples demonstrate how to request specific Mapbox data using our C# library.

#### Forward Geocoder

*ForwardGeocoder.unity*

A forward geocoding request will fetch GeoJSON from a place name query. A new request is sent every time the user input field is edited.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#geocoding) for more information.

#### Reverse Geocoder

*ReverseGeocoder.unity*

A reverse geocoding request will fetch GeoJSON from a location query. The location query string must be in the format of `latitude, longitude`. A new request is sent every time the user input field is edited.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#geocoding) for more information.

#### Directions

*Directions.unity*

Enter a start location query (eg. "San Francisco, CA"), this is a forward geocode request.

Enter a destination query (eg. "Los Angeles, CA"), this is also a forward geocode request.

When the geocode requests have been completed, a directions request is executed. A new request is sent every time the start or destination user input fields are edited.

Directions results will be logged to the UI when they are available (in the form of JSON).

#### Raster Tile

*RasterTile.unity*

Uses a forward geocoder request to fetch a styled raster tile from a Map object. A new request is sent whenever the zoom or style are changed, or when the user input field is edited.

See: https://www.mapbox.com/help/define-style/

See: https://www.mapbox.com/api-documentation/#retrieve-raster-tiles-from-styles

#### Vector Tile

*VectorTile.unity*

Uses a forward geocoder request to fetch GeoJSON from a vector tile. A new request is sent whenever the user input field is edited.

In this example, the result is GeoJSON with a feature collection.

Visit [our API documentation](https://www.mapbox.com/api-documentation/#retrieve-features-from-vector-tiles) for more information.

### Mesh Generation Basics

*MeshGeneration.unity*

This example demonstates the basics of the Mapbox Unity SDK `MapVisualization` framework. Specifically, `TerrainFactory`, `MapImageFactory`, and `MeshFactory` are used together to generate a layered map.

`MapController` acts as the entry point. Here, you can specify the map center (`LatLng`) and range. Range `X`, `Y`, `Z`, `W` corresponds to the number of tiles (`int`) for North, East, South, and West, respectively.

See `TerrainFactory.asset` to customize the base ground mesh. This can either be `Flat` (no elevation), or modified with the [Mapbox Global Elevation Layer](https://www.mapbox.com/blog/terrain-rgb/). The `Resolution` property specifies how vertices each tile's generated plane will have. **Note: this factory is needed if you plan to also use a `MapImageFactory` (for texture application purposes).**

See `MapImageFactory.asset` to customize the raster `MapId` you would like to use. Select `Custom` `Map Type` to use your own Mapbox Studio `Style URL`. 

See `MeshFactory.asset` to see how specific layers are extracted from vector tiles. In this case, we are generating meshes for both `building` and `road`. Therefore, each layer has a `VectorLayerVisualizer` responsible for handling that layer's specific data (such as geometry).

### Mesh Generataion Pois

*PoiGeneration.unity*

With the exception of a `PoiVisualizer ` (`PoiDemoPoiVisualizer`) being added to the `MeshFactory`, this example is identical to `Mesh Generation Basics`.

`PoiDemoPoiVisualizer.asset` allows you to override which prefab to spawn for each `po_label` contained in the vector tile. This prefab should have a component that implements `ILabelVisualizationHelper` attached to it. This exists to inject feature data into (such as label and `Maki` icon).

### Mesh Generation Styles

*StylingDemoMeshGeneration.unity*

This example demonstrates how to use `TypeFilters` to filter specific features for processing. In this case, we have chosen to exclude `schools` from mesh generation. Additionally, you can use `ModifierStacks` to further customize specific features (to color banks differently, for example).

### Drive

*Drive.unity*

This example demonstrates how to utilize [Mapbox Traffic](https://www.mapbox.com/vector-tiles/mapbox-traffic-v1/) and [Mapbox Directions](https://www.mapbox.com/api-documentation/#directions) data within the Mapbox Unity SDK `MapVisualization` framework.

`DirectionsHelper` is responsible for passing `Transform` positions to the `DirectionsFactory`, as waypoints, in the form of `Latitude/Longitude`.  You can use up to 25 waypoints. See `DriveDirectionFactory.asset` to analyze how the generated route is rendered.

Please see `DriveTrafficVisualizer.asset` to analyze how we styled low, moderate, heavy, and severe traffic congestion. Each congestion feature uses a `ModifierStack` to customize its generated appearance (such as height, width, and material/color).

The ground layer was generated with a `flat` `TerrainFactory` and a `MapImageFactory` (for raster tiles) with the Mapbox Dark style applied. 

To understand 3D building generation, please see `Mesh Generation Basics`. One particular difference in this example, however, is the use of a `MergedModifierStack` for `DriveBuildingVisualizer.asset`. This `ModifierStack` is responsible for merging buildings during generation. This optimization reduces the number of transforms and draw calls in the scene, vastly improving the final frame rate.

### Slippy Vector Terrain

*SlippyDemo.Unity*

This example demonstrates one way to create a [slippy map](http://wiki.openstreetmap.org/wiki/Slippy_Map). The `Slippy` component attached to the `MapController` game object is responsible for requesting new tiles as needed, based on the position of the camera relative to the map. This is achieved using `raycasting` and a dictionary of known (requested and fetched) tiles.

Use W, A, S, D keyboard controls to navigate the map at runtime.

Please see `Mesh Generation Basics` to understand how features are customized. 

### Voxels

*VoxelWorld.unity*

This Minecraft-inspired example demonstrates a less traditional way to consume Mapbox data for maps or world construction.

`VoxelTile` is responsible for fetching both a styled raster tile and a `mapbox.terrain-rgb` (global elevation) tile. The styled raster pixels are sampled to determine which voxels to generate, via the `VoxelFetcher`. This is achieved using a `nearest color` formula. The elevation tile pixels are sampled to determine where to vertically place the voxels.

`Zoom`: what [zoom level](http://wiki.openstreetmap.org/wiki/Zoom_levels) to request the tiles at.

`Elevation Multiplier`: use to exaggerate the real-world height.

`Voxel Depth Padding`: determine how many voxels to spawn below the designated height. This helps fill holes in environments with extreme elevation variations.

`Tile Width in Voxels`: How many voxels across each tile will generate. This will affect the detail of the world. Raster textures are downsampled according to this value.

`Voxel Batch Count`: How many voxels to spawn at once. Keep this number low to prevent locking the main thread during construction. 

Please read [the blog post](https://www.mapbox.com/blog/how-to-minecraft-unity/) describing how this was made for more information. 