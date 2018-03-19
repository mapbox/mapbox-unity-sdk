namespace Mapbox.Unity.Map
{
	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties
	{
		public ImagerySourceType sourceType = ImagerySourceType.MapboxStreets;

		//[StyleSearch]
		// TODO : Do we really need a separate DS for default styles ??
		// Style struct should be enough to hold all tile-service info?
		//public Style CustomStyle = new Style();
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets)

		};
		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();
	}
}
