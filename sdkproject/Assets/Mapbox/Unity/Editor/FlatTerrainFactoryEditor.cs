using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;

[CustomEditor(typeof(FlatTerrainFactory))]
public class FlatTerrainFactoryEditor : FactoryEditor
{
	private FlatTerrainFactory _factory;
	public SerializedProperty
		material_Prop,
		collider_Prop,
		addLayer_Prop,
		layerId_Prop;
	private MonoScript script;

	void OnEnable()
	{
		_factory = target as FlatTerrainFactory;
		material_Prop = serializedObject.FindProperty("_baseMaterial");
		collider_Prop = serializedObject.FindProperty("_addCollider");
		addLayer_Prop = serializedObject.FindProperty("_addToLayer");
		layerId_Prop = serializedObject.FindProperty("_layerId");

		script = MonoScript.FromScriptableObject((FlatTerrainFactory)target);
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(material_Prop, new GUIContent("Material"));
		EditorGUILayout.Space();
		collider_Prop.boolValue = EditorGUILayout.Toggle("Add Collider", collider_Prop.boolValue);
		EditorGUILayout.Space();
		addLayer_Prop.boolValue = EditorGUILayout.Toggle("Add To Layer", addLayer_Prop.boolValue);
		if (addLayer_Prop.boolValue)
		{
			layerId_Prop.intValue = EditorGUILayout.LayerField("Layer", layerId_Prop.intValue);
		}

		serializedObject.ApplyModifiedProperties();
	}
}
