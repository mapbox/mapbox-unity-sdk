namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class ElevationModificationOptions
	{
		public ElevationLayerType elevationLayerType = ElevationLayerType.None;
		public Material baseMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
		public int sampleCount = 10;
		public bool addCollider = false;
		public float exaggerationFactor = 1;
		public bool useRelativeHeight = true;
	}
}
