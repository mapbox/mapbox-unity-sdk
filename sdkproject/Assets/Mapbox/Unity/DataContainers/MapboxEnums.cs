namespace Mapbox.Unity.Map
{
	using System.ComponentModel;

	// Map related enums
	public enum MapPresetType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Map with imagery and terrain, used along with a location provider.")]
#endif
		LocationBasedMap,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map with imagery and terrain and vector data - building,roads and poi's.")]
#endif
		WorldSimulator,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map with imagery and terrain and vector data, used for AR tabletop scenario.")]
#endif
		ARTableTop,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map with imagery and terrain and vector data, used for world scale AR scenario.")]
#endif
		ARWorldScale,
	}

	public enum MapPlacementType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Map's root is located at the center of tile containing location specified.")]
#endif
		AtTileCenter,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map's root is located at the location specified.")]
#endif
		AtLocationCenter
	}

	public enum MapScalingType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Map is rendered at actual scale, unity to mercator conversion factor is ignored. ")]
#endif
		WorldScale,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map is rendered at the scale defined by unity to mercator conversion factor. ")]
#endif
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
#if !ENABLE_WINMD_SUPPORT
		[Description("Map extent defined by the camera's viewport bounds.")]
#endif
		CameraBounds,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map extent defined by range of tiles around map's center tile.")]
#endif
		RangeAroundCenter,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map extent defined by range of tiles around a target transform.")]
#endif
		RangeAroundTransform,
#if !ENABLE_WINMD_SUPPORT
		[Description("Map extent defined by custom tile provider.")]
#endif
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
#if !ENABLE_WINMD_SUPPORT
		[Description("Use image texture using tiled UV.")]
#endif
		Tiled = 0,
#if !ENABLE_WINMD_SUPPORT
		[Description("Use an image texture atlas to define textures for roof & sides of buildings.")]
#endif
		Atlas = 2,
#if !ENABLE_WINMD_SUPPORT
		[Description("Use an image texture atlas and a color pallete to define textures for roof & sides of buildings.")]
#endif
		AtlasWithColorPalette = 3,
	}

	public enum ImagerySourceType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Streets is a comprehensive, general-purpose map that emphasizes accurate, legible styling of road and transit networks")]
#endif
		MapboxStreets,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Outdoors is a general-purpose map with curated tilesets and specialized styling tailored to hiking, biking, and the most adventurous use cases.")]
#endif
		MapboxOutdoors,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Light and Mapbox Dark are subtle, full-featured maps designed to provide geographic context while highlighting the data on your analytics dashboard, data visualization, or data overlay.")]
#endif
		MapboxDark,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Light and Mapbox Dark are subtle, full-featured maps designed to provide geographic context while highlighting the data on your analytics dashboard, data visualization, or data overlay.")]
#endif
		MapboxLight,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Satellite is our full global base map that is perfect as a blank canvas or an overlay for your own data.")]
#endif
		MapboxSatellite,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Satellite Streets combines our Mapbox Satellite with vector data from Mapbox Streets. The comprehensive set of road, label, and POI information brings clarity and context to the crisp detail in our high-resolution satellite imagery.")]
#endif
		MapboxSatelliteStreet,
#if !ENABLE_WINMD_SUPPORT
		[Description("Use custom tilesets created using Mapbox studio.")]
#endif
		Custom,
#if !ENABLE_WINMD_SUPPORT
		[Description("Turn off image rendering.")]
#endif
		None
	}

	public enum ElevationSourceType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Terrain provides digital elevation model with worldwide coverage. ")]
#endif
		MapboxTerrain,
#if !ENABLE_WINMD_SUPPORT
		[Description("Use custom digital elevation model tileset.")]
#endif
		Custom,
#if !ENABLE_WINMD_SUPPORT
		[Description("Render flat terrain.")]
#endif
		None
	}

	public enum VectorSourceType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Streets along with unique identifiers for building features. Combines building footprints that may be in different tiles.")]
#endif
		MapboxStreetsWithBuildingIds = 0,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Streets vector tiles are largely based on data from OpenStreetMap, a free & global source of geographic data built by volunteers.")]
#endif
		MapboxStreets = 1,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Streets vector tiles are largely based on data from OpenStreetMap, a free & global source of geographic data built by volunteers.")]
#endif
		MapboxStreetsV8WithBuildingIds = -1,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mapbox Streets vector tiles are largely based on data from OpenStreetMap, a free & global source of geographic data built by volunteers.")]
#endif
		MapboxStreetsV8 = -2,
#if !ENABLE_WINMD_SUPPORT
		[Description("Use custom tilesets created using Mapbox studio. ")]
#endif
		Custom = 2,
#if !ENABLE_WINMD_SUPPORT
		[Description("Turn off vector data rendering.")]
#endif
		None = 3
	}
	public enum ElevationLayerType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Render flat terrain with no elevation.")]
#endif
		FlatTerrain,
#if !ENABLE_WINMD_SUPPORT
		[Description("Render terrain with elevation from the source specified.")]
#endif
		TerrainWithElevation,
#if !ENABLE_WINMD_SUPPORT
		[Description("Render low polygon terrain with elevation from the source specified")]
#endif
		LowPolygonTerrain,

		// TODO : Might want to reconsider this option.
#if !ENABLE_WINMD_SUPPORT
		[Description("Render terrain with no elevation for a globe.")]
#endif
		GlobeTerrain
	}

	public enum TileTerrainType
	{
		//starting from -1 to match ElevationLayerType
		None = -1,
		Flat = 0,
		Elevated = 1,
		LowPoly = 2,
		Globe = 3
	}

	public enum ExtrusionType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("No extrusion.")]
#endif
		None,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrude features using the property value.")]
#endif
		PropertyHeight,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrude features using the property value. Sets height based on property's minimum height, if height isn't uniform. Results in flat tops.")]
#endif
		MinHeight,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrude features using the property value. Sets height based on property's maximum height, if height isn't uniform. Results in flat tops.")]
#endif
		MaxHeight,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrude features using the property value. Values are clamped in to min and max values if they are lower or greater than min,max values respectively.")]
#endif
		RangeHeight,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrude all features using the fixed value.")]
#endif
		AbsoluteHeight,


	}

	public enum ExtrusionGeometryType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrudes both roof and side wall geometry of the vector feature.")]
#endif
		RoofAndSide,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrudes only roof geometry of the vector feature.")]
#endif
		RoofOnly,
#if !ENABLE_WINMD_SUPPORT
		[Description("Extrudes only side wall geometry of the vector feature.")]
#endif
		SideOnly,
	}

	public enum ColliderType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("No collider.")]
#endif
		None,
#if !ENABLE_WINMD_SUPPORT
		[Description("Box collider addded to the GameObject.")]
#endif
		BoxCollider,
#if !ENABLE_WINMD_SUPPORT
		[Description("Mesh collider added to the GameObject.")]
#endif
		MeshCollider,
#if !ENABLE_WINMD_SUPPORT
		[Description("Sphere collider added to the GameObject.")]
#endif
		SphereCollider,
	}

	public enum MapFeatureType
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Building Layer.")]
#endif
		Building,
#if !ENABLE_WINMD_SUPPORT
		[Description("Road Layer.")]
#endif
		Road,
#if !ENABLE_WINMD_SUPPORT
		[Description("Parkland Layer.")]
#endif
		Parkland,
	};

	public enum StyleTypes
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Custom style.")]
#endif
		Custom,
#if !ENABLE_WINMD_SUPPORT
		[Description("Simple style combines stylized vector designs with scriptable palettes to create a simple, procedurally colored rendering style.")]
#endif
		Simple,
#if !ENABLE_WINMD_SUPPORT
		[Description("Light style uses colored materials to create light, greyscale shading for your map.")]
#endif
		Light,
#if !ENABLE_WINMD_SUPPORT
		[Description("Dark style uses colored materials to create dark, greyscale shading for your map.")]
#endif
		Dark,
#if !ENABLE_WINMD_SUPPORT
		[Description("Realistic style combines modern, urban designs with physically based rendering materials to help create a contemporary, realistic rendering style.")]
#endif
		Realistic,
#if !ENABLE_WINMD_SUPPORT
		[Description("Fantasy style combines old world medieval designs with physically based rendering materials to help create a fantasy rendering style.")]
#endif
		Fantasy,
#if !ENABLE_WINMD_SUPPORT
		[Description("Satellite style uses high-resolution satellite imagery as a texture set. The comprehensive set of road, label, and POI information brings clarity and context to the crisp detail in our high-resolution satellite imagery.")]
#endif
		Satellite,
#if !ENABLE_WINMD_SUPPORT
		[Description("Color style uses user-defined color and opacity to create colorful, flat shading for your map.")]
#endif
		Color,
	}

	public enum SamplePalettes
	{
		City,
		Urban,
		Warm,
		Cool,
		Rainbow
	}

	public enum LocationPrefabFindBy
	{
#if !ENABLE_WINMD_SUPPORT
		[Description("Display points of interest based on a choice of categories")]
#endif
		MapboxCategory,
#if !ENABLE_WINMD_SUPPORT
		[Description("Display points of interest based on name")]
#endif
		POIName,
#if !ENABLE_WINMD_SUPPORT
		[Description("Display points of interest at specific address or geographical co-ordinates on the map")]
#endif
		AddressOrLatLon,
	}

	public enum LocationPrefabCategories
	{
		None = 0,
		AnyCategory = ~0,
		ArtsAndEntertainment = 1 << 0,
		Food = 1 << 1,
		Nightlife = 1 << 2,
		OutdoorsAndRecreation = 1 << 3,
		Services = 1 << 4,
		Shops = 1 << 5,
		Transportation = 1 << 6
	}

	public enum FeatureProcessingStage
	{
		PreProcess,
		Process,
		PostProcess
	}

	public enum PresetFeatureType
	{
		Buildings,
		Roads,
		Landuse,
		Points,
		Custom
	}

	public enum JoinType
	{
		Miter = 0,
		Round = 1,
		Bevel = 2,
		Butt,
		Square,
		Fakeround,
		Flipbevel
	}

	public enum LineJoinType
	{
		Miter = 0,
		Round = 1,
		Bevel = 2
	}

	public enum LineCapType
	{
		Butt = 3,
		Round = 1,
		Square = 4
	}

}
