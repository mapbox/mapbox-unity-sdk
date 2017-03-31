﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;

[CustomEditor(typeof(MapImageFactory))]
public class MapImageFactoryEditor : FactoryEditor
{
    private MapImageFactory _factory;
    public SerializedProperty
        mapIdType_Prop,
        material_Prop,
        basicMaps_Prop,
        customMapId_Prop,
        mapId_Prop;
    private MonoScript script;

    private string[] _basicMapIds = new string[6] {
        "mapbox://styles/mapbox/streets-v10",
        "mapbox://styles/mapbox/outdoors-v10",
        "mapbox://styles/mapbox/dark-v9",
        "mapbox://styles/mapbox/light-v9",
        "mapbox://styles/mapbox/satellite-v9",
        "mapbox://styles/mapbox/satellite-streets-v10"};

    private string[] _basicMapNames = new string[6] {
        "Streets",
        "Outdoors",
        "Dark",
        "Light",
        "Satellite",
        "Satellite Street"};

    private int _choiceIndex = 0;
    void OnEnable()
    {
        _factory = target as MapImageFactory;
        customMapId_Prop = serializedObject.FindProperty("_customMapId");
        mapIdType_Prop = serializedObject.FindProperty("_mapIdType");
        mapId_Prop = serializedObject.FindProperty("_mapId");
        material_Prop = serializedObject.FindProperty("_baseMaterial");
        basicMaps_Prop = serializedObject.FindProperty("_basicMapIds");

        script = MonoScript.FromScriptableObject((MapImageFactory)target);
        for (int i = 0; i < _basicMapIds.Length; i++)
        {
            if (_basicMapIds[i] == mapId_Prop.stringValue)
            {
                _choiceIndex = i;
                break;
            }
        }        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
        GUI.enabled = true;
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(mapIdType_Prop, new GUIContent("Map Type"));
        EditorGUILayout.Space();
        var st = (MapImageType)mapIdType_Prop.enumValueIndex;
        EditorGUI.indentLevel++;
        
        switch (st)
        {
            case MapImageType.BasicMapboxStyle:
                {
                    _choiceIndex = EditorGUILayout.Popup("Style", _choiceIndex, _basicMapNames);
                    mapId_Prop.stringValue = _basicMapIds[_choiceIndex];
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(mapId_Prop, new GUIContent("Map Id"));
                    GUI.enabled = true;
                    EditorGUILayout.PropertyField(material_Prop, new GUIContent("Material"));
                    break;
                }
            case MapImageType.Custom:
                {
                    EditorGUILayout.PropertyField(customMapId_Prop, new GUIContent("Map Id"));
                    mapId_Prop.stringValue = customMapId_Prop.stringValue;
                    EditorGUILayout.PropertyField(material_Prop, new GUIContent("Material"));
                    break;
                }
            case MapImageType.None:
                break;

        }
        EditorGUI.indentLevel--;

        if (GUILayout.Button("Update"))
        {
            _factory.Update();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
