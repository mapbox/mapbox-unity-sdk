namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;

	public class DynamicZoomMap : AbstractMap
	{
		// TODO: these could also live in AbstractMap, instead.
		[SerializeField]
		[Range(0, 22)]
		public int MinZoom;

		[SerializeField]
		[Range(0, 22)]
		public int MaxZoom;

		// TODO: remove?
		//public string _webMerc;

		public override void Initialize(Vector2d latLon, int zoom)
		{
			_centerLatitudeLongitude = latLon;
			_zoom = zoom;

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_centerLatitudeLongitude, _zoom));

			// FIXME: The only difference from BasicMap? Can we solve this another way?
			_centerMercator = Conversions.LatLonToMeters(_centerLatitudeLongitude);
			_worldRelativeScale = (float)(1f / referenceTileRect.Size.x);
			Debug.LogFormat("center, latLng:{0} webMerc:{1}", _centerLatitudeLongitude, _centerMercator);

			_mapVisualizer.Initialize(this, _fileSouce);
			_tileProvider.Initialize(this);

			SendInitialized();
		}

		// TODO: remove!
		//private void Update()
		//{
			//_webMerc = CenterMercator.ToString();
			//SetCenterLatitudeLongitude(Conversions.MetersToLatLon(_centerMercator));
		//}
	}
}