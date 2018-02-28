namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Unity.MeshGeneration.Interfaces;


	// Map related enums
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
		// Camera bounds will mean 
		// CameraBoundsTile Provider for all cases except Zoomable MapType
		// For Zoomable map - QuadTreeTileProvider.

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
		SideOnly,
		RoofOnly,
		RoofAndSide,
	}
	public static class MapboxDefaultImagery
	{
		public static Style GetParameters(ImagerySourceType defaultImagery)
		{
			Style defaultStyle = new Style();
			switch (defaultImagery)
			{
				case ImagerySourceType.Streets:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/streets-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.Outdoors:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/outdoors-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.Dark:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/dark-v9",
						Name = "Dark"
					};

					break;
				case ImagerySourceType.Light:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/light-v9",
						Name = "Light"
					};

					break;
				case ImagerySourceType.Satellite:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/light-v9",
						Name = "Satellite"
					};

					break;
				case ImagerySourceType.SatelliteStreet:
					defaultStyle = new Style
					{
						Id = "mapbox.satellite",
						Name = "Satellite Streets"
					};

					break;
				case ImagerySourceType.Custom:
					throw new Exception("Invalid type : Custom");
				case ImagerySourceType.None:
					throw new Exception("Invalid type : None");
				default:
					break;
			}

			return defaultStyle;
		}
	}

	[Serializable]
	public class MapLocationOptions
	{
		[Geocode]
		[SerializeField]
		public string latitudeLongitude = "0,0";
		[Range(0, 22)]
		public float zoom = 4.0f;

		//TODO : Add Coordinate conversion class. 
		public MapCoordinateSystemType coordinateSystemType;
	}

	public interface ITileProviderOptions
	{
	}

	[Serializable]
	public class RangeTileProviderOptions : ITileProviderOptions
	{
		public int west;
		public int north;
		public int east;
		public int south;


		public void SetOptions(int northRange, int southRange, int eastRange, int westRange)
		{
			west = westRange;
			north = northRange;
			east = eastRange;
			south = southRange;
		}
	}

	[Serializable]
	public class RangeAroundTransformTileProviderOptions : ITileProviderOptions
	{
		public Transform targetTransform;
		public int visibleBuffer;
		public int disposeBuffer;

		public void SetOptions(Transform tgtTransform, int visibleRange, int disposeRange)
		{
			targetTransform = tgtTransform;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
		}
	}

	[Serializable]
	public class CameraBoundsTileProviderOptions : ITileProviderOptions
	{
		public Camera camera;
		public int visibleBuffer;
		public int disposeBuffer;
		public float updateInterval;

		public void SetOptions(Camera mapCamera, int visibleRange, int disposeRange, float updateTimeInterval)
		{
			camera = mapCamera;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
			updateInterval = updateTimeInterval;
		}
	}

	public class TileProviderOptions : ITileProviderOptions
	{
		public static ITileProviderOptions RangeAroundCenterOptions(int northRange, int southRange, int eastRange, int westRange)
		{
			return new RangeTileProviderOptions()
			{
				west = westRange,
				north = northRange,
				east = eastRange,
				south = southRange
			};
		}

		public static ITileProviderOptions RangeAroundTransformOptions(Transform tgtTransform, int visibleRange, int disposeRange)
		{
			return new RangeAroundTransformTileProviderOptions
			{
				targetTransform = tgtTransform,
				visibleBuffer = visibleRange,
				disposeBuffer = disposeRange,
			};
		}
		public static ITileProviderOptions CameraBoundsProviderOptions(Camera camera, int visibleRange, int disposeRange, float updateTime)
		{
			return new CameraBoundsTileProviderOptions
			{
				camera = camera,
				visibleBuffer = visibleRange,
				disposeBuffer = disposeRange,
				updateInterval = updateTime
			};
		}
	}

	[Serializable]
	public class MapExtentOptions
	{
		public MapExtentType extentType = MapExtentType.CameraBounds;

		public CameraBoundsTileProviderOptions cameraBoundsOptions;
		public RangeTileProviderOptions rangeAroundCenterOptions;
		public RangeAroundTransformTileProviderOptions rangeAroundTransformOptions;

		public MapExtentOptions(MapExtentType type)
		{
			extentType = type;
		}

		public ITileProviderOptions GetTileProviderOptions()
		{
			ITileProviderOptions options = new TileProviderOptions();
			switch (extentType)
			{
				case MapExtentType.CameraBounds:
					options = cameraBoundsOptions;// TileProviderOptions.CameraBoundsProviderOptions(camera, visibleBuffer, disposeBuffer, updateInterval);
					break;
				case MapExtentType.RangeAroundCenter:
					options = rangeAroundCenterOptions;// TileProviderOptions.RangeAroundCenterOptions(north, south, east, west);
					break;
				case MapExtentType.RangeAroundTransform:
					options = rangeAroundTransformOptions; //TileProviderOptions.RangeAroundTransformOptions(targetTransform, visibleBuffer, disposeBuffer);
					break;
				default:
					break;
			}
			return options;
		}
	}

	[Serializable]
	public class MapPlacementOptions
	{
		public MapVisualizationType visualizationType;
		public MapPlacementType placementType;
		//public MapStreamingType streamingType;
		public MapExtentOptions extentOptions;

		public IMapPlacementStrategy placementStrategy;
	}

	[Serializable]
	public class MapScalingOptions
	{
		public MapScalingType scalingType;
		public MapUnitType unitType;
		public float unityToMercatorConversionFactor;

		public IMapScalingStrategy scalingStrategy;
	}

	[Serializable]
	public class MapOptions
	{
		public MapLocationOptions locationOptions;
		public MapPlacementOptions placementOptions;
		public MapScalingOptions scalingOptions;
	}

	[Serializable]
	public class LayerSourceOptions
	{
		public bool isActive;
		public Style layerSource;

		public string Id
		{
			get
			{
				return layerSource.Id;
			}
			set
			{
				layerSource.Id = value;
			}
		}
	}

	public abstract class LayerProperties
	{
	}

	[Serializable]
	public class PropertyValuePair
	{
		public string featureKey;
		public string featureValue;
	}

	[Serializable]
	public class LayerPerformanceOptions
	{
		public bool isEnabled = true;
		public int entityPerCoroutine = 20;
	}

	[Serializable]
	public class LayerModifierOptions
	{
		public PositionTargetType moveFeaturePositionTo;
		[NodeEditorElement("Mesh Modifiers")]
		public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")]
		public List<GameObjectModifier> GoModifiers;
	}

	[Serializable]
	public class CoreVectorLayerProperties
	{
		public bool isActive = true;
		public string sublayerName = "untitled";
		public VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
		public string layerName = "layerName";
		public List<LayerFilter> filters;
		public LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.All;
		public bool snapToTerrain = true;
		public bool groupFeatures = false;
	}

	[Serializable]
	public class GeometryExtrusionOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(HeightModifier);
			}
		}
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		public string propertyName;
		public float minimumHeight;
		public float maximumHeight;
	}

	[Serializable]
	public class GeometryStylingOptions
	{
		public bool isExtruded;

	}

	[Serializable]
	public class GeometryMaterialOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(MaterialModifier);
			}
		}
		public bool projectMapImagery;
		public MaterialList[] materials = new MaterialList[2];

		public GeometryMaterialOptions()
		{
			materials = new MaterialList[2];
			materials[0] = new MaterialList();
			materials[1] = new MaterialList();
		}
	}

	[Serializable]
	public class VectorLayerProperties : LayerProperties
	{
		public LayerSourceOptions sourceOptions;
		public LayerPerformanceOptions performanceOptions;
		public GeometryStylingOptions defaultStylingOptions;
		public List<VectorSubLayerProperties> vectorSubLayers = new List<VectorSubLayerProperties>(2);
	}

	[Serializable]
	public class VectorSubLayerProperties : LayerProperties
	{
		public CoreVectorLayerProperties coreOptions;
		public GeometryExtrusionOptions extrusionOptions;
		public GeometryMaterialOptions materialOptions = new GeometryMaterialOptions();
		//public GeometryStylingOptions stylingOptions;
		public LayerModifierOptions modifierOptions;
	}

	[Serializable]
	public class ImageryRasterOptions
	{
		public bool useCompression = false;
		public bool useRetina = false;
		public bool useMipMap = false;
	}

	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties
	{
		public ImagerySourceType sourceType = ImagerySourceType.Streets;

		//[StyleSearch]
		// TODO : Do we really need a separate DS for default styles ??
		// Style struct should be enough to hold all tile-service info?
		//public Style CustomStyle = new Style();
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.Streets)

		};
		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();
	}

	[Serializable]
	public class TerrainSideWallOptions
	{
		public bool isActive = false;
		public float wallHeight = 10;
		public Material wallMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
	}

	[Serializable]
	public class UnityLayerOptions
	{
		public bool addToLayer = false;
		public int layerId = 0;
	}

	[Serializable]
	public class ElevationModificationOptions
	{
		public ElevationLayerType elevationLayerType = ElevationLayerType.None;
		public Material baseMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
		public int sampleCount = 10;
		public bool addCollider = false;
		public float exaggerationFactor = 1;
		public bool useRelativeHeight = true;
	}

	[Serializable]
	public class ElevationLayerProperties : LayerProperties
	{
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			layerSource = new Style()
			{
				Id = "mapbox.terrain-rgb"
			},
			isActive = true
		};
		public ElevationModificationOptions elevationLayerOptions;
		public UnityLayerOptions unityLayerOptions;
		public TerrainSideWallOptions sideWallOptions;
	}

	//public class Terrain
	// Layer Interfaces
	public interface ILayer
	{
		MapLayerType LayerType { get; }
		bool IsLayerActive { get; set; }
		string LayerSource { get; set; }

		LayerProperties LayerProperty { get; set; }

		//TODO : These methods should return a status. 
		void Initialize(LayerProperties properties);
		void Update(LayerProperties properties);
		void Remove();

	}

	public interface ITerrainLayer : ILayer
	{

	}

	public interface IImageryLayer : ILayer
	{

	}

	public interface IVectorDataLayer : ILayer
	{
		VectorPrimitiveType PrimitiveType { get; set; }
	}

	// Layer Concrete Implementation. 
	[Serializable]
	public class TerrainLayer : ITerrainLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Elevation;
			}
		}

		[SerializeField]
		bool _isLayerActive;
		public bool IsLayerActive
		{
			get
			{
				return _isLayerActive;
			}
			set
			{
				_isLayerActive = value;
			}
		}

		[SerializeField]
		string _layerSource;
		public string LayerSource
		{
			get
			{
				return _layerSource;
			}
			set
			{
				_layerSource = value;
			}
		}
		[SerializeField]
		ElevationLayerProperties _layerProperty;
		public LayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
			set
			{
				_layerProperty = (ElevationLayerProperties)value;
			}
		}

		public void Initialize(LayerProperties properties)
		{
			var elevationLayerProperties = (ElevationLayerProperties)properties;

			switch (elevationLayerProperties.elevationLayerOptions.elevationLayerType)
			{
				case ElevationLayerType.None:
					_elevationFactory = ScriptableObject.CreateInstance<FlatTerrainFactory>();
					break;
				case ElevationLayerType.LowPolygonTerrain:
					_elevationFactory = ScriptableObject.CreateInstance<LowPolyTerrainFactory>();
					break;
				case ElevationLayerType.Terrain:
					if (elevationLayerProperties.sideWallOptions.isActive)
					{
						_elevationFactory = ScriptableObject.CreateInstance<TerrainWithSideWallsFactory>();
					}
					else
					{
						Debug.Log("Setting Terrain Factory");
						_elevationFactory = ScriptableObject.CreateInstance<TerrainFactory>();
					}

					break;
				case ElevationLayerType.GlobeTerrain:
					_elevationFactory = ScriptableObject.CreateInstance<FlatSphereTerrainFactory>();
					break;
				default:
					break;
			}
			_elevationFactory.SetOptions(elevationLayerProperties);
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
		public AbstractTileFactory ElevationFactory
		{
			get
			{
				return _elevationFactory;
			}
		}
		private AbstractTileFactory _elevationFactory;

	}

	[Serializable]
	public class ImageryLayer : IImageryLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Imagery;
			}
		}

		[SerializeField]
		bool _isLayerActive;
		public bool IsLayerActive
		{
			get
			{
				return _isLayerActive;
			}
			set
			{
				_isLayerActive = value;
			}
		}

		[SerializeField]
		string _layerSource;
		public string LayerSource
		{
			get
			{
				return _layerSource;
			}
			set
			{
				_layerSource = value;
			}
		}

		[SerializeField]
		ImageryLayerProperties _layerProperty;
		public LayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
			set
			{
				_layerProperty = (ImageryLayerProperties)value;
			}
		}

		public void Initialize(LayerProperties properties)
		{
			var imageLayerProperties = (ImageryLayerProperties)properties;
			if (imageLayerProperties.sourceType != ImagerySourceType.Custom && imageLayerProperties.sourceType != ImagerySourceType.None)
			{
				imageLayerProperties.sourceOptions.layerSource = MapboxDefaultImagery.GetParameters(imageLayerProperties.sourceType);
			}
			_imageFactory = ScriptableObject.CreateInstance<MapImageFactory>();
			_imageFactory._mapIdType = imageLayerProperties.sourceType;
			_imageFactory._customStyle = imageLayerProperties.sourceOptions.layerSource;
			_imageFactory._useCompression = imageLayerProperties.rasterOptions.useCompression;
			_imageFactory._useMipMap = imageLayerProperties.rasterOptions.useMipMap;
			_imageFactory._useRetina = imageLayerProperties.rasterOptions.useRetina;
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
		public MapImageFactory ImageFactory
		{
			get
			{
				return _imageFactory;
			}
		}
		private MapImageFactory _imageFactory;
	}

	[Serializable]
	public class VectorLayer : IVectorDataLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Vector;
			}
		}

		public bool IsLayerActive
		{
			get;
			set;
		}

		public string LayerSource
		{
			get;
			set;
		}

		public LayerProperties LayerProperty
		{
			get;
			set;
		}

		public VectorPrimitiveType PrimitiveType
		{
			get;
			set;
		}

		public void Initialize(LayerProperties properties)
		{
			var vectorLayerProperties = (VectorLayerProperties)properties;
			_vectorTileFactory = ScriptableObject.CreateInstance<VectorTileFactoryNew>();
			_vectorTileFactory.SetOptions(vectorLayerProperties);
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}

		public VectorTileFactoryNew VectorFactory
		{
			get
			{
				return _vectorTileFactory;
			}
		}
		private VectorTileFactoryNew _vectorTileFactory;
	}

	[Serializable]
	public class UnifiedMapOptions
	{
		public MapOptions mapOptions = new MapOptions();
		public ImageryLayerProperties imageryLayerProperties = new ImageryLayerProperties();
		public ElevationLayerProperties elevationLayerProperties = new ElevationLayerProperties();
		public VectorLayerProperties vectorLayerProperties = new VectorLayerProperties();
	}

	public class MapAPI : MonoBehaviour
	{
		protected UnifiedMap _map;
		protected AbstractTileProvider _tileProvider;
		protected AbstractMapVisualizer _mapVisualizer;
		//protected AbstractTileFactory _elevationLayer;

		[SerializeField]
		UnifiedMapOptions _unifiedMapOptions = new UnifiedMapOptions();

		public event Action OnInitialized = delegate { };

		public UnifiedMap Map
		{
			get
			{
				return _map;
			}
		}

		void SendInitialized()
		{
			Debug.Log("MapManager Init");
			OnInitialized();
		}

		void SetUpFlat2DMap()
		{
			switch (_unifiedMapOptions.mapOptions.placementOptions.placementType)
			{
				case MapPlacementType.AtTileCenter:
					_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
				case MapPlacementType.AtLocationCenter:
					_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtLocationCenterStrategy();
					break;
				default:
					_unifiedMapOptions.mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
			}

			switch (_unifiedMapOptions.mapOptions.scalingOptions.scalingType)
			{
				case MapScalingType.WorldScale:
					_unifiedMapOptions.mapOptions.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
					break;
				case MapScalingType.Custom:
					_unifiedMapOptions.mapOptions.scalingOptions.scalingStrategy = new MapScalingAtUnityScaleStrategy();
					break;
				default:
					break;
			}


			_map.OnInitialized += SendInitialized;

			ITileProviderOptions tileProviderOptions = _unifiedMapOptions.mapOptions.placementOptions.extentOptions.GetTileProviderOptions();
			// Setup tileprovider based on type. 
			switch (_unifiedMapOptions.mapOptions.placementOptions.extentOptions.extentType)
			{
				case MapExtentType.CameraBounds:
					_tileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
					break;
				case MapExtentType.RangeAroundCenter:
					_tileProvider = gameObject.AddComponent<RangeTileProvider>();
					break;
				case MapExtentType.RangeAroundTransform:
					_tileProvider = gameObject.AddComponent<RangeAroundTransformTileProvider>();
					break;
				default:
					break;
			}

			_tileProvider.SetOptions(tileProviderOptions);



			var mapImageryLayers = new ImageryLayer();
			mapImageryLayers.Initialize(_unifiedMapOptions.imageryLayerProperties);


			var mapElevationLayer = new TerrainLayer();
			mapElevationLayer.Initialize(_unifiedMapOptions.elevationLayerProperties);
			//var terrainFactory = ScriptableObject.CreateInstance<TerrainWithSideWallsFactory>();
			//terrainFactory._mapId = "mapbox.terrain-rgb";


			var mapVectorLayer = new VectorLayer();
			mapVectorLayer.Initialize(_unifiedMapOptions.vectorLayerProperties);
			_mapVisualizer.Factories = new List<AbstractTileFactory>
			{
				mapElevationLayer.ElevationFactory,
				mapImageryLayers.ImageFactory,
				mapVectorLayer.VectorFactory
			};

			_map.TileProvider = _tileProvider;
			_map.MapVisualizer = _mapVisualizer;

			_map.InitializeMap(_unifiedMapOptions.mapOptions);

			Debug.Log("Setup 2DMap done. ");
		}


		// Use this for initialization
		void Start()
		{
			_map = gameObject.AddComponent<UnifiedMap>();
			// Setup a visualizer to get a "Starter" map.
			_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();

			switch (_unifiedMapOptions.mapOptions.placementOptions.visualizationType)
			{
				case MapVisualizationType.Flat2D:
					SetUpFlat2DMap();
					break;
				case MapVisualizationType.Globe3D:
					break;
				default:
					break;
			}

			_unifiedMapOptions.vectorLayerProperties.vectorSubLayers = new List<VectorSubLayerProperties>();
			_unifiedMapOptions.vectorLayerProperties.vectorSubLayers.Add(new VectorSubLayerProperties()
			{
				coreOptions = new CoreVectorLayerProperties()
				{
					layerName = "Building"
				}
			});
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
