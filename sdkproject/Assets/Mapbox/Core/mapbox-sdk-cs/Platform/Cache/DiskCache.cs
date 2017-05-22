using Mapbox.Platform;
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


	public class DiskCache : ICache
	{


		private struct CacheItem
		{
			public long Timestamp;
			public byte[] Data;
		}


		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public DiskCache(int maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
			_cachedResponses = new Dictionary<string, CacheItem>();
		}


		private int _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, CacheItem> _cachedResponses;


		public void Add(string key, byte[] data)
		{
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


		public byte[] Get(string key)
		{
			lock (_lock)
			{
				if (!_cachedResponses.ContainsKey(key))
				{
					UnityEngine.Debug.Log("DiskCache: not found " + key);
					return null;
				}
				UnityEngine.Debug.Log("DiskCache: returning " + key);
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