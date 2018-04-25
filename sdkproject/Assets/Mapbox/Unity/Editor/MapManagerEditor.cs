namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Platform.TilesetTileJSON;
	using System.Collections.Generic;

	[CustomEditor(typeof(AbstractMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditor : Editor
	{
		private Rect buttonRect;
		/// <summary>
		/// Gets or sets the layerID
		/// </summary>
		/// <value><c>true</c> then show general section; otherwise hide, <c>false</c>.</value>
		private string TilesetId
		{
			get
			{
				return EditorPrefs.GetString("MapManagerEditor_tilesetId");
			}
			set
			{
				EditorPrefs.SetString("MapManagerEditor_tilesetId", value);
			}
		}


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
		/// Gets or sets a value to show or hide Vector section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show vector; otherwise, <c>false</c>.</value>
		bool ShowVector
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

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUILayout.BeginVertical();
			EditorGUILayout.Space();

			ShowGeneral = EditorGUILayout.Foldout(ShowGeneral, new GUIContent { text = "GENERAL", tooltip = "Options related to map data" });
			if (ShowGeneral)
			{
				//EditorGUILayout.Space();
				//EditorGUILayout.LabelField("Presets");
				//selected = property.FindPropertyRelative("mapPreset").enumValueIndex;
				//var options = property.FindPropertyRelative("mapPreset").enumDisplayNames;

				//GUIContent[] content = new GUIContent[options.Length];
				//for (int i = 0; i < options.Length; i++)
				//{
				//	content[i] = new GUIContent();
				//	content[i].text = options[i];
				//	content[i].tooltip = EnumExtensions.Description((MapPresetType)i);
				//}
				//selected = property.FindPropertyRelative("mapPreset").enumValueIndex;
				//selected = GUILayout.SelectionGrid(selected, content, options.Length);


				//if (selected != previousSelection)
				//{
				//	previousSelection = selected;
				//	property.FindPropertyRelative("mapPreset").enumValueIndex = selected;

				//	switch ((MapPresetType)selected)
				//	{
				//		case MapPresetType.LocationBasedMap:
				//			PresetLocationBased(property);

				//			//TODO : Get opinions on this UX. 
				//			//var locationProvider = _map.gameObject.GetComponent<LocationProviderFactory>();
				//			//Debug.Log("target -> " + ((locationProvider == null) ? "null" : "notnull"));
				//			//if (locationProvider == null)
				//			//(_map.gameObject).AddComponent<LocationProviderFactory>();
				//			break;
				//		case MapPresetType.WorldSimulator:
				//			PresetWorldSimulator(property);
				//			break;
				//		case MapPresetType.ARTableTop:
				//			break;
				//		case MapPresetType.ARWorldScale:
				//			PresetARWorldScale(property);
				//			break;
				//		default:
				//			break;
				//	}

				//}

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

			ShowVector = EditorGUILayout.Foldout(ShowVector, "VECTOR");
			if (ShowVector)
			{
				var vectorDataProperty = serializedObject.FindProperty("_vectorData");


				var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
				var layerSourceProperty = layerProperty.FindPropertyRelative("sourceOptions");
				var sourceType = layerProperty.FindPropertyRelative("sourceType");
				VectorSourceType sourceTypeValue = (VectorSourceType)sourceType.enumValueIndex;
				string layerString = layerProperty.FindPropertyRelative("sourceOptions.layerSource.Id").stringValue;

				if (sourceTypeValue != VectorSourceType.None && !string.IsNullOrEmpty(layerString))
				{
					if (string.IsNullOrEmpty(TilesetId) || layerString != TilesetId)
					{
						EditorTileJSONData tileJSONData = EditorTileJSONData.Instance;
						tileJSONData.tileJSONLoaded = false;
						TilesetId = layerString;
						Unity.MapboxAccess.Instance.TileJSON.Get(layerString,ProcessTileJSONData);
					}
				}
				ShowSection(vectorDataProperty, "_layerProperty");
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

		private void ProcessTileJSONData(TileJSONResponse tjr)
		{
			TileJSONResponse response = tjr;
			EditorTileJSONData tileJSONData = EditorTileJSONData.Instance;
			tileJSONData.ClearData();

			List<string> layerPropertiesList = new List<string>();
			List<string> sourceLayersList = new List<string>();

			if (response==null || response.VectorLayers == null || response.VectorLayers.Length == 0)
			{
				return;
			}

			var propertyName = "";
			var propertyDescription = "";
			var layerSource = "";

			foreach (var layer in response.VectorLayers)
			{
				var layerName = layer.Id;
				layerPropertiesList = new List<string>();
				layerSource = layer.Source;

				if (layer.Fields.Count == 0)
					continue;
				
				foreach (var property in layer.Fields)
				{
					propertyName = property.Key;
					propertyDescription = property.Value;
					layerPropertiesList.Add(propertyName);

					//adding property descriptions
					if (tileJSONData.LayerPropertyDescriptionDictionary.ContainsKey(layerName))
					{
						if (tileJSONData.LayerPropertyDescriptionDictionary[layerName].ContainsKey(propertyName))
						{
							tileJSONData.LayerPropertyDescriptionDictionary[layerName][propertyName] = propertyDescription;
						}
						else
						{
							tileJSONData.LayerPropertyDescriptionDictionary[layerName].Add(propertyName, propertyDescription);
						}
					}
					else
					{
						tileJSONData.LayerPropertyDescriptionDictionary.Add(layerName, new Dictionary<string, string>() { { propertyName, propertyDescription } });
					}
				}

				//loading layer sources
				if (tileJSONData.LayerSourcesDictionary.ContainsKey(layerName))
				{
					tileJSONData.LayerSourcesDictionary[layerName].Add(layerSource);
				}
				else
				{
					tileJSONData.LayerSourcesDictionary.Add(layerName, new List<string>() { layerSource });
				}

				//loading layers to a data source
				if (tileJSONData.SourceLayersDictionary.ContainsKey(layerSource))
				{
					string commonLayersKey = tileJSONData.commonLayersKey;
					List<string> sourceList = new List<string>();
					tileJSONData.LayerSourcesDictionary.TryGetValue(layerName, out sourceList);

					if (sourceList.Count > 1 && sourceList.Contains(layerSource)) // the current layerName has more than one source
					{
						if (tileJSONData.SourceLayersDictionary.ContainsKey(commonLayersKey))
						{
							tileJSONData.SourceLayersDictionary[commonLayersKey].Add(layerName);
						}
						else
						{
							tileJSONData.SourceLayersDictionary.Add(commonLayersKey, new List<string>() { layerName });
						}

						//remove the layer from other different sources
						foreach (var source in sourceList)
						{
							tileJSONData.SourceLayersDictionary[source].Remove(layerName);

							//if the source contains zero layers remove th source from the list
							if (tileJSONData.SourceLayersDictionary[source].Count == 0)
								tileJSONData.SourceLayersDictionary.Remove(source);
						}
					}
					else
					{
						tileJSONData.SourceLayersDictionary[layerSource].Add(layerName);
					}
				}
				else
				{
					tileJSONData.SourceLayersDictionary.Add(layerSource, new List<string>() { layerName });
				}
			}
			
			tileJSONData.tileJSONLoaded = true;
			Debug.Log(tileJSONData);
		}
	}
}
