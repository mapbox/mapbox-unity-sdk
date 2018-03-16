namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(UnityLayerOptions))]
	public class UnityLayerOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var addtoLayerProp = property.FindPropertyRelative("addToLayer");
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), addtoLayerProp, new GUIContent { text = "Add to Unity layer" });
			if (addtoLayerProp.boolValue == true)
			{
				EditorGUI.indentLevel++;
				var layerId = property.FindPropertyRelative("layerId");
				layerId.intValue = EditorGUILayout.LayerField("Layer", layerId.intValue);
				//EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerId"));
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return 1.0f * lineHeight;
		}
	}
}