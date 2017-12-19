namespace Mapbox.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor.SceneManagement;
	using UnityEditor;
	using System.IO;
	using System.Collections;
	using Mapbox.Unity;
	using Mapbox.Tokens;
	using Mapbox.Json;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Utilities.DebugTools;
	using UnityEditor.Callbacks;
	using System;

	public class MapboxConfigurationWindow : EditorWindow
	{
		public static MapboxConfigurationWindow instance;
		static MapboxConfiguration _mapboxConfig;
		static MapboxTokenStatus _currentTokenStatus;
		static MapboxAccess _mapboxAccess;
		static bool _waitingToLoad = false;

		//default mapboxconfig
		static string _configurationFile;
		static string _accessToken = "";
		[Range(0, 1000)]
		static int _memoryCacheSize = 500;
		[Range(0, 3000)]
		static int _mbtilesCacheSize = 2000;
		static int _webRequestTimeout = 30;

		//mapbox access callbacks
		static bool _listeningForTokenValidation = false;
		static bool _validating = false;
		static string _lastValidatedToken;

		//gui flags
		bool _showConfigurationFoldout;
		bool _showChangelogFoldout;
		Vector2 _scrollPosition;

		//samples
		static int _selectedSample;
		static ScenesList _sceneList;
		static GUIContent[] _sampleContent;
		string _sceneToOpen;

		//styles
		GUISkin _skin;
		Color _defaultContentColor;
		Color _defaultBackgroundColor;
		GUIStyle _titleStyle;
		GUIStyle _bodyStyle;
		GUIStyle _linkStyle;

		GUIStyle _textFieldStyle;
		GUIStyle _submitButtonStyle;
		GUIStyle _checkingButtonStyle;

		GUIStyle _validFieldStyle;
		GUIStyle _validButtonStyle;
		Color _validContentColor;
		Color _validBackgroundColor;

		GUIStyle _invalidFieldStyle;
		GUIStyle _invalidButtonStyle;
		Color _invalidContentColor;
		Color _invalidBackgroundColor;
		GUIStyle _errorStyle;

		GUIStyle _verticalGroup;
		GUIStyle _horizontalGroup;
		GUIStyle _scrollViewStyle;

		GUIStyle _sampleButtonStyle;


		[DidReloadScripts]
		public static void ShowWindowOnImport()
		{
			if (ShouldShowConfigurationWindow())
			{
				PlayerPrefs.SetInt(Constants.Path.DID_PROMPT_CONFIGURATION, 1);
				PlayerPrefs.Save();
				InitWhenLoaded();
			}
		}

		[MenuItem("Mapbox/Setup")]
		static void InitWhenLoaded()
		{
			if(EditorApplication.isCompiling && !_waitingToLoad)
			{
				//subscribe to updates
				_waitingToLoad = true;
				EditorApplication.update += InitWhenLoaded;
				return;
			}

			if(!EditorApplication.isCompiling)
			{
				//unsubscribe from updates if waiting
				if(_waitingToLoad)
				{
					EditorApplication.update -= InitWhenLoaded;
					_waitingToLoad = false;
				}

				Init();
			}
		}

		static void Init()
		{
			Runnable.EnableRunnableInEditor();

			//verify that the config file exists
			_configurationFile = Path.Combine(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE, Unity.Constants.Path.CONFIG_FILE);
			if (!Directory.Exists(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE))
			{
				Directory.CreateDirectory(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE);
			}

			if (!File.Exists(_configurationFile))
			{
				_mapboxConfig = new MapboxConfiguration
				{
					AccessToken = _accessToken,
					MemoryCacheSize = (uint)_memoryCacheSize,
					MbTilesCacheSize = (uint)_mbtilesCacheSize,
					DefaultTimeout = _webRequestTimeout
				};
				var json = JsonUtility.ToJson(_mapboxConfig);
				File.WriteAllText(_configurationFile, json);
				AssetDatabase.Refresh();
			}

			//finish opening the window after the assetdatabase is refreshed.
			EditorApplication.delayCall += OpenWindow;
		}

		static void OpenWindow()
		{
			EditorApplication.delayCall -= OpenWindow;

			//setup mapboxaccess listener
			_mapboxAccess = MapboxAccess.Instance;
			if (!_listeningForTokenValidation)
			{
				_mapboxAccess.OnTokenValidation += HandleValidationResponse;
				_listeningForTokenValidation = true;
			}

			//setup local variables from mapbox config file
			_mapboxConfig = _mapboxAccess.Configuration;
			if (_mapboxConfig != null)
			{
				_accessToken = _mapboxConfig.AccessToken;
				_memoryCacheSize = (int)_mapboxConfig.MemoryCacheSize;
				_mbtilesCacheSize = (int)_mapboxConfig.MbTilesCacheSize;
				_webRequestTimeout = (int)_mapboxConfig.DefaultTimeout;

			}

			//validate current config
			if (!string.IsNullOrEmpty(_accessToken))
			{
				SubmitConfiguration();
			}

			//cache sample scene gui content
			GetSceneList();
			_selectedSample = -1;

			//instantiate the config window
			instance = GetWindow(typeof(MapboxConfigurationWindow)) as MapboxConfigurationWindow;
			instance.minSize = new Vector2(800, 350);
			instance.titleContent = new GUIContent("Mapbox Setup");
			instance.Show();

		}

		static void GetSceneList()
		{
			_sceneList = Resources.Load<ScenesList>("Mapbox/ScenesList");

			//exclude scenes with no image data
			var content = new List<SceneData>();
			if (_sceneList != null)
			{
				for (int i = 0; i < _sceneList.SceneList.Length; i++)
				{
					if (File.Exists(_sceneList.SceneList[i].ScenePath))
					{
						if (_sceneList.SceneList[i].Image != null)
						{
							content.Add(_sceneList.SceneList[i]);
						}
					}
				}
			}

			_sampleContent = new GUIContent[content.Count];
			for (int i = 0; i < _sampleContent.Length; i++)
			{
				_sampleContent[i] = new GUIContent(content[i].Name, content[i].Image, content[i].ScenePath);
			}

		}


		/// <summary>
		/// Unity Events
		/// </summary>

		private void OnDisable() { AssetDatabase.Refresh(); }

		private void OnDestroy() { AssetDatabase.Refresh(); }

		private void OnLostFocus() { AssetDatabase.Refresh(); }


		/// <summary>
		/// Mapbox access
		/// </summary>
		private static void SubmitConfiguration()
		{
			var mapboxConfiguration = new MapboxConfiguration
			{
				AccessToken = _accessToken,
				MemoryCacheSize = (uint)_memoryCacheSize,
				MbTilesCacheSize = (uint)_mbtilesCacheSize,
				DefaultTimeout = _webRequestTimeout
			};
			_mapboxAccess.SetConfiguration(mapboxConfiguration, false);
			_validating = true;
		}

		private static void HandleValidationResponse(MapboxTokenStatus status)
		{
			_currentTokenStatus = status;
			_validating = false;
			_lastValidatedToken = _accessToken;

			//save the config
			_configurationFile = Path.Combine(Unity.Constants.Path.MAPBOX_RESOURCES_ABSOLUTE, Unity.Constants.Path.CONFIG_FILE);
			var json = JsonUtility.ToJson(_mapboxAccess.Configuration);
			File.WriteAllText(_configurationFile, json);
		}


		void OnGUI()
		{
			//only run after init
			if (instance == null)
			{
				//TODO: loading message?
				InitWhenLoaded();
				return;
			}

			InitStyles();

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, _scrollViewStyle);
			EditorGUILayout.BeginVertical();
			// Access token link.
			DrawAccessTokenLink();

			// Access token entry and validation.
			DrawAccessTokenField();

			// Draw the validation error, if one exists
			DrawError();
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(_verticalGroup);

			// Configuration
			DrawConfigurationSettings();
			GUILayout.Space(8);

			// Changelog
			DrawChangelog();

			EditorGUILayout.EndVertical();

			// Draw Example links if the scenelist asset is where it should be.
			if (_sampleContent.Length > 0)
			{
				EditorGUILayout.BeginVertical(_verticalGroup);
				DrawExampleLinks();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();
		}

		void InitStyles()
		{
			_defaultContentColor = GUI.contentColor;
			_defaultBackgroundColor = GUI.backgroundColor;

			_titleStyle = new GUIStyle(GUI.skin.FindStyle("IN TitleText"));
			_titleStyle.padding.left = 3;
			//_titleStyle.fontSize = 16;
			_bodyStyle = new GUIStyle(GUI.skin.FindStyle("WordWrapLabel"));
			//_bodyStyle.fontSize = 14;
			_linkStyle = new GUIStyle(GUI.skin.FindStyle("PR PrefabLabel"));
			//_linkStyle.fontSize = 14;
			_linkStyle.padding.left = 0;
			_linkStyle.padding.top = -1;

			_textFieldStyle = new GUIStyle(GUI.skin.FindStyle("TextField"));
			_textFieldStyle.margin.right = 0;
			_textFieldStyle.margin.top = 0;

			_submitButtonStyle = new GUIStyle(GUI.skin.FindStyle("ButtonRight"));
			_submitButtonStyle.padding.top = 0;
			_submitButtonStyle.margin.top = 0;
			_submitButtonStyle.fixedWidth = 200;

			_checkingButtonStyle = new GUIStyle(_submitButtonStyle);

			_validFieldStyle = new GUIStyle(_textFieldStyle);
			_validButtonStyle = new GUIStyle(GUI.skin.FindStyle("LODSliderRange"));
			_validButtonStyle.alignment = TextAnchor.MiddleCenter;
			_validButtonStyle.padding = new RectOffset(0, 0, 0, 0);
			_validButtonStyle.border = new RectOffset(0, 0, 5, -2);
			_validButtonStyle.fixedWidth = 60;

			_validContentColor = new Color(1, 1, 1, .7f);
			_validBackgroundColor = new Color(.2f, .8f, .2f, 1);
			_invalidContentColor = new Color(1, 1, 1, .7f);
			_invalidBackgroundColor = new Color(.8f, .2f, .2f, 1);

			_errorStyle = new GUIStyle(GUI.skin.FindStyle("ErrorLabel"));
			_errorStyle.padding.left = 5;

			_verticalGroup = new GUIStyle();
			_verticalGroup.margin = new RectOffset(0, 0, 0, 35);
			_horizontalGroup = new GUIStyle();
			_horizontalGroup.padding = new RectOffset(0, 0, 4, 4);
			_scrollViewStyle = new GUIStyle(GUI.skin.FindStyle("scrollview"));
			_scrollViewStyle.padding = new RectOffset(20, 20, 40, 0);

			_sampleButtonStyle = new GUIStyle(GUI.skin.FindStyle("button"));
			_sampleButtonStyle.imagePosition = ImagePosition.ImageAbove;
			_sampleButtonStyle.padding = new RectOffset(0, 0, 5, 5);
			_sampleButtonStyle.fontStyle = FontStyle.Bold;
		}

		void DrawAccessTokenLink()
		{

			EditorGUILayout.LabelField("Access Token", _titleStyle);

			EditorGUILayout.BeginHorizontal(_horizontalGroup);
			if (string.IsNullOrEmpty(_accessToken))
			{
				//fit box to text to create an 'inline link'
				GUIContent labelContent = new GUIContent("Copy your free token from");
				GUIContent linkContent = new GUIContent("mapbox.com");

				EditorGUILayout.LabelField(labelContent, _bodyStyle, GUILayout.Width(_bodyStyle.CalcSize(labelContent).x));

				if (GUILayout.Button(linkContent, _linkStyle))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}

				//create link cursor
				var rect = GUILayoutUtility.GetLastRect();
				rect.width = _linkStyle.CalcSize(new GUIContent(linkContent)).x;
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

				GUILayout.FlexibleSpace();

			}
			else
			{

				GUIContent labelContent = new GUIContent("Manage your tokens at");
				GUIContent linkContent = new GUIContent("mapbox.com/studio/accounts/tokens/");

				EditorGUILayout.LabelField(labelContent, _bodyStyle, GUILayout.Width(_bodyStyle.CalcSize(labelContent).x));

				if (GUILayout.Button(linkContent, _linkStyle))
				{
					Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
				}

				//create link cursor
				var rect = GUILayoutUtility.GetLastRect();
				rect.width = _linkStyle.CalcSize(new GUIContent(linkContent)).x;
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

				GUILayout.FlexibleSpace();

			}
			EditorGUILayout.EndHorizontal();

		}

		void DrawAccessTokenField()
		{
			EditorGUILayout.BeginHorizontal(_horizontalGroup);

			//_accessToken is empty
			if (string.IsNullOrEmpty(_accessToken))
			{
				_accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button("Submit", _submitButtonStyle);
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				//_accessToken is being validated
				if (_validating)
				{
					EditorGUI.BeginDisabledGroup(true);
					_accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
					GUILayout.Button("Checking", _submitButtonStyle);
					EditorGUI.EndDisabledGroup();
				}
				//_accessToken is the same as the already submitted token
				else if (string.Equals(_lastValidatedToken, _accessToken))
				{
					//valid token
					if (_currentTokenStatus == MapboxTokenStatus.TokenValid)
					{
						GUI.backgroundColor = _validBackgroundColor;
						GUI.contentColor = _validContentColor;

						_accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
						GUILayout.Button("Valid", _validButtonStyle);

						GUI.contentColor = _defaultContentColor;
						GUI.backgroundColor = _defaultBackgroundColor;

					}
					//invalid token
					else
					{

						GUI.contentColor = _invalidContentColor;
						GUI.backgroundColor = _invalidBackgroundColor;

						_accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
						GUILayout.Button("Invalid", _validButtonStyle);

						GUI.contentColor = _defaultContentColor;
						GUI.backgroundColor = _defaultBackgroundColor;

					}
					//Submit button
					if (GUILayout.Button("Submit", _submitButtonStyle))
					{
						SubmitConfiguration();
					}

				}
				//_accessToken is a new, unsubmitted token.
				else
				{
					_accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);

					if (GUILayout.Button("Submit", _submitButtonStyle))
					{
						SubmitConfiguration();
					}
				}
			}

			EditorGUILayout.EndHorizontal();

		}

		void DrawError()
		{
			//draw the error message, if one exists
			EditorGUILayout.BeginHorizontal(_horizontalGroup);

			if (_currentTokenStatus != MapboxTokenStatus.TokenValid
				&& _currentTokenStatus != MapboxTokenStatus.StatusNotYetSet
				&& string.Equals(_lastValidatedToken, _accessToken)
				&& !_validating)
			{
				EditorGUILayout.LabelField(_currentTokenStatus.ToString(), _errorStyle);
			}
			else
			{
				//EditorGUILayout.LabelField("", _errorStyle);
			}

			EditorGUILayout.EndHorizontal();

		}

		void DrawChangelog()
		{
			//_showChangelogFoldout = EditorGUILayout.Foldout(_showChangelogFoldout, "v" + Constants.SDK_VERSION + " Changelog", true);

			//if (_showChangelogFoldout)
			//{
			EditorGUI.indentLevel = 2;
			//EditorGUILayout.BeginHorizontal(_horizontalGroup);
			//	GUIContent labelContent = new GUIContent("SDK version " + Constants.SDK_VERSION + " changelog, and learn how to contribute at");
			GUIContent linkContent = new GUIContent("v" + Constants.SDK_VERSION + " changelog");
			//	EditorGUILayout.LabelField(labelContent, _bodyStyle, GUILayout.Width(_bodyStyle.CalcSize(labelContent).x));

			if (GUILayout.Button(linkContent, _linkStyle))
			{
				Application.OpenURL("https://www.mapbox.com/mapbox-unity-sdk/docs/05-changelog.html");
			}

			//	GUILayout.FlexibleSpace();
			//EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel = 0;
			//}
		}

		void DrawConfigurationSettings()
		{
			_showConfigurationFoldout = EditorGUILayout.Foldout(_showConfigurationFoldout, "Configuration", true);

			if (_showConfigurationFoldout)
			{
				EditorGUIUtility.labelWidth = 240f;
				EditorGUI.indentLevel = 2;
				_memoryCacheSize = EditorGUILayout.IntSlider("Mem Cache Size (# of tiles)", _memoryCacheSize, 0, 1000);
				_mbtilesCacheSize = EditorGUILayout.IntSlider("MBTiles Cache Size (# of tiles)", _mbtilesCacheSize, 0, 3000);
				_webRequestTimeout = EditorGUILayout.IntField("Default Web Request Timeout (s)", _webRequestTimeout);

				EditorGUILayout.BeginHorizontal(_horizontalGroup);
				GUILayout.Space(35f);
				if (GUILayout.Button("Save"))
				{
					SubmitConfiguration();
				}
				EditorGUI.indentLevel = 0;
				EditorGUIUtility.labelWidth = 0f;
				EditorGUILayout.EndHorizontal();
			}

		}

		void DrawExampleLinks()
		{
			EditorGUI.BeginDisabledGroup(_currentTokenStatus != MapboxTokenStatus.TokenValid
			                            || _validating);

			if (_currentTokenStatus == MapboxTokenStatus.TokenValid)
			{
				EditorGUILayout.LabelField("Sample Scenes", _titleStyle);
			}
			else
			{
				EditorGUILayout.LabelField("Sample Scenes", "Paste your mapbox access token to get started", _titleStyle);
			}


			int rowCount = 2;
			EditorGUILayout.BeginHorizontal(_horizontalGroup);

			_selectedSample = GUILayout.SelectionGrid(-1, _sampleContent, rowCount, _sampleButtonStyle);

			if (_selectedSample != -1)
			{
				EditorApplication.isPaused = false;
				EditorApplication.isPlaying = false;

				_sceneToOpen = _sampleContent[_selectedSample].tooltip;
				EditorApplication.update += OpenAndPlayScene;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
		}

		private void OpenAndPlayScene()
		{
			if( EditorApplication.isPlaying)
			{
				return;
			}

			EditorApplication.update -= OpenAndPlayScene;

			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				EditorSceneManager.OpenScene(_sceneToOpen);
				EditorApplication.isPlaying = true;

				var editorWindow = GetWindow(typeof(MapboxConfigurationWindow));
				editorWindow.Close();

			}
			else
			{
				_sceneToOpen = null;
				_selectedSample = -1;
			}
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