namespace Mapbox.Examples.Scripts
{
	using Mapbox.Unity.Location;
	using Mapbox.Utils;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using UnityEngine;
	using UnityEngine.UI;

	public class LogLocationProviderData : MonoBehaviour
	{

		[SerializeField]
		private Text _logText;


		private CultureInfo _invariantCulture = CultureInfo.InvariantCulture;


		// Use this for initialization
		void Start()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}


		void OnDestroy()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
		}


		void LocationProvider_OnLocationUpdated(Location location)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(string.Format("IsLocationServiceEnabled: {0}", location.IsLocationServiceEnabled));
			sb.AppendLine(string.Format("IsLocationServiceInitializing: {0}", location.IsLocationServiceInitializing));
			sb.AppendLine(string.Format("IsLocationUpdated: {0}", location.IsLocationUpdated));
			sb.AppendLine(string.Format("IsLocationUpdated: {0}", location.IsHeadingUpdated));
			string locationProviderClass = LocationProviderFactory.Instance.DefaultLocationProvider.GetType().Name;
			sb.AppendLine(string.Format("location provider: {0} ({1})", location.Provider, locationProviderClass));
			sb.AppendLine(string.Format("UTC time; device:{0} location:{1}", DateTime.UtcNow.ToString("yyyyMMdd HHmmss"), UnixTimestampUtils.From(location.Timestamp).ToString("yyyyMMdd HHmmss")));
			sb.AppendLine(string.Format(_invariantCulture, "position: {0:0.00000000} / {1:0.00000000}", location.LatitudeLongitude.x, location.LatitudeLongitude.y));
			sb.AppendLine(string.Format(_invariantCulture, "accuracy: {0:0.0}m", location.Accuracy));
			sb.AppendLine(string.Format(_invariantCulture, "heading: {0:0.0}°", location.Heading));
			sb.AppendLine(string.Format(_invariantCulture, "speed: {0:0.0}km/h", location.SpeedKmPerHour));
			sb.AppendLine(string.Format("HasGpsFix: {0}", location.HasGpsFix));
			sb.AppendLine(string.Format("SatellitesUsed: {0} SatellitesInView:{1}", location.SatellitesUsed, location.SatellitesInView));

			_logText.text = sb.ToString();
		}


		// Update is called once per frame
		void Update()
		{

		}



	}
}
