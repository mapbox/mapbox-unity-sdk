namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Platform.TilesetTileJSON;
	using System.Collections.Generic;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomEditor(typeof(AbstractMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditor : Editor
	{

		private string objectId = "";
		/// <summary>
		/// Gets or sets a value indicating whether to show general section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> then show general section; otherwise hide, <c>false</c>.</value>
		bool ShowGeneral
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "MapManagerEditor_showGeneral");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "MapManagerEditor_showGeneral", value);
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
				return EditorPrefs.GetBool(objectId + "MapManagerEditor_showImage");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "MapManagerEditor_showImage", value);
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
				return EditorPrefs.GetBool(objectId + "MapManagerEditor_showTerrain");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "MapManagerEditor_showTerrain", value);
			}
		}

		/// <summary>
		/// Gets or sets a value to show or hide Map Layers section <see cref="T:Mapbox.Editor.MapManagerEditor"/> show features.
		/// </summary>
		/// <value><c>true</c> if show features; otherwise, <c>false</c>.</value>
		bool ShowMapLayers
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "MapManagerEditor_showMapLayers");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "MapManagerEditor_showMapLayers", value);
			}
		}

		bool ShowPosition
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "MapManagerEditor_showPosition");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "MapManagerEditor_showPosition", value);
			}
		}

		private GUIContent mapIdGui = new GUIContent
		{
			text = "Map Id",
			tooltip = "Map Id corresponding to the tileset."
		};

		bool _isGUIContentSet = false;
		GUIContent[] _sourceTypeContent;
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnInspectorGUI()
		{
			objectId = serializedObject.targetObject.GetInstanceID().ToString();
			serializedObject.Update();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.Space();

			ShowGeneral = EditorGUILayout.Foldout(ShowGeneral, new GUIContent { text = "GENERAL", tooltip = "Options related to map data" });

			if (ShowGeneral)
			{
				DrawMapOptions(serializedObject);
			}
			ShowSepartor();

			ShowImage = EditorGUILayout.Foldout(ShowImage, "IMAGE");
			if (ShowImage)
			{
				GUILayout.Space(-1.5f * _lineHeight);
				ShowSection(serializedObject.FindProperty("_imagery"), "_layerProperty");
			}

			ShowSepartor();

			ShowTerrain = EditorGUILayout.Foldout(ShowTerrain, "TERRAIN");
			if (ShowTerrain)
			{
				GUILayout.Space(-1.5f * _lineHeight);
				ShowSection(serializedObject.FindProperty("_terrain"), "_layerProperty");
			}

			ShowSepartor();

			ShowMapLayers = EditorGUILayout.Foldout(ShowMapLayers, "MAP LAYERS");
			if (ShowMapLayers)
			{
				DrawMapLayerOptions();
			}
			EditorGUILayout.EndVertical();

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
			if (!((AbstractMap)serializedObject.targetObject).IsAccessTokenValid)
			{
				EditorGUILayout.HelpBox("Invalid Access Token. Please add a valid access token using the Mapbox  > Setup Menu", MessageType.Error);
			}

			EditorGUILayout.LabelField("Location ", GUILayout.Height(_lineHeight));

			EditorGUILayout.PropertyField(property.FindPropertyRelative("locationOptions"));


			var extentOptions = property.FindPropertyRelative("extentOptions");
			var extentOptionsType = extentOptions.FindPropertyRelative("extentType");


			if ((MapExtentType)extentOptionsType.enumValueIndex == MapExtentType.Custom)
			{
				var tileProviderProperty = mapObject.FindProperty("_tileProvider");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(extentOptionsType);
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(extentOptions);
				}
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(tileProviderProperty);
				EditorGUI.indentLevel--;
			}
			else
			{
				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(extentOptions);
			}

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("_initializeOnStart"));

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			ShowPosition = EditorGUILayout.Foldout(ShowPosition, "Others");
			if (ShowPosition)
			{
				GUILayout.Space(-_lineHeight);

				EditorGUI.BeginChangeCheck();
				var placementOptions = property.FindPropertyRelative("placementOptions");
				EditorGUILayout.PropertyField(placementOptions);
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(placementOptions);
				}

				GUILayout.Space(-_lineHeight);

				EditorGUI.BeginChangeCheck();
				var scalingOptions = property.FindPropertyRelative("scalingOptions");
				EditorGUILayout.PropertyField(scalingOptions);
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(scalingOptions);
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(property.FindPropertyRelative("loadingTexture"));
				EditorGUILayout.PropertyField(property.FindPropertyRelative("tileMaterial"));
				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(property);
				}
			}
		}

		void DrawMapLayerOptions()
		{
			var vectorDataProperty = serializedObject.FindProperty("_vectorData");
			var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
			var layerSourceProperty = layerProperty.FindPropertyRelative("sourceOptions");
			var sourceTypeProperty = layerProperty.FindPropertyRelative("_sourceType");
			VectorSourceType sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;
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

			EditorGUI.BeginChangeCheck();
			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(new GUIContent
			{
				text = "Data Source",
				tooltip = "Source tileset for Vector Data"
			}, sourceTypeProperty.enumValueIndex, _sourceTypeContent);

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
					EditorGUILayout.PropertyField(layerSourceProperty, mapIdGui);
					isActiveProperty.boolValue = true;
					break;
				case VectorSourceType.None:
					isActiveProperty.boolValue = false;
					break;
				default:
					isActiveProperty.boolValue = false;
					break;
			}

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(layerProperty);
			}

			if (sourceTypeValue != VectorSourceType.None)
			{
				var isStyleOptimized = layerProperty.FindPropertyRelative("useOptimizedStyle");
				EditorGUILayout.PropertyField(isStyleOptimized);

				if (isStyleOptimized.boolValue)
				{
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("optimizedStyle"), new GUIContent("Style Options"));
				}
				GUILayout.Space(-_lineHeight);
				EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Options"));
			}

			EditorGUILayout.Space();
			ShowSepartor();

			GUILayout.Space(-2.0f * _lineHeight);
			ShowSection(serializedObject.FindProperty("_vectorData"), "_layerProperty");
		}
	}
}
