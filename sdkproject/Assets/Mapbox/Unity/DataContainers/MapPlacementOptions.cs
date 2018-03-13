namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapPlacementOptions
	{
		public MapPlacementType placementType = MapPlacementType.AtLocationCenter;
		public bool snapMapToZero = false;
		public IMapPlacementStrategy placementStrategy;
	}
}
