namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;
	using Utils;

	public class MapAtSpecificLocation : AbstractMap
	{
		public override void Initialize(Vector2d latLon, int zoom)
		{
			_worldHeightFixed = false;
			_centerLatitudeLongitude = latLon;
			_zoom = zoom;
			_initialZoom = zoom;

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_centerLatitudeLongitude, AbsoluteZoom));
			_centerMercator = referenceTileRect.Center;

			_worldRelativeScale = (float)(_unityTileSize / referenceTileRect.Size.x);

			// The magic line.
			// Because this offsets the map root, you must manually compensate for this offset with any future
			// conversion operations (lat/lon <--> unity world space)!!!
			_root.localPosition = -Conversions.GeoToWorldPosition(_centerLatitudeLongitude.x, _centerLatitudeLongitude.y, _centerMercator, _worldRelativeScale).ToVector3xz();

			_mapVisualizer.Initialize(this, _fileSource);
			_tileProvider.Initialize(this);

			SendInitialized();
		}
	}
}