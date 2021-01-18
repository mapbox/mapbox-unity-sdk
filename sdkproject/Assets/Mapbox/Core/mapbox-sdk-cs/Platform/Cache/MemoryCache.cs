using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Mapbox.Platform.Cache
{
	public interface IMemoryCache
	{
		void Add(string tilesetId, CanonicalTileId tileId, CacheItem cacheItem, bool forceInsert);
		void AddToDisposeList(string tilesetId, CanonicalTileId tileId);
		CacheItem Get(string tilesetId, CanonicalTileId tileId);
		void Clear();
		bool Exists(string tilesetId, CanonicalTileId tileId);
		void MarkFixed(CanonicalTileId tileId, string tilesetId);
	}

	public class MemoryCache : IMemoryCache
	{
		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public MemoryCache(uint maxCacheSize)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_maxCacheSize = maxCacheSize;
			_cachedItems = new Dictionary<int, CacheItem>();
			_fixedItems = new Dictionary<int, CacheItem>();
			_itemsToDestroy = new List<int>();
		}

		private uint _maxCacheSize;
		private Dictionary<int, CacheItem> _cachedItems;
		private Dictionary<int, CacheItem> _fixedItems;
		private int _destroyedItemCounter = 0;
		private int _destroyedItemLimit = 20;

		private List<int> _itemsToDestroy;

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void Add(string tilesetId, CanonicalTileId tileId, CacheItem cacheItem, bool forceInsert)
		{
			var key = tileId.GenerateKey(tilesetId);

			//this tile was recycled so the data was marked for pruning
			//but then user loaded same tile again so we are removing that flag from _texOrder list
			if (_itemsToDestroy.Contains(key))
			{
				_itemsToDestroy.Remove(key);
			}

			if (!_cachedItems.ContainsKey(key))
			{
				//item doesn't exists, we simply add it to list
				cacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedItems.Add(key, cacheItem);
				//_texOrder.Add(key);
			}
			else
			{
				//an item with same key exists, we destroy older one to prevent memory leak first
				//then add new one to list
				if(Debug.isDebugBuild) Debug.Log("An item with same key exists in memory cache. Destroying older one, caching new one.");

				RemoveItemCacheItem(key);
				cacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedItems[key] = cacheItem;
			}

			CheckCacheLimit();
			CheckForUnloadingAssets();
		}

		public void AddToDisposeList(string tilesetId, CanonicalTileId tileId)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (!_itemsToDestroy.Contains(key) && _cachedItems.ContainsKey(key))
			{
				_itemsToDestroy.Add(key);
			}
		}

		private void CheckCacheLimit()
		{
			if (_cachedItems.Count >= _maxCacheSize)
			{
				if (_itemsToDestroy.Count == 0)
				{
					//something is horribly wrong
					Debug.Log("Memory cache is in a very wrong state, destroying all cached items.");
					var keys = _cachedItems.Keys.ToArray();
					foreach (var keyToRemove in keys)
					{
						RemoveItemCacheItem(keyToRemove);
						_destroyedItemCounter++;
						_cachedItems.Remove(keyToRemove);
					}
				}
				else
				{
					var keyToRemove = _itemsToDestroy[0];
					_itemsToDestroy.RemoveAt(0);
					RemoveItemCacheItem(keyToRemove);
					_destroyedItemCounter++;
					_cachedItems.Remove(keyToRemove);
				}
			}
		}

		private void CheckForUnloadingAssets()
		{
			if (_destroyedItemCounter >= _destroyedItemLimit)
			{
				_destroyedItemCounter = 0;
				Resources.UnloadUnusedAssets();
			}
		}

		private void RemoveItemCacheItem(int keyToRemove)
		{
			var item = _cachedItems[keyToRemove];
			if (item is TextureCacheItem)
			{
				(item as TextureCacheItem).Texture2D.Destroy();
			}

			item.Data = null;
		}

		public CacheItem Get(string tilesetId, CanonicalTileId tileId)
		{
			var key = tileId.GenerateKey(tilesetId);

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			if (!_cachedItems.ContainsKey(key))
			{
				if (_fixedItems != null && _fixedItems.ContainsKey(key))
				{
					return _fixedItems[key];
				}

				return null;
			}

			_itemsToDestroy.Remove(key);
			_itemsToDestroy.Add(key);
			return _cachedItems[key];
		}

		public void Clear()
		{
			if (_cachedItems != null)
			{
				foreach (var item in _cachedItems)
				{
					if (item.Value is TextureCacheItem)
					{
						(item.Value as TextureCacheItem).Texture2D?.Destroy();
					}

				}

				_cachedItems.Clear();
				_itemsToDestroy.Clear();
			}
			else
			{
				_cachedItems = new Dictionary<int, CacheItem>();
				_itemsToDestroy = new List<int>();
			}
		}

		public bool Exists(string tilesetId, CanonicalTileId tileId)
		{
			var key = tileId.GenerateKey(tilesetId);
			return _cachedItems.ContainsKey(key);
		}

		public void MarkFixed(CanonicalTileId tileId, string tilesetId)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (_cachedItems.ContainsKey(key))
			{
				if(!_fixedItems.ContainsKey(key))
				{
				var cacheItem = _cachedItems[key];
				_cachedItems.Remove(key);
				_itemsToDestroy.Remove(key);
				_fixedItems.Add(key, cacheItem);
				}
				else
				{
					Debug.Log("Item is already marked as base image");
				}
			}
			else
			{
				Debug.Log("Item isn't in memory cache, this shouldn't happen really");
			}
		}


#if UNITY_EDITOR
		public Dictionary<int, CacheItem> GetCachedItems => _cachedItems;
		public Dictionary<int, CacheItem> GetFixedItems => _fixedItems;
#endif
	}
}