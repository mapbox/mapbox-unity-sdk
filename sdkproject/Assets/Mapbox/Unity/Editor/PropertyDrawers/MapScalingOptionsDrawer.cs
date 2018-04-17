namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(MapScalingOptions))]
	public class MapScalingOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] scalingTypeContent;
		bool isGUIContentSet = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var scalingType = property.FindPropertyRelative("scalingType");
			var displayNames = scalingType.enumDisplayNames;
			int count = scalingType.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				scalingTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					scalingTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((MapScalingType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			// Draw label.
			var scalingTypePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = label.text, tooltip = "Scale of map in game units.", });

			scalingType.enumValueIndex = EditorGUI.Popup(scalingTypePosition, scalingType.enumValueIndex, scalingTypeContent);

			if ((MapScalingType)scalingType.enumValueIndex == MapScalingType.Custom)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("unityTileSize"));
			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			var scalingType = property.FindPropertyRelative("scalingType");
			if ((MapScalingType)scalingType.enumValueIndex == MapScalingType.Custom)
			{
				return 2.0f * lineHeight;
			}
			else
			{
				return 1.0f * lineHeight;
			}
		}
	}
}