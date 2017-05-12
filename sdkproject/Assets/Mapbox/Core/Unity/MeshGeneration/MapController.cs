using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Map;
using Mapbox.Map;
namespace Mapbox.Unity.MeshGeneration
{
	using UnityEngine;
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Unity.MeshGeneration.Data;

	//public class WorldParameters
	//{
	//    public RectD ReferenceTileRect;
	//    public float WorldScaleFactor = 1;
	//}

	public class MapController : MonoBehaviour
	{
		[Geocode]
		[SerializeField]
		string _latitudeLongitudeString;

		[SerializeField]
		int _zoom;

		[SerializeField]
		RangeTileProvider _tileProvider;

		[SerializeField]
		Factory[] _factories;

		[SerializeField]
		float _unityTileSize = 100;

		MapboxAccess _fileSouce;

		Vector2d _latitudeLongitude;

		//private bool _snapYToZero = true;

		//UnwrappedTileId _refTile;

		RectD _referenceTileRect;
		public RectD ReferenceTileRect
		{
			get
			{
				return _referenceTileRect;
			}
		}

		float _worldScaleFactor;
		public float WorldScaleFactor
		{
			get
			{
				return _worldScaleFactor;
			}
		}

		void Awake()
		{
			_tileProvider.OnTileAdded += TileProvider_OnTileAdded;
			_fileSouce = MapboxAccess.Instance;
		}

		void OnDestroy()
		{
			if (_tileProvider != null)
			{
				_tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
			}
		}

		public void SetLatitudeLongitude(string latitudeLongitudeString)
		{
			_latitudeLongitudeString = latitudeLongitudeString;
		}

		void Start()
		{
			var latLonSplit = _latitudeLongitudeString.Split(',');
			_latitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));

			var refTile = TileCover.CoordinateToTileId(_latitudeLongitude, _zoom);
			_referenceTileRect = Conversions.TileBounds(refTile, _zoom);
			_worldScaleFactor = (float)(_unityTileSize / _referenceTileRect.Size.x);
			transform.localScale = Vector3.one * _worldScaleFactor;

			foreach (var factory in _factories)
			{
				factory.Initialize(_fileSouce);
			}
			_tileProvider.Initialize(refTile, _zoom);
		}

		void TileProvider_OnTileAdded(object sender, Map.TileStateChangedEventArgs e)
		{
			var tile = new GameObject(e.TileId.ToString()).AddComponent<UnityTile>();
			tile.Zoom = _zoom;
			tile.RelativeScale = Conversions.GetTileScaleInMeters(0, _zoom) / Conversions.GetTileScaleInMeters((float)_latitudeLongitude.x, _zoom);
			tile.TileCoordinate = new Vector2(e.TileId.X, e.TileId.Y);
			tile.Rect = Conversions.TileBounds(tile.TileCoordinate, _zoom);
			tile.transform.localPosition = new Vector3((float)(tile.Rect.Center.x - ReferenceTileRect.Center.x), 0, (float)(tile.Rect.Center.y - ReferenceTileRect.Center.y));
			tile.transform.SetParent(transform, false);

			foreach (var factory in _factories)
			{
				factory.Register(tile);
			}
		}
	}
}