using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScriptablePalette))]
public class ScriptablePaletteEditor : Editor
{
    private const int _NUM_COLORS_MIN = 3;
    private const int _NUM_COLORS_MAX = 10;

    private const float _HELPER_TEXT_COLOR_VAL = 0.7f;

    private const string _COLOR_FIELD_PREFIX = "Color {0}";

    private const string _KEY_COLOR_INFO = "Color used as a seed for generating the color palette";
    private const string _NUM_COLOR_INFO = "Number of colors to generate in the palette";

    private const string _HUE_RANGE_INFO = "Maximum range that randomly generated colors will deviate from the key color hue";
    private const string _SAT_RANGE_INFO = "Maximum range that randomly generated colors will deviate from the key color saturation";
    private const string _VAL_RANGE_INFO = "Maximum range that randomly generated colors will deviate from the key color value";

    override public void OnInspectorGUI()
    {
        ScriptablePalette sp = (ScriptablePalette)target;

        EditorGUILayout.Space();

        GUIStyle wrapTextStyle = new GUIStyle();
        wrapTextStyle.wordWrap = true;
        wrapTextStyle.normal.textColor = new Color(_HELPER_TEXT_COLOR_VAL, _HELPER_TEXT_COLOR_VAL, _HELPER_TEXT_COLOR_VAL, 1.0f);

        EditorGUILayout.LabelField("Generate a palette for building colorization by defining a key color, palette size and hue/saturation/value range parameters.", wrapTextStyle);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Base and detail layer colors will be randomly set from the generated palette at runtime for each building.", wrapTextStyle);

        EditorGUILayout.Space();

        GUIContent keyColorContent = new GUIContent("Key color", _KEY_COLOR_INFO);
        sp.m_keyColor = EditorGUILayout.ColorField(keyColorContent, sp.m_keyColor);

        EditorGUILayout.Space();

        GUIContent numColorContent = new GUIContent("Palette Size", _NUM_COLOR_INFO);
        sp.m_numColors = EditorGUILayout.IntSlider(numColorContent, sp.m_numColors, _NUM_COLORS_MIN, _NUM_COLORS_MAX);

        GUIContent hueRangeContent = new GUIContent("Hue Range", _HUE_RANGE_INFO);
        GUIContent satRangeContent = new GUIContent("Saturation Range", _SAT_RANGE_INFO);
        GUIContent valRangeContent = new GUIContent("Value Range", _VAL_RANGE_INFO);

        sp.m_hueRange = EditorGUILayout.Slider(hueRangeContent, sp.m_hueRange, 0, 1);
        sp.m_saturationRange = EditorGUILayout.Slider(satRangeContent, sp.m_saturationRange, 0, 1);
        sp.m_valueRange = EditorGUILayout.Slider(valRangeContent, sp.m_valueRange, 0, 1);

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Palette"))
        {
            sp.GeneratePalette();
        }

        GUILayout.Space(20);

        if(sp.m_colors == null || sp.m_colors.Length == 0)
        {
            EditorGUILayout.LabelField("No color palette defined!", EditorStyles.boldLabel);
        }
        else
        {
            for (int i = 0; i < sp.m_colors.Length; i++)
            {
                string fieldName = string.Format(_COLOR_FIELD_PREFIX, i);
                sp.m_colors[i] = EditorGUILayout.ColorField(fieldName, sp.m_colors[i]);
            }
        }

        GUILayout.Space(20);

        EditorGUILayout.LabelField("Color Overrides", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Layer colors can be overriden by enabling any of the overrides below.", wrapTextStyle);
        EditorGUILayout.LabelField("If a layer's color is overridden, it will be set directly from the defined override color.", wrapTextStyle);

        EditorGUILayout.Space();

        sp.m_setBaseColor_Override = EditorGUILayout.Toggle("Override base", sp.m_setBaseColor_Override);
        if (sp.m_setBaseColor_Override)
        {
            sp.m_baseColor_Override = EditorGUILayout.ColorField("Base Color:", sp.m_baseColor_Override);
        }

        sp.m_setDetailColor1_Override = EditorGUILayout.Toggle("Override detail 1", sp.m_setDetailColor1_Override);
        if (sp.m_setDetailColor1_Override)
        {
            sp.m_detailColor1_Override = EditorGUILayout.ColorField("Detail 1 Color:", sp.m_detailColor1_Override);
        }

        sp.m_setDetailColor2_Override = EditorGUILayout.Toggle("Override detail 2", sp.m_setDetailColor2_Override);
        if (sp.m_setDetailColor2_Override)
        {
            sp.m_detailColor2_Override = EditorGUILayout.ColorField("Detail 2 Color:", sp.m_detailColor2_Override);
        }
    }
}
