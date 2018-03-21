namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	#if UNITY_EDITOR
	using UnityEditor;
	#endif
	public class ScenesList : ScriptableObject
	{
		public SceneData[] SceneList;

		//ensure that linked scenes are stored in this object
		#if UNITY_EDITOR

		public void LinkScenes()
		{
			for (int i = 0; i < SceneList.Length; i++)
			{
				if (!ThisAssetContainsScene(SceneList[i]))
				{
					//duplicate the asset
					var path = AssetDatabase.GetAssetPath(this);
					var newScene = ScriptableObject.CreateInstance<SceneData>();
					newScene.name = SceneList[i].name;
					newScene.ScenePath = SceneList[i].ScenePath;
					newScene.Text = SceneList[i].Text;
					newScene.Image = SceneList[i].Image;

					//assign it to the current scene list
					AssetDatabase.AddObjectToAsset(newScene, path);
					SceneList[i] = newScene;

					//save the scenelist
					EditorUtility.SetDirty(this);
					AssetDatabase.SaveAssets();

					//TODO: clean up unreferenced sub-assets with Destroy
				}
			}
		}

		private bool ThisAssetContainsScene(SceneData scene)
		{
			var path = AssetDatabase.GetAssetPath(this);
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
			foreach (var asset in assets)
			{
				if (asset == scene)
				{
					//Debug.Log("Asset " + scene + " is contained in " + path);
					return true;
				}
			}

			return false;

		}
		#endif
	}
}
