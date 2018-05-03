namespace Mapbox.Unity.Location
{
	using System;
	using System.Globalization;
	using System.IO;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;

	/// <summary>
	/// <para>
	/// The EditorLocationProvider is responsible for providing mock location data via log file obtained
	/// via a customized 'Google GnssLogger' for testing purposes in the Unity editor.
	/// </para>
	/// <para>GnssLogger was changed to include 'Bearing' and 'DeviceOrientation' in the log here:</para>
	/// <para>
	/// https://github.com/google/gps-measurement-tools/blob/2f6ba51e7ddfa3a34d0f75933f833af20417042a/GNSSLogger/app/src/main/java/com/google/android/apps/location/gps/gnsslogger/FileLogger.java#L246-L256
	/// </para>
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
			public string provider;
			public Vector2d LatLng;
			public double Timestamp;
			public float Accuracy;
			public float? Speed;
			public float? UserHeading;
			public float? DeviceOrientation;
			public bool? HasGpxFix;
		}

		private GpsFix NextLocationData
		{
			get
			{
				string line = string.Empty;

				while (1 == 1)
				{
					line = _textReader.ReadLine();
					// rewind if end of log (or last empty line) reached
					if (null == line || string.IsNullOrEmpty(line))
					{
						((StreamReader)_textReader).BaseStream.Position = 0;
						((StreamReader)_textReader).DiscardBufferedData();
						continue;
					}

					// skip comments
					if (line.StartsWith("#")) { continue; } else { break; }
				}

				string[] tokens = line.Split(",".ToCharArray());
				//log was neither created with stock GnssLogger nor with customized one
				if (tokens.Length != 8 && tokens.Length != 10)
				{
					Debug.LogError("unsupported log file");
					return new GpsFix();
				}

				double lat;
				double lng;
				double timestamp;
				float speed;
				float accuracy;
				float userHeading;
				float deviceOrientation;

				GpsFix gpsFix = new GpsFix();

				gpsFix.HasGpxFix = tokens[0].Equals("Fix") ? true : (bool?)null;
				gpsFix.provider = tokens[1];

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

				gpsFix.Speed = float.TryParse(tokens[5], NumberStyles.Any, _invariantCulture, out speed) ? speed : (float?)null;
				gpsFix.Accuracy = float.TryParse(tokens[6], NumberStyles.Any, _invariantCulture, out accuracy) ? accuracy : 0;

				// backwards compability with unmodified GnssLogger files that don't contain a 'Bearing'
				// timestamp is always the last value
				int idxTimestamp = tokens.Length - 1;
				gpsFix.Timestamp = double.TryParse(tokens[idxTimestamp], NumberStyles.Any, _invariantCulture, out timestamp) ? timestamp / 1000 : 0;

				// bearing and orientation included
				if (tokens.Length == 10)
				{
					gpsFix.UserHeading = float.TryParse(tokens[7], NumberStyles.Any, _invariantCulture, out userHeading) ? userHeading : (float?)null;
					gpsFix.DeviceOrientation = float.TryParse(tokens[8], NumberStyles.Any, _invariantCulture, out deviceOrientation) ? deviceOrientation : (float?)null;
				}

				return gpsFix;
			}
		}


		protected override void SetLocation()
		{
			GpsFix gpsFix = NextLocationData;

			_currentLocation.IsLocationServiceEnabled = true;

			if (gpsFix.UserHeading.HasValue)
			{
				_currentLocation.UserHeading = gpsFix.UserHeading.Value;
			}
			else
			{
				// calculate heading ourselves
				_currentLocation.UserHeading = (float)(Math.Atan2(gpsFix.LatLng.y - _currentLocation.LatitudeLongitude.y, gpsFix.LatLng.x - _currentLocation.LatitudeLongitude.x) * 180 / Math.PI);
			}

			if (gpsFix.DeviceOrientation.HasValue)
			{
				_currentLocation.DeviceOrientation = gpsFix.DeviceOrientation.Value;
			}
			else
			{
				// simluate device rotating all the time
				_currentLocation.DeviceOrientation += 15;
				if (_currentLocation.DeviceOrientation > 359) { _currentLocation.DeviceOrientation = 0; }
			}

			_currentLocation.Provider = gpsFix.provider;
			_currentLocation.HasGpsFix = gpsFix.HasGpxFix;
			_currentLocation.LatitudeLongitude = gpsFix.LatLng;
			_currentLocation.Timestamp = gpsFix.Timestamp;
			_currentLocation.Accuracy = gpsFix.Accuracy;
			_currentLocation.SpeedMetersPerSecond = gpsFix.Speed;
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsUserHeadingUpdated = true;
		}
	}
}
