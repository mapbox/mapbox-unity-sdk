namespace Mapbox.Unity.Map
{
	using System;

	[Serializable]
	public class MapOptions
	{
		public MapLocationOptions locationOptions = new MapLocationOptions();
		public MapExtentOptions extentOptions = new MapExtentOptions(MapExtentType.RangeAroundCenter);
		public MapPlacementOptions placementOptions = new MapPlacementOptions();
		public MapScalingOptions scalingOptions = new MapScalingOptions();
	}
}
