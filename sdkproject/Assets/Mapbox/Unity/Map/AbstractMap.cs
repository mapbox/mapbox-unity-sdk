namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;

	public abstract class AbstractMap : MonoBehaviour, IMap
	{
		[SerializeField]
		bool _initializeOnStart = true;

		[Geocode]
		[SerializeField]
		protected string _latitudeLongitudeString;

		[SerializeField]
		[Range(0, 22)]
		protected int _zoom;
		public int Zoom
		{
			get
			{
				return _zoom;
			}
		}

		[SerializeField]
		protected Transform _root;
		public Transform Root
		{
			get
			{
				return _root;
			}
		}

		[SerializeField]
		protected AbstractTileProvider _tileProvider;

		[SerializeField]
		protected AbstractMapVisualizer _mapVisualizer;
		public AbstractMapVisualizer MapVisualizer
		{
			get
			{
				return _mapVisualizer;
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

		[SerializeField]
		protected bool _snapMapHeightToZero = true;

		protected bool _worldHeightFixed = false;

		protected MapboxAccess _fileSouce;

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

		public void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}

		public void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			_latitudeLongitudeString = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetZoom(int zoom)
		{
			_zoom = zoom;
		}

		public event Action OnInitialized = delegate { };

		void Awake()
		{
			_worldHeightFixed = false;
			_fileSouce = MapboxAccess.Instance;
			_tileProvider.OnTileAdded += TileProvider_OnTileAdded;
			_tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
			if (!_root)
			{
				_root = transform;
			}
		}

		void Start()
		{
			if (_initializeOnStart)
			{
				Initialize(Conversions.StringToLatLon(_latitudeLongitudeString), _zoom);
			}
		}

		// TODO: implement IDisposable, instead?
		void OnDestroy()
		{
			if (_tileProvider != null)
			{
				_tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
				_tileProvider.OnTileRemoved -= TileProvider_OnTileRemoved;
			}

			_mapVisualizer.Destroy();
		}

		void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			if (_snapMapHeightToZero && !_worldHeightFixed)
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

		void TileProvider_OnTileRemoved(UnwrappedTileId tileId)
		{
			_mapVisualizer.DisposeTile(tileId);
		}

		protected void SendInitialized()
		{
			OnInitialized();
		}

		public abstract void Initialize(Vector2d latLon, int zoom);
	}
}