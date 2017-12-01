namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	using UnityEditor;

	public static class NavigationBuilder
	{
		[MenuItem("Mapbox/AddExamplesScenesToBuildSettings")]
		public static void AddExampleScenesToBuildSettings()
		{
			var allScenes = PathHelpers.AllScenes;
			EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[allScenes.Count + 1];

			var mainScenes = AssetDatabase.FindAssets("main t:Scene");
			var mainScene = AssetDatabase.GUIDToAssetPath(mainScenes[0]);
			buildScenes[0] = new EditorBuildSettingsScene(mainScene, true);

			for (int i = 0; i < allScenes.Count; i++)
			{
				var sceneToAdd = new EditorBuildSettingsScene(allScenes[i], true);
				buildScenes[i + 1] = sceneToAdd;
			}

			EditorBuildSettings.scenes = buildScenes;

			SaveSceneList();
		}

		static void SaveSceneList()
		{
			ScenesList list = (ScenesList)AssetDatabase.LoadAssetAtPath("Assets/Mapbox/Resources/ScenesList.asset", typeof(ScenesList));
			if (list == null)
			{
				list = ScriptableObject.CreateInstance<ScenesList>();
				AssetDatabase.CreateAsset(list, "Assets/Resources/Mapbox/ScenesList.asset");
			}

			var scenes = EditorBuildSettings.scenes;
			list.SceneList = new string[scenes.Length - 1];
			for (int i = 0; i < scenes.Length - 1; ++i)
			{
				list.SceneList[i] = scenes[i + 1].path;
			}

			EditorUtility.SetDirty(list);
			AssetDatabase.SaveAssets();
		}

		static void Verify(string path)
		{
			Debug.Log("NavigationBuilder: " + path);
			var scenes = Resources.Load<ScenesList>("ScenesList").SceneList;
			foreach (var scene in scenes)
			{
				Debug.Log("NavigationBuilder: " + scene);
			}
		}
	}
}
