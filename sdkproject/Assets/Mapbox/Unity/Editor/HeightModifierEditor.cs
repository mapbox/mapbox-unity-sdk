using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeightModifier))]
public class HeightModifierEditor : UnityEditor.Editor
{
	private MonoScript script;
	private SerializedProperty _type;
	private SerializedProperty _basicOptions;
	private SerializedProperty _textureOptions;
	private SerializedProperty _chamferOptions;

	private void OnEnable()
	{
		script = MonoScript.FromScriptableObject((HeightModifier)target);
		_type = serializedObject.FindProperty("HeightModifierType");
		_basicOptions = serializedObject.FindProperty("_basicOptions");
		_textureOptions = serializedObject.FindProperty("_texturedOptions");
		_chamferOptions = serializedObject.FindProperty("_chamferOptions");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		GUI.enabled = false;
		script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
		GUI.enabled = true;

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(_type);
		EditorGUILayout.Space();

		switch ((HeightModifierTypes) _type.enumValueIndex)
		{
			case HeightModifierTypes.Basic:
			{
				EditorGUILayout.PropertyField(_basicOptions);
				break;
			}
			case HeightModifierTypes.Textured:
			{
				EditorGUILayout.PropertyField(_textureOptions);
				break;
			}
			case HeightModifierTypes.Chamfered:
			{
				EditorGUILayout.PropertyField(_chamferOptions);
				break;
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
