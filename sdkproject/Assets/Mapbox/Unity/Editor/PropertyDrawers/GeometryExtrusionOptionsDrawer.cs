﻿namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;
	using System.Linq;
	using Mapbox.Platform.TilesetTileJSON;
	using System.Collections.Generic;

	[CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
	public class GeometryExtrusionOptionsDrawer : PropertyDrawer
	{
		//indices for tileJSON lookup
		int _propertyIndex = 0;

		bool _isInitialized = false;
		private static List<string> propertyNamesList = new List<string>();
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] extrusionTypeContent;
		bool isGUIContentSet = false;
		GUIContent[] _propertyNameContent;
		bool _isLayerNameGUIContentSet = false;
		static TileJsonData tileJsonData = new TileJsonData();
		static TileJSONResponse tileJsonResponse;
		static bool dataUnavailable = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var displayNames = extrusionTypeProperty.enumDisplayNames;
			int count = extrusionTypeProperty.enumDisplayNames.Length;

			if (!isGUIContentSet)
			{
				extrusionTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					extrusionTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ExtrusionType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			var extrusionTypeLabel = new GUIContent
			{
				text = "Extrusion Type",
				tooltip = "Type of geometry extrusion"
			};

			extrusionTypeProperty.enumValueIndex = EditorGUILayout.Popup(extrusionTypeLabel, extrusionTypeProperty.enumValueIndex, extrusionTypeContent);

			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			var minHeightProperty = property.FindPropertyRelative("minimumHeight");
			var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

			var extrusionGeometryType = property.FindPropertyRelative("extrusionGeometryType");
			var extrusionGeometryGUI = new GUIContent { text = "Extrusion Geometry Type", tooltip = EnumExtensions.Description((Unity.Map.ExtrusionGeometryType)extrusionGeometryType.enumValueIndex) };
			EditorGUI.indentLevel++;
			switch (sourceTypeValue)
			{
				case Unity.Map.ExtrusionType.None:
					break;
				case Unity.Map.ExtrusionType.PropertyHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					DrawPropertyDropDown(property, position);
					break;
				case Unity.Map.ExtrusionType.MinHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					DrawPropertyDropDown(property, position);
					break;
				case Unity.Map.ExtrusionType.MaxHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					DrawPropertyDropDown(property, position);
					break;
				case Unity.Map.ExtrusionType.RangeHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					DrawPropertyDropDown(property, position);
					EditorGUILayout.PropertyField(minHeightProperty);
					EditorGUILayout.PropertyField(maxHeightProperty);
					if (minHeightProperty.floatValue > maxHeightProperty.floatValue)
					{
						EditorGUILayout.HelpBox("Maximum Height less than Minimum Height!", MessageType.Error);
					}
					break;
				case Unity.Map.ExtrusionType.AbsoluteHeight:
					EditorGUILayout.PropertyField(extrusionGeometryType, extrusionGeometryGUI);
					EditorGUILayout.PropertyField(maxHeightProperty, new GUIContent { text = "Height" });
					break;
				default:
					break;
			}

			EditorGUILayout.PropertyField(property.FindPropertyRelative("extrusionScaleFactor"), new GUIContent { text = "Scale Factor" });
			EditorGUI.indentLevel--;
		}

		private void DrawPropertyDropDown(SerializedProperty property, Rect position)
		{
			var selectedLayerName = property.FindPropertyRelative("_selectedLayerName").stringValue;

			var serializedMapObject = property.serializedObject;
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJsonData = mapObject.VectorData.LayerProperty.tileJsonData;

			DrawPropertyName(property, position, selectedLayerName);
		}

		private void DrawPropertyName(SerializedProperty property, Rect position, string selectedLayerName)
		{
			var descriptionString = "No description available";

			if (string.IsNullOrEmpty(selectedLayerName) || tileJsonData == null || !tileJsonData.PropertyDisplayNames.ContainsKey(selectedLayerName))
			{
				DrawWarningMessage(position);
			}
			else
			{
				dataUnavailable = false;
				var propertyDisplayNames = tileJsonData.PropertyDisplayNames[selectedLayerName];
				propertyNamesList = propertyDisplayNames;

				var propertyString = property.FindPropertyRelative("propertyName").stringValue;
				if (propertyNamesList.Contains(propertyString))
				{
					//if the layer contains the current layerstring, set it's index to match
					_propertyIndex = propertyDisplayNames.FindIndex(s => s.Equals(propertyString));

				}
				else
				{
					//if the selected layer isn't in the source, add a placeholder entry
					_propertyIndex = 0;
					propertyNamesList.Insert(0, propertyString);
				}

				//create GUIcontent array
				_propertyNameContent = new GUIContent[propertyNamesList.Count];
				for (int extIdx = 0; extIdx < propertyNamesList.Count; extIdx++)
				{
					var parsedPropertyString = propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					_propertyNameContent[extIdx] = new GUIContent
					{
						text = propertyNamesList[extIdx],
						//this lookup doesn't work for placeholder properties
						tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
					};
				}

				//display popup
				var propertyNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
				_propertyIndex = EditorGUILayout.Popup(propertyNameLabel, _propertyIndex, _propertyNameContent);
				var parsedString = propertyNamesList[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();

				//this lookup doesn't work for placeholder properties
				descriptionString = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedString];
				property.FindPropertyRelative("propertyName").stringValue = parsedString;
			}

			descriptionString = string.IsNullOrEmpty(descriptionString) ? "No description available" : descriptionString;

			var propertyDescriptionPrefixLabel = new GUIContent { text = "Property Description", tooltip = "Factual information about the selected property" };
			EditorGUILayout.LabelField(propertyDescriptionPrefixLabel, new GUIContent(descriptionString), (GUIStyle)"wordWrappedLabel");
		}

		private void DrawWarningMessage(Rect position)
		{
			dataUnavailable = true;
			GUIStyle labelStyle = new GUIStyle(EditorStyles.popup);
			//labelStyle.normal.textColor = Color.red;
			labelStyle.fontStyle = FontStyle.Bold;
			var layerNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
			EditorGUILayout.LabelField(layerNameLabel, new GUIContent("No layers found: Invalid MapId / No Internet."), labelStyle);//(GUIStyle)"minipopUp");
			return;
		}
	}
}
