namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(ElevationLayerProperties))]
	public class ElevationLayerPropertiesDrawer : PropertyDrawer
	{
		string objectId = "";
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] sourceTypeContent;
		bool isGUIContentSet = false;

		bool ShowPosition
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "ElevationLayerProperties_showPosition");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "ElevationLayerProperties_showPosition", value);
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
				return EditorPrefs.GetString(objectId + "ElevationLayerProperties_customSourceMapId");
			}
			set
			{
				EditorPrefs.SetString(objectId + "ElevationLayerProperties_customSourceMapId", value);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			property.serializedObject.Update();
			ElevationLayerProperties elevationLayerProperties = (ElevationLayerProperties)EditorHelper.GetTargetObjectOfProperty(property);

			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

			var sourceTypeProperty = property.FindPropertyRelative("sourceType");

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
			var sourceTypeLabel = new GUIContent { text = "Data Source", tooltip = "Source tileset for Terrain." };

			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(sourceTypeLabel, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			EditorHelper.CheckForModifiedProperty(sourceTypeProperty, elevationLayerProperties);

			var sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
			var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");
			switch (sourceTypeValue)
			{
				case ElevationSourceType.MapboxTerrain:
					var sourcePropertyValue = MapboxDefaultElevation.GetParameters(sourceTypeValue);
					layerSourceId.stringValue = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.PropertyField(sourceOptionsProperty, _mapIdGui);
					GUI.enabled = true;
					break;
				case ElevationSourceType.Custom:
					layerSourceId.stringValue = CustomSourceMapId;
					EditorGUILayout.PropertyField(sourceOptionsProperty, _mapIdGui);
					CustomSourceMapId = layerSourceId.stringValue;
					break;
				default:
					break;
			}

			var elevationLayerType = property.FindPropertyRelative("elevationLayerType");

			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = false;
				elevationLayerType.enumValueIndex = (int)ElevationLayerType.FlatTerrain;
			}

			EditorGUILayout.PropertyField(property.FindPropertyRelative("elevationLayerType"), new GUIContent { text = elevationLayerType.displayName, tooltip = ((ElevationLayerType)elevationLayerType.enumValueIndex).Description() });
			EditorHelper.CheckForModifiedProperty(elevationLayerType, elevationLayerProperties);

			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = true;
			}

			GUILayout.Space(-lineHeight);

			EditorGUILayout.PropertyField(property.FindPropertyRelative("requiredOptions"), true);
			EditorHelper.CheckForModifiedProperty(property.FindPropertyRelative("requiredOptions"), elevationLayerProperties);

			ShowPosition = EditorGUILayout.Foldout(ShowPosition, "Others");
			property.serializedObject.ApplyModifiedProperties();
			if (ShowPosition)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("modificationOptions"), true);
				EditorHelper.CheckForModifiedProperty(property.FindPropertyRelative("modificationOptions"), elevationLayerProperties);

				EditorGUILayout.PropertyField(property.FindPropertyRelative("sideWallOptions"), true);
				EditorHelper.CheckForModifiedProperty(property.FindPropertyRelative("sideWallOptions"), elevationLayerProperties);

				EditorGUILayout.PropertyField(property.FindPropertyRelative("unityLayerOptions"), true);
				EditorHelper.CheckForModifiedProperty(property.FindPropertyRelative("unityLayerOptions"), elevationLayerProperties);
			}
			property.serializedObject.ApplyModifiedProperties();
		}
	}
}
