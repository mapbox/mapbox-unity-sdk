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


	public class MbTilesCache : ICache, IDisposable
	{


		private struct CacheItem
		{
			public long Timestamp;
			public byte[] Data;
		}



		// TODO: add support for disposal strategy (timestamp, distance, etc.)
		public MbTilesCache(int maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
			_mbTiles = new Dictionary<string, MbTiles.MbTiles>();
		}


		#region


		~MbTilesCache()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					foreach (var mbtCache in _mbTiles)
					{
						MbTiles.MbTiles mbt = mbtCache.Value;
						mbt.Dispose();
						mbt = null;
					}
					_mbTiles.Clear();
				}
				_disposed = true;
			}
		}

		#endregion



		private bool _disposed;
		private int _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, MbTiles.MbTiles> _mbTiles;


		public void Add(string key, byte[] data)
		{
			MbTiles.MbTiles.CacheKey cacheKey = extractCacheKey(key);
			//UnityEngine.Debug.Log("diskcache, key:" + key);
			//UnityEngine.Debug.Log("diskcache, cacheKey: " + cacheKey);

			if (!_mbTiles.ContainsKey(cacheKey.tileset))
			{
				MbTiles.MbTiles mbt = new MbTiles.MbTiles(cacheKey.tileset + ".cache");
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

			MbTiles.MbTiles currentMbTiles = _mbTiles[cacheKey.tileset];

			if (!currentMbTiles.TileExists(cacheKey))
			{
				//UnityEngine.Debug.Log("MbTilesCache: adding " + key);
				_mbTiles[cacheKey.tileset].AddTile(cacheKey, data);
			}
		}


		/// <summary>
		/// TEMPORARY HACK TO GET KEY DATA BACK OUT OF URL
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private MbTiles.MbTiles.CacheKey extractCacheKey(string url)
		{
			MbTiles.MbTiles.CacheKey ck = new MbTiles.MbTiles.CacheKey();
			string tileId = string.Empty;
			if (url.Contains("mapbox.terrain-rgb"))
			{
				ck.tileset = "mapbox.terrain-rgb";
				tileId = url.Substring(
					url.IndexOf("rgb/") + "rgb/".Length,
					url.IndexOf(".png") - (url.IndexOf("rgb/") + "rgb/".Length)
				);
			}
			else if (url.Contains("mapbox.satellite"))
			{
				ck.tileset = "mapbox.satellite";
				tileId = url.Substring(
					url.IndexOf("satellite/") + "satellite/".Length,
					url.IndexOf(".png") - (url.IndexOf("satellite/") + "satellite/".Length)
				);
			}

			string[] tokens = tileId.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			ck.zoom = Convert.ToInt32(tokens[0]);
			ck.x = Convert.ToInt64(tokens[1]);
			ck.y = Convert.ToInt64(tokens[2]);

			return ck;
		}


		public byte[] Get(string key)
		{
			MbTiles.MbTiles.CacheKey cacheKey = extractCacheKey(key);
			if (!_mbTiles.ContainsKey(cacheKey.tileset))
			{
				return null;
			}

			return _mbTiles[cacheKey.tileset].GetTile(cacheKey);
		}


		public void Clear()
		{
			throw new NotImplementedException("MbTilesCache: Clear is not implemented");
		}


	}
}