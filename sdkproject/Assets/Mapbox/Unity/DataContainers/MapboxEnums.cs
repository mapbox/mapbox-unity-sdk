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
		Tiled = 0,
		[Description("Use an image texture atlas to define textures for roof & sides of buildings.")]
		Atlas = 2,
		[Description("Use an image texture atlas and a color pallete to define textures for roof & sides of buildings.")]
		AtlasWithColorPalette = 3,
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
		[Description("No extrusion.")]
		None,
		[Description("Extrude features using the property value.")]
		PropertyHeight,
		[Description("Extrude features using the property value. Sets height based on property's minimum height, if height isn't uniform. Results in flat tops.")]
		MinHeight,
		[Description("Extrude features using the property value. Sets height based on property's maximum height, if height isn't uniform. Results in flat tops.")]
		MaxHeight,
		[Description("Extrude features using the property value. Values are clamped in to min and max values if they are lower or greater than min,max values respectively.")]
		RangeHeight,
		[Description("Extrude all features using the fixed value.")]
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

	public enum ColliderType
	{
		[Description("No collider.")]
		None,
		[Description("Box collider addded to the GameObject.")]
		BoxCollider,
		[Description("Mesh collider added to the GameObject.")]
		MeshCollider,
		[Description("Sphere collider added to the GameObject.")]
		SphereCollider,
	}

	public enum MapFeatureType
	{
		[Description("Building Layer.")]
		Building,
		[Description("Road Layer.")]
		Road,
		[Description("Parkland Layer.")]
		Parkland,
	};

	public enum StyleTypes
	{
		[Description("Custom style.")]
		Custom,
		[Description("Simple style combines stylized vector designs with scriptable palettes to create a simple, procedurally colored rendering style.")]
		Simple,
		[Description("Light style uses colored materials to create light, greyscale shading for your map.")]
		Light,
		[Description("Dark style uses colored materials to create dark, greyscale shading for your map.")]
		Dark,
		[Description("Realistic style combines modern, urban designs with physically based rendering materials to help create a contemporary, realistic rendering style.")]
		Realistic,
		[Description("Fantasy style combines old world medieval designs with physically based rendering materials to help create a fantasy rendering style.")]
		Fantasy,
		[Description("Satellite style uses high-resolution satellite imagery as a texture set. The comprehensive set of road, label, and POI information brings clarity and context to the crisp detail in our high-resolution satellite imagery.")]
		Satellite,

	}

	public enum LocationPrefabFindBy
	{
		[Description("Display points of interest based on a choice of categories")]
		MapboxCategory,
		[Description("Display points of interest based on name")]
		POIName,
		[Description("Display points of interest at specific address or geographical co-ordinates on the map")]
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
}
