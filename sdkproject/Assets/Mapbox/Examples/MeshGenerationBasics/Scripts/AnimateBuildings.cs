namespace Mapbox.Examples
{
	using UnityEngine;

	public class AnimateBuildings : MonoBehaviour
	{
		void Awake()
		{
			var runnable = AnimateBuildingsRunnable.Instance;
			StartCoroutine(runnable.Run(this));
		}
	}
}