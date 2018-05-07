namespace Mapbox.Unity.Map
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEditor;

	[CustomPropertyDrawer(typeof(SpawnPrefabOptions))]
	public class SpawnPrefabOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		private GUIContent prefabContent = new GUIContent
		{
			text = "Prefab",
			tooltip = "The prefab to be spawned"
		};

		private GUIContent scalePrefabContent = new GUIContent
		{
			text = "Scale down with world",
			tooltip = "The prefab will scale with the map object"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUILayout.PropertyField(property.FindPropertyRelative("prefab"), prefabContent);
			EditorGUILayout.PropertyField(property.FindPropertyRelative("scaleDownWithWorld"), scalePrefabContent);
		}

		//public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		//{
		//	return 0;
		//}
	}
}
