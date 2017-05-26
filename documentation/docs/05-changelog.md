## CHANGELOG

### v.1.0.0

*05/26/2017*

##### Memory/Performance

- Added support for runtime texture compression (DXT) in the `MapImageFactory`
- `MapVisualizer` now pools gameobjects/textures/data to avoid instantiation and destruction costs
- TerrainFactory now allocates less memory when manipulating geometry
- Elevation textures are no longer held in memory and height data parsing and access is much faster
- Added new `FlatTerrainFactory` that is optimized specifically for flat maps
- Tiles can now be cached in memory—configure the cache size in `MapboxAccess.cs` (default size is 500)
- Slippy maps now dispose tiles that determined to be "out of range" 
  - Tiles that are out of range before completion are properly cancelled
- Terrain generation in Unity 5.5+ should be much faster and allocate less memory

##### New Features


- Added new retina-resolution raster tiles
- Added mipmap, compression, and retina-resolution support to `MapImageFactory`
- The `PoiGeneration` example now includes clickable 3D world-space gameobjects—use these as reference for placing objects in Unity space according to a latitude/longitude
- `MapVisualizer` and `TileFactories` now invoke state change events—use these to know when a map or specific factory is completed (loaded)

  - See an example of implementing a loading screen in `Drive.unity`
- You can now specify GameObject `Layer` for tiles in the `TerrainFactory`
- Add colliders to your terrain by checking the `Add Collider` flag in the `TerrainFactory`
- Add colliders or specify GameObject `Layer` for buildings, roads, etc. with `ColliderModifier` and `LayerModifier`

##### Bug Fixes

- Building snapped to terrain are now rendered correctly (check `Flat Tops` in the `HeightModifier`)
- Web request exceptions are now properly forwarded to the `Response` (should fix `Unknown tile tag: 15`)
- Complex building geometry should now be rendered correctly (holes, floating parts, etc.)
- Materials assigned to a `TerrainFactory` are now properly applied at runtime
- Because of `UnityTile` pooling, you should no longer encounter `key already exists in dictionary` exceptions related to tile factories—this means you can change map attributes (location, zoom, terrain, etc.) at runtime without throwing exceptions

##### Improvements

- Map configuration values are no longer static, and an `OnInitialized` event is invoked when the `AbstractMap` reference values have been computed (prevents temporal coupling)
- Snapping to terrain has been simplified—just add a `SnapToTerrainModifier` to your `ModifierStack`
- `Slippy.cs` has been refactored to `CameraBoundsTileProvider.cs` and the backing abstraction enables you to write your own tile provider system (zoomable, path-based, region, etc.)
- `MapController.cs` has been refactored to `AbstractMap` —this is not yet abstract, but should provide an example of how to construct a map using a `MapVisualizer` and a `TileProvider`
- `UnityTile` has been refactored to support reuse and has the ability to cancel its backing web requests
- `DirectionsFactory` no longer relies on a `MapVisualizer` or `DirectionsHelper`, but can still use existing `MeshModifiers`

### v0.5.1

*05/01/2017*

- Terrain height works as intended again (fixed out of range exception)
- Fixed issue where visualizers for `MeshFactories` were not being serialized properly
- Fixed null reference exception when creating a new `MeshFactory`

### v0.5.0 

*04/26/2017*

- Added support for UWP 
    - Share your Hololens creations with us! 
- Fixed precision issue with tile conversions
    - Replaced `Geocoordinate` with `Vector2d`
- Mapbox API Token is now stored in MapboxAccess.txt
    - `MapboxConvenience` has been removed
- Added `LocationProviders` and example scene to build maps or place objects based on a latitude/longitude
- Mesh Generation:
    - General performance improvements (local tile geometry)
    - Custom editors for map factories
    - Added new `MergedModifierStack` which will reduce the number of transforms and draw calls in dense maps
    - Continuous UVs for building facades
    - `DirectionsFactory` now draws full geometry, not just waypoints
    - Fixed occasional vertex mismatch in `PolygonMeshModifier.cs` (which caused an index out of range exception)

### v0.4.0
- Updates mapbox-sdk-unity-core to v1.0.0-alpha13; features vector tile overzooming
  - Updates to attribution guidelines in README.MD
  - Added Conversions.cs and VectorExtensions.cs to enable simple conversions from geocoordinate to unity coordinate space

### v0.3.0
- Added new infrastructure for mesh generation
  - Added new demos for basic, styled, point of interest vector mesh generation
  - Added new demo for vector tiles + terrain with a slippy implementation (dynamic tile loading)
  - Added a new demo for Mapbox Directions & Traffic
  - Deprecated old slippy demo
  - Deprecated old directions component demo

### v0.2.0
- Added core sdk support for mapbox styles
  - vector tile decoding optimizations for speed and lazy decoding
  - Added attribution prefab
  - new Directions example
  - All examples scripts updated streamlined to use MapboxConvenience object

### v0.1.1
- removed orphaned references from link.xml, this was causing build errors
  - moved JSON utility to Mapbox namespace to avoid conflicts with pre-exisiting frameworks
