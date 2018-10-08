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

			EditorGUI.BeginChangeCheck();
			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(sourceTypeLabel, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			var sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;

			var sourceOptionsProperty = property.FindPropertyRelative("sourceOptions");
			var layerSourceProperty = sourceOptionsProperty.FindPropertyRelative("layerSource");
			var layerSourceId = layerSourceProperty.FindPropertyRelative("Id");

			EditorGUI.BeginChangeCheck();

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
					layerSourceId.stringValue = string.IsNullOrEmpty(CustomSourceMapId) ? MapboxDefaultElevation.GetParameters(ElevationSourceType.MapboxTerrain).Id : CustomSourceMapId;
					EditorGUILayout.PropertyField(sourceOptionsProperty, _mapIdGui);
					CustomSourceMapId = layerSourceId.stringValue;
					break;
				default:
					break;
			}

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			var elevationLayerType = property.FindPropertyRelative("elevationLayerType");

			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = false;
				elevationLayerType.enumValueIndex = (int)ElevationLayerType.FlatTerrain;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("elevationLayerType"), new GUIContent { text = elevationLayerType.displayName, tooltip = ((ElevationLayerType)elevationLayerType.enumValueIndex).Description() });
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = true;
			}

			GUILayout.Space(-lineHeight);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("colliderOptions"), true);
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property.FindPropertyRelative("colliderOptions"));
			}
			GUILayout.Space(2 * -lineHeight);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("requiredOptions"), true);
			GUILayout.Space(-lineHeight);
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			ShowPosition = EditorGUILayout.Foldout(ShowPosition, "Others");

			if (ShowPosition)
			{
				EditorGUI.BeginChangeCheck();

				EditorGUILayout.PropertyField(property.FindPropertyRelative("modificationOptions"), true);

				EditorGUILayout.PropertyField(property.FindPropertyRelative("sideWallOptions"), true);

				EditorGUILayout.PropertyField(property.FindPropertyRelative("unityLayerOptions"), true);

				if (EditorGUI.EndChangeCheck())
				{
					EditorHelper.CheckForModifiedProperty(property);
				}
			}
		}
	}
}
