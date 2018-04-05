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
		GUIContent[] sourceTypeContent;
		bool isGUIContentSet = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");

			var displayNames = extrusionTypeProperty.enumDisplayNames;
			int count = extrusionTypeProperty.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				sourceTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					sourceTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ExtrusionType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Extrusion Type", tooltip = "Type of geometry extrusion" });


			EditorGUI.indentLevel--;
			extrusionTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, extrusionTypeProperty.enumValueIndex, sourceTypeContent);
			EditorGUI.indentLevel++;
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
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.MinHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.MaxHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.RangeHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
					if (minHeightProperty.floatValue > maxHeightProperty.floatValue)
					{
						//position.y += lineHeight;
						EditorGUILayout.HelpBox("Maximum Height less than Minimum Height!", MessageType.Error);
					}
					break;
				case Unity.Map.ExtrusionType.AbsoluteHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty, new GUIContent { text = "Height" });
					break;
				default:
					break;
			}
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionScaleFactor"), new GUIContent { text = "Scale Factor" });
			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			int rows = 1;
			//if (showPosition)
			{
				switch (sourceTypeValue)
				{
					case Unity.Map.ExtrusionType.None:
						rows += 1;
						break;
					case Unity.Map.ExtrusionType.PropertyHeight:
					case Unity.Map.ExtrusionType.MinHeight:
					case Unity.Map.ExtrusionType.MaxHeight:
						rows += 3;
						break;
					case Unity.Map.ExtrusionType.RangeHeight:
						rows += 5;
						break;
					case Unity.Map.ExtrusionType.AbsoluteHeight:
						rows += 3;
						break;
					default:
						rows += 2;
						break;
				}
			}
			return (float)rows * lineHeight;
		}
	}
}