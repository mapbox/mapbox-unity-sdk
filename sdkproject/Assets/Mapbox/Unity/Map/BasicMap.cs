namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Map;

	public class BasicMap : AbstractMap
	{
		void Start()
		{
			Initialize();
		}
			
		protected override void Initialize()
		{
			var latLonSplit = _latitudeLongitudeString.Split(',');
			_centerLatitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_centerLatitudeLongitude, _zoom));
			_centerMercator = referenceTileRect.Center;

			_worldRelativeScale = (float)(_unityTileSize / referenceTileRect.Size.x);
			_mapVisualizer.Initialize(this, _fileSouce);
			_tileProvider.Initialize(this);

			SendInitialized();
		}
	}
}