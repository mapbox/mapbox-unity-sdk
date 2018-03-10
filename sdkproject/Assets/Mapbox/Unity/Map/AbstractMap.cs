namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;

	public interface IUnifiedMap
	{
		//void InitializeMap(MapOptions options);
		void UpdateMap(MapLocationOptions options);
		void ResetMap();
	}

	public interface IMapScalingStrategy
	{
		void SetUpScaling(AbstractMap map);
	}

	public class MapScalingAtWorldScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			map.SetWorldRelativeScale(Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x));
		}
	}

	public class MapScalingAtUnityScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetWorldRelativeScale((float)(map.CurrentOptions.scalingOptions.unityToMercatorConversionFactor / referenceTileRect.Size.x));
		}
	}

	public interface IMapPlacementStrategy
	{
		void SetUpPlacement(AbstractMap map);
	}

	public class MapPlacementAtTileCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetCenterMercator(referenceTileRect.Center);
		}
	}

	public class MapPlacementAtLocationCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			map.SetCenterMercator(Conversions.LatLonToMeters(map.CenterLatitudeLongitude));
		}
	}

	public class AbstractMap : MonoBehaviour, IMap
	{
		private bool _initializeOnStart = true;

		//[SerializeField]
		//UnifiedMapOptions _unifiedMapOptions = new UnifiedMapOptions();
		//public UnifiedMapOptions MapOptions
		//{
		//	get
		//	{
		//		return _unifiedMapOptions;
		//	}

		//}
		[SerializeField]
		private MapOptions mapOptions;
		public MapOptions CurrentOptions
		{
			get
			{
				return mapOptions;
			}
			set
			{
				mapOptions = value;
			}
		}

		[SerializeField]
		ImageryLayer _imagery = new ImageryLayer();
		[NodeEditorElement("Layers")]
		public ImageryLayer ImageLayer
		{
			get
			{
				return _imagery;
			}
		}

		[SerializeField]
		TerrainLayer _terrain = new TerrainLayer();
		[NodeEditorElement("Layers")]
		public TerrainLayer Terrain
		{
			get
			{
				return _terrain;
			}
		}

		[SerializeField]
		VectorLayer _vectorData = new VectorLayer();
		[NodeEditorElement("Layers")]
		public VectorLayer VectorData
		{
			get
			{
				return _vectorData;
			}
		}

		protected AbstractTileProvider _tileProvider;
		public AbstractTileProvider TileProvider
		{
			get
			{
				return _tileProvider;
			}
			set
			{
				if (_tileProvider != null)
				{
					_tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
					_tileProvider.OnTileRemoved -= TileProvider_OnTileRemoved;
					_tileProvider.OnTileRepositioned -= TileProvider_OnTileRepositioned;
				}
				_tileProvider = value;
				_tileProvider.OnTileAdded += TileProvider_OnTileAdded;
				_tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
				_tileProvider.OnTileRepositioned += TileProvider_OnTileRepositioned;

			}
		}

		//[NodeEditorElement("MapVisualizer")]
		protected AbstractMapVisualizer _mapVisualizer;
		public AbstractMapVisualizer MapVisualizer
		{
			get
			{
				return _mapVisualizer;
			}
			set
			{
				_mapVisualizer = value;
			}
		}

		protected float _unityTileSize = 1;
		public float UnityTileSize
		{
			get
			{
				return _unityTileSize;
			}
		}

		public int AbsoluteZoom
		{
			get
			{
				return (int)Math.Floor(CurrentOptions.locationOptions.zoom);
			}
		}

		protected int _initialZoom;
		public int InitialZoom
		{
			get
			{
				return _initialZoom;
			}
		}

		protected bool _worldHeightFixed = false;

		protected MapboxAccess _fileSource;

		protected Vector2d _centerLatitudeLongitude;
		public Vector2d CenterLatitudeLongitude
		{
			get
			{
				return _centerLatitudeLongitude;
			}
		}

		protected Vector2d _centerMercator;
		public Vector2d CenterMercator
		{
			get
			{
				return _centerMercator;
			}
		}

		protected float _worldRelativeScale;
		public float WorldRelativeScale
		{
			get
			{
				return _worldRelativeScale;
			}
		}

		public float Zoom
		{
			get
			{
				return CurrentOptions.locationOptions.zoom;
			}
		}

		public void SetZoom(float zoom)
		{
			CurrentOptions.locationOptions.zoom = zoom;
		}

		public Transform Root
		{
			get
			{
				return transform;
			}
		}

		public void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}

		public void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			mapOptions.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
		}
		public event Action OnInitialized = delegate { };

		void Awake()
		{
			// Setup a visualizer to get a "Starter" map.
			_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();
			//TODO : Check if we need _root option.
			//if (!_root)
			//{
			//	_root = transform;
			//}
		}

		// Use this for initialization
		void Start()
		{
			StartCoroutine("SetupAccess");
			if (_initializeOnStart)
			{
				switch (mapOptions.placementOptions.visualizationType)
				{
					case MapVisualizationType.Flat2D:
						SetUpMap();
						break;
					case MapVisualizationType.Globe3D:
						break;
					default:
						break;
				}
			}
		}

		protected IEnumerator SetupAccess()
		{
			_fileSource = MapboxAccess.Instance;

			yield return new WaitUntil(() => MapboxAccess.Configured);
		}

		protected virtual void SetUpMap()
		{
			switch (mapOptions.placementOptions.placementType)
			{
				case MapPlacementType.AtTileCenter:
					mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
				case MapPlacementType.AtLocationCenter:
					mapOptions.placementOptions.placementStrategy = new MapPlacementAtLocationCenterStrategy();
					break;
				default:
					mapOptions.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
			}

			switch (mapOptions.scalingOptions.scalingType)
			{
				case MapScalingType.WorldScale:
					mapOptions.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
					break;
				case MapScalingType.Custom:
					mapOptions.scalingOptions.scalingStrategy = new MapScalingAtUnityScaleStrategy();
					break;
				default:
					break;
			}

			ITileProviderOptions tileProviderOptions = mapOptions.extentOptions.GetTileProviderOptions();
			// Setup tileprovider based on type. 
			switch (mapOptions.extentOptions.extentType)
			{
				case MapExtentType.CameraBounds:
					TileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
					break;
				case MapExtentType.RangeAroundCenter:
					TileProvider = gameObject.AddComponent<RangeTileProvider>();
					break;
				case MapExtentType.RangeAroundTransform:
					TileProvider = gameObject.AddComponent<RangeAroundTransformTileProvider>();
					break;
				default:
					break;
			}

			TileProvider.SetOptions(tileProviderOptions);

			if (_imagery == null)
			{
				_imagery = new ImageryLayer();
			}
			_imagery.Initialize(new ImageryLayerProperties());

			if (_terrain == null)
			{
				_terrain = new TerrainLayer();
			}
			_terrain.Initialize();

			if (_vectorData == null)
			{
				_vectorData = new VectorLayer();
			}
			_vectorData.Initialize();

			_mapVisualizer.Factories = new List<AbstractTileFactory>
			{
				_terrain.ElevationFactory,
				_imagery.ImageFactory,
				_vectorData.VectorFactory
			};

			InitializeMap(mapOptions);

			Debug.Log("Setup 2DMap done. ");
		}

		// TODO: implement IDisposable, instead?
		protected virtual void OnDestroy()
		{
			if (_tileProvider != null)
			{
				_tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
				_tileProvider.OnTileRemoved -= TileProvider_OnTileRemoved;
				_tileProvider.OnTileRepositioned -= TileProvider_OnTileRepositioned;
			}

			_mapVisualizer.Destroy();
		}

		protected virtual void InitializeMap(MapOptions options)
		{
			CurrentOptions = options;
			_worldHeightFixed = false;
			_fileSource = MapboxAccess.Instance;
			_centerLatitudeLongitude = Conversions.StringToLatLon(options.locationOptions.latitudeLongitude);
			_initialZoom = (int)options.locationOptions.zoom;

			options.scalingOptions.scalingStrategy.SetUpScaling(this);

			options.placementOptions.placementStrategy.SetUpPlacement(this);

			_mapVisualizer.Initialize(this, _fileSource);
			_tileProvider.Initialize(this);

			SendInitialized();
		}

		public virtual void Initialize(Vector2d latLon, int zoom)
		{
			_initializeOnStart = false;
			if (mapOptions == null)
			{
				mapOptions = new MapOptions();
			}
			mapOptions.locationOptions.latitudeLongitude = String.Format("{0},{1}", latLon.x, latLon.y);
			mapOptions.locationOptions.zoom = zoom;

			switch (mapOptions.placementOptions.visualizationType)
			{
				case MapVisualizationType.Flat2D:
					SetUpMap();
					break;
				case MapVisualizationType.Globe3D:
					break;
				default:
					break;
			}
		}

		public virtual void UpdateMap(MapLocationOptions options)
		{
			float differenceInZoom = 0.0f;
			if (Math.Abs(Zoom - options.zoom) > Constants.EpsilonFloatingPoint)
			{
				SetZoom(options.zoom);
				differenceInZoom = Zoom - InitialZoom;
			}
			//Update center latitude longitude
			var centerLatitudeLongitude = Conversions.StringToLatLon(options.latitudeLongitude);
			double xDelta = centerLatitudeLongitude.x;
			double zDelta = centerLatitudeLongitude.y;

			xDelta = xDelta > 0 ? Mathd.Min(xDelta, Mapbox.Utils.Constants.LatitudeMax) : Mathd.Max(xDelta, -Mapbox.Utils.Constants.LatitudeMax);
			zDelta = zDelta > 0 ? Mathd.Min(zDelta, Mapbox.Utils.Constants.LongitudeMax) : Mathd.Max(zDelta, -Mapbox.Utils.Constants.LongitudeMax);

			//Set Center in Latitude Longitude and Mercator. 
			SetCenterLatitudeLongitude(new Vector2d(xDelta, zDelta));
			CurrentOptions.scalingOptions.scalingStrategy.SetUpScaling(this);

			CurrentOptions.placementOptions.placementStrategy.SetUpPlacement(this);

			//Scale the map accordingly.
			if (Math.Abs(differenceInZoom) > Constants.EpsilonFloatingPoint)
			{
				Root.localScale = Vector3.one * Mathf.Pow(2, differenceInZoom);
			}
		}

		public void ResetMap()
		{
			Initialize(Conversions.StringToLatLon(mapOptions.locationOptions.latitudeLongitude), (int)mapOptions.locationOptions.zoom);
		}

		protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			if (CurrentOptions.placementOptions.snapMapToZero)
			{
				_worldHeightFixed = true;
				var tile = _mapVisualizer.LoadTile(tileId);
				if (tile.HeightDataState == MeshGeneration.Enums.TilePropertyState.Loaded)
				{
					var h = tile.QueryHeightData(.5f, .5f);
					Root.transform.position = new Vector3(
					 Root.transform.position.x,
					 -h,
					 Root.transform.position.z);
				}
				else
				{
					tile.OnHeightDataChanged += (s) =>
					{
						var h = s.QueryHeightData(.5f, .5f);
						Root.transform.position = new Vector3(
							 Root.transform.position.x,
							 -h,
							 Root.transform.position.z);
					};
				}
			}
			else
			{
				_mapVisualizer.LoadTile(tileId);
			}
		}

		protected virtual void TileProvider_OnTileRemoved(UnwrappedTileId tileId)
		{
			_mapVisualizer.DisposeTile(tileId);
		}

		protected virtual void TileProvider_OnTileRepositioned(UnwrappedTileId tileId)
		{
			_mapVisualizer.RepositionTile(tileId);
		}

		protected void SendInitialized()
		{
			OnInitialized();
		}

		internal Vector3 GeoToWorldPositionXZ(Vector2d latitudeLongitude)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));
			var worldPos = Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
			return Root.TransformPoint(worldPos);
		}

		public virtual Vector3 GeoToWorldPosition(Vector2d latitudeLongitude, bool queryHeight = true)
		{
			var worldPos = GeoToWorldPositionXZ(latitudeLongitude);

			if (queryHeight)
			{
				//Query Height.
				worldPos.y = QueryHeightData(latitudeLongitude);
			}

			return worldPos;
		}

		public virtual Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			return (Root.InverseTransformPoint(realworldPoint)).GetGeoPosition(CenterMercator, WorldRelativeScale * scaleFactor);
		}

		public virtual float QueryHeightData(Vector2d latlong)
		{
			var _meters = Conversions.LatLonToMeters(latlong.x, latlong.y);
			var tile = MapVisualizer.ActiveTiles[Conversions.LatitudeLongitudeToTileId(latlong.x, latlong.y, (int)Zoom)];
			var _rect = tile.Rect;
			var _worldPos = GeoToWorldPositionXZ(new Vector2d(latlong.x, latlong.y));
			return tile.QueryHeightData((float)((_meters - _rect.Min).x / _rect.Size.x), (float)((_meters.y - _rect.Max.y) / _rect.Size.y));
		}


	}
}

