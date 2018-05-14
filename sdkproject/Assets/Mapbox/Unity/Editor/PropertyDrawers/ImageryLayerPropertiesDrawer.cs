namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(ImageryLayerProperties))]
	public class ImageryLayerPropertiesDrawer : PropertyDrawer
	{
		string objectId = "";

		GUIContent[] sourceTypeContent;
		bool isGUIContentSet = false;

		private GUIContent _mapIdGui = new GUIContent
		{
			text = "Map Id",
			tooltip = "Map Id corresponding to the tileset."
		};

		string CustomSourceMapId
		{
			get
			{
				return EditorPrefs.GetString(objectId + "ImageryLayerProperties_customSourceMapId");
			}
			set
			{
				EditorPrefs.SetString(objectId + "ImageryLayerProperties_customSourceMapId", value);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

			var displayNames = sourceTypeProperty.enumDisplayNames;
			int count = sourceTypeProperty.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				sourceTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					sourceTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ImagerySourceType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			// Draw label.
			var sourceTypeLabel = new GUIContent { text = "Data Source", tooltip = "Source tileset for Imagery." };

			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(sourceTypeLabel, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
			var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");

			switch (sourceTypeValue)
			{
				case ImagerySourceType.MapboxStreets:
				case ImagerySourceType.MapboxOutdoors:
				case ImagerySourceType.MapboxDark:
				case ImagerySourceType.MapboxLight:
				case ImagerySourceType.MapboxSatellite:
				case ImagerySourceType.MapboxSatelliteStreet:
					var sourcePropertyValue = MapboxDefaultImagery.GetParameters(sourceTypeValue);
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.PropertyField(sourceOptionsProperty, _mapIdGui);
					GUI.enabled = true;
					break;
				case ImagerySourceType.Custom:
					layerSourceId.stringValue = CustomSourceMapId;
					EditorGUILayout.PropertyField(sourceOptionsProperty, new GUIContent { text = "Map Id / Style URL", tooltip = _mapIdGui.tooltip });
					CustomSourceMapId = layerSourceId.stringValue;
					break;
				case ImagerySourceType.None:
					break;
				default:
					break;
			}
			if (sourceTypeValue != ImagerySourceType.None)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("rasterOptions"));
			}
		}
	}
}
