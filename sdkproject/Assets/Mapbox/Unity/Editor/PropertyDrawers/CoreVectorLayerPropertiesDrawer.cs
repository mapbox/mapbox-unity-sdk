namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using System.Linq;
	using System;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(CoreVectorLayerProperties))]
	public class CoreVectorLayerPropertiesDrawer : PropertyDrawer
	{
		int index
		{
			get
			{
				return EditorPrefs.GetInt(objectId + "CoreOptions_propertySelectionIndex");
			}
			set
			{
				EditorPrefs.SetInt(objectId + "CoreOptions_propertySelectionIndex", value);
			}
		}

		static float _lineHeight = EditorGUIUtility.singleLineHeight;
		bool _isGUIContentSet = false;
		bool _isLayerNameGUIContentSet = false;
		GUIContent[] _primitiveTypeContent;
		GUIContent[] _layerTypeContent;
		static bool _isInitialized = false;
		string objectId = "";
		static string currentSource = "";
		static TileJsonData tileJsonData = new TileJsonData();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

			// Draw label.
			var primitiveType = property.FindPropertyRelative("geometryType");

			var primitiveTypeLabel = new GUIContent
			{
				text = "Primitive Type",
				tooltip = "Primitive geometry type of the visualizer, allowed primitives - point, line, polygon."
			};

			var displayNames = primitiveType.enumDisplayNames;
			int count = primitiveType.enumDisplayNames.Length;

			if (!_isGUIContentSet)
			{
				_primitiveTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					_primitiveTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((VectorPrimitiveType)extIdx),
					};
				}
				_isGUIContentSet = true;
			}

			primitiveType.enumValueIndex = EditorGUILayout.Popup(primitiveTypeLabel, primitiveType.enumValueIndex, _primitiveTypeContent);

			var serializedMapObject = property.serializedObject;
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJsonData = mapObject.VectorData.LayerProperty.tileJsonData;

			var layerDisplayNames = tileJsonData.LayerDisplayNames;

			var newSource = property.FindPropertyRelative("sourceId").stringValue;

			if (_isInitialized)
			{
				if (currentSource != newSource)
				{
					index = 0;
				}
				currentSource = newSource;

				DrawLayerName(property, position, layerDisplayNames);
			}
			else
			{
				_isInitialized = true;
				currentSource = newSource;
			}

			EditorGUILayout.PropertyField(property.FindPropertyRelative("snapToTerrain"));
			EditorGUILayout.PropertyField(property.FindPropertyRelative("groupFeatures"));

			if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("lineWidth"));
			}
		}
		private static int count = 0;
		private void DrawLayerName(SerializedProperty property, Rect position, List<string> layerDisplayNames)
		{
			var layerNameLabel = new GUIContent
			{
				text = "Layer Name",
				tooltip = "The layer name from the Mapbox tileset that would be used for visualizing a feature"
			};

			if (layerDisplayNames.Count == 0)
			{
				EditorGUI.indentLevel--;
				EditorGUILayout.HelpBox("No layers found : Invalid MapId / No Internet.", MessageType.None);
				EditorGUI.indentLevel++;
				return;
			}

			if (!_isLayerNameGUIContentSet)
			{
				_layerTypeContent = new GUIContent[layerDisplayNames.Count];
				for (int extIdx = 0; extIdx < layerDisplayNames.Count; extIdx++)
				{
					_layerTypeContent[extIdx] = new GUIContent
					{
						text = layerDisplayNames[extIdx],
					};
				}
				_isLayerNameGUIContentSet = true;
			}

			index = EditorGUILayout.Popup(layerNameLabel, index, _layerTypeContent);
			var parsedString = layerDisplayNames.ToArray()[index].Split(new string[] { tileJsonData.commonLayersKey }, System.StringSplitOptions.None)[0].Trim();
			property.FindPropertyRelative("layerName").stringValue = parsedString;
		}
	}
}
