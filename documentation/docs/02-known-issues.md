**Please Note: Current support is limited to Unity 5.4 and above!**

### Platform Support

- [WebGL builds not showing maps](https://github.com/mapbox/mapbox-unity-sdk/issues/19)
- Minimum iOS version supported is 8
- Minimum Android version supported is 15

### General

- Maps generated with the `Map Factory Framework` [do not automatically snap](https://github.com/mapbox/mapbox-unity-sdk/issues/93) to `y = 0` upon generation—keep this in mind as you place your camera or map
- Map/mesh generation is not yet threaded
- [Tile caching to disk](https://github.com/mapbox/mapbox-unity-sdk/issues/34) is not yet implemented
- `foreach` is being used extensively (GC cost--this is not an issue in Unity 5.5+)

### Vector Tile

- Vector tile overzooming buffer hardcoded (to zero)
- Buildings are split along tile borders (features duplicated across boundaries)
- Building parts are not associated with specific buildings (other than spatially)

### Global Elevation Data

- Some tiles are missing elevation data (`mapbox.terrain-rgb`)—these tiles will be treated as `flat` terrain by the `TerrainFactory`

### Directions

- [Cannot cancel direction queries](https://github.com/mapbox/mapbox-sdk-cs/issues/19)
- [DirectionsResource `geometries`](https://github.com/mapbox/mapbox-unity-sdk/issues/33) property is not implemented 

### Traffic

- Traffic visualizer is not offsetting data for both sides of the street



