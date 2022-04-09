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

		//this is in-use unity tile to raster tile
		protected Dictionary<UnityTile, RasterTile> _tileTracker = new Dictionary<UnityTile, RasterTile>();
		protected Dictionary<int, HashSet<UnityTile>> _tileUserTracker = new Dictionary<int, HashSet<UnityTile>>();
		//this is grand parent id to raster tile
		//so these two are vastly separate, don't try to optimize
		protected Dictionary<CanonicalTileId, RasterTile> _requestedTiles = new Dictionary<CanonicalTileId, RasterTile>();
		protected Dictionary<int, HashSet<UnityTile>> _tileWaitingList = new Dictionary<int, HashSet<UnityTile>>();

		protected virtual void ConnectTiles(UnityTile unityTile, RasterTile dataTile, bool addToWaitingList = true)
		{
			unityTile.AddTile(dataTile);
			dataTile.AddUser(unityTile.CanonicalTileId);

			if(addToWaitingList)
			{
				if(!_tileWaitingList.ContainsKey(dataTile.Key))
				{
					_tileWaitingList.Add(dataTile.Key, new HashSet<UnityTile>());
				}
				_tileWaitingList[dataTile.Key].Add(unityTile);
			}
			else
			{
				if (!_tileUserTracker.ContainsKey(dataTile.Key))
				{
					_tileUserTracker.Add(dataTile.Key, new HashSet<UnityTile>());
				}

				_tileUserTracker[dataTile.Key].Add(unityTile);
			}

			_tileTracker.Add(unityTile, dataTile);
		}

		public virtual void RegisterTile(UnityTile tile, bool loadParent = false)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				return;
			}

			if (loadParent)
			{
				ApplyParentTexture(tile);
			}

			var memoryCacheItem = _fetcher.FetchDataInstant(tile.CanonicalTileId, _sourceSettings.Id);
			if (memoryCacheItem != null)
			{
				var dataTile = (RasterTile) memoryCacheItem.Tile;
				ConnectTiles(tile, dataTile, false);
				SetTexture(tile, dataTile);
				TextureReceived(dataTile);
			}
			else
			{
				if (!_requestedTiles.ContainsKey(tile.CanonicalTileId))
				{
					var dataTile = CreateTile(tile.CanonicalTileId, _sourceSettings.Id);
					ConnectTiles(tile, dataTile);
					_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId);
					_requestedTiles.Add(tile.CanonicalTileId, dataTile);
				}
				else
				{
					Debug.Log("can this happen? shouldn't with imagery");
				}
			}
		}

		public virtual void UnregisterTile(UnityTile tile, bool clearData = true)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				var noTileIsWaitingForIt = true;
				var requestedTile = _tileTracker[tile];
				requestedTile.AddLog("cancelling ", tile.CanonicalTileId);

				//remove this unityTile from list of unityTiles waiting for
				//that particular data tile
				if (_tileWaitingList.ContainsKey(requestedTile.Key))
				{
					if (_tileWaitingList[requestedTile.Key].Contains(tile))
					{
						_tileWaitingList[requestedTile.Key].Remove(tile);

						if (_tileWaitingList[requestedTile.Key].Count == 0)
						{
							_tileWaitingList.Remove(requestedTile.Key);
						}
						else
						{
							noTileIsWaitingForIt = false;
						}
					}
				}

				if (_tileUserTracker.ContainsKey(requestedTile.Key))
				{
					_tileUserTracker[requestedTile.Key].Remove(tile);
					if (_tileUserTracker[requestedTile.Key].Count == 0)
					{
						_tileUserTracker.Remove(requestedTile.Key);
					}
					else
					{
						noTileIsWaitingForIt = false;
					}
				}

				if(noTileIsWaitingForIt)
				{
					requestedTile.AddLog("disposing 2 ", tile.CanonicalTileId);
					_fetcher.CancelFetching(requestedTile, _sourceSettings.Id);
					MapboxAccess.Instance.CacheManager.TileDisposed(requestedTile, _sourceSettings.Id);
				}

				tile.RemoveTile(_tileTracker[tile]);
				_tileTracker[tile].RemoveUser(tile.CanonicalTileId);
				_tileTracker.Remove(tile);

				//can't use tile.CanonicalTileId here because tile might be using
				//lower level (parent) data, like terrain/elevation does.
				//so tile.CanonicalTileId != requestedTile.Id
				if (_requestedTiles.ContainsKey(requestedTile.Id))
				{
					_requestedTiles.Remove(requestedTile.Id);
				}
			}
		}

		public virtual void Stop(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				var noTileIsWaitingForIt = true;
				var requestedTile = _tileTracker[tile];

				if (_tileWaitingList.ContainsKey(requestedTile.Key))
				{
					_tileWaitingList[requestedTile.Key].Remove(tile);
					if (_tileWaitingList[requestedTile.Key].Count == 0)
					{
						_tileWaitingList.Remove(requestedTile.Key);
					}
					else
					{
						noTileIsWaitingForIt = false;
					}
				}

				if (_tileUserTracker.ContainsKey(requestedTile.Key))
				{
					_tileUserTracker[requestedTile.Key].Remove(tile);
					if (_tileUserTracker[requestedTile.Key].Count == 0)
					{
						_tileUserTracker.Remove(requestedTile.Key);
					}
					else
					{
						noTileIsWaitingForIt = false;
					}
				}

				if (noTileIsWaitingForIt)
				{
					_requestedTiles.Remove(tile.CanonicalTileId);
					requestedTile.AddLog("cancelling for stop call");
					_fetcher.CancelFetching(requestedTile, _sourceSettings.Id);
				}
			}
		}

		public virtual void ClearTile(UnityTile tile)
		{
			SetTexture(tile, null);
		}

		protected virtual void OnTextureReceived(RasterTile dataTile)
		{
			if (_tileWaitingList.ContainsKey(dataTile.Key))
			{
				if (!_tileUserTracker.ContainsKey(dataTile.Key))
				{
					_tileUserTracker.Add(dataTile.Key, new HashSet<UnityTile>());
				}

				foreach (var utile in _tileWaitingList[dataTile.Key])
				{
					if (utile.ContainsDataTile(dataTile))
					{
						SetTexture(utile, dataTile);
						_tileUserTracker[dataTile.Key].Add(utile);
					}
				}
				_tileWaitingList.Remove(dataTile.Key);
				TextureReceived(dataTile);
			}
			else
			{
				//this means tile is unregistered during fetching... but somehow it didn't get cancelled?
				dataTile.AddLog("tile is unregistered during fetching?");
			}

			if (_requestedTiles.ContainsKey(dataTile.Id))
			{
				_requestedTiles.Remove(dataTile.Id);
			}
		}

		protected virtual void OnFetcherError(RasterTile dataTile, TileErrorEventArgs errorEventArgs)
		{
			dataTile.AddLog("OnFetcherError ImageFactoryManager");

			//cancelled tiles are removed from this list as soon as cancellation order is received
			//so this is only for failed web calls
			if (_tileWaitingList.ContainsKey(dataTile.Key))
			{
				foreach (var unityTile in _tileWaitingList[dataTile.Key])
				{
					unityTile.RemoveTile(dataTile);
					dataTile.RemoveUser(unityTile.CanonicalTileId);

					if (_tileTracker.ContainsKey(unityTile))
					{
						_tileTracker.Remove(unityTile);
					}
				}
				_tileWaitingList.Remove(dataTile.Key);
			}

			if (_tileUserTracker.ContainsKey(dataTile.Key))
			{
				Debug.Log("fetching failed but there was already tiles using it?");
				//this shouldn't happen, adding this check just to see everything is in order
				_tileUserTracker.Remove(dataTile.Key);
			}

			//cancelled tiles are removed from this list as soon as cancellation order is received
			//so this is only for failed web calls
			if (_requestedTiles.ContainsKey(dataTile.Id))
			{
				_requestedTiles.Remove(dataTile.Id);
			}

			FetchingError(dataTile, errorEventArgs);
		}

		protected virtual void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId;
			//tile.SetParentTexture(parent, null);
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical, true);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentTexture(parent, (RasterTile) cacheItem.Tile);
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