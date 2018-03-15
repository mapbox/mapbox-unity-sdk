namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class LayerPerformanceOptions
	{
		[Tooltip("Enable Coroutines to distribute tile loading using coroutines, reduces the load on the main thread and keeps applications responsive. First load may be slower but subsequent loading will be faster. ")]
		public bool isEnabled = true;
		[Tooltip("Number of feature entities to group in one single coroutine call. ")]
		public int entityPerCoroutine = 20;
	}
}
