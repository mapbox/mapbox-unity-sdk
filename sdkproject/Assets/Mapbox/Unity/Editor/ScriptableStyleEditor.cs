using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;

[CustomEditor(typeof(ScriptableStyle))]
public class ScriptableStyleEditor : Editor {

	MapFeatureType _featureType;

	override public void OnInspectorGUI()
	{
		
		//ScriptableStyle ss = (ScriptableStyle)target;
		/*
		GUIStyle styleNameTextStyle = new GUIStyle();
		styleNameTextStyle.fontSize = 18;
		styleNameTextStyle.fontStyle = FontStyle.Bold;
		styleNameTextStyle.normal.textColor = Color.white;

		EditorGUILayout.LabelField(ss.name, styleNameTextStyle);

		EditorGUILayout.Space();

		for (int i = 0; i < ss.m_features.Count; i++)
		{
			EditorGUI.indentLevel = 0;
			MapFeatureStyleBundle feature = ss.m_features[i];

			//feature.m_featureType = (MapFeatureType)EditorGUILayout.EnumPopup("Feature Type:", feature.m_featureType);

			//string featureType = feature.m_featureType.ToString();
			GUIStyle featureTypeTextStyle = new GUIStyle();
			featureTypeTextStyle.fontSize = 14;
			//featureTypeTextStyle.fontStyle = FontStyle.Bold;
			featureTypeTextStyle.normal.textColor = Color.white;//new Color(0.2f, 0.2f, 0.2f);
			EditorGUILayout.LabelField(feature.m_featureType.ToString(), featureTypeTextStyle);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Theme:");

			/*
			for (int j = 0; j < feature.m_themes.Count; j++)
			{

				GeometryMaterialOptions materialAtlasPaletteBundle = feature.m_themes[j];

				EditorGUI.indentLevel = 1;

				EditorGUILayout.LabelField(materialAtlasPaletteBundle.Name);

				EditorGUI.indentLevel = 2;

				materialAtlasPaletteBundle.m_material = EditorGUILayout.ObjectField("Material:", materialAtlasPaletteBundle.m_material, typeof(Material), false) as Material;
				materialAtlasPaletteBundle.m_atlasInfo = EditorGUILayout.ObjectField("AltasInfo:", materialAtlasPaletteBundle.m_material, typeof(AtlasInfo), false) as AtlasInfo;
				materialAtlasPaletteBundle.m_palette = EditorGUILayout.ObjectField("Palette:", materialAtlasPaletteBundle.m_material, typeof(ScriptablePalette), false) as ScriptablePalette;



			}
	
			EditorGUILayout.Space();
			EditorGUI.indentLevel = 0;
			EditorGUILayout.LabelField("Ornaments:");

			for (int j = 0; j < feature.m_ornaments.Count; j++)
			{

				OrnamentBundle ornamentBundle = feature.m_ornaments[j];
				EditorGUI.indentLevel = 1;
				EditorGUILayout.LabelField(ornamentBundle.Name);
				EditorGUI.indentLevel = 2;

				ornamentBundle.m_prefab = EditorGUILayout.ObjectField("Ornament prefab:", ornamentBundle.m_prefab, typeof(GameObject), false) as GameObject;
				ornamentBundle.m_spawnLocation = (SpawnLocation)EditorGUILayout.EnumPopup("Ornament Location:", ornamentBundle.m_spawnLocation);//; EditorGUILayout.ObjectField("Ornament Location:", ornamentBundle.m_spawnLocation, typeof(SpawnLocation), false) as SpawnLocation;
				ornamentBundle.m_spawnAlignment = (SpawnAlignment)EditorGUILayout.EnumPopup("Ornament Alignment:", ornamentBundle.m_spawnAlignment);

				//ornamentBundle.m_spawnAlignment = EditorGUILayout.ObjectField("Ornament Alignment:", ornamentBundle.m_spawnAlignment, typeof(SpawnAlignment), false) as SpawnAlignment;

				ornamentBundle.m_density = EditorGUILayout.Slider("Density:", ornamentBundle.m_density, 0.0f, 100.0f);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Remove Feature"))
			{
				ss.RemoveFeature(feature);
			}

			EditorGUILayout.Space();

			EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

		}

		EditorGUI.indentLevel = 0;

		*/


		DrawDefaultInspector();
		/*
		_featureType = (MapFeatureType)EditorGUILayout.EnumPopup("Feature Type:", _featureType);

		if (GUILayout.Button("Add Feature"))
		{
			ss.AddFeature(_featureType);
		}
		if (GUILayout.Button("Clear Features"))
		{
			ss.ClearFeatures();
		}
		*/

	}
}
