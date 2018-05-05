namespace Mapbox.Editor
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
		int index
		{
			get
			{
				return EditorPrefs.GetInt(objectId + "GeometryOptions_propertySelectionIndex");
			}
			set
			{
				EditorPrefs.SetInt(objectId + "GeometryOptions_propertySelectionIndex", value);
			}
		}

		bool _isInitialized = false;
		string objectId = "";
		private static List<string> propertyNamesList = new List<string>();
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] sourceTypeContent;
		bool isGUIContentSet = false;
		static TileJsonData tileJsonData = new TileJsonData();
		static TileJSONResponse tileJsonResponse;
		static bool dataUnavailable = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			EditorGUI.BeginProperty(position, label, property);
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");

			var displayNames = extrusionTypeProperty.enumDisplayNames;
			int count = extrusionTypeProperty.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				sourceTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					sourceTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ExtrusionType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Extrusion Type", tooltip = "Type of geometry extrusion" });


			EditorGUI.indentLevel--;
			extrusionTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, extrusionTypeProperty.enumValueIndex, sourceTypeContent);
			EditorGUI.indentLevel++;
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
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					DrawPropertyDropDown(property, position);
					if (!dataUnavailable)
					{
						position.y += 2.5f * lineHeight;
					}
					break;
				case Unity.Map.ExtrusionType.MinHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					DrawPropertyDropDown(property, position);
					if (!dataUnavailable)
					{
						position.y += 2.5f * lineHeight;
					}					
					break;
				case Unity.Map.ExtrusionType.MaxHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					DrawPropertyDropDown(property, position);
					if (!dataUnavailable)
					{
						position.y += 2.5f * lineHeight;
					}
					break;
				case Unity.Map.ExtrusionType.RangeHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					DrawPropertyDropDown(property, position);
					if (!dataUnavailable)
					{
						position.y += 2.5f * lineHeight;
					}
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
					if (minHeightProperty.floatValue > maxHeightProperty.floatValue)
					{
						EditorGUILayout.HelpBox("Maximum Height less than Minimum Height!", MessageType.Error);
					}
					break;
				case Unity.Map.ExtrusionType.AbsoluteHeight:
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), extrusionGeometryType, extrusionGeometryGUI);
					position.y += lineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty, new GUIContent { text = "Height" });
					break;
				default:
					break;
			}

			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionScaleFactor"), new GUIContent { text = "Scale Factor" });
			EditorGUI.indentLevel--;


			EditorGUI.EndProperty();
		}

		private void DrawPropertyDropDown(SerializedProperty property, Rect position)
		{
			var selectedLayerName = property.FindPropertyRelative("_selectedLayerName").stringValue;

			var serializedMapObject = property.serializedObject;
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJsonData = mapObject.VectorData.LayerProperty.tileJsonData;


			if (string.IsNullOrEmpty(selectedLayerName) || tileJsonData == null || !tileJsonData.PropertyDisplayNames.ContainsKey(selectedLayerName))
			{
				DrawWarningMessage(position);
				return;
			}

			dataUnavailable = false;
			var propertyDisplayNames = tileJsonData.PropertyDisplayNames[selectedLayerName];

			if (_isInitialized == true)
			{
				if (!Enumerable.SequenceEqual(propertyNamesList, propertyDisplayNames))
				{
					index = 0;
					propertyNamesList = propertyDisplayNames;
				}
				else
				{
					DrawPropertyName(property, position, propertyDisplayNames, selectedLayerName);
				}
			}
			else
			{
				_isInitialized = true;
				DrawPropertyName(property, position, propertyDisplayNames, selectedLayerName);
			}
		}

		private void DrawPropertyName(SerializedProperty property, Rect position, List<string>propertyDisplayNames, string selectedLayerName)
		{
			propertyNamesList = propertyDisplayNames;
			Rect typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" });
			EditorGUI.indentLevel -= 2;
			index = EditorGUI.Popup(typePosition, index, propertyNamesList.ToArray());
			position.y += lineHeight;
			var parsedString = propertyNamesList.ToArray()[index].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
			property.FindPropertyRelative("propertyName").stringValue = parsedString;
			EditorGUI.indentLevel += 2;

			var descriptionString = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedString];
			typePosition.y += lineHeight;
			typePosition.height = (float)(2.5f * lineHeight);
			EditorGUI.PrefixLabel(new Rect(position.x, typePosition.y + lineHeight / 2, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Property Description", tooltip = "Factual information about the selected property" });
			EditorGUI.HelpBox(typePosition, descriptionString, MessageType.Info);
		}

		private void DrawWarningMessage(Rect position)
		{
			dataUnavailable = true;
			Rect typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" });
			EditorGUI.indentLevel-=2;
			EditorGUI.HelpBox(typePosition, "No properties found : Invalid MapId / No Internet.", MessageType.None);
			EditorGUI.indentLevel+=2;
			return;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			int rows = 1;
			//if (showPosition)
			{
				switch (sourceTypeValue)
				{
					case Unity.Map.ExtrusionType.None:
						rows += 1;
						break;
					case Unity.Map.ExtrusionType.PropertyHeight:
					case Unity.Map.ExtrusionType.MinHeight:
					case Unity.Map.ExtrusionType.MaxHeight:
						if (dataUnavailable)
							rows += 3;
						else
							rows += 6;
						break;
					case Unity.Map.ExtrusionType.RangeHeight:
						if (dataUnavailable)
							rows += 5;
						else
							rows += 8;
						break;
					case Unity.Map.ExtrusionType.AbsoluteHeight:
							rows += 3;
						break;
					default:
						rows += 2;
						break;
				}
			}
			return (float)rows * lineHeight;
		}
	}
}