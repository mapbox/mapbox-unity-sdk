namespace Mapbox.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Mapbox.Unity.Utilities;

    [CustomPropertyDrawer(typeof(GeocodeAttribute))]
    public class GeocodeAttributeDrawer : PropertyDrawer
    {
        const string searchButtonContent = "Search";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float buttonWidth = EditorGUIUtility.singleLineHeight * 4;

            Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(fieldRect, property);

            if (GUI.Button(buttonRect, searchButtonContent))
            {
                GeocodeAttributeSearchWindow.Open(property);
            }
        }
    }
}