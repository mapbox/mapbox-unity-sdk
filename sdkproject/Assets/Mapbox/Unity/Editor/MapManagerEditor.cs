using System;
using Mapbox.Unity.DataContainers;

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

		private GUIContent tilesetIdGui = new GUIContent
		{
			text = "Tileset Id",
			tooltip = "Id of the tileset."
		};

		bool _isGUIContentSet = false;
		GUIContent[] _sourceTypeContent;
		static float _lineHeight = EditorGUIUtility.singleLineHeight;

		VectorLayerPropertiesDrawer _vectorLayerDrawer = new VectorLayerPropertiesDrawer();

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

			var _mapVisualizerProperty = serializedObject.FindProperty("_mapVisualizer");
			var _mapVisualizerObject = new SerializedObject(_mapVisualizerProperty.objectReferenceValue);

			ShowImage = EditorGUILayout.Foldout(ShowImage, "IMAGE");
			if (ShowImage)
			{
				GUILayout.Space(-1.5f * _lineHeight);
				ShowSection(_mapVisualizerObject.FindProperty("ImageryLayer"), "_layerProperty");
			}

			ShowSepartor();

			ShowTerrain = EditorGUILayout.Foldout(ShowTerrain, "TERRAIN");
			if (ShowTerrain)
			{
				GUILayout.Space(-1.5f * _lineHeight);
				ShowSection(_mapVisualizerObject.FindProperty("TerrainLayer"), "_layerProperty");
			}

			ShowSepartor();

			ShowMapLayers = EditorGUILayout.Foldout(ShowMapLayers, "MAP LAYERS");
			if (ShowMapLayers)
			{
				DrawMapLayerOptions();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
			var vectorDataProperty = _mapVisualizerObject.FindProperty("VectorLayer");
			var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
			_vectorLayerDrawer.PostProcessLayerProperties(layerProperty);
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
			var property = mapObject.FindProperty("Options");
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
			var _mapVisualizerProperty = serializedObject.FindProperty("_mapVisualizer");
			var _mapVisualizerObject = new SerializedObject(_mapVisualizerProperty.objectReferenceValue);

			var vectorDataProperty = _mapVisualizerObject.FindProperty("VectorLayer");
			var layerProperty = vectorDataProperty.FindPropertyRelative("_layerProperty");
			var layerSourceProperty = layerProperty.FindPropertyRelative("sourceOptions");
			var sourceTypeProperty = layerProperty.FindPropertyRelative("_sourceType");
			VectorSourceType sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;
			var layerSourceId = layerProperty.FindPropertyRelative("sourceOptions.layerSource.Id");
			string layerString = layerSourceId.stringValue;
			var isActiveProperty = layerSourceProperty.FindPropertyRelative("isActive");

			var displayNames = sourceTypeProperty.enumDisplayNames;
			var names = sourceTypeProperty.enumNames;
			int count = sourceTypeProperty.enumDisplayNames.Length;
			if (!_isGUIContentSet)
			{
				_sourceTypeContent = new GUIContent[count];

				var index = 0;
				foreach (var name in names)
				{
					_sourceTypeContent[index] = new GUIContent
					{
						text = displayNames[index],
						tooltip = ((VectorSourceType)Enum.Parse(typeof(VectorSourceType), name)).Description(),
					};
					index++;
				}
				//
				//				for (int extIdx = 0; extIdx < count; extIdx++)
				//				{
				//					_sourceTypeContent[extIdx] = new GUIContent
				//					{
				//						text = displayNames[extIdx],
				//						tooltip = ((VectorSourceType)extIdx).Description(),
				//					};
				//				}

				_isGUIContentSet = true;
			}

			EditorGUI.BeginChangeCheck();
			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(new GUIContent
			{
				text = "Data Source",
				tooltip = "Source tileset for Vector Data"
			}, sourceTypeProperty.enumValueIndex, _sourceTypeContent);

			//sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;
			sourceTypeValue = ((VectorSourceType)Enum.Parse(typeof(VectorSourceType), names[sourceTypeProperty.enumValueIndex]));

			switch (sourceTypeValue)
			{
				case VectorSourceType.MapboxStreets:
				case VectorSourceType.MapboxStreetsV8:
				case VectorSourceType.MapboxStreetsWithBuildingIds:
				case VectorSourceType.MapboxStreetsV8WithBuildingIds:
					var sourcePropertyValue = MapboxDefaultVector.GetParameters(sourceTypeValue);
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.PropertyField(layerSourceProperty, tilesetIdGui);
					GUI.enabled = true;
					isActiveProperty.boolValue = true;
					break;
				case VectorSourceType.Custom:
					EditorGUILayout.PropertyField(layerSourceProperty, tilesetIdGui);
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
				GUILayout.Space(-_lineHeight);

			}

			EditorGUILayout.Space();
			ShowSepartor();

			_vectorLayerDrawer.DrawUI(layerProperty);

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(layerProperty);
			}
		}
	}
}
