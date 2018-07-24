namespace Mapbox.Editor
{
	using UnityEngine;
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

		public void DrawUI(SerializedProperty subLayerCoreOptions, SerializedProperty layerProperty, VectorPrimitiveType primitiveTypeProp, VectorSourceType sourceType)
		{
			objectId = layerProperty.serializedObject.targetObject.GetInstanceID().ToString();

			EditorGUILayout.BeginVertical();
			showModeling = EditorGUILayout.Foldout(showModeling, new GUIContent { text = "Modeling", tooltip = "This section provides you with options to fine tune your meshes" });
			if (showModeling)
			{
				EditorGUI.indentLevel++;

				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(subLayerCoreOptions);

				if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
				{
					GUILayout.Space(-_lineHeight);
					var extrusionOptions = layerProperty.FindPropertyRelative("extrusionOptions");
					extrusionOptions.FindPropertyRelative("_selectedLayerName").stringValue = subLayerCoreOptions.FindPropertyRelative("layerName").stringValue;
					EditorGUILayout.PropertyField(extrusionOptions);
				}

				var snapToTerrainProperty = subLayerCoreOptions.FindPropertyRelative("snapToTerrain");
				var combineMeshesProperty = subLayerCoreOptions.FindPropertyRelative("combineMeshes");

				snapToTerrainProperty.boolValue = EditorGUILayout.Toggle(snapToTerrainProperty.displayName, snapToTerrainProperty.boolValue);
				combineMeshesProperty.boolValue = EditorGUILayout.Toggle(combineMeshesProperty.displayName, combineMeshesProperty.boolValue);

				if ((primitiveTypeProp == VectorPrimitiveType.Polygon || primitiveTypeProp == VectorPrimitiveType.Custom) && sourceType != VectorSourceType.MapboxStreets)
				{
					layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue = true;
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("buildingsWithUniqueIds"), new GUIContent
					{
						text = "Buildings With Unique Ids",
						tooltip =
							"Turn on this setting only when rendering 3D buildings from the Mapbox Streets with Building Ids tileset. Using this setting with any other polygon layers or source will result in visual artifacts. "
					});
				}

				if (sourceType != VectorSourceType.None)
				{
					GUILayout.Space(-_lineHeight);
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Options"));
				}

				EditorGUILayout.BeginHorizontal();
				if (combineMeshesProperty.boolValue == false)
				{
					var featurePositionProperty = layerProperty.FindPropertyRelative("moveFeaturePositionTo");
					GUIContent dropDownLabel = new GUIContent
					{
						text = "Feature Position",
						tooltip = "Position to place feature in the tile. "
					};

					GUIContent[] dropDownItems = new GUIContent[featurePositionProperty.enumDisplayNames.Length];

					for (int i = 0; i < featurePositionProperty.enumDisplayNames.Length; i++)
					{
						dropDownItems[i] = new GUIContent
						{
							text = featurePositionProperty.enumDisplayNames[i]
						};
					}

					featurePositionProperty.enumValueIndex = EditorGUILayout.Popup(dropDownLabel, featurePositionProperty.enumValueIndex, dropDownItems);
				}
				EditorGUILayout.EndHorizontal();

				DrawMeshModifiers(layerProperty);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
		}


		private void DrawMeshModifiers(SerializedProperty property)
		{
			var combineMeshesProperty = property.FindPropertyRelative("coreOptions").FindPropertyRelative("combineMeshes");
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField(new GUIContent
			{
				text = "Mesh Modifiers",
				tooltip = "Modifiers that manipulate the features mesh. "
			});

			var meshfac = property.FindPropertyRelative("MeshModifiers");

			for (int i = 0; i < meshfac.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical();
				meshfac.GetArrayElementAtIndex(ind).objectReferenceValue =
					EditorGUILayout.ObjectField(meshfac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(MeshModifier), false)
						as ScriptableObject;

				EditorGUILayout.EndVertical();

				if (GUILayout.Button(new GUIContent("x"), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
				{
					if (meshfac.arraySize > 0)
					{
						meshfac.DeleteArrayElementAtIndex(ind);
					}
					if (meshfac.arraySize > 0)
					{
						meshfac.DeleteArrayElementAtIndex(ind);
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUI.indentLevel * 12);
			Rect buttonRect = GUILayoutUtility.GetLastRect();
			if (GUILayout.Button(new GUIContent("Add New"), (GUIStyle)"minibuttonleft"))
			{
				PopupWindow.Show(buttonRect, new PopupSelectionMenu(typeof(MeshModifier), meshfac));
				if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
			}

			if (GUILayout.Button(new GUIContent("Add Existing"), (GUIStyle)"minibuttonright"))
			{
				ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel--;
		}
	}
}