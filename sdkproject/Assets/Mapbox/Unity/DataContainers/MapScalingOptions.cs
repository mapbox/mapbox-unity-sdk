using System;
using Mapbox.Unity.Map.Interfaces;
using UnityEngine;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class MapScalingOptions : MapboxDataProperty
	{
		public MapScalingType scalingType = MapScalingType.Custom;
		[Tooltip("Size of each tile in Unity units.")]
		public float unityTileSize = 1f;

		public IMapScalingStrategy scalingStrategy;
	}
}
