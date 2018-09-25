namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class RangeAroundTransformTileProviderOptions : ExtentOptions
	{
		public Transform targetTransform;
		public int visibleBuffer;
		public int disposeBuffer;

		public override void SetOptions(ExtentOptions extentOptions)
		{
			RangeAroundTransformTileProviderOptions options = extentOptions as RangeAroundTransformTileProviderOptions;
			if (options != null)
			{
				SetOptions(options.targetTransform, options.visibleBuffer, options.disposeBuffer);
			}
			else
			{
				Debug.LogError("ExtentOptions type mismatch : Using " + extentOptions.GetType() + " to set extent of type " + this.GetType());
			}
		}
		public void SetOptions(Transform tgtTransform = null, int visibleRange = 1, int disposeRange = 1)
		{
			targetTransform = tgtTransform;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
		}
	}
}
