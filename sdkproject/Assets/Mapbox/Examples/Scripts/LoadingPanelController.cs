
using System.Linq;

namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using UnityEngine.UI;

	[ExecuteInEditMode]
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

			_map.OnEditorPreviewEnabled += OnEditorPreviewEnabled;
			_map.OnEditorPreviewDisabled += OnEditorPreviewDisabled;

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
					//_content.SetActive(true);
				}

			};
		}

		void OnEditorPreviewEnabled()
		{
			_content.SetActive(false);
		}

		void OnEditorPreviewDisabled()
		{
			_content.SetActive(true);
		}


		void Update()
		{
			var t = _curve.Evaluate(Time.time);
			_text.color = Color.Lerp(Color.clear, Color.white, t);
		}
	}
}
