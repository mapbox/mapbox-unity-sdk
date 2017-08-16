using UnityEngine;
using System.Collections;
using UnityEditor;
using System.ComponentModel;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.MeshGeneration.Factories;

namespace Mapbox.NodeEditor
{
	[CustomEditor(typeof(MapVisualizer))]
	public class MapVisualizerEditor : UnityEditor.Editor
	{
		private MonoScript script;

		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((MapVisualizer)target);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Factories");
			var facs = serializedObject.FindProperty("_factories");
			for (int i = 0; i < facs.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				GUI.enabled = false;
				facs.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(i).objectReferenceValue, typeof(AbstractTileFactory)) as ScriptableObject;
				GUI.enabled = true;

				if (GUILayout.Button(new GUIContent("E"), GUILayout.Width(20)))
				{
					Mapbox.NodeEditor.ScriptableCreatorWindow.Open(typeof(AbstractTileFactory), facs, ind);
				}
				if (GUILayout.Button(new GUIContent("-"), GUILayout.Width(20)))
				{
					facs.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button(new GUIContent("Add New")))
			{
				Mapbox.NodeEditor.ScriptableCreatorWindow.Open(typeof(AbstractTileFactory), facs);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}