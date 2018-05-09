namespace Mapbox.Examples.Scripts
{
	using Mapbox.Unity.Location;
	using Mapbox.Utils;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using UnityEngine;
	using UnityEngine.UI;

	public class LogLocationProviderData : MonoBehaviour
	{

		[SerializeField]
		private Text _logText;

		[SerializeField]
		private Toggle _logToggle;


		private CultureInfo _invariantCulture = CultureInfo.InvariantCulture;
		private bool _logToFile = false;
		private TextWriter _textWriter = null;
		/// <summary>column delimiter when logging to file </summary>
		private string _delimiter = ";";


		void Start()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;

			if (null != _logToggle)
			{
				_logToggle.onValueChanged.AddListener((isOn) => { _logToFile = isOn; });
			}
		}


		void OnDestroy()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			closeLogFile();
		}


		void LocationProvider_OnLocationUpdated(Location location)
		{

			/////////////// GUI logging //////////////////////
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(string.Format("IsLocationServiceEnabled: {0}", location.IsLocationServiceEnabled));
			sb.AppendLine(string.Format("IsLocationServiceInitializing: {0}", location.IsLocationServiceInitializing));
			sb.AppendLine(string.Format("IsLocationUpdated: {0}", location.IsLocationUpdated));
			sb.AppendLine(string.Format("IsHeadingUpdated: {0}", location.IsUserHeadingUpdated));
			string locationProviderClass = LocationProviderFactory.Instance.DefaultLocationProvider.GetType().Name;
			sb.AppendLine(string.Format("location provider: {0} ({1})", location.Provider, locationProviderClass));
			sb.AppendLine(string.Format("UTC time:{0}  - device:  {1}{0}  - location:{2}", Environment.NewLine, DateTime.UtcNow.ToString("yyyyMMdd HHmmss"), UnixTimestampUtils.From(location.Timestamp).ToString("yyyyMMdd HHmmss")));
			sb.AppendLine(string.Format(_invariantCulture, "position: {0:0.00000000} / {1:0.00000000}", location.LatitudeLongitude.x, location.LatitudeLongitude.y));
			sb.AppendLine(string.Format(_invariantCulture, "accuracy: {0:0.0}m", location.Accuracy));
			sb.AppendLine(string.Format(_invariantCulture, "user heading: {0:0.0}°", location.UserHeading));
			sb.AppendLine(string.Format(_invariantCulture, "device orientation: {0:0.0}°", location.DeviceOrientation));
			sb.AppendLine(nullableAsStr<float>(location.SpeedKmPerHour, "speed: {0:0.0}km/h"));
			sb.AppendLine(nullableAsStr<bool>(location.HasGpsFix, "HasGpsFix: {0}"));
			sb.AppendLine(nullableAsStr<int>(location.SatellitesUsed, "SatellitesUsed:{0} ") + nullableAsStr<int>(location.SatellitesInView, "SatellitesInView:{0}"));

			_logText.text = sb.ToString();


			/////////////// file logging //////////////////////

			// start logging to file
			if (_logToFile && null == _textWriter)
			{
				string fileName = "MBX-location-log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
				string persistentPath = Application.persistentDataPath;
				string fullFilePathAndName = Path.Combine(persistentPath, fileName);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
				// use `GetFullPath` on that to sanitize the path: replaces `/` returned by `Application.persistentDataPath` with `\`
				fullFilePathAndName = Path.GetFullPath(fullFilePathAndName);
#endif
//#if UNITY_ANDROID
//				if (RuntimePlatform.Android == Application.platform)
//				{
//					// persistentDataPath evaluates to storage/Android/data/<bundle-identifier>/files
//					// but files writting there cannot be accessed from Windows file explorer
//					// save into dedicated subdirectory below `files`
//					string logFolder = Path.Combine(persistentPath, "location-logs");
//					if (!Directory.Exists(logFolder)) { Directory.CreateDirectory(logFolder); }
//					fullFilePathAndName = Path.Combine(logFolder, fileName);
//				}
//#endif

				Debug.Log("starting new log file: " + fullFilePathAndName);

				_textWriter = new StreamWriter(fullFilePathAndName, false, new UTF8Encoding(false));
				_textWriter.WriteLine("location service enabled{0}location service initializing{0}location updated{0}heading updated{0}location provider{0}location provider class{0}UTC device{0}UTC location{0}lat{0}lng{0}accuracy[m]{0}user heading[°]{0}device orientation[°]{0}speed{0}has gps fix{0}satellites used{0}satellites in view", _delimiter);
				_logToggle.GetComponentInChildren<Text>().text = "stop logging";
			}


			// stop logging to file
			if (!_logToFile && null != _textWriter)
			{
				Debug.Log("stop logging to file");
				_logToggle.GetComponentInChildren<Text>().text = "start logging";
				closeLogFile();
			}


			// write line to log file
			if (_logToFile && null != _textWriter)
			{
				string[] lineTokens = new string[]
				{
					location.IsLocationServiceEnabled.ToString(),
					location.IsLocationServiceInitializing.ToString(),
					location.IsLocationUpdated.ToString(),
					location.IsUserHeadingUpdated.ToString(),
					location.Provider,
					LocationProviderFactory.Instance.DefaultLocationProvider.GetType().Name,
					DateTime.UtcNow.ToString("yyyyMMdd-HHmmss.fff"),
					UnixTimestampUtils.From(location.Timestamp).ToString("yyyyMMdd-HHmmss.fff"),
					string.Format(_invariantCulture, "{0:0.00000000}", location.LatitudeLongitude.x),
					string.Format(_invariantCulture, "{0:0.00000000}", location.LatitudeLongitude.y),
					string.Format(_invariantCulture, "{0:0.0}", location.Accuracy),
					string.Format(_invariantCulture, "{0:0.0}", location.UserHeading),
					string.Format(_invariantCulture, "{0:0.0}", location.DeviceOrientation),
					nullableAsStr<float>(location.SpeedKmPerHour, "{0:0.0}"),
					nullableAsStr<bool>(location.HasGpsFix, "{0}"),
					nullableAsStr<int>(location.SatellitesUsed, "{0}"),
					nullableAsStr<int>(location.SatellitesInView, "{0}")
				};

				string logMsg = string.Join(_delimiter, lineTokens);
				Debug.Log(logMsg);
				_textWriter.WriteLine(logMsg);
			}
		}


		private string nullableAsStr<T>(T? val, string formatString = null) where T : struct
		{
			if (null == val && null == formatString) { return "[not supported by provider]"; }
			if (null == val && null != formatString) { return string.Format(_invariantCulture, formatString, "[not supported by provider]"); }
			if (null != val && null == formatString) { return val.Value.ToString(); }
			return string.Format(_invariantCulture, formatString, val);
		}


		private void closeLogFile()
		{
			if (null == _textWriter) { return; }
			Debug.Log("closing stream writer");
			_textWriter.Flush();
			_textWriter.Close();
			_textWriter.Dispose();
			_textWriter = null;
		}


		void Update() { }



	}
}
