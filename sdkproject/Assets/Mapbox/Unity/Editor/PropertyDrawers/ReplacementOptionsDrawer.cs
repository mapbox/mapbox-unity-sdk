using UnityEditor;
using UnityEngine;

namespace Mapbox.Unity.Map
{
	[CustomPropertyDrawer(typeof(ReplacementOptions))]
	public class ReplacementOptionsDrawer : PropertyDrawer
	{
		static float _lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent prefabItemLabel = new GUIContent("Replacement Section");

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			//EditorGUILayout.PropertyField(property.FindPropertyRelative("prefabItemOptions"),prefabItemLabel);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return _lineHeight;
		}
	}
}
