namespace Mapbox.Unity.Map
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEditor;
	using Mapbox.Editor;

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
			EditorGUI.BeginProperty(position, label, property);
			position.height = 2.5f * lineHeight;
			EditorGUI.BeginChangeCheck();
			property.FindPropertyRelative("prefab").objectReferenceValue = EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), prefabContent, property.FindPropertyRelative("prefab").objectReferenceValue, typeof(UnityEngine.GameObject), false);
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("scaleDownWithWorld"), scalePrefabContent);
			if(EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 2.0f * lineHeight;
		}
	}
}
