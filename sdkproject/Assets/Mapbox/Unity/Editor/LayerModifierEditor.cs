namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CustomEditor(typeof(LayerModifier))]
	public class LayerModifierEditor : Editor
	{
		public SerializedProperty layerId_Prop;
		private MonoScript script;

		void OnEnable()
		{
			layerId_Prop = serializedObject.FindProperty("_layerId");

			script = MonoScript.FromScriptableObject((LayerModifier)target);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			layerId_Prop.intValue = EditorGUILayout.LayerField("Layer", layerId_Prop.intValue);

			serializedObject.ApplyModifiedProperties();
		}
	}
}