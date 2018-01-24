namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;

	public interface IUnifiedMap
	{
		void InitializeMap(MapOptions options);
		void UpdateMap(MapLocationOptions options);
		void ResetMap();
	}

	public interface IMapScalingStrategy
	{
		void SetUpScaling(UnifiedMap map);
	}

	public class MapScalingAtWorldScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(UnifiedMap map)
		{
			map.SetWorldRelativeScale(Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x));
		}
	}

	public class MapScalingAtUnityScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(UnifiedMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetWorldRelativeScale((float)(map.CurrentOptions.scalingOptions.unityToMercatorConversionFactor / referenceTileRect.Size.x));
		}
	}

	public interface IMapPlacementStrategy
	{
		void SetUpPlacement(UnifiedMap map);
	}

	public class MapPlacementAtTileCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(UnifiedMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetCenterMercator(referenceTileRect.Center);
		}
	}

	public class MapPlacementAtLocationCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(UnifiedMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetCenterMercator(referenceTileRect.Center);

			map.transform.localPosition = -Conversions.GeoToWorldPosition(map.CenterLatitudeLongitude.x, map.CenterLatitudeLongitude.y, map.CenterMercator, map.WorldRelativeScale).ToVector3xz();
		}
	}

	public class UnifiedMap : MonoBehaviour, IMap
	{
		private MapOptions _currentOptions;
		public MapOptions CurrentOptions
		{
			get
			{
				return _currentOptions;
			}
			set
			{
				_currentOptions = value;
			}
		}
		public void InitializeMap(MapOptions options)
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

		[SerializeField]
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

		[SerializeField]
		[NodeEditorElement("MapVisualizer")]
		public AbstractMapVisualizer _mapVisualizer;
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

		[SerializeField]
		protected float _unityTileSize = 100;
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
		[SerializeField]
		protected bool _snapMapHeightToZero = true;

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
				throw new NotImplementedException();
			}
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
			_currentOptions.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
		}
		public event Action OnInitialized = delegate { };

		//protected virtual void Awake()
		//{
		//	Debug.Log("Awake Called");
		//}

		//protected virtual void Start()
		//{
		//	InitializeMap(_currentOptions);
		//}

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

		protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			//if (_snapMapHeightToZero && !_worldHeightFixed)
			//{
			//	//TODO : Fix this 
			//	//_worldHeightFixed = true;
			//	//var tile = _mapVisualizer.LoadTile(tileId);
			//	//if (tile.HeightDataState == MeshGeneration.Enums.TilePropertyState.Loaded)
			//	//{
			//	//	var h = tile.QueryHeightData(.5f, .5f);
			//	//	Root.transform.position = new Vector3(
			//	//	 Root.transform.position.x,
			//	//	 -h,
			//	//	 Root.transform.position.z);
			//	//}
			//	//else
			//	//{
			//	//	tile.OnHeightDataChanged += (s) =>
			//	//	{
			//	//		var h = s.QueryHeightData(.5f, .5f);
			//	//		Root.transform.position = new Vector3(
			//	//			 Root.transform.position.x,
			//	//			 -h,
			//	//			 Root.transform.position.z);
			//	//	};
			//	//}
			//}
			//else
			//{
			_mapVisualizer.LoadTile(tileId);
			//}
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

		public virtual Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			return (transform.InverseTransformPoint(realworldPoint)).GetGeoPosition(CenterMercator, WorldRelativeScale);
		}

		public virtual Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
		{
			return transform.TransformPoint(Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale).ToVector3xz());
		}

		public virtual void Initialize(Vector2d latLon, int zoom)
		{
			//_worldHeightFixed = false;
			//_fileSource = MapboxAccess.Instance;

		}
		public virtual void UpdateMap(MapLocationOptions options)
		{

		}
		public void ResetMap()
		{
			Initialize(Conversions.StringToLatLon(_currentOptions.locationOptions.latitudeLongitude), (int)_currentOptions.locationOptions.zoom);
		}

		public void SetZoom(float zoom)
		{
			throw new NotImplementedException();
		}
	}
}

