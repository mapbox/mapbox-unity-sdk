using Mapbox.Platform;
using Mapbox.Unity.CustomLayer;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.DataFetching;
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

	public class MapImageFactory : AbstractTileFactory
	{
		public ImageFactoryManager ImageFactoryManager;

		[SerializeField] ImageryLayerProperties _properties;
		protected ImageDataFetcher DataFetcher;

		public MapImageFactory(ImageryLayerProperties properties)
		{
			_properties = properties;
			ImageFactoryManager = new MapboxImageFactoryManager(_properties, true);
			ImageFactoryManager.FetchingError += OnFetchingError;
		}

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

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ImageryLayerProperties)options;
			ImageFactoryManager?.SetSourceOptions(_properties.sourceOptions);
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

		protected override void OnStopped(UnityTile tile)
		{
			ImageFactoryManager.Stop(tile);
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			ImageFactoryManager.UnregisterTile(tile);
		}

		protected override void OnClearTile(UnityTile tile)
		{
			ImageFactoryManager.ClearTile(tile);
		}

		private void OnFetchingError(RasterTile rasterTile, TileErrorEventArgs errorEventArgs)
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
