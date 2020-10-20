using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Mapbox.Platform.Cache
{
	public class TextureMemoryCache
	{
		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public TextureMemoryCache(uint maxCacheSize)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_maxCacheSize = maxCacheSize;
			_cachedTextures = new Dictionary<string, TextureCacheItem>();
			_fixedTextures = new Dictionary<string, TextureCacheItem>();
			//_textureOrderQueue = new Queue<string>();
			_texOrder = new List<string>();
		}


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, TextureCacheItem> _cachedTextures;

		private Dictionary<string, TextureCacheItem> _fixedTextures;

		//private Queue<string> _textureOrderQueue;
		private List<string> _texOrder;

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void ReInit()
		{
			_cachedTextures = new Dictionary<string, TextureCacheItem>();
		}

		public void Add(string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			string key = mapId + "||" + tileId;

			lock (_lock)
			{
				if (_cachedTextures.Count >= _maxCacheSize)
				{
					// var toRemove = _cachedTextures.OrderBy(c => c.Value.AddedToCacheTicksUtc).First();
					// toRemove.Value.Texture2D.Destroy();
					var keyToRemove = _texOrder[0];
					_texOrder.RemoveAt(0);
					_cachedTextures[keyToRemove].Texture2D.Destroy();
					_cachedTextures.Remove(keyToRemove);
				}

				// TODO: forceInsert
				if (!_cachedTextures.ContainsKey(key))
				{
					textureCacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
					_cachedTextures.Add(key, textureCacheItem);
					//_textureOrderQueue.Enqueue(key);
					_texOrder.Add(key);
				}
				else
				{
					textureCacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
					_cachedTextures[key] = textureCacheItem;
				}
			}
		}

		public CacheItem Get(string tilesetId, CanonicalTileId tileId)
		{
			string key = GenerateKey(tilesetId, tileId);

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			if (!_cachedTextures.ContainsKey(key))
			{
				if (_fixedTextures != null && _fixedTextures.ContainsKey(key))
				{
					return _fixedTextures[key];
				}

				return null;
			}

			_texOrder.Remove(key);
			_texOrder.Add(key);
			return _cachedTextures[key];
		}

		private static string GenerateKey(string tilesetId, CanonicalTileId tileId)
		{
			return tilesetId + "||" + tileId;
		}

		public void Clear()
		{
			lock (_lock)
			{
				_cachedTextures.Clear();
				//_textureOrderQueue.Clear();
				_texOrder.Clear();
			}
		}

		public void Clear(string tilesetId)
		{
			lock (_lock)
			{
				tilesetId += "||";
				List<string> toDelete = _cachedTextures.Keys.Where(k => k.Contains(tilesetId)).ToList();
				foreach (string key in toDelete)
				{
					_cachedTextures.Remove(key);
				}
			}
		}

		public bool Exists(string tilesetId, CanonicalTileId tileId)
		{
			string key = tilesetId + "||" + tileId;
			return _cachedTextures.ContainsKey(key);
		}

		public void MarkFixed(CanonicalTileId tileId, string tilesetId)
		{
			var key = GenerateKey(tilesetId, tileId);
			if (_cachedTextures.ContainsKey(key))
			{
				var cacheItem = _cachedTextures[key];
				_cachedTextures.Remove(key);
				_texOrder.Remove(key);
				_fixedTextures.Add(key, cacheItem);
			}
			else
			{
				Debug.Log("Texture isn't in memory cache, this shouldn't happen really");
			}
		}
	}
}