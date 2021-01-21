using CustomImageLayerSample;
using UnityEngine.UI;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;

	public enum MapImageType
	{
		BasicMapboxStyle,
		Custom,
		None
	}

	/// <summary>
	/// Uses raster image services to create materials & textures for terrain
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Image Factory")]
	public class MapImageFactory : AbstractTileFactory
	{
		public ImageFactoryManager ImageFactoryManager;

		[SerializeField]
		ImageryLayerProperties _properties;
		protected ImageDataFetcher DataFetcher;

		public ImageryLayerProperties Properties
		{
			get
			{
				return _properties;
			}
		}

		public string TilesetId
		{
			get
			{
				return _properties.sourceOptions.Id;
			}

			set
			{
				_properties.sourceOptions.Id = value;
			}
		}

		protected virtual void OnDestroy()
		{
			//unregister events
			if (ImageFactoryManager != null)
			{
				ImageFactoryManager.FetchingError -= OnFetchingError;
			}
		}

		protected override void OnInitialized()
		{
			ImageFactoryManager = new MapboxImageFactoryManager(_fileSource, TilesetId, true);
			ImageFactoryManager.FetchingError += OnFetchingError;
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ImageryLayerProperties)options;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (_properties.sourceType == ImagerySourceType.None)
			{
				tile.SetRasterData(null);
			}
			else
			{
				ImageFactoryManager.RegisterTile(tile);
			}
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			ImageFactoryManager.UnregisterTile(tile);
		}

		private void OnFetchingError(UnityTile tile, RasterTile rasterTile, TileErrorEventArgs errorEventArgs)
		{
			OnErrorOccurred(errorEventArgs);
		}

		protected override void OnErrorOccurred(UnityTile tile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
			if (tile != null)
			{
				foreach (var exception in e.Exceptions)
				{
					Debug.Log(exception);
				}
			}
		}

		protected override void OnPostProcess(UnityTile tile)
		{

		}

		protected override void OnUnbindEvents()
		{

		}
	}
}
