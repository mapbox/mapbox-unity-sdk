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
	using Mapbox.Platform.TilesetTileJSON;

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

		private static MapboxAccess _instance;

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


		public static bool Configured;
		public static string ConfigurationJSON;
		private MapboxConfiguration _configuration;
		private string _tokenNotSetErrorMessage = "No configuration file found! Configure your access token from the Mapbox > Setup menu.";

		/// <summary>
		/// The Mapbox API access token.
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
			if (null == _configuration || string.IsNullOrEmpty(_configuration.AccessToken))
			{
				Debug.LogError(_tokenNotSetErrorMessage);
			}
		}

		public void SetConfiguration(MapboxConfiguration configuration, bool throwExecptions = true)
		{
			if (configuration == null)
			{
				if (throwExecptions)
				{
					throw new InvalidTokenException(_tokenNotSetErrorMessage);
				}

			}

			if (null == configuration || string.IsNullOrEmpty(configuration.AccessToken))
			{
				Debug.LogError(_tokenNotSetErrorMessage);
			}
			else
			{
				TokenValidator.Retrieve(configuration.AccessToken, (response) =>
				{
					if (OnTokenValidation != null)
					{
						OnTokenValidation(response.Status);
					}

					if (response.Status != MapboxTokenStatus.TokenValid
					   && throwExecptions)
					{
						configuration.AccessToken = string.Empty;
						Debug.LogError(new InvalidTokenException(response.Status.ToString().ToString()));
					}
				});

				_configuration = configuration;

				ConfigureFileSource();
				ConfigureTelemetry();

				Configured = true;
			}
		}


		public void ClearAllCacheFiles()
		{
			// explicity call Clear() to close any connections that might be referenced by the current scene
			CachingWebFileSource cwfs = _fileSource as CachingWebFileSource;
			if (null != cwfs) { cwfs.Clear(); }

			// remove all left over files (eg orphaned .journal) from the cache directory
			string cacheDirectory = Path.Combine(Application.persistentDataPath, "cache");
			if (!Directory.Exists(cacheDirectory)) { return; }

			foreach (var file in Directory.GetFiles(cacheDirectory))
			{
				try
				{
					File.Delete(file);
				}
				catch (Exception deleteEx)
				{
					Debug.LogErrorFormat("Could not delete [{0}]: {1}", file, deleteEx);
				}
			}

			//reinit caches after clear
			if (null != cwfs) { cwfs.ReInit(); }

			Debug.Log("done clearing caches");
		}

		/// <summary>
		/// Loads the access token from <see href="https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity6.html">Resources folder</see>.
		/// </summary>
		private void LoadAccessToken()
		{

			if (string.IsNullOrEmpty(ConfigurationJSON))
			{
				TextAsset configurationTextAsset = Resources.Load<TextAsset>(Constants.Path.MAPBOX_RESOURCES_RELATIVE);
				if (null == configurationTextAsset)
				{
					throw new InvalidTokenException(_tokenNotSetErrorMessage);
				}
				ConfigurationJSON = configurationTextAsset.text;
			}

#if !WINDOWS_UWP
			SetConfiguration(ConfigurationJSON == null ? null : JsonUtility.FromJson<MapboxConfiguration>(ConfigurationJSON));
#else
			SetConfiguration(ConfigurationJSON == null ? null : Mapbox.Json.JsonConvert.DeserializeObject<MapboxConfiguration>(ConfigurationJSON));
#endif
		}


		void ConfigureFileSource()
		{
			_fileSource = new CachingWebFileSource(_configuration.AccessToken, _configuration.AutoRefreshCache)
				.AddCache(new MemoryCache(_configuration.MemoryCacheSize))
#if !UNITY_WEBGL
				.AddCache(new SQLiteCache(_configuration.FileCacheSize))
#endif
				;
		}


		void ConfigureTelemetry()
		{
			// TODO: enable after token validation has been made async
			//if (
			//	null == _configuration
			//	|| string.IsNullOrEmpty(_configuration.AccessToken)
			//	|| !_tokenValid
			//)
			//{
			//	Debug.LogError(_tokenNotSetErrorMessage);
			//	return;
			//}
			try
			{
				_telemetryLibrary = TelemetryFactory.GetTelemetryInstance();
				_telemetryLibrary.Initialize(_configuration.AccessToken);
				_telemetryLibrary.SetLocationCollectionState(GetTelemetryCollectionState());
				_telemetryLibrary.SendTurnstile();
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error initializing telemetry: {0}", ex);
			}
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


		TileJSON _tileJson;
		/// <summary>
		/// Lazy TileJSON wrapper: https://www.mapbox.com/api-documentation/#retrieve-tilejson-metadata
		/// </summary>
		public TileJSON TileJSON
		{
			get
			{
				if (_tileJson == null)
				{
					_tileJson = new TileJSON(new FileSource(_configuration.AccessToken), _configuration.DefaultTimeout);
				}
				return _tileJson;
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
		public uint FileCacheSize = 2500;
		public int DefaultTimeout = 30;
		public bool AutoRefreshCache = false;
	}
}
