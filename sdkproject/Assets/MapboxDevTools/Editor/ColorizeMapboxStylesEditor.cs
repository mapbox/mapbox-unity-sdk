using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

    //AtlasConstructor
[CustomEditor(typeof(ColorizeMapboxStyles))]
public class ColorizeMapboxStylesEditor : Editor
{

    override public void OnInspectorGUI()
    {
        ColorizeMapboxStyles colorize = (ColorizeMapboxStyles)target;

        EditorGUI.BeginChangeCheck();

        colorize.m_baseColor = EditorGUILayout.ColorField("Base Color", colorize.m_baseColor);
        colorize.m_detailColor1 = EditorGUILayout.ColorField("Detail 1 Color", colorize.m_detailColor1);
        colorize.m_detailColor2 = EditorGUILayout.ColorField("Detail 2 Color", colorize.m_detailColor2);

        if (EditorGUI.EndChangeCheck())
        {
            colorize.SetColorValues();
        }

        if (GUILayout.Button("Randomize Colors"))
        {
            colorize.SetRandomColor();
        }
    }
}

