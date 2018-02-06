namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(ImageryLayerProperties))]
	public class ImageryLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			showPosition = EditorGUI.Foldout(position, showPosition, label);

			if (showPosition)
			{
				position.y += lineHeight;
				// Draw label.
				var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Style Name"));
				var sourceTypeProperty = property.FindPropertyRelative("sourceType");

				sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);
				var sourceTypeValue = (ImagerySourceType)sourceTypeProperty.enumValueIndex;

				EditorGUI.indentLevel++;

				position.y += lineHeight;
				switch (sourceTypeValue)
				{
					case ImagerySourceType.Streets:
					case ImagerySourceType.Outdoors:
					case ImagerySourceType.Dark:
					case ImagerySourceType.Light:
					case ImagerySourceType.Satellite:
					case ImagerySourceType.SatelliteStreet:
						var sourcePropertyValue = MapboxDefaultImagery.GetParameters(sourceTypeValue);
						var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
						var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
						var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
						layerSourceId.stringValue = sourcePropertyValue.Id;
						GUI.enabled = false;
						EditorGUI.PropertyField(position, sourceOptionsProperty, new GUIContent("Source Option"));
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
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("rasterOptions"), new GUIContent { text = "RasterOptions" });
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();

		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += (2 * lineHeight);
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rasterOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
		}
	}
}
