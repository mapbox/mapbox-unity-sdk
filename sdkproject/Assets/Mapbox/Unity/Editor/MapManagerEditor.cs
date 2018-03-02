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
		bool showGeneral = true;
		bool showImage = false;
		bool showTerrain = false;
		bool showVector = false;
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var property = serializedObject.FindProperty("_unifiedMapOptions");
			GUILayout.BeginVertical();
			EditorGUILayout.Space();

			showGeneral = EditorGUILayout.Foldout(showGeneral, "GENERAL");
			if (showGeneral)
			{
				ShowSection(property, "mapOptions");
			}

			ShowSepartor();

			showImage = EditorGUILayout.Foldout(showImage, "IMAGE");
			if (showImage)
			{
				ShowSection(property, "imageryLayerProperties");
			}

			ShowSepartor();

			showTerrain = EditorGUILayout.Foldout(showTerrain, "TERRAIN");
			if (showTerrain)
			{
				ShowSection(property, "elevationLayerProperties");
			}

			ShowSepartor();

			showVector = EditorGUILayout.Foldout(showVector, "VECTOR");
			if (showVector)
			{
				ShowSection(property, "vectorLayerProperties");
			}
			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}

		void ShowSection(SerializedProperty property, string propertyName)
		{
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(property.FindPropertyRelative(propertyName));

		}
		void ShowSepartor()
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.Space();
		}
	}
}
