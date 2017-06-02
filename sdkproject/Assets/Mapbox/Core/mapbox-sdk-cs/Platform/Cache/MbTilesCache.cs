using Mapbox.Platform;
using Mapbox.Platform.MbTiles;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Mapbox.Platform.Cache
{



	/// ///////////////
	//////////////
	/*

		SAME IMPLEMENTATION AS MEMORYCACHE ---- JUST FOR TESTING 

		*////////////////


	public class MbTilesCache : ICache
	{


		private struct CacheItem
		{
			public long Timestamp;
			public byte[] Data;
		}


		private struct CacheKey
		{
			public string tileset;
			public int zoom;
			public long x;
			public long y;
			public override string ToString()
			{
				return string.Format("tileset:{0} z:{1} x:{2} y:{3} - {4}", tileset, zoom, x, y, DateTime.Now.Ticks);
			}
		}


		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public MbTilesCache(int maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
			_cachedResponses = new Dictionary<string, CacheItem>();
			_mbTiles = new Dictionary<string, MbTiles.MbTiles>();
		}


		private int _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, CacheItem> _cachedResponses;
		private Dictionary<string, MbTiles.MbTiles> _mbTiles;


		public void Add(string key, byte[] data)
		{
			CacheKey cacheKey = extractCacheKey(key);
			UnityEngine.Debug.Log("diskcache, key:" + key);
			UnityEngine.Debug.Log("diskcache, cacheKey: " + cacheKey);

			if (!_mbTiles.ContainsKey(cacheKey.tileset))
			{
				MbTiles.MbTiles mbt = new MbTiles.MbTiles(cacheKey.tileset);
				MetaDataRequired md = new MetaDataRequired()
				{
					TilesetName = cacheKey.tileset,
					Description = "TODO: " + cacheKey.tileset,
					Format = "TODO: " + cacheKey.tileset,
					Type = "TODO: " + cacheKey.tileset,
					Version = 1
				};

				_mbTiles[cacheKey.tileset] = mbt;

			}

			lock (_lock)
			{
				if (_cachedResponses.Count >= _maxCacheSize)
				{
					UnityEngine.Debug.Log("DiskCache: pruning " + _cachedResponses.OrderBy(c => c.Value.Timestamp).First().Key);
					_cachedResponses.Remove(_cachedResponses.OrderBy(c => c.Value.Timestamp).First().Key);
				}

				if (!_cachedResponses.ContainsKey(key))
				{
					UnityEngine.Debug.Log("DiskCache: adding " + key);
					_cachedResponses.Add(key, new CacheItem() { Timestamp = DateTime.Now.Ticks, Data = data });
				}
			}
		}


		/// <summary>
		/// TEMPORARY HACK TO GET KEY DATA BACK OUT OF URL
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private CacheKey extractCacheKey(string url)
		{
			CacheKey ck = new CacheKey();
			if (url.Contains("mapbox.terrain-rgb"))
			{
				ck.tileset = "mapbox.terrain-rgb";
			}
			else if (url.Contains("mapbox.satellite"))
			{
				ck.tileset = "mapbox.satellite";
			}

			return ck;
		}


		public byte[] Get(string key)
		{
			lock (_lock)
			{
				if (!_cachedResponses.ContainsKey(key))
				{
					//UnityEngine.Debug.Log("DiskCache: not found " + key);
					return null;
				}
				//UnityEngine.Debug.Log("DiskCache: returning " + key);
				return _cachedResponses[key].Data;
			}
		}


		public void Clear()
		{
			lock (_lock)
			{
				_cachedResponses.Clear();
			}
		}


	}
}