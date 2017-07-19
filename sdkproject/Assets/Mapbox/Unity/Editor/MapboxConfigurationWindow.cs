namespace Mapbox.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using System.IO;
	using System.Collections;
	using Mapbox.Unity;
	using Mapbox.Json;
	using Mapbox.Unity.Utilities;

	public class MapboxConfigurationWindow : EditorWindow
	{
		static string _configurationFile;
		static MapboxConfiguration _mapboxConfiguration;
		static string _accessToken;
		[Range(0, 1000)]
		static int _memoryCacheSize = 500;
		[Range(0, 3000)]
		static int _mbtilesCacheSize = 2000;
		static int _webRequestTimeout = 10;

		string _lastAccessToken;
		string _validationCode;

		private static bool _validating = false;
		private static bool _tokenSaved = false;
		private static bool _savingConfig = false;
		private static int _previousMemCacheSize = -1;
		private static int _previousMbTilesCacheSize = -1;
		private static int _previousWebRequestTimeout = -1;
		private static System.Timers.Timer _timer = null;
		private static MapboxConfigurationWindow _configWindow = null;

		[MenuItem("Mapbox/Configure")]
		static void Init()
		{
			_configurationFile = Path.Combine(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE, Unity.Constants.Path.CONFIG_FILE);

			if (!Directory.Exists(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE))
			{
				Directory.CreateDirectory(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE);
			}
			if (!File.Exists(_configurationFile))
			{
				var json = JsonUtility.ToJson(new MapboxConfiguration { AccessToken = "", MemoryCacheSize = (uint)_memoryCacheSize, MbTilesCacheSize = (uint)_mbtilesCacheSize, DefaultTimeout = _webRequestTimeout });
				File.WriteAllText(_configurationFile, json);
			}

			var configurationJson = File.ReadAllText(_configurationFile);
			_mapboxConfiguration = JsonUtility.FromJson<MapboxConfiguration>(configurationJson);
			_accessToken = _mapboxConfiguration.AccessToken;
			_memoryCacheSize = _previousMemCacheSize = (int)_mapboxConfiguration.MemoryCacheSize;
			_mbtilesCacheSize = _previousMbTilesCacheSize = (int)_mapboxConfiguration.MbTilesCacheSize;
			_webRequestTimeout = _previousWebRequestTimeout = _mapboxConfiguration.DefaultTimeout;

			_configWindow = (MapboxConfigurationWindow)GetWindow(typeof(MapboxConfigurationWindow));
			_configWindow.Show();
		}


		// OnDestroy is called when the EditorWindow is closed
		private void OnDestroy() { AssetDatabase.Refresh(); }

		// This function is called when the object becomes disabled or inactive
		private void OnDisable() { AssetDatabase.Refresh(); }

		// Called when the window loses keyboard focus
		private void OnLostFocus() { AssetDatabase.Refresh(); }


		void OnGUI()
		{
			if (null == _configWindow)
			{
				Init();
				return;
			}

			EditorGUIUtility.labelWidth = 200f;
			_memoryCacheSize = EditorGUILayout.IntSlider("Mem Cache Size (# of tiles)", _memoryCacheSize, 0, 1000);
			_mbtilesCacheSize = EditorGUILayout.IntSlider("MBTiles Cache Size (# of tiles)", _mbtilesCacheSize, 0, 3000);
			_webRequestTimeout = EditorGUILayout.IntField("Default Web Request Timeout (s)", _webRequestTimeout);


			#region handle token verification


			_accessToken = EditorGUILayout.TextField("Access Token", _accessToken);

			bool matchSuccess = false;
			if (!string.IsNullOrEmpty(_accessToken))
			{
				System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[pst]k\.[0-9a-zA-Z-_]+\.[0-9a-zA-Z-_]+$");
				System.Text.RegularExpressions.Match match = regex.Match(_accessToken);
				matchSuccess = match.Success;
			}

			if (string.IsNullOrEmpty(_accessToken) || !matchSuccess)
			{
				_tokenSaved = false;
				EditorGUILayout.HelpBox("You must have a valid access token!", MessageType.Error);
				if (GUILayout.Button("Get a token from mapbox.com for free"))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}
			}
			else
			{
				if (
					(!string.Equals(_accessToken, _lastAccessToken) || string.IsNullOrEmpty(_validationCode))
					&& !_validating
				)
				{
					Runnable.Run(ValidateToken(_accessToken));
				}

				if (string.Equals(_validationCode, "TokenValid") && !_tokenSaved)
				{
					SaveConfiguration();
					EditorGUILayout.HelpBox("TokenValid: saved to " + _configurationFile, MessageType.Info);
					_tokenSaved = true;
				}
				else if (string.Equals(_validationCode, "TokenValid") && _tokenSaved)
				{
					EditorGUILayout.HelpBox("TokenValid: saved to " + _configurationFile, MessageType.Info);
				}
				else if (_validating)
				{
					EditorGUILayout.HelpBox("Verifying token!", MessageType.Error);
				}
				else
				{
					EditorGUILayout.HelpBox(_validationCode, MessageType.Error);
				}
			}


			#endregion


			if (
				_memoryCacheSize != _previousMemCacheSize
				|| _mbtilesCacheSize != _previousMbTilesCacheSize
				|| _webRequestTimeout != _previousWebRequestTimeout
			)
			{
				if (null != _timer)
				{
					_timer.Stop();
					_timer.Elapsed -= Timer_Elapsed;
					_timer.Dispose();
					_timer = null;
				}

				_timer = new System.Timers.Timer(500);
				_timer.AutoReset = false;
				_timer.Elapsed += Timer_Elapsed;
				_timer.Start();

			}

			_previousMemCacheSize = _memoryCacheSize;
			_previousMbTilesCacheSize = _mbtilesCacheSize;
			_previousWebRequestTimeout = _webRequestTimeout;

		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			SaveConfiguration(); ;
		}


		IEnumerator ValidateToken(string token)
		{
			try
			{
				_validating = true;
				_lastAccessToken = token;

				var www = new WWW(Utils.Constants.BaseAPI + "tokens/v2?access_token=" + token);
				while (!www.isDone)
				{
					yield return 0;
				}
				var json = www.text;
				if (!string.IsNullOrEmpty(json))
				{
					ParseTokenResponse(json);
				}
			}
			finally
			{
				_validating = false;
				Repaint();
			}
		}

		void ParseTokenResponse(string json)
		{
			var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			if (dict.ContainsKey("code"))
			{
				_validationCode = dict["code"].ToString();
			}
		}


		void SaveConfiguration()
		{
			try
			{
				_savingConfig = true;
				// if access token is not valid, but save was triggered by changing other parameters, nuke it
				if (!string.Equals(_validationCode, "TokenValid"))
				{
					_accessToken = "";
				}
				var json = JsonUtility.ToJson(new MapboxConfiguration { AccessToken = _accessToken, MemoryCacheSize = (uint)_memoryCacheSize, MbTilesCacheSize = (uint)_mbtilesCacheSize, DefaultTimeout = _webRequestTimeout });
				File.WriteAllText(_configurationFile, json);
			}
			finally
			{
				_savingConfig = false;
			}
		}

	}
}