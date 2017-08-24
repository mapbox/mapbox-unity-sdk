using UnityEngine;
using System.Collections;
using UnityEditor;
using System.ComponentModel;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Filters;
using NodeEditorNamespace;

namespace Mapbox.NodeEditor
{
	[CustomEditor(typeof(VectorLayerVisualizer))]
	public class VectorLayerVisualizerEditor : UnityEditor.Editor
	{
		private VectorLayerVisualizer _layerVis;
		private MonoScript script;
		private SerializedProperty _classKeyProp;
		private SerializedProperty _keyProp;


		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((VectorLayerVisualizer)target);
			_layerVis = target as VectorLayerVisualizer;
			_classKeyProp = serializedObject.FindProperty("_classificationKey");
			_keyProp = serializedObject.FindProperty("_key");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			EditorGUILayout.PropertyField(_classKeyProp);
			EditorGUILayout.PropertyField(_keyProp);


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
					GUI.enabled = false;
					EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(ind).objectReferenceValue, typeof(FilterBase));
					GUI.enabled = true;
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

				if (GUILayout.Button(new GUIContent("Add New")))
				{
					ScriptableCreatorWindow.Open(typeof(FilterBase), facs);
				}
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
					GUI.enabled = false;
					EditorGUILayout.ObjectField(_layerVis._defaultStack, typeof(ModifierStackBase));
					GUI.enabled = true;
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

					if (_layerVis._stackValues[i] != null)
					{
						var nname = EditorGUILayout.TextField(_layerVis._stackKeys[i], GUILayout.MaxWidth(100));
						facs.GetArrayElementAtIndex(ind).FindPropertyRelative("Type").stringValue = nname;
					}

					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					GUI.enabled = false;
					EditorGUILayout.ObjectField(_layerVis._stackValues[i], typeof(ModifierStackBase));
					GUI.enabled = true;
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

				if (GUILayout.Button(new GUIContent("Add New")))
				{
					ScriptableCreatorWindow.Open(typeof(ModifierStackBase), facs, 0, (asset) =>
					{
						_layerVis.Stacks.Add(new TypeVisualizerTuple() { Stack = (ModifierStackBase)asset });
					});
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}