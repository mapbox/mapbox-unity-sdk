using System;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace CustomImageLayerSample
{
	public abstract class ImageFactoryManager
	{
		public Action<UnityTile, RasterTile> TextureReceived = (t, s) => { };
		public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

		protected BaseImageDataFetcher _baseImageDataFetcher;
		protected ImageDataFetcher _fetcher;
		protected string _tilesetId;

		protected ImageFactoryManager(string tilesetId)
		{
			_tilesetId = tilesetId;

			_baseImageDataFetcher = new BaseImageDataFetcher();
			_fetcher = new ImageDataFetcher();
			_fetcher.TextureReceived += OnTextureReceived;
			_fetcher.FetchingError += OnFetcherError;
		}

		protected abstract RasterTile CreateTile(CanonicalTileId tileId, string tilesetId);
		protected abstract void SetTexture(UnityTile unityTile, RasterTile dataTile);

		public virtual void RegisterTile(UnityTile tile)
		{
			ApplyParentTexture(tile);
			var dataTile = CreateTile(tile.CanonicalTileId, _tilesetId);
			if (tile != null)
			{
				tile.AddTile(dataTile);
			}

			_fetcher.FetchData(dataTile, _tilesetId, tile.CanonicalTileId, true, tile);
		}

		public virtual void UnregisterTile(UnityTile tile)
		{
			_fetcher.CancelFetching(tile.UnwrappedTileId, _tilesetId);
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
			var parent = tile.UnwrappedTileId.Parent;
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_tilesetId, parent.Canonical);
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
					_baseImageDataFetcher.FetchData(CreateTile(tileId, _tilesetId), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
				}
			}

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					tileId = new CanonicalTileId(1, i, j);
					_baseImageDataFetcher.FetchData(CreateTile(tileId, _tilesetId), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
				}
			}

			tileId = new CanonicalTileId(0, 0, 0);
			_baseImageDataFetcher.FetchData(CreateTile(tileId, _tilesetId), imageryLayerSourceId, tileId, rasterOptionsUseRetina);
		}
	}
}