# Included Examples

### Drive

*Drive.unity*

This example demonstrates how to utilize [Mapbox Traffic](https://www.mapbox.com/vector-tiles/mapbox-traffic-v1/) and [Mapbox Directions](https://www.mapbox.com/api-documentation/#directions) data within the Mapbox Unity SDK `MapVisualization` framework.

`DirectionsHelper` is responsible for passing `Transform` positions to the `DirectionsFactory`, as waypoints, in the form of `Latitude/Longitude`.  You can use up to 25 waypoints. See `DriveDirectionFactory.asset` to analyze how the generated route is rendered.

Please see `DriveTrafficVisualizer.asset` to analyze how we styled low, moderate, heavy, and severe traffic congestion. Each congestion feature uses a `ModifierStack` to customize its generated appearance (such as height, width, and material/color).

The ground layer was generated with a `flat` `TerrainFactory` and a `MapImageFactory` (for raster tiles) with the Mapbox Dark style applied. 

To understand 3D building generation, please see `Mesh Generation Basics`.

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

### Playground

#### Directions

#### Forward Geocoder

#### Raster Tile

#### Reverse Geocoder

#### Vector Tile

### Slippy Vector Terrain

### Voxels
