namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class LayerPerformanceOptions
	{
		public bool isEnabled = true;
		public int entityPerCoroutine = 20;
	}
}
