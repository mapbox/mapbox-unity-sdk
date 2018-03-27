namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(ElevationLayerProperties))]
	public class ElevationLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Style Name", tooltip = EnumExtensions.Description(sourceTypeValue) });

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);
			sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;
			switch (sourceTypeValue)
			{
				case ElevationSourceType.MapboxTerrain:
					var sourcePropertyValue = MapboxDefaultElevation.GetParameters(sourceTypeValue);
					var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
					var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
					var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUI.PropertyField(position, sourceOptionsProperty, new GUIContent("Source Option"));
					GUI.enabled = true;
					break;
				case ElevationSourceType.Custom:
					EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceOptions"), true);
					break;
				default:
					break;
			}


			//EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceOptions"), true);
			if (sourceTypeValue != ElevationSourceType.None)
			{
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
			}
			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = false;
			}
			var elevationLayerType = property.FindPropertyRelative("elevationLayerType");
			EditorGUI.PropertyField(position, elevationLayerType, new GUIContent { text = elevationLayerType.displayName, tooltip = EnumExtensions.Description((ElevationLayerType)elevationLayerType.enumValueIndex) });
			position.y += lineHeight;
			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = true;
			}

			EditorGUI.PropertyField(position, property.FindPropertyRelative("requiredOptions"), true);
			position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredOptions"));
			showPosition = EditorGUI.Foldout(position, showPosition, "Others");
			if (showPosition)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("modificationOptions"), true);
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("modificationOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("sideWallOptions"), true);
				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sideWallOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("unityLayerOptions"), true);
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			float height = ((sourceTypeValue == ElevationSourceType.None) ? 2.0f : 3.0f) * lineHeight;

			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredOptions"));
			if (showPosition)
			{
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("modificationOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("unityLayerOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sideWallOptions"));
			}
			return height;
		}
	}

}