namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(CoreVectorLayerProperties))]
	public class CoreVectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			// Draw label.
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("isActive"));
			position.y += lineHeight;
			var primitiveType = property.FindPropertyRelative("geometryType");
			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Primitive Type", tooltip = "Primitive geometry type of the visualizer, allowed primitives - point, line, polygon." });

			primitiveType.enumValueIndex = EditorGUI.Popup(typePosition, primitiveType.enumValueIndex, primitiveType.enumDisplayNames);

			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerName"));

			position.y += lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("snapToTerrain"));

			position.y += lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("groupFeatures"));

			if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("lineWidth"));
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sourceTypeProperty = property.FindPropertyRelative("geometryType");

			float height = 0.0f;
			height += (((((VectorPrimitiveType)sourceTypeProperty.enumValueIndex == VectorPrimitiveType.Line)) ? 6.0f : 5.0f) * EditorGUIUtility.singleLineHeight);

			return height;
		}
	}
}