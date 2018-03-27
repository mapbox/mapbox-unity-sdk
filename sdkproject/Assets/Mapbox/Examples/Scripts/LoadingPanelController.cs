namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEngine.UI;

	public class LoadingPanelController : MonoBehaviour
	{
		[SerializeField]
		GameObject _content;

		[SerializeField]
		Text _text;

		[SerializeField]
		AnimationCurve _curve;

		AbstractMap _map;
		void Awake()
		{
			_map = FindObjectOfType<AbstractMap>();
			_map.OnInitialized += _map_OnInitialized;
		}

		void _map_OnInitialized()
		{
			var visualizer = _map.MapVisualizer;
			_text.text = "LOADING";
			visualizer.OnMapVisualizerStateChanged += (s) =>
			{

				if (this == null)
					return;

				if (s == ModuleState.Finished)
				{
					_content.SetActive(false);
				}
				else if (s == ModuleState.Working)
				{

					// Uncommment me if you want the loading screen to show again
					// when loading new tiles.
					//Content.SetActive(true);
				}
			};
		}

		void Update()
		{
			var t = _curve.Evaluate(Time.time);
			_text.color = Color.Lerp(Color.clear, Color.white, t);
		}
	}
}