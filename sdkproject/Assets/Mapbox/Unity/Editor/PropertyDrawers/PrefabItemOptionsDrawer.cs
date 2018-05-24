namespace Mapbox.Editor
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEditor;
	using System;
	using System.Collections.Generic;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(PrefabItemOptions))]
	public class PrefabItemOptionsDrawer : PropertyDrawer
	{

		static float _lineHeight = EditorGUIUtility.singleLineHeight;
		const string searchButtonContent = "Search";
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

		private GUIContent densitySlider = new GUIContent
		{
			text = "Density",
			tooltip = "The number of prefabs to spawn per-tile; try a lower number if the map is cluttered"
		};

		private GUIContent nameField = new GUIContent
		{
			text = "Name",
			tooltip = "Spawn at locations containing this name string"
		};

		GUIContent[] findByPropContent;
		bool isGUIContentSet = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUILayout.Space(-_lineHeight);
			var prefabItemCoreOptions = property.FindPropertyRelative("coreOptions");
			GUILayout.Label(prefabItemCoreOptions.FindPropertyRelative("sublayerName").stringValue + " Properties");

			//Prefab Game Object
			EditorGUI.indentLevel++;
			var spawnPrefabOptions = property.FindPropertyRelative("spawnPrefabOptions");
			EditorGUILayout.PropertyField(spawnPrefabOptions);
			GUILayout.Space(1);
			EditorGUI.indentLevel--;

			//Prefab Locations title
			GUILayout.Label(prefabLocationsTitle);

			//FindBy drop down
			EditorGUILayout.BeginHorizontal();

			var findByProp = property.FindPropertyRelative("findByType");

			var displayNames = findByProp.enumDisplayNames;
			int count = findByProp.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				findByPropContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					findByPropContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = ((LocationPrefabFindBy)extIdx).Description(),
					};
				}
				isGUIContentSet = true;
			}

			EditorGUI.indentLevel++;

			findByProp.enumValueIndex = EditorGUILayout.Popup(findByDropDown, findByProp.enumValueIndex, findByPropContent);

			EditorGUILayout.EndHorizontal();

			switch ((LocationPrefabFindBy)findByProp.enumValueIndex)
			{
				case (LocationPrefabFindBy.MapboxCategory):
					ShowCategoryOptions(property);
					break;
				case (LocationPrefabFindBy.AddressOrLatLon):
					ShowAddressOrLatLonUI(property);
					break;
				case (LocationPrefabFindBy.POIName):
					ShowPOINames(property);
					break;
				default:
					break;
			}
			EditorGUI.indentLevel--;
		}

		private void ShowCategoryOptions(SerializedProperty property)
		{
			//Category drop down
			var categoryProp = property.FindPropertyRelative("categories");
			categoryProp.intValue = (int)(LocationPrefabCategories)(EditorGUILayout.EnumFlagsField(categoriesDropDown, (LocationPrefabCategories)categoryProp.intValue));
			ShowDensitySlider(property);
		}

		private void ShowAddressOrLatLonUI(SerializedProperty property)
		{
			//EditorGUILayout.BeginVertical();
			var coordinateProperties = property.FindPropertyRelative("coordinates");

			for (int i = 0; i < coordinateProperties.arraySize; i++)
			{
				EditorGUILayout.BeginHorizontal();
				//get the element to draw
				var coordinate = coordinateProperties.GetArrayElementAtIndex(i);

				//label for each location.
				var coordinateLabel = String.Format("Location {0}", i);

				// draw coordinate string.
				coordinate.stringValue = EditorGUILayout.TextField(coordinateLabel, coordinate.stringValue);

				// draw search button.
				if (GUILayout.Button(new GUIContent(searchButtonContent), (GUIStyle)"minibuttonleft", GUILayout.MaxWidth(100)))
				{
					GeocodeAttributeSearchWindow.Open(coordinate);
				}

				//include a remove button in the row
				if (GUILayout.Button(new GUIContent(" X "), (GUIStyle)"minibuttonright", GUILayout.MaxWidth(30)))
				{
					coordinateProperties.DeleteArrayElementAtIndex(i);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUIUtility.labelWidth - 3);

			if (GUILayout.Button(new GUIContent("Add Location"), (GUIStyle)"minibutton"))
			{
				coordinateProperties.arraySize++;
				var newElement = coordinateProperties.GetArrayElementAtIndex(coordinateProperties.arraySize - 1);
				newElement.stringValue = "";
			}
			EditorGUILayout.EndHorizontal();
		}


		private void ShowPOINames(SerializedProperty property)
		{
			//Name field
			var categoryProp = property.FindPropertyRelative("nameString");

			categoryProp.stringValue = EditorGUILayout.TextField(nameField, categoryProp.stringValue);

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
