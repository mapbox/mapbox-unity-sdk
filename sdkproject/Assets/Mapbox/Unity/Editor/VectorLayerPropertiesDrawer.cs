namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using UnityEditor.IMGUI.Controls;

	[CustomPropertyDrawer(typeof(VectorLayerProperties))]
	public class VectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = false;
		VectorSubLayerTreeView layerTreeView = new VectorSubLayerTreeView(new TreeViewState());

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			showPosition = EditorGUI.Foldout(position, showPosition, label);

			if (showPosition)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				//EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceOptions"), new GUIContent("Source Option"));
				//position.y += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
				//EditorGUI.PropertyField(position, property.FindPropertyRelative("performanceOptions"), new GUIContent("Perfomance Option"));

				//GUILayout.Space(EditorGUIUtility.singleLineHeight);

				EditorGUI.LabelField(position, "Layers", new GUIStyle { fontStyle = FontStyle.Bold });
				position.y += lineHeight;
				List<string> test = new List<string> { "a", "b", "c", "d" };
				layerTreeView.Layers = test;//property.FindPropertyRelative("vectorSubLayers");
				layerTreeView.Reload();
				layerTreeView.OnGUI(new Rect(position.x, position.y, position.width, 50));
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += (40 * lineHeight);
				//height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("performanceOptions"));
				//height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sourceOptions"));
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
		}
	}
}
