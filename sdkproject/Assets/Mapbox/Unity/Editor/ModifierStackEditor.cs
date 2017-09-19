﻿namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Editor.NodeEditor;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CustomEditor(typeof(ModifierStack))]
	public class ModifierStackEditor : UnityEditor.Editor
	{

		private MonoScript script;
		private SerializedProperty _positionType;
		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((ModifierStack)target);
			_positionType = serializedObject.FindProperty("_moveFeaturePositionTo");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_positionType, new GUIContent("Feature Position"));


			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Mesh Modifiers");
			var facs = serializedObject.FindProperty("MeshModifiers");
			for (int i = 0; i < facs.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				GUI.enabled = false;
				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				facs.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs.GetArrayElementAtIndex(i).objectReferenceValue, typeof(MeshModifier)) as ScriptableObject;
				GUI.enabled = true;
				EditorGUILayout.EndVertical();
				if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
				{
					ScriptableCreatorWindow.Open(typeof(MeshModifier), facs, ind);
				}
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					facs.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button(new GUIContent("Add New")))
			{
				ScriptableCreatorWindow.Open(typeof(MeshModifier), facs);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Game Object Modifiers");
			var facs2 = serializedObject.FindProperty("GoModifiers");
			for (int i = 0; i < facs2.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				GUI.enabled = false;
				facs2.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(facs2.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier)) as ScriptableObject;
				GUI.enabled = true;
				EditorGUILayout.EndVertical();

				if (GUILayout.Button(NodeBasedEditor.magnifierTexture, (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
				{
					ScriptableCreatorWindow.Open(typeof(GameObjectModifier), facs2, ind);
				}
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					facs2.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button(new GUIContent("Add New")))
			{
				ScriptableCreatorWindow.Open(typeof(GameObjectModifier), facs2);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}