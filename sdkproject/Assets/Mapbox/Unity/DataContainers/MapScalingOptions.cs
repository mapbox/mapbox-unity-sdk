namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class MapScalingOptions
	{
		public MapScalingType scalingType = MapScalingType.Custom;
		//public MapUnitType unitType = MapUnitType.meters;
		[Tooltip("Size of each tile in Unity units.")]
		public float unityTileSize = 100f;

		public IMapScalingStrategy scalingStrategy;
	}
}
