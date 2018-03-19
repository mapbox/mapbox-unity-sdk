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

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			// Draw label.
			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;
			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Style Name", tooltip = EnumExtensions.Description(sourceTypeValue) });

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);
			sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;
			switch (sourceTypeValue)
			{
				case ImagerySourceType.MapboxStreets:
				case ImagerySourceType.MapboxOutdoors:
				case ImagerySourceType.MapboxDark:
				case ImagerySourceType.MapboxLight:
				case ImagerySourceType.MapboxSatellite:
				case ImagerySourceType.MapboxSatelliteStreet:
					var sourcePropertyValue = MapboxDefaultImagery.GetParameters(sourceTypeValue);
					var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
					var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
					var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUI.PropertyField(position, sourceOptionsProperty);
					GUI.enabled = true;
					break;
				case ImagerySourceType.Custom:
					EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceOptions"), new GUIContent("Source Options"));
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
