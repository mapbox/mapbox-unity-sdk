namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(MapLocationOptions))]
	public class MapLocationOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				EditorGUI.PropertyField(position, subproperty, true);
				position.height = lineHeight;
				position.y += lineHeight;
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return (2.0f * lineHeight);
		}
	}
}