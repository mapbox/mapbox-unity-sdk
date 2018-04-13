namespace Mapbox.Editor
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEditor;
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


		private GUIContent  popularityDropDown = new GUIContent
		{
			text = "Popularity",
			tooltip = "Would you like to filter them by popularity of the POI?"
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
			EditorGUILayout.PrefixLabel(findByDropDown);
			 
			var findByProp = property.FindPropertyRelative("findByType");
			findByProp.enumValueIndex = EditorGUILayout.Popup(findByProp.enumValueIndex, findByProp.enumDisplayNames);


			//Popularity drop down
			EditorGUILayout.PrefixLabel(popularityDropDown);

			var popularityProp = property.FindPropertyRelative("popularity");
			popularityProp.enumValueIndex = EditorGUILayout.Popup(popularityProp.enumValueIndex, popularityProp.enumDisplayNames);
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