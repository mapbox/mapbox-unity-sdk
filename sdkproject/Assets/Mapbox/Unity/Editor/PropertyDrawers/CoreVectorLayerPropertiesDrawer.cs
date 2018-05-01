namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;

	[CustomPropertyDrawer(typeof(CoreVectorLayerProperties))]
	public class CoreVectorLayerPropertiesDrawer : PropertyDrawer
	{
		int index
		{
			get
			{
				return EditorPrefs.GetInt("CoreOptions_propertySelectionIndex");
			}
			set
			{
				EditorPrefs.SetInt("CoreOptions_propertySelectionIndex", value);
			}
		}

		static float lineHeight = EditorGUIUtility.singleLineHeight;
		static TileJsonData tileJSONData = new TileJsonData();
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
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
			//EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerName"));
			var serializedMapObject = property.serializedObject;
			//serializedMapObject.Update();
			AbstractMap mapObject = (AbstractMap)serializedMapObject.targetObject;
			tileJSONData = mapObject.VectorData.LayerProperty.tileJsonData;

			if (tileJSONData == null || tileJSONData.LayerDisplayNames.Count <= 0)
			{
				tileJSONData.ProcessTileJSONData(mapObject.tileJSONResponse);
			}
			else
			{
				var layerDisplayNames = tileJSONData.LayerDisplayNames;
				var layerNamesArray = layerDisplayNames.ToArray();
				typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Layer Name", tooltip = "The layer name from the Mapbox tileset that would be used for visualizing a feature" });
				EditorGUI.indentLevel--;
				index = EditorGUI.Popup(typePosition, index, layerNamesArray);
				var parsedString = layerNamesArray[index].Split(new string[] { tileJSONData.commonLayersKey }, System.StringSplitOptions.None)[0].Trim();
				property.FindPropertyRelative("layerName").stringValue = parsedString;
				EditorGUI.indentLevel++;
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
			//serializedMapObject.ApplyModifiedProperties();
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