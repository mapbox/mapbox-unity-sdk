namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class ElevationModificationOptions
	{
		public int sampleCount = 10;
		public bool useRelativeHeight = false;
		public float earthRadius = 1000f;
	}
}
