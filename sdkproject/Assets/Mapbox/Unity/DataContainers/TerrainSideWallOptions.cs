namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	[Serializable]
	public class TerrainSideWallOptions
	{
		[Tooltip("Adds side walls to terrain meshes, reduces visual artifacts.")]
		public bool isActive = false;
		[Tooltip("Height of side walls.")]
		public float wallHeight = 10;
		[Tooltip("Unity material to use for side walls.")]
		public Material wallMaterial;// = Resources.Load("TerrainMaterial", typeof(Material)) as Material;
	}
}
