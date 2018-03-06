namespace Mapbox.Unity.Map
{
	using System;
	using System.ComponentModel;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Interfaces;

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

	public enum MapVisualizationType
	{
		Flat2D,
		Globe3D
	}

	public enum MapPlacementType
	{
		[Description("Map's root is located at the center of tile containing location specified.")]
		AtTileCenter,
		[Description("Map's root is located at the location specified.")]
		AtLocationCenter
	}

	public enum MapStreamingType
	{
		Zoomable,
		Static,
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
		RangeAroundTransform
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
		Polygon
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
		[Description("Mapbox Terrain provides digital elevation model with world wide coverage. ")]
		MapboxTerrain,
		[Description("Use custom digital elevation model tileset.")]
		Custom,
		[Description("Render flat terrain.")]
		None
	}

	public enum VectorSourceType
	{
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
		RoofAndSide,
		RoofOnly,
		SideOnly,
	}

	[Serializable]
	public class PropertyValuePair
	{
		public string featureKey;
		public string featureValue;
	}

	[Serializable]
	public class GeometryStylingOptions
	{
		public bool isExtruded;

	}

	public class MapAPI : MonoBehaviour
	{
		//protected UnifiedMap _map;
		//protected AbstractTileProvider _tileProvider;
		//protected AbstractMapVisualizer _mapVisualizer;
		////protected AbstractTileFactory _elevationLayer;

		//[SerializeField]
		//UnifiedMapOptions _unifiedMapOptions = new UnifiedMapOptions();

		//public event Action OnInitialized = delegate { };

		//public UnifiedMap Map
		//{
		//	get
		//	{
		//		return _map;
		//	}
		//}

		//void SendInitialized()
		//{
		//	Debug.Log("MapManager Init");
		//	OnInitialized();
		//}

		//void SetUpFlat2DMap()
		//{
		//	switch (_unifiedMapOptions.mapOptions.placementOptions.placementType)
		//	{
		//		case MapPlacementType.AtTileCenter:
		//			_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
		//			break;
		//		case MapPlacementType.AtLocationCenter:
		//			_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtLocationCenterStrategy();
		//			break;
		//		default:
		//			_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
		//			break;
		//	}

		//	switch (_unifiedMapOptions.mapOptions.scalingOptions.scalingType)
		//	{
		//		case MapScalingType.WorldScale:
		//			_unifiedMapOptions.mapOptions.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
		//			break;
		//		case MapScalingType.Custom:
		//			_unifiedMapOptions.mapOptions.scalingOptions.scalingStrategy = new MapScalingAtUnityScaleStrategy();
		//			break;
		//		default:
		//			break;
		//	}


		//	_map.OnInitialized += SendInitialized;

		//	ITileProviderOptions tileProviderOptions = _unifiedMapOptions.mapOptions.placementOptions.extentOptions.GetTileProviderOptions();
		//	// Setup tileprovider based on type. 
		//	switch (_unifiedMapOptions.mapOptions.placementOptions.extentOptions.extentType)
		//	{
		//		case MapExtentType.CameraBounds:
		//			_tileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
		//			break;
		//		case MapExtentType.RangeAroundCenter:
		//			_tileProvider = gameObject.AddComponent<RangeTileProvider>();
		//			break;
		//		case MapExtentType.RangeAroundTransform:
		//			_tileProvider = gameObject.AddComponent<RangeAroundTransformTileProvider>();
		//			break;
		//		default:
		//			break;
		//	}

		//	_tileProvider.SetOptions(tileProviderOptions);



		//	var mapImageryLayers = new ImageryLayer();
		//	mapImageryLayers.Initialize(_unifiedMapOptions.imageryLayerProperties);


		//	var mapElevationLayer = new TerrainLayer();
		//	mapElevationLayer.Initialize(_unifiedMapOptions.elevationLayerProperties);
		//	//var terrainFactory = ScriptableObject.CreateInstance<TerrainWithSideWallsFactory>();
		//	//terrainFactory._mapId = "mapbox.terrain-rgb";


		//	var mapVectorLayer = new VectorLayer();
		//	mapVectorLayer.Initialize(_unifiedMapOptions.vectorLayerProperties);
		//	_mapVisualizer.Factories = new List<AbstractTileFactory>
		//	{
		//		mapElevationLayer.ElevationFactory,
		//		mapImageryLayers.ImageFactory,
		//		mapVectorLayer.VectorFactory
		//	};

		//	_map.TileProvider = _tileProvider;
		//	_map.MapVisualizer = _mapVisualizer;

		//	_map.InitializeMap(_unifiedMapOptions.mapOptions);

		//	Debug.Log("Setup 2DMap done. ");
		//}


		//// Use this for initialization
		//void Start()
		//{
		//	_map = gameObject.AddComponent<UnifiedMap>();
		//	// Setup a visualizer to get a "Starter" map.
		//	_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();

		//	switch (_unifiedMapOptions.mapOptions.placementOptions.visualizationType)
		//	{
		//		case MapVisualizationType.Flat2D:
		//			SetUpFlat2DMap();
		//			break;
		//		case MapVisualizationType.Globe3D:
		//			break;
		//		default:
		//			break;
		//	}

		//	_unifiedMapOptions.vectorLayerProperties.vectorSubLayers = new List<VectorSubLayerProperties>();
		//	_unifiedMapOptions.vectorLayerProperties.vectorSubLayers.Add(new VectorSubLayerProperties()
		//	{
		//		coreOptions = new CoreVectorLayerProperties()
		//		{
		//			layerName = "Building"
		//		}
		//	});
		//}

		//// Update is called once per frame
		//void Update()
		//{

		//}
	}
}
