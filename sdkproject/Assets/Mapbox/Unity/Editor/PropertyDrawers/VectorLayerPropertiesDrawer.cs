namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using UnityEditor.IMGUI.Controls;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(VectorLayerProperties))]
	public class VectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		static bool showPosition = false;
		static bool showOthers = false;
		VectorSubLayerTreeView layerTreeView = new VectorSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Style Name", tooltip = EnumExtensions.Description(sourceTypeValue) });

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);
			sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;
			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var isActiveProperty = sourceOptionsProperty.FindPropertyRelative("isActive");
			switch (sourceTypeValue)
			{
				case VectorSourceType.MapboxStreets:
				case VectorSourceType.MapboxStreetsWithBuildingIds:
					var sourcePropertyValue = MapboxDefaultVector.GetParameters(sourceTypeValue);
					var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
					var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.PropertyField(sourceOptionsProperty, new GUIContent("Source Option"));
					GUI.enabled = true;
					isActiveProperty.boolValue = true;
					break;
				case VectorSourceType.Custom:
					EditorGUILayout.PropertyField(sourceOptionsProperty, new GUIContent("Source Option"));
					isActiveProperty.boolValue = true;
					break;
				case VectorSourceType.None:
					isActiveProperty.boolValue = false;
					break;
				default:
					isActiveProperty.boolValue = false;
					break;
			}
			if (sourceTypeValue != VectorSourceType.None)
			{
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));

				var isStyleOptimized = property.FindPropertyRelative("useOptimizedStyle");
				EditorGUILayout.PropertyField(isStyleOptimized);
				position.y += lineHeight;

				if (isStyleOptimized.boolValue)
				{
					EditorGUILayout.PropertyField(property.FindPropertyRelative("optimizedStyle"), new GUIContent("Style Options"));
				}
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("optimizedStyle"));
				EditorGUILayout.PropertyField(property.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Options"));
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("performanceOptions"));

				EditorGUILayout.LabelField(new GUIContent { text = "Vector Layer Visualizers", tooltip = "Visualizers for vector features contained in a layer. " });

				var subLayerArray = property.FindPropertyRelative("vectorSubLayers");
				var layersRect = GUILayoutUtility.GetRect(0, 500, Mathf.Max(subLayerArray.arraySize + 1, 1) * lineHeight, (subLayerArray.arraySize + 1) * lineHeight);


				layerTreeView.Layers = subLayerArray;
				layerTreeView.Reload();
				layerTreeView.OnGUI(layersRect);

				selectedLayers = layerTreeView.GetSelection();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				GUILayout.BeginHorizontal();

				if (GUILayout.Button(new GUIContent("Add Visualizer"), (GUIStyle)"minibuttonleft"))
				{
					subLayerArray.arraySize++;
					//subLayerArray.InsertArrayElementAtIndex(subLayerArray.arraySize);

					var subLayer = subLayerArray.GetArrayElementAtIndex(subLayerArray.arraySize - 1);
					var subLayerName = subLayer.FindPropertyRelative("coreOptions.sublayerName");
					Debug.Log("Active status -> " + subLayer.FindPropertyRelative("coreOptions.isActive").boolValue.ToString());
					subLayerName.stringValue = "Untitled";


					// Set defaults here beacuse SerializedProperty copies the previous element. 
					var subLayerCoreOptions = subLayer.FindPropertyRelative("coreOptions");
					subLayerCoreOptions.FindPropertyRelative("isActive").boolValue = true;
					subLayerCoreOptions.FindPropertyRelative("layerName").stringValue = "building";
					subLayerCoreOptions.FindPropertyRelative("geometryType").enumValueIndex = (int)VectorPrimitiveType.Polygon;
					subLayerCoreOptions.FindPropertyRelative("snapToTerrain").boolValue = true;
					subLayerCoreOptions.FindPropertyRelative("groupFeatures").boolValue = false;
					subLayerCoreOptions.FindPropertyRelative("lineWidth").floatValue = 1.0f;

					var subLayerExtrusionOptions = subLayer.FindPropertyRelative("extrusionOptions");
					subLayerExtrusionOptions.FindPropertyRelative("propertyName").stringValue = "height";

				}
				if (GUILayout.Button(new GUIContent("Remove Selected"), (GUIStyle)"minibuttonright"))
				{
					foreach (var index in selectedLayers.OrderByDescending(i => i))
					{
						subLayerArray.DeleteArrayElementAtIndex(index);
					}
					selectedLayers = new int[0];
					layerTreeView.SetSelection(selectedLayers);
				}

				GUILayout.EndHorizontal();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				if (selectedLayers.Count == 1)
				{
					var index = selectedLayers[0];

					var layerProperty = subLayerArray.GetArrayElementAtIndex(index);

					layerProperty.isExpanded = true;
					DrawLayerVisualizerProperties(layerProperty);
				}
				else
				{
					GUILayout.Label("Select a visualizer to see properties");
				}
			}
			EditorGUI.EndProperty();
		}

		void DrawLayerVisualizerProperties(SerializedProperty layerProperty)
		{
			GUILayout.Label("Vector Layer Visualizer Properties");
			GUILayout.BeginVertical();

			var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
			VectorPrimitiveType primitiveTypeProp = (VectorPrimitiveType)subLayerCoreOptions.FindPropertyRelative("geometryType").enumValueIndex;

			EditorGUILayout.PropertyField(subLayerCoreOptions);

			if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
			{
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("extrusionOptions"));

				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("materialOptions"));
			}
			//EditorGUI.indentLevel--;
			showOthers = EditorGUILayout.Foldout(showOthers, "Advanced");
			//EditorGUI.indentLevel++;
			if (showOthers)
			{
				if (primitiveTypeProp == VectorPrimitiveType.Polygon)
				{
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("buildingsWithUniqueIds"), new GUIContent { text = "Buildings With Unique Ids", tooltip = "Turn on this setting only when rendering 3D buildings from the Mapbox Streets with Building Ids tileset. Using this setting with any other polygon layers or source will result in visual artifacts. " });
				}
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("filterOptions"), new GUIContent("Filters"));
				//EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("modifierOptions"), new GUIContent("Modifiers"));
				DrawModifiers(layerProperty, new GUIContent { text = "Modifier Options", tooltip = "Additional Feature modifiers to apply to the visualizer. " });
				//EditorGUI.indentLevel--;
			}

			GUILayout.EndVertical();
		}

		void DrawModifiers(SerializedProperty property, GUIContent label)
		{
			showPosition = EditorGUILayout.Foldout(showPosition, label.text);
			EditorGUILayout.BeginVertical();
			if (showPosition)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(new GUIContent { text = "Feature Position", tooltip = "Position to place feature in the tile. " });
				var featurePositionProperty = property.FindPropertyRelative("moveFeaturePositionTo");
				featurePositionProperty.enumValueIndex = EditorGUILayout.Popup(featurePositionProperty.enumValueIndex, featurePositionProperty.enumDisplayNames);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField(new GUIContent { text = "Mesh Modifiers", tooltip = "Modifiers that manipulate the features mesh. " });

				var meshfac = property.FindPropertyRelative("MeshModifiers");

				for (int i = 0; i < meshfac.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					//GUILayout.Space(5);
					meshfac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(meshfac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(MeshModifier), false) as ScriptableObject;
					EditorGUILayout.EndVertical();
					if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac, ind);
					}
					if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
					{
						meshfac.DeleteArrayElementAtIndex(ind);
					}
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
				{
					meshfac.arraySize++;
					meshfac.GetArrayElementAtIndex(meshfac.arraySize - 1).objectReferenceValue = null;
				}
				if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
				{
					ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac);
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField(new GUIContent { text = "Game Object Modifiers", tooltip = "Modifiers that manipulate the GameObject after mesh generation." });
				var gofac = property.FindPropertyRelative("GoModifiers");
				for (int i = 0; i < gofac.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					gofac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(gofac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier), false) as ScriptableObject;
					EditorGUILayout.EndVertical();

					if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac, ind);
					}
					if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
					{
						gofac.DeleteArrayElementAtIndex(ind);
					}
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
				{
					gofac.arraySize++;
					gofac.GetArrayElementAtIndex(gofac.arraySize - 1).objectReferenceValue = null;
				}
				if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
				{
					ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac);
				}
				EditorGUILayout.EndHorizontal();
				//GUILayout.EndArea();
			}
			//EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}
	}
}
