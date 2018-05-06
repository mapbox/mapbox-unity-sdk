namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
	public class GeometryExtrusionOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] extrusionTypeContent;
		bool isGUIContentSet = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var displayNames = extrusionTypeProperty.enumDisplayNames;
			int count = extrusionTypeProperty.enumDisplayNames.Length;

			if (!isGUIContentSet)
			{
				extrusionTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					extrusionTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ExtrusionType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			var extrusionTypeLabel = new GUIContent
			{
				text = "Extrusion Type",
				tooltip = "Type of geometry extrusion"
			};

			extrusionTypeProperty.enumValueIndex = EditorGUILayout.Popup(extrusionTypeLabel, extrusionTypeProperty.enumValueIndex, extrusionTypeContent);

			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			var minHeightProperty = property.FindPropertyRelative("minimumHeight");
			var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

			var extrusionGeometryType = property.FindPropertyRelative("extrusionGeometryType");
			var extrusionGeometryGUI = new GUIContent { text = "Extrusion Geometry Type", tooltip = EnumExtensions.Description((Unity.Map.ExtrusionGeometryType)extrusionGeometryType.enumValueIndex) };
			EditorGUI.indentLevel++;
			switch (sourceTypeValue)
			{
				case Unity.Map.ExtrusionType.None:
					break;
				case Unity.Map.ExtrusionType.PropertyHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.MinHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.MaxHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.RangeHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(property.FindPropertyRelative("propertyName"));
					EditorGUILayout.PropertyField(minHeightProperty);
					EditorGUILayout.PropertyField(maxHeightProperty);
					if (minHeightProperty.floatValue > maxHeightProperty.floatValue)
					{
						EditorGUILayout.HelpBox("Maximum Height less than Minimum Height!", MessageType.Error);
					}
					break;
				case Unity.Map.ExtrusionType.AbsoluteHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(maxHeightProperty, new GUIContent { text = "Height" });
					break;
				default:
					break;
			}
			EditorGUILayout.PropertyField(property.FindPropertyRelative("extrusionScaleFactor"), new GUIContent { text = "Scale Factor" });
			EditorGUI.indentLevel--;
		}
	}
}