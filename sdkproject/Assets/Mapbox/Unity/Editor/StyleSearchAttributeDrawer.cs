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
		const string searchButtonContent = "Search";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)

		{
			float buttonWidth = EditorGUIUtility.singleLineHeight * 4;

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			if (GUI.Button(position, searchButtonContent))
			{
				StyleSearchWindow.Open(property);
			}

			EditorGUILayout.TextField("Id: ", property.FindPropertyRelative("Id").stringValue);
			EditorGUILayout.TextField("Name: ", property.FindPropertyRelative("Name").stringValue);
			EditorGUILayout.TextField("Modified: ", property.FindPropertyRelative("Modified").stringValue);
		}
	}
}