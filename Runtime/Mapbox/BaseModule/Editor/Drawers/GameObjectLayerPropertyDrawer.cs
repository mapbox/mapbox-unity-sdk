using System;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEditor;
using UnityEngine;

namespace Mapbox.BaseModule.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(GameObjectLayerAttribute))]
    public class GameObjectLayerPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    DrawString(rect, property, label, layers);
                    break;
                case SerializedPropertyType.Integer:
                    DrawInt(rect, property, label, layers);
                    break;
                default:
                    EditorGUI.LabelField(rect, label.text, "Use [Layer] with string or int fields only");
                    break;
            }

            EditorGUI.EndProperty();
        }

        private static void DrawString(Rect rect, SerializedProperty property, GUIContent label, string[] layers)
        {
            var index = IndexOf(layers, property.stringValue);
            var newLayer = DrawPopup(rect, label, index, layers);

            if (!property.stringValue.Equals(newLayer, StringComparison.Ordinal))
            {
                property.stringValue = newLayer;
            }
        }

        private static void DrawInt(Rect rect, SerializedProperty property, GUIContent label, string[] layers)
        {
            var layerName = LayerMask.LayerToName(property.intValue);
            var index = IndexOf(layers, layerName);
            var newLayer = DrawPopup(rect, label, index, layers);
            var newLayerNumber = LayerMask.NameToLayer(newLayer);

            if (property.intValue != newLayerNumber)
            {
                property.intValue = newLayerNumber;
            }
        }

        private static string DrawPopup(Rect rect, GUIContent label, int index, string[] layers)
        {
            var newIndex = EditorGUI.Popup(rect, label.text, index, layers);
            return layers[newIndex];
        }

        private static int IndexOf(string[] layers, string layer)
        {
            var index = Array.IndexOf(layers, layer);
            return Mathf.Clamp(index, 0, layers.Length - 1);
        }
    }
}
