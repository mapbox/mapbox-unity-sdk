### mapbox-unity-sdk
# Mapbox Unity SDK - 'https://www.mapbox.com/unity/'
# Mapbox-unity-sdk
# For Unity 2017.1.2+
## AR support requires Unity 2017.3+, Android 7+ (Nougat), iOS 11.3
>
Find the AR specific README here.
>
If AR support is not needed these subfolders of sdkproject/Assets/ maybe deleted:
>
# MapboxAR
UnityARInterface
GoogleARCore
UnityARKitPlugin
(for 5.4x compatible versions, please use
>
this commit)
>
Tools for using Mapbox APIs with C# / Unity. If you'd like to contribute to the project, read CONTRIBUTING.md.
>
This repo contains:
>
Unity specific tools and libraries for processing Mapbox data
Example projects using Mapbox Maps SDK for Unity
DocFX project for generating API documentation
Written manuals and guides
Getting started
Versioned SDK (easy, current stable release)
Download unitypackage from
'https://www.mapbox.com/unity-sdk/#download'
If you've installed the SDK before, delete Assets/Mapbox
folder from your project
Within Uni
ty:
Assets -> Import Package -> Custom Package... -> All -> Import,
wait
smirk
From this Repository (advanced, latest development)
Downloading the repo as a zip does not work!
>
git clone git@github.com:mapbox/mapbox-unity-sdk.git
cd mapbox-unity-sdk
Windows: update-mapbox-unity-sdk-core.bat
>
Linux/Mac: ./update-mapbox-unity-sdk-core.sh
>
Documentation
Documentation is generated using DocFX from this repo and is hosted at: 'https://www.mapbox.com/mapbox-unity-sdk/'.
>
Building a Unity Package
To build a Unity Package for import into your own project from the included sdkproject:
>
Select Mapbox folder in the project view.
Right-click and choose Export Package....
screen shot 2017-05-26 at 1 14 01 pm
Uncheck Include Dependencies.
screen shot 2017-05-26 at 1 14 55 pm
Click Export and choose a location. <--< Mapbox Tiling Service On this page Create a tileset source Append to an existing tileset source Replace a tileset source Retrieve tileset source information List tileset sources Delete a tileset source Create a tileset Update a tileset Publish a tileset Update tileset information Retrieve information about a single tileset job List information about all jobs for a tileset View the global queue Validate a recipe Retrieve a tileset's recipe Update a tileset's recipe List tilesets Delete tileset Retrieve TileJSON metadata Mapbox Tiling Service errors Mapbox Tiling Service restrictions and limits Mapbox Tiling Service pricing Mapbox Tiling Service (MTS) supports reading metadata for raster and vector tilesets. To request the tiles themselves, use the Vector Tiles API for vector tiles or the Raster Tiles API for raster tiles instead.
Besides accessing MTS programmatically using the API endpoints described in this documentation, you can also prepare and upload data for MTS using the Tilesets CLI, a command-line Python tool. For more information about the Tilesets CLI, see the Tilesets CLI documentation and the Get started with MTS and the Tilesets CLI tutorial.
>
Beta support for creating tilesets The Mapbox Tiling Service endpoints for creating and interacting with tileset sources, tileset recipes, and tilesets are in public beta, and are subject to potential changes.
>
For a general overview of MTS, its use cases, and typical workflows for using it, see the Mapbox Tiling Service overview documentation.
>
Create a tileset source Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
# Class EarcutLibrary
# Inheritance
# System.Object
# EarcutLibrary
# Inherited Members
# System.Object.Equals(System.Object)
# System.Object.Equals(System.Object, System.Object)
# System.Object.GetHashCode()
# System.Object.GetType()
# System.Object.MemberwiseClone()
# System.Object.ToString()
# System.Object.ReferenceEquals(System.Object, System.Object)
# Namespace:Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers
# Assembly:cs.temp.dll.dll
# Syntax
# public static class EarcutLibrary
# Methods
# Earcut(List
>```
<Single>, List<Int32>, Int32)
    ><
# Declaration
# public static List<int> Earcut(List<float> data, List<int> holeIndices, int dim)
Parameters
Type	Name	Description
List<System.Single>	data	
List<System.Int32>	holeIndices	
System.Int32	dim	
Returns
Type	Description
List<System.Int32>	
Flatten(List<List<Vector3>>)
Declaration
public static Data Flatten(List<List<Vector3>> data)
Parameters
Type	Name	Description
List<List<Vector3>>	data	
Returns
Type	Description```><][
Data
]POST /tilesets/v1/sources/{quantomphantom573}/{pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ} tilesets:writetoken scope Creates a tileset source. A tileset source is raw geographic data formatted as line-delimited GeoJSON and uploaded to Mapbox.com. (Learn more about how line-delimited GeoJSON is used by MTS in the Tileset sources guide.)
>
Tileset sources are necessary to use MTS to create a new vector tileset, and they are referenced via a tileset source ID. The same tileset source can be used across multiple tilesets.
>
A tileset source can be composed of up to 10 source files. Each individual source file must not exceed 20 GB. The maximum combined total size of all files that compose a tileset source is 50 GB. If the total size of all the files that compose a tileset source is greater than 50 GB, MTS will return a response that contains an error property with more details. To add multiple source files to a tileset source, see the Append to an existing tileset source endpoint. To replace a tileset source with new source files, use the Replace a tileset source endpoint.
>
If you no longer need a tileset source, you should manually delete it after any related tilesets are finished processing using the Delete a tileset source endpoint. The related tilesets will continue working normally.
>
Required parameters Description username The Mapbox username of the account for which to create a tileset source. id The ID for the tileset source to be created. Limited to 32 characters. The only allowed special characters are - and _. The request body must be line-delimited GeoJSON. For information about how to convert GeoJSON or other data formats to line-delimited GeoJSON, see the Tileset sources troubleshooting guide.
>
Example request: Create a tileset source $ curl -X POST "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573/hello-world?access_token=pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ This endpoint requires a token with tilesets:write scope. "
-F file=@/Users/quantomphantom573/data/mts/countries.geojson.ld
--header "Content-Type: multipart/form-data" Response: Create a tileset source If the request is successful, the response will contain the following properties:
>
Property Description file_size The size in bytes of the individual file you have added to your tileset source. files The total number of files in the tileset source. id The unique identifier for the tileset source. source_size The total size in bytes of all the files in the tileset source. Example response: Create a tileset source { "file_size": 10592, "files": 1, "id": "mapbox://tileset-source/quantomphantom573/hello-world", "source_size": 10592 } Append to an existing tileset source Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
POST /tilesets/v1/sources/{quantomphantom573}/{pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ } tilesets:writetoken scope Appends new source data to a tileset source, or creates a source if it does not exist already. A tileset source is raw geographic data formatted as line-delimited GeoJSON and uploaded to Mapbox.com. (Learn more about how line-delimited GeoJSON is used by MTS in the Tileset sources guide.)
>
Tileset sources are necessary to use MTS to create a new vector tileset, and they are referenced via a tileset source ID. The same tileset source can be used across multiple tilesets.
>
A tileset source can be composed of up to 10 source files. Each individual source file must not exceed 20 GB. The maximum combined total size of all files that compose a tileset source is 50 GB. If the total size of all the files that compose a tileset source is greater than 50 GB, MTS will return a response that contains an error property with more details. To add multiple source files to a tileset source, post to this endpoint multiple times. This will append the uploaded files to the tileset source. To replace a tileset source with new source files, use the Replace a tileset source endpoint.
>
If you no longer need a tileset source, you should manually delete it after any related tilesets are finished processing using the Delete a tileset source endpoint. The related tilesets will continue working normally.
>
Required parameters Description username The Mapbox username of the account for which to create a tileset source. id The ID for the tileset source to be append the new source data. The request body must be line-delimited GeoJSON. For information about how to convert GeoJSON or other data formats to line-delimited GeoJSON, see the Tileset sources troubleshooting guide.
>
Example request: Append to an existing tileset source $ curl -X POST "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573/hello-world?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
-F file=@/Users/quantomphantom573/data/mts/countries.geojson.ld
--header "Content-Type: multipart/form-data" Response: Append to an existing tileset source If the request is successful, the response will contain the following properties:
>
Property Description file_size The size in bytes of the individual file you have added to your tileset source. files The total number of files in the tileset source. id The unique identifier for the tileset source. source_size The total size in bytes of all the files in the tileset source. Example response: Append to an existing tileset source { "file_size": 10592, "files": 2, "id": "mapbox://tileset-source/quantomphantom573/hello-world", "source_size": 20884 } Replace a tileset source Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
PUT /tilesets/v1/sources/{quantomphantom573}/{pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ } tilesets:writetoken scope Replaces a tileset source with new source data, or creates a source if it does not exist already. If the total size of the uploaded file is greater than 20 GB, MTS will return a response that contains an error property with more details.
>
If you no longer need a tileset source, you should manually delete it after any related tilesets are finished processing using the Delete a tileset source endpoint. The related tilesets will continue working normally.
>
Required parameters Description username The Mapbox username of the account for which to create a tileset source. id The ID for the tileset source to be replaced. The request body must be line-delimited GeoJSON. For information about how to convert GeoJSON or other data formats to line-delimited GeoJSON, see the Tileset sources troubleshooting guide.
>
Example request: Replace a tileset source $ curl -X PUT "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573/hello-world?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
-F file=@/Users/username/data/mts/countries.geojson.ld
--header "Content-Type: multipart/form-data" Response: Replace a tileset source If the request is successful, the response will contain the following properties:
>
Property Description file_size The size in bytes of the individual file you have added to your tileset source. files The total number of files in the tileset source. id The unique identifier for the tileset source. source_size The total size in bytes of all the files in the tileset source. Example response: Replace a tileset source { "file_size": 10592, "files": 1, "id": "mapbox://tileset-source/username/hello-world", "source_size": 10592 } Retrieve tileset source information Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
GET /tilesets/v1/sources/{username}/{id} tilesets:readtoken scope Get information for a specific tileset source, including the number and total size of the files in the tileset source.
>
Required parameters Description username The Mapbox username of the account for which to retrieve tileset source information. id The ID for the tileset source to be retrieved. Example request: Retrieve tileset source information $ curl "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573/hello-world?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:read scope. " Response: Retrieve tileset source information If the request is successful, the response will contain the following properties:
>
Property Description files The total number of files in the tileset source. id The unique identifier for the tileset source. size The total size in bytes of all files in the tileset source. size_nice The total size of all files in the tileset source, in a human-readable format. Example response: Retrieve tileset source information { "files": 2, "id": "mapbox://tileset-source/username/hello-world", "size": 20884, "size_nice": "20.39KB" } List tileset sources Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
GET /tilesets/v1/sources/{username} tilesets:listtoken scope List all the tileset sources that belong to an account. This endpoint supports pagination.
>
Required parameters Description username The Mapbox username of the account for which to retrieve tileset source information. You can further refine the results from this endpoint with the following optional parameters:
>
Optional parameters Description sortby Sort the listings by their created or modified timestamps. limit The maximum number of tilesets to return, from 1 to 500. The default is 100. start The tileset after which to start the listing. The key is found in the Link header of a response. See the pagination section for details. Example request: List tileset sources $ curl "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. " Response: List tileset sources If the request is successful, the response will be a list of the tileset sources that belong to the specified account. If the account has more than 2000 tileset sources, the response list will be capped at 2000.
>
Example response: List tileset sources [ { "files": 2, "id": "mapbox://tileset-source/username/hello-world", "size": 20884 }, { "files": 3, "id": "mapbox://tileset-source/username/hola-mundo", "size": 650332 } ] Delete a tileset source Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
DELETE /tilesets/v1/sources/{username}/{id} tilesets:writetoken scope Permanently delete a tileset source and all its files. This is not a recoverable action.
>
Don't delete a tileset source while it's in use If you delete a tileset source, any in-progress jobs that use that tileset source will fail.
>
Required parameters Description username The Mapbox username of the account for which to delete a tileset source. id The ID for the tileset source to be deleted. Example request: Delete a tileset source $ curl -X DELETE "https://api.mapbox.com/tilesets/v1/sources/quantomphantom573/hello-world?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. " Response: Delete a tileset source If the tileset source is successfully deleted, the response will be HTTP 204 No Content.
>
Create a tileset Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
POST /tilesets/v1/{tileset} tilesets:writetoken scope Prerequisites for creating a new tileset Before you can create a new tileset, you need to create a tileset source and write a tileset recipe. The tileset recipe defines how to transform the data in the tileset source into vector tiles.
>
Create a new tileset.
>
Required parameters Description tileset The ID for the tileset to be created, which is composed of your username followed by a period and the tileset's unique name (username.tileset_name). Limited to 32 characters. This character limit does not include your username. The only allowed special characters are - and _. The request body must be a JSON object that contains the following properties:
>
Required request body property Description recipe A recipe that describes how the GeoJSON data you uploaded should be transformed into tiles. For more information on how to create and format recipes, see the Recipe reference and Recipe examples. name The name of the tileset. Limited to 64 characters. Additionally, the request body may contain the following optional properties:
>
Optional request body properties Description private A boolean that describes whether the tileset must be used with an access token from your Mapbox account. Default is true. description A description of the tileset. Limited to 500 characters. attribution An array of attribution objects, each with text and link keys. Limited to three attribution objects, 80 characters maximum combined across all text values, and 1000 characters maximum combined across all link values. attribution.text The attribution text for the tileset. attribution.link The URL used for the tileset's attribution. Example request: Create a tileset $ curl -X POST "https://api.mapbox.com/tilesets/v1/{tileset}?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
-d @tileset-recipe.json
--header "Content-Type:application/json" Example request body: Create a tileset { "recipe": { "version": 1, "layers": { "hello_world": { "source": "mapbox://tileset-source/username/hello-world", "minzoom": 0, "maxzoom": 5 } } }, "name": "Hello World", "description": "Spaceship Earth with all of the people, places, and things.", "attribution": [ { "text": "© Hello Legal World", "link": "https://docs.mapbox.com" } ] } Response: Create a tileset { "message": "Successfully created empty tileset <tileset_id>. Publish your tileset to begin processing your data into vector tiles." } If a tileset with the specified ID already exists, MTS will return an HTTP 400 status code.
>
Update a tileset You can update an existing tileset's source, recipe, metadata, and tiles using MTS:
>
Update a tileset's source using the Update a tileset’s recipe endpoint. You can reference a new tileset source in the updated tileset recipe. Update a tileset’s recipe using the Update a tileset’s recipe endpoint. The updated recipe can specify a new minzoom, maxzoom, and more following the rules in the MTS recipe specification. Update a tileset's metadata using the Update tileset information MTS endpoint. You can update a tileset's name, description, private or public state, and attribution information. Update a tileset’s tiles using the Publish a tileset MTS endpoint. The steps to do so are outlined in the Update a tileset with MTS guide. This process can be repeated indefinitely for the same tileset when your source data updates or your recipe requires changes. Publish a tileset Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
POST /tilesets/v1/{tileset}/publish tilesets:writetoken scope Once you’ve created a tileset, you can request that the data be "published" into vector tiles. This action will start an asynchronous process known as a job that retrieves your data and processes it into vector tiles according to the recipe you have defined.
>
This endpoint can also be used to update an existing tileset. Note that due to tile caching, when updating an existing tileset new tiles will only become visible as the cached versions of those tiles expire.

A given tileset can only have one active processing publish job at a time. This ensures the data you have staged is processed before any future data you have staged is processed. If your tileset has an active publish request being processed, all later publish requests will be queued to run in the order they were received. You can only have five jobs in the queue per account.
>
All jobs are entered into a global Mapbox queue. The larger the queue, the longer it will take your tileset to process. For instructions on how to see the size of the queue, see the View the global queue section.
>
The size of a single vector tile is limited to 500 KB. If a job drops features due to tile size, this will be noted in the warnings field of the job object.
>
Required parameters Description tileset The ID of the tileset to be published, which is composed of your username followed by a period and the tileset's unique name (username.tileset_name). Processing time There are a number of factors that can influence the processing time when you publish a tileset, including:
>
The size of the geographical area represented by the data that is being processed. For example, data for a city block will be processed much faster than data for the entire earth. The maximum zoom level applied to each layer. The higher the maximum zoom level, the more tiles that need to be created. Each additional zoom level creates four times as many tiles as the previous zoom level did. As an example, a maximum zoom level of 1 produces four tiles, while a maximum zoom level of 2 produces 16 tiles. The complexity of the tileset recipe, in particular filters, attributes, and union configurations will likely add to the processing time. The size of the global queue, which you can check with the View the global queue endpoint. MTS global queue indicates the number of tileset jobs waiting to be processed. Example request: Publish a tileset $ curl -X POST "https://api.mapbox.com/tilesets/v1/{tileset}/publish?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. " Response: Publish a tileset { "message": "Processing <tileset_id>",
"jobId": "<job_id>" } If your recipe references a non-existent tileset source, MTS will return an HTTP 400 status code.
>
Update tileset information Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
PATCH /tilesets/v1/{tileset} tilesets:writetoken scope Update a tileset's information such as name, description, and privacy settings. This is not the endpoint for updating a tileset's recipe, sources, or tiles.
>
Optional request body properties Description name The name of the tileset. Limited to 64 characters. description A description of the tileset. Limited to 500 characters. private A boolean that describes whether the tileset must be used with an access token from your Mapbox account. attribution An array of attribution objects, each with text and link keys. Limited to three attribution objects, 80 characters maximum combined across all text values, and 1000 characters maximum combined across all link values. For more details on how attribution is displayed when multiple tilesets with custom attribution are composited together, see the attribution display section. attribution.text The attribution text for the tileset. attribution.link The URL used for the tileset's attribution. Attribution display with multiple tilesets Custom map attribution strings are composited in reverse order of the composite request and are separated by a space character. For example, the following request to mapbox.mapbox-streets-v8,example.custom-tileset (in which the first tileset is a Mapbox tileset and the second tileset is a custom tileset) will show attribution that looks like:
>
© Example Maps 2020 © Mapbox © OpenStreetMap Example request: Update a tileset $ curl -X PATCH "https://api.mapbox.com/tilesets/v1/{tileset}?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
--data '{"name":"New name same me","attribution":[{"text":"New data license","link":"https://example.com"}]}'
--header "Content-Type:application/json" Response: Update a tileset HTTP 204 No Content Retrieve information about a single tileset job Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.
>
GET /tilesets/v1/{tileset}/jobs/{job_id} tilesets:readtoken scope Retrieve information about a single job associated with a tileset, based on its unique job ID.
>
Required parameters Description tileset The ID for the tileset for which to retrieve information, which is composed of your username followed by a period and the tileset's unique name (username.tileset_name). job_id The publish job's unique identifier. This identifier is returned in the jobId field of a Publish a tileset response. Example request: Retrieve information about a single tileset job $ curl "https://api.mapbox.com/tilesets/v1/{tileset}/jobs/{job_id}?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:read scope. " Response: Retrieve information about a single tileset job A successful request will return a JSON response that contains the following properties:
>
Property Description id The publish job's unique identifier. stage The status of the job. Possible values are queued, processing, success, or failed. created Timestamp indicating when the job was created. created_nice Human-readable timestamp indicating when the job was created. published Timestamp indicating when the job was published. tilesetId The specified tileset's unique identifier. errors The errors related to the tileset, if relevant. If there are no errors, this will be an empty array. For more information, see our MTS errors documentation. warnings Any warnings related to the tileset, if relevant. If there are no warnings, this will be an empty array. For more information about warnings, see our MTS warnings documentation. recipe The MTS recipe that was used to publish the job. tileset_precisions An object listing the Tileset Processing billing tiers that were used by one or more layers in the job's recipe and the number of square kilometers that were processed in each tier. layer_stats An object that contains statistics about the given layer: layer_stats.total_tiles The total number of tiles in the tileset that contain this layer. layer_stats.point_count The total number of point features for the layer, multiplied by the number of tiles there are in the tileset. layer_stats.linestring_count The total number of linestring features for the layer, multiplied by the number of tiles there are in the tileset. layer_stats.polygon_count The total number of polygon features for the layer, multiplied by the number of tiles there are in the tileset. layer_stats.capped The number of tiles in which features were no longer added to the layer during tiling due to the size of the layer. layer_stats.maxzoom The maximum zoom level of the layer. layer_stats.minzoom The minimum zoom level of the layer. layer_stats.checksum A checksum of the geometry and attributes of the features in the layer, including the order of the features but not including their IDs. If two checksums are the same, the tiles from the two tiling jobs are almost certainly identical, but identical recipes and sources are not strictly guaranteed to produce identical checksums. layer_stats.zooms An object that contains statistics for each zoom level in the layer: layer_stats.zooms.ymin The minimum y tile coordinate at the zoom level. layer_stats.zooms.ymax The maximum y tile coordinate at the zoom level. layer_stats.zooms.xmin The minimum x tile coordinate at the zoom level. layer_stats.zooms.xmax The maximum x tile coordinate at the zoom level. layer_stats.zooms.tiles The total number of tiles at this zoom level for the layer. layer_stats.zooms.capped The number of tiles in which features were no longer added during tiling due to the size of the layer at this zoom level. layer_stats.zooms.capped_list A list of tiles in this zoom level that exceeded the layer_size. The list may be truncated if large numbers of tiles exceeded the limit. Each element in the capped_list is an object containing a tile (referred to by its zoom/x/y coordinates) and the layer_size that would have been required for that tile to have avoided being capped (or "more than maximum" if the required size exceeds system limits). layer_stats.zooms.sum_area The total area that tiles cover at this zoom level for the layer. layer_stats.zooms.sum_size The total size in bytes of all tiles for this layer at this zoom level, uncompressed. layer_stats.zooms.avg_size The average size of a tile for this layer at this zoom level, uncompressed. layer_stats.zooms.max_size The maximum size of a tile at this zoom level for this layer. layer_stats.zooms.size_histogram An array of 20 counts of tiles that shows the distribution of tile layer sizes. The first count is the number of tiles between 1 byte and 25 kilobytes, with each subsequent count representing another 25 kilobyte range, up to 500 kilobytes. Any tile layers larger than 500 kilobytes are also included in the last count. Example response: Retrieve information about a single tileset job { "id": "unique_hash", "stage": "success", "created": 1560981902377, "created_nice": "Wed Jun 19 2019 22:05:02 GMT+0000 (UTC)", "published": 1560982158721, "tilesetId": "user.id", "errors": [], "warnings": [], "tileset_precisions": { "1m": 658731.7540137176 }, "layer_stats": { "sample_pois": { "total_tiles": 71, "linestring_count": 0, "capped": 15, "maxzoom": 12, "zooms": { "0": { "ymin": 0, "ymax": 0, "xmin": 0, "xmax": 0, "tiles": 1, "capped": 1, "capped_list": [ { "layer_size": 1337, "tile": "0/0/0" } ], "sum_size": 512036, "sum_area": 508164394.24620897, "avg_size": 512036, "max_size": 512036, "size_histogram": [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 ] } }
} }, "recipe": { "version": 1, "layers": { "sample_pois": { "minzoom": 1, "maxzoom": 12, "source": "mapbox://tileset-source/user/source" } } } } List information about all jobs for a tileset Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.

GET /tilesets/v1/{tileset}/jobs tilesets:listtoken scope List information about all jobs associated with a tileset. You can also use this endpoint to query jobs at a specific processing stage: processing, queued, success, or failed. This endpoint supports pagination.

Required parameter Description tileset The ID for the tileset for which to list all jobs, which is composed of your username followed by a period and the tileset's unique name (username.tileset_name). You can further refine the results from this endpoint with the following optional parameters:

Optional parameter Description stage Query for jobs at a specific processing stage: processing, queued, success, or failed. limit The maximum number of tilesets to return, from 1 to 500. The default is 100. start The tileset after which to start the listing. The key is found in the Link header of a response. See the pagination section for details. Example request: List information about all jobs for a tileset

Request all associated jobs
$ curl "https://api.mapbox.com/tilesets/v1/{tileset}/jobs?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. "

Request only associated jobs at the success stage
$ curl "https://api.mapbox.com/tilesets/v1/{tileset}/jobs?stage=success&access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. " Response: List information about all jobs for a tileset A successful request returns one or more JSON objects that describe a tileset's jobs. Each object will contain the following properties:

Property Description id The publish job's unique identifier. stage The status of the job. Possible values are queued, processing, success, or failed. created Timestamp indicating when the job was created. created_nice Human-readable timestamp indicating when the job was created. published Timestamp indicating when the job was published. tilesetId The specified tileset's unique identifier. errors The errors related to the tileset, if relevant. If there are no errors, this will be an empty array. For more information, see our MTS errors documentation. warnings Any warnings related to the tileset, if relevant. If there are no warnings, this will be an empty array. For more information about warnings, see our MTS warnings documentation. recipe The MTS recipe that was used to publish the job. tileset_precisions An object listing the Tileset Processing billing tiers that were used by one or more layers in the job's recipe and the number of square kilometers that were processed in each tier. Example response: List information about all jobs for a tileset [
{ "id": "job_1_id", "stage": "success", "created": 1560981902377, "created_nice": "Wed Jun 19 2019 22:05:02 GMT+0000 (UTC)", "published": 1560982158721, "tilesetId": "user.id", "errors": [], "warnings": [], "tileset_precisions": { "1m": 658731.7540137176 }, "recipe": { "version": 1, "layers": { "sample_pois": { "minzoom": 1, "maxzoom": 12, "source": "mapbox://tileset-source/user/source" } } } }, { "id": "job_2_id", "stage": "processing", "created": 1560982159327, "created_nice": "Wed Jun 19 2019 22:09:19 GMT+0000 (UTC)", "published": 1560985238565, "tilesetId": "user.id", "errors": [], "warnings": [], "recipe": { "version": 1, "layers": { "sample_pois": { "minzoom": 1, "maxzoom": 12, "source": "mapbox://tileset-source/user/revised-source" } } } } ] View the global queue Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.

PUT /tilesets/v1/queue tilesets:readtoken scope View the number of queued jobs in the global queue, which shows the number of jobs waiting to be processed.

Example request: View the global queue $ curl -X PUT "https://api.mapbox.com/tilesets/v1/queue?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:read scope. " Response: View the global queue A request to this endpoint returns the number of jobs waiting to be processed in the global MTS queue.

Property Description total The number of queued jobs. Example response: View the global queue { "total": 42 } Validate a recipe Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.

PUT /tilesets/v1/validateRecipe tilesets:writetoken scope Validate a recipe document before using it to create a new tileset. The entire request body must be the recipe JSON document. For guidance on how to format a recipe, see the Recipe reference.

You can control the behavior of this endpoint with the following optional parameter:

Optional parameters Description accept_invalid_sources If true, do not validate the source URL for each layer. This allows you to validate the syntax of a recipe while it is being developed before the sources are uploaded. Example request: Validate a recipe $ curl -X PUT "https://api.mapbox.com/tilesets/v1/validateRecipe?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
-d @recipe.json
--header "Content-Type:application/json" Response: Validate a recipe The response will be a JSON object that tells you whether the recipe document is valid or not. If the recipe document is not valid, the errors property will contain an informational message about the issue.

Example response: Validate a recipe Valid recipe:

{ "valid": true } Recipe not valid:

{ "valid": false, "errors": [ "minzoom 22 cannot be larger than maxzoom 11" ] } Retrieve a tileset's recipe Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.

GET /tilesets/v1/{tileset}/recipe tilesets:listtoken scope Request the recipe body that you used when you created a specific tileset.

Required parameter Description tileset The ID for the tileset for which to retrieve the recipe, which is composed of your username followed by a period and the tileset's unique name (username.tileset_name). Example request: Retrieve a tileset's recipe $ curl "https://api.mapbox.com/tilesets/v1/{tileset}/recipe?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. " Response: Retrieve a tileset's recipe Returns the recipe body you provided when you originally created the specified tileset.

Example response: Retrieve a tileset's recipe { "recipe": { "version": 1, "layers": { "my_layer": { "source": "mapbox://tileset-source/my-source-data", "minzoom": 0, "maxzoom": 4 } } }, "id": "username.id" } Update a tileset's recipe Beta support for creating tilesets This Mapbox Tiling Service API endpoint is in public beta and is subject to potential changes.

PATCH /tilesets/v1/{tileset}/recipe tilesets:writetoken scope Update a tileset’s recipe. This endpoint performs a validation step on the new recipe.

Required parameter Description tileset The ID for the tileset for which you are updating the recipe, which is composed of your username followed by a period and the tileset's unique name. The entire request body must be the recipe JSON document.

Updates to the layer name Updating the layer_name of your recipe will break any downstream styles that reference this tileset. If a style references this tileset and you change the layer_name, you must update your style by deleting the relevant layer and re-adding the tileset’s new layer.

Example request: Update a tileset's recipe $ curl -X PATCH "https://api.mapbox.com/tilesets/v1/{tileset}/recipe?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. "
-d @recipe.json
--header "Content-Type:application/json" Response: Update a tileset's recipe If the updated recipe is valid, the response will be HTTP 204 No Content.

If the updated recipe is not valid, the errors property will contain an informational message about the issue:

{ "message": "Recipe is invalid.", "errors": [ "minzoom 22 cannot be larger than maxzoom 11" ] } List tilesets GET /tilesets/v1/{username} tilesets:listtoken scope List all the tilesets that belong to a specific account. This endpoint supports pagination. It returns a maximum of 100 tilesets by default.

Required parameter Description username The username of the account for which to list tilesets You can further refine the results from this endpoint with the following optional parameters:

Optional parameters Description type Filter results by tileset type, either raster or vector. visibility Filter results by visibility, either public or private. Private tilesets require an access token that belong to the owner. Public tilesets can be requested with any user's access token. sortby Sort the listings by their created or modified timestamps. limit The maximum number of tilesets to return, from 1 to 100. The default is 100. start The tileset after which to start the listing. The key is found in the Link header of a response. See the pagination section for details. Example request: List tilesets $ curl "https://api.mapbox.com/tilesets/v1/quantomphantom573?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. "

Limit the results to the 25 most recently created vector tilesets
$ curl "https://api.mapbox.com/tilesets/v1/quantomphantom573?type=vector&limit=25&sortby=created&access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. "

Limit the results to the tilesets after the tileset with a start key of abc123
$ curl "https://api.mapbox.com/tilesets/v1/quantomphantom573?start=abc123&access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:list scope. " Response: List tilesets A request to MTS returns an array of tileset objects. Each tileset object contains the following properties:

Property Description type The kind of data contained, either raster or vector. center The longitude, latitude, and zoom level for the center of the contained data, given in the format [lon, lat, zoom]. created A timestamp indicating when the tileset was created. description A human-readable description of the tileset. filesize The storage in bytes consumed by the tileset. id The unique identifier for the tileset. modified A timestamp indicating when the tileset was last modified. name The name of the tileset. visibility The access control for the tileset, either public or private. status The processing status of the tileset, one of: available, pending, or invalid. For tilesets created with the Mapbox Tiling Service, this is always set to available. To see the stage of a MTS tileset's most recent job, use the tileset jobs listing endpoint with a limit=1 query parameter. Example response: List tilesets [ { "type": "vector", "center": [-0.2680000000000007, 11.7014165, 2], "created": "2015-09-09T23:30:17.936Z", "description": "", "filesize": 17879790, "id": "mapbox.05bv6e12", "modified": "2015-09-09T23:30:17.906Z", "name": "routes.geojson", "visibility": "public", "status": "available" }, { "type": "raster", "center": [-110.32479628173822, 44.56501277250615, 8], "created": "2016-12-10T01:29:37.682Z", "description": "", "filesize": 794079, "id": "mapbox.4umcnx2j", "modified": "2016-12-10T01:29:37.289Z", "name": "sample-4czm7e", "visibility": "private", "status": "available" } ] Supported libraries: List tilesets Mapbox wrapper libraries help you integrate Mapbox APIs into your existing application. The following SDK supports this endpoint:

Mapbox JavaScript SDK See the SDK documentation for details and examples of how to use the relevant methods to query this endpoint.

Delete tileset DELETE /tilesets/v1/{username.tileset_id} tilesets:writetoken scope Delete a tileset. Note that you can only delete your own tileset.

Required parameter Description username.tileset_id The ID the tileset you want to delete, which is composed of your username followed by a period and the tileset's unique identifier. Example request: Delete tileset $ curl -X DELETE "https://api.mapbox.com/tilesets/v1/{username.tileset_id}?access_token=YOUR MAPBOX ACCESS TOKEN This endpoint requires a token with tilesets:write scope. " Response: Delete tileset HTTP 200 Retrieve TileJSON metadata GET /v4/{tileset_id}.json Given a valid Mapbox tileset ID, returns TileJSON metadata for that tileset.

Required parameters Description tileset_id Unique identifier for the vector tileset in the format username.id. To composite multiple vector tilesets, use a comma-separated list of up to 15 tileset IDs. This endpoint can be further customized with the optional secure parameter:

Optional parameters Description secure By default, resource URLs in the retrieved TileJSON (such as in the "tiles" array) will use the HTTP scheme. Include this query parameter in your request to receive HTTPS resource URLs instead. Example request: Retrieve TileJSON metadata $ curl "https://api.mapbox.com/v4/examples.civir98a801cq2oo6w6mk1aor-9msik.json?access_token=pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ"

Request HTTPS resource URLs in the retrieved TileJSON
$ curl "https://api.mapbox.com/v4/examples.civir98a801cq2oo6w6mk1aor-9msik.json?secure&access_token=pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ" Response: Retrieve TileJSON metadata Returns TileJSON metadata for a tileset. The TileJSON object describes a map's resources, like tiles, markers, and UTFGrid, as well as its name, description, and centerpoint.

Example response: Retrieve TileJSON metadata { "bounds": [ -87.769775, 41.715515, -87.530221, 41.940403 ], "center": [ -87.649998, 41.82795900000001, 0 ], "created": 1479169405055, "filesize": 3321, "format": "pbf", "id": "examples.civir98a801cq2oo6w6mk1aor-9msik", "mapbox_logo": true, "maxzoom": 10, "minzoom": 0, "modified": 1542143499397, "name": "chicago-parks", "private": false, "scheme": "xyz", "tilejson": "2.2.0", "tiles": [ "http://a.tiles.mapbox.com/v4/examples.civir98a801cq2oo6w6mk1aor-9msik/{z}/{x}/{y}.vector.pbf", "http://b.tiles.mapbox.com/v4/examples.civir98a801cq2oo6w6mk1aor-9msik/{z}/{x}/{y}.vector.pbf" ], "vector_layers": [{ "description": "", "fields": { "description": "String", "id": "String", "marker-color": "String", "marker-size": "String", "marker-symbol": "String", "title": "String" }, "id": "chicago-parks", "maxzoom": 22, "minzoom": 0, "source": "examples.civir98a801cq2oo6w6mk1aor-9msik", "source_name": "chicago-parks" }], "version": "1.0.0", "webpage": "http://a.tiles.mapbox.com/v4/examples.civir98a801cq2oo6w6mk1aor-9msik/page.html" } Mapbox Tiling Service errors Response body message HTTP status code Description Not Authorized - No Token 401 No token was used in the query. Not Authorized - Invalid Token 401 Check the access token you used in the query. This endpoint requires a token with {scope} scope 403 The access token used in the query needs the specified scope. Not Found 404 The resource or the account does not exist. Cannot find tileset 404 Check the tileset ID you used in the query. The requested url's querystring "limit" property contains in invalid value. 422 The limit specified in the query is larger than 500, or contains non-numeric characters. Resource is locked and cannot be modified 409 A tileset source is "locked" when in use by an active tileset publish job. While locked the resource cannot be deleted or modified. Once a publish job is complete the resource is unlocked. Invalid start key 422 Check the start key used in the query. Classic styles are no longer supported; see https://blog.mapbox.com/deprecating-studio-classic-styles-d8892ac38cb4 for more information 410 This is a deprecation notice from API requests for Classic styles, which are no longer supported. For help troubleshooting common errors, see our MTS errors documentation.

Mapbox Tiling Service restrictions and limits You must make requests over HTTPS. HTTP is not supported. Mapbox Tiling Service endpoint Requests per minute Size limits Create a tileset source Beta This feature is in public beta and is subject to changes. 100 Tileset sources can be composed of up to 10 source files.

Each uploaded file must have a maximum size of 20 GB. The maximum combined total size of all files that compose a tileset source is 50 GB. If the total size of all the files that compose a tileset source is greater than 50 GB, MTS will return a response that contains an error property with more details. Retrieve tileset source information Beta This feature is in public beta and is subject to changes. 100,000 — List tileset sources Beta This feature is in public beta and is subject to changes. 100,000 — Delete a tileset source Beta This feature is in public beta and is subject to changes. 100 — Create a tileset Beta This feature is in public beta and is subject to changes. 100 — Publish a tileset Beta This feature is in public beta and is subject to changes. 2 The size of a single vector tile is limited to 500 KB. If a job drops features due to tile size, this will be noted in the warnings field of the job object. Update tileset information Beta This feature is in public beta and is subject to changes. 100 — Retrieve information about a single tileset job Beta This feature is in public beta and is subject to changes. 100,000 — List information about all jobs for a tileset Beta This feature is in public beta and is subject to changes. 100,000 — View the global queue Beta This feature is in public beta and is subject to changes. 100,000 — Validate a recipe Beta This feature is in public beta and is subject to changes. 100 — Retrieve a tileset's recipe Beta This feature is in public beta and is subject to changes. 100,000 — Update a tileset's recipe Beta This feature is in public beta and is subject to changes. 100 — List tilesets 100,000 — Delete a tileset 100,000 — Retrieve TileJSON metadata 100,000 — If you require a higher rate limit, contact us. Mapbox Tiling Service pricing Billed by tileset processing and tileset hosting See rates and discounts per tileset processing and tileset hosting in the pricing page's Tilesets section When you use Mapbox Tiling Service, usage statistics can be reviewed on your Statistics dashboard. If an account's Tilesets usage exceeds the monthly free tier, you will see two different line items on the account's invoice: tileset processing and tileset hosting. The cost of each will depend on the area of your tiled data and the precision level of your tilesets.

To see details on the precision level and area tiled for a specific tileset, go to your Tilesets page and click on the tileset's name to visit the Tileset explorer. The Tileset explorer provides a link to the pricing calculator in the Billing metrics section, where you can input the square kilometers of your tiled data for a pricing estimate.

To learn more about how pricing works for this service, see the MTS pricing guide.

var map = L.map('map');

L.tileLayer('https://api.mapbox.com/styles/v1/{id}/tiles/{z}/{x}/{y}?access_token={accessToken}', { attribution: '© Mapbox © OpenStreetMap Improve this map', tileSize: 512, maxZoom: 18, zoomOffset: -1, id: 'mapbox/streets-v11', accessToken: 'pk.eyJ1IjoicXVhbnRvbXBoYW50b201NzMiLCJhIjoiY2p2eTZkMWpxMDhmZzQzcDFrbjRobXY2YiJ9.cJqSyyaC5iXcDT1O4ztrQQ' }).addTo(map);
