using Mapbox.Map;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.Map.Strategies
{
	public class MapPlacementAtTileCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetCenterMercator(referenceTileRect.Center);
		}
	}
}
