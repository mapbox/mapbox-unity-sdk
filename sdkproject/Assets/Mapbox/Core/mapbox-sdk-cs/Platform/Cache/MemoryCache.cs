using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;


namespace Mapbox.Platform.Cache
{
	public interface IMemoryCache
	{
		void Add(CanonicalTileId tileId, string tilesetId, CacheItem cacheItem, bool forceInsert);
		CacheItem Get(CanonicalTileId tileId, string tilesetId, bool resetDestructionIndex = false);
		void Clear();
		bool Exists(CanonicalTileId tileId, string tilesetId);
		void MarkFallback(CanonicalTileId tileId, string tilesetId);
		void UpdateExpiration(string tilesetId, CanonicalTileId tileId, DateTime expirationDate);
		void TileDisposed(UnityTile tile, string tilesetId);
	}

	public class MemoryCache : IMemoryCache
	{
		protected uint _maxCacheSize;
		protected Dictionary<int, CacheItem> _cachedItems;
		protected Dictionary<int, CacheItem> _fallbackItems;
		protected int _destroyedItemCounter = 0;
		protected int _destroyedItemLimit = 20;

		//this is bad, this should be a linked list or something
		//private List<int> _itemsToDestroy;
		protected HashSet<int> _destructionHashset;
		protected Queue<int> _destructionQueue;
		protected bool _cacheSizeWarningShown = false;

		public MemoryCache(uint maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
			_cachedItems = new Dictionary<int, CacheItem>();
			_fallbackItems = new Dictionary<int, CacheItem>();
			_destructionQueue = new Queue<int>();
			_destructionHashset = new HashSet<int>();
		}

		public uint MaxCacheSize => _maxCacheSize;

		public virtual void Add(CanonicalTileId tileId, string tilesetId, CacheItem cacheItem, bool forceInsert)
		{
			if (cacheItem.Tile == null)
			{
				Debug.Log("what");
			}
			var key = tileId.GenerateKey(tilesetId);

			//this tile was recycled so the data was marked for pruning
			//but then user loaded same tile again so we are removing that flag from _texOrder list

			if (_destructionHashset.Contains(key))
			{
				_destructionHashset.Remove(key);
			}

			//data is already in fallback items list
			//no need to keep a clone in temp cache as well
			//get method will check both
			if (_fallbackItems.ContainsKey(key))
			{
				return;
			}

			if (!_cachedItems.ContainsKey(key))
			{
				//item doesn't exists, we simply add it to list
				cacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				//cacheItem.Tile.Released += TileRecycled;
				_cachedItems.Add(key, cacheItem);
			}
			else
			{
				//WRONG an item with same key exists, we just update the added time
				//WRONG do we need to check if tile/data inside is same?
				//remember when a tile is unloaded and reloaded, it'll have same cache item key
				//BUT the tiles inside it will be different. New cache item will have new tiles in it
				//and if we don't handle them properly, they'll leak.
				//_cachedItems[key].AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;

				_cachedItems[key].AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
			}

			CheckCacheLimit();
			CheckForUnloadingAssets();
		}

		public void UpdateExpiration(string tilesetId, CanonicalTileId tileId, DateTime expirationDate)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (_cachedItems.ContainsKey(key))
			{
				_cachedItems[key].ExpirationDate = expirationDate;
			}
			else if (_fallbackItems.ContainsKey(key))
			{
				_fallbackItems[key].ExpirationDate = expirationDate;
			}
		}

		public virtual CacheItem Get(CanonicalTileId tileId, string tilesetId, bool resetDestructionIndex = false)
		{
			var key = tileId.GenerateKey(tilesetId);

			if (_fallbackItems != null && _fallbackItems.ContainsKey(key))
			{
				return _fallbackItems[key];
			}

			if (_cachedItems.ContainsKey(key))
			{

				//this is reseting destruction queue index to prevent
				//system delete in-use parent tile images which causes black tiles.
				//this is a slow and temp solution, should be replaced by a better solution.
				if (resetDestructionIndex)
				{
					var size = _destructionQueue.Count;
					for (int i = 0; i < size; i++)
					{
						var item = _destructionQueue.Dequeue();
						if (item != key && _destructionHashset.Contains(item))
						{
							_destructionQueue.Enqueue(item);
						}
					}

					_destructionQueue.Enqueue(key);
				}

				//this would have made sense but temp parent texture feature breaks it
				//_destructionHashset.Remove(key);
				return _cachedItems[key];
			}

			return null;

		}

		public virtual void TileDisposed(UnityTile tile, string tilesetId)
		{
			var key = tile.CanonicalTileId.GenerateKey(tilesetId);
			if (!_fallbackItems.ContainsKey(key))
			{
				if (_cachedItems.ContainsKey(key) && !_destructionHashset.Contains(key))
				{
					_destructionHashset.Add(key);
					_destructionQueue.Enqueue(key);
				}
			}
		}

		public virtual void Clear()
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
				_destructionHashset.Clear();
				_destructionQueue.Clear();
			}
			else
			{
				_cachedItems = new Dictionary<int, CacheItem>();
				_destructionHashset.Clear();
				_destructionQueue.Clear();
			}
		}

		public virtual bool Exists(CanonicalTileId tileId, string tilesetId)
		{
			var key = tileId.GenerateKey(tilesetId);
			return _cachedItems.ContainsKey(key);
		}

		public virtual void MarkFallback(CanonicalTileId tileId, string tilesetId)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (_cachedItems.ContainsKey(key))
			{
				if(!_fallbackItems.ContainsKey(key))
				{
				var cacheItem = _cachedItems[key];
				_cachedItems.Remove(key);
				_destructionHashset.Remove(key);
				_fallbackItems.Add(key, cacheItem);
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

		private void CheckCacheLimit()
		{
			if (_cachedItems.Count >= _maxCacheSize)
			{
				if (_destructionHashset.Count == 0 && !_cacheSizeWarningShown)
				{
					_cacheSizeWarningShown = true;
					Debug.Log(string.Format("Memory cache is full ({0} texture at the moment). Either your cache setting is too low ({1}) for your camera view and settings " +
					                        "or textures aren't disposed properly as memory cache hasn't received unregister signal. " +
					                        "Not taking any actions but latter might crash the app due to memory usage soon." +
					                        "This message won't repeat to prevent spam but issue will consist.", _cachedItems.Count, _maxCacheSize));
					// var keys = _cachedItems.Keys.ToArray();
					// foreach (var keyToRemove in keys)
					// {
					// 	RemoveItemCacheItem(keyToRemove);
					// 	_destroyedItemCounter++;
					// }
				}
				else
				{
					var removed = 5;
					while (_destructionQueue.Count > 0 && removed > 0)
					{
						var keyToRemove = _destructionQueue.Dequeue();
						if (_destructionHashset.Contains(keyToRemove))
						{
							_destructionHashset.Remove(keyToRemove);
							RemoveItemCacheItem(keyToRemove);
							_destroyedItemCounter++;
							removed--;
						}
					}

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

		protected virtual void RemoveItemCacheItem(int keyToRemove)
		{
			var item = _cachedItems[keyToRemove];
			_cachedItems.Remove(keyToRemove);
			if (item is TextureCacheItem)
			{
				(item as TextureCacheItem).Texture2D.Destroy();
			}

			item.Data = null;
		}

#if UNITY_EDITOR

#endif
	}

	public class EditorMemoryCache : MemoryCache
	{
		public Action<CanonicalTileId, string, CacheItem, bool> TileAdded = (s, id, arg3, arg4) => {};
		public Action<CanonicalTileId, string> TileReleased = (s, id) => {};
		public Action<CanonicalTileId, string> TileRead = (s, id) => {};
		public Action<CanonicalTileId, string> TileSetFallback = (s, id) => {};
		public Action<CanonicalTileId, string> TilePruned = (s, id) => {};

		public Dictionary<int, CacheItem> GetCachedItems => _cachedItems;
		public Dictionary<int, CacheItem> GetFallbackItems => _fallbackItems;
		public Queue<int> GetDestructionQueue => _destructionQueue;

		public EditorMemoryCache(uint maxCacheSize) : base(maxCacheSize)
		{
		}

		public override void Add(CanonicalTileId tileId, string tilesetId, CacheItem cacheItem, bool forceInsert)
		{
			base.Add(tileId, tilesetId, cacheItem, forceInsert);
			TileAdded(tileId, tilesetId, cacheItem, forceInsert);
		}

		public override void TileDisposed(UnityTile tile, string tilesetId)
		{
			base.TileDisposed(tile, tilesetId);
			TileReleased(tile.CanonicalTileId, tilesetId);
		}

		public override CacheItem Get(CanonicalTileId tileId, string tilesetId, bool b)
		{
			TileRead(tileId, tilesetId);
			return base.Get(tileId, tilesetId);
			TileRead(tileId, tilesetId);
		}

		public override void Clear()
		{
			base.Clear();
		}

		public override bool Exists(CanonicalTileId tileId, string tilesetId)
		{
			return base.Exists(tileId, tilesetId);
		}

		protected override void RemoveItemCacheItem(int keyToRemove)
		{
			if (_cachedItems.ContainsKey(keyToRemove))
			{
				var item = _cachedItems[keyToRemove];
				base.RemoveItemCacheItem(keyToRemove);
				TilePruned(item.TileId, item.TilesetId);
			}
			else
			{
				Debug.Log("why?");
			}
		}

		public override void MarkFallback(CanonicalTileId tileId, string tilesetId)
		{
			base.MarkFallback(tileId, tilesetId);
			TileSetFallback(tileId, tilesetId);
		}
	}
}