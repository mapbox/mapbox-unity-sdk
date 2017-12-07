namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;

	/// <summary>
	/// Abstract Map (Basic Map etc)
	/// This is one of the few monobehaviours we use in the system and used mainly to tie scene and map visualization object/system 
	/// together.It’s a replacement for the application (or map controller class in a project) in our demos.
	/// Ideally devs should have their own map initializations and tile call logic in their app and make calls to 
	/// map visualization object from their own controllers directly. It can also be used as an interface for 
	/// small projects or tests.
	/// </summary>

	public abstract class AbstractMap : MonoBehaviour, IMap
	{
		[SerializeField]
		[Range(0, 22)]
		protected float _zoom;
		public float Zoom
		{
			get
			{
				return _zoom;
			}
		}
		public void SetZoom(float zoom)
		{
			_zoom = zoom;
		}
		[SerializeField]
		bool _initializeOnStart = true;

		[Geocode]
		[SerializeField]
		protected string _latitudeLongitudeString;

		public int AbsoluteZoom
		{
			get
			{
				return (int)Math.Floor(Zoom);
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
		[NodeEditorElement("MapVisualizer")]
		public AbstractMapVisualizer _mapVisualizer;
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

		public void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}

		public void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			_latitudeLongitudeString = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
		}
		public event Action OnInitialized = delegate { };

		protected virtual void Awake()
		{
			_worldHeightFixed = false;
			_fileSource = MapboxAccess.Instance;
			_tileProvider.OnTileAdded += TileProvider_OnTileAdded;
			_tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
			_tileProvider.OnTileRepositioned += TileProvider_OnTileRepositioned;
			if (!_root)
			{
				_root = transform;
			}
		}

		protected virtual void Start()
		{
			if (_initializeOnStart)
			{
				Initialize(Conversions.StringToLatLon(_latitudeLongitudeString), AbsoluteZoom);
			}
			_initialZoom = AbsoluteZoom;
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

		protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId)
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
			return (_root.InverseTransformPoint(realworldPoint)).GetGeoPosition(CenterMercator, WorldRelativeScale);
		}

		public virtual Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
		{
			return _root.TransformPoint(Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale).ToVector3xz());
		}

		public abstract void Initialize(Vector2d latLon, int zoom);

		public void Reset()
		{
			Initialize(Conversions.StringToLatLon(_latitudeLongitudeString), (int)_zoom);
		}
	}
}
