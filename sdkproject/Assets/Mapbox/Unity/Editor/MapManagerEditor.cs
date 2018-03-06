namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomEditor(typeof(AbstractMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditor : Editor
	{
		bool showGeneral = true;
		bool showImage = false;
		bool showTerrain = false;
		bool showVector = false;
		static int selected = 2;
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var property = serializedObject.FindProperty("_unifiedMapOptions");
			GUILayout.BeginVertical();
			EditorGUILayout.Space();

			showGeneral = EditorGUILayout.Foldout(showGeneral, new GUIContent { text = "GENERAL", tooltip = "Options related to map data" });
			if (showGeneral)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Presets");
				selected = property.FindPropertyRelative("mapPreset").enumValueIndex;
				var options = property.FindPropertyRelative("mapPreset").enumDisplayNames;

				GUIContent[] content = new GUIContent[options.Length];
				for (int i = 0; i < options.Length; i++)
				{
					content[i] = new GUIContent();
					content[i].text = options[i];
					content[i].tooltip = EnumExtensions.Description((MapPresetType)i);
				}
				property.FindPropertyRelative("mapPreset").enumValueIndex = GUILayout.SelectionGrid(selected, content, options.Length);
				EditorGUILayout.Space();

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
