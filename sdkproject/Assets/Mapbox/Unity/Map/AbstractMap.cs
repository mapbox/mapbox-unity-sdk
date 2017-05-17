namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;

	// TODO: need to be abstract?
	public class AbstractMap : MonoBehaviour, IMap
	{
		[Geocode]
		[SerializeField]
		string _latitudeLongitudeString;

		[SerializeField]
		int _zoom;
		public int Zoom
		{
			get
			{
				return _zoom;
			}
			set
			{
				_zoom = value;
			}
		}

		[SerializeField]
		Transform _root;
		public Transform Root
		{
			get
			{
				return _root;
			}
		}

		[SerializeField]
		AbstractTileProvider _tileProvider;

		[SerializeField]
		MapVisualization _mapVisualization;

		[SerializeField]
		float _unityTileSize = 100;

		MapboxAccess _fileSouce;

		Vector2d _mapCenterLatitudeLongitude;
		public Vector2d CenterLatitudeLongitude
		{
			get
			{
				return _mapCenterLatitudeLongitude;
			}
			set
			{
				_mapCenterLatitudeLongitude = value;
			}
		}

		Vector2d _mapCenterMercator;
		public Vector2d CenterMercator
		{
			get
			{
				return _mapCenterMercator;
			}
		}

		float _worldRelativeScale;
		public float WorldRelativeScale
		{
			get
			{
				return _worldRelativeScale;
			}
		}

		protected virtual void Awake()
		{
			_fileSouce = MapboxAccess.Instance;
			_tileProvider.OnTileAdded += TileProvider_OnTileAdded;
			_tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
			if (!_root)
			{
				_root = transform;
			}
		}

		protected virtual void OnDestroy()
		{
			if (_tileProvider != null)
			{
				_tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
				_tileProvider.OnTileAdded -= TileProvider_OnTileRemoved;
			}
		}

		protected virtual void Start()
		{
			var latLonSplit = _latitudeLongitudeString.Split(',');
			_mapCenterLatitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_mapCenterLatitudeLongitude, _zoom), _zoom);
			_mapCenterMercator = referenceTileRect.Center;

			_worldRelativeScale = (float)(_unityTileSize / referenceTileRect.Size.x);
			Root.localScale = Vector3.one * _worldRelativeScale;

			_mapVisualization.Initialize(this, _fileSouce);
			_tileProvider.Initialize(this);
		}

		void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			_mapVisualization.InitializeTile(tileId);
		}

		void TileProvider_OnTileRemoved(UnwrappedTileId tileId)
		{
			_mapVisualization.DisposeTile(tileId);
		}
	}
}