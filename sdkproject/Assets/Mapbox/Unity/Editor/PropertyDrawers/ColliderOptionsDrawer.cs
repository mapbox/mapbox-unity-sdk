using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ColliderOptions))]
public class ColliderOptionsDrawer : PropertyDrawer
{

	static float lineHeight = EditorGUIUtility.singleLineHeight;
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		//logic to draw the property
	}
}
