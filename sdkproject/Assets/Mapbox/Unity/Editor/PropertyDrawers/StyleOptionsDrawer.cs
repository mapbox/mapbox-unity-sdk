namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(Style))]
	public class StyleOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("Id"), new GUIContent { text = "Map Id", tooltip = "Map Id corresponding to the tileset." });

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return lineHeight;
		}
	}
}