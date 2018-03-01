namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;

	[CustomEditor(typeof(UnifiedMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditor : Editor
	{
		//public override void OnInspectorGUI()
		//{
		//	GUILayout.BeginVertical();
		//	EditorGUILayout.PropertyField(serializedObject.FindProperty("_mapOptions"), new GUIContent("Map Options"));

		//	GUILayout.EndVertical();
		//}

	}
}
