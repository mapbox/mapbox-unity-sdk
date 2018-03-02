namespace Mapbox.Unity.Map
{
	using System;

	[Serializable]
	public class MapOptions
	{
		public MapLocationOptions locationOptions;
		public MapExtentOptions extentOptions;
		public MapPlacementOptions placementOptions;
		public MapScalingOptions scalingOptions;
	}
}
