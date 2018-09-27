namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections.Generic;
	using UnityEditor;
	using Mapbox.Editor;
	using UnityEditor.IMGUI.Controls;
	using System.Linq;

	public class PointsOfInterestSubLayerPropertiesDrawer
	{
		string objectId = "";
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		PointsOfInterestSubLayerTreeView layerTreeView = new PointsOfInterestSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();

		int SelectionIndex
		{
			get
			{
				return EditorPrefs.GetInt(objectId + "LocationPrefabsLayerProperties_selectionIndex");
			}
			set
			{
				EditorPrefs.SetInt(objectId + "LocationPrefabsLayerProperties_selectionIndex", value);
			}
		}

		public void DrawUI(SerializedProperty property)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			var prefabItemArray = property.FindPropertyRelative("locationPrefabList");
			var layersRect = EditorGUILayout.GetControlRect(GUILayout.MinHeight(Mathf.Max(prefabItemArray.arraySize + 1, 1) * _lineHeight),
															GUILayout.MaxHeight((prefabItemArray.arraySize + 1) * _lineHeight));

			layerTreeView.Layers = prefabItemArray;
			layerTreeView.Reload();
			layerTreeView.OnGUI(layersRect);

			if (layerTreeView.hasChanged)
			{
				EditorHelper.CheckForModifiedProperty(property);
				layerTreeView.hasChanged = false;
			}

			selectedLayers = layerTreeView.GetSelection();
			//if there are selected elements, set the selection index at the first element.
			//if not, use the Selection index to persist the selection at the right index.
			if (selectedLayers.Count > 0)
			{
				//ensure that selectedLayers[0] isn't out of bounds
				if (selectedLayers[0] > prefabItemArray.arraySize - 1)
				{
					selectedLayers[0] = prefabItemArray.arraySize - 1;
				}

				SelectionIndex = selectedLayers[0];

			}
			else
			{
				selectedLayers = new int[1] { SelectionIndex };
				if (SelectionIndex > 0 && (SelectionIndex <= prefabItemArray.arraySize - 1))
				{
					layerTreeView.SetSelection(selectedLayers);
				}
			}


			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("Add Layer"), (GUIStyle)"minibuttonleft"))
			{

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				selectedLayers = layerTreeView.GetSelection();

				prefabItemArray.arraySize++;

				var prefabItem = prefabItemArray.GetArrayElementAtIndex(prefabItemArray.arraySize - 1);
				var prefabItemName = prefabItem.FindPropertyRelative("coreOptions.sublayerName");

				prefabItemName.stringValue = "New Location";

				// Set defaults here because SerializedProperty copies the previous element.
				prefabItem.FindPropertyRelative("coreOptions.isActive").boolValue = true;
				prefabItem.FindPropertyRelative("coreOptions.snapToTerrain").boolValue = true;
				var categories = prefabItem.FindPropertyRelative("categories");
				categories.intValue = (int)(LocationPrefabCategories.AnyCategory);//To select any category option

				var density = prefabItem.FindPropertyRelative("density");
				density.intValue = 15;//To select all locations option

				selectedLayers = new int[1] { prefabItemArray.arraySize - 1 };
				layerTreeView.SetSelection(selectedLayers);

				if (EditorHelper.DidModifyProperty(property))
				{
					PrefabItemOptions prefabItemOptionToAdd= (PrefabItemOptions)EditorHelper.GetTargetObjectOfProperty(prefabItem) as PrefabItemOptions;
					((VectorLayerProperties)EditorHelper.GetTargetObjectOfProperty(property)).OnSubLayerPropertyAdded(new VectorLayerUpdateArgs { property = prefabItemOptionToAdd });
				}
			}

			if (GUILayout.Button(new GUIContent("Remove Selected"), (GUIStyle)"minibuttonright"))
			{
				if (prefabItemArray.arraySize == 0)
				{
					return;
				}

				List<PrefabItemOptions> LayersToRemove = new List<PrefabItemOptions>();
				foreach (var index in selectedLayers.OrderByDescending(i => i))
				{
					PrefabItemOptions prefabItemOptionsToRemove = (PrefabItemOptions)EditorHelper.GetTargetObjectOfProperty(prefabItemArray.GetArrayElementAtIndex(index)) as PrefabItemOptions;
					if(prefabItemOptionsToRemove != null)
					{
						LayersToRemove.Add(prefabItemOptionsToRemove);
					}
					prefabItemArray.DeleteArrayElementAtIndex(index);
				}
				selectedLayers = new int[0];
				layerTreeView.SetSelection(selectedLayers);
				if (EditorHelper.DidModifyProperty(property))
				{
					for (int i = 0; i < LayersToRemove.Count; i++)
					{
						((VectorLayerProperties)EditorHelper.GetTargetObjectOfProperty(property)).OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = LayersToRemove[i]});
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			if (selectedLayers.Count == 1 && prefabItemArray.arraySize != 0)
			{
				//ensure that selectedLayers[0] isn't out of bounds
				if (selectedLayers[0] > prefabItemArray.arraySize - 1)
				{
					selectedLayers[0] = prefabItemArray.arraySize - 1;
				}
				SelectionIndex = selectedLayers[0];

				var layerProperty = prefabItemArray.GetArrayElementAtIndex(SelectionIndex);

				layerProperty.isExpanded = true;
				var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
				bool isLayerActive = subLayerCoreOptions.FindPropertyRelative("isActive").boolValue;
				if (!isLayerActive)
				{
					GUI.enabled = false;
				}
				DrawLayerLocationPrefabProperties(layerProperty, property);
				if (!isLayerActive)
				{
					GUI.enabled = true;
				}
			}
			else
			{
				GUILayout.Space(15);
				GUILayout.Label("Select a visualizer to see properties");
			}
		}

		void DrawLayerLocationPrefabProperties(SerializedProperty layerProperty, SerializedProperty property)
		{
			EditorGUILayout.PropertyField(layerProperty);
		}
	}
}
