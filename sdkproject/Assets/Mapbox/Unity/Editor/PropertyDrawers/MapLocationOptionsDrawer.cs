namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(MapLocationOptions))]
	public class MapLocationOptionsDrawer : PropertyDrawer
	{
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.indentLevel++;
			GUILayout.Space(-1f * _lineHeight);
			EditorGUILayout.PropertyField(property.FindPropertyRelative("latitudeLongitude"));
			EditorGUILayout.PropertyField(property.FindPropertyRelative("zoom"), GUILayout.Height(_lineHeight));
			EditorGUI.indentLevel--;
		}
	}
}