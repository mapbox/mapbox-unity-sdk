namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(ImageryLayerProperties))]
	public class ImageryLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

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
				return EditorPrefs.GetString("ImageryLayerProperties_customSourceMapId");
			}
			set
			{
				EditorPrefs.SetString("ImageryLayerProperties_customSourceMapId", value);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

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
						tooltip = ((ImagerySourceType)extIdx).Description(),
					};
				}
				isGUIContentSet = true;
			}
			// Draw label.

			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Data Source", tooltip = "Source tileset for Imagery." });

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;
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
					EditorGUI.PropertyField(position, sourceOptionsProperty,_mapIdGui);
					GUI.enabled = true;
					break;
				case ImagerySourceType.Custom:
					layerSourceId.stringValue = CustomSourceMapId;
					EditorGUI.PropertyField(position, sourceOptionsProperty, new GUIContent{text = "Map Id / Style URL", tooltip = _mapIdGui.tooltip} );
					CustomSourceMapId = layerSourceId.stringValue;
					break;
				case ImagerySourceType.None:
					break;
				default:
					break;
			}
			if (sourceTypeValue != ImagerySourceType.None)
			{
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("rasterOptions"));
			}

			EditorGUI.EndProperty();

		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

			if (sourceTypeValue == ImagerySourceType.None)
			{
				return lineHeight;
			}
			else
			{
				float height = 0.0f;
				height += (1.0f * lineHeight);
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rasterOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
				return height;
			}
		}
	}
}
