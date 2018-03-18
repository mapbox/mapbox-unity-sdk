namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Map;

	[CustomEditor(typeof(FlatTerrainFactory))]
	public class FlatTerrainFactoryEditor : FactoryEditor
	{
		public SerializedProperty layerProperties;
		private MonoScript script;

		void OnEnable()
		{
			layerProperties = serializedObject.FindProperty("_elevationOptions");
			var terrainType = layerProperties.FindPropertyRelative("elevationLayerType");
			terrainType.enumValueIndex = (int)ElevationLayerType.FlatTerrain;
			script = MonoScript.FromScriptableObject((FlatTerrainFactory)target);
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
}