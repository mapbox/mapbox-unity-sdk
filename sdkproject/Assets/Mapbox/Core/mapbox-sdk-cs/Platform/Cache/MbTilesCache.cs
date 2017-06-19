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
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, MbTilesDb> _mbTiles;


		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}


		public void Add(string mapId, CanonicalTileId tileId, byte[] data)
		{

			mapId = cleanMapId(mapId);

			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(mapId))
				{
					initializeMbTiles(mapId);
				}
			}

			MbTilesDb currentMbTiles = _mbTiles[mapId];

			if (!currentMbTiles.TileExists(tileId))
			{
				_mbTiles[mapId].AddTile(tileId, data);
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



		public byte[] Get(string mapId, CanonicalTileId tileId)
		{
			mapId = cleanMapId(mapId);
			lock (_lock)
			{
				if (!_mbTiles.ContainsKey(mapId))
				{
					initializeMbTiles(mapId);
				}
			}

			return _mbTiles[mapId].GetTile(tileId);
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