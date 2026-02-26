using System.IO;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.UnityMapService;
using UnityEditor;
using UnityEngine;

namespace MapboxUnitySDK.Editor
{
	[InitializeOnLoad]
	public class MapboxConfigurationWindow : EditorWindow
	{
		public static MapboxConfigurationWindow instance;
		static MapboxTokenStatus _currentTokenStatus = MapboxTokenStatus.StatusNotYetSet;
		static bool _waitingToLoad = false;

		//default mapboxconfig
		static string _configurationFilePath;
		static MapboxContext _mapboxContext;

		//gui flags
		bool _showConfigurationFoldout;
		bool _showChangelogFoldout;
		Vector2 _scrollPosition;

		//styles
		GUISkin _skin;
		Color _defaultContentColor;
		Color _defaultBackgroundColor;
		GUIStyle _titleStyle;
		GUIStyle _bodyStyle;
		GUIStyle _linkStyle;

		GUIStyle _textFieldStyle;
		GUIStyle _submitButtonStyle;

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

		[MenuItem("Mapbox/Token Configuration")]
		static void InitWhenLoaded()
		{
			if (EditorApplication.isCompiling && !_waitingToLoad)
			{
				//subscribe to updates
				_waitingToLoad = true;
				EditorApplication.update += InitWhenLoaded;
				return;
			}

			if (!EditorApplication.isCompiling)
			{
				//unsubscribe from updates if waiting
				if (_waitingToLoad)
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

			if (!Directory.Exists(Constants.Path.MAPBOX_RESOURCES_ABSOLUTE))
			{
				Directory.CreateDirectory(Constants.Path.MAPBOX_RESOURCES_ABSOLUTE);
			}

			_configurationFilePath = Constants.Path.MAPBOX_CONFIG_ABSOLUTE;
			if (!File.Exists(_configurationFilePath))
			{
				WriteConfigFile(new MapboxConfiguration(), _configurationFilePath);
				AssetDatabase.Refresh();
			}

			//finish opening the window after the assetdatabase is refreshed.
			EditorApplication.delayCall += OpenWindow;
		}

		private static void WriteConfigFile(MapboxConfiguration config, string path)
		{
			var json = JsonUtility.ToJson(config);
			File.WriteAllText(path, json);
		}

		static void OpenWindow()
		{
			_mapboxContext = new MapboxContext();
			EditorApplication.delayCall -= OpenWindow;
			//instantiate the config window
			instance = GetWindow(typeof(MapboxConfigurationWindow)) as MapboxConfigurationWindow;
			instance.minSize = new Vector2(800, 180);
			instance.titleContent = new GUIContent("Mapbox Setup");
			instance.Show();
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
			_mapboxContext.ValidateToken(() =>
			{
				WriteConfigFile(_mapboxContext.Configuration, _configurationFilePath);
				Debug.Log(_mapboxContext.TokenStatus());
			});
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

			if (GUILayout.Button("Save"))
			{
				SubmitConfiguration();
			}
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}

		void InitStyles()
		{
			_defaultContentColor = GUI.contentColor;
			_defaultBackgroundColor = GUI.backgroundColor;

			_titleStyle = new GUIStyle(GUI.skin.FindStyle("IN TitleText"));
			_titleStyle.padding.left = 3;
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
			if (string.IsNullOrEmpty(_mapboxContext.Configuration.AccessToken))
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
			if (string.IsNullOrEmpty(_mapboxContext.Configuration.AccessToken))
			{
				_mapboxContext.Configuration.AccessToken = EditorGUILayout.TextField("", _mapboxContext.Configuration.AccessToken, _textFieldStyle);
			}
			else
			{
				//_accessToken is being validated
				if (_mapboxContext.TokenStatus() == MapboxTokenStatus.TokenValid)
				{
					GUI.backgroundColor = _validBackgroundColor;
					GUI.contentColor = _validContentColor;

					_mapboxContext.Configuration.AccessToken = EditorGUILayout.TextField("", _mapboxContext.Configuration.AccessToken, _textFieldStyle);

					GUI.contentColor = _defaultContentColor;
					GUI.backgroundColor = _defaultBackgroundColor;
				}
				//_accessToken is a new, unsubmitted token.
				else
				{
					_mapboxContext.Configuration.AccessToken = EditorGUILayout.TextField("", _mapboxContext.Configuration.AccessToken, _textFieldStyle);
				}
			}

			EditorGUILayout.EndHorizontal();

		}

		void DrawError()
		{
			//draw the error message, if one exists
			EditorGUILayout.BeginHorizontal(_horizontalGroup);

			if (_currentTokenStatus != MapboxTokenStatus.TokenValid
				&& _currentTokenStatus != MapboxTokenStatus.StatusNotYetSet)
			{
				EditorGUILayout.LabelField(_currentTokenStatus.ToString(), _errorStyle);
			}

			EditorGUILayout.EndHorizontal();

		}
	}

	public class MapboxMenu
	{
		[MenuItem("Mapbox/Clear Caches")]
		public static void DeleteAllCache()
		{
			if (Application.isPlaying)
			{
				var core = GameObject.FindObjectOfType<MapBehaviourCore>();
				if (core == null)
				{
					Debug.LogError("No Map Core found");
					return;
				}
				core.MapboxMap.ClearCachedData();
			}
			else
			{
				MapboxCacheManager.DeleteAllCache();	
			}
		}
	}
}