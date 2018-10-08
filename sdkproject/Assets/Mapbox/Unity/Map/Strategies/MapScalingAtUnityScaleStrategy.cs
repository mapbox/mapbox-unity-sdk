using Mapbox.Map;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.Map.Strategies
{
	public class MapScalingAtUnityScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetWorldRelativeScale((float)(map.Options.scalingOptions.unityTileSize / referenceTileRect.Size.x));
		}
	}
}
