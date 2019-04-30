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
		List<string> _propertyNamesList = new List<string>();
		GUIContent[] _propertyNameContent;

		private string[] descriptionArray;
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		private string objectId = "";
		bool showFilters
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showFilters");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showFilters", value);
			}
		}

		GUIContent operatorGui = new GUIContent { text = "Operator", tooltip = "Filter operator to apply. " };
		GUIContent numValueGui = new GUIContent { text = "Num Value", tooltip = "Numeric value to match using the operator.  " };
		GUIContent strValueGui = new GUIContent { text = "Str Value", tooltip = "String value to match using the operator.  " };
		GUIContent minValueGui = new GUIContent { text = "Min", tooltip = "Minimum numeric value to match using the operator.  " };
		GUIContent maxValueGui = new GUIContent { text = "Max", tooltip = "Maximum numeric value to match using the operator.  " };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			VectorFilterOptions options = (VectorFilterOptions)EditorHelper.GetTargetObjectOfProperty(property);

			showFilters = EditorGUILayout.Foldout(showFilters, new GUIContent { text = "Filters", tooltip = "Filter features in a vector layer based on criterion specified.  " });
			if (showFilters)
			{
				var propertyFilters = property.FindPropertyRelative("filters");

				for (int i = 0; i < propertyFilters.arraySize; i++)
				{
					DrawLayerFilter(property, propertyFilters, i, options);
				}
				if (propertyFilters.arraySize > 0)
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(property.FindPropertyRelative("combinerType"));
					if (EditorGUI.EndChangeCheck())
					{
						EditorHelper.CheckForModifiedProperty(property);
					}
				}
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUI.indentLevel * 12);

				EditorGUI.BeginChangeCheck();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibutton"))
				{
					options.AddFilter();
				}
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(property);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}

		private void DrawLayerFilter(SerializedProperty originalProperty, SerializedProperty propertyFilters, int index, VectorFilterOptions vectorFilterOptions)
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

			EditorGUI.BeginChangeCheck();
			filterOperatorProp.enumValueIndex = EditorGUILayout.Popup(filterOperatorProp.enumValueIndex, filterOperatorProp.enumDisplayNames, GUILayout.MaxWidth(150));
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			EditorGUI.BeginChangeCheck();
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
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			if (GUILayout.Button(new GUIContent(" X "), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
			{
				vectorFilterOptions.RemoveFilter(index);
				propertyFilters.DeleteArrayElementAtIndex(index);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		private void DrawPropertyDropDown(SerializedProperty originalProperty, SerializedProperty filterProperty)
		{

			var selectedLayerName = originalProperty.FindPropertyRelative("_selectedLayerName").stringValue;
			AbstractMap mapObject = (AbstractMap)originalProperty.serializedObject.targetObject;
			TileJsonData tileJsonData = mapObject.VectorData.GetTileJsonData();

			if (string.IsNullOrEmpty(selectedLayerName) || !tileJsonData.PropertyDisplayNames.ContainsKey(selectedLayerName))
			{
				DrawWarningMessage();
				return;
			}

			var parsedString = "no property selected";
			var descriptionString = "no description available";
			var propertyDisplayNames = tileJsonData.PropertyDisplayNames[selectedLayerName];
			_propertyNamesList = new List<string>(propertyDisplayNames);

			var propertyString = filterProperty.FindPropertyRelative("Key").stringValue;
			//check if the selection is valid
			if (_propertyNamesList.Contains(propertyString))
			{
				//if the layer contains the current layerstring, set it's index to match
				_propertyIndex = propertyDisplayNames.FindIndex(s => s.Equals(propertyString));

				//create guicontent for a valid layer
				_propertyNameContent = new GUIContent[_propertyNamesList.Count];
				for (int extIdx = 0; extIdx < _propertyNamesList.Count; extIdx++)
				{
					var parsedPropertyString = _propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					_propertyNameContent[extIdx] = new GUIContent
					{
						text = _propertyNamesList[extIdx],
						tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
					};
				}

				//display popup
				EditorGUI.BeginChangeCheck();
				_propertyIndex = EditorGUILayout.Popup(_propertyIndex, _propertyNameContent, GUILayout.MaxWidth(150));
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(filterProperty);
				}

				//set new string values based on selection
				parsedString = _propertyNamesList[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
				descriptionString = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedString];

			}
			else
			{
				//if the selected layer isn't in the source, add a placeholder entry
				_propertyIndex = 0;
				_propertyNamesList.Insert(0, propertyString);

				//create guicontent for an invalid layer
				_propertyNameContent = new GUIContent[_propertyNamesList.Count];

				//first property gets a unique tooltip
				_propertyNameContent[0] = new GUIContent
				{
					text = _propertyNamesList[0],
					tooltip = "Unavialable in Selected Layer"
				};

				for (int extIdx = 1; extIdx < _propertyNamesList.Count; extIdx++)
				{
					var parsedPropertyString = _propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					_propertyNameContent[extIdx] = new GUIContent
					{
						text = _propertyNamesList[extIdx],
						tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
					};
				}

				//display popup
				EditorGUI.BeginChangeCheck();
				_propertyIndex = EditorGUILayout.Popup(_propertyIndex, _propertyNameContent, GUILayout.MaxWidth(150));
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(filterProperty);
				}

				//set new string values based on the offset
				parsedString = _propertyNamesList[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
				descriptionString = "Unavailable in Selected Layer.";

			}
			EditorGUI.BeginChangeCheck();
			filterProperty.FindPropertyRelative("Key").stringValue = parsedString;
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(filterProperty);
			}
			filterProperty.FindPropertyRelative("KeyDescription").stringValue = descriptionString;
		}

		private void DrawWarningMessage()
		{
			GUIStyle labelStyle = new GUIStyle(EditorStyles.popup);
			labelStyle.fontStyle = FontStyle.Bold;
			EditorGUILayout.LabelField(new GUIContent(), new GUIContent("No properties"), labelStyle, new GUILayoutOption[] { GUILayout.MaxWidth(155) });
			return;
		}
	}
}
