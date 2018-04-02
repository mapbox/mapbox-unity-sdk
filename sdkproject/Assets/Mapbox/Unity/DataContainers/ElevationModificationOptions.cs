namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class ElevationModificationOptions
	{
		[Tooltip("Number of samples to use for terrain mesh.")]
		public int sampleCount = 10;
		[Tooltip("Use world relative scale to scale terrain height.")]
		public bool useRelativeHeight = false;
		[Tooltip("Earth radius in Unity units.")]
		public float earthRadius = 1000f;
	}
}
