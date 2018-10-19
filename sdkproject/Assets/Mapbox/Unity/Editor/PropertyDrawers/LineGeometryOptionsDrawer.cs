namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using System.Linq;
	using System;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(LineGeometryOptions))]
	public class LineGeometryOptionsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(property.FindPropertyRelative("Width"));

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
		}
	}
}
