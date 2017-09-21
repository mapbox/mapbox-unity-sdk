namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Editor.NodeEditor;
	using Mapbox.Unity.Map;

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

			var texture = serializedObject.FindProperty("_loadingTexture");
			EditorGUILayout.ObjectField(texture, typeof(Texture2D));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Factories");
			var facs = serializedObject.FindProperty("_factories");
			for (int i = 0; i < facs.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				GUI.enabled = false;
				facs.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(i).objectReferenceValue, typeof(AbstractTileFactory), false) as ScriptableObject;
				GUI.enabled = true;
				EditorGUILayout.EndVertical();

				if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
				{
					ScriptableCreatorWindow.Open(typeof(AbstractTileFactory), facs, ind);
				}
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					facs.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button(new GUIContent("Add New")))
			{
				ScriptableCreatorWindow.Open(typeof(AbstractTileFactory), facs);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}