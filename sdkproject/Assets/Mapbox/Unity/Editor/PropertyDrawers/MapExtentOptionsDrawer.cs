namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(MapExtentOptions))]
	public class MapExtentOptionsDrawer : PropertyDrawer
	{
		static string extTypePropertyName = "extentType";
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;


			var kindProperty = property.FindPropertyRelative(extTypePropertyName);
			var displayNames = kindProperty.enumDisplayNames;
			int count = kindProperty.enumDisplayNames.Length;
			GUIContent[] extentTypeContent = new GUIContent[count];
			for (int extIdx = 0; extIdx < count; extIdx++)
			{
				extentTypeContent[extIdx] = new GUIContent
				{
					text = displayNames[extIdx],
					tooltip = EnumExtensions.Description((MapExtentType)extIdx),
				};
			}
			// Draw label.
			var kindPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = label.text, tooltip = EnumExtensions.Description((MapExtentType)kindProperty.enumValueIndex), });

			kindProperty.enumValueIndex = EditorGUI.Popup(kindPosition, kindProperty.enumValueIndex, extentTypeContent);

			var kind = (MapExtentType)kindProperty.enumValueIndex;


			EditorGUI.indentLevel++;

			var rect = new Rect(position.x, position.y + lineHeight, position.width, lineHeight);

			switch (kind)
			{
				case MapExtentType.CameraBounds:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("cameraBoundsOptions"), new GUIContent { text = "CameraOptions-" });
					break;
				case MapExtentType.RangeAroundCenter:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("rangeAroundCenterOptions"), new GUIContent { text = "RangeAroundCenter" });
					break;
				case MapExtentType.RangeAroundTransform:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("rangeAroundTransformOptions"), new GUIContent { text = "RangeAroundTransform" });
					break;
				default:
					break;
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();


		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var kindProperty = property.FindPropertyRelative(extTypePropertyName);

			var kind = (MapExtentType)kindProperty.enumValueIndex;

			int rows = 1;

			switch (kind)
			{
				case MapExtentType.CameraBounds:
					rows += 2;
					break;
				case MapExtentType.RangeAroundCenter:
					rows += 4;
					break;
				case MapExtentType.RangeAroundTransform:
					rows += 3;
					break;
				default:
					break;
			}
			return (float)rows * EditorGUIUtility.singleLineHeight;
		}

	}
}