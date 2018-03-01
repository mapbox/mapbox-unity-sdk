namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class RangeAroundTransformTileProviderOptions : ITileProviderOptions
	{
		public Transform targetTransform;
		public int visibleBuffer;
		public int disposeBuffer;

		public void SetOptions(Transform tgtTransform, int visibleRange, int disposeRange)
		{
			targetTransform = tgtTransform;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
		}
	}
}
