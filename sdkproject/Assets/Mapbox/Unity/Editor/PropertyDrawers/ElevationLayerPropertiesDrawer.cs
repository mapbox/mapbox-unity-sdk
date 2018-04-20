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
		GUIContent[] sourceTypeContent;
		bool isGUIContentSet = false;

		bool ShowPosition
		{
			get
			{
				return EditorPrefs.GetBool("ElevationLayerProperties_showPosition");
			}
			set
			{
				EditorPrefs.SetBool("ElevationLayerProperties_showPosition", value);
			}
		}

		private GUIContent _mapIdGui = new GUIContent
		{
			text = "Map Id",
			tooltip = "Map Id corresponding to the tileset."
		};

		string CustomSourceMapId
		{
			get
			{
				return EditorPrefs.GetString("ElevationLayerProperties_customSourceMapId");
			}
			set
			{
				EditorPrefs.SetString("ElevationLayerProperties_customSourceMapId", value);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			var sourceTypeProperty = property.FindPropertyRelative("sourceType");
			var sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

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
						tooltip = ((ElevationSourceType)extIdx).Description(),
					};
				}
				isGUIContentSet = true;
			}
			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Data Source", tooltip = "Source tileset for Terrain." });

			sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			position.y += lineHeight;

			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
			var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
			switch (sourceTypeValue)
			{
				case ElevationSourceType.MapboxTerrain:
					var sourcePropertyValue = MapboxDefaultElevation.GetParameters(sourceTypeValue);
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUI.PropertyField(position, sourceOptionsProperty, _mapIdGui);
					GUI.enabled = true;
					break;
				case ElevationSourceType.Custom:
					layerSourceId.stringValue = CustomSourceMapId;
					EditorGUI.PropertyField(position, sourceOptionsProperty, _mapIdGui);
					CustomSourceMapId = layerSourceId.stringValue;
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
			EditorGUI.PropertyField(position, elevationLayerType, new GUIContent { text = elevationLayerType.displayName, tooltip = ((ElevationLayerType)elevationLayerType.enumValueIndex).Description() });
			position.y += lineHeight;
			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = true;
			}

			EditorGUI.PropertyField(position, property.FindPropertyRelative("requiredOptions"), true);
			position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredOptions"));
			ShowPosition = EditorGUI.Foldout(position, ShowPosition, "Others");
			if (ShowPosition)
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
			if (ShowPosition)
			{
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("modificationOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("unityLayerOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sideWallOptions"));
			}
			return height;
		}
	}

}
