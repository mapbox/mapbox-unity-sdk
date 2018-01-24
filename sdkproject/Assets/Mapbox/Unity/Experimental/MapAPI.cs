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
		Polygon,
		Polyline,
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

	[Serializable]
	public class MapPlacementOptions
	{
		public MapVisualizationType visualizationType;
		public MapPlacementType placementType;
		public MapStreamingType streamingType;
		public MapExtentType extentType;

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

	public abstract class LayerProperties
	{
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
		public Style CustomStyle = new Style();
		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();
	}

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

	public class TerrainLayer : ITerrainLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Elevation;
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

		public void Initialize(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
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
			if (imageLayerProperties.sourceType != ImagerySourceType.Custom || imageLayerProperties.sourceType != ImagerySourceType.None)
			{
				imageLayerProperties.CustomStyle = MapboxDefaultImagery.GetParameters(imageLayerProperties.sourceType);
			}
			_imageFactory = ScriptableObject.CreateInstance<MapImageFactory>();
			_imageFactory._mapIdType = imageLayerProperties.sourceType;
			_imageFactory._customStyle = imageLayerProperties.CustomStyle;
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
			throw new System.NotImplementedException();
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
	}




	public class MapAPI : MonoBehaviour
	{
		protected UnifiedMap _map;
		protected AbstractTileProvider _tileProvider;

		[SerializeField]
		MapOptions _mapOptions = new MapOptions();

		[SerializeField]
		ImageryLayerProperties _imageryLayerProperties = new ImageryLayerProperties();

		void SetUpFlat2DMap()
		{
			switch (_mapOptions.placementOptions.placementType)
			{
				case MapPlacementType.AtTileCenter:
					_mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
				case MapPlacementType.AtLocationCenter:
					_mapOptions.placementOptions.placementStrategy = new MapPlacementAtLocationCenterStrategy();
					break;
				default:
					_mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
			}

			switch (_mapOptions.scalingOptions.scalingType)
			{
				case MapScalingType.WorldScale:
					_mapOptions.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
					break;
				case MapScalingType.Custom:
					_mapOptions.scalingOptions.scalingStrategy = new MapScalingAtUnityScaleStrategy();
					break;
				default:
					break;
			}

			_map = gameObject.AddComponent<UnifiedMap>();

			// Setup tileprovider based on type. 
			switch (_mapOptions.placementOptions.extentType)
			{
				case MapExtentType.CameraBounds:
					if (_mapOptions.placementOptions.streamingType == MapStreamingType.Zoomable)
					{
						_tileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
					}
					else
					{
						_tileProvider = gameObject.AddComponent<CameraBoundsTileProvider>();
					}
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


			// Setup a visualizer to get a "Starter" map.
			var _mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();

			var mapImageryLayers = new ImageryLayer();
			mapImageryLayers.Initialize(_imageryLayerProperties);
			var terrainFactory = ScriptableObject.CreateInstance<TerrainWithSideWallsFactory>();
			terrainFactory._mapId = "mapbox.terrain-rgb";
			_mapVisualizer.Factories = new List<AbstractTileFactory>
			{
				terrainFactory,
				mapImageryLayers.ImageFactory

			};

			_map.TileProvider = _tileProvider;
			_map.MapVisualizer = _mapVisualizer;

			_map.InitializeMap(_mapOptions);

		}


		// Use this for initialization
		void Start()
		{
			switch (_mapOptions.placementOptions.visualizationType)
			{
				case MapVisualizationType.Flat2D:
					SetUpFlat2DMap();
					break;
				case MapVisualizationType.Globe3D:
					break;
				default:
					break;
			}
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
