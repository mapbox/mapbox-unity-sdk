namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.Map;

	public class LoadingPanelController : MonoBehaviour
	{
		public MapVisualizer MapVisualizer;
		public GameObject Content;

		void Awake()
		{
			MapVisualizer.OnMapVisualizerStateChanged += (s) =>
			{
				if (s == ModuleState.Finished)
				{
					Content.SetActive(false);
				}
				else if (s == ModuleState.Working)
				{
					Content.SetActive(true);
				}

			};
		}
	}
}