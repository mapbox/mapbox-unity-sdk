
using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
	public interface ISqliteCache
	{
		event Action<string> DataPrunedForFile;
		event Action DatabaseCleared;
		
		void ReadySqliteDatabase();
		bool IsUpToDate();
		
		uint MaxCacheSize { get; }
		long TileCount(string tilesetName);
		
		void Add(MapboxTileData item, bool replaceIfExists, Action<bool> callback = null);
		void SyncAdd(string tilesetName, CanonicalTileId tileId, byte[] data, string path, string etag, DateTime? expirationDate, bool forceInsert);
		T Get<T>(string tilesetId, CanonicalTileId tileId, T data = null) where T : MapboxTileData, new();
		void UpdateExpiration(string tilesetId, CanonicalTileId tileId, DateTime date);
		int RemoveData(string tilesetId, int zoom, int i, int i1);
		List<tiles> GetAllTiles();
		
		bool ClearDatabase();
		int Clear(string tilesetId);
		bool DeleteSqliteFile();
	}
}
