namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	[Serializable]
	public class ImageryLayer : IImageryLayer
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
			set
			{
				_layerProperty.sourceOptions.Id = value;
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
		}

		public void SetRasterOptions(ImageryRasterOptions rasterOptions)
		{
			_layerProperty.rasterOptions = rasterOptions;
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
		public MapImageFactory Factory
		{
			get
			{
				return _imageFactory;
			}
		}
		private MapImageFactory _imageFactory;
	}
}
