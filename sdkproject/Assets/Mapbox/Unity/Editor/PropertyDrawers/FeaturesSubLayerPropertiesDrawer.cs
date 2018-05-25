namespace Mapbox.Editor
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using UnityEditor.IMGUI.Controls;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.VectorTile.ExtensionMethods;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Platform.TilesetTileJSON;

	public class FeaturesSubLayerPropertiesDrawer
	{
		static float _lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] _sourceTypeContent;
		bool _isGUIContentSet = false;
		bool _isInitialized = false;
		private TileJsonData tileJSONData;
		private static TileJSONResponse tileJSONResponse;
		/// <summary>
		/// Gets or sets the layerID
		/// </summary>
		/// <value><c>true</c> then show general section; otherwise hide, <c>false</c>.</value>

		string objectId = "";
		private string TilesetId
		{
			get
			{
				return EditorPrefs.GetString(objectId + "VectorSubLayerProperties_tilesetId");
			}
			set
			{
				EditorPrefs.SetString(objectId + "VectorSubLayerProperties_tilesetId", value);
			}
		}

		bool ShowPosition
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showPosition");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showPosition", value);
			}
		}

		bool ShowOthers
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showOthers");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showOthers", value);
			}
		}

		int SelectionIndex
		{
			get
			{
				return EditorPrefs.GetInt(objectId + "VectorSubLayerProperties_selectionIndex");
			}
			set
			{
				EditorPrefs.SetInt(objectId + "VectorSubLayerProperties_selectionIndex", value);
			}
		}

		FeatureSubLayerTreeView layerTreeView = new FeatureSubLayerTreeView(new TreeViewState());
		IList<int> selectedLayers = new List<int>();
		public void DrawUI(SerializedProperty property)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			var serializedMapObject = property.serializedObject;
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJSONData = mapObject.VectorData.LayerProperty.tileJsonData;

			var sourceTypeProperty = property.FindPropertyRelative("_sourceType");
			var sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

			var displayNames = sourceTypeProperty.enumDisplayNames;
			int count = sourceTypeProperty.enumDisplayNames.Length;
			if (!_isGUIContentSet)
			{
				_sourceTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					_sourceTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = ((VectorSourceType)extIdx).Description(),
					};
				}
				_isGUIContentSet = true;
			}

			sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
			var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
			var isActiveProperty = sourceOptionsProperty.FindPropertyRelative("isActive");
			switch (sourceTypeValue)
			{
				case VectorSourceType.MapboxStreets:
				case VectorSourceType.MapboxStreetsWithBuildingIds:
					var sourcePropertyValue = MapboxDefaultVector.GetParameters(sourceTypeValue);
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					if (_isInitialized)
					{
						LoadEditorTileJSON(property, sourceTypeValue, layerSourceId.stringValue);
					}
					else
					{
						_isInitialized = true;
					}
					if (tileJSONData.PropertyDisplayNames.Count == 0 && tileJSONData.tileJSONLoaded)
					{
						EditorGUILayout.HelpBox("Invalid Map Id / There might be a problem with the internet connection.", MessageType.Error);
					}
					GUI.enabled = true;
					isActiveProperty.boolValue = true;
					break;
				case VectorSourceType.Custom:
					if (_isInitialized)
					{
						string test = layerSourceId.stringValue;
						LoadEditorTileJSON(property, sourceTypeValue, layerSourceId.stringValue);
					}
					else
					{
						_isInitialized = true;
					}
					if (tileJSONData.PropertyDisplayNames.Count == 0 && tileJSONData.tileJSONLoaded)
					{
						EditorGUILayout.HelpBox("Invalid Map Id / There might be a problem with the internet connection.", MessageType.Error);
					}
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
				EditorGUILayout.LabelField(new GUIContent
				{
					text = "Vector Layer Visualizers",
					tooltip = "Visualizers for vector features contained in a layer. "
				});

				var subLayerArray = property.FindPropertyRelative("vectorSubLayers");

				var layersRect = EditorGUILayout.GetControlRect(GUILayout.MinHeight(Mathf.Max(subLayerArray.arraySize + 1, 1) * _lineHeight),
																GUILayout.MaxHeight((subLayerArray.arraySize + 1) * _lineHeight));
				layerTreeView.Layers = subLayerArray;
				layerTreeView.Reload();
				layerTreeView.OnGUI(layersRect);

				selectedLayers = layerTreeView.GetSelection();

				//if there are selected elements, set the selection index at the first element.
				//if not, use the Selection index to persist the selection at the right index.
				if (selectedLayers.Count > 0)
				{
					//ensure that selectedLayers[0] isn't out of bounds
					if (selectedLayers[0] - layerTreeView.uniqueId > subLayerArray.arraySize - 1)
					{
						selectedLayers[0] = subLayerArray.arraySize - 1 + layerTreeView.uniqueId;
					}

					SelectionIndex = selectedLayers[0];
				}
				else
				{
					if (SelectionIndex > 0 && (SelectionIndex - layerTreeView.uniqueId <= subLayerArray.arraySize - 1))
					{
						selectedLayers = new int[1] { SelectionIndex };
						layerTreeView.SetSelection(selectedLayers);
					}
				}

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button(new GUIContent("Add Visualizer"), (GUIStyle)"minibuttonleft"))
				{
					subLayerArray.arraySize++;

					var subLayer = subLayerArray.GetArrayElementAtIndex(subLayerArray.arraySize - 1);
					var subLayerName = subLayer.FindPropertyRelative("coreOptions.sublayerName");

					subLayerName.stringValue = "Untitled";

					// Set defaults here because SerializedProperty copies the previous element.
					var subLayerCoreOptions = subLayer.FindPropertyRelative("coreOptions");
					subLayerCoreOptions.FindPropertyRelative("isActive").boolValue = true;
					subLayerCoreOptions.FindPropertyRelative("layerName").stringValue = "building";
					subLayerCoreOptions.FindPropertyRelative("geometryType").enumValueIndex = (int)VectorPrimitiveType.Polygon;
					subLayerCoreOptions.FindPropertyRelative("snapToTerrain").boolValue = true;
					subLayerCoreOptions.FindPropertyRelative("groupFeatures").boolValue = false;
					subLayerCoreOptions.FindPropertyRelative("lineWidth").floatValue = 1.0f;

					var subLayerExtrusionOptions = subLayer.FindPropertyRelative("extrusionOptions");
					subLayerExtrusionOptions.FindPropertyRelative("extrusionType").enumValueIndex = (int)ExtrusionType.None;
					subLayerExtrusionOptions.FindPropertyRelative("extrusionGeometryType").enumValueIndex =
						(int)ExtrusionGeometryType.RoofAndSide;
					subLayerExtrusionOptions.FindPropertyRelative("propertyName").stringValue = "height";
					subLayerExtrusionOptions.FindPropertyRelative("extrusionScaleFactor").floatValue = 1f;

					var subLayerFilterOptions = subLayer.FindPropertyRelative("filterOptions");
					subLayerFilterOptions.FindPropertyRelative("filters").ClearArray();
					subLayerFilterOptions.FindPropertyRelative("combinerType").enumValueIndex =
						(int)LayerFilterCombinerOperationType.Any;

					var subLayerGeometryMaterialOptions = subLayer.FindPropertyRelative("materialOptions");
					subLayerGeometryMaterialOptions.FindPropertyRelative("style").enumValueIndex = (int)StyleTypes.Realistic;

					GeometryMaterialOptions geometryMaterialOptionsReference = MapboxDefaultStyles.GetDefaultAssets();

					var mats = subLayerGeometryMaterialOptions.FindPropertyRelative("materials");
					mats.arraySize = 2;

					var topMatArray = mats.GetArrayElementAtIndex(0).FindPropertyRelative("Materials");
					var sideMatArray = mats.GetArrayElementAtIndex(1).FindPropertyRelative("Materials");

					if (topMatArray.arraySize == 0)
					{
						topMatArray.arraySize = 1;
					}
					if (sideMatArray.arraySize == 0)
					{
						sideMatArray.arraySize = 1;
					}

					var topMat = topMatArray.GetArrayElementAtIndex(0);
					var sideMat = sideMatArray.GetArrayElementAtIndex(0);

					var atlas = subLayerGeometryMaterialOptions.FindPropertyRelative("atlasInfo");
					var palette = subLayerGeometryMaterialOptions.FindPropertyRelative("colorPalette");

					topMat.objectReferenceValue = geometryMaterialOptionsReference.materials[0].Materials[0];
					sideMat.objectReferenceValue = geometryMaterialOptionsReference.materials[1].Materials[0];
					atlas.objectReferenceValue = geometryMaterialOptionsReference.atlasInfo;
					palette.objectReferenceValue = geometryMaterialOptionsReference.colorPalette;

					subLayer.FindPropertyRelative("buildingsWithUniqueIds").boolValue = false;
					subLayer.FindPropertyRelative("moveFeaturePositionTo").enumValueIndex = (int)PositionTargetType.TileCenter;
					subLayer.FindPropertyRelative("MeshModifiers").ClearArray();
					subLayer.FindPropertyRelative("GoModifiers").ClearArray();

					var subLayerColliderOptions = subLayer.FindPropertyRelative("colliderOptions");
					subLayerColliderOptions.FindPropertyRelative("colliderType").enumValueIndex = (int)ColliderType.None;

					selectedLayers = new int[1] { subLayerArray.arraySize - 1 + layerTreeView.uniqueId };
					layerTreeView.SetSelection(selectedLayers);
				}

				if (GUILayout.Button(new GUIContent("Remove Selected"), (GUIStyle)"minibuttonright"))
				{
					foreach (var index in selectedLayers.OrderByDescending(i => i))
					{
						subLayerArray.DeleteArrayElementAtIndex(index - layerTreeView.uniqueId);
					}

					selectedLayers = new int[0];
					layerTreeView.SetSelection(selectedLayers);
				}

				EditorGUILayout.EndHorizontal();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);

				if (selectedLayers.Count == 1 && subLayerArray.arraySize != 0)
				{
					//ensure that selectedLayers[0] isn't out of bounds
					if (selectedLayers[0] - layerTreeView.uniqueId > subLayerArray.arraySize - 1)
					{
						selectedLayers[0] = subLayerArray.arraySize - 1 + layerTreeView.uniqueId;
					}

					SelectionIndex = selectedLayers[0];

					var layerProperty = subLayerArray.GetArrayElementAtIndex(SelectionIndex - layerTreeView.uniqueId);

					layerProperty.isExpanded = true;
					var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
					bool isLayerActive = subLayerCoreOptions.FindPropertyRelative("isActive").boolValue;
					if (!isLayerActive)
					{
						GUI.enabled = false;
					}

					DrawLayerVisualizerProperties(sourceTypeValue, layerProperty, property);
					if (!isLayerActive)
					{
						GUI.enabled = true;
					}
				}
				else
				{
					GUILayout.Label("Select a visualizer to see properties");
				}
			}
		}

		void DrawLayerVisualizerProperties(VectorSourceType sourceType, SerializedProperty layerProperty, SerializedProperty property)
		{
			var subLayerCoreOptions = layerProperty.FindPropertyRelative("coreOptions");
			GUILayout.Space(-_lineHeight);
			EditorGUILayout.PrefixLabel(subLayerCoreOptions.FindPropertyRelative("sublayerName").stringValue + " Properties");
			// V1
			EditorGUILayout.BeginVertical();
			EditorGUI.indentLevel++;
			VectorPrimitiveType primitiveTypeProp =
				(VectorPrimitiveType)subLayerCoreOptions.FindPropertyRelative("geometryType").enumValueIndex;

			GUILayout.Space(-_lineHeight);
			EditorGUILayout.PropertyField(subLayerCoreOptions);

			var extrusionOptions = layerProperty.FindPropertyRelative("extrusionOptions");
			//loading up the selectedLayerName for extrusion options to pull up the right propertyName
			extrusionOptions.FindPropertyRelative("_selectedLayerName").stringValue = subLayerCoreOptions.FindPropertyRelative("layerName").stringValue;

			if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
			{
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("colliderOptions"));
				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("extrusionOptions"));
				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("materialOptions"));
			}

			//EditorGUI.indentLevel--;
			ShowOthers = EditorGUILayout.Foldout(ShowOthers, "Advanced");
			EditorGUI.indentLevel++;
			if (ShowOthers)
			{
				if (primitiveTypeProp == VectorPrimitiveType.Polygon && sourceType != VectorSourceType.MapboxStreets)
				{
					EditorGUI.indentLevel--;
					layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue = true;
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("buildingsWithUniqueIds"), new GUIContent
					{
						text = "Buildings With Unique Ids",
						tooltip =
							"Turn on this setting only when rendering 3D buildings from the Mapbox Streets with Building Ids tileset. Using this setting with any other polygon layers or source will result in visual artifacts. "
					});
					EditorGUI.indentLevel++;
				}
				else
				{
					layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue = false;
				}
				var filterOptions = layerProperty.FindPropertyRelative("filterOptions");
				filterOptions.FindPropertyRelative("_selectedLayerName").stringValue = subLayerCoreOptions.FindPropertyRelative("layerName").stringValue;

				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(filterOptions, new GUIContent("Filters"));
				DrawModifiers(layerProperty, new GUIContent { text = "Modifier Options", tooltip = "Additional Feature modifiers to apply to the visualizer. " });
			}
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel--;
		}

		void DrawModifiers(SerializedProperty property, GUIContent label)
		{
			var groupFeaturesProperty = property.FindPropertyRelative("coreOptions").FindPropertyRelative("groupFeatures");
			ShowPosition = EditorGUILayout.Foldout(ShowPosition, label.text);
			EditorGUILayout.BeginVertical();
			if (ShowPosition)
			{
				EditorGUILayout.BeginHorizontal();
				if (groupFeaturesProperty.boolValue == false)
				{
					EditorGUILayout.PrefixLabel(new GUIContent
					{
						text = "Feature Position",
						tooltip = "Position to place feature in the tile. "
					});
					var featurePositionProperty = property.FindPropertyRelative("moveFeaturePositionTo");
					featurePositionProperty.enumValueIndex = EditorGUILayout.Popup(featurePositionProperty.enumValueIndex,
						featurePositionProperty.enumDisplayNames);
				}

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

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
						meshfac.DeleteArrayElementAtIndex(ind);
						meshfac.DeleteArrayElementAtIndex(ind);
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
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
				EditorGUI.indentLevel--;
				EditorGUILayout.Space();

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
						gofac.DeleteArrayElementAtIndex(ind);
						gofac.DeleteArrayElementAtIndex(ind);
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUI.indentLevel * 12);
				//buttonRect = GUILayoutUtility.GetLastRect();
				if (GUILayout.Button(new GUIContent("Add New"), (GUIStyle)"minibuttonleft"))
				{
					PopupWindow.Show(buttonRect, new PopupSelectionMenu(typeof(GameObjectModifier), gofac));
					if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
				}
				//EditorWindow.Repaint();
				//buttonRect = GUILayoutUtility.GetLastRect();
				if (GUILayout.Button(new GUIContent("Add Existing"), (GUIStyle)"minibuttonright"))
				{
					
					ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
		}

		private void LoadEditorTileJSON(SerializedProperty property, VectorSourceType sourceTypeValue, string sourceString)
		{
			if (sourceTypeValue != VectorSourceType.None && !string.IsNullOrEmpty(sourceString))
			{
				if (tileJSONResponse == null || string.IsNullOrEmpty(sourceString) || sourceString != TilesetId)
				{
					//tileJSONData.ClearData();
					try
					{
						Unity.MapboxAccess.Instance.TileJSON.Get(sourceString, (response) =>
						{
							//if the code has reached this point it means that there is a valid access token
							tileJSONResponse = response;
							if (response == null || response.VectorLayers == null) //indicates bad tileresponse
							{
								tileJSONData.ClearData();
								return;
							}
							tileJSONData.ProcessTileJSONData(response);
						});
					}
					catch (System.Exception)
					{
						//no valid access token causes MapboxAccess to throw an error and hence setting this property
						tileJSONData.ClearData();
					}
				}
				else if (tileJSONData.LayerPropertyDescriptionDictionary.Count == 0)
				{
					tileJSONData.ProcessTileJSONData(tileJSONResponse);
				}
			}
			else
			{
				tileJSONData.ClearData();
			}
			TilesetId = sourceString;
		}
	}
}
