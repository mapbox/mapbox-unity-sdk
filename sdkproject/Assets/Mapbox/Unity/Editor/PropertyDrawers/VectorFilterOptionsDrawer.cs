namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Filters;
	using System.Linq;
	using System.Collections.Generic;

	[CustomPropertyDrawer(typeof(VectorFilterOptions))]
	public class VectorFilterOptionsDrawer : PropertyDrawer
	{
		//indices for tileJSON lookup
		int _propertyIndex = 0;

		private static string objectId = "";
		private string[] descriptionArray;
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showFilters = true;
		static bool _isInitialized = false;
		static string cachedLayerName = "";
		static bool dataUnavailable = false;

		GUIContent operatorGui = new GUIContent { text = "Operator", tooltip = "Filter operator to apply. " };
		GUIContent numValueGui = new GUIContent { text = "Num Value", tooltip = "Numeric value to match using the operator.  " };
		GUIContent strValueGui = new GUIContent { text = "Str Value", tooltip = "String value to match using the operator.  " };
		GUIContent minValueGui = new GUIContent { text = "Min", tooltip = "Minimum numeric value to match using the operator.  " };
		GUIContent maxValueGui = new GUIContent { text = "Max", tooltip = "Maximum numeric value to match using the operator.  " };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

			showFilters = EditorGUILayout.Foldout(showFilters, new GUIContent { text = "Filters", tooltip = "Filter features in a vector layer based on criterion specified.  " });
			if (showFilters)
			{
				var propertyFilters = property.FindPropertyRelative("filters");

				for (int i = 0; i < propertyFilters.arraySize; i++)
				{
					DrawLayerFilter(property, propertyFilters, i);
				}
				if (propertyFilters.arraySize > 0)
				{
					EditorGUILayout.PropertyField(property.FindPropertyRelative("combinerType"));
				}
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUI.indentLevel * 12);
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibutton"))
				{
					propertyFilters.arraySize++;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}

		void DrawLayerFilter(SerializedProperty originalProperty, SerializedProperty propertyFilters, int index)
		{
			var property = propertyFilters.GetArrayElementAtIndex(index);
			var filterOperatorProp = property.FindPropertyRelative("filterOperator");

			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(new GUIContent { text = "Key", tooltip = "Name of the property to use as key. This property is case sensitive." }, GUILayout.MaxWidth(150));


			switch ((LayerFilterOperationType)filterOperatorProp.enumValueIndex)
			{
				case LayerFilterOperationType.IsEqual:
				case LayerFilterOperationType.IsGreater:
				case LayerFilterOperationType.IsLess:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(numValueGui, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.Contains:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(strValueGui, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.IsInRange:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(minValueGui, GUILayout.MaxWidth(100));
					EditorGUILayout.LabelField(maxValueGui, GUILayout.MaxWidth(100));
					break;
				default:
					break;
			}

			EditorGUILayout.EndHorizontal();


			EditorGUILayout.BeginHorizontal();
			var selectedLayerName = originalProperty.FindPropertyRelative("_selectedLayerName").stringValue;

			DrawPropertyDropDown(originalProperty, property);
			filterOperatorProp.enumValueIndex = EditorGUILayout.Popup(filterOperatorProp.enumValueIndex, filterOperatorProp.enumDisplayNames, GUILayout.MaxWidth(150));

			switch ((LayerFilterOperationType)filterOperatorProp.enumValueIndex)
			{
				case LayerFilterOperationType.IsEqual:
				case LayerFilterOperationType.IsGreater:
				case LayerFilterOperationType.IsLess:
					property.FindPropertyRelative("Min").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Min").doubleValue, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.Contains:
					property.FindPropertyRelative("PropertyValue").stringValue = EditorGUILayout.TextField(property.FindPropertyRelative("PropertyValue").stringValue, GUILayout.MaxWidth(150));
					break;
				case LayerFilterOperationType.IsInRange:
					property.FindPropertyRelative("Min").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Min").doubleValue, GUILayout.MaxWidth(100));
					property.FindPropertyRelative("Max").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Max").doubleValue, GUILayout.MaxWidth(100));
					break;
				default:
					break;
			}
			if (GUILayout.Button(new GUIContent(" X "), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
			{
				propertyFilters.DeleteArrayElementAtIndex(index);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

		}

		private void DrawPropertyDropDown(SerializedProperty originalProperty, SerializedProperty filterProperty)
		{
			var selectedLayerName = originalProperty.FindPropertyRelative("_selectedLayerName").stringValue;
			AbstractMap mapObject = (AbstractMap)originalProperty.serializedObject.targetObject;
			TileJsonData tileJsonData = mapObject.VectorData.LayerProperty.tileJsonData;

			if (string.IsNullOrEmpty(selectedLayerName) || !tileJsonData.PropertyDisplayNames.ContainsKey(selectedLayerName))
			{
				DrawWarningMessage();
				return;
			}

			dataUnavailable = false;
			var propertyDisplayNames = tileJsonData.PropertyDisplayNames[selectedLayerName];

			descriptionArray = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName].Values.ToArray<string>();
			GUIContent[] properties = new GUIContent[propertyDisplayNames.Count];
			for (int i = 0; i < propertyDisplayNames.Count; i++)
			{
				properties[i] = new GUIContent(propertyDisplayNames[i], descriptionArray[i]);
			}

			_propertyIndex = EditorGUILayout.Popup(_propertyIndex, properties, GUILayout.MaxWidth(150));
			var parsedString = propertyDisplayNames[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
			filterProperty.FindPropertyRelative("Key").stringValue = parsedString;
		}

		private void DrawWarningMessage()
		{
			dataUnavailable = true;
			GUIStyle labelStyle = new GUIStyle(EditorStyles.popup);
			labelStyle.fontStyle = FontStyle.Bold;
			EditorGUILayout.LabelField(new GUIContent(), new GUIContent("Check MapId / Internet."), labelStyle, new GUILayoutOption[] { GUILayout.MaxWidth(155) });//(GUIStyle)"minipopUp");
			return;
		}
	}
}
