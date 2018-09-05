﻿namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;
	using System.Linq;
	using Mapbox.Platform.TilesetTileJSON;
	using System.Collections.Generic;
	using com.spacepuppyeditor;

	[CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
	public class GeometryExtrusionOptionsDrawer : PropertyDrawer
	{
		//indices for tileJSON lookup
		int _propertyIndex = 0;
		private static List<string> _propertyNamesList = new List<string>();
		GUIContent[] _propertyNameContent;

		GUIContent[] extrusionTypeContent;
		bool isGUIContentSet = false;
		static TileJsonData tileJsonData = new TileJsonData();
		static TileJSONResponse tileJsonResponse;


		//private void ChangeCheck()

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			GeometryExtrusionOptions extrusionOptions = (GeometryExtrusionOptions)EditorHelper.GetTargetObjectOfProperty(property);

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
			bool extrusionTypeHasChanged = extrusionTypeProperty.serializedObject.ApplyModifiedProperties();
			if (extrusionTypeHasChanged && extrusionOptions != null)
			{
				extrusionOptions.HasChanged = true;
			}

			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			var minHeightProperty = property.FindPropertyRelative("minimumHeight");
			var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

			var extrusionGeometryType = property.FindPropertyRelative("extrusionGeometryType");
			var extrusionGeometryGUI = new GUIContent { text = "Geometry Type", tooltip = EnumExtensions.Description((Unity.Map.ExtrusionGeometryType)extrusionGeometryType.enumValueIndex) };
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

			bool extrusionGeometryTypeHasChanged = extrusionGeometryType.serializedObject.ApplyModifiedProperties();
			if (extrusionGeometryTypeHasChanged && extrusionOptions != null)
			{
				extrusionOptions.HasChanged = true;
			}

			EditorGUILayout.PropertyField(property.FindPropertyRelative("extrusionScaleFactor"), new GUIContent { text = "Scale Factor" });
			bool scaleFactorHasChanged = property.FindPropertyRelative("extrusionScaleFactor").serializedObject.ApplyModifiedProperties();
			if (scaleFactorHasChanged && extrusionOptions != null)
			{
				extrusionOptions.HasChanged = true;
			}
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
			var parsedString = "No property selected";
			var descriptionString = "No description available";

			if (string.IsNullOrEmpty(selectedLayerName) || tileJsonData == null || !tileJsonData.PropertyDisplayNames.ContainsKey(selectedLayerName))
			{
				DrawWarningMessage(position);
			}
			else
			{
				var propertyDisplayNames = tileJsonData.PropertyDisplayNames[selectedLayerName];
				_propertyNamesList = new List<string>(propertyDisplayNames);

				//check if the selection is valid
				var propertyString = property.FindPropertyRelative("propertyName").stringValue;
				if (_propertyNamesList.Contains(propertyString))
				{
					//if the layer contains the current layerstring, set it's index to match
					_propertyIndex = propertyDisplayNames.FindIndex(s => s.Equals(propertyString));


					//create guicontent for a valid layer
					_propertyNameContent = new GUIContent[_propertyNamesList.Count];
					for (int extIdx = 0; extIdx < _propertyNamesList.Count; extIdx++)
					{
						var parsedPropertyString = _propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
						_propertyNameContent[extIdx] = new GUIContent
						{
							text = _propertyNamesList[extIdx],
							tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
						};
					}

					//display popup
					var propertyNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
					_propertyIndex = EditorGUILayout.Popup(propertyNameLabel, _propertyIndex, _propertyNameContent);

					//set new string values based on selection
					parsedString = _propertyNamesList[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					descriptionString = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedString];

				}
				else
				{
					//if the selected layer isn't in the source, add a placeholder entry
					_propertyIndex = 0;
					_propertyNamesList.Insert(0, propertyString);

					//create guicontent for an invalid layer
					_propertyNameContent = new GUIContent[_propertyNamesList.Count];

					//first property gets a unique tooltip
					_propertyNameContent[0] = new GUIContent
					{
						text = _propertyNamesList[0],
						tooltip = "Unavialable in Selected Layer"
					};

					for (int extIdx = 1; extIdx < _propertyNamesList.Count; extIdx++)
					{
						var parsedPropertyString = _propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
						_propertyNameContent[extIdx] = new GUIContent
						{
							text = _propertyNamesList[extIdx],
							tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
						};
					}

					//display popup
					var propertyNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
					_propertyIndex = EditorGUILayout.Popup(propertyNameLabel, _propertyIndex, _propertyNameContent);

					//set new string values based on the offset
					parsedString = _propertyNamesList[_propertyIndex].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					descriptionString = "Unavailable in Selected Layer.";

				}

				property.FindPropertyRelative("propertyName").stringValue = parsedString;
				property.FindPropertyRelative("propertyDescription").stringValue = descriptionString;

			}

			descriptionString = string.IsNullOrEmpty(descriptionString) ? "No description available" : descriptionString;

			var propertyDescriptionPrefixLabel = new GUIContent { text = "Property Description", tooltip = "Factual information about the selected property" };
			EditorGUILayout.LabelField(propertyDescriptionPrefixLabel, new GUIContent(descriptionString), (GUIStyle)"wordWrappedLabel");
		}

		private void DrawWarningMessage(Rect position)
		{
			GUIStyle labelStyle = new GUIStyle(EditorStyles.popup);
			//labelStyle.normal.textColor = Color.red;
			labelStyle.fontStyle = FontStyle.Bold;
			var layerNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
			EditorGUILayout.LabelField(layerNameLabel, new GUIContent("No properties found in layer"), labelStyle);//(GUIStyle)"minipopUp");
			return;
		}
	}
}
