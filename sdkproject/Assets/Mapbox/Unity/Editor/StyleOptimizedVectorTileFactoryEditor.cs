namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Editor.NodeEditor;

	[CustomEditor(typeof(StyleOptimizedVectorTileFactory))]
	public class StyleOptimizedVectorTileFactoryEditor : FactoryEditor
	{
		private string _defaultMapId = "mapbox.mapbox-streets-v7";
		private MonoScript script;
		private StyleOptimizedVectorTileFactory _factory;
		//SerializedProperty _visualizerList;
		public SerializedProperty mapId_Prop, style_Prop;

		private int ListSize;
		void OnEnable()
		{
			_factory = target as StyleOptimizedVectorTileFactory;
			//_visualizerList = serializedObject.FindProperty("Visualizers");
			mapId_Prop = serializedObject.FindProperty("_mapId");
			style_Prop = serializedObject.FindProperty("_optimizedStyle");
			script = MonoScript.FromScriptableObject(_factory);

			if (string.IsNullOrEmpty(mapId_Prop.stringValue))
			{
				mapId_Prop.stringValue = _defaultMapId;
				serializedObject.ApplyModifiedProperties();
				Repaint();
			}
		}

		public override void OnInspectorGUI()
		{
			if (_factory == null)
				return;

			serializedObject.Update();

			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

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

			EditorGUILayout.PropertyField(style_Prop, new GUIContent("Optimized Style"));

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Visualizers");
			var facs = serializedObject.FindProperty("Visualizers");
			for (int i = 0; i < facs.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				GUI.enabled = false;
				EditorGUILayout.BeginHorizontal();
				if (_factory.Visualizers[i] != null)
				{
					_factory.Visualizers[i].Key = EditorGUILayout.TextField(_factory.Visualizers[i].Key, GUILayout.MaxWidth(100));
				}
				//facs.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(i).objectReferenceValue, typeof(LayerVisualizerBase)) as ScriptableObject;
				if (_factory.Visualizers[i] == null)
					EditorGUILayout.TextField("null");
				else
					_factory.Visualizers[i] = (LayerVisualizerBase)EditorGUILayout.ObjectField(_factory.Visualizers[i], typeof(LayerVisualizerBase), false);
				EditorGUILayout.EndHorizontal();
				GUI.enabled = true;
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

			if (GUILayout.Button(new GUIContent("Add New")))
			{
				ScriptableCreatorWindow.Open(typeof(LayerVisualizerBase), facs);
			}
			EditorUtility.SetDirty(_factory);
			serializedObject.ApplyModifiedProperties();
		}
	}
}