## CHANGELOG

#### [Develop](https://github.com/mapbox/mapbox-unity-sdk/tree/develop)

- Added new raster tiles that request retina resolution 
- Added mipmap, runtime compression (via DXT), and retina resolution support to `MapImageFactory`
- Flat building rooftops (on top of terrain) are now rendered correctly
- Complex building data should now be rendered correctly (cut out holes, floating pieces, etc.)
- The `PoiGeneration` example now includes 3D world-space gameobject placement

#### v0.5.1

*05/01/2017*

- Terrain height works as intended again (fixed out of range exception)
- Fixed issue where visualizers for `MeshFactories` were not being serialized properly
- Fixed null reference exception when creating a new `MeshFactory`

#### v0.5.0 

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

#### v0.4.0
- Updates mapbox-sdk-unity-core to v1.0.0-alpha13; features vector tile overzooming
  - Updates to attribution guidelines in README.MD
  - Added Conversions.cs and VectorExtensions.cs to enable simple conversions from geocoordinate to unity coordinate space

#### v0.3.0
- Added new infrastructure for mesh generation
  - Added new demos for basic, styled, point of interest vector mesh generation
  - Added new demo for vector tiles + terrain with a slippy implementation (dynamic tile loading)
  - Added a new demo for Mapbox Directions & Traffic
  - Deprecated old slippy demo
  - Deprecated old directions component demo

#### v0.2.0
- Added core sdk support for mapbox styles
  - vector tile decoding optimizations for speed and lazy decoding
  - Added attribution prefab
  - new Directions example
  - All examples scripts updated streamlined to use MapboxConvenience object

#### v0.1.1
- removed orphaned references from link.xml, this was causing build errors
  - moved JSON utility to Mapbox namespace to avoid conflicts with pre-exisiting frameworks
