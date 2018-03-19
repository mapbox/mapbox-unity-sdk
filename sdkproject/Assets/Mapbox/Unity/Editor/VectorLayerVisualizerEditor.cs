namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Editor.NodeEditor;

	[CustomEditor(typeof(VectorLayerVisualizer))]
	public class VectorLayerVisualizerEditor : Editor
	{
		private VectorLayerVisualizer _layerVis;
		private MonoScript script;
		private SerializedProperty _classKeyProp;
		private SerializedProperty _keyProp;
		private SerializedProperty _useCoroutines;
		private SerializedProperty _entityPerCoroutine;


		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((VectorLayerVisualizer)target);
			_layerVis = target as VectorLayerVisualizer;
			_classKeyProp = serializedObject.FindProperty("_classificationKey");
			_keyProp = serializedObject.FindProperty("_key");
			_useCoroutines = serializedObject.FindProperty("_enableCoroutines");
			_entityPerCoroutine = serializedObject.FindProperty("_entityPerCoroutine");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			EditorGUILayout.PropertyField(_classKeyProp);
			EditorGUILayout.PropertyField(_keyProp);
			EditorGUILayout.PropertyField(_useCoroutines);
			if(_useCoroutines.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_entityPerCoroutine);
				EditorGUI.indentLevel--;
			}

			//FILTERS
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Filters");
				var facs = serializedObject.FindProperty("Filters");
				for (int i = 0; i < facs.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					facs.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(ind).objectReferenceValue, typeof(FilterBase), false);
					EditorGUILayout.EndVertical();

					if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(FilterBase), facs, ind);
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
					ScriptableCreatorWindow.Open(typeof(FilterBase), facs);
				}
				EditorGUILayout.EndHorizontal();
			}

			//DEFAULT STACK
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Default Stack");
				var def = serializedObject.FindProperty("_defaultStack");
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					_layerVis._defaultStack = (ModifierStackBase)EditorGUILayout.ObjectField(_layerVis._defaultStack, typeof(ModifierStackBase), false);
					EditorGUILayout.EndVertical();

					if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(ModifierStackBase), null, 0, (asset) =>
						{
							def.objectReferenceValue = asset;
							serializedObject.ApplyModifiedProperties();
						});
					}
					if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
					{
						def.objectReferenceValue = null;
						serializedObject.ApplyModifiedProperties();
					}
					EditorGUILayout.EndHorizontal();
				}
			}


			//STACKS
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Stacks");
				var facs = serializedObject.FindProperty("Stacks");
				for (int i = 0; i < facs.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();
					if (_layerVis.Stacks != null)
					{
						var nname = EditorGUILayout.TextField(_layerVis.Stacks[i].Type, GUILayout.MaxWidth(100));
						facs.GetArrayElementAtIndex(ind).FindPropertyRelative("Type").stringValue = nname;
					}

					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					_layerVis.Stacks[i].Stack = (ModifierStackBase)EditorGUILayout.ObjectField(_layerVis.Stacks[i].Stack, typeof(ModifierStackBase), true);
					EditorGUILayout.EndVertical();


					if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(ModifierStackBase), facs, ind, (asset) =>
						{
							var pp = facs.GetArrayElementAtIndex(ind).FindPropertyRelative("Stack");
							pp.objectReferenceValue = asset;

							serializedObject.ApplyModifiedProperties();
						});
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
				}
				if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
				{
					ScriptableCreatorWindow.Open(typeof(ModifierStackBase), facs, 0, (asset) =>
					{
						_layerVis.Stacks.Add(new TypeVisualizerTuple() { Stack = (ModifierStackBase)asset });
					});
				}
				EditorGUILayout.EndHorizontal();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}