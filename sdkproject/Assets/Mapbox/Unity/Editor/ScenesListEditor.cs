namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	using UnityEditor;

	[CustomEditor(typeof(ScenesList))]
	public class ScenesListEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			ScenesList e = target as ScenesList;

			if (GUILayout.Button("Link Listed Scenes"))
			{
				e.LinkScenes();
			}
		}
	}
}