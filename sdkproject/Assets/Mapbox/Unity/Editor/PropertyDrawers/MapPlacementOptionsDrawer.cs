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

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var placementType = property.FindPropertyRelative("placementType");
			var snapMapToTerrain = property.FindPropertyRelative("snapMapToZero");
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), placementType, new GUIContent { text = placementType.displayName, tooltip = EnumExtensions.Description((MapPlacementType)placementType.enumValueIndex) });
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