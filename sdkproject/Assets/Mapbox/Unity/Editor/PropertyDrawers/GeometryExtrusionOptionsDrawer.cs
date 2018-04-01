namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
	public class GeometryExtrusionOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Extrusion Type"));
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");

			extrusionTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, extrusionTypeProperty.enumValueIndex, extrusionTypeProperty.enumDisplayNames);
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			var minHeightProperty = property.FindPropertyRelative("minimumHeight");
			var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

			EditorGUI.indentLevel++;
			switch (sourceTypeValue)
			{
				case Unity.Map.ExtrusionType.None:
					break;
				case Unity.Map.ExtrusionType.PropertyHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					break;
				case Unity.Map.ExtrusionType.MinHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
					//maxHeightProperty.floatValue = minHeightProperty.floatValue;
					break;
				case Unity.Map.ExtrusionType.MaxHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
					//min.floatValue = minHeightProperty.floatValue;
					break;
				case Unity.Map.ExtrusionType.RangeHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
					break;
				case Unity.Map.ExtrusionType.AbsoluteHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty, new GUIContent { text = "Height" });
					break;
				default:
					break;
			}
			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			int rows = 0;
			//if (showPosition)
			{
				switch (sourceTypeValue)
				{
					case Unity.Map.ExtrusionType.None:
						rows += 1;
						break;
					case Unity.Map.ExtrusionType.PropertyHeight:
						rows += 3;
						break;
					case Unity.Map.ExtrusionType.MinHeight:
					case Unity.Map.ExtrusionType.MaxHeight:
						rows += 4;
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