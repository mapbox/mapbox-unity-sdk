Thanks again for signing up for the Mapbox Unity SDK! 

Please note: we currently only support Unity 5.4 and above. 

If you run into any issues or have any feedback, please reach out to us at unity-beta@mapbox.com.

API documentation is available here: https://mapbox.github.io/mapbox-sdk-unity/api/index.html

All uses of Mapbox’s custom maps and data must attribute both Mapbox and the appropriate data providers. Mapbox’s
custom design is copyrighted and our data sources require attribution. This requirement extends to all plan levels.

For your convenience, we have included a prefab called “Attribution.” You must include the the Mapbox wordmark and 
attribution notice on any map that uses the Mapbox Unity SDK. We provide with the SDK an Attribution prefab that includes 
all required information. This prefab utilizes UGUI for integration and customization. You may adjust the position of the 
Mapbox wordmark and attribution notice (pivots and anchors of the rect transform), but they must remain visible on the map. 
You may also change the background color (transparent by default) of the rect transform and the text color of the text 
attribution notice to best match your design aesthetics, but all information must be clearly legible. You may not otherwise 
alter the Mapbox wordmark or text attribution notice. If you wish to otherwise relocate or to remove the Mapbox wordmark, 
please contact our sales team to discuss options available under our Enterprise plans. Read more on our website: 
https://www.mapbox.com/help/attribution/.

Before testing the included examples, paste your api token in: Assets/Mapbox/Prefabs/MapboxConvenience.prefab Token field.
This will ensure that all the demos work properly.


Demos:


* MeshGenerationBasics
	This demo shows the most basic elements of the Mesh Generation system. Contains three factories; Terrain, Imagery and Mesh with 
	two layer visualizers for buildings and roads.

* MeshGenerationPois
	This demo focuses on Poi Visualizer class and adds PoiVisualizer on the Basic demo setup, which uses the Maki icons to 
	represent points of interest on 2D UI space.

* MeshGenerationStyles
	This demo shows the filtering and styling capabilities of the system and has two alternative styles for different types of 
	buildings and one alternative style for roads (footway). Building visualizer also has uses a filter module and filters out the school 
	from the process.

* Drive
	Drive demo integrates Mapbox Directions api and traffic data into the mesh generation and shows traffic congestion by different
	road colors. It also contains start/target points to test Directions api.

* Playground

	Playground demos consist of the following examples:

	ForwardGeocoder:
		A forward geocoding request will fetch GeoJSON from a place name query.
		See: https://www.mapbox.com/api-documentation/#geocoding for more information.

	ReverseGeocoder:
		A reverse geocoding request will fetch GeoJSON from a latitude, longitude query.
		See: https://www.mapbox.com/api-documentation/#geocoding for more information.

	VectorTile:
		Uses a forward geocoder request to fetch GeoJSON from a Map object.
		In this example, the result is GeoJSON with a feature collection.
		See: https://www.mapbox.com/api-documentation/#retrieve-features-from-vector-tiles

	RasterTile:
		Uses a forward geocoder request to fetch a style's raster tile from a Map object.
		"Request image tiles from a style that can be arranged and displayed with the help of a mapping library."
		See: https://www.mapbox.com/help/define-style/
		See: https://www.mapbox.com/api-documentation/#retrieve-raster-tiles-from-styles

	Directions:
		Enter a start location query (eg. "San Francisco, CA"), this is a forward geocode request.
		Enter a destination query (eg. "Los Angeles, CA"), this is also a forward geocode request.
		When the requests have been completed, a directions request is executed.
		Direction results will be logged to the UI when they are available (in the form of JSON).

* SlippyVectorTerrain
	The purpose of this example is to demonstrate a slippy map built with the sdk. Use W, A, S, and D keyboard keys to
	pan the map. [Add information]

* Voxels
	VoxelWorld uses a Mapbox Studio style (as a raster tile) to map specific prefabs to the colors of each pixel in the texture.
	These prefabs are vertically offset using Mapbox global terrain data (elevation). Note that this example only uses maps out
	a single tile. Read more here: https://www.mapbox.com/blog/how-to-minecraft-unity/.


CHANGELOG

v0.4.0
  - Updates mapbox-sdk-unity-core to v1.0.0-alpha13; features vector tile overzooming
  - Updates to attribution guidelines in README.MD
  - Added Conversions.cs and VectorExtensions.cs to enable simple conversions from geocoordinate to unity coordinate space

v0.3.0
  - Added new infrastructure for mesh generation
  - Added new demos for basic, styled, point of interest vector mesh generation
  - Added new demo for vector tiles + terrain with a slippy implementation (dynamic tile loading)
  - Added a new demo for Mapbox Directions & Traffic
  - Deprecated old slippy demo
  - Deprecated old directions component demo

v0.2.0
  - Added core sdk support for mapbox styles
  - vector tile decoding optimizations for speed and lazy decoding
  - Added attribution prefab
  - new Directions example
  - All examples scripts updated streamlined to use MapboxConvenience object

v0.1.1
  - removed orphaned references from link.xml, this was causing build errors
  - moved JSON utility to Mapbox namespace to avoid conflicts with pre-exisiting frameworks
