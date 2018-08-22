namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEngine;
	using UnityEditor;
	/*
	[CustomPropertyDrawer(typeof(FeatureBundleList))]
	public class FeatureBundleListDrawer : PropertyDrawer
	{
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var features = property.FindPropertyRelative("features");
			for (int i = 0; i < features.arraySize; i++)
			{
				var feature = features.GetArrayElementAtIndex(i);
				var name = feature.FindPropertyRelative("features");
				//EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
			}
		}

	}
	*/
}