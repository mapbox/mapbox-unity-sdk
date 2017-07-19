namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Utilities;

	/// <summary>
	/// Custom property drawer for style searching. <para/>
	/// Includes a search window to enable listing of styles associated with a username. 
	/// Requires a Mapbox token be set for the project.
	/// </summary>
	[CustomPropertyDrawer(typeof(StyleSearchAttribute))]
	public class StyleSearchAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float buttonWidth = EditorGUIUtility.singleLineHeight * 4;

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			if (GUILayout.Button("Search"))
			{
				StyleSearchWindow.Open(property);
			}

			var id = property.FindPropertyRelative("Id");
			var name = property.FindPropertyRelative("Name");
			var modified = property.FindPropertyRelative("Modified");

			id.stringValue = EditorGUILayout.TextField("Id: ", id.stringValue);
			name.stringValue = EditorGUILayout.TextField("Name: ", name.stringValue);
			modified.stringValue = EditorGUILayout.TextField("Modified: ", modified.stringValue);

			if (GUILayout.Button("Clear"))
			{
				id.stringValue = "";
				name.stringValue = "";
				modified.stringValue = "";
			}
		}
	}
}