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
			map.SetCenterMercator(Conversions.LatLonToMeters(map.CenterLatitudeLongitude));
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
			_currentOptions.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
		}
		public event Action OnInitialized = delegate { };

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

		public virtual Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			var worldPos = Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
			return Root.TransformPoint(worldPos);
		}

		public virtual Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			return (Root.InverseTransformPoint(realworldPoint)).GetGeoPosition(CenterMercator, WorldRelativeScale * scaleFactor);
		}
		public virtual void Initialize(Vector2d latLon, int zoom)
		{
			//_worldHeightFixed = false;
			//_fileSource = MapboxAccess.Instance;

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
			Initialize(Conversions.StringToLatLon(_currentOptions.locationOptions.latitudeLongitude), (int)_currentOptions.locationOptions.zoom);
		}
	}
}

