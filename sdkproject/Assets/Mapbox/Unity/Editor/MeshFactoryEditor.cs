using UnityEngine;
using System.Collections;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.MeshGeneration.Interfaces;

[CustomEditor(typeof(MeshFactory))]
public class MeshFactoryEditor : FactoryEditor
{
    private string _defaultMapId = "mapbox.mapbox-streets-v7";
    private MonoScript script;
    private MeshFactory _factory;
    SerializedProperty _visualizerList;
    public SerializedProperty mapId_Prop, customMapId_Prop;

    private int ListSize;
    void OnEnable()
    {
        _factory = target as MeshFactory;
        _visualizerList = serializedObject.FindProperty("Visualizers");
        mapId_Prop = serializedObject.FindProperty("_mapId");
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
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layer Visualizers");

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key");
        EditorGUILayout.LabelField("Visualizers");
        EditorGUILayout.EndHorizontal();

        if (_factory.Visualizers != null)
        {
            for (int i = 0; i < _factory.Visualizers.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (_factory.Visualizers[i] != null)
                    _factory.Visualizers[i].Key = EditorGUILayout.TextField(_factory.Visualizers[i].Key, GUILayout.MaxWidth(100));
                _factory.Visualizers[i] = (LayerVisualizerBase)EditorGUILayout.ObjectField(_factory.Visualizers[i], typeof(LayerVisualizerBase));

                if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                {
                    _visualizerList.DeleteArrayElementAtIndex(i);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        if (GUILayout.Button("Add New Visualizer"))
        {
            _factory.Visualizers.Add(null);
        }
        EditorUtility.SetDirty(_factory);
        serializedObject.ApplyModifiedProperties();
    }
}
