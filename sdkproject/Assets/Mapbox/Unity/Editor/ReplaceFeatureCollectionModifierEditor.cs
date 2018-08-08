using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Editor;
/*
[CustomEditor(typeof(ReplaceFeatureCollectionModifier))]

public class ReplaceFeatureCollectionModifierEditor : Editor 
{
	GUIStyle titleStyle;
	private const float titleColorValue = 0.9f;
	Color titleColor = new Color(titleColorValue, titleColorValue, titleColorValue);

	void OnEnable()
	{
		titleStyle = new GUIStyle();
		titleStyle.fontSize = 12;
		titleStyle.fontStyle = FontStyle.Bold;
		titleStyle.normal.textColor = titleColor;
	}

	public override void OnInspectorGUI()
	{
		
		ReplaceFeatureCollectionModifier modifier = (ReplaceFeatureCollectionModifier)target;

		var featureBundleList = serializedObject.FindProperty("featureBundleList");
		for (int i = 0; i < modifier.features.Count; i++)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

			FeatureBundle feature = modifier.features[i];

			EditorGUILayout.BeginHorizontal();
			string labelName = feature.prefab != null ? feature.prefab.name : "Unnamed";
			EditorGUILayout.LabelField(labelName, titleStyle);
			if (GUILayout.Button(new GUIContent("X"), (GUIStyle)"minibutton", GUILayout.Width(30)))
			{
				modifier.features.Remove(feature);
				continue;
			}

			EditorGUILayout.EndHorizontal();
			
			feature.active = EditorGUILayout.Toggle("Active", feature.active);
			feature.prefab = EditorGUILayout.ObjectField("Prefab", feature.prefab, typeof(GameObject), false) as GameObject;
			feature.scaleDownWithWorld = EditorGUILayout.Toggle("Scale down with world", feature.scaleDownWithWorld);

			var test = serializedObject.FindProperty("")

			for (int j = 0; j < feature._prefabLocations.Count; j++)
			{
				EditorGUILayout.BeginHorizontal();
				string latLon = feature._prefabLocations[i];


				float buttonWidth = EditorGUIUtility.singleLineHeight * 4;

				//Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth, EditorGUIUtility.singleLineHeight);
				//Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

				//EditorGUI.PropertyField(fieldRect, property);

				if (GUILayout.Button("Search"))
				{
					GeocodeAttributeSearchWindow.Open(property);
				}




				latLon = EditorGUILayout.PropertyField(latLon);
				EditorGUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add Location"))
			{
				feature._prefabLocations.Add("");
			}
		}
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		EditorGUILayout.Space();
		if (GUILayout.Button("Add Feature"))
		{
			FeatureBundle feature = new FeatureBundle();
			modifier.features.Add(feature);
		}
	}

}
*/