namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomEditor(typeof(AbstractMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditor : Editor
	{
		/// <summary>
		/// Gets or sets a value indicating whether to show general section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> then show general section; otherwise hide, <c>false</c>.</value>
		bool ShowGeneral
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showGeneral");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showGeneral", value);
			}
		}
		/// <summary>
		/// Gets or sets a value to show or hide Image section<see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show image; otherwise, <c>false</c>.</value>
		bool ShowImage
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showImage");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showImage", value);
			}
		}
		/// <summary>
		/// Gets or sets a value to show or hide Terrain section <see cref="T:Mapbox.Editor.MapManagerEditor"/>
		/// </summary>
		/// <value><c>true</c> if show terrain; otherwise, <c>false</c>.</value>
		bool ShowTerrain
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showTerrain");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showTerrain", value);
			}
		}

		/// <summary>
		/// Gets or sets a value show or hide Map Layers section <see cref="T:Mapbox.Editor.MapManagerEditor"/> show features.
		/// </summary>
		/// <value><c>true</c> if show features; otherwise, <c>false</c>.</value>
		bool ShowMapLayers
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showMapLayers");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showMapLayers", value);
			}
		}

		/// <summary>
		/// Gets or sets a value to show or hide Vector section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show vector; otherwise, <c>false</c>.</value>
		bool ShowLocationPrefabs
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showLocationPrefabs");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showLocationPrefabs", value);
			}
		}

		/// <summary>
		/// Gets or sets a value to show or hide Vector section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show vector; otherwise, <c>false</c>.</value>
		bool ShowFeatures
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showVector");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showVector", value);
			}
		}


		bool ShowPosition
		{
			get
			{
				return EditorPrefs.GetBool("MapManagerEditor_showPosition");
			}
			set
			{
				EditorPrefs.SetBool("MapManagerEditor_showPosition", value);
			}
		}

		private GUIContent _requiredMapIdGui = new GUIContent
		{
			text = "Required Map Id",
			tooltip = "For location prefabs to spawn the \"streets-v7\" tileset needs to be a part of the Vector data source"
		};

		private GUIContent mapIdGui = new GUIContent
		{
			text = "Map Id",
			tooltip = "Map Id corresponding to the tileset."
		};

		string CustomSourceMapId
		{
			get { return EditorPrefs.GetString("VectorLayerProperties_customSourceMapId"); }
			set { EditorPrefs.SetString("VectorLayerProperties_customSourceMapId", value); }
		}

		bool _isGUIContentSet = false;
		GUIContent[] _sourceTypeContent;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUILayout.BeginVertical();
			EditorGUILayout.Space();

			ShowGeneral = EditorGUILayout.Foldout(ShowGeneral, new GUIContent { text = "GENERAL", tooltip = "Options related to map data" });
			if (ShowGeneral)
			{
				EditorGUILayout.Space();
				DrawMapOptions(serializedObject);
			}

			ShowSepartor();

			ShowImage = EditorGUILayout.Foldout(ShowImage, "IMAGE");
			if (ShowImage)
			{
				ShowSection(serializedObject.FindProperty("_imagery"), "_layerProperty");
			}

			ShowSepartor();

			ShowTerrain = EditorGUILayout.Foldout(ShowTerrain, "TERRAIN");
			if (ShowTerrain)
			{
				ShowSection(serializedObject.FindProperty("_terrain"), "_layerProperty");
			}

			ShowSepartor();

			ShowMapLayers = EditorGUILayout.Foldout(ShowMapLayers, "MAP LAYERS");
			if (ShowMapLayers)
			{
				EditorGUI.indentLevel++;
				var vectorDataProperty = serializedObject.FindProperty("_vectorData");
				var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
				var layerSourceProperty = layerProperty.FindPropertyRelative("sourceOptions");
				var sourceTypeProperty = layerProperty.FindPropertyRelative("_sourceType");
				VectorSourceType sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;
				string streets_v7 = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id;
				var layerSourceId = layerProperty.FindPropertyRelative("sourceOptions.layerSource.Id");
				string layerString = layerSourceId.stringValue;
				var isActiveProperty = layerSourceProperty.FindPropertyRelative("isActive");

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

				sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(new GUIContent
					{
						text = "Data Source",
						tooltip = "Source tileset for Vector Data"
					},sourceTypeProperty.enumValueIndex, _sourceTypeContent);

				sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;

				switch (sourceTypeValue)
				{
					case VectorSourceType.MapboxStreets:
					case VectorSourceType.MapboxStreetsWithBuildingIds:
						var sourcePropertyValue = MapboxDefaultVector.GetParameters(sourceTypeValue);
						layerSourceId.stringValue = sourcePropertyValue.Id;
						GUI.enabled = false;
						EditorGUILayout.PropertyField(layerSourceProperty, mapIdGui);
						GUI.enabled = true;
						isActiveProperty.boolValue = true;
						break;
					case VectorSourceType.Custom:
						layerSourceId.stringValue = CustomSourceMapId;
						EditorGUILayout.PropertyField(layerSourceProperty, mapIdGui);
						CustomSourceMapId = layerSourceId.stringValue;
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
					var isStyleOptimized = layerProperty.FindPropertyRelative("useOptimizedStyle");
					EditorGUILayout.PropertyField(isStyleOptimized);

					if (isStyleOptimized.boolValue)
					{
						EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("optimizedStyle"), new GUIContent("Style Options"));
					}

					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Options"));
				}
				EditorGUILayout.Space();
				ShowSepartor();

				ShowLocationPrefabs = EditorGUILayout.Foldout(ShowLocationPrefabs, "POINTS OF INTEREST");
				if (ShowLocationPrefabs)
				{
					if (sourceTypeValue != VectorSourceType.None && layerString.Contains(streets_v7))
					{
						GUI.enabled = false;
						EditorGUILayout.TextField(_requiredMapIdGui, streets_v7);
						GUI.enabled = true;
						ShowSection(vectorDataProperty, "_locationPrefabsLayerProperties");
					}
					else
					{
						EditorGUILayout.HelpBox("In order to place location prefabs please add \"mapbox.mapbox-streets-v7\" to the data source in the FEATURES section.", MessageType.Error);
					}
				}
				ShowSepartor();
				ShowFeatures = EditorGUILayout.Foldout(ShowFeatures, "FEATURES");
				if (ShowFeatures)
				{
					ShowSection(serializedObject.FindProperty("_vectorData"), "_layerProperty");
				}
				EditorGUI.indentLevel--;
			}
			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}

		void ShowSection(SerializedProperty property, string propertyName)
		{
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(property.FindPropertyRelative(propertyName));
		}

		void ShowSepartor()
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.Space();
		}

		void DrawMapOptions(SerializedObject mapObject)
		{
			var property = mapObject.FindProperty("_options");

			EditorGUILayout.LabelField("Location ");
			EditorGUILayout.PropertyField(property.FindPropertyRelative("locationOptions"));
			var extentOptions = property.FindPropertyRelative("extentOptions");
			var extentOptionsType = extentOptions.FindPropertyRelative("extentType");
			if ((MapExtentType)extentOptionsType.enumValueIndex == MapExtentType.Custom)
			{

				var test = mapObject.FindProperty("_tileProvider");
				EditorGUILayout.PropertyField(extentOptionsType);
				EditorGUILayout.PropertyField(test);
			}
			else
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("extentOptions"));
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("_initializeOnStart"));

			ShowPosition = EditorGUILayout.Foldout(ShowPosition, "Others");
			if (ShowPosition)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("placementOptions"));
				EditorGUILayout.PropertyField(property.FindPropertyRelative("scalingOptions"));
				EditorGUILayout.PropertyField(property.FindPropertyRelative("loadingTexture"));
			}
		}

		void PresetLocationBased(SerializedProperty unifiedMap)
		{
			//Set
			//placement = atLocationCenter,
			//scaling = custom
			//turn off vector layers.
			var mapOptionsProp = unifiedMap.FindPropertyRelative("mapOptions");
			var vectorLayerProps = unifiedMap.FindPropertyRelative("vectorLayerProperties");
			var placementType = mapOptionsProp.FindPropertyRelative("placementOptions.placementType");
			var scalingType = mapOptionsProp.FindPropertyRelative("scalingOptions.scalingType");
			var unitType = mapOptionsProp.FindPropertyRelative("scalingOptions.unitType");
			var extentType = mapOptionsProp.FindPropertyRelative("extentOptions.extentType");
			var vectorSourceType = vectorLayerProps.FindPropertyRelative("sourceType");

			placementType.enumValueIndex = (int)MapPlacementType.AtLocationCenter;

			scalingType.enumValueIndex = (int)MapScalingType.Custom;
			unitType.enumValueIndex = (int)MapUnitType.meters;

			extentType.enumValueIndex = (int)MapExtentType.CameraBounds;

			vectorSourceType.enumValueIndex = (int)VectorSourceType.None;

		}

		void PresetWorldSimulator(SerializedProperty unifiedMap)
		{
			//Set
			//placement = atLocationCenter,
			//scaling = custom
			//turn on vector layers.
			var mapOptionsProp = unifiedMap.FindPropertyRelative("mapOptions");
			var vectorLayerProps = unifiedMap.FindPropertyRelative("vectorLayerProperties");
			var placementType = mapOptionsProp.FindPropertyRelative("placementOptions.placementType");
			var scalingType = mapOptionsProp.FindPropertyRelative("scalingOptions.scalingType");
			var unitType = mapOptionsProp.FindPropertyRelative("scalingOptions.unitType");
			var extentType = mapOptionsProp.FindPropertyRelative("extentOptions.extentType");
			var vectorSourceType = vectorLayerProps.FindPropertyRelative("sourceType");

			placementType.enumValueIndex = (int)MapPlacementType.AtLocationCenter;

			scalingType.enumValueIndex = (int)MapScalingType.Custom;
			unitType.enumValueIndex = (int)MapUnitType.meters;

			extentType.enumValueIndex = (int)MapExtentType.CameraBounds;

			vectorSourceType.enumValueIndex = (int)VectorSourceType.MapboxStreets;
		}

		void PresetARTableTop(SerializedProperty unifiedMap)
		{
			//Set
			//placement = atLocationCenter,
			//scaling = custom
			//turn on vector layers.
			var mapOptionsProp = unifiedMap.FindPropertyRelative("mapOptions");
			var vectorLayerProps = unifiedMap.FindPropertyRelative("vectorLayerProperties");
			var placementType = mapOptionsProp.FindPropertyRelative("placementOptions.placementType");
			var scalingType = mapOptionsProp.FindPropertyRelative("scalingOptions.scalingType");
			var extentType = mapOptionsProp.FindPropertyRelative("extentOptions.extentType");
			var vectorSourceType = vectorLayerProps.FindPropertyRelative("sourceType");

			placementType.enumValueIndex = (int)MapPlacementType.AtLocationCenter;

			scalingType.enumValueIndex = (int)MapScalingType.WorldScale;

			extentType.enumValueIndex = (int)MapExtentType.CameraBounds;

			vectorSourceType.enumValueIndex = (int)VectorSourceType.MapboxStreets;

		}

		void PresetARWorldScale(SerializedProperty unifiedMap)
		{
			//Set
			//placement = atLocationCenter,
			//scaling = custom
			//turn on vector layers.
			var mapOptionsProp = unifiedMap.FindPropertyRelative("mapOptions");
			var vectorLayerProps = unifiedMap.FindPropertyRelative("vectorLayerProperties");
			var placementType = mapOptionsProp.FindPropertyRelative("placementOptions.placementType");
			var scalingType = mapOptionsProp.FindPropertyRelative("scalingOptions.scalingType");
			var extentType = mapOptionsProp.FindPropertyRelative("extentOptions.extentType");
			var vectorSourceType = vectorLayerProps.FindPropertyRelative("sourceType");

			placementType.enumValueIndex = (int)MapPlacementType.AtLocationCenter;

			scalingType.enumValueIndex = (int)MapScalingType.WorldScale;

			extentType.enumValueIndex = (int)MapExtentType.CameraBounds;

			vectorSourceType.enumValueIndex = (int)VectorSourceType.MapboxStreets;

		}
	}
}
