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

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var scalingType = property.FindPropertyRelative("scalingType");

			var conversionFactor = EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight),
														   scalingType,
														   new GUIContent
														   {
															   text = scalingType.displayName,
															   tooltip = EnumExtensions.Description((MapScalingType)scalingType.enumValueIndex)
														   });
			if ((MapScalingType)scalingType.enumValueIndex == MapScalingType.Custom)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("unityTileSize"), new GUIContent { text = "Unity Tile Size", tooltip = "TODO : FIX DESCRIPTION - Unity Tile Size " });
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