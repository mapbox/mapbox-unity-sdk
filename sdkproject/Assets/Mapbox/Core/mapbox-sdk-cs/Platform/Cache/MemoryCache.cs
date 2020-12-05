using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using UnityEngine;


namespace Mapbox.Platform.Cache
{
	public class MemoryCache
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
			_texOrder = new List<int>();
		}

		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<int, CacheItem> _cachedItems;
		private Dictionary<int, CacheItem> _fixedItems;
		private int _destroyedItemCounter = 0;
		private int _destroyedItemLimit = 20;

		private List<int> _texOrder;

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void Add(string tilesetId, CanonicalTileId tileId, CacheItem cacheItem, bool forceInsert)
		{
			var key = tileId.GenerateKey(tilesetId);

			if (!_cachedItems.ContainsKey(key))
			{
				//item doesn't exists, we simply add it to list
				cacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedItems.Add(key, cacheItem);
				_texOrder.Add(key);
			}
			else
			{
				//an item with samea key exists, we destroy older one to prevent memory leak first
				//then add new one to list
				if(Debug.isDebugBuild) Debug.Log("An item with same key exists in memory cache. Destroying older one, caching new one.");

				RemoveItemCacheItem(key);
				cacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedItems[key] = cacheItem;
			}

			CheckCacheLimit();
			CheckForUnloadingAssets();
		}

		private void CheckCacheLimit()
		{
			if (_cachedItems.Count >= _maxCacheSize)
			{
				var keyToRemove = _texOrder[0];
				_texOrder.RemoveAt(0);
				RemoveItemCacheItem(keyToRemove);
				_destroyedItemCounter++;
				_cachedItems.Remove(keyToRemove);
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

			_texOrder.Remove(key);
			_texOrder.Add(key);
			return _cachedItems[key];
		}

		// private static string GenerateKey(string tilesetId, CanonicalTileId tileId)
		// {
		// 	return string.Format("{0}_{1}", tilesetId, tileId);
		// }

		public void Clear()
		{
			lock (_lock)
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
					_texOrder.Clear();
				}
				else
				{
					_cachedItems = new Dictionary<int, CacheItem>();
					_texOrder = new List<int>();
				}
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
				var cacheItem = _cachedItems[key];
				_cachedItems.Remove(key);
				_texOrder.Remove(key);
				_fixedItems.Add(key, cacheItem);
			}
			else
			{
				Debug.Log("Item isn't in memory cache, this shouldn't happen really");
			}
		}
	}
}