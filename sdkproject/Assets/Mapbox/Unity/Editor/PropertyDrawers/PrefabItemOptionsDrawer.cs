namespace Mapbox.Editor
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEditor;
	using System;
	using System.Collections.Generic;

	[CustomPropertyDrawer(typeof(PrefabItemOptions))]
	public class PrefabItemOptionsDrawer : PropertyDrawer
	{

		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		private GUIContent prefabContent = new GUIContent
		{
			text = "Prefab",
			tooltip = "The prefab that will be spawned at the locations chosen below"
		};

		private GUIContent prefabLocationsTitle = new GUIContent
		{
			text = "Prefab Locations",
			tooltip = "The properties for creating POI filters"
		};


		private GUIContent findByDropDown = new GUIContent
		{
			text = "Find by",
			tooltip = "The type of filter you would like to use for looking up POIs"
		};

		private GUIContent categoriesDropDown = new GUIContent
		{
			text = "Category",
			tooltip = "Would you like to filter them by categories for the POI?"
		};

		private GUIContent  popularityDropDown = new GUIContent
		{
			text = "Popularity",
			tooltip = "Would you like to filter them by popularity of the POI?"
		};

		private GUIContent nameField = new GUIContent
		{
			text = "Name",
			tooltip = "All the POIs containing this string will be shown on the map"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//position.height = _lineHeight;
			var prefabItemCoreOptions = property.FindPropertyRelative("coreOptions");
			GUILayout.Label(prefabItemCoreOptions.FindPropertyRelative("sublayerName").stringValue + " Properties");
			GUILayout.Space(1);
			//Prefab Game Object
			EditorGUILayout.PropertyField(property.FindPropertyRelative("prefab"),prefabContent);

			//Prefab Locations title
			GUILayout.Label(prefabLocationsTitle);

			//FindBy drop down
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(findByDropDown);
			 
			var findByProp = property.FindPropertyRelative("findByType");
			findByProp.enumValueIndex = EditorGUILayout.Popup(findByProp.enumValueIndex, findByProp.enumDisplayNames);
			EditorGUILayout.EndHorizontal();

			switch((LocationPrefabFindBy)findByProp.enumValueIndex)
			{
				case (LocationPrefabFindBy.MapboxCategory):
					ShowCategoryOptions(property);
					break;
				case(LocationPrefabFindBy.AddressOrLatLon):
					ShowAddressOrLatLonUI(property);
					break;
				case (LocationPrefabFindBy.POIName):
					ShowPOINames(property);
					break;
				default:
					break;
			}
		}

		private void ShowCategoryOptions(SerializedProperty property)
		{
			//Category drop down
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(categoriesDropDown);

			var categoryProp = property.FindPropertyRelative("categories");
			categoryProp.intValue = (int)(LocationPrefabCategories)(EditorGUILayout.EnumFlagsField((LocationPrefabCategories)categoryProp.intValue));
			EditorGUILayout.EndHorizontal();

			ShowPopularityDropDown(property);
		}

		private void ShowAddressOrLatLonUI(SerializedProperty property)
		{
			
		}


		private void ShowPOINames(SerializedProperty property)
		{
			//Name field
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(nameField);

			var categoryProp = property.FindPropertyRelative("poiName");
			categoryProp.stringValue = EditorGUILayout.TextField(categoryProp.stringValue);
			EditorGUILayout.EndHorizontal();

			ShowPopularityDropDown(property);
		}

		private void ShowPopularityDropDown(SerializedProperty property)
		{
			//Popularity drop down
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(popularityDropDown);

			var popularityProp = property.FindPropertyRelative("popularity");
			popularityProp.enumValueIndex = EditorGUILayout.Popup(popularityProp.enumValueIndex, popularityProp.enumDisplayNames);
			EditorGUILayout.EndHorizontal();
		}

		private Rect GetNewRect(Rect position)
		{
			return new Rect(position.x, position.y, position.width, _lineHeight);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return _lineHeight;
		}
	}
}