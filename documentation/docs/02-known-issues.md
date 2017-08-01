**Please Note: Current support is limited to Unity 5.4 and above!**

### Platform Support

- Minimum iOS version supported is 8
- Minimum Android version supported is 15
- Android duplicate library check is only performed automatically in Unity 5.6+ (run the unit test in older versions to check for duplicates)

### General

- If you experience issues with tiles not refreshing as expected, please remember to clear the disk cache
- `foreach` is being used extensively (GC cost--this is not an issue in Unity 5.5+)

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



