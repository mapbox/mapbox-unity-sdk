namespace Mapbox.Unity.Map
{
	using System.ComponentModel;

	// Map related enums
	public enum MapPresetType
	{
		[Description("Map with imagery and terrain, used along with a location provider.")]
		LocationBasedMap,
		[Description("Map with imagery and terrain and vector data - building,roads and poi's.")]
		WorldSimulator,
		[Description("Map with imagery and terrain and vector data, used for AR tabletop scenario.")]
		ARTableTop,
		[Description("Map with imagery and terrain and vector data, used for world scale AR scenario.")]
		ARWorldScale,
	}

	public enum MapPlacementType
	{
		[Description("Map's root is located at the center of tile containing location specified.")]
		AtTileCenter,
		[Description("Map's root is located at the location specified.")]
		AtLocationCenter
	}

	public enum MapScalingType
	{
		[Description("Map is rendered at actual scale, unity to mercator conversion factor is ignored. ")]
		WorldScale,
		[Description("Map is rendered at the scale defined by unity to mercator conversion factor. ")]
		Custom
	}

	public enum MapUnitType
	{
		meters,
		kilometers,
		miles
	}

	public enum MapExtentType
	{
		[Description("Map extent defined by the camera's viewport bounds.")]
		CameraBounds,
		[Description("Map extent defined by range of tiles around map's center tile.")]
		RangeAroundCenter,
		[Description("Map extent defined by range of tiles around a target transform.")]
		RangeAroundTransform,
		[Description("Map extent defined by custom tile provider.")]
		Custom,
	}

	public enum MapCoordinateSystemType
	{
		WebMercator,
	}

	//Layer related enums. 
	public enum MapLayerType
	{
		Imagery,
		Elevation,
		Vector
	}

	public enum VectorPrimitiveType
	{
		Point,
		Line,
		Polygon,
		Custom
	}

	public enum UvMapType
	{
		[Description("Use image texture using tiled UV.")]
		Tiled,
		[Description("Use image texture from the Imagery source as texture for roofs. ")]
		Satellite,
		[Description("Use an image texture atlas to define textures for roof & sides of buildings.")]
		Atlas,
		[Description("Use an image texture atlas and a color pallete to define textures for roof & sides of buildings.")]
		AtlasWithColorPalette,
	}

	public enum ImagerySourceType
	{
		[Description("Mapbox Streets is a comprehensive, general-purpose map that emphasizes accurate, legible styling of road and transit networks")]
		MapboxStreets,
		[Description("Mapbox Outdoors is a general-purpose map with curated tilesets and specialized styling tailored to hiking, biking, and the most adventurous use cases.")]
		MapboxOutdoors,
		[Description("Mapbox Light and Mapbox Dark are subtle, full-featured maps designed to provide geographic context while highlighting the data on your analytics dashboard, data visualization, or data overlay.")]
		MapboxDark,
		[Description("Mapbox Light and Mapbox Dark are subtle, full-featured maps designed to provide geographic context while highlighting the data on your analytics dashboard, data visualization, or data overlay.")]
		MapboxLight,
		[Description("Mapbox Satellite is our full global base map that is perfect as a blank canvas or an overlay for your own data.")]
		MapboxSatellite,
		[Description("Mapbox Satellite Streets combines our Mapbox Satellite with vector data from Mapbox Streets. The comprehensive set of road, label, and POI information brings clarity and context to the crisp detail in our high-resolution satellite imagery.")]
		MapboxSatelliteStreet,
		[Description("Use custom tilesets created using Mapbox studio.")]
		Custom,
		[Description("Turn off image rendering.")]
		None
	}

	public enum ElevationSourceType
	{
		[Description("Mapbox Terrain provides digital elevation model with worldwide coverage. ")]
		MapboxTerrain,
		[Description("Use custom digital elevation model tileset.")]
		Custom,
		[Description("Render flat terrain.")]
		None
	}

	public enum VectorSourceType
	{
		[Description("Mapbox Streets along with unique identifiers for building features. Combines building footprints that may be in different tiles.")]
		MapboxStreetsWithBuildingIds,
		[Description("Mapbox Streets vector tiles are largely based on data from OpenStreetMap, a free & global source of geographic data built by volunteers.")]
		MapboxStreets,
		[Description("Use custom tilesets created using Mapbox studio. ")]
		Custom,
		[Description("Turn off vector data rendering.")]
		None
	}
	public enum ElevationLayerType
	{
		[Description("Render flat terrain with no elevation.")]
		FlatTerrain,
		[Description("Render terrain with elevation from the source specified.")]
		TerrainWithElevation,
		[Description("Render low polygon terrain with elevation from the source specified")]
		LowPolygonTerrain,

		// TODO : Might want to reconsider this option. 
		[Description("Render terrain with no elevation for a globe.")]
		GlobeTerrain
	}
	public enum ExtrusionType
	{
		None,
		PropertyHeight,
		MinHeight,
		MaxHeight,
		RangeHeight,
		AbsoluteHeight,
	}

	public enum ExtrusionGeometryType
	{
		[Description("Extrudes both roof and side wall geometry of the vector feature.")]
		RoofAndSide,
		[Description("Extrudes only roof geometry of the vector feature.")]
		RoofOnly,
		[Description("Extrudes only side wall geometry of the vector feature.")]
		SideOnly,
	}
}
