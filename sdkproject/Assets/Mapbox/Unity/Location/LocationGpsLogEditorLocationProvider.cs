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


		Vector2d LatitudeLongitude
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
				if (
					!double.TryParse(tokens[2], NumberStyles.Any, _invariantCulture, out lat)
					|| !double.TryParse(tokens[3], NumberStyles.Any, _invariantCulture, out lng)
				)
				{
					return Vector2d.zero;
				}

				return new Vector2d(lat, lng);
			}
		}


		protected override void SetLocation()
		{
			_currentLocation.Heading = 0;
			_currentLocation.LatitudeLongitude = LatitudeLongitude;
			_currentLocation.Accuracy = _accuracy;
			_currentLocation.Timestamp = UnixTimestampUtils.To(DateTime.UtcNow);
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsHeadingUpdated = true;
		}
	}
}
