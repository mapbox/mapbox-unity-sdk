namespace Mapbox.Unity.Location
{
	using System;
	using System.Globalization;
	using System.IO;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;

	/// <summary>
	/// The EditorLocationProvider is responsible for providing mock location data via log file obtained via Google's GnssLogger
	/// for testing purposes in the Unity editor.
	/// </summary>
	public class LocationGpsLogEditorLocationProvider : AbstractEditorLocationProvider
	{
		/// <summary>
		/// The mock "latitude, longitude" location, respresented with a string.
		/// You can search for a place using the embedded "Search" button in the inspector.
		/// This value can be changed at runtime in the inspector.
		/// </summary>
		[SerializeField]
		private TextAsset _gpsLogFile;


		private TextReader _textReader;
		private CultureInfo _invariantCulture = CultureInfo.InvariantCulture;


#if UNITY_EDITOR
		protected override void Awake()
		{
			base.Awake();
			MemoryStream ms = new MemoryStream(_gpsLogFile.bytes);
			_textReader = new StreamReader(ms);
		}

#endif

		private struct GpsFix
		{
			public Vector2d LatLng;
			public double Timestamp;
			public float Accuracy;
			public float? Speed;
		}

		private GpsFix NextLocationData
		{
			get
			{
				string line;
				line = _textReader.ReadLine();
				// rewind if end of log reached
				if (null == line)
				{
					((StreamReader)_textReader).BaseStream.Position = 0;
					((StreamReader)_textReader).DiscardBufferedData();
				}

				// skip comments
				while ((line = _textReader.ReadLine()).StartsWith("#")) { }

				string[] tokens = line.Split(",".ToCharArray());
				double lat;
				double lng;
				double timestamp;
				float speed;
				float accuracy;

				GpsFix gpsFix = new GpsFix();

				if (
					!double.TryParse(tokens[2], NumberStyles.Any, _invariantCulture, out lat)
					|| !double.TryParse(tokens[3], NumberStyles.Any, _invariantCulture, out lng)
				)
				{
					gpsFix.LatLng = Vector2d.zero;
				}
				else
				{
					gpsFix.LatLng = new Vector2d(lat, lng);
				}

				gpsFix.Timestamp = double.TryParse(tokens[7], NumberStyles.Any, _invariantCulture, out timestamp) ? timestamp / 1000 : 0;
				gpsFix.Speed = float.TryParse(tokens[5], NumberStyles.Any, _invariantCulture, out speed) ? speed : (float?)null;
				gpsFix.Accuracy = float.TryParse(tokens[6], NumberStyles.Any, _invariantCulture, out accuracy) ? accuracy : 0;

				return gpsFix;
			}
		}


		//private static readonly System.Random _random = new System.Random();
		protected override void SetLocation()
		{
			GpsFix gpsFix = NextLocationData;

			_currentLocation.IsLocationServiceEnabled = true;
			//_currentLocation.Heading = 0;
			_currentLocation.Heading = (float)(Math.Atan2(gpsFix.LatLng.y - _currentLocation.LatitudeLongitude.y, gpsFix.LatLng.x - _currentLocation.LatitudeLongitude.x) * 180 / Math.PI);
			_currentLocation.LatitudeLongitude = gpsFix.LatLng;
			_currentLocation.Timestamp = gpsFix.Timestamp;
			_currentLocation.Accuracy = gpsFix.Accuracy;
			//_currentLocation.Accuracy = _random.Next(1, 100);
			_currentLocation.SpeedMetersPerSecond = gpsFix.Speed;
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsHeadingUpdated = true;
		}
	}
}
