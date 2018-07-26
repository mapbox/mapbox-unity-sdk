namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;

	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties, INotifyPropertyChanged
	{
		//[TestAttribute]
		public ImagerySourceType sourceType = ImagerySourceType.MapboxStreets;

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public ImagerySourceType SourceType
		{
			get
			{
				return sourceType;
			}
			set
			{
				
				if (value != this.sourceType)
				{
					this.sourceType = value;
					NotifyPropertyChanged("SourceType");
				}
			}
		}

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
