namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections.Generic;
	using UnityEditor;
	using Mapbox.Editor;
	using UnityEditor.IMGUI.Controls;
	using System.Linq;

	[CustomPropertyDrawer(typeof(LocationPrefabsLayerProperties))]
	public class LocationPrefabsLayerPropertiesDrawer : PropertyDrawer
	{
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		VectorSubLayerTreeView layerTreeView = new VectorSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();

		int SelectionIndex
		{
			get
			{
				return EditorPrefs.GetInt("VectorLayerProperties_selectionIndex");
			}
			set
			{
				EditorPrefs.SetInt("VectorLayerProperties_selectionIndex", value);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = _lineHeight;

			var prefabItemArray = property.FindPropertyRelative("locationPrefabList");
			var layersRect = GUILayoutUtility.GetRect(0, 500, Mathf.Max(prefabItemArray.arraySize + 1, 1) * _lineHeight, (prefabItemArray.arraySize + 1) * _lineHeight);

			layerTreeView.Layers = prefabItemArray;
			layerTreeView.Reload();
			layerTreeView.OnGUI(layersRect);

			selectedLayers = layerTreeView.GetSelection();

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			GUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("Add Item"), (GUIStyle)"minibuttonleft"))
			{
				selectedLayers = layerTreeView.GetSelection();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				prefabItemArray.arraySize++;
				//subLayerArray.InsertArrayElementAtIndex(subLayerArray.arraySize);

				var prefabItem = prefabItemArray.GetArrayElementAtIndex(prefabItemArray.arraySize - 1);
				var prefabItemName = prefabItem.FindPropertyRelative("coreOptions.sublayerName");

				prefabItemName.stringValue = "New Location";

				// Set defaults here because SerializedProperty copies the previous element.
				prefabItem.FindPropertyRelative("coreOptions.isActive").boolValue = true;
				prefabItem.FindPropertyRelative("coreOptions.snapToTerrain").boolValue = true;

				selectedLayers = new int[1] { prefabItemArray.arraySize - 1 };
				layerTreeView.SetSelection(selectedLayers);
			}
			if (GUILayout.Button(new GUIContent("Remove Selected"), (GUIStyle)"minibuttonright"))
			{
				foreach (var index in selectedLayers.OrderByDescending(i => i))
				{
					prefabItemArray.DeleteArrayElementAtIndex(index);
				}
				selectedLayers = new int[0];
				layerTreeView.SetSelection(selectedLayers);
			}

			GUILayout.EndHorizontal();

			if (selectedLayers.Count == 1 && (SelectionIndex > 0 && (SelectionIndex <= prefabItemArray.arraySize - 1)))
			{
				SelectionIndex = selectedLayers[0];

				var layerProperty = prefabItemArray.GetArrayElementAtIndex(SelectionIndex);

				layerProperty.isExpanded = true;
				DrawLayerLocationPrefabProperties(layerProperty);
			}
			else
			{
				GUILayout.Label("Select a location item to see its properties");
			}
			EditorGUI.EndProperty();
		}


		void DrawLayerLocationPrefabProperties(SerializedProperty layerProperty)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(layerProperty);
			EditorGUI.indentLevel--;
		}

	} 
}
