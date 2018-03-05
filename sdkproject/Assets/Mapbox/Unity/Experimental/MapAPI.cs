namespace Mapbox.Unity.Map
{
	using System;
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
		LocationBasedMap,
		WorldSimulator,
		ARTableTop,
		ARWorldScale,
	}

	public enum MapVisualizationType
	{
		Flat2D,
		Globe3D
	}

	public enum MapPlacementType
	{
		AtTileCenter,
		AtLocationCenter
	}

	public enum MapStreamingType
	{
		Zoomable,
		Static,
	}

	public enum MapScalingType
	{
		WorldScale,
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
		CameraBounds,
		RangeAroundCenter,
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
		Streets,
		Outdoors,
		Dark,
		Light,
		Satellite,
		SatelliteStreet,
		Custom,
		None
	}

	public enum ElevationSourceType
	{
		MapboxTerrain,
		Custom,
	}

	public enum VectorSourceType
	{
		MapboxStreets,
		Custom,
		None
	}
	public enum ElevationLayerType
	{
		None,
		LowPolygonTerrain,
		Terrain,
		// TODO : Might want to reconsider this option. 
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
