﻿namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile.ExtensionMethods;
	using com.spacepuppyeditor;

	[CustomPropertyDrawer(typeof(ColliderOptions))]
	public class ColliderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool isGUIContentSet = false;
		GUIContent[] colliderTypeContent;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			ColliderOptions colliderOptions = (ColliderOptions)EditorHelper.GetTargetObjectOfProperty(property);
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

			colliderTypeProperty.enumValueIndex = EditorGUILayout.Popup(colliderTypeLabel, colliderTypeProperty.enumValueIndex, colliderTypeContent);
			bool colliderHasChanged = colliderTypeProperty.serializedObject.ApplyModifiedProperties();

			if (colliderHasChanged && colliderOptions != null)
			{
				colliderOptions.HasChanged = true;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}
	}
}
