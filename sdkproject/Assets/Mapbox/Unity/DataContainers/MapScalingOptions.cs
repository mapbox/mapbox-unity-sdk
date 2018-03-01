namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapScalingOptions
	{
		public MapScalingType scalingType;
		public MapUnitType unitType;
		public float unityToMercatorConversionFactor;

		public IMapScalingStrategy scalingStrategy;
	}
}
