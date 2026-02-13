using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.SQLite;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache
{
	public class SqliteCache : ISqliteCache, IDisposable
	{
		public event Action<string> DataPrunedForFile = s => { };
		public event Action DatabaseCleared = () => { };

		public const int PruneCacheDelta = 20;
		
		protected string PersistantCacheRootFolderPath;
		protected string CacheRootFolderName = "Mapbox/SqliteCache";
		protected string cacheFileName = "cache.db";
		protected TaskManager _taskManager;
		protected const int DATABASE_CODE_VERSION = 2;

		public uint MaxCacheSize => _maxTileCount;


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private bool _disposed;
		private string _dbName;
		private string _dbPath;
		private SQLiteConnection _sqlite;
		private readonly uint _maxTileCount;
		/// <summary>check cache size only every '_pruneCacheDelta' calls to 'Add()' to avoid being too chatty with the database</summary>
		
		/// <summary>counter to keep track of calls to `Add()`</summary>
		private int _pruneCacheCounter = 0;
		private readonly object _lock = new object();


		public SqliteCache(TaskManager taskManager, uint? maxTileCount = null, string dbFolderNamePostFix = "")
		{
			CacheRootFolderName += dbFolderNamePostFix;
			PersistantCacheRootFolderPath = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
			_taskManager = taskManager;
			_maxTileCount = maxTileCount ?? 3000;
			_dbName = cacheFileName;
			ReadySqliteDatabase();
		}

		/// <summary>
		/// <para>Reinitialize cache.</para>
		/// <para>This is needed after 'Clear()' to recreate the cache database.</para>
		/// <para>And has been implemented on purpose to not hold on to any references to the cache directory after 'Clear()'</para>
		/// </summary>
		
		public bool IsUpToDate()
		{
			var fileVersion = _sqlite.ExecuteScalar<int>("PRAGMA user_version");
			return fileVersion == DATABASE_CODE_VERSION;
		}

		/// <summary>
		/// Creates file if necessary
		/// Creates tables and indexes
		/// </summary>
		public void ReadySqliteDatabase()
		{
			if (_sqlite == null)
			{
				OpenOrCreateDatabase();
			}

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
tile_data    BLOB,
tile_path    TEXT,
timestamp    INTEGER NOT NULL,
etag         TEXT,
expirationDate INTEGER,
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

			_sqlite.Execute("PRAGMA user_version=" + DATABASE_CODE_VERSION);
		}

		
		public long TileCount(string tilesetName)
		{
			int? tilesetId = getTilesetId(tilesetName);
			if (!tilesetId.HasValue) { return 0; }

			return _sqlite
				.Table<tiles>()
				.Where(t => t.tile_set == tilesetId.Value)
				.LongCount();
		}
		
		public void Add(MapboxTileData item, bool forceInsert = false, Action<bool> callback = null)
		{
			Add(item.TilesetId, item.TileId, item.Data, string.Empty, item.ETag, item.ExpirationDate, forceInsert, callback);
		}

		public void SyncAdd(string tilesetName, CanonicalTileId tileId, byte[] data, string path, string etag, DateTime? expirationDate, bool forceInsert)
		{
			try
			{
				// tile exists and we don't want to overwrite -> exit early
				if (TileExists(tilesetName, tileId) && !forceInsert
				)
				{
					return;
				}

				int? tilesetId = GetOrCreateTilesetId(tilesetName);

				if (tilesetId < 0)
				{
					Debug.LogErrorFormat("could not get tilesetID for [{0}] tile: {1}", tilesetName, tileId);
					return;
				}

				lock (_lock)
				{
					var nowInUnix = (int) UnixTimestampUtils.To(DateTime.Now);
					var newTile = new tiles
					{
						tile_set = tilesetId.Value,
						zoom_level = tileId.Z,
						tile_column = tileId.X,
						tile_row = tileId.Y,
						tile_data = data,
						tile_path = path,
						timestamp = nowInUnix,
						etag = etag,
						expirationDate = expirationDate.HasValue ? (int) UnixTimestampUtils.To(expirationDate.Value) : nowInUnix
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

					// int rowsAffected = _sqlite.InsertOrReplace(new tiles
					// {
					// 	tile_set = tilesetId.Value,
					// 	zoom_level = tileId.Z,
					// 	tile_column = tileId.X,
					// 	tile_row = tileId.Y,
					// 	tile_data = data,
					// 	tile_path = path,
					// 	timestamp = nowInUnix,
					// 	etag = etag,
					// 	expirationDate = expirationDate.HasValue ? (int) UnixTimestampUtils.To(expirationDate.Value) : nowInUnix
					// });
					// if (1 != rowsAffected)
					// {
					// 	throw new Exception(string.Format("tile [{0} / {1}] was not inserted, rows affected:{2}", tilesetName, tileId, rowsAffected));
					// }
				}
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error inserting {0} {1} {2} ", tilesetName, tileId, ex);
			}

			// update counter only when new tile gets inserted
			if (!forceInsert)
			{
				_pruneCacheCounter++;
			}

			if (0 == _pruneCacheCounter % PruneCacheDelta)
			{
				_pruneCacheCounter = 0;
				prune();
			}
		}

		public virtual void UpdateExpiration(string tilesetName, CanonicalTileId tileId, DateTime expirationDate)
		{
			_taskManager.AddTask(
				new SqliteCacheUpdateExpirationTaskWrapper<bool>()
				{
					TileId = tileId,
					DataAction = () =>
					{
						lock (_lock)
						{
							var tilesetId = getTilesetId(tilesetName);
							if (!tilesetId.HasValue)
							{
								tilesetId = insertTileset(tilesetName);
							}

							var query = "UPDATE tiles " +
							            "SET expirationdate = ?1" +
							            "WHERE tile_set = ?2 AND zoom_level = ?3 AND tile_column = ?4 AND tile_row = ?5 ";
							var command = _sqlite.CreateCommand(query,
								(int) UnixTimestampUtils.To(expirationDate),
								tilesetId,
								tileId.Z,
								tileId.X,
								tileId.Y);
							var rowsAffected = command.ExecuteNonQuery();
							if (1 != rowsAffected)
							{
								throw new Exception(string.Format("tile [{0} / {1}] was not updated, rows affected:{2}", tilesetName, tileId, rowsAffected));
								return false;
							}
						}
						return true;
					}
				}, 4);
		}

		public virtual T Get<T>(string tilesetName, CanonicalTileId tileId, T data = null) where T : MapboxTileData, new()
		{
			lock (_lock)
			{
				if (FetchSqliteEntry(tileId, tilesetName, out var tile))
				{
					if (data == null)
					{
						return new T()
						{
							TileId = tileId,
							TilesetId = tilesetName,
							CacheType = CacheType.SqliteCache,
							Data = tile.tile_data,
							ETag = tile.etag,
							ExpirationDate = tile.expirationDateFormatted
						};
					}
					else
					{
						data.TileId = tileId;
						data.TilesetId = tilesetName;
						data.CacheType = CacheType.SqliteCache;
						data.Data = tile.tile_data;
						data.ETag = tile.etag;
						data.ExpirationDate = tile.expirationDateFormatted;
						return data;
					}
				}
				else
				{
					return null;
				}
			}
		}
		
		public virtual int RemoveData(string tilesetName, int zoom, int x, int y)
		{
			lock (_lock)
			{
				var tilesetId = getTilesetId(tilesetName);
				var query = "DELETE FROM tiles " +
				            "WHERE tile_set = ?1 AND zoom_level = ?2 AND tile_column = ?3 AND tile_row = ?4 ";
				var command = _sqlite.CreateCommand(query,
					tilesetId,
					zoom,
					x,
					y);
				return command.ExecuteNonQuery();
			}
		}
		

		public int Clear(string tilesetName)
		{
			int? tilesetId = getTilesetId(tilesetName);
			if (!tilesetId.HasValue) { return 0; }
			//just delete on table 'tilesets', we've setup cascading which should take care of tabls 'tiles'
			return _sqlite.Delete<tilesets>(tilesetId.Value);
		}

		public List<tiles> GetAllTiles()
		{
			return _sqlite.Table<tiles>().ToList();
		}

		public bool ClearDatabase()
		{
			try
			{
				var tableNames = _sqlite.Table<SqliteSchemaObject>().Where
					(x => x.type == "table");

				foreach (var table in tableNames)
				{
					if (table.name == "sqlite_sequence") //sqlites own table, cannot be dropped
					{
						continue;
					}

					_sqlite.Execute("DROP TABLE " + table.name);
				}

				var indexNames = _sqlite.Table<SqliteSchemaObject>().Where
					(x => x.type == "index");

				foreach (var index in indexNames)
				{
					_sqlite.Execute("DROP INDEX "+ index.name );
				}

				_sqlite.Execute("VACUUM;");
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
				return false;
			}

			DatabaseCleared();
			return true;
		}

		public bool DeleteSqliteFile()
		{
			if (null == _sqlite) { return false; }

			_sqlite.Dispose();
			_sqlite = null;

			string cacheDirectory = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
			if (!Directory.Exists(cacheDirectory)) { return true; }
			var filePath = GetFullDbPath(_dbName);

			var isDeletedSuccesfully = true;
			var error = string.Empty;
			for (int i = 0; i < 5; i++)
			{
				Thread.Sleep(10);
				try
				{
					isDeletedSuccesfully = true;
					File.Delete(filePath);
				}
				catch (Exception e)
				{
					isDeletedSuccesfully = false;
					error = e.ToString();
				}
			}

			if (!isDeletedSuccesfully)
			{
				Debug.LogError(error);
			}

			DatabaseCleared();
			return isDeletedSuccesfully;
		}
		
		public static bool DeleteSqliteFolder()
		{
			string cacheDirectory = Path.Combine(Application.persistentDataPath, "Mapbox");
			if (!Directory.Exists(cacheDirectory)) { return true; }

			DirectoryInfo di = new DirectoryInfo(cacheDirectory);
			var isDeletedSuccesfully = true;
			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				if (folder.Name.StartsWith("SqliteCache"))
				{
					var error = string.Empty;
					for (int i = 0; i < 5; i++)
					{
						Thread.Sleep(10);
						try
						{
							folder.Delete(true);
							isDeletedSuccesfully = true;
							break;
						}
						catch (Exception e)
						{
							isDeletedSuccesfully = false;
							error = e.ToString();
						}
					}

					if (!isDeletedSuccesfully)
					{
						Debug.LogError(error);
					}
				}
			}

			return isDeletedSuccesfully;
		}
		
		
		
		
		
		
		protected virtual void Add(string tilesetName, CanonicalTileId tileId, byte[] data, string path, string etag,
			DateTime? expirationDate, bool forceInsert = false, Action<bool> callback = null)
		{
			//Debug.Log(string.Format("Sqlite add {0} - {1}", tileId, path));
			if (expirationDate == null)
			{
				Debug.Log("Expiration date shouldn't be null");
				callback?.Invoke(false);
			}
			
			_taskManager.AddTask(
				new SqliteCacheAddTaskWrapper<bool>()
				{
					TileId = tileId,
					DataAction = () =>
					{
						lock (_lock)
						{
							SyncAdd(tilesetName, tileId, data, path, etag, expirationDate, forceInsert);
						}

						return true;
					},
					DataCompleted = (resultTask, data) =>
					{
						if (resultTask.IsCompleted && !resultTask.IsFaulted)
						{
							callback?.Invoke(true);
						}
						else
						{
							Debug.Log(resultTask.Exception.ToString() + resultTask.Exception.Message);
							callback?.Invoke(false);
						}
						
					}
				}, 4);
		}
		
		public int UpdateTile(tiles newTile)
		{
			var query = "UPDATE tiles " +
			            "SET tile_data = ?1, tile_path = ?2, timestamp = ?3, expirationDate = ?4, etag = ?5" +
			            "WHERE tile_set = ?6 AND zoom_level = ?7 AND tile_column = ?8 AND tile_row = ?9 ";
			var command = _sqlite.CreateCommand(query,
				newTile.tile_data,
				newTile.tile_path,
				newTile.timestamp,
				newTile.expirationDate,
				newTile.etag,
				newTile.tile_set,
				newTile.zoom_level,
				newTile.tile_column,
				newTile.tile_row);
			return command.ExecuteNonQuery();
		}

		private long InsertTile(tiles newTile)
		{
			var query = "INSERT INTO tiles " +
			            "(tile_set, zoom_level, tile_column, tile_row, tile_data, tile_path, timestamp, expirationDate, etag)" +
			            "VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9)";

			var command = _sqlite.CreateCommand(query,
				newTile.tile_set,
				newTile.zoom_level,
				newTile.tile_column,
				newTile.tile_row,
				newTile.tile_data,
				newTile.tile_path,
				newTile.timestamp,
				newTile.expirationDate,
				newTile.etag);
			var rowsChanged = command.ExecuteNonQuery();
			if (rowsChanged > 0)
			{
				newTile.id = (int) SQLite3.LastInsertRowid(_sqlite.Handle);
			}

			return rowsChanged;
		}
		
		private void RunPragmas()
		{
			// some pragmas to speed things up a bit :-)
			// inserting 1,000 tiles takes 1-2 sec as opposed to ~20 sec
			string[] cmds = new string[]
			{
				"PRAGMA vacuum",
				"PRAGMA optimize",
				"PRAGMA synchronous=OFF",
				"PRAGMA count_changes=OFF",
				"PRAGMA foreign_keys = ON",
				"PRAGMA journal_mode=MEMORY",
				"PRAGMA temp_store=MEMORY",
				"PRAGMA user_version=" + DATABASE_CODE_VERSION
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

		private void OpenOrCreateDatabase()
		{
			_dbPath = GetFullDbPath(_dbName);
			_sqlite = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
			RunPragmas();
		}
		
		
		#region idisposable
		~SqliteCache()
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

		private string GetFullDbPath(string dbName)
		{
			string dbPath = PersistantCacheRootFolderPath; //Path.Combine(Application.persistentDataPath, "cache");
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
			dbPath = Path.GetFullPath(dbPath);
#endif
			if (!Directory.Exists(dbPath)) { Directory.CreateDirectory(dbPath); }
			dbPath = Path.Combine(dbPath, dbName);

			return dbPath;
		}
		
		private int? GetOrCreateTilesetId(string tilesetName)
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
		
		private void prune()
		{
			lock (_lock)
			{

				long tileCnt = _sqlite.ExecuteScalar<long>("SELECT COUNT(zoom_level) FROM tiles");

				if (tileCnt < _maxTileCount)
				{
					return;
				}

				long toDelete = (tileCnt - _maxTileCount) * 2;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			Debug.LogFormat("{0} {1} about to prune()", methodName, _tileset);
#endif

				tiles tile = null;
				try
				{
					var cmd = _sqlite.CreateCommand("SELECT * FROM tiles WHERE tile_path <> '' AND rowid IN ( SELECT rowid FROM tiles ORDER BY timestamp ASC LIMIT ? );", toDelete);
					var tilesToDelete = cmd.ExecuteQuery<tiles>();
					DeleteFile(tilesToDelete);
						
					// var thread = new Thread(DeleteFile);
					// thread.IsBackground = true;
					//thread.Start(tilesToDelete);

					// no 'ORDER BY' or 'LIMIT' possible if sqlite hasn't been compiled with 'SQLITE_ENABLE_UPDATE_DELETE_LIMIT'
					// https://sqlite.org/compile.html#enable_update_delete_limit
					_sqlite.Execute("DELETE FROM tiles WHERE rowid IN ( SELECT rowid FROM tiles ORDER BY timestamp ASC LIMIT ? );", toDelete);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("error pruning: {0}", ex);
					Debug.Log(string.Format("{0},{1},{2},{3}", tile.tile_set, tile.zoom_level, tile.tile_column, tile.tile_row));
				}
			}
		}

		private void DeleteFile(List<tiles> tilesToDelete)
		{
			foreach (var tile in tilesToDelete)
			{
				DataPrunedForFile(tile.tile_path);
			}
		}

		/// <summary>
		/// Returns the tile data, otherwise null
		/// </summary>
		/// <param name="tileId">Canonical tile id to identify the tile</param>
		/// <returns>tile data as byte[], if tile is not cached returns null</returns>
		
		private bool FetchSqliteEntry(CanonicalTileId tileId, string tilesetName, out tiles tile)
		{
			tile = null;

			try
			{
				int? tilesetId = getTilesetId(tilesetName);
				if (!tilesetId.HasValue)
				{
					return false;
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
				return false;
			}

			if (null == tile)
			{
				return false;
			}
			
			if (tile.expirationDate.HasValue)
			{
				tile.expirationDateFormatted = UnixTimestampUtils.From((double) tile.expirationDate.Value);
			}

			tile.timestamp = (int) UnixTimestampUtils.To(DateTime.Now);
			_sqlite.InsertOrReplace(tile);

			return true;
		}

		/// <summary>
		/// Check if tile exists
		/// </summary>
		/// <param name="tileId">Canonical tile id</param>
		/// <returns>True if tile exists</returns>
		private bool TileExists(string tilesetName, CanonicalTileId tileId)
		{
			lock (_lock)
			{
				var tilesetId = getTilesetId(tilesetName);
				var query = "SELECT EXISTS(SELECT 1 " +
				            "FROM tiles " +
				            "WHERE tile_set    = ?1 " +
				            "  AND zoom_level  = ?2 " +
				            "  AND tile_column = ?3 " +
				            "  AND tile_row    = ?4 " +
				            "LIMIT 1)";
				var countCommand = _sqlite.CreateCommand(query,
					tilesetId,
					tileId.Z,
					tileId.X,
					tileId.Y);
				var count = countCommand.ExecuteScalar<int>();

				return count > 0;
			}
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

		#region OfflineCache

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
		#endregion
	}

	[Table("sqlite_master")]
	public class SqliteSchemaObject
	{
		public string type { get; set; }
		public string name { get; set; }
		public string tbl_name { get; set; }
		public int rootpage { get; set; }
		public string sql { get; set; }
	}
}