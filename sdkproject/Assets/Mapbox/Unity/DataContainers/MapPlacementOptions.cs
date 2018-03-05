namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapPlacementOptions
	{
		public MapVisualizationType visualizationType = MapVisualizationType.Flat2D;
		public MapPlacementType placementType = MapPlacementType.AtLocationCenter;

		public IMapPlacementStrategy placementStrategy;
	}
}
