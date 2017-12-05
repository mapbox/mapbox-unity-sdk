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
    using Mapbox.Unity.Utilities.DebugTools;
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
        string _lastValidatedToken;
		bool _showConfigurationFoldout;
        bool _showChangelogFoldout;
		Vector2 _scrollPosition;
		bool _isTokenValid;

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

		[MenuItem("Mapbox/Setup")]
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
			editorWindow.minSize = new Vector2(600, 200);
            editorWindow.titleContent = new GUIContent("Mapbox Setup");
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
            InitStyles();

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,_scrollViewStyle);
            //EditorGUIUtility.labelWidth = 200f;

            EditorGUILayout.BeginVertical(_verticalGroup);
            // Access token link.
            DrawAccessTokenLink();
			// Access token entry and validation.
			DrawAccessTokenField();
            // Draw the validation error, if one exists
            DrawError();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(_verticalGroup);
            //changelog
            DrawChangelog();
			// Configuration.
			DrawConfigurationSettings();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(_verticalGroup);
			// Examples.
			//DrawExampleLinks();
            EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();
		}

        void InitStyles()
        {
            _defaultContentColor = GUI.contentColor;
            _defaultBackgroundColor = GUI.backgroundColor;

            _titleStyle = new GUIStyle(GUI.skin.FindStyle("WhiteLabel"));
            _bodyStyle = new GUIStyle(GUI.skin.FindStyle("WordWrapLabel"));
            _linkStyle = new GUIStyle(GUI.skin.FindStyle("PR PrefabLabel"));
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

            _verticalGroup = new GUIStyle();
            _verticalGroup.margin = new RectOffset(0, 0, 0, 30);
            _horizontalGroup = new GUIStyle();
            _horizontalGroup.padding = new RectOffset(0, 0, 4, 0);
            _scrollViewStyle = new GUIStyle(GUI.skin.FindStyle("scrollview"));
            _scrollViewStyle.padding = new RectOffset(20, 20, 40, 0);
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

                EditorGUILayout.LabelField(labelContent, _bodyStyle, GUILayout.Width( _bodyStyle.CalcSize(labelContent).x ));
               
				if (GUILayout.Button(linkContent, _linkStyle))
				{
					Application.OpenURL("https://www.mapbox.com/install/unity/permission/");
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

            if( string.IsNullOrEmpty(_accessToken)){
                _accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Submit", _submitButtonStyle);
                EditorGUI.EndDisabledGroup();
            }
            else
			{
                //string input, validating
				if (_validating)
				{
                    _accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Checking", _submitButtonStyle);
					EditorGUI.EndDisabledGroup();

				}
                //string input, input is the same as the already validated token
                else if(string.Equals(_lastValidatedToken, _accessToken))
                {
                    if (string.Equals(_validationCode, "TokenValid"))
                    {
                        GUI.backgroundColor = _validBackgroundColor;
                        GUI.contentColor = _validContentColor;

                        _accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
                        GUILayout.Button("Valid", _validButtonStyle);
                        _isTokenValid = true;

                        //restore default colors;
                        GUI.contentColor = _defaultContentColor;
                        GUI.backgroundColor = _defaultBackgroundColor;

                    }
                    else
                    {

                        GUI.contentColor = _invalidContentColor;
                        GUI.backgroundColor = _invalidBackgroundColor;

                        _accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
                        GUILayout.Button("Invalid", _validButtonStyle);
                        _isTokenValid = false;

                        //restore default colors;
                        GUI.contentColor = _defaultContentColor;
                        GUI.backgroundColor = _defaultBackgroundColor;

                    }

                    if (GUILayout.Button("Submit", _submitButtonStyle))
                    {
                        Debug.Log("MapboxConfigurationWindow: " + "?");
                        Runnable.Run(ValidateToken(_accessToken));
                    }

                }
                //a token has been sent, but the current input doesn't match it.
                else
                {
                    _accessToken = EditorGUILayout.TextField("", _accessToken, _textFieldStyle);
                    
                    if (GUILayout.Button("Submit", _submitButtonStyle))
                    {
                        Debug.Log("MapboxConfigurationWindow: " + "?");
                        Runnable.Run(ValidateToken(_accessToken));
                    }
                }
            }

			EditorGUILayout.EndHorizontal();

		}

        void DrawError()
        {
            //draw the error message, if one exists
            EditorGUILayout.BeginHorizontal();

            if (!_isTokenValid && string.Equals(_lastValidatedToken, _accessToken) && !_validating)
            {
                EditorGUILayout.LabelField(_validationCode, _errorStyle);
            }
            else
            {
                EditorGUILayout.LabelField("", _errorStyle);
            }

            EditorGUILayout.EndHorizontal();

        }

        void DrawChangelog()
        {
            _showChangelogFoldout = EditorGUILayout.Foldout(_showChangelogFoldout, "Changelog", true);
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
            int rowCount = 2;

            NavigationBuilder.AddExampleScenesToBuildSettings();
            ScenesList list = (ScenesList)AssetDatabase.LoadAssetAtPath("Assets/Resources/Mapbox/ScenesList.asset", typeof(ScenesList));

			EditorGUI.BeginDisabledGroup(!_isTokenValid);
            EditorGUILayout.BeginHorizontal(_horizontalGroup);

            int currentRow = 0;
            foreach( var scene in list.SceneList)
            {
                GUILayout.Button(scene);

                currentRow++;
                if (currentRow < rowCount)
                {
                    currentRow = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(_horizontalGroup);
                }

            }

            EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
		}

		IEnumerator ValidateToken(string token)
		{
			_validating = true;
            _lastValidatedToken = token;

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