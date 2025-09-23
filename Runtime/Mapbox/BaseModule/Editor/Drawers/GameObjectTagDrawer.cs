using System;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mapbox.BaseModule.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(GameObjectTagAttribute))]
    public class GameObjectTagDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {
                var tagList = InternalEditorUtility.tags;
                var index = 0;
                for (var i = 1; i < tagList.Length; i++)
                {
                    if (tagList[i].Equals(property.stringValue, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                }

                var newIndex = EditorGUI.Popup(rect, label.text, index, tagList);
                var newValue = tagList[newIndex];
                if (!property.stringValue.Equals(newValue, StringComparison.Ordinal))
                {
                    property.stringValue = newValue;
                }
            }
            else
            {
                EditorGUI.LabelField(rect, label.text, "Use [Tag] with string fields only");
            }

            EditorGUI.EndProperty();
        }
    }
}
