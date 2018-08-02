namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;

	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties
	{
		public event Action OnPropertyUpdated = delegate { };

		public void UpdateProperty()
		{
			if (OnPropertyUpdated != null)
			{
				OnPropertyUpdated();
			}
		}

		//[TestAttribute]
		public ImagerySourceType sourceType = ImagerySourceType.MapboxStreets;

		//[StyleSearch]
		// TODO : Do we really need a separate DS for default styles ??
		// Style struct should be enough to hold all tile-service info?
		//public Style CustomStyle = new Style();

		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			
			//if(SourceType == ImagerySourceType.MapboxDark)
			//{}
			layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets)
			//layerSource = MapboxDefaultImagery.GetParameters(sourceType);

		};

		/*
		public LayerSourceOptions sourceOptions()
		{
			LayerSourceOptions layerSourceOptions = new LayerSourceOptions()
			{
				isActive = true,
				//layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets)
				layerSource = MapboxDefaultImagery.GetParameters(sourceType)

			};
			return layerSourceOptions;
		}
		*/
		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();
	}
}
