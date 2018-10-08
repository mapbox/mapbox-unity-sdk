using Mapbox.Unity.Map.TileProviders;

namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	[CustomPropertyDrawer(typeof(MapOptions))]
	public class MapOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;
			EditorGUI.LabelField(position, "Location ");
			position.y += lineHeight;
			EditorGUILayout.PropertyField(property.FindPropertyRelative("locationOptions"));
			position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("locationOptions"));
			var extentOptions = property.FindPropertyRelative("extentOptions");
			var extentOptionsType = extentOptions.FindPropertyRelative("extentType");
			if ((MapExtentType)extentOptionsType.enumValueIndex == MapExtentType.Custom)
			{
				var test = property.serializedObject.FindProperty("_tileProvider");

				EditorGUI.PropertyField(position, test);
				position.y += lineHeight;
			}
			else
			{
				EditorGUI.PropertyField(position, property.FindPropertyRelative("extentOptions"));

				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("extentOptions"));
			}

			showPosition = EditorGUI.Foldout(position, showPosition, "Others");
			if (showPosition)
			{
				position.y += lineHeight;
				EditorGUILayout.PropertyField(property.FindPropertyRelative("placementOptions"));

				position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("placementOptions"));
				EditorGUI.PropertyField(position, property.FindPropertyRelative("scalingOptions"));

			}
			EditorGUI.EndProperty();

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			float height = 2.0f * lineHeight;
			if (showPosition)
			{
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("placementOptions"));
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("scalingOptions"));
			}
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("locationOptions"));
			height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("extentOptions"));
			return height;
		}
	}

	[CustomPropertyDrawer(typeof(AbstractTileProvider))]
	public class AbstractTileProviderDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.ObjectField(position, property);
			EditorGUI.EndProperty();
		}
	}
}