namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	using UnityEditor;
	using System.IO;

	public static class NavigationBuilder
	{
		[MenuItem("Mapbox/Serialize Example Scenes")]
		public static void AddExampleScenesToBuildSettings()
		{
			var allScenes = PathHelpers.AllScenes;
			EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[allScenes.Count + 1];

			var mainScenes = AssetDatabase.FindAssets("main t:Scene");
			var mainScene = AssetDatabase.GUIDToAssetPath(mainScenes[0]);
			buildScenes[0] = new EditorBuildSettingsScene(mainScene, true);

			for (int i = 0; i < allScenes.Count; i++)
			{
				EditorBuildSettingsScene sceneToAdd = new EditorBuildSettingsScene(allScenes[i], true);
				buildScenes[i + 1] = sceneToAdd;
			}

			EditorBuildSettings.scenes = buildScenes;
			SaveSceneList();
			AssetDatabase.Refresh();
		}

		static void SaveSceneList()
		{
			var list = ScriptableObject.CreateInstance<ScenesList>();
			AssetDatabase.CreateAsset(list, Constants.Path.SCENELIST);

			var scenes = EditorBuildSettings.scenes;
			list.SceneList = new SceneData[scenes.Length - 1];
			for (int i = 0; i < scenes.Length - 1; ++i)
			{
				string scenePath = scenes[i + 1].path;
				string name = Path.GetFileNameWithoutExtension(scenePath);
				string imagePath = Directory.GetParent(scenePath) + "/Screenshots/" + name + ".png";
				Texture2D image = null;
				if (File.Exists(imagePath))
				{
					image = (Texture2D)AssetDatabase.LoadAssetAtPath(imagePath, typeof(Texture2D));
				}

				//todo text
				TextAsset text = null;

				var scene = ScriptableObject.CreateInstance<SceneData>();
				scene.name = name;
				scene.Name = name;
				scene.ScenePath = scenePath;
				scene.Text = text;
				scene.Image = image;

				AssetDatabase.AddObjectToAsset(scene, list);
				list.SceneList[i] = scene;
			}

			EditorUtility.SetDirty(list);
			AssetDatabase.SaveAssets();
		}

		static void Verify(string path)
		{
			Debug.Log("NavigationBuilder: " + path);
			var scenes = Resources.Load<ScenesList>("Mapbox/ScenesList").SceneList;
			foreach (var scene in scenes)
			{
				Debug.Log("NavigationBuilder: " + scene);
			}
		}
	}
}
