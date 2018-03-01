namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class TerrainSideWallOptions
	{
		public bool isActive = false;
		public float wallHeight = 10;
		public Material wallMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
	}
}
