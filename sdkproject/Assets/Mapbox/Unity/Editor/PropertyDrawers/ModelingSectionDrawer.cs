namespace Mapbox.Editor
{
	using UnityEngine;
	using System;
	using System.Collections;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Editor;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	public class ModelingSectionDrawer
	{
		private string objectId = "";
		bool showModeling
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showModeling");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showModeling", value);
			}
		}
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		public void DrawUI(SerializedProperty subLayerCoreOptions, SerializedProperty layerProperty, VectorPrimitiveType primitiveTypeProp, Action<SerializedProperty> action = null)
		{
			objectId = layerProperty.serializedObject.targetObject.GetInstanceID().ToString();

			EditorGUILayout.BeginVertical();
			showModeling = EditorGUILayout.Foldout(showModeling, new GUIContent { text = "Modeling", tooltip = "This section provides you with options to fine tune your meshes" });
			if (showModeling)
			{
				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(subLayerCoreOptions);

				if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
				{
					GUILayout.Space(-_lineHeight);
					var extrusionOptions = layerProperty.FindPropertyRelative("extrusionOptions");
					extrusionOptions.FindPropertyRelative("_selectedLayerName").stringValue = subLayerCoreOptions.FindPropertyRelative("layerName").stringValue;
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(extrusionOptions);
					if (EditorGUI.EndChangeCheck() && action != null)
					{
						action(extrusionOptions);
					}
				}

				EditorGUI.BeginChangeCheck();
				var snapToTerrainProperty = subLayerCoreOptions.FindPropertyRelative("snapToTerrain");
				snapToTerrainProperty.boolValue = EditorGUILayout.Toggle(snapToTerrainProperty.displayName, snapToTerrainProperty.boolValue);

				if (EditorGUI.EndChangeCheck() && action != null)
				{
					action(snapToTerrainProperty);
				}

				EditorGUI.BeginChangeCheck();
				var combineMeshesProperty = subLayerCoreOptions.FindPropertyRelative("combineMeshes");
				combineMeshesProperty.boolValue = EditorGUILayout.Toggle(combineMeshesProperty.displayName, combineMeshesProperty.boolValue);
				if (EditorGUI.EndChangeCheck() && action != null)
				{
					action(combineMeshesProperty);
				}

				if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("colliderOptions"));
					if (EditorGUI.EndChangeCheck() && action != null)
					{
						action(layerProperty.FindPropertyRelative("colliderOptions"));
					}
				}
			}
			EditorGUILayout.EndVertical();
		}



	}
}