using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
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

		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, TextureCacheItem> _cachedTextures;
		private Dictionary<string, TextureCacheItem> _fixedTextures;
		private int _destroyedTextureCounter = 0;
		private int _destroyedTextureLimit = 20;

		private List<string> _texOrder;

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void Add(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			string key = tileId.GenerateKey(tilesetId);

			if (!_cachedTextures.ContainsKey(key))
			{
				//item doesn't exists, we simply add it to list
				textureCacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedTextures.Add(key, textureCacheItem);
				_texOrder.Add(key);
			}
			else
			{
				//an item with samea key exists, we destroy older one to prevent memory leak first
				//then add new one to list
				if(Debug.isDebugBuild) Debug.Log("An texture item with same key exists in memory cache. Destroying older one, caching new one.");

				RemoveTextureCacheItem(key);
				textureCacheItem.AddedToCacheTicksUtc = DateTime.UtcNow.Ticks;
				_cachedTextures[key] = textureCacheItem;
			}

			CheckCacheLimit();
			CheckForUnloadingAssets();
		}

		private void CheckCacheLimit()
		{
			if (_cachedTextures.Count >= _maxCacheSize)
			{
				var keyToRemove = _texOrder[0];
				_texOrder.RemoveAt(0);
				RemoveTextureCacheItem(keyToRemove);
				_destroyedTextureCounter++;
				_cachedTextures.Remove(keyToRemove);
			}
		}

		private void CheckForUnloadingAssets()
		{
			if (_destroyedTextureCounter >= _destroyedTextureLimit)
			{
				_destroyedTextureCounter = 0;
				Resources.UnloadUnusedAssets();
			}
		}

		private void RemoveTextureCacheItem(string keyToRemove)
		{
			_cachedTextures[keyToRemove].Texture2D.Destroy();
			_cachedTextures[keyToRemove].Data = null;
		}

		public CacheItem Get(string tilesetId, CanonicalTileId tileId)
		{
			string key = tileId.GenerateKey(tilesetId);

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

		// private static string GenerateKey(string tilesetId, CanonicalTileId tileId)
		// {
		// 	return string.Format("{0}_{1}", tilesetId, tileId);
		// }

		public void Clear()
		{
			lock (_lock)
			{
				if (_cachedTextures != null)
				{
					foreach (var item in _cachedTextures)
					{
						if (item.Value.Texture2D != null)
						{
							item.Value.Texture2D.Destroy();
						}
					}

					_cachedTextures.Clear();
					_texOrder.Clear();
				}
				else
				{
					_cachedTextures = new Dictionary<string, TextureCacheItem>();
					_texOrder = new List<string>();
				}
			}
		}

		public void Clear(string tilesetId)
		{
			lock (_lock)
			{
				List<string> toDelete = _cachedTextures.Keys.Where(k => k.Contains(tilesetId)).ToList();
				foreach (string key in toDelete)
				{
					_cachedTextures.Remove(key);
				}
			}
		}

		public bool Exists(string tilesetId, CanonicalTileId tileId)
		{
			string key = tileId.GenerateKey(tilesetId);
			return _cachedTextures.ContainsKey(key);
		}

		public void MarkFixed(CanonicalTileId tileId, string tilesetId)
		{
			var key = tileId.GenerateKey(tilesetId);
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