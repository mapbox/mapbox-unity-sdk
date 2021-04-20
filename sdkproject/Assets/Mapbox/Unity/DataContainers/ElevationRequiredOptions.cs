using System;
using UnityEngine;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class ElevationRequiredOptions : MapboxDataProperty
	{
		[Range(0, 100)]
		[Tooltip("Multiplication factor to vertically exaggerate elevation on terrain, does not work with Flat Terrain.")]
		public float exaggerationFactor = 1;

		public override bool NeedsForceUpdate()
		{
			return true;
		}
	}
}
