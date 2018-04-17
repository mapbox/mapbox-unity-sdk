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
			tooltip = "The prefab that will be spawned at the locations chosen below"
		};

		private GUIContent scalePrefabContent = new GUIContent
		{
			text = "Scale down with world",
			tooltip = "Turning on this option causes the prefab to scale down when you scale the map object"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = 2.5f * lineHeight;
			EditorGUI.PropertyField(new Rect(position.x,position.y,position.width,lineHeight),property.FindPropertyRelative("prefab"), prefabContent);
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width,lineHeight),property.FindPropertyRelative("scaleDownWithWorld"), scalePrefabContent);
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}
