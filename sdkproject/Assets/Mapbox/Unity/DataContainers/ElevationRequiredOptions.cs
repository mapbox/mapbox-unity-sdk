namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class ElevationRequiredOptions
	{
		[Tooltip("Unity material used for rendering terrain tiles.")]
		public Material baseMaterial;
		[Tooltip("Add Unity Physics collider to terrain tiles, used for detecting collisions etc.")]
		public bool addCollider = false;
		[Range(0, 100)]
		[Tooltip("Multiplication factor to vertically exaggerate elevation on terrain, does not work with Flat Terrain.")]
		public float exaggerationFactor = 1;

	}
}
