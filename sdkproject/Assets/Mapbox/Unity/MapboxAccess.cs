namespace Mapbox.Unity
{
	using UnityEngine;
	using System;
	using System.IO;
	using Mapbox.Geocoding;
	using Mapbox.Directions;
	using Mapbox.Platform;
	using Mapbox.Platform.Cache;
	using Mapbox.Unity.Telemetry;
	using Mapbox.Map;
	using Mapbox.MapMatching;
	using Mapbox.Tokens;

	/// <summary>
	/// Object for retrieving an API token and making http requests.
	/// Contains a lazy <see cref="T:Mapbox.Geocoding.Geocoder">Geocoder</see> and a lazy <see cref="T:Mapbox.Directions.Directions">Directions</see> for convenience.
	/// </summary>
	public class MapboxAccess : IFileSource
	{
		ITelemetryLibrary _telemetryLibrary;
		CachingWebFileSource _fileSource;

		public delegate void TokenValidationEvent(MapboxTokenStatus response);
		public event TokenValidationEvent OnTokenValidation;

		static MapboxAccess _instance;

		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static MapboxAccess Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new MapboxAccess();
				}
				return _instance;
			}
		}


		MapboxConfiguration _configuration;
		/// <summary>
		/// The Mapbox API access token. 
		/// See <see href="https://www.mapbox.com/mapbox-unity-sdk/docs/01-mapbox-api-token.html">Mapbox API Congfiguration in Unity</see>.
		/// </summary>
		public MapboxConfiguration Configuration
		{
			get
			{
				return _configuration;
			}
		}

		MapboxAccess()
		{
			LoadAccessToken();
		}

		public void SetConfiguration(MapboxConfiguration configuration, bool throwExecptions = true)
		{
			if (configuration == null)
			{
				if (throwExecptions)
				{
					throw new InvalidTokenException("No configuration file found! Configure your access token from the Mapbox > Settings menu.");
				}
			}

			TokenValidator.Retrieve(configuration.AccessToken, (response) =>
			{
				if (OnTokenValidation != null)
				{
					OnTokenValidation(response.Status);
				}

				if (response.Status != MapboxTokenStatus.TokenValid
				   && throwExecptions)
				{
					throw new InvalidTokenException(response.Status.ToString());
				}
			});

			_configuration = configuration;

			ConfigureFileSource();
			ConfigureTelemetry();
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
			SetConfiguration(configurationTextAsset == null ? null : JsonUtility.FromJson<MapboxConfiguration>(configurationTextAsset.text));
#else
			SetConfiguration(configurationTextAsset == null ? null : Mapbox.Json.JsonConvert.DeserializeObject<MapboxConfiguration>(configurationTextAsset.text));
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
			_telemetryLibrary = TelemetryFactory.GetTelemetryInstance();
			_telemetryLibrary.Initialize(_configuration.AccessToken);
			_telemetryLibrary.SetLocationCollectionState(GetTelemetryCollectionState());
			_telemetryLibrary.SendTurnstile();
		}

		public void SetLocationCollectionState(bool enable)
		{
			PlayerPrefs.SetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY, (enable ? 1 : 0));
			PlayerPrefs.Save();
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


		Geocoder _geocoder;
		/// <summary>
		/// Lazy geocoder.
		/// </summary>
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


		Directions _directions;
		/// <summary>
		/// Lazy Directions.
		/// </summary>
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

		MapMatcher _mapMatcher;
		/// <summary>
		/// Lazy Map Matcher.
		/// </summary>
		public MapMatcher MapMatcher
		{
			get
			{
				if (_mapMatcher == null)
				{
					_mapMatcher = new MapMatcher(new FileSource(_configuration.AccessToken), _configuration.DefaultTimeout);
				}
				return _mapMatcher;
			}
		}


		MapboxTokenApi _tokenValidator;
		/// <summary>
		/// Lazy token validator.
		/// </summary>
		public MapboxTokenApi TokenValidator
		{
			get
			{
				if (_tokenValidator == null)
				{
					_tokenValidator = new MapboxTokenApi();
				}
				return _tokenValidator;
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
		public int DefaultTimeout = 30;
	}
}
