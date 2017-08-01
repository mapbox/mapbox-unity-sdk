namespace Mapbox.Unity
{
	using UnityEngine;
	using System;
	using Mapbox.Geocoding;
	using Mapbox.Directions;
	using Mapbox.Platform;
	using Mapbox.Platform.Cache;
	using Mapbox.Unity.Telemetry;
	using Mapbox.Map;

	/// <summary>
	/// Object for retrieving an API token and making http requests.
	/// Contains a lazy <see cref="T:Mapbox.Geocoding.Geocoder">Geocoder</see> and a lazy <see cref="T:Mapbox.Directions.Directions">Directions</see> for convenience.
	/// </summary>
	public class MapboxAccess : IFileSource
	{
		ITelemetryLibrary _telemetryLibrary;
		CachingWebFileSource _fileSource;

		static MapboxAccess _instance = new MapboxAccess();
		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static MapboxAccess Instance
		{
			get
			{
				return _instance;
			}
		}


		/// <summary>
		/// The Mapbox API access token. 
		/// See <see href="https://www.mapbox.com/mapbox-unity-sdk/docs/01-mapbox-api-token.html">Mapbox API Congfiguration in Unity</see>.
		/// </summary>
		MapboxConfiguration _configuration;
		public MapboxConfiguration Configuration
		{
			get
			{
				return _configuration;
			}
			private set
			{
				if (value == null)
				{
					throw new InvalidTokenException("Please configure your access token from the Mapbox menu!");
				}
				_configuration = value;
			}
		}

		MapboxAccess()
		{
			LoadAccessToken();
			ConfigureFileSource();
			ConfigureTelemetry();
		}
		
		public void SetConfiguration(MapboxConfiguration configuration)
		{
			_configuration = configuration;
		}

		/// <summary>
		/// Clear all existing tile caches. Deletes MBTiles database files.
		/// </summary>
		public void ClearCache()
		{
			CachingWebFileSource cwfs = _fileSource as CachingWebFileSource;
			if (null != cwfs)
			{
				cwfs.Clear();
			}
		}


		/// <summary>
		/// Loads the access token from <see href="https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity6.html">Resources folder</see>.
		/// </summary>
		private void LoadAccessToken()
		{
			TextAsset configurationTextAsset = Resources.Load<TextAsset>(Constants.Path.MAPBOX_RESOURCES_RELATIVE);
#if !WINDOWS_UWP
			Configuration = configurationTextAsset == null ? null : JsonUtility.FromJson<MapboxConfiguration>(configurationTextAsset.text);
#else
			Configuration = configurationTextAsset == null ? null : Mapbox.Json.JsonConvert.DeserializeObject<MapboxConfiguration>(configurationTextAsset.text);
#endif
		}


		void ConfigureFileSource()
		{
			_fileSource = new CachingWebFileSource(_configuration.AccessToken)
				.AddCache(new MemoryCache(_configuration.MemoryCacheSize))
#if !UNITY_WEBGL
				.AddCache(new MbTilesCache(_configuration.MbTilesCacheSize))
#endif
				;
		}


		void ConfigureTelemetry()
		{
#if UNITY_EDITOR
			_telemetryLibrary = TelemetryEditor.Instance;
#elif UNITY_IOS
			_telemetryLibrary = TelemetryIos.Instance;
#elif UNITY_ANDROID
			_telemetryLibrary = TelemetryAndroid.Instance;
#else
			_telemetryLibrary = TelemetryFallback.Instance;
#endif


			_telemetryLibrary.Initialize(_configuration.AccessToken);
			_telemetryLibrary.SetLocationCollectionState(GetTelemetryCollectionState());
			_telemetryLibrary.SendTurnstile();
		}

		public void SetLocationCollectionState(bool enable)
		{
			PlayerPrefs.SetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY, (enable ? 1 : 0));
			_telemetryLibrary.SetLocationCollectionState(enable);
		}

		bool GetTelemetryCollectionState()
		{
			if (!PlayerPrefs.HasKey(Constants.Path.SHOULD_COLLECT_LOCATION_KEY))
			{
				PlayerPrefs.SetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY, 1);
			}
			return PlayerPrefs.GetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY) != 0;
		}

		/// <summary>
		/// Makes an asynchronous url query.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="url">URL.</param>
		/// <param name="callback">Callback.</param>
		public IAsyncRequest Request(
			string url
			, Action<Response> callback
			, int timeout = 10
			, CanonicalTileId tileId = new CanonicalTileId()
			, string mapId = null
		)
		{
			return _fileSource.Request(url, callback, _configuration.DefaultTimeout, tileId, mapId);
		}


		/// <summary>
		/// Lazy geocoder.
		/// </summary>
		Geocoder _geocoder;
		public Geocoder Geocoder
		{
			get
			{
				if (_geocoder == null)
				{
					_geocoder = new Geocoder(new FileSource(_configuration.AccessToken));
				}
				return _geocoder;
			}
		}


		/// <summary>
		/// Lazy Directions.
		/// </summary>
		Directions _directions;
		public Directions Directions
		{
			get
			{
				if (_directions == null)
				{
					_directions = new Directions(new FileSource(_configuration.AccessToken));
				}
				return _directions;
			}
		}


		class InvalidTokenException : Exception
		{
			public InvalidTokenException(string message) : base(message)
			{
			}
		}
	}

	public class MapboxConfiguration
	{
		public string AccessToken;
		public uint MemoryCacheSize = 500;
		public uint MbTilesCacheSize = 2000;
		public int DefaultTimeout = 10;
	}
}
