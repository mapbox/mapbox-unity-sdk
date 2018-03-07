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

	[CustomPropertyDrawer(typeof(VectorLayerProperties))]
	public class VectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = false;
		bool showOthers = false;
		VectorSubLayerTreeView layerTreeView = new VectorSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var labelBoldStyle = new GUIStyle { fontStyle = FontStyle.Bold };
			var labelItalicCenteredStyle = new GUIStyle { fontStyle = FontStyle.Italic };

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Style Name"));
			var sourceTypeProperty = property.FindPropertyRelative("sourceType");

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);
			var sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;
			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var isActiveProperty = sourceOptionsProperty.FindPropertyRelative("isActive");
			switch (sourceTypeValue)
			{
				case VectorSourceType.MapboxStreets:
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
					EditorGUILayout.PropertyField(property.FindPropertyRelative("sourceOptions"), true);
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

				var isStyleOptimized = property.FindPropertyRelative("isStyleOptimized");
				EditorGUILayout.PropertyField(isStyleOptimized);
				position.y += lineHeight;

				if (isStyleOptimized.boolValue)
				{
					EditorGUILayout.PropertyField(property.FindPropertyRelative("optimizedStyle"), new GUIContent("Style Options"));
				}
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("optimizedStyle"));
				EditorGUILayout.PropertyField(property.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Options"));
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("performanceOptions"));

				EditorGUILayout.LabelField("Visualizer Stack");

				var subLayerArray = property.FindPropertyRelative("vectorSubLayers");
				var layersRect = GUILayoutUtility.GetRect(0, 500, Mathf.Max(subLayerArray.arraySize + 1, 1) * lineHeight, (subLayerArray.arraySize + 1) * lineHeight);


				layerTreeView.Layers = subLayerArray;
				layerTreeView.Reload();
				layerTreeView.OnGUI(layersRect);

				selectedLayers = layerTreeView.GetSelection();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Add Layer"))
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

					var subLayerExtrusionOptions = subLayer.FindPropertyRelative("extrusionOptions");
					subLayerExtrusionOptions.FindPropertyRelative("propertyName").stringValue = "height";

				}
				if (GUILayout.Button("Remove Selected"))
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
					GUILayout.Label("Select a layer to see properties", labelItalicCenteredStyle);
				}
			}
			EditorGUI.EndProperty();
		}

		void DrawLayerVisualizerProperties(SerializedProperty layerProperty)
		{
			GUILayout.Label("Visualizer Stack Properties");
			GUILayout.BeginVertical();

			var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
			VectorPrimitiveType primitiveTypeProp = (VectorPrimitiveType)subLayerCoreOptions.FindPropertyRelative("geometryType").enumValueIndex;

			EditorGUILayout.PropertyField(subLayerCoreOptions);

			if (primitiveTypeProp != VectorPrimitiveType.Point)
			{
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("extrusionOptions"));

				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("materialOptions"));
			}
			showOthers = EditorGUILayout.Foldout(showOthers, "Advanced");
			if (showOthers)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("filterOptions"), new GUIContent("Filters"));
				//EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("modifierOptions"), new GUIContent("Modifiers"));
				DrawModifiers(layerProperty, new GUIContent("Modifier Options"));
				EditorGUI.indentLevel--;
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
				EditorGUILayout.PrefixLabel("Feature Position");
				var featurePositionProperty = property.FindPropertyRelative("moveFeaturePositionTo");
				featurePositionProperty.enumValueIndex = EditorGUILayout.Popup(featurePositionProperty.enumValueIndex, featurePositionProperty.enumDisplayNames);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Mesh Modifiers");

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
				EditorGUILayout.LabelField("Game Object Modifiers");
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
