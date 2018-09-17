namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	[Serializable]
	public class ImageryLayer : AbstractLayer, IImageryLayer
	{
		[SerializeField]
		ImageryLayerProperties _layerProperty = new ImageryLayerProperties();

		[NodeEditorElement("Image Layer")]
		public ImageryLayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
			set
			{
				_layerProperty = value;
			}
		}
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Imagery;
			}
		}

		public bool IsLayerActive
		{
			get
			{
				return (_layerProperty.sourceType != ImagerySourceType.None);
			}
		}

		public string LayerSource
		{
			get
			{
				return _layerProperty.sourceOptions.Id;
			}
			internal set
			{
				if (value != _layerProperty.sourceOptions.Id)
				{
					_layerProperty.sourceOptions.Id = value;
					_layerProperty.HasChanged = true;
				}
			}
		}

		public ImageryLayer()
		{

		}

		public ImageryLayer(ImageryLayerProperties properties)
		{
			_layerProperty = properties;
		}

		public void SetLayerSource(ImagerySourceType imageSource)
		{
			if (imageSource != ImagerySourceType.Custom && imageSource != ImagerySourceType.None)
			{
				_layerProperty.sourceType = imageSource;
				_layerProperty.sourceOptions.layerSource = MapboxDefaultImagery.GetParameters(imageSource);

				_layerProperty.HasChanged = true;
			}
			else
			{
				Debug.LogWarning("Invalid style - trying to set " + imageSource.ToString() + " as default style!");
			}
		}

		public void SetLayerSource(string imageSource)
		{
			if (!string.IsNullOrEmpty(imageSource))
			{
				_layerProperty.sourceType = ImagerySourceType.Custom;
				_layerProperty.sourceOptions.Id = imageSource;
			}
			else
			{
				_layerProperty.sourceType = ImagerySourceType.None;
				Debug.LogWarning("Empty source - turning off imagery. ");
			}
			_layerProperty.HasChanged = true;
		}

		public void SetRasterOptions(ImageryRasterOptions rasterOptions)
		{
			_layerProperty.rasterOptions = rasterOptions;
			_layerProperty.HasChanged = true;
		}

		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (ImageryLayerProperties)properties;
			Initialize();
		}

		public void Initialize()
		{
			if (_layerProperty.sourceType != ImagerySourceType.Custom && _layerProperty.sourceType != ImagerySourceType.None)
			{
				_layerProperty.sourceOptions.layerSource = MapboxDefaultImagery.GetParameters(_layerProperty.sourceType);
			}
			_imageFactory = ScriptableObject.CreateInstance<MapImageFactory>();
			_imageFactory.SetOptions(_layerProperty);
			_layerProperty.PropertyHasChanged += RedrawLayer;
		}

		public void RedrawLayer(object sender, System.EventArgs e)
		{
			Factory.SetOptions(_layerProperty);
			NotifyUpdateLayer(_imageFactory, sender as MapboxDataProperty, false);
		}

		public void Remove()
		{
			_layerProperty = new ImageryLayerProperties
			{
				sourceType = ImagerySourceType.None
			};
		}

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
		}

		private MapImageFactory _imageFactory;
		public MapImageFactory Factory
		{
			get
			{
				return _imageFactory;
			}
		}

	}
}
