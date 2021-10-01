using UnityEditor.Rendering;

namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CustomEditor(typeof(ModifierStack))]
	public class ModifierStackEditor : UnityEditor.Editor
	{

		private MonoScript script;
		private SerializedProperty _positionType;
		private SerializedProperty _filters;
		private SerializedProperty _combineMeshes;
		private Texture2D _magnifier;

		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((ModifierStack)target);
			_positionType = serializedObject.FindProperty("moveFeaturePositionTo");
			_filters = serializedObject.FindProperty("filterOptions");
			_combineMeshes = serializedObject.FindProperty("combineMeshes");
			_magnifier = EditorGUIUtility.FindTexture("d_ViewToolZoom");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(_filters, new GUIContent("Filters"));
			EditorGUILayout.PropertyField(_combineMeshes, new GUIContent("Combine Meshes"));
			EditorGUILayout.PropertyField(_positionType, new GUIContent("Feature Position"));
			var meshfac = serializedObject.FindProperty("MeshModifiers");
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Mesh Modifiers");
			if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30), GUILayout.Height(22)))
			{
				meshfac.arraySize++;
				meshfac.GetArrayElementAtIndex(meshfac.arraySize - 1).objectReferenceValue = null;
			}
			if (GUILayout.Button(_magnifier, (GUIStyle)"minibuttonright", GUILayout.Width(30)))
			{
				ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac);
			}
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < meshfac.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				meshfac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(meshfac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(MeshModifier), false) as ScriptableObject;
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					meshfac.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			var gofac = serializedObject.FindProperty("GoModifiers");
			EditorGUILayout.LabelField("Game Object Modifiers");
			if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30), GUILayout.Height(22)))
			{
				gofac.arraySize++;
				gofac.GetArrayElementAtIndex(gofac.arraySize - 1).objectReferenceValue = null;
			}
			if (GUILayout.Button(_magnifier, (GUIStyle)"minibuttonright", GUILayout.Width(30)))
			{
				ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac);
			}
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < gofac.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				gofac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(gofac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier), false) as ScriptableObject;
				if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30), GUILayout.Height(22)))
				{
					gofac.DeleteArrayElementAtIndex(ind);
				}
				EditorGUILayout.EndHorizontal();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}