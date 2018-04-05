namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomPropertyDrawer(typeof(ColliderOptions))]
	public class ColliderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Collider Type", tooltip = "The type of collider added to game objects in this layer." });
			var colliderTypeProperty = property.FindPropertyRelative("colliderType");

			List<GUIContent> enumContent = new List<GUIContent>();
			foreach(var enumValue in colliderTypeProperty.enumDisplayNames)
			{
				var guiContent =  new GUIContent { text = enumValue, tooltip =  ((Unity.Map.ColliderType)colliderTypeProperty.enumValueIndex).Description()} ;
				enumContent.Add(guiContent);
			}

			EditorGUI.indentLevel--;
			colliderTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, colliderTypeProperty.enumValueIndex, enumContent.ToArray());
			EditorGUI.indentLevel++;
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}
	}
}
