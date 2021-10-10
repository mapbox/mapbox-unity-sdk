using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.DataFetching;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public abstract class ImageFactoryManager
	{
		public Action<UnityTile, RasterTile> TextureReceived = (t, s) => { };
		public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };
		public bool DownloadFallbackImagery = false;

		protected BaseImageDataFetcher _baseImageDataFetcher;
		protected ImageDataFetcher _fetcher;
		protected LayerSourceOptions _sourceSettings;

		protected ImageFactoryManager(LayerSourceOptions sourceSettings, bool downloadFallbackImagery)
		{
			DownloadFallbackImagery = downloadFallbackImagery;
			_sourceSettings = sourceSettings;

			_baseImageDataFetcher = new BaseImageDataFetcher();
			_fetcher = new ImageDataFetcher();
			_fetcher.TextureReceived += OnTextureReceived;
			_fetcher.FetchingError += OnFetcherError;
		}

		protected abstract RasterTile CreateTile(CanonicalTileId tileId, string tilesetId);
		protected abstract void SetTexture(UnityTile unityTile, RasterTile dataTile);

		private Dictionary<UnityTile, Tile> _tileTracker = new Dictionary<UnityTile, Tile>();

		public virtual void RegisterTile(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				Debug.Log("Tile is already in tracking list?");
			}
			ApplyParentTexture(tile);
			var dataTile = CreateTile(tile.CanonicalTileId, _sourceSettings.Id);
			_tileTracker.Add(tile, dataTile);
			if (tile != null)
			{
				tile.AddTile(dataTile);
			}

			_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId, tile);
		}

		public virtual void UnregisterTile(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				tile.RemoveTile(_tileTracker[tile]);
				_tileTracker.Remove(tile);
			}
			SetTexture(tile, null);
			_fetcher.CancelFetching(tile.UnwrappedTileId, _sourceSettings.Id);
			MapboxAccess.Instance.CacheManager.TileDisposed(tile, _sourceSettings.Id);
		}

		protected virtual void OnTextureReceived(UnityTile unityTile, RasterTile dataTile)
		{
			//unity tile can be null here in some cases like base maps (basemap is z2 imagery we download for fallback)
			//base/fallback images doesn't require unitytile object, they are just pulled and cached
			if (unityTile != null && unityTile.CanonicalTileId != dataTile.Id)
			{
				Debug.Log("wtf");
			}

			if (unityTile != null)
			{
				SetTexture(unityTile, dataTile);
			}

			TextureReceived(unityTile, dataTile);
		}

		private void OnFetcherError(UnityTile unityTile, RasterTile dataTile, TileErrorEventArgs errorEventArgs)
		{
			FetchingError(unityTile, dataTile, errorEventArgs);
		}

		protected virtual void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId;
			tile.SetParentTexture(parent, null);
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical, true);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentTexture(parent, cacheItem.Texture2D);
					break;
				}

				parent = parent.Parent;
			}
		}

		protected virtual void DownloadAndCacheBaseTiles(string imageryLayerSourceId, bool rasterOptionsUseRetina)
		{
			CanonicalTileId tileId;
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					tileId = new CanonicalTileId(2, i, j);
					_baseImageDataFetcher.FetchData(CreateTile(tileId, _sourceSettings.Id), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
				}
			}

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					tileId = new CanonicalTileId(1, i, j);
					_baseImageDataFetcher.FetchData(CreateTile(tileId, _sourceSettings.Id), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
				}
			}

			tileId = new CanonicalTileId(0, 0, 0);
			_baseImageDataFetcher.FetchData(CreateTile(tileId, _sourceSettings.Id), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
		}

		public void SetSourceOptions(LayerSourceOptions properties)
		{
			_sourceSettings = properties;
		}
	}
}