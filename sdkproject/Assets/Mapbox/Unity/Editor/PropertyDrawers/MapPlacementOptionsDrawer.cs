namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(MapPlacementOptions))]
	public class MapPlacementOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		GUIContent[] placementTypeContent;
		bool isGUIContentSet = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var placementType = property.FindPropertyRelative("placementType");
			var snapMapToTerrain = property.FindPropertyRelative("snapMapToZero");

			var displayNames = placementType.enumDisplayNames;
			int count = placementType.enumDisplayNames.Length;
			if (!isGUIContentSet)
			{
				placementTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					placementTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((MapPlacementType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			// Draw label.
			var placementTypePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = label.text, tooltip = "Placement of Map root.", });

			placementType.enumValueIndex = EditorGUI.Popup(placementTypePosition, placementType.enumValueIndex, placementTypeContent);

			//EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), placementType, new GUIContent { text = placementType.displayName, tooltip = EnumExtensions.Description((MapPlacementType)placementType.enumValueIndex) });
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), snapMapToTerrain, new GUIContent { text = snapMapToTerrain.displayName, tooltip = "If checked, map's root will be snapped to zero. " });
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return 2.0f * lineHeight;
		}
	}
}