namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(GeometryMaterialOptions))]
	public class GeometryMaterialOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineHeight), new GUIContent { text = "Material Options", tooltip = "Unity materials to be used for features. " });
			EditorGUI.indentLevel++;
			//if (showPosition)
			{
				position.y += lineHeight;
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Texturing Type", tooltip = "Use image texture from the Imagery source as texture for roofs. " });
				var texturingType = property.FindPropertyRelative("texturingType");
				EditorGUI.indentLevel--;
				texturingType.enumValueIndex = EditorGUI.Popup(typePosition, texturingType.enumValueIndex, texturingType.enumDisplayNames);
				EditorGUI.indentLevel++;

				var matList = property.FindPropertyRelative("materials");
				if (matList.arraySize == 0)
				{
					matList.arraySize = 2;
				}

				var roofMat = matList.GetArrayElementAtIndex(0);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), roofMat, new GUIContent { text = "Roof Material", tooltip = "Unity material to use for extruded roof/top mesh. " });
				position.y += EditorGUI.GetPropertyHeight(roofMat);

				var wallMat = matList.GetArrayElementAtIndex(1);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), wallMat, new GUIContent { text = "Wall Material", tooltip = "Unity material to use for extruded wall/side mesh. " });
				position.y += EditorGUI.GetPropertyHeight(wallMat);

				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					position.y += lineHeight;
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				}
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.AtlasWithColorPalette)
				{
					position.y += lineHeight;
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
					position.y += lineHeight;
					var colorPalette = property.FindPropertyRelative("colorPallete");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), colorPalette, new GUIContent { text = "Color Palette", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				}
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			float height = 0.0f;
			if (showPosition)
			{
				height += (2.0f * lineHeight);
				var matList = property.FindPropertyRelative("materials");

				for (int i = 0; i < matList.arraySize; i++)
				{
					var matInList = matList.GetArrayElementAtIndex(i);
					height += EditorGUI.GetPropertyHeight(matInList);
				}
				var texturingType = property.FindPropertyRelative("texturingType");
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					height += lineHeight;
				}
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.AtlasWithColorPalette)
				{
					height += (2.0f * lineHeight);
				}
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
		}
	}

	//[CustomPropertyDrawer(typeof(TypeVisualizerTuple))]
	//public class TypeVisualizerBaseDrawer : PropertyDrawer
	//{
	//	static float lineHeight = EditorGUIUtility.singleLineHeight;
	//	bool showPosition = true;
	//	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	//	{
	//		EditorGUI.BeginProperty(position, label, property);

	//		position.height = lineHeight;

	//		EditorGUI.PropertyField(position, property.FindPropertyRelative("Stack"));

	//		EditorGUI.EndProperty();
	//	}
	//	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	//	{
	//		// Reserve space for the total visible properties.
	//		int rows = 2;
	//		//Debug.Log("Height - " + rows * lineHeight);
	//		return (float)rows * lineHeight;
	//	}
	//}

}