namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;

	[CustomEditor(typeof(FlatSphereTerrainFactory))]
	public class FlatSphereTerrainFactoryEditor : FactoryEditor
	{
		public SerializedProperty
			material_Prop,
			collider_Prop,
			addLayer_Prop,
			radius_Prop,
			sample_Prop,
			layerId_Prop;
		private MonoScript script;

		void OnEnable()
		{
			material_Prop = serializedObject.FindProperty("_baseMaterial");
			collider_Prop = serializedObject.FindProperty("_addCollider");
			addLayer_Prop = serializedObject.FindProperty("_addToLayer");
			layerId_Prop = serializedObject.FindProperty("_layerId");
			radius_Prop = serializedObject.FindProperty("_radius");
			sample_Prop = serializedObject.FindProperty("_sampleCount");

			script = MonoScript.FromScriptableObject((FlatSphereTerrainFactory)target);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			radius_Prop.floatValue = EditorGUILayout.FloatField("Earth Radius", radius_Prop.floatValue);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(sample_Prop, new GUIContent("Resolution"));
			EditorGUILayout.LabelField("x  " + sample_Prop.intValue);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(material_Prop, new GUIContent("Material"));

			EditorGUILayout.Space();
			collider_Prop.boolValue = EditorGUILayout.Toggle("Add Collider", collider_Prop.boolValue);
			addLayer_Prop.boolValue = EditorGUILayout.Toggle("Add To Layer", addLayer_Prop.boolValue);
			if (addLayer_Prop.boolValue)
			{
				layerId_Prop.intValue = EditorGUILayout.LayerField("Layer", layerId_Prop.intValue);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}