**Please Note: Current support is limited to Unity 2017.1+**

### Platform Support

- Minimum iOS version supported is 8
- Minimum Android version supported is 15
- For UWP, please read these [special notes](https://github.com/mapbox/mapbox-unity-sdk/blob/develop/documentation/docs/windowsstore-uwp-hololens.md).
- Hololens builds are currently not working as expected
- iOS builds with `dynamic batching` turned on may crash.

### General

- If you experience issues with tiles not refreshing as expected, please remember to clear the disk cache
  - You can do this from the Mapbox menu or with `MapboxAccess.Instance.ClearCache();`

### Vector Tile

- Vector tile overzooming buffer hardcoded (to zero)
- Buildings are split along tile borders (features duplicated across boundaries)
- Building parts are not associated with specific buildings (other than spatially)

### Global Elevation Data

- Some tiles are missing elevation data (`mapbox.terrain-rgb`)—these tiles will be treated as `flat` terrain by the `TerrainFactory`

### Directions

- [Cannot cancel direction queries](https://github.com/mapbox/mapbox-sdk-cs/issues/19)

### Traffic

- Traffic visualizer is not offsetting data for both sides of the street


### Tile Error Callbacks

- When a client sends a `Cancel()` / `Recycle()` request to a `UnityTile` instance, the `OnTileError` event handler method is unsubscribed from the `OnTileError` event. Followed by that, a "request aborted" exception is sent as a part of the response to the requested `Tile`. But, the `UnityTile` instance cannot listen to this exception since, it has already unsubscribed from the `OnTileError` event
