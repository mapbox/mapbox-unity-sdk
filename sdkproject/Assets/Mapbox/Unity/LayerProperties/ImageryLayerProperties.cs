namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;


	/*
	public class TestAttribute : PropertyAttribute
	{
		//Type type;

		public TestAttribute()
		{
			Debug.Log("Changed");
			//this.type = t;
		}
	}
*/


	[System.Serializable]
	public class ImageryLayerProperties : LayerProperties, INotifyPropertyChanged
	{
		//[TestAttribute]
		public ImagerySourceType sourceType = ImagerySourceType.MapboxStreets;

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String propertyName = "")
		{
			Debug.Log("NotifyPropertyChanged");
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
					Debug.Log("SourceType");
					this.sourceType = value;
					NotifyPropertyChanged();
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
			layerSource = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets)

		};
		public ImageryRasterOptions rasterOptions = new ImageryRasterOptions();
	}
}
