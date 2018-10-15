using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.Map.Strategies
{
	public class MapPlacementAtLocationCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			map.SetCenterMercator(Conversions.LatLonToMeters(map.CenterLatitudeLongitude));
		}
	}
}
