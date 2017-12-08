# Included Examples

*In addition to the bundled examples, you can find tutorials [here](https://www.mapbox.com/unity-sdk/tutorials/).*

For each example, associated scripts and resources can be found in the same root directory as the scene itself.

## Explorer: 
This is a master example that allows you to explore Mapbox’s location data and the Map Editor. You can see a variety of data layers associated with Mapbox’s vector tiles, including building data, points of interest (POIs), roads, and real-time traffic data. Each map vector tile includes high levels of detail about a particular location or building in GeoJSON format that you can leverage for procedurally generating experiences or styling. You can toggle these data layers on and off as needed in the Map Editor (access through the Mapbox menu). 

Mapbox has worldwide data coverage so you can create truly global game levels, AR experiences, and apps. Search anywhere in the world to explore the maps and location data you have available to you. 


## Globe
This example demonstrates a procedurally generated globe model leveraging Mapbox’s global coverage map data. The custom markers showcase how to drop pins according to exact world coordinates. The example also uses a special terrain factor that spherically projects our square tiles onto the globe. 


## LocationProvider: A Location-based Game or App Template

This is a starting point for a location-based game or app. We’re dropping a pin at your location on a 2D map that includes building data outlines and other procedurally generated geo features from Mapbox’s data that you can toggle and style as needed. We’ve created a fun map style in Mapbox Studio for this example - and so can you. This example also includes an update that allows you to zoom in and out, and the map updates accordingly. 


## PlayGround > Directions
This example showcases leveraging Mapbox’s Search and Navigation APIs in Unity to get directions worldwide.

Enter a start location query (eg. "San Francisco, CA"), this is a forward geocode request. Then enter a destination query (eg. "Los Angeles, CA"), this is also a forward geocode request.

When the geocode requests have been completed, a directions request is executed. A new request is sent every time the start or destination user input fields are edited.

Directions results will be logged to the UI when they are available (in the form of JSON).


## PlayGround > Forward Geocoder
This example showcases leveraging Mapbox’s Search API to do a forward geocoding request, which means querying a place by name and getting all of its GeoJSON data. A new request is sent every time the user input field is edited.

Visit Mapbox’s API documentation for more information.

PlayGround - RasterTile
<b>Raster Tile</b>

Uses a forward geocoder request (search a location by name, not coordinates) to fetch a styled raster tile from. You can change the map styling in the dropdown (and import your own through Mapbox Studio) . A new request is sent whenever the zoom or style are changed, or when the user input field is edited.

See: https://www.mapbox.com/help/define-style/
See: https://www.mapbox.com/api-documentation/#retrieve-raster-tiles-from-styles


## PlayGround > Reverse Geocoder
This example showcases leveraging Mapbox’s Search API to do a reverse geocoding request, which means querying a place by latitude and longitude string to get all of the features associated with that location. A new request is sent every time the user input field is edited.

Visit Mapbox’s API documentation for more information.


## PlayGround > Vector Tile
This example shows using a forward geocoder request (search by location name, not coordinates) to fetch a vector tile. You can leverage vector tile data to do things like procedurally generate decorations or experiences based on building heights, land features, etc. In this example, the result is printed as GeoJSON with a feature collection. A new request is sent whenever the user input field is edited.

Visit Mapbox’s API documentation for more information.


## VectorTileMaps > BasicVectorMap: 3D Buildings from Vector Tile Data
This example shows how we extrude 3D buildings from the building height data contained in a vector tile. Mapbox’s vector tiles include a wide array of data about locations that you can leverage. 

## VectorTileMaps > InteractiveStyledVectorMap: Styling Based on Vector Tile Data
This is a vector tile map where you can interact with individual buildings to show their associated information (contained in the vector tile). This example also shows how to procedurally decorate land use and how to render building types differently based on their classification. 

## VectorTileMaps > PoiVectorMap: Points of Interest
This is a vector tile map showcasing Point of Interest (poi_label) markers. Mapbox’s (streets-v7) vector tile set includes POIs you can leverage or you can import your own custom data sets to create custom POIs through Mapbox Studio. 

## VectorTileMaps > TerrainVectorMap: Vector Data on Top of 3D Terrain
This is a vector tile map with terrain data turned on (we use a separate terrain factory and then we snap vector tile features to the extruded terrain mesh). Mapbox has worldwide terrain data coverage. You can also use terrain data with raster tiles to superimpose satellite imagery over the elevation data to create real world elevation models. 

## VoxelMap: Real World Data, Minecraft-inspired Map
This Minecraft-inspired example demonstrates a less traditional way to consume Mapbox data for maps or world construction.

VoxelTile is responsible for fetching both a styled raster tile and a mapbox.terrain-rgb (global elevation) tile. The styled raster pixels are sampled to determine which voxels to generate, via the VoxelFetcher. This is achieved using a nearest color formula. The elevation tile pixels are sampled to determine where to vertically place the voxels.

Zoom: the zoom level at which to request the tiles.

Elevation Multiplier: used to exaggerate the real-world height.

Voxel Depth Padding: determine how many voxels to spawn below the designated height. This helps fill holes in environments with extreme elevation variations.

Tile Width in Voxels: How many voxels to generate across each tile. This will affect the detail of the world. Raster textures are downsampled according to this value.

Voxel Batch Count: The number of voxels to spawn at once. Keep this number low to prevent locking the main thread during construction.

## ZoomableMap: Worldwide Dynamic Zoom & Panning Support
This example is a starting point for creating a traditional web-based zoomable map. Go anywhere in the world and check out Mapbox’s high-quality satellite imagery. 


**New in v1.2.0:**
- Map initialization happens now with the `InitializeMapWithLocationProvider` component, rather than on awake or through a custom map builder. This is a more modular approach.
- TileProvider has been replaced with `RangeAroundTransformTileProvider` which will dynamically load tiles as the player avatar is updated with location data.
