using Mapbox.Unity.DataContainers;

namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(MapPlacementOptions))]
	public class MapPlacementOptionsDrawer : PropertyDrawer
	{
		GUIContent[] placementTypeContent;
		bool isGUIContentSet = false;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var placementType = property.FindPropertyRelative("placementType");

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

			placementType.enumValueIndex = EditorGUILayout.Popup(new GUIContent { text = label.text, tooltip = "Placement of Map root.", }, placementType.enumValueIndex, placementTypeContent);
		}
	}
}
