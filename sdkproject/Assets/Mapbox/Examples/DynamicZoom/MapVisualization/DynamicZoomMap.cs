namespace Mapbox.Unity.Examples.DynamicZoom
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;

	public class DynamicZoomMap : AbstractMap
	{
		[SerializeField]
		[Range(0, 22)]
		public int MinZoom;

		[SerializeField]
		[Range(0, 22)]
		public int MaxZoom;

		// TODO: remove?
		public string _webMerc;

		void Start()
		{
			Initialize();
		}

		protected override void Initialize()
		{
			var latLonSplit = _latitudeLongitudeString.Split(',');
			_centerLatitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));

			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_centerLatitudeLongitude, _zoom));
			_centerMercator = Conversions.LatLonToMeters(_centerLatitudeLongitude);
			Debug.LogFormat("center, latLng:{0} webMerc:{1}", _centerLatitudeLongitude, _centerMercator);

			_worldRelativeScale = (float)(1f / referenceTileRect.Size.x);
			_mapVisualizer.Initialize(this, _fileSouce);
			_tileProvider.Initialize(this);

			SendInitialized();
		}

		// TODO: remove!
		private void Update()
		{
			_webMerc = CenterMercator.ToString();
			SetCenterLatitudeLongitude(Conversions.MetersToLatLon(_centerMercator));
		}
	}
}