namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Map;

	/// <summary>
	/// Abstract Map (Basic Map etc)
	/// This is one of the few monobehaviours we use in the system and used mainly to tie scene and map visualization object/system 
	/// together.It's a replacement for the application (or map controller class in a project) in our demos.
	/// Ideally devs should have their own map initializations and tile call logic in their app and make calls to 
	/// map visualization object from their own controllers directly. It can also be used as an interface for 
	/// small projects or tests.
	/// </summary>
	/// 
	public class BasicMap : AbstractMap
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
			_mapVisualizer.Initialize(this, _fileSource);
			_tileProvider.Initialize(this);

			SendInitialized();
		}
	}
}