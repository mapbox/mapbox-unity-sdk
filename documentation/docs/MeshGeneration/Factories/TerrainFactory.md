# Terrain Factory

Terrain Factory creates the world base mesh. It's able to create flat terrain, real height terrain or modified terrain where real height is multiplied by a factor.

####Parameters

**Map Type**: Flat, Height or Modified Height.

**Map ID Type**: Data source used in the *Height* and *Modified Height* map types. Default value is "mapbox.terrain-rgb".

**Height Multiplier**: Used in *Modified Height* map type as a simple multiplier on queried terrain height.

**Resolution**: Terrain is generated as a grid mesh and resolution represents the sample count on each edge of this grid.

**Material**: Base material for the generated mesh. Will most likely be overridden by MapImageFactory but useful if you're not using it.

**Update**: Update function recreates the terrain using current parameters. 

####Methods

Terrain factory has three public methods; `Initialize`, `Register` and `Update` coming from the base `Factory` class.

`Initialize` method clears and reinitalizes the registered tile list. Can be used as a reset method.

`Register` method adds new tiles to the registered tiles list to be processed.

`Update` method clears the mesh data for all tiles and recraetes them using current settings. Clearing the mesh data first is important for stitching tile edges to each other.

And bunch of private methods;

`Run` starts the processing of a tile and directs tile to different functions depending on the `Map Type` parameter. These methods can be extracted into external modules in the future.




##Flat Terrain

![flat terrain](images/flatterrain.png)

##Real Height

![real height terrain](images/realheight.png)

##Modified Terrain Height

![modified terrain](images/modifiedterrain.png)