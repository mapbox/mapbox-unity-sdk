namespace Mapbox.Unity.Map
{
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;
	using UnityEngine;

	public class MapAtWorldScale : AbstractMap
	{
		[SerializeField]
		bool _useRelativeScale;

		public override void Initialize(Vector2d latLon, int zoom)
		{
			_worldHeightFixed = false;
			_centerLatitudeLongitude = latLon;
			_zoom = zoom;
			_initialZoom = zoom;

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_centerLatitudeLongitude, AbsoluteZoom));
			_centerMercator = referenceTileRect.Center;

			_worldRelativeScale = _useRelativeScale ? Mathf.Cos(Mathf.Deg2Rad * (float)_centerLatitudeLongitude.x) : 1f;

			_mapVisualizer.Initialize(this, _fileSource);
			_tileProvider.Initialize(this);

			SendInitialized();
		}
	}
}