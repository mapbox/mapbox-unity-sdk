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
		private int shiftLeftPixels = -16;
		private GUIContent prefabLocationsTitle = new GUIContent
		{
			text = "Prefab Locations",
			tooltip = "Where on the map to spawn the selected prefab"
		};


		private GUIContent findByDropDown = new GUIContent
		{
			text = "Find by",
			tooltip = "Find points-of-interest by category, name, or address"
		};

		private GUIContent categoriesDropDown = new GUIContent
		{
			text = "Category",
			tooltip = "Spawn at locations in the categories selected"
		};

		private GUIContent  densitySlider = new GUIContent
		{
			text = "Density",
			tooltip = "The number of prefabs to spawn per-tile; try a lower number if the map is cluttered"
		};

		private GUIContent nameField = new GUIContent
		{
			text = "Name",
			tooltip = "Spawn at locations containing this name string"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.y += 1.2f*_lineHeight;

			var prefabItemCoreOptions = property.FindPropertyRelative("coreOptions");
			GUILayout.Label(prefabItemCoreOptions.FindPropertyRelative("sublayerName").stringValue + " Properties");
			GUILayout.Space(2.5f*EditorGUIUtility.singleLineHeight);

			//Prefab Game Object
			position.y += _lineHeight;
			var spawnPrefabOptions = property.FindPropertyRelative("spawnPrefabOptions");
			EditorGUI.PropertyField(new Rect(position.x, position.y + 2,position.width,2*_lineHeight),spawnPrefabOptions);
			GUILayout.Space(1);

			//Prefab Locations title
			GUILayout.Label(prefabLocationsTitle);

			//FindBy drop down
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(findByDropDown);
			var findByProp = property.FindPropertyRelative("findByType");
			GUILayout.Space(shiftLeftPixels);
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

			EditorGUI.EndProperty();
		}

		private void ShowCategoryOptions(SerializedProperty property)
		{
			//Category drop down
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(categoriesDropDown);

			var categoryProp = property.FindPropertyRelative("categories");
			GUILayout.Space(shiftLeftPixels);
			categoryProp.intValue = (int)(LocationPrefabCategories)(EditorGUILayout.EnumFlagsField((LocationPrefabCategories)categoryProp.intValue));
			EditorGUILayout.EndHorizontal();

			ShowDensitySlider(property);
		}

		private void ShowAddressOrLatLonUI(SerializedProperty property)
		{
			EditorGUILayout.BeginVertical();
			var coordinateProperties = property.FindPropertyRelative("coordinates");

			for (int i = 0; i < coordinateProperties.arraySize; i++)
			{
				EditorGUILayout.BeginHorizontal();
				//get the element to draw
				var coordinate = coordinateProperties.GetArrayElementAtIndex(i);

				//use the tagged property drawer for the coordinate layout
				EditorGUILayout.PropertyField(coordinate);

				//include a remove button in the row
				if (GUILayout.Button(new GUIContent(" X "), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
				{
					coordinateProperties.DeleteArrayElementAtIndex(i);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("       ");
			GUILayout.Space(-5);
			if(GUILayout.Button( "Add Location"))
			{
				
				coordinateProperties.arraySize++;
				var newElement = coordinateProperties.GetArrayElementAtIndex(coordinateProperties.arraySize - 1);
				newElement.stringValue = "";
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}


		private void ShowPOINames(SerializedProperty property)
		{
			//Name field
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(nameField);

			var categoryProp = property.FindPropertyRelative("nameString");
			GUILayout.Space(shiftLeftPixels);

			categoryProp.stringValue = EditorGUILayout.TextField(categoryProp.stringValue);
			EditorGUILayout.EndHorizontal();

			ShowDensitySlider(property);
		}

		private void ShowDensitySlider(SerializedProperty property)
		{
			//Density slider
			var densityProp = property.FindPropertyRelative("density");
			if (Application.isPlaying)
			{
				GUI.enabled = false;
			}

			EditorGUILayout.PropertyField(densityProp, densitySlider);
			GUI.enabled = true;
			densityProp.serializedObject.ApplyModifiedProperties();
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