namespace Utilities
{
	using UnityEngine;
	using UnityEngine.UI;
	using UnityEngine.SceneManagement;

	[RequireComponent(typeof(Button))]
	public class LoadSceneOnButtonPress : MonoBehaviour
	{
		Button _button;

		void Awake()
		{
			_button = GetComponent<Button>();
			_button.onClick.AddListener(LoadScene);
		}

		void LoadScene()
		{
			var scenePath = GetComponentInChildren<Text>().text;
			SceneManager.LoadScene(scenePath);
		}
	}
}