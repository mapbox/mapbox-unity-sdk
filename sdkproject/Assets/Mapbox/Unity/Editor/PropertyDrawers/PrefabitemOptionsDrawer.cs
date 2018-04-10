namespace Mapbox.Editor
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEditor;
	using System.Collections.Generic;

	[CustomPropertyDrawer(typeof(ColliderOptions))]
	public class PrefabItemOptionsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
		}
	}
}