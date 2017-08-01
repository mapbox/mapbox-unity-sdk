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

		bool _justOpened = true;
		string _validationCode = "";
		bool _validating = false;

		[MenuItem("Mapbox/Configure")]
		static void Init()
		{
			Runnable.EnableRunnableInEditor();
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
			_memoryCacheSize = (int)_mapboxConfiguration.MemoryCacheSize;
			_mbtilesCacheSize = (int)_mapboxConfiguration.MbTilesCacheSize;
			_webRequestTimeout = _mapboxConfiguration.DefaultTimeout;

			var editorWindow = GetWindow(typeof(MapboxConfigurationWindow));
			editorWindow.minSize = new Vector2(900, 200);
			editorWindow.Show();
		}

		private void OnDestroy() { AssetDatabase.Refresh(); }

		private void OnDisable() { AssetDatabase.Refresh(); }

		private void OnLostFocus() { AssetDatabase.Refresh(); }

		void Update()
		{
			if (_justOpened && !string.IsNullOrEmpty(_accessToken))
			{
				Runnable.Run(ValidateToken(_accessToken));
				_justOpened = false;
			}
		}

		void OnGUI()
		{
			EditorGUIUtility.labelWidth = 200f;
			_memoryCacheSize = EditorGUILayout.IntSlider("Mem Cache Size (# of tiles)", _memoryCacheSize, 0, 1000);
			_mbtilesCacheSize = EditorGUILayout.IntSlider("MBTiles Cache Size (# of tiles)", _mbtilesCacheSize, 0, 3000);
			_webRequestTimeout = EditorGUILayout.IntField("Default Web Request Timeout (s)", _webRequestTimeout);
			_accessToken = EditorGUILayout.TextField("Access Token", _accessToken);
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (string.IsNullOrEmpty(_accessToken))
			{
				EditorGUILayout.HelpBox("You must have a valid access token!", MessageType.Error);
				if (GUILayout.Button("Get a token from mapbox.com for free"))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}
			}
			else
			{
				if (_validating)
				{
					GUILayout.Button("Verifying token...");
				}
				else if (GUILayout.Button("Save"))
				{
					Runnable.Run(ValidateToken(_accessToken));
				}
				else if (string.Equals(_validationCode, "TokenValid"))
				{
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("Token Valid: saved to " + _configurationFile, MessageType.Info);
				}
				else
				{
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox(_validationCode, MessageType.Error);
				}
			}
		}


		IEnumerator ValidateToken(string token)
		{
			_validating = true;

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
			_validating = false;
		}


		void ParseTokenResponse(string json)
		{
			var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			if (dict.ContainsKey("code"))
			{
				_validationCode = dict["code"].ToString();
			}

			SaveConfiguration();

		}


		void SaveConfiguration()
		{
			var configuration = new MapboxConfiguration
			{
				AccessToken = _accessToken,
				MemoryCacheSize = (uint)_memoryCacheSize,
				MbTilesCacheSize = (uint)_mbtilesCacheSize,
				DefaultTimeout = _webRequestTimeout,
			};

			var json = JsonUtility.ToJson(configuration);
			File.WriteAllText(_configurationFile, json);
			AssetDatabase.Refresh();
			Repaint();

			MapboxAccess.Instance.SetConfiguration(configuration);
		}
	}
}