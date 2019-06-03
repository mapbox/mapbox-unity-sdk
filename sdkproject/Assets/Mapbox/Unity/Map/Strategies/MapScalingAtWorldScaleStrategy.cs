using Mapbox.Unity.Map.Interfaces;
using UnityEngine;

namespace Mapbox.Unity.Map.Strategies
{
	public class MapScalingAtWorldScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			var scaleFactor = Mathf.Pow(2, (map.AbsoluteZoom - map.InitialZoom));
			map.SetWorldRelativeScale(scaleFactor);
		}
	}
}
