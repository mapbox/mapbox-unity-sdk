namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;

	[CustomEditor(typeof(FlatTerrainFactory))]
	public class FlatTerrainFactoryEditor : FactoryEditor
	{
		public SerializedProperty
			material_Prop,
			collider_Prop,
			addLayer_Prop,
			addWall_Prop,
			wallHeight_Prop,
			wallMaterial_Prop,
			layerId_Prop;
		private MonoScript script;

		void OnEnable()
		{
			material_Prop = serializedObject.FindProperty("_baseMaterial");
			collider_Prop = serializedObject.FindProperty("_addCollider");
			addLayer_Prop = serializedObject.FindProperty("_addToLayer");
			addWall_Prop = serializedObject.FindProperty("_createSideWalls");
			wallHeight_Prop = serializedObject.FindProperty("_sideWallHeight");
			wallMaterial_Prop = serializedObject.FindProperty("_sideWallMaterial");
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
			addWall_Prop.boolValue = EditorGUILayout.Toggle("Add Walls", addWall_Prop.boolValue);
			if (addWall_Prop.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(wallHeight_Prop, new GUIContent("Wall Height"));
				EditorGUILayout.PropertyField(wallMaterial_Prop, new GUIContent("Wall Material"));
				EditorGUI.indentLevel--;
			}
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
}