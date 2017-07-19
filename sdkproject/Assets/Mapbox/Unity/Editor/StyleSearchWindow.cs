namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Generic;
	using Mapbox.Unity;
	using Mapbox.Json;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories;
	using System.Collections;

	public class StyleSearchWindow : EditorWindow
	{
		SerializedProperty _property;

		string _username = "";
		string _errorString = "";

		List<Style> _styles;

		const string searchFieldName = "searchField";
		const float width = 320f;
		const float height = 300f;

		bool _isSearching = false;

		void OnEnable()
		{
			EditorApplication.playmodeStateChanged += OnModeChanged;
		}

		void OnDisable()
		{
			EditorApplication.playmodeStateChanged -= OnModeChanged;
		}

		bool hasSetFocus = false;

		public static void Open(SerializedProperty property)
		{
			StyleSearchWindow window = GetWindow<StyleSearchWindow>(true, "Search for style");

			window._property = property;
			window._username = property.FindPropertyRelative("UserName").stringValue;
			if (!string.IsNullOrEmpty(window._username))
			{
				window.Search(window._username);
			}

			Runnable.EnableRunnableInEditor();
			Event e = Event.current;
			Vector2 mousePos = GUIUtility.GUIToScreenPoint(e.mousePosition);
			window.position = new Rect(mousePos.x - width, mousePos.y, width, height);
		}

		void OnModeChanged()
		{
			Close();
		}

		void OnGUI()
		{
			GUILayout.Label("Styles");

			_username = EditorGUILayout.TextField("Username: ", _username);

			if (_username.Length == 0)
			{
				GUILayout.Label("Enter a user name");
			}
			else
			{
				if (GUILayout.Button("Search"))
				{
					Search(_username);
				}

				if (_styles != null && _styles.Count > 0)
				{
					GUILayout.Label("Results:");
					for (int i = 0; i < _styles.Count; i++)
					{
						var style = _styles[i];
						if (GUILayout.Button(style.Name))
						{
							_property.FindPropertyRelative("Name").stringValue = style.Name;
							_property.FindPropertyRelative("Id").stringValue = style.Id;
							_property.FindPropertyRelative("Modified").stringValue = style.Modified;
							_property.FindPropertyRelative("UserName").stringValue = style.UserName;
							_property.serializedObject.ApplyModifiedProperties();
							EditorUtility.SetDirty(_property.serializedObject.targetObject);

							Close();
						}
					}
				}
				else
				{
					if (_isSearching)
					{
						GUILayout.Label("Searching...");
					}
					else if (!string.IsNullOrEmpty(_errorString))
					{
						EditorGUILayout.HelpBox(_errorString, MessageType.Error);
						if (GUILayout.Button("Check username/get token with styles:list support"))
						{
							Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
						}
					}
					else
						GUILayout.Label("No search results");
				}
			}
		}

		void Search(string searchString)
		{
			_errorString = "";
			if (!string.IsNullOrEmpty(searchString) && !_isSearching)
			{
				Runnable.Run(ListStyles(MapboxAccess.Instance.Configuration.AccessToken));
				_isSearching = true;
			}
		}

		IEnumerator ListStyles(string token)
		{
			// "https://api.mapbox.com/styles/v1/{username}?access_token=your-access-token"
			var www = new WWW(Utils.Constants.BaseAPI + string.Format("styles/v1/{0}?access_token={1}", _username, token));
			while (!www.isDone)
			{
				yield return 0;
			}
			var json = www.text;
			if (!string.IsNullOrEmpty(json))
			{
				ParseResponse(json);
			}
		}

		void ParseResponse(string json)
		{
			_styles = new List<Style>();
			if (json.Contains("This API requires a token with styles:list scope"))
			{
				_errorString = "This API requires a token with styles:list scope!";
				_isSearching = false;
				return;
			}

			if (json.Contains("Forbidden"))
			{
				_errorString = "Forbidden!";
				_isSearching = false;
				return;
			}
			var settings = new JsonSerializerSettings();
			settings.DateParseHandling = DateParseHandling.None;
			var styleArray = JsonConvert.DeserializeObject<object[]>(json);
			foreach (var style in styleArray)
			{
				var styleData = JsonConvert.DeserializeObject<Dictionary<string, object>>(style.ToString(), settings);
				string id = string.Format("mapbox://styles/{0}/{1}", _username, styleData["id"]);
				_styles.Add(new Style() { Name = styleData["name"].ToString(), Id = id, Modified = styleData["modified"].ToString(), UserName = _username });
			}
			_isSearching = false;
			Repaint();
		}
	}
}