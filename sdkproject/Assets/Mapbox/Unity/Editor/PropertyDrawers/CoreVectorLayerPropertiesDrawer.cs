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
		int _layerIndex = 0;
		bool _isGUIContentSet = false;
		GUIContent[] _primitiveTypeContent;
		GUIContent[] _layerTypeContent;
		static TileJsonData tileJsonData = new TileJsonData();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, null, property);

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
			DrawLayerName(property, position, layerDisplayNames);

			var snapToTerrainProperty = property.FindPropertyRelative("snapToTerrain");
			var groupFeaturesProperty = property.FindPropertyRelative("groupFeatures");

			snapToTerrainProperty.boolValue = EditorGUILayout.Toggle(snapToTerrainProperty.displayName, snapToTerrainProperty.boolValue);
			groupFeaturesProperty.boolValue = EditorGUILayout.Toggle(groupFeaturesProperty.displayName, groupFeaturesProperty.boolValue);

			if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("lineWidth"));
			}
			EditorGUI.EndProperty();
		}
		//private static int count = 0;
		private void DrawLayerName(SerializedProperty property, Rect position, List<string> layerDisplayNames)
		{

			var layerNameLabel = new GUIContent
			{
				text = "Layer Name",
				tooltip = "The layer name from the Mapbox tileset that would be used for visualizing a feature"
			};

			//disable the selection if there is no layer
			if (layerDisplayNames.Count == 0)
			{
				EditorGUILayout.LabelField(layerNameLabel, new GUIContent("No layers found: Invalid MapId / No Internet."), (GUIStyle)"minipopUp");
				return;
			}

			//check the string value at the current _layerIndex to verify that the stored index matches the property string.
			var layerString = property.FindPropertyRelative("layerName").stringValue;
			if (layerDisplayNames.Contains(layerString))
			{
				//if the layer contains the current layerstring, set it's index to match
				_layerIndex = layerDisplayNames.FindIndex(s => s.Equals(layerString));

			}
			else
			{
				//if the selected layer isn't in the source, add a placeholder entry
				_layerIndex = 0;
				layerDisplayNames.Insert(0, layerString);
				if (!tileJsonData.LayerPropertyDescriptionDictionary.ContainsKey(layerString))
				{
					tileJsonData.LayerPropertyDescriptionDictionary.Add(layerString, new Dictionary<string, string>());
				}

			}

			//create the display name guicontent array with an additional entry for the currently selected item
			_layerTypeContent = new GUIContent[layerDisplayNames.Count];
			for (int extIdx = 0; extIdx < layerDisplayNames.Count; extIdx++)
			{
				_layerTypeContent[extIdx] = new GUIContent
				{
					text = layerDisplayNames[extIdx],
				};
			}

			//draw the layer selection popup
			_layerIndex = EditorGUILayout.Popup(layerNameLabel, _layerIndex, _layerTypeContent);
			var parsedString = layerDisplayNames.ToArray()[_layerIndex].Split(new string[] { tileJsonData.commonLayersKey }, System.StringSplitOptions.None)[0].Trim();
			property.FindPropertyRelative("layerName").stringValue = parsedString;
		}
	}
}
