using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class MapScalingOptions : MapboxDataProperty
	{
		public MapScalingType scalingType = MapScalingType.Custom;
		[Tooltip("Size of each tile in Unity units.")]
		public float unityTileSize = 100f;

		public IMapScalingStrategy scalingStrategy;
	}
}
