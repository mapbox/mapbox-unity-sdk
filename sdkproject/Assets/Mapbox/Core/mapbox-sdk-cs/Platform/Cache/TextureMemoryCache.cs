using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Mapbox.Platform.Cache
{
	public class TextureMemoryCache : ITextureCache
	{
		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public TextureMemoryCache(uint maxCacheSize)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_maxCacheSize = maxCacheSize;
			_cachedTextures = new Dictionary<string, TextureCacheItem>();
			_textureOrderQueue = new Queue<string>();
		}


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, TextureCacheItem> _cachedTextures;
		private Queue<string> _textureOrderQueue;


		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void ReInit()
		{
			_cachedTextures = new Dictionary<string, TextureCacheItem>();
		}

		public void Add(string mapdId, CanonicalTileId tilesetId, CacheItem item, bool forceInsert)
		{

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
					var keyToRemove = _textureOrderQueue.Dequeue();
					_cachedTextures[keyToRemove].Texture2D.Destroy();
					_cachedTextures.Remove(keyToRemove);
				}

				// TODO: forceInsert
				if (!_cachedTextures.ContainsKey(key))
				{
					textureCacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
					_cachedTextures.Add(key, textureCacheItem);
					_textureOrderQueue.Enqueue(key);
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
			string key = tilesetId + "||" + tileId;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			lock (_lock)
			{
				if (!_cachedTextures.ContainsKey(key))
				{
					return null;
				}

				return _cachedTextures[key];
			}
		}

		public Texture2D GetTexture(string tilesetId, CanonicalTileId tileId)
		{
			string key = tilesetId + "||" + tileId;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			lock (_lock)
			{
				if (!_cachedTextures.ContainsKey(key))
				{
					return null;
				}

				return _cachedTextures[key].Texture2D;
			}
		}

		public void GetAsync(string tilesetId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
		{
			string key = tilesetId + "||" + tileId;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			lock (_lock)
			{
				if (!_cachedTextures.ContainsKey(key))
				{
					callback(null);
				}

				callback(_cachedTextures[key]);
			}
		}

		public void Clear()
		{
			lock (_lock)
			{
				_cachedTextures.Clear();
				_textureOrderQueue.Clear();
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
	}
}
