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
		GUIContent[] extrusionTypeContent;
		bool isGUIContentSet = false;
		GUIContent[] _propertyNameContent;
		bool _isLayerNameGUIContentSet = false;
		static TileJsonData tileJsonData = new TileJsonData();
		static TileJSONResponse tileJsonResponse;
		static bool dataUnavailable = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

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

		private void DrawPropertyName(SerializedProperty property, Rect position, List<string> propertyDisplayNames, string selectedLayerName)
		{
			propertyNamesList = propertyDisplayNames;

			if (!_isLayerNameGUIContentSet)
			{
				_propertyNameContent = new GUIContent[propertyNamesList.Count];
				for (int extIdx = 0; extIdx < propertyNamesList.Count; extIdx++)
				{
					var parsedPropertyString = propertyNamesList[extIdx].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
					_propertyNameContent[extIdx] = new GUIContent
					{
						text = propertyNamesList[extIdx],
						tooltip = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedPropertyString]
					};
				}
				_isLayerNameGUIContentSet = true;
			}

			var propertyNameLabel = new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" };
			index = EditorGUILayout.Popup(propertyNameLabel, index, _propertyNameContent);
			var parsedString = propertyNamesList[index].Split(new string[] { tileJsonData.optionalPropertiesString }, System.StringSplitOptions.None)[0].Trim();
			var descriptionString = tileJsonData.LayerPropertyDescriptionDictionary[selectedLayerName][parsedString];

			property.FindPropertyRelative("propertyName").stringValue = parsedString;



			EditorGUILayout.PrefixLabel(new GUIContent { text = "Property Description", tooltip = "Factual information about the selected property" });
			EditorGUILayout.HelpBox(descriptionString, MessageType.Info);
		}

		private void DrawWarningMessage(Rect position)
		{
			dataUnavailable = true;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent { text = "Property Name", tooltip = "The name of the property in the selected Mapbox layer that will be used for extrusion" });
			GUILayout.Space(-0.55f * EditorGUIUtility.labelWidth);
			EditorGUILayout.HelpBox("No properties found : Invalid MapId / No Internet.", MessageType.None);
			EditorGUILayout.EndHorizontal();
			return;
		}
	}
}
