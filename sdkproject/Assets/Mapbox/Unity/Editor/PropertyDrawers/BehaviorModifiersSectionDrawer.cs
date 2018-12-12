namespace Mapbox.Editor
{
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	public class BehaviorModifiersSectionDrawer
	{
		string objectId = "";

		bool showGameplay
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showGameplay");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showGameplay", value);
			}
		}

		public void DrawUI(SerializedProperty layerProperty, VectorPrimitiveType primitiveTypeProp, VectorSourceType sourceType)
		{

			showGameplay = EditorGUILayout.Foldout(showGameplay, "Behavior Modifiers");
			if (showGameplay)
			{

				bool isPrimitiveTypeValidForBuidingIds = (primitiveTypeProp == VectorPrimitiveType.Polygon || primitiveTypeProp == VectorPrimitiveType.Custom);
				bool isSourceValidForBuildingIds = sourceType != VectorSourceType.MapboxStreets;

				layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue = isPrimitiveTypeValidForBuidingIds && isSourceValidForBuildingIds;

				if (layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue == true)
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("buildingsWithUniqueIds"), new GUIContent
					{
						text = "Buildings With Unique Ids",
						tooltip =
							"Turn on this setting only when rendering 3D buildings from the Mapbox Streets with Building Ids tileset. Using this setting with any other polygon layers or source will result in visual artifacts. "
					});
					if (EditorGUI.EndChangeCheck())
					{
						EditorHelper.CheckForModifiedProperty(layerProperty);
					}
				}

				var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
				var combineMeshesProperty = subLayerCoreOptions.FindPropertyRelative("combineMeshes");

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
					EditorGUI.BeginChangeCheck();
					featurePositionProperty.enumValueIndex = EditorGUILayout.Popup(dropDownLabel, featurePositionProperty.enumValueIndex, dropDownItems);
					if (EditorGUI.EndChangeCheck())
					{
						EditorHelper.CheckForModifiedProperty(layerProperty);
					}
				}
				EditorGUILayout.EndHorizontal();
				DrawMeshModifiers(layerProperty);
				DrawGoModifiers(layerProperty);
			}
		}

		private void DrawMeshModifiers(SerializedProperty property)
		{

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
					bool elementWasDeleted = false;
					if (meshfac.arraySize > 0)
					{
						meshfac.DeleteArrayElementAtIndex(ind);
						elementWasDeleted = true;
					}
					if (meshfac.arraySize > 0)
					{
						meshfac.DeleteArrayElementAtIndex(ind);
					}
					if (elementWasDeleted)
					{
						EditorHelper.CheckForModifiedProperty(property);
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
				ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac, -1, null, property);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel--;
		}

		private void DrawGoModifiers(SerializedProperty property)
		{

			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField(new GUIContent
			{
				text = "Game Object Modifiers",
				tooltip = "Modifiers that manipulate the GameObject after mesh generation."
			});
			var gofac = property.FindPropertyRelative("GoModifiers");

			for (int i = 0; i < gofac.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				gofac.GetArrayElementAtIndex(ind).objectReferenceValue =
					EditorGUILayout.ObjectField(gofac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier),
						false) as ScriptableObject;
				EditorGUILayout.EndVertical();

				if (GUILayout.Button(new GUIContent("x"), GUILayout.Width(30)))
				{
					bool elementWasDeleted = false;
					if (gofac.arraySize > 0)
					{
						gofac.DeleteArrayElementAtIndex(ind);
						elementWasDeleted = true;
					}
					if (gofac.arraySize > 0)
					{
						gofac.DeleteArrayElementAtIndex(ind);
					}
					if (elementWasDeleted)
					{
						EditorHelper.CheckForModifiedProperty(property);
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
				PopupWindow.Show(buttonRect, new PopupSelectionMenu(typeof(GameObjectModifier), gofac));
				if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
			}

			if (GUILayout.Button(new GUIContent("Add Existing"), (GUIStyle)"minibuttonright"))
			{

				ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac, -1, null, property);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}
	}
}
