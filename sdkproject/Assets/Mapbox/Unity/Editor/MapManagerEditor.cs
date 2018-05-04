namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;

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

		private GUIContent _mapIdGui = new GUIContent
		{
			text = "Required Map Id",
			tooltip = "For location prefabs to spawn the \"streets-v7\" tileset needs to be a part of the Vector data source"
		};

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

			ShowLocationPrefabs = EditorGUILayout.Foldout(ShowLocationPrefabs, "LOCATION PREFABS");
			if (ShowLocationPrefabs)
			{
				var vectorDataProperty = serializedObject.FindProperty("_vectorData");


				var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
				var layerSourceProperty = layerProperty.FindPropertyRelative("sourceOptions");
				var sourceType = layerProperty.FindPropertyRelative("_sourceType");
				VectorSourceType sourceTypeValue = (VectorSourceType)sourceType.enumValueIndex;
				string streets_v7 = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id;
				string layerString = layerProperty.FindPropertyRelative("sourceOptions.layerSource.Id").stringValue;

				if(sourceTypeValue != VectorSourceType.None && layerString.Contains(streets_v7))
				{
					GUI.enabled = false;
					EditorGUILayout.TextField(_mapIdGui, streets_v7);
					GUI.enabled = true;
					ShowSection(vectorDataProperty, "_locationPrefabsLayerProperties");
				}
				else
				{
					EditorGUILayout.HelpBox("In order to place location prefabs please add \"mapbox.mapbox-streets-v7\" to the data source in the Vector section.",MessageType.Error);
				}
			}

			ShowSepartor();

			ShowVector = EditorGUILayout.Foldout(ShowVector, "VECTOR");
			if (ShowVector)
			{
				ShowSection(serializedObject.FindProperty("_vectorData"), "_layerProperty");
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
