namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Generic;
	using Mapbox.Geocoding;
	using Mapbox.Unity;

	public class GeocodeAttributeSearchWindow : EditorWindow
	{
		SerializedProperty _property;

		string _searchInput = "";

		ForwardGeocodeResource _resource;

		List<Feature> _features;

		System.Action<string> _callback;

		const string searchFieldName = "searchField";
		const float width = 320f;
		const float height = 300f;

		bool _isSearching = false;

		void OnEnable()
		{
			_resource = new ForwardGeocodeResource("");
			EditorApplication.playModeStateChanged += OnModeChanged;
		}

		void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnModeChanged;
		}

		bool hasSetFocus = false;

		public static void Open(SerializedProperty property)
		{
			GeocodeAttributeSearchWindow window = EditorWindow.GetWindow<GeocodeAttributeSearchWindow>(true, "Search for location");

			window._property = property;

			Event e = Event.current;
			Vector2 mousePos = GUIUtility.GUIToScreenPoint(e.mousePosition);

			window.position = new Rect(mousePos.x - width, mousePos.y, width, height);
		}

		void OnModeChanged(PlayModeStateChange state)
		{
			Close();
		}

		void OnGUI()
		{
			GUILayout.Label("Search for a location");

			string oldSearchInput = _searchInput;

			GUI.SetNextControlName(searchFieldName);
			_searchInput = GUILayout.TextField(_searchInput);

			if (_searchInput.Length == 0)
			{
				GUILayout.Label("Type in a location to find it's latitude and longtitude");
			}
			else
			{
				bool changed = oldSearchInput != _searchInput;
				if (changed)
				{
					HandleUserInput(_searchInput);
				}

				if (_features.Count > 0)
				{
					GUILayout.Label("Results:");
					for (int i = 0; i < _features.Count; i++)
					{
						Feature feature = _features[i];
						string coordinates = feature.Center.x + ", " + feature.Center.y;
						string buttonContent = feature.Address + " (" + coordinates + ")";

						if (GUILayout.Button(buttonContent))
						{
							_property.stringValue = coordinates;

							_property.serializedObject.ApplyModifiedProperties();
							EditorUtility.SetDirty(_property.serializedObject.targetObject);

							Close();
						}
					}
				}
				else
				{
					if (_isSearching)
						GUILayout.Label("Searching...");
					else
						GUILayout.Label("No search results");
				}
			}

			if (!hasSetFocus)
			{
				GUI.FocusControl(searchFieldName);
				hasSetFocus = true;
			}
		}

		void HandleUserInput(string searchString)
		{
			_features = new List<Feature>();
			_isSearching = true;

			if (!string.IsNullOrEmpty(searchString))
			{
				_resource.Query = searchString;
				MapboxAccess.Instance.Geocoder.Geocode(_resource, HandleGeocoderResponse);
			}
		}

		void HandleGeocoderResponse(ForwardGeocodeResponse res)
		{
			if (res != null)
			{
				_features = res.Features;
			}
			_isSearching = false;
			this.Repaint();

			//_hasResponse = true;
			//_coordinate = res.Features[0].Center;
			//Response = res;
			//if (OnGeocoderResponse != null)
			//{
			//	OnGeocoderResponse(this, EventArgs.Empty);
			//}
		}
	}
}