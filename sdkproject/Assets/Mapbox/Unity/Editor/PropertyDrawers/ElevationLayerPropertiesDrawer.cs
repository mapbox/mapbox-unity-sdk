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

		private void UpdateProperty(SerializedProperty property)
		{
			property.serializedObject.ApplyModifiedProperties();
			var map = (AbstractMap)property.serializedObject.targetObject;
			map.Terrain.LayerProperty.UpdateProperty();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			

			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

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
			var sourceTypeLabel = new GUIContent { text = "Data Source", tooltip = "Source tileset for Terrain." };
			EditorGUI.BeginChangeCheck();
			sourceTypeProperty.enumValueIndex = EditorGUILayout.Popup(sourceTypeLabel, sourceTypeProperty.enumValueIndex, sourceTypeContent);
			sourceTypeValue = (ElevationSourceType)sourceTypeProperty.enumValueIndex;
			if (EditorGUI.EndChangeCheck())
			{
				UpdateProperty(property);
			}
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

			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = false;
			}
			EditorGUI.BeginChangeCheck();
			var elevationLayerType = property.FindPropertyRelative("elevationLayerType");

			EditorGUILayout.PropertyField(property.FindPropertyRelative("elevationLayerType"), new GUIContent { text = elevationLayerType.displayName, tooltip = ((ElevationLayerType)elevationLayerType.enumValueIndex).Description() });


			position.y += lineHeight;


			EditorGUILayout.PropertyField(property.FindPropertyRelative("requiredOptions"), true);



			if (sourceTypeValue == ElevationSourceType.None)
			{
				GUI.enabled = true;
			}

			if (EditorGUI.EndChangeCheck())
			{
				UpdateProperty(property);
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
					UpdateProperty(property);
				}

			}
		}
	}
}
