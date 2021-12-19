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
	//important note
	//relationship between Factory manager and data fetcher can be a little messy.
	//factory manager creates a dataTile object and passes it to dataFetcher
	//initial idea was that dataFetcher would fill&complete this object and returns it
	//but we also use same dataTile for caching so IF dataFetcher finds same data (in another dataTile object)
	//in memory, it returns that instance.
	//So factory manager should be ready to handle situations where it sends one dataTile instance and returns whole another
	//but with same tileId, tilesetId etc of course.
	//shortly, you will not always get the same item you send, this is why it's using (int)Key instead of RasterTile references in tracker lists
	//(see MapboxTerrainFactoryManager)
	public abstract class ImageFactoryManager
	{
		public Action<RasterTile> TextureReceived = (s) => { };
		public Action<RasterTile, TileErrorEventArgs> FetchingError = (r, s) => { };
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

		private Dictionary<UnityTile, RasterTile> _tileTracker = new Dictionary<UnityTile, RasterTile>();
		private Dictionary<int, HashSet<UnityTile>> _tileUserTracker = new Dictionary<int, HashSet<UnityTile>>();

		private void ConnectTiles(UnityTile unityTile, Tile dataTile)
		{
			unityTile.AddTile(dataTile);
			dataTile.AddUser(unityTile.CanonicalTileId);
			_tileTracker.Add(unityTile, (RasterTile) dataTile);

			if(!_tileUserTracker.ContainsKey(dataTile.Key))
			{
				_tileUserTracker.Add(dataTile.Key, new HashSet<UnityTile>());
			}
			_tileUserTracker[dataTile.Key].Add(unityTile);
		}

		public virtual void RegisterTile(UnityTile tile)
		{
			var memoryCacheItem = _fetcher.FetchDataInstant(tile.CanonicalTileId, _sourceSettings.Id);
			if (memoryCacheItem != null)
			{
				var dataTile = (RasterTile) memoryCacheItem.Tile;
				ConnectTiles(tile, dataTile);
				SetTexture(tile, dataTile);
				TextureReceived(dataTile);
			}
			else
			{
				ApplyParentTexture(tile);

				var dataTile = CreateTile(tile.CanonicalTileId, _sourceSettings.Id);
				ConnectTiles(tile, dataTile);

				_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId);
			}
		}

		public virtual void UnregisterTile(UnityTile tile, bool clearData = true)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				var dataTile = _tileTracker[tile];
				if (_tileUserTracker.ContainsKey(dataTile.Key))
				{
					_tileUserTracker[dataTile.Key].Remove(tile);
				}

				if (_tileUserTracker[dataTile.Key].Count == 0)
				{
					_tileUserTracker.Remove(dataTile.Key);
					MapboxAccess.Instance.CacheManager.TileDisposed(dataTile, _sourceSettings.Id);
				}

				tile.RemoveTile(dataTile);
				dataTile.RemoveUser(tile.CanonicalTileId);
				_tileTracker.Remove(tile);
			}
			_fetcher.CancelFetching(tile.UnwrappedTileId, _sourceSettings.Id);
			//MapboxAccess.Instance.CacheManager.TileDisposed(tile, _sourceSettings.Id);
		}

		public virtual void ClearTile(UnityTile tile)
		{
			SetTexture(tile, null);
		}

		protected virtual void OnTextureReceived(RasterTile dataTile)
		{
			foreach (var tile in _tileUserTracker[dataTile.Key])
			{
				SetTexture(tile, dataTile);
			}
			TextureReceived(dataTile);
		}

		protected virtual void OnFetcherError(RasterTile dataTile, TileErrorEventArgs errorEventArgs)
		{
			FetchingError(dataTile, errorEventArgs);
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