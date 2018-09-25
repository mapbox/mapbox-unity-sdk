namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class CameraBoundsTileProviderOptions : ExtentOptions
	{
		public Camera camera;
		public int visibleBuffer;
		public int disposeBuffer;

		public override void SetOptions(ExtentOptions extentOptions)
		{
			CameraBoundsTileProviderOptions options = extentOptions as CameraBoundsTileProviderOptions;
			if (options != null)
			{
				camera = options.camera;
				visibleBuffer = options.visibleBuffer;
				disposeBuffer = options.disposeBuffer;
			}
			else
			{
				Debug.LogError("ExtentOptions type mismatch : Using " + extentOptions.GetType() + " to set extent of type " + this.GetType());
			}
		}

		public void SetOptions(Camera mapCamera, int visibleRange, int disposeRange)
		{
			camera = mapCamera;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
		}
	}
}
