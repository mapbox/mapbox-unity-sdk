using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapPlacementOptions : MapboxDataProperty
	{
		public MapPlacementType placementType = MapPlacementType.AtLocationCenter;
		public bool snapMapToZero = false;
		public IMapPlacementStrategy placementStrategy;
	}
}
