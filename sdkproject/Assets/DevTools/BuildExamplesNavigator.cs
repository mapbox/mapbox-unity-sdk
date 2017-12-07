namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	using UnityEngine.UI;

	public class BuildExamplesNavigator : MonoBehaviour
	{
		[SerializeField]
		GameObject _buttonPrefab;

		void Awake()
		{
			var scenes = Resources.Load<ScenesList>("Mapbox/ScenesList").SceneList;
			foreach (var scene in scenes)
			{
				var button = Instantiate(_buttonPrefab) as GameObject;
				button.transform.SetParent(GetComponentInChildren<VerticalLayoutGroup>().transform);
				var text = button.GetComponentInChildren<Text>();
				text.text = scene.ScenePath.Replace(".unity", "").Replace("Assets/", "");
			}
		}
	}
}