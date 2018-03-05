namespace Mapbox.Platform.Cache
{


	using Mapbox.Map;
	using Mapbox.Platform.MbTiles;
	using System;
	using System.Collections.Generic;
	using System.Linq;


	public class MbTilesCache : ICache, IDisposable
	{

		public MbTilesCache(uint maxCacheSize)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
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


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private bool _disposed;
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, MbTilesDb> _mbTiles;


		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}


		public void Add(string mapId, CanonicalTileId tileId, CacheItem item, bool forceInsert)
		{

			mapId = cleanMapId(mapId);

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1} {2} forceInsert:{3}", methodName, mapId, tileId, forceInsert);
#endif

			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(mapId))
				{
					initializeMbTiles(mapId);
				}
			}

			MbTilesDb currentMbTiles = _mbTiles[mapId];

			if (!currentMbTiles.TileExists(tileId) || forceInsert)
			{
				_mbTiles[mapId].AddTile(tileId, item, forceInsert);
			}
		}


		private void initializeMbTiles(string mapId)
		{
			if (string.IsNullOrEmpty(mapId))
			{
				throw new Exception("Cannot intialize MbTilesCache without a map id");
			}

			mapId = cleanMapId(mapId);

			MbTilesDb mbt = new MbTilesDb(mapId + ".cache", _maxCacheSize);
			MetaDataRequired md = new MetaDataRequired()
			{
				TilesetName = mapId,
				Description = "TODO: " + mapId,
				Format = "TODO: " + mapId,
				Type = "TODO: " + mapId,
				Version = 1
			};
			mbt.CreateMetaData(md);

			_mbTiles[mapId] = mbt;
		}



		public CacheItem Get(string mapId, CanonicalTileId tileId)
		{
			mapId = cleanMapId(mapId);

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1} {2}", methodName, mapId, tileId);
#endif

			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(mapId))
				{
#if MAPBOX_DEBUG_CACHE
					UnityEngine.Debug.LogFormat("initializing MbTiles {0}", mapId);
#endif
					initializeMbTiles(mapId);
				}
			}

			CacheItem item = _mbTiles[mapId].GetTile(tileId);
			if (null == item)
			{
				return null;
			}

			return item;
		}


		public void Clear()
		{
			string[] toDelete = _mbTiles.Keys.ToArray();
			foreach (var mapId in toDelete)
			{
				Clear(mapId);
			}
		}


		public void Clear(string mapId)
		{
			mapId = cleanMapId(mapId);
			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(mapId)) { return; }

				_mbTiles[mapId].Delete();
				_mbTiles[mapId].Dispose();
				_mbTiles[mapId] = null;
				_mbTiles.Remove(mapId);
			}
		}


		/// <summary>
		/// Map ID (tile set name) could be somehting like 'mapbox://styles/mapbox/dark-v9.cache'.
		/// This doesn't work as a file name
		/// </summary>
		/// <param name="mapId">Map ID, tile set name</param>
		/// <returns></returns>
		private string cleanMapId(string mapId)
		{
			return mapId.Substring(mapId.LastIndexOf('/') + 1);
		}

	}
}
