namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using System.Linq;
	using System;
	using Mapbox.VectorTile.ExtensionMethods;
	using Mapbox.Editor;

	[CustomPropertyDrawer(typeof(CoreVectorLayerProperties))]
	public class CoreVectorLayerPropertiesDrawer : PropertyDrawer
	{
		bool _isGUIContentSet = false;
		GUIContent[] _primitiveTypeContent;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			EditorGUI.BeginProperty(position, null, property);

			var primitiveType = property.FindPropertyRelative("geometryType");

			var primitiveTypeLabel = new GUIContent
			{
				text = "Primitive Type",
				tooltip = "Primitive geometry type of the visualizer, allowed primitives - point, line, polygon."
			};

			var displayNames = primitiveType.enumDisplayNames;
			int count = primitiveType.enumDisplayNames.Length;

			if (!_isGUIContentSet)
			{
				_primitiveTypeContent = new GUIContent[count];
				for (int extIdx = 0; extIdx < count; extIdx++)
				{
					_primitiveTypeContent[extIdx] = new GUIContent
					{
						text = displayNames[extIdx],
						tooltip = EnumExtensions.Description((VectorPrimitiveType)extIdx),
					};
				}
				_isGUIContentSet = true;
			}

			EditorGUI.BeginChangeCheck();
			primitiveType.enumValueIndex = EditorGUILayout.Popup(primitiveTypeLabel, primitiveType.enumValueIndex, _primitiveTypeContent);
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
//
//			if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
//			{
//				EditorGUI.BeginChangeCheck();
//				EditorGUILayout.PropertyField(property.FindPropertyRelative("lineWidth"));
//				if (EditorGUI.EndChangeCheck())
//				{
//					EditorHelper.CheckForModifiedProperty(property);
//				}
//			}
			EditorGUI.EndProperty();
		}
	}
}
