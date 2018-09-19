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
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("latitudeLongitude"));
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("zoom"), GUILayout.Height(_lineHeight));
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
			EditorGUI.indentLevel--;
		}
	}
}