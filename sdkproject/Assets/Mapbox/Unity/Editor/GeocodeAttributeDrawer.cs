namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Utilities;

	/// <summary>
	/// Custom property drawer for geocodes <para/>
	/// Includes a search window to enable search of Lat/Lon via geocoder. 
	/// Requires a Mapbox token be set for the project
	/// </summary>
	[CustomPropertyDrawer(typeof(GeocodeAttribute))]
	public class GeocodeAttributeDrawer : PropertyDrawer
	{
		const string searchButtonContent = "Search";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUILayout.BeginHorizontal();
			if (string.IsNullOrEmpty(label.text))
			{

				property.stringValue = EditorGUILayout.TextField(property.stringValue);
			}
			else
			{
				property.stringValue = EditorGUILayout.TextField(label, property.stringValue);
			}
			if (GUILayout.Button(new GUIContent(searchButtonContent), (GUIStyle)"minibutton", GUILayout.MaxWidth(100)))
			{
				GeocodeAttributeSearchWindow.Open(property);
			}
			GUILayout.EndHorizontal();
		}
	}
}