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
		bool isGUIContentSet = false;
		GUIContent[] colliderTypeContent;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var map = (AbstractMap)property.serializedObject.targetObject;
			var colliderOptions = map.VectorData.LayerProperty.vectorSubLayers[0].colliderOptions;

			EditorGUI.BeginProperty(position, null, property);
			var colliderTypeLabel = new GUIContent
			{
				text = "Collider Type",
				tooltip = "The type of collider added to game objects in this layer."
			};
			var colliderTypeProperty = property.FindPropertyRelative("colliderType");

			var displayNames = colliderTypeProperty.enumDisplayNames;
			int count = colliderTypeProperty.enumDisplayNames.Length;

			if (!isGUIContentSet)
			{
				colliderTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					colliderTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((ColliderType)extIdx),
					};
				}
				isGUIContentSet = true;
			}

			colliderOptions.ColliderType = (ColliderType)EditorGUILayout.Popup(colliderTypeLabel, colliderTypeProperty.enumValueIndex, colliderTypeContent);
			//this will trigger changes if the user selects the enum dropdown in the UI and chooses the SAME option...
			//is there a better way to compare pre/post values other than change check?
			/*
			EditorGUI.BeginChangeCheck();
			colliderTypeProperty.enumValueIndex = EditorGUI.Popup(position, colliderTypeLabel, colliderTypeProperty.enumValueIndex, colliderTypeContent);
			if(EditorGUI.EndChangeCheck())
			{
				colliderOptions.HasChanged = true;
			}
			*/
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}
	}
}
