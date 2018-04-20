namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity;

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
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			EditorGUILayout.HelpBox("Style Id and Modified date is required for optimized tileset feature. You can copy&paste those values from Styles page under your Mapbox Account or use the search feature to fetch them automatically.", MessageType.Info);
			EditorGUI.indentLevel++;


			var id = property.FindPropertyRelative("Id");

			var name = property.FindPropertyRelative("Name");
			var modified = property.FindPropertyRelative("Modified");

			id.stringValue = EditorGUILayout.TextField("Style Id: ", id.stringValue);
			name.stringValue = EditorGUILayout.TextField("Name: ", name.stringValue);
			modified.stringValue = EditorGUILayout.TextField("Modified: ", modified.stringValue);

			EditorGUILayout.BeginHorizontal();
			if (string.IsNullOrEmpty(MapboxAccess.Instance.Configuration.AccessToken))
			{
				GUI.enabled = false;
				GUILayout.Button("Need Mapbox Access Token");
				GUI.enabled = true;
			}
			else
			{
				if (GUILayout.Button("Search"))
				{
					StyleSearchWindow.Open(property);
				}
			}

			if (GUILayout.Button("Clear", GUILayout.Width(100)))
			{
				id.stringValue = "";
				name.stringValue = "";
				modified.stringValue = "";
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
		}
	}
}
