using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Modifiers;

[CustomEditor(typeof(ReplaceFeatureCollectionModifier))]

public class ReplaceFeatureCollectionModifierEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		ReplaceFeatureCollectionModifier modifier = (ReplaceFeatureCollectionModifier)target;
		for (int i = 0; i < modifier.features.Count; i++)
		{
			FeatureBundle feature = modifier.features[i];
			feature.active = EditorGUILayout.Toggle("Active", feature.active);

			//feature.spawnPrefabOptions = EditorGUILayout.Toggle("Active", feature.active);
		}
	}

}
