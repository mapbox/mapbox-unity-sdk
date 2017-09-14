namespace Mapbox.Unity.Examples.DynamicZoom
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using System;
	using Mapbox.Map;

	public class DynamicZoomMap : AbstractMap
	{

		[SerializeField]
		[Range(0, 22)]
		public int MinZoom;

		[SerializeField]
		[Range(0, 22)]
		public int MaxZoom;

		[SerializeField]
		DynamicZoomCameraMovement _cameraMovement;


		public string _webMerc;

		public Vector2d CenterWebMerc;

		public int UnityTileSize { get { return (int)_unityTileSize; } }


		private Camera _referenceCamera;



		protected override void Start()
		{
			Root.localScale = Vector3.one;

			base.Start();

			Root.localScale = Vector3.one;



			CenterWebMerc = Conversions.GeoToWorldPosition(CenterLatitudeLongitude, new Vector2d(0, 0));
			CenterWebMerc = Conversions.LatLonToMeters(CenterLatitudeLongitude);
			Debug.LogFormat("center, latLng:{0} webMerc:{1}", CenterLatitudeLongitude, CenterWebMerc);

			if (null == _cameraMovement) { Debug.LogErrorFormat("{0}: camera movement not set", this.GetType().Name); }
			_referenceCamera = _cameraMovement._referenceCamera;
			if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			DynamicZoomTileProvider tileProvider = _tileProvider as DynamicZoomTileProvider;
			if (null == tileProvider) { Debug.LogErrorFormat("assigned tiled provider is not of type: {0}",typeof(DynamicZoomTileProvider).Name); }

			_cameraMovement.Map = this;
			tileProvider._referenceCamera = _referenceCamera;
			tileProvider.Initialize(this);

		}


		private void Update()
		{
			_webMerc = CenterWebMerc.ToString();
			CenterLatitudeLongitude = Conversions.MetersToLatLon(CenterWebMerc);
		}







	}
}