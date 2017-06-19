using Mapbox.Map;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Mapbox.Platform.Cache
{


	public class MemoryCache : ICache
	{


		private struct CacheItem
		{
			public long Timestamp;
			public byte[] Data;
		}


		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public MemoryCache(uint maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
			_cachedResponses = new Dictionary<string, CacheItem>();
		}


		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, CacheItem> _cachedResponses;


		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}


		public void Add(string mapdId, CanonicalTileId tileId, byte[] data)
		{
			string key = mapdId + "||" + tileId;

			lock (_lock)
			{
				if (_cachedResponses.Count >= _maxCacheSize)
				{
					_cachedResponses.Remove(_cachedResponses.OrderBy(c => c.Value.Timestamp).First().Key);
				}

				if (!_cachedResponses.ContainsKey(key))
				{
					_cachedResponses.Add(key, new CacheItem() { Timestamp = DateTime.Now.Ticks, Data = data });
				}
			}
		}


		public byte[] Get(string mapId, CanonicalTileId tileId)
		{
			string key = mapId + "||" + tileId;

			lock (_lock)
			{
				if (!_cachedResponses.ContainsKey(key))
				{
					return null;
				}

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


		public void Clear(string mapId)
		{
			lock (_lock)
			{
				mapId += "||";
				List<string> toDelete = _cachedResponses.Keys.Where(k => k.Contains(mapId)).ToList();
				foreach (string key in toDelete)
				{
					_cachedResponses.Remove(key);
				}
			}
		}


	}
}