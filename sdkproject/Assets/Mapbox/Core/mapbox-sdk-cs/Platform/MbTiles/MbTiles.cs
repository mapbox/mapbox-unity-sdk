using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite4Unity3d;
using Mapbox.Utils;
using UnityEngine;
using Mapbox.Map;


namespace Mapbox.Platform.MbTiles
{


	public class MbTilesDb : IDisposable
	{


		private bool _disposed;
		private string _dbPath;
		private SQLiteConnection _sqlite;
		private uint? _maxTileCount;
		/// <summary>check cache size only every '_pruneCacheDelta' calls to 'Add()' to avoid being too chatty with the database</summary>
		private const int _pruneCacheDelta = 20;
		/// <summary>counter to keep track of calls to `Add()`</summary>
		private int _pruneCacheCounter = 0;


		public MbTilesDb(string tileset, uint? maxTileCount = null)
		{

			openOrCreateDb(tileset);
			_maxTileCount = maxTileCount;

			//hrmpf: multiple PKs not supported by sqlite.net
			//https://github.com/praeclarum/sqlite-net/issues/282
			//TODO: do it via plain SQL

			List<SQLite4Unity3d.SQLiteConnection.ColumnInfo> colInfo = _sqlite.GetTableInfo(typeof(tiles).Name);
			if (0 == colInfo.Count)
			{
				//sqlite does not support multiple PK columns, create table manually
				//_sqlite.CreateTable<tiles>();

				string cmdCreateTTbliles = @"CREATE TABLE tiles(
zoom_level  INTEGER NOT NULL,
tile_column BIGINT  NOT NULL,
tile_row    BIGINT  NOT NULL,
tile_data   BLOB    NOT NULL,
timestamp   INTEGER NOT NULL,
	PRIMARY KEY(
		zoom_level ASC,
		tile_column ASC,
		tile_row ASC
	)
);";
				_sqlite.Execute(cmdCreateTTbliles);

				string cmdIdxTimestamp = "CREATE INDEX idx_timestamp ON tiles (timestamp ASC);";
				_sqlite.Execute(cmdIdxTimestamp);
			}

			//some pragmas to speed things up a bit :-)
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


		#region idisposable


		~MbTilesDb()
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
						_sqlite.Dispose();
						_sqlite = null;
					}
				}
				_disposed = true;
			}
		}


		#endregion


		private void openOrCreateDb(string dbName)
		{
			_dbPath = Path.Combine(Application.persistentDataPath, "cache");
			if (!Directory.Exists(_dbPath)) { Directory.CreateDirectory(_dbPath); }
			_dbPath = Path.Combine(_dbPath, dbName);
			_sqlite = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
			//Debug.LogFormat("MBTiles path ----> {0}", _dbPath);
		}




		public System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<string, string>> MetaData()
		{
			TableQuery<metadata> tq = _sqlite.Table<metadata>();
			if (null == tq) { return null; }
			return tq.Select(r => new KeyValuePair<string, string>(r.name, r.value)).ToList().AsReadOnly();
		}


		public void CreateMetaData(MetaDataRequired md)
		{
			List<SQLite4Unity3d.SQLiteConnection.ColumnInfo> colInfo = _sqlite.GetTableInfo(typeof(metadata).Name);
			if (0 != colInfo.Count) { return; }

			//UnityEngine.Debug.LogFormat("creating table '{0}'", typeof(metadata).Name);
			_sqlite.CreateTable<metadata>();
			_sqlite.InsertAll(new[]
			{
				new metadata{ name="name", value=md.TilesetName},
				new metadata{name="type", value=md.Type},
				new metadata{name="version", value=md.Version.ToString()},
				new metadata{name="description", value=md.Description},
				new metadata{name="format", value=md.Format}
			});
		}


		public void AddTile(CanonicalTileId tileId, byte[] data)
		{
			_sqlite.Insert(new tiles
			{
				zoom_level = tileId.Z,
				tile_column = tileId.X,
				tile_row = tileId.Y,
				tile_data = data,
				timestamp = (int)UnixTimestampUtils.To(DateTime.Now)
			});

			_pruneCacheCounter++;
			if (0 == _pruneCacheCounter % _pruneCacheDelta)
			{
				_pruneCacheCounter = 0;
				prune();
			}
		}


		private void prune()
		{
			if (!_maxTileCount.HasValue) { return; }

			long tileCnt = _sqlite.ExecuteScalar<long>("SELECT COUNT(zoom_level) FROM tiles");

			if (tileCnt < _maxTileCount.Value) { return; }

			long toDelete = tileCnt - _maxTileCount.Value;

			// no 'ORDER BY' or 'LIMIT' possible if sqlite hasn't been compiled with 'SQLITE_ENABLE_UPDATE_DELETE_LIMIT'
			// https://sqlite.org/compile.html#enable_update_delete_limit
			// int rowsAffected = _sqlite.Execute("DELETE FROM tiles ORDER BY timestamp ASC LIMIT ?", toDelete);
			_sqlite.Execute("DELETE FROM tiles WHERE rowid IN ( SELECT rowid FROM tiles ORDER BY timestamp ASC LIMIT ? );", toDelete);
		}


		/// <summary>
		/// Returns the tile data, otherwise null
		/// </summary>
		/// <param name="tileId">Canonical tile id to identify the tile</param>
		/// <returns>tile data as byte[], if tile is not cached returns null</returns>
		public byte[] GetTile(CanonicalTileId tileId)
		{
			tiles tile = _sqlite
				.Table<tiles>()
				.Where(t => t.zoom_level == tileId.Z && t.tile_column == tileId.X && t.tile_row == tileId.Y)
				.FirstOrDefault();

			if (null == tile)
			{
				return null;
			}

			return tile.tile_data;
		}


		/// <summary>
		/// Check if tile exists
		/// </summary>
		/// <param name="tileId">Canonical tile id</param>
		/// <returns>True if tile exists</returns>
		public bool TileExists(CanonicalTileId tileId)
		{
			return null != _sqlite
				.Table<tiles>()
				.Where(t => t.zoom_level == tileId.Z && t.tile_column == tileId.X && t.tile_row == tileId.Y)
				.FirstOrDefault();
		}


		/// <summary>
		/// Delete the database file
		/// </summary>
		public void Delete()
		{
			//already disposed
			if (null == _sqlite) { return; }

			_sqlite.Close();
			_sqlite.Dispose();
			_sqlite = null;

			UnityEngine.Debug.LogFormat("deleting {0}", _dbPath);

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

	}
}
