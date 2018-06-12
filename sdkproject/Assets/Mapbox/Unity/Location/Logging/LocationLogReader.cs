namespace Mapbox.Unity.Location
{


	using Mapbox.Utils;
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using UnityEngine;


	/// <summary>
	/// Parses location data and returns Location objects.
	/// </summary>
	public class LocationLogReader : LocationLogAbstractBase, IDisposable
	{


		public LocationLogReader(byte[] contents)
		{
			MemoryStream ms = new MemoryStream(contents);
			_textReader = new StreamReader(ms);
		}


		private bool _disposed;
		private TextReader _textReader;


		#region idisposable


		~LocationLogReader()
		{
			Dispose(false);
		}


		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					if (null != _textReader)
					{
						_textReader.Close();
						_textReader.Dispose();
						_textReader = null;
					}
				}
				_disposed = true;
			}
		}


		#endregion


		/// <summary>
		/// Returns 'Location' objects from the data passed in. Loops through the data.
		/// </summary>
		/// <returns>'Location' objects and loops through the data.</returns>
		public IEnumerator<Location> GetLocations()
		{

			while (true)
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

				string[] tokens = line.Split(Delimiter.ToCharArray());
				//simple safety net: check if number of columns matches
				if (tokens.Length != HeaderNames.Length)
				{
					Debug.LogError("unsupported log file");
					yield return new Location();
				}

				Location location = new Location();

				location.IsLocationServiceEnabled = bool.Parse(tokens[(int)LogfileColumns.LocationServiceEnabled]);
				location.IsLocationServiceInitializing = bool.Parse(tokens[(int)LogfileColumns.LocationServiceInitializing]);
				location.IsLocationUpdated = bool.Parse(tokens[(int)LogfileColumns.LocationUpdated]);
				location.IsUserHeadingUpdated = bool.Parse(tokens[(int)LogfileColumns.UserHeadingUpdated]);
				location.Provider = tokens[(int)LogfileColumns.LocationProvider];
				location.ProviderClass = tokens[(int)LogfileColumns.LocationProviderClass];

				DateTime dtDevice;
				string dtDeviceTxt = tokens[(int)LogfileColumns.UtcTimeDevice];
				if (DateTime.TryParseExact(dtDeviceTxt, "yyyyMMdd-HHmmss.fff", _invariantCulture, DateTimeStyles.AssumeUniversal, out dtDevice))
				{
					location.TimestampDevice = UnixTimestampUtils.To(dtDevice);
				}

				DateTime dtLocation;
				string dtLocationTxt = tokens[(int)LogfileColumns.UtcTimeOfLocation];
				if (DateTime.TryParseExact(dtLocationTxt, "yyyyMMdd-HHmmss.fff", _invariantCulture, DateTimeStyles.AssumeUniversal, out dtLocation))
				{
					location.Timestamp = UnixTimestampUtils.To(dtLocation);
				}

				double lat;
				string latTxt = tokens[(int)LogfileColumns.Latitude];
				double lng;
				string lngTxt = tokens[(int)LogfileColumns.Longitude];
				if (
					!double.TryParse(latTxt, NumberStyles.Any, _invariantCulture, out lat)
					|| !double.TryParse(lngTxt, NumberStyles.Any, _invariantCulture, out lng)
				)
				{
					location.LatitudeLongitude = Vector2d.zero;
				}
				else
				{
					location.LatitudeLongitude = new Vector2d(lat, lng);
				}


				float accuracy;
				location.Accuracy = float.TryParse(tokens[(int)LogfileColumns.Accuracy], NumberStyles.Any, _invariantCulture, out accuracy) ? accuracy : 0;
				float userHeading;
				location.UserHeading = float.TryParse(tokens[(int)LogfileColumns.UserHeading], NumberStyles.Any, _invariantCulture, out userHeading) ? userHeading : 0;
				float deviceOrientation;
				location.DeviceOrientation = float.TryParse(tokens[(int)LogfileColumns.DeviceOrientation], NumberStyles.Any, _invariantCulture, out deviceOrientation) ? deviceOrientation : 0;
				float speed;
				location.SpeedMetersPerSecond = float.TryParse(tokens[(int)LogfileColumns.Speed], NumberStyles.Any, _invariantCulture, out speed) ? speed / 3.6f : (float?)null;
				bool hasGpsFix;
				location.HasGpsFix = bool.TryParse(tokens[(int)LogfileColumns.HasGpsFix], out hasGpsFix) ? hasGpsFix : (bool?)null;
				int satellitesUsed;
				location.SatellitesUsed = int.TryParse(tokens[(int)LogfileColumns.SatellitesUsed], out satellitesUsed) ? satellitesUsed : (int?)null;
				int satellitesInView;
				location.SatellitesInView = int.TryParse(tokens[(int)LogfileColumns.SatellitesInView], out satellitesInView) ? satellitesInView : (int?)null;

				yield return location;
			}
		}



	}
}
