namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using System.Linq;
	using System;

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

		static bool _isInitialized = false;
		string objectId = "";
		static string currentSource = "";
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		static TileJsonData tileJsonData = new TileJsonData();
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			// Draw label.
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("isActive"));
			position.y += lineHeight;
			var primitiveType = property.FindPropertyRelative("geometryType");

			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Primitive Type", tooltip = "Primitive geometry type of the visualizer, allowed primitives - point, line, polygon." });
			EditorGUI.indentLevel--;
			primitiveType.enumValueIndex = EditorGUI.Popup(typePosition, primitiveType.enumValueIndex, primitiveType.enumDisplayNames);
			EditorGUI.indentLevel++;

			position.y += lineHeight;
			var serializedMapObject = property.serializedObject;
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJsonData = mapObject.VectorData.LayerProperty.tileJsonData;

			var layerDisplayNames = tileJsonData.LayerDisplayNames;

			var newSource = property.FindPropertyRelative("sourceId").stringValue;

			if(_isInitialized)
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

			position.y += lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("snapToTerrain"));

			position.y += lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("groupFeatures"));

			if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("lineWidth"));
			}

			EditorGUI.EndProperty();
		}
		private static int count = 0;
		private void DrawLayerName(SerializedProperty property,Rect position,List<string> layerDisplayNames)
		{
			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Layer Name", tooltip = "The layer name from the Mapbox tileset that would be used for visualizing a feature" });

			if (layerDisplayNames.Count == 0)
			{
				EditorGUI.indentLevel--;
				EditorGUI.HelpBox(typePosition, "No layers found : Invalid MapId / No Internet.", MessageType.None);
				EditorGUI.indentLevel++;
				return;
			}

			EditorGUI.indentLevel--;
			index = EditorGUI.Popup(typePosition, index, layerDisplayNames.ToArray());
			var parsedString = layerDisplayNames.ToArray()[index].Split(new string[] { tileJsonData.commonLayersKey }, System.StringSplitOptions.None)[0].Trim();
			property.FindPropertyRelative("layerName").stringValue = parsedString;
			EditorGUI.indentLevel++;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sourceTypeProperty = property.FindPropertyRelative("geometryType");

			float height = 0.0f;
			height += (((((VectorPrimitiveType)sourceTypeProperty.enumValueIndex == VectorPrimitiveType.Line)) ? 6.0f : 5.0f) * EditorGUIUtility.singleLineHeight);

			return height;
		}
	}
}