namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Editor.NodeEditor;

	[CustomEditor(typeof(VectorTileFactory))]
	public class VectorTileFactoryEditor : UnityEditor.Editor
	{
		private string _defaultMapId = "mapbox.mapbox-streets-v7";
		public SerializedProperty mapId_Prop;
		private VectorTileFactory _factory;
		private MonoScript script;

		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((VectorTileFactory)target);
			mapId_Prop = serializedObject.FindProperty("_mapId");
			_factory = target as VectorTileFactory;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			EditorGUILayout.PropertyField(serializedObject.FindProperty("State"));
			GUI.enabled = true;
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(mapId_Prop, new GUIContent("Map Id"));
			if (GUILayout.Button("R", GUILayout.Width(30)))
			{
				mapId_Prop.stringValue = _defaultMapId;
				GUI.FocusControl(null);
				Repaint();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Visualizers");
			var facs = serializedObject.FindProperty("Visualizers");
			for (int i = 0; i < facs.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				EditorGUILayout.BeginHorizontal();
				if (_factory.Visualizers[i] != null)
				{
					var obj = new SerializedObject(facs.GetArrayElementAtIndex(ind).objectReferenceValue);
					_factory.Visualizers[i].Active = EditorGUILayout.Toggle(_factory.Visualizers[i].Active, GUILayout.MaxWidth(20));
					obj.FindProperty("_key").stringValue = EditorGUILayout.TextField(obj.FindProperty("_key").stringValue, GUILayout.MaxWidth(100));
					obj.ApplyModifiedProperties();
				}
				if (_factory.Visualizers[i] == null)
					EditorGUILayout.TextField("null");
				else
					_factory.Visualizers[i] = (LayerVisualizerBase)EditorGUILayout.ObjectField(_factory.Visualizers[i], typeof(LayerVisualizerBase), false);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();

				if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
				{
					ScriptableCreatorWindow.Open(typeof(LayerVisualizerBase), facs, ind);
				}
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					facs.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
			{
				facs.arraySize++;
				facs.GetArrayElementAtIndex(facs.arraySize - 1).objectReferenceValue = null;
			}
			if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
			{
				ScriptableCreatorWindow.Open(typeof(LayerVisualizerBase), facs);
			}
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}
	}
}