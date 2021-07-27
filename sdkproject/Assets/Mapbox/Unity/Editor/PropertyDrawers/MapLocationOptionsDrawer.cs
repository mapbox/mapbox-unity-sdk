using Mapbox.Unity.DataContainers;

namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(MapLocationOptions))]
	public class MapLocationOptionsDrawer : PropertyDrawer
	{
		static float _lineHeight = EditorGUIUtility.singleLineHeight;
		private Vector3 zoomValues;

		public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
		{
			EditorGUI.indentLevel++;
			GUILayout.Space(-1f * _lineHeight);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(property.FindPropertyRelative("latitudeLongitude"), label);
			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}

			EditorGUI.BeginChangeCheck();

			var minZoomProp = property.FindPropertyRelative("MinZoom");
			var maxZoomProp = property.FindPropertyRelative("MaxZoom");
			var zoomProp = property.FindPropertyRelative("zoom");

			//TODO DEAR PERSON READING THIS; PLEASE FIX SECTION BELOW
			zoomValues.Set(minZoomProp.floatValue, zoomProp.floatValue, maxZoomProp.floatValue);
			zoomValues = EditorGUILayout.Vector3Field("Zoom (Min/Cur/Max)", zoomValues);

			minZoomProp.floatValue = (int) Mathf.Max(0, Mathf.Min(zoomValues.z, zoomValues.x));
			maxZoomProp.floatValue = (int) Mathf.Min(20, Mathf.Max(zoomValues.z, zoomValues.x));
			zoomProp.floatValue = Mathf.Clamp(zoomValues.y, zoomValues.x, zoomValues.z);

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
			EditorGUI.indentLevel--;
		}
	}
}