using UnityEngine.PlayerLoop;

namespace Mapbox.Platform.Cache
{
	using Mapbox.Map;
	using Mapbox.Utils;
	using SQLite4Unity3d;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using UnityEngine;

	public class SQLiteCache : ICache, IDisposable
	{
		/// <summary>
		/// maximum number tiles that get cached
		/// </summary>
		public uint MaxCacheSize
		{
			get { return _maxTileCount; }
		}


		/// <summary>
		/// Check cache size every n inserts
		/// </summary>
		public uint PruneCacheDelta
		{
			get { return _pruneCacheDelta; }
		}


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private bool _disposed;
		private string _dbName;
		private string _dbPath;
		private SQLiteConnection _sqlite;
		private readonly uint _maxTileCount;

		/// <summary>check cache size only every '_pruneCacheDelta' calls to 'Add()' to avoid being too chatty with the database</summary>
		private const int _pruneCacheDelta = 20;

		/// <summary>counter to keep track of calls to `Add()`</summary>
		private int _pruneCacheCounter = 0;

		private object _lock = new object();


		public SQLiteCache(uint? maxTileCount = null, string dbName = "cache.db")
		{
			_maxTileCount = maxTileCount ?? 3000;
			_dbName = dbName;
			init();
		}

		#region idisposable

		~SQLiteCache()
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
					if (null != _sqlite)
					{
						_sqlite.Execute("VACUUM;"); // compact db to keep file size small
						_sqlite.Close();
						_sqlite.Dispose();
						_sqlite = null;
					}
				}

				_disposed = true;
			}
		}

		#endregion

		private void init()
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			OpenOrCreateDatabase(_dbName);

			//hrmpf: multiple PKs not supported by sqlite.net
			//https://github.com/praeclarum/sqlite-net/issues/282
			//do it via plain SQL

			CreateTables();
			PragmaCommands();
		}

		/// <summary>
		/// <para>Reinitialize cache.</para>
		/// <para>This is needed after 'Clear()' to recreate the cache database.</para>
		/// <para>And has been implemented on purpose to not hold on to any references to the cache directory after 'Clear()'</para>
		/// </summary>
		public void ReInit()
		{
			if (null != _sqlite)
			{
				_sqlite.Dispose();
				_sqlite = null;
			}

			init();
		}

		private void OpenOrCreateDatabase(string dbName)
		{
			_dbPath = GetFullDbPath(dbName);
			_sqlite = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
		}

		public static string GetFullDbPath(string dbName)
		{
			string dbPath = Path.Combine(Application.persistentDataPath, "cache");
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
			dbPath = Path.GetFullPath(dbPath);
#endif
			if (!Directory.Exists(dbPath))
			{
				Directory.CreateDirectory(dbPath);
			}

			dbPath = Path.Combine(dbPath, dbName);

			return dbPath;
		}

		public void Add(string tilesetName, CanonicalTileId tileId, CacheItem item, bool forceInsert = false)
		{
#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1} {2} forceInsert:{3}", methodName, tileset, tileId, forceInsert);
#endif
			try
			{
				// tile exists and we don't want to overwrite -> exit early
				if (
					TileExists(tilesetName, tileId)
					&& !forceInsert
				)
				{
					return;
				}

				int? tilesetId = GetOrCraeteTileset(tilesetName);

				if (tilesetId < 0)
				{
					Debug.LogErrorFormat("could not get tilesetID for [{0}] tile: {1}", tilesetName, tileId);
					return;
				}

				var newTile = new tiles
				{
					tile_set = tilesetId.Value,
					zoom_level = tileId.Z,
					tile_column = tileId.X,
					tile_row = tileId.Y,
					tile_data = item.Data,
					expirationdate = (int) UnixTimestampUtils.To(item.ExpirationDate),
					etag = item.ETag,
					accessed = (int) UnixTimestampUtils.To(DateTime.Now)
				};
				int rowsAffected = UpdateTile(newTile);
				if (rowsAffected == 0)
				{
					rowsAffected = (int) InsertTile(newTile);
					if (rowsAffected > 0)
					{
						_pruneCacheCounter++;
					}
				}
				if (rowsAffected < 1)
				{
					throw new Exception(string.Format("tile [{0} / {1}] was not inserted, rows affected:{2}", tilesetName, tileId, rowsAffected));
				}
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error inserting {0} {1} {2} ", tilesetName, tileId, ex);
			}

			if (0 == _pruneCacheCounter % _pruneCacheDelta)
			{
				_pruneCacheCounter = 0;
				//prune();
				pruneNonOfflineTiles();
			}
		}

		private int? GetOrCraeteTileset(string tilesetName)
		{
			int? tilesetId;
			lock (_lock)
			{
				tilesetId = getTilesetId(tilesetName);
				if (!tilesetId.HasValue)
				{
					tilesetId = insertTileset(tilesetName);
				}
			}

			return tilesetId;
		}

		private void pruneNonOfflineTiles()
		{
			long tileCnt = _sqlite.ExecuteScalar<long>("SELECT COUNT(id) FROM (SELECT id FROM tiles LEFT JOIN tile2offline ON id = tileId WHERE tileId IS NULL)");

			if (tileCnt > MaxCacheSize)
			{
				var query = "SELECT max(accessed) " +
				            "FROM ( " +
				            "    SELECT accessed " +
				            "    FROM tiles " +
				            "    LEFT JOIN tile2offline " +
				            "    ON tileId = tiles.id " +
				            "    WHERE tileId IS NULL " +
				            "  ORDER BY accessed ASC LIMIT ?1 " +
				            ") ";

				var command = _sqlite.CreateCommand(query, 5);
				var accessed = command.ExecuteScalar<int>();

				var tileQuery = "DELETE FROM tiles " +
				                "WHERE id IN ( " +
				                "  SELECT id FROM tiles " +
				                "  LEFT JOIN tile2offline " +
				                "  ON tileId = tiles.id " +
				                "  WHERE tileId IS NULL " +
				                "  AND accessed <= ?1 " +
				                ") ";
				var tileCommand = _sqlite.CreateCommand(tileQuery, accessed);
				var rowChanged = tileCommand.ExecuteNonQuery();
			}

//
//
// 			long tileCnt = _sqlite.ExecuteScalar<long>("SELECT COUNT(zoom_level) FROM tiles");
//
// 			if (tileCnt < _maxTileCount)
// 			{
// 				return;
// 			}
//
// 			long toDelete = tileCnt - _maxTileCount;
//
// #if MAPBOX_DEBUG_CACHE
// 			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
// 			Debug.LogFormat("{0} {1} about to prune()", methodName, _tileset);
// #endif
//
// 			try
// 			{
// 				// no 'ORDER BY' or 'LIMIT' possible if sqlite hasn't been compiled with 'SQLITE_ENABLE_UPDATE_DELETE_LIMIT'
// 				// https://sqlite.org/compile.html#enable_update_delete_limit
// 				_sqlite.Execute("DELETE FROM tiles WHERE rowid IN ( SELECT rowid FROM tiles ORDER BY timestamp ASC LIMIT ? );", toDelete);
// 			}
// 			catch (Exception ex)
// 			{
// 				Debug.LogErrorFormat("error pruning: {0}", ex);
// 			}
		}

		/// <summary>
		/// Returns the tile data, otherwise null
		/// </summary>
		/// <param name="tilesetName">Name of the tileset/style requested</param>
		/// <param name="tileId">Canonical tile id to identify the tile</param>
		/// <returns>tile data as byte[], if tile is not cached returns null</returns>
		public CacheItem Get(string tilesetName, CanonicalTileId tileId)
		{
#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			Debug.LogFormat("{0} {1} {2}", methodName, _tileset, tileId);
#endif
			tiles tile = null;

			try
			{
				int? tilesetId = getTilesetId(tilesetName);
				if (!tilesetId.HasValue)
				{
					return null;
				}

				tile = _sqlite
					.Table<tiles>()
					.Where(t =>
						t.tile_set == tilesetId.Value
						&& t.zoom_level == tileId.Z
						&& t.tile_column == tileId.X
						&& t.tile_row == tileId.Y
					)
					.FirstOrDefault();
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("error getting tile {1} {2} from cache{0}{3}", Environment.NewLine, tilesetName, tileId, ex);
				return null;
			}

			if (null == tile)
			{
				return null;
			}

			return new CacheItem()
			{
				Data = tile.tile_data,
				AddedToCacheTicksUtc = tile.timestamp,
				ETag = tile.etag
			};
		}

		/// <summary>
		/// Check if tile exists
		/// </summary>
		/// <param name="tilesetName">Name of the tileset/style requested</param>
		/// <param name="tileId">Canonical tile id</param>
		/// <returns>True if tile exists</returns>
		public bool TileExists(string tilesetName, CanonicalTileId tileId)
		{
			var query = "SELECT length(tile_data) " +
			"FROM tiles " +
			"WHERE tile_set    = ?1 " +
			"  AND zoom_level  = ?2 " +
			"  AND tile_column = ?3 " +
			"  AND tile_row    = ?4 " +
			"LIMIT 1";
			var countCommand = _sqlite.CreateCommand(query,
				tilesetName,
				tileId.Z,
				tileId.X,
				tileId.Y);
			var count = countCommand.ExecuteScalar<int>();

			return count > 0;
		}

		/// <summary>
		/// FOR INTERNAL DEBUGGING ONLY - DON'T RELY ON IN PRODUCTION
		/// </summary>
		/// <param name="tilesetName"></param>
		/// <returns></returns>
		public long TileCount(string tilesetName)
		{
			int? tilesetId = getTilesetId(tilesetName);
			if (!tilesetId.HasValue)
			{
				return 0;
			}

			return _sqlite
				.Table<tiles>()
				.Where(t => t.tile_set == tilesetId.Value)
				.LongCount();
		}

		public int GetAmbientTileCount()
		{
			return _sqlite.ExecuteScalar<int>("SELECT COUNT(id) FROM (SELECT id FROM tiles LEFT JOIN tile2offline ON id = tileId WHERE tileId IS NULL)");
		}

		public int GetOfflineTileCount()
		{
			return _sqlite.ExecuteScalar<int>("SELECT COUNT(id) FROM (SELECT id FROM tiles LEFT JOIN tile2offline ON id = tileId WHERE tileId IS NOT NULL)");
		}

		public int GetOfflineTileCount(string offlineMapName)
		{
			var query = "SELECT COUNT(tileId) FROM tile2offline WHERE mapId = (SELECT id FROM offlinemaps WHERE name = ?1)";
			var command = _sqlite.CreateCommand(query, offlineMapName);
			return command.ExecuteScalar<int>();
		}

		public int GetOfflineDataSize(int offlineMapId)
		{
			var query = "SELECT SUM(LENGTH(tile_data)) " +
			            "FROM tile2offline, tiles " +
			            "WHERE mapId = ?1 " +
			            "AND tileId = tiles.id ";
			var command = _sqlite.CreateCommand(query, offlineMapId);
			return command.ExecuteScalar<int>();
		}

		public int GetOfflineDataSize(string offlineMapName)
		{
			var query = "SELECT SUM(LENGTH(tile_data)) " +
			            "FROM tile2offline, tiles " +
			            "WHERE mapId = (SELECT id FROM offlinemaps WHERE name = ?1) " +
			            "AND tileId = tiles.id ";
			var command = _sqlite.CreateCommand(query, offlineMapName);
			return command.ExecuteScalar<int>();
		}

		public void ClearAmbientCache()
		{
			var query = "DELETE FROM tiles WHERE id NOT IN ( SELECT tileId FROM tile2offline)";
			var clearAmbientCommand = _sqlite.CreateCommand(query);
			clearAmbientCommand.ExecuteNonQuery();
		}

		public void Clear(string tilesetName)
		{

		}

		/// <summary>
		/// <para>Delete the database file.</para>
		/// <para>Call 'ReInit()' if you intend to continue using the cache after 'Clear()!</para>
		/// </summary>
		public void Clear()
		{
			//already disposed
			if (null == _sqlite)
			{
				return;
			}

			_sqlite.Close();
			_sqlite.Dispose();
			_sqlite = null;

			Debug.LogFormat("deleting {0}", _dbPath);

			// try several times in case SQLite needs a bit more time to dispose
			for (int i = 0; i < 5; i++)
			{
				try
				{
					File.Delete(_dbPath);
					return;
				}
				catch
				{
#if !WINDOWS_UWP
					System.Threading.Thread.Sleep(100);
#else
					System.Threading.Tasks.Task.Delay(100).Wait();
#endif
				}
			}

			// if we got till here, throw on last try
			File.Delete(_dbPath);
		}

		public void MarkOffline(int offlineMapId, string tilesetName, CanonicalTileId tileId)
		{
			try
			{
				var query = "INSERT OR IGNORE INTO tile2offline (mapId, tileId)" +
				            "SELECT ?1, tiles.id " +
				            "FROM tiles " +
				            "WHERE tile_set    = (SELECT id FROM tilesets WHERE name = ?2) " +
				            "  AND zoom_level  = ?3 " +
				            "  AND tile_column = ?4 " +
				            "  AND tile_row    = ?5";
				var command = _sqlite.CreateCommand(query,
					offlineMapId,
					tilesetName,
					tileId.Z,
					tileId.X,
					tileId.Y);
				command.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error inserting {0} {1} {2} ", offlineMapId, tileId, ex);
			}
		}

		public void DeleteOfflineMap(int offlineMapId)
		{
			var query = "DELETE FROM offlinemaps WHERE id = ?";
			var command = _sqlite.CreateCommand(query, offlineMapId);
			command.ExecuteNonQuery();
		}

		public void DeleteOfflineMap(string offlineMapName)
		{
			var query = "DELETE FROM offlinemaps WHERE name = ?";
			var command = _sqlite.CreateCommand(query, offlineMapName);
			command.ExecuteNonQuery();
		}



		public Dictionary<string, int> GetOfflineMapList()
		{
			var mapList = new Dictionary<string, int>();

			var maps = _sqlite.Table<offlineMaps>().ToList();

			foreach (var offlineMap in maps)
			{
				mapList.Add(offlineMap.name, _sqlite.Table<tile2offline>().Where(x => x.mapId == offlineMap.id).Count());
			}

			return mapList;
		}

		private int insertTileset(string tilesetName)
		{
			try
			{
				_sqlite.BeginTransaction(true);
				tilesets newTileset = new tilesets {name = tilesetName};
				int rowsAffected = _sqlite.Insert(newTileset);
				if (1 != rowsAffected)
				{
					throw new Exception(string.Format("tileset [{0}] was not inserted, rows affected:{1}", tilesetName, rowsAffected));
				}

				return newTileset.id;
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("could not insert tileset [{0}]: {1}", tilesetName, ex);
				return -1;
			}
			finally
			{
				_sqlite.Commit();
			}
		}

		private int? getTilesetId(string tilesetName)
		{
			tilesets tileset = _sqlite
				.Table<tilesets>()
				.Where(ts => ts.name.Equals(tilesetName))
				.FirstOrDefault();
			return null == tileset ? (int?) null : tileset.id;
		}

		private int insertOfflineMap(string offlineMapName)
		{
			try
			{
				_sqlite.BeginTransaction(true);
				var newOfflineMap = new offlineMaps() {name = offlineMapName};
				int rowsAffected = _sqlite.Insert(newOfflineMap);
				if (1 != rowsAffected)
				{
					throw new Exception(string.Format("tileset [{0}] was not inserted, rows affected:{1}", offlineMapName, rowsAffected));
				}

				return newOfflineMap.id;
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("could not insert offlinemaps [{0}]: {1}", offlineMapName, ex);
				return -1;
			}
			finally
			{
				_sqlite.Commit();
			}
		}

		private int? getOfflineMapId(string offlineMapName)
		{
			var offlineMap = _sqlite
				.Table<offlineMaps>()
				.Where(ts => ts.name.Equals(offlineMapName))
				.FirstOrDefault();
			return null == offlineMap ? (int?) null : offlineMap.id;
		}

		public int GetOrAddOfflineMapId(string offlineMapName)
		{
			int? offlineMapId;
			offlineMapId = getOfflineMapId(offlineMapName);
			if (!offlineMapId.HasValue)
			{
				offlineMapId = insertOfflineMap(offlineMapName);
			}

			return offlineMapId.Value;
		}

		public int UpdateTile(tiles newTile)
		{
			var query = "UPDATE tiles " +
			            "SET tile_data = ?1, timestamp = ?2, expirationdate = ?3, etag = ?4, accessed = ?5 " +
			            "WHERE tile_set = ?6 AND zoom_level = ?7 AND tile_column = ?8 AND tile_row = ?9 ";
			var command = _sqlite.CreateCommand(query,
				newTile.tile_data,
				newTile.timestamp,
				newTile.expirationdate,
				newTile.etag,
				newTile.accessed,
				newTile.tile_set,
				newTile.zoom_level,
				newTile.tile_column,
				newTile.tile_row);
			return command.ExecuteNonQuery();
		}

		public long InsertTile(tiles newTile)
		{
			var query = "INSERT INTO tiles " +
			            "(tile_set, zoom_level, tile_column, tile_row, tile_data, timestamp, expirationdate, etag, accessed)" +
			            "VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9)";

			var command = _sqlite.CreateCommand(query,
				newTile.tile_set,
				newTile.zoom_level,
				newTile.tile_column,
				newTile.tile_row,
				newTile.tile_data,
				newTile.timestamp,
				newTile.expirationdate,
				newTile.etag,
				newTile.accessed);
			var rowsChanged = command.ExecuteNonQuery();
			if (rowsChanged > 0)
			{
				newTile.id = (int) SQLite3.LastInsertRowid(_sqlite.Handle);
			}

			return rowsChanged;
		}



		private void CreateTables()
		{
			List<SQLiteConnection.ColumnInfo> colInfoTileset = _sqlite.GetTableInfo(typeof(tilesets).Name);
			if (0 == colInfoTileset.Count)
			{
				string cmdCreateTableTilesets = @"CREATE TABLE tilesets(
id    INTEGER PRIMARY KEY ASC AUTOINCREMENT NOT NULL UNIQUE,
name  STRING  NOT NULL
);";
				_sqlite.Execute(cmdCreateTableTilesets);
				string cmdCreateIdxNames = @"CREATE UNIQUE INDEX idx_names ON tilesets (name ASC);";
				_sqlite.Execute(cmdCreateIdxNames);
			}

			List<SQLiteConnection.ColumnInfo> colInfoTiles = _sqlite.GetTableInfo(typeof(tiles).Name);
			if (0 == colInfoTiles.Count)
			{
				string cmdCreateTableTiles = @"CREATE TABLE tiles(
id 			 INTEGER PRIMARY KEY ASC AUTOINCREMENT NOT NULL UNIQUE, 
tile_set     INTEGER REFERENCES tilesets (id) ON DELETE CASCADE ON UPDATE CASCADE,
zoom_level   INTEGER NOT NULL,
tile_column  BIGINT  NOT NULL,
tile_row     BIGINT  NOT NULL,
tile_data    BLOB    NOT NULL,
timestamp    INTEGER NOT NULL,
expirationdate    INTEGER NOT NULL,
etag         TEXT,
accessed INTEGER NOT NULL,
CONSTRAINT tileConstraint UNIQUE (tile_set, zoom_level, tile_column, tile_row)
);";
				_sqlite.Execute(cmdCreateTableTiles);

				string cmdIdxTileset = "CREATE INDEX idx_tileset ON tiles (tile_set ASC);";
				_sqlite.Execute(cmdIdxTileset);
				string cmdIdxTimestamp = "CREATE INDEX idx_timestamp ON tiles (timestamp ASC);";
				_sqlite.Execute(cmdIdxTimestamp);
			}


			List<SQLiteConnection.ColumnInfo> colInfoOfflineMaps = _sqlite.GetTableInfo(typeof(offlineMaps).Name);
			if (0 == colInfoOfflineMaps.Count)
			{
				string cmdCreateTableOfflineMaps = @"CREATE TABLE offlinemaps(
id    INTEGER PRIMARY KEY ASC AUTOINCREMENT NOT NULL UNIQUE,
name  STRING  NOT NULL
);";
				_sqlite.Execute(cmdCreateTableOfflineMaps);
				string cmdCreateIdxOfflineMapNames = @"CREATE UNIQUE INDEX idx_offlineMapNames ON offlinemaps (name ASC);";
				_sqlite.Execute(cmdCreateIdxOfflineMapNames);
			}

			List<SQLiteConnection.ColumnInfo> colInfoTileToOffline = _sqlite.GetTableInfo(typeof(tile2offline).Name);
			if (0 == colInfoTileToOffline.Count)
			{
				string cmdCreateTableTile2Offline = @"CREATE TABLE tile2offline(
tileId    INTEGER NOT NULL,
mapId    INTEGER NOT NULL,
CONSTRAINT tileAssignmentConstraint UNIQUE (tileId, mapId)
);";
				_sqlite.Execute(cmdCreateTableTile2Offline);
				string cmdCreateIdxOfflineMap2Tiles = @"CREATE UNIQUE INDEX idx_offlineMapToTiles ON tile2offline (tileId, mapId ASC);";
				_sqlite.Execute(cmdCreateIdxOfflineMap2Tiles);
			}
		}

		private void PragmaCommands()
		{
			// some pragmas to speed things up a bit :-)
			// inserting 1,000 tiles takes 1-2 sec as opposed to ~20 sec
			string[] cmds = new string[]
			{
				"PRAGMA synchronous=OFF",
				"PRAGMA count_changes=OFF",
				"PRAGMA journal_mode=MEMORY",
				"PRAGMA temp_store=MEMORY"
			};
			foreach (var cmd in cmds)
			{
				try
				{
					_sqlite.Execute(cmd);
				}
				catch (SQLiteException ex)
				{
					// workaround for sqlite.net's exeception:
					// https://stackoverflow.com/a/23839503
					if (ex.Result != SQLite3.Result.Row)
					{
						UnityEngine.Debug.LogErrorFormat("{0}: {1}", cmd, ex);
						// TODO: when mapbox-sdk-cs gets backported to its own repo -> throw
						//throw; // to throw or not to throw???
					}
				}
			}
		}

	}
}