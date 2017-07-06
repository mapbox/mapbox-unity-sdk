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

		[MenuItem("Mapbox/Configure")]
		static void Init()
		{
			_configurationFile = Path.Combine(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE, Unity.Constants.Path.CONFIG_FILE);

			Runnable.EnableRunnableInEditor();
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
			_memoryCacheSize = (int)_mapboxConfiguration.MemoryCacheSize;
			_mbtilesCacheSize = (int)_mapboxConfiguration.MbTilesCacheSize;
			_webRequestTimeout = _mapboxConfiguration.DefaultTimeout;

			var window = (MapboxConfigurationWindow)GetWindow(typeof(MapboxConfigurationWindow));
			window.Show();
		}

		void OnGUI()
		{
			EditorGUIUtility.labelWidth = 200f;
			_memoryCacheSize = EditorGUILayout.IntSlider("Mem Cache Size (# of tiles)", _memoryCacheSize, 0, 1000);
			_mbtilesCacheSize = EditorGUILayout.IntSlider("MBTiles Cache Size (# of tiles)", _mbtilesCacheSize, 0, 3000);
			_webRequestTimeout = EditorGUILayout.IntField("Default Web Request Timeout", _webRequestTimeout);

			_accessToken = EditorGUILayout.TextField("Access Token", _accessToken);
			if (string.IsNullOrEmpty(_accessToken))
			{
				EditorGUILayout.HelpBox("You must have an access token!", MessageType.Error);
				if (GUILayout.Button("Get a token from mapbox.com for free"))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}
			}
			else
			{
				if (!string.Equals(_accessToken, _lastAccessToken) || string.IsNullOrEmpty(_validationCode))
				{
					Runnable.Run(ValidateToken(_accessToken));
				}

				var messageType = MessageType.Error;
				if (string.Equals(_validationCode, "TokenValid"))
				{
					messageType = MessageType.Info;
					var json = JsonUtility.ToJson(new MapboxConfiguration { AccessToken = _accessToken, MemoryCacheSize = (uint)_memoryCacheSize, MbTilesCacheSize = (uint)_mbtilesCacheSize, DefaultTimeout = _webRequestTimeout });
					File.WriteAllText(_configurationFile, json);
					AssetDatabase.Refresh();
					EditorGUILayout.HelpBox("TokenValid: saved to " + _configurationFile, messageType);
				}
				else
				{
					EditorGUILayout.HelpBox(_validationCode, messageType);
				}
				Repaint();
			}
		}

		IEnumerator ValidateToken(string token)
		{
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

		void ParseTokenResponse(string json)
		{
			var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			if (dict.ContainsKey("code"))
			{
				_validationCode = dict["code"].ToString();
			}
		}
	}
}