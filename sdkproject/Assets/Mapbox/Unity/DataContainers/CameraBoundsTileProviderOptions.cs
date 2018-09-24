namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class CameraBoundsTileProviderOptions : TileProviderOptions
	{
		public Camera camera;
		public int visibleBuffer;
		public int disposeBuffer;

		public void SetOptions(Camera mapCamera, int visibleRange, int disposeRange, float updateTimeInterval)
		{
			camera = mapCamera;
			visibleBuffer = visibleRange;
			disposeBuffer = disposeRange;
		}
	}
}
