namespace Mapbox.Examples
{
	using UnityEngine;

	public class ChangeShadowDistance : MonoBehaviour
	{
		public int ShadowDistance;

		void Start()
		{
			QualitySettings.shadowDistance = ShadowDistance;
		}
	}
}