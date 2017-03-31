﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.MeshGeneration.Interfaces;

[CustomEditor(typeof(MeshFactory))]
public class MeshFactoryEditor : FactoryEditor
{
    private MonoScript script;
    private MeshFactory _factory;
    SerializedProperty _visualizerList;
    private int ListSize;
    void OnEnable()
    {
        _factory = target as MeshFactory;
        _visualizerList = serializedObject.FindProperty("Visualizers");
        script = MonoScript.FromScriptableObject(_factory);
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layer Visualizers");
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key");
        EditorGUILayout.LabelField("Visualizers");
        EditorGUILayout.EndHorizontal();
        for (int i = 0; i < _factory.Visualizers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if(_factory.Visualizers[i] != null)
                _factory.Visualizers[i].Key = EditorGUILayout.TextField(_factory.Visualizers[i].Key, GUILayout.MaxWidth(100));
            _factory.Visualizers[i] = (LayerVisualizerBase)EditorGUILayout.ObjectField(_factory.Visualizers[i], typeof(LayerVisualizerBase));

            if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
            {
                _visualizerList.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add New Visualizer"))
        {
            _factory.Visualizers.Add(null);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
