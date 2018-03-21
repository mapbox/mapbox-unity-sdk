using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Map;
using Mapbox.Editor;

[CustomEditor(typeof(TerrainWithSideWallsFactory))]
public class TerrainWithSideWallsFactoryEditor : FactoryEditor
{
	public SerializedProperty layerProperties;
	private MonoScript script;

	void OnEnable()
	{
		layerProperties = serializedObject.FindProperty("_elevationOptions");
		var terrainType = layerProperties.FindPropertyRelative("elevationLayerType");
		terrainType.enumValueIndex = (int)ElevationLayerType.TerrainWithElevation;
		script = MonoScript.FromScriptableObject((TerrainFactory)target);
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		GUI.enabled = false;
		script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
		GUI.enabled = true;
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(layerProperties);
		EditorGUILayout.Space();

		serializedObject.ApplyModifiedProperties();
	}
}
