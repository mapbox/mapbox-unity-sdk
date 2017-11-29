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
	using UnityEditor.Callbacks;
	using System;

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
		bool _showConfigurationFoldout;
		Vector2 _scrollPosition;
		bool _isTokenValid;

		[DidReloadScripts]
		static void ShowWindowOnImport()
		{
			if (ShouldShowConfigurationWindow())
			{
				PlayerPrefs.SetInt(Constants.Path.DID_PROMPT_CONFIGURATION, 1);
				PlayerPrefs.Save();
				Init();
			}
		}

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
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true);
			EditorGUIUtility.labelWidth = 200f;

			// Access token link.
			EditorGUILayout.LabelField("Access Token");
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			DrawAccessTokenLink();

			// Access token entry and validation.
			DrawAccessTokenField();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Configuration.
			DrawConfigurationSettings();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Examples.
			DrawExampleLinks();
			EditorGUILayout.EndScrollView();
		}

		void DrawAccessTokenLink()
		{
			if (string.IsNullOrEmpty(_accessToken))
			{
				if (GUILayout.Button("Copy your free token from mapbox.com"))
				{
					Application.OpenURL("https://www.mapbox.com/install/unity/permission/");
				}
			}
			else
			{
				if (GUILayout.Button("Manage your tokens at mapbox.com"))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}
			}
		}

		void DrawAccessTokenField()
		{
			EditorGUILayout.BeginHorizontal();
			_accessToken = EditorGUILayout.TextField("", _accessToken);

			if (!string.IsNullOrEmpty(_accessToken))
			{
				if (_validating)
				{
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Checking");
					EditorGUI.EndDisabledGroup();

				}
				else if (GUILayout.Button("Submit"))
				{
					Debug.Log("MapboxConfigurationWindow: " + "?");
					Runnable.Run(ValidateToken(_accessToken));

				}
				else if (string.Equals(_validationCode, "TokenValid"))
				{
					EditorGUILayout.HelpBox("Valid", MessageType.Info);
					_isTokenValid = true;
				}
				else
				{
					EditorGUILayout.HelpBox("Invalid", MessageType.Error);
					_isTokenValid = false;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		void DrawConfigurationSettings()
		{
			_showConfigurationFoldout = EditorGUILayout.Foldout(_showConfigurationFoldout, "Configuration", true);

			if (_showConfigurationFoldout)
			{
				EditorGUI.indentLevel = 2;
				_memoryCacheSize = EditorGUILayout.IntSlider("Mem Cache Size (# of tiles)", _memoryCacheSize, 0, 1000);
				_mbtilesCacheSize = EditorGUILayout.IntSlider("MBTiles Cache Size (# of tiles)", _mbtilesCacheSize, 0, 3000);
				_webRequestTimeout = EditorGUILayout.IntField("Default Web Request Timeout (s)", _webRequestTimeout);
			}
		}

		void DrawExampleLinks()
		{
			EditorGUI.BeginDisabledGroup(!_isTokenValid);
			GUILayout.Button("thing1");
			GUILayout.Button("thing2");
			GUILayout.Button("thing3");
			EditorGUI.EndDisabledGroup();
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

		static bool ShouldShowConfigurationWindow()
		{
			if (!PlayerPrefs.HasKey(Constants.Path.DID_PROMPT_CONFIGURATION))
			{
				PlayerPrefs.SetInt(Constants.Path.DID_PROMPT_CONFIGURATION, 0);
			}

			return PlayerPrefs.GetInt(Constants.Path.DID_PROMPT_CONFIGURATION) == 0;
		}
	}
}