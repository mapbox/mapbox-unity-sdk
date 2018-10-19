namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;

	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties
	{
		public ImagerySourceType sourceType = ImagerySourceType.MapboxStreets;

		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets)
		};

		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();

		public override bool NeedsForceUpdate()
		{
			return true;
		}
	}
}
