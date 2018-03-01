namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	[Serializable]
	public class ImageryLayer : IImageryLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Imagery;
			}
		}

		[SerializeField]
		bool _isLayerActive;
		public bool IsLayerActive
		{
			get
			{
				return _isLayerActive;
			}
			set
			{
				_isLayerActive = value;
			}
		}

		[SerializeField]
		string _layerSource;
		public string LayerSource
		{
			get
			{
				return _layerSource;
			}
			set
			{
				_layerSource = value;
			}
		}

		[SerializeField]
		ImageryLayerProperties _layerProperty;
		public LayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
			set
			{
				_layerProperty = (ImageryLayerProperties)value;
			}
		}

		public void Initialize(LayerProperties properties)
		{
			var imageLayerProperties = (ImageryLayerProperties)properties;
			if (imageLayerProperties.sourceType != ImagerySourceType.Custom && imageLayerProperties.sourceType != ImagerySourceType.None)
			{
				imageLayerProperties.sourceOptions.layerSource = MapboxDefaultImagery.GetParameters(imageLayerProperties.sourceType);
			}
			_imageFactory = ScriptableObject.CreateInstance<MapImageFactory>();
			_imageFactory._mapIdType = imageLayerProperties.sourceType;
			_imageFactory._customStyle = imageLayerProperties.sourceOptions.layerSource;
			_imageFactory._useCompression = imageLayerProperties.rasterOptions.useCompression;
			_imageFactory._useMipMap = imageLayerProperties.rasterOptions.useMipMap;
			_imageFactory._useRetina = imageLayerProperties.rasterOptions.useRetina;
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
		public MapImageFactory ImageFactory
		{
			get
			{
				return _imageFactory;
			}
		}
		private MapImageFactory _imageFactory;
	}
}
