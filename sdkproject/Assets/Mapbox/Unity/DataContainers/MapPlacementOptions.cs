using System;
using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class MapPlacementOptions : MapboxDataProperty
	{
		public MapPlacementType placementType = MapPlacementType.AtLocationCenter;
		public IMapPlacementStrategy placementStrategy;
	}
}
