namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using UnityEditor.IMGUI.Controls;

	[CustomPropertyDrawer(typeof(VectorLayerProperties))]
	public class VectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = false;
		VectorSubLayerTreeView layerTreeView = new VectorSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			showPosition = EditorGUI.Foldout(position, showPosition, label);

			if (showPosition)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceOptions"), new GUIContent("Source Option"));
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Option"));

				//GUILayout.Space(EditorGUIUtility.singleLineHeight);
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("performanceOptions"));

				var labelBoldStyle = new GUIStyle { fontStyle = FontStyle.Bold };
				var labelItalicCenteredStyle = new GUIStyle { fontStyle = FontStyle.Italic };
				EditorGUI.LabelField(position, "Layers", labelBoldStyle);
				position.y += lineHeight;
				var subLayerArray = property.FindPropertyRelative("vectorSubLayers");
				//Debug.Log("Array size : " + subLayerArray.arraySize);
				var testRect = new Rect(position.x, position.y, position.width, (subLayerArray.arraySize + 1) * lineHeight);


				layerTreeView.Layers = subLayerArray;
				layerTreeView.Reload();
				layerTreeView.OnGUI(testRect);

				selectedLayers = layerTreeView.GetSelection();
				GUILayout.BeginVertical();//
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Add Layer"))
				{
					//subLayerArray.arraySize++;
					subLayerArray.InsertArrayElementAtIndex(subLayerArray.arraySize);

					var subLayer = subLayerArray.GetArrayElementAtIndex(subLayerArray.arraySize - 1);
					var subLayerName = subLayer.FindPropertyRelative("coreOptions.layerName");
					subLayerName.stringValue = "Untitled";
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
					position.y += (subLayerArray.arraySize + 3) * lineHeight;
					EditorGUI.LabelField(position, "Layers Properties", labelBoldStyle);
					position.y += lineHeight;
					EditorGUI.PropertyField(position, layerProperty.FindPropertyRelative("coreOptions"), new GUIContent("Core Options"));
					position.y += (EditorGUI.GetPropertyHeight(layerProperty.FindPropertyRelative("coreOptions")));
					EditorGUI.PropertyField(position, layerProperty.FindPropertyRelative("extrusionOptions"));
					position.y += (EditorGUI.GetPropertyHeight(layerProperty.FindPropertyRelative("extrusionOptions")));
					EditorGUI.PropertyField(position, layerProperty.FindPropertyRelative("materialOptions"));
					position.y += (EditorGUI.GetPropertyHeight(layerProperty.FindPropertyRelative("materialOptions")));
					EditorGUI.PropertyField(position, layerProperty.FindPropertyRelative("modifierOptions"), new GUIContent("Modifier Options"));
				}
				else
				{
					GUILayout.Label("Select a layer to see properties", labelItalicCenteredStyle);
				}
				GUILayout.EndVertical();
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				var subLayers = property.FindPropertyRelative("vectorSubLayers");
				height += (4.0f * lineHeight);
				height += (subLayers.arraySize) * lineHeight;
				height += (selectedLayers.Count == 1) ? EditorGUI.GetPropertyHeight(subLayers.GetArrayElementAtIndex(selectedLayers[0]).FindPropertyRelative("coreOptions")) : lineHeight;
				height += (selectedLayers.Count == 1) ? EditorGUI.GetPropertyHeight(subLayers.GetArrayElementAtIndex(selectedLayers[0]).FindPropertyRelative("modifierOptions")) : lineHeight;
				height += (selectedLayers.Count == 1) ? EditorGUI.GetPropertyHeight(subLayers.GetArrayElementAtIndex(selectedLayers[0]).FindPropertyRelative("extrusionOptions")) : lineHeight;
				height += (selectedLayers.Count == 1) ? EditorGUI.GetPropertyHeight(subLayers.GetArrayElementAtIndex(selectedLayers[0]).FindPropertyRelative("materialOptions")) : lineHeight;
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("performanceOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
		}
	}
}
