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
			_mbTiles = new Dictionary<string, MbTilesDb>();
		}


		#region IDisposable


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
						MbTilesDb mbt = mbtCache.Value;
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
		private Dictionary<string, MbTilesDb> _mbTiles;


		public void Add(string key, byte[] data)
		{
			MbTilesDb.CacheKey cacheKey = extractCacheKey(key);
			//if (data.Length > 100000)
			//{
			//	//UnityEngine.Debug.LogFormat("size:{0} {1}", data.Length, key);
			//	System.IO.File.WriteAllBytes(
			//		string.Format(@"C:\mb\ConvertTerrainRGB\data\{0}-{1}-{2}.png", cacheKey.zoom, cacheKey.x, cacheKey.y)
			//		, data
			//	);
			//}

			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(cacheKey.tileset))
				{
					initializeMbTiles(cacheKey);
				}
			}

			MbTilesDb currentMbTiles = _mbTiles[cacheKey.tileset];

			if (!currentMbTiles.TileExists(cacheKey))
			{
				_mbTiles[cacheKey.tileset].AddTile(cacheKey, data);
			}
		}


		private void initializeMbTiles(MbTilesDb.CacheKey cacheKey)
		{
			MbTilesDb mbt = new MbTilesDb(cacheKey.tileset + ".cache", _maxCacheSize);
			MetaDataRequired md = new MetaDataRequired()
			{
				TilesetName = cacheKey.tileset,
				Description = "TODO: " + cacheKey.tileset,
				Format = "TODO: " + cacheKey.tileset,
				Type = "TODO: " + cacheKey.tileset,
				Version = 1
			};
			mbt.CreateMetaData(md);

			_mbTiles[cacheKey.tileset] = mbt;
		}


		/// <summary>
		/// TEMPORARY HACK TO GET TILESETNAME AND TILEID BACK OUT OF URL
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private MbTilesDb.CacheKey extractCacheKey(string url)
		{
			//UnityEngine.Debug.Log("url:" + url);
			MbTilesDb.CacheKey ck = new MbTilesDb.CacheKey();


			//https://api.mapbox.com:443/v4/mapbox.terrain-rgb/11/384/799.pngraw?events=true
			//https://api.mapbox.com:443/v4/mapbox.satellite/11/384/799.png?events=true
			//https://api.mapbox.com:443/styles/v1/mapbox/streets-v10/tiles/16/10474/25333?events=true
			//https://api.mapbox.com:443/styles/v1/mapbox/dark-v9/tiles/16/10473/25332?events=true
			//https://api.mapbox.com:443/v4/mapbox.mapbox-streets-v7/16/10474/25333.vector.pbf?events=true
			//https://api.mapbox.com:443/styles/v1/mapbox/outdoors-v10/tiles/15/5241/12663?events=true

			//if (url.Contains("mapbox.terrain-rgb")) { ck.tileset = "mapbox.terrain-rgb"; }
			//if (url.Contains("mapbox.satellite")) { ck.tileset = "mapbox.satellite"; }
			//if (url.Contains("streets-v10")) { ck.tileset = "mapbox.streets.style.v10"; }
			//if (url.Contains("mapbox.mapbox-streets-v7")) { ck.tileset = "mapbox.streets.vt.v7"; }

			if (url.Contains("mapbox.satellite"))
			{
				ck.tileset = "mapbox.satellite";
			}
			else
			{
				// my regex kung fu is baaaad
				// eg 'mapbox.terrain-rgb' becomes just 'terrain-rgb'
				// and 'mapbox.mapbox-streets-v7' -> 'mapbox-streets'
				System.Text.RegularExpressions.Regex regexTileset = new System.Text.RegularExpressions.Regex(@"\w+[-]\w+");
				System.Text.RegularExpressions.Match matchTileset = regexTileset.Match(url);
				if (!matchTileset.Success)
				{
					throw new Exception(string.Format("could not extract tile id from url: {0}", url));
				}
				ck.tileset = matchTileset.Value;

			}


			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\d+\/\d+\/\d+");
			System.Text.RegularExpressions.Match match = regex.Match(url);
			if (!match.Success)
			{
				throw new Exception(string.Format("could not extract tile id from url: {0}", url));
			}
			string tileId = match.Value;

			string[] tokens = tileId.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			ck.zoom = Convert.ToInt32(tokens[0]);
			ck.x = Convert.ToInt64(tokens[1]);
			ck.y = Convert.ToInt64(tokens[2]);

			if (string.IsNullOrEmpty(ck.tileset))
			{
				throw new Exception(string.Format("tileset not set: {0}", url));
			}

			return ck;
		}


		public byte[] Get(string key)
		{
			MbTiles.MbTilesDb.CacheKey cacheKey = extractCacheKey(key);
			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(cacheKey.tileset))
				{
					initializeMbTiles(cacheKey);
				}
			}

			return _mbTiles[cacheKey.tileset].GetTile(cacheKey);
		}


		public void Clear()
		{
			throw new NotImplementedException("MbTilesCache: Clear is not implemented");
		}


	}
}