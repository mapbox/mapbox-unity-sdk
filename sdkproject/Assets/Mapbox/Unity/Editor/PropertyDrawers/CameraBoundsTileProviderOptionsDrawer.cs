namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(CameraBoundsTileProviderOptions))]
	public class CameraBoundsTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var camera = property.FindPropertyRelative("camera");
			var updateInterval = property.FindPropertyRelative("updateInterval");
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), camera, new GUIContent { text = camera.displayName, tooltip = "Camera to control map extent." });
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), updateInterval, new GUIContent { text = updateInterval.displayName, tooltip = "Time in ms between map extent update." });
			EditorGUI.EndProperty();
		}
	}
}