using System;
using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class MapPlacementOptions : MapboxDataProperty
	{
		public MapPlacementType placementType = MapPlacementType.AtLocationCenter;
		public bool snapMapToZero = false;
		public IMapPlacementStrategy placementStrategy;
	}
}
