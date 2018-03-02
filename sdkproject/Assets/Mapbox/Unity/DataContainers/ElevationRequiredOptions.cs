namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class ElevationRequiredOptions
	{
		public ElevationLayerType elevationLayerType = ElevationLayerType.None;
		public Material baseMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
		public bool addCollider = false;
		[Range(1, 100)]
		public float exaggerationFactor = 1;

	}
}
