﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;

[CustomEditor(typeof(TerrainFactory))]
public class TerrainFactoryEditor : FactoryEditor
{
    private string _defaultMapId = "mapbox.terrain-rgb";
    private TerrainFactory _factory;
    public SerializedProperty
         state_Prop,
         sampleCount_Prop,
        mapIdType_Prop,
         heightMod_Prop,
        customMapId_Prop,
        material_Prop,
        mapId_Prop,
        collider_Prop,
        addLayer_Prop,
        layerId_Prop;
    private MonoScript script;

    void OnEnable()
    {
        _factory = target as TerrainFactory;
        state_Prop = serializedObject.FindProperty("_generationType");
        mapIdType_Prop = serializedObject.FindProperty("_mapIdType");
        sampleCount_Prop = serializedObject.FindProperty("_sampleCount");
        heightMod_Prop = serializedObject.FindProperty("_heightModifier");
        mapId_Prop = serializedObject.FindProperty("_mapId");
        customMapId_Prop = serializedObject.FindProperty("_customMapId");
        material_Prop = serializedObject.FindProperty("_baseMaterial");
        collider_Prop = serializedObject.FindProperty("_addCollider");
        addLayer_Prop = serializedObject.FindProperty("_addToLayer");
        layerId_Prop = serializedObject.FindProperty("_layerId");

        script = MonoScript.FromScriptableObject((TerrainFactory)target);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
        GUI.enabled = true;
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(state_Prop, new GUIContent("Map Type"));
        EditorGUILayout.Space();
        var st = (TerrainGenerationType)state_Prop.enumValueIndex;
        EditorGUI.indentLevel++;
        switch (st)
        {
            case TerrainGenerationType.Flat:

                break;

            case TerrainGenerationType.Height:
                EditorGUILayout.PropertyField(mapIdType_Prop);
                switch ((MapIdType)mapIdType_Prop.enumValueIndex)
                {
                    case MapIdType.Standard:
                        GUI.enabled = false;
                        EditorGUILayout.PropertyField(mapId_Prop, new GUIContent("Map Id"));
                        mapId_Prop.stringValue = _defaultMapId;
                        GUI.enabled = true;
                        break;
                    case MapIdType.Custom:
                        EditorGUILayout.PropertyField(customMapId_Prop, new GUIContent("Map Id"));
                        mapId_Prop.stringValue = customMapId_Prop.stringValue;
                        break;
                }
                EditorGUILayout.PropertyField(heightMod_Prop, new GUIContent("Height Multiplier"));
                break;

        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(sampleCount_Prop, new GUIContent("Resolution"));
        EditorGUILayout.LabelField("x  " + sampleCount_Prop.intValue);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(material_Prop, new GUIContent("Material"));

        EditorGUILayout.Space();
        collider_Prop.boolValue = EditorGUILayout.Toggle("Add Collider", collider_Prop.boolValue);
        EditorGUILayout.Space();
        addLayer_Prop.boolValue = EditorGUILayout.Toggle("Add To Layer", addLayer_Prop.boolValue);
        if (addLayer_Prop.boolValue)
        {
            layerId_Prop.intValue = EditorGUILayout.LayerField("Layer", layerId_Prop.intValue);
        }

        if (GUILayout.Button("Update"))
        {
            //_factory.Update();
			Debug.Log("TerrainFactoryEditor: " + "FIX ME!");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
