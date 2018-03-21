namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Filters;

	[CustomPropertyDrawer(typeof(VectorFilterOptions))]
	public class VectorFilterOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showFilters = true;

		GUIContent operatorGui = new GUIContent { text = "Operator", tooltip = "Filter operator to apply. " };
		GUIContent numValueGui = new GUIContent { text = "Num Value", tooltip = "Numeric value to match using the operator.  " };
		GUIContent strValueGui = new GUIContent { text = "Str Value", tooltip = "String value to match using the operator.  " };
		GUIContent minValueGui = new GUIContent { text = "Min", tooltip = "Minimum numeric value to match using the operator.  " };
		GUIContent maxValueGui = new GUIContent { text = "Max", tooltip = "Maximum numeric value to match using the operator.  " };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			showFilters = EditorGUI.Foldout(position, showFilters, new GUIContent { text = "Filters", tooltip = "Filter features in a vector layer based on criterion specified.  " });
			if (showFilters)
			{
				var propertyFilters = property.FindPropertyRelative("filters");

				for (int i = 0; i < propertyFilters.arraySize; i++)
				{
					DrawLayerFilter(propertyFilters, i);
				}
				if (propertyFilters.arraySize > 0)
				{
					EditorGUILayout.PropertyField(property.FindPropertyRelative("combinerType"));
				}

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibutton"))
				{
					propertyFilters.arraySize++;
					//propertyFilters.GetArrayElementAtIndex(propertyFilters.arraySize - 1) = null;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}

		void DrawLayerFilter(SerializedProperty propertyFilters, int index)
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
			property.FindPropertyRelative("Key").stringValue = EditorGUILayout.TextField(property.FindPropertyRelative("Key").stringValue, GUILayout.MaxWidth(150));
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
	}

	//[CustomPropertyDrawer(typeof(TypeVisualizerTuple))]
	//public class TypeVisualizerBaseDrawer : PropertyDrawer
	//{
	//	static float lineHeight = EditorGUIUtility.singleLineHeight;
	//	bool showPosition = true;
	//	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	//	{
	//		EditorGUI.BeginProperty(position, label, property);

	//		position.height = lineHeight;

	//		EditorGUI.PropertyField(position, property.FindPropertyRelative("Stack"));

	//		EditorGUI.EndProperty();
	//	}
	//	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	//	{
	//		// Reserve space for the total visible properties.
	//		int rows = 2;
	//		//Debug.Log("Height - " + rows * lineHeight);
	//		return (float)rows * lineHeight;
	//	}
	//}

}