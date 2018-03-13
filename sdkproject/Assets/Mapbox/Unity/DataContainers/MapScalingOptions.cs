namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapScalingOptions
	{
		public MapScalingType scalingType = MapScalingType.Custom;
		//public MapUnitType unitType = MapUnitType.meters;
		public float unityTileSize = 100f;

		public IMapScalingStrategy scalingStrategy;
	}
}
