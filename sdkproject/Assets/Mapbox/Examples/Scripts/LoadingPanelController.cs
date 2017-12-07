namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.Map;

	public class LoadingPanelController : MonoBehaviour
	{
		public GameObject Content;

		void Awake()
		{
			var map = FindObjectOfType<AbstractMap>();
			var visualizer = map.MapVisualizer;
			visualizer.OnMapVisualizerStateChanged += (s) =>
			{
				if (this == null)
					return;

				if (s == ModuleState.Finished)
				{
					Content.SetActive(false);
				}
				else if (s == ModuleState.Working)
				{
					// Uncommment me if you want the loading screen to show again
					// when loading new tiles.
					//Content.SetActive(true);
				}
			};
		}
	}
}