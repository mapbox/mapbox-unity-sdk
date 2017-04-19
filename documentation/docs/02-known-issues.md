**Please Note: Current support is limited to Unity 5.4 and above!**

### Platform Support

- UWP builds not currently working
- WebGL builds not showing maps (build successful)

### Vector Tile

- Vector tile overzooming buffer hardcoded (to zero)
- Buildings are split along tile borders (features duplicated across boundaries)
- Building parts are not associated with specific buildings (other than spatially)
- Some complex building data is not rendered correctly (cut out holes, floating pieces, etc.)
- Building rooftops are not flat when placed on non-flat terrain

### Global Elevation Data

- Elevation textures are held in memory (post construction)
- [Elevation fetching is inefficient](https://github.com/mapbox/mapbox-sdk-cs/issues/18)

- Some tiles have invalid elevation data (`mapbox.terrain-rgb`)

### Directions

- [Cannot cancel direction queries](https://github.com/mapbox/mapbox-sdk-cs/issues/19)
- DirectionsResource `geometries` property is not implemented

### Traffic

- Traffic visualizer is not offsetting data for both sides of the street

### General

- [Tile requests are not yet threaded](https://github.com/mapbox/mapbox-sdk-cs/issues/46)
- Progress reporting of map fetching/construction is not yet implemented
- No support for runtime texture compression (raster tiles)
- [Texture2D memory leak when destroying tiles](https://github.com/mapbox/mapbox-sdk-cs/issues/31)
- No support for [custom raster tile format or size](https://www.mapbox.com/api-documentation/#retrieve-tiles)
- Tile caching is not yet implemented
- [Request rate exceeded errors not properly reported](https://github.com/mapbox/mapbox-sdk-cs/issues/55)
- `foreach` is being used extensively (GC cost--this is not an issue in Unity 5.5+)