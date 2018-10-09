namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class ImageryRasterOptions : MapboxDataProperty
	{
		[Tooltip("Use higher resolution Mapbox imagery for retina displays; better visual quality and larger texture sizes.")]
		public bool useRetina = false;
		[Tooltip("Use Unity compression for the tile texture.")]
		public bool useCompression = false;
		[Tooltip("Use texture with Unity generated mipmaps.")]
		public bool useMipMap = false;

		public override bool NeedsForceUpdate()
		{
			return true;
		}
	}
}
