namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(LayerPerformanceOptions))]
	public class LayerPerformanceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		SerializedProperty isActiveProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			isActiveProperty = property.FindPropertyRelative("isEnabled");

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Enable Coroutines"));
			isActiveProperty.boolValue = EditorGUI.Toggle(typePosition, isActiveProperty.boolValue);

			if (isActiveProperty.boolValue == true)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("entityPerCoroutine"), true);
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (isActiveProperty != null && isActiveProperty.boolValue == true)
			{
				height += (2.0f * EditorGUIUtility.singleLineHeight);
				//height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("layerSource"), false);
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