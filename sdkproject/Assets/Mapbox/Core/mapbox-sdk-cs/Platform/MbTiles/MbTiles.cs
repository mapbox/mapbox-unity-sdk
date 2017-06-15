using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite4Unity3d;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Platform.MbTiles
{
	public class MbTilesDb : IDisposable
	{


		public struct CacheKey
		{
			public string tileset;
			public int zoom;
			public long x;
			public long y;
			public override string ToString()
			{
				return string.Format("tileset:{0} z:{1} x:{2} y:{3} - {4}", tileset, zoom, x, y, DateTime.Now.Ticks);
			}
		}


		private bool _disposed;
		private SQLiteConnection _sqlite;
		private int? _maxTileCount;
		/// <summary>check cache size only every '_pruneCacheDelta' calls to 'Add()' to avoid being too chatty with the database</summary>
		private const int _pruneCacheDelta = 10;
		/// <summary>counter to keep track of calls to `Add()`</summary>
		private int _pruneCacheCounter = 0;

		public MbTilesDb(string tileset, int? maxTileCount = null)
		{

			openOrCreateDb(tileset);
			_maxTileCount = maxTileCount;

			//hrmpf: multiple PKs not supported by sqlite.net
			//https://github.com/praeclarum/sqlite-net/issues/282
			//TODO: do it via plain SQL

			List<SQLite4Unity3d.SQLiteConnection.ColumnInfo> colInfo = _sqlite.GetTableInfo(typeof(tiles).Name);
			if (0 == colInfo.Count)
			{
				UnityEngine.Debug.LogFormat("creating table '{0}'", typeof(tiles).Name);
				//sqlite does not support multiple PK columns, create table manually
				//_sqlite.CreateTable<tiles>();

				//
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

			//speed things up a bit :-)
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
						UnityEngine.Debug.Log("------------------ VACUUMING ----------------------");
						_sqlite.Execute("VACUUM;");
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
#if UNITY_EDITOR
			var dbPath = string.Format(@"Assets/StreamingAssets/{0}", dbName);
#else
			// check if file exists in Application.persistentDataPath
			var filepath = string.Format("{0}/{1}", Application.persistentDataPath, dbName);

			Debug.LogFormat("filepath: {0}", filepath);

			if (!File.Exists(filepath))
			{
				Debug.Log("Database not in Persistent path");
				// if it doesn't ->
				// open StreamingAssets directory and load the db ->

#if UNITY_ANDROID
				var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + dbName);  // this is the path to your StreamingAssets in android
				while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
				// then save to Application.persistentDataPath
				File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
				var loadDb = Application.dataPath + "/Raw/" + dbName;  // this is the path to your StreamingAssets in iOS
				// then save to Application.persistentDataPath
				File.Copy(loadDb, filepath);
#elif UNITY_WP8
				var loadDb = Application.dataPath + "/StreamingAssets/" + dbName;  // this is the path to your StreamingAssets in iOS
				// then save to Application.persistentDataPath
				File.Copy(loadDb, filepath);

#elif UNITY_WINRT
				var loadDb = Application.dataPath + "/StreamingAssets/" + dbName;  // this is the path to your StreamingAssets in iOS
				// then save to Application.persistentDataPath
				if (File.Exists(loadDb))
				{
					File.Copy(loadDb, filepath);
				}
#else
				var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
																						 // then save to Application.persistentDataPath
				// only copy if db exists
				if (File.Exists(loadDb))
				{
					File.Copy(loadDb, filepath);
				}
				Debug.LogErrorFormat("loadDb: {0}", loadDb);
				Debug.LogErrorFormat("filepath: {0}", filepath);
#endif

				Debug.Log("Database written");
			}

			var dbPath = filepath;
#endif
			_sqlite = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
			Debug.Log("Final PATH: " + dbPath);

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

			UnityEngine.Debug.LogFormat("creating table '{0}'", typeof(metadata).Name);
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


		public void AddTile(CacheKey key, byte[] data)
		{
			_sqlite.Insert(new tiles
			{
				zoom_level = key.zoom,
				tile_column = key.x,
				tile_row = key.y,
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

			UnityEngine.Debug.LogFormat("pruning cache, maxTileCnt:{0} tileCnt:{1} 2del:{2} -{3}"
				, _maxTileCount
				, tileCnt
				, toDelete
				, DateTime.Now.Ticks
			);

			// no 'ORDER BY' or 'LIMIT' possible if sqlite hasn't been compiled with 'SQLITE_ENABLE_UPDATE_DELETE_LIMIT'
			// https://sqlite.org/compile.html#enable_update_delete_limit
			// int rowsAffected = _sqlite.Execute("DELETE FROM tiles ORDER BY timestamp ASC LIMIT ?", toDelete);
			int rowsAffected = _sqlite.Execute("DELETE FROM tiles WHERE rowid IN ( SELECT rowid FROM tiles ORDER BY timestamp ASC LIMIT ? );", toDelete);
			UnityEngine.Debug.LogFormat("tiles deleted:{0} -{1}", rowsAffected, DateTime.Now.Ticks);
		}


		public byte[] GetTile(CacheKey key)
		{
			tiles tile = _sqlite
				.Table<tiles>()
				.Where(t => t.zoom_level == key.zoom && t.tile_column == key.x && t.tile_row == key.y)
				.FirstOrDefault();

			if (null == tile)
			{
				//UnityEngine.Debug.LogWarningFormat("{0} not yet cached", key);
				return null;
			}
			else
			{
				return tile.tile_data;
			}
		}


		public bool TileExists(CacheKey key)
		{
			return null != _sqlite
				.Table<tiles>()
				.Where(t => t.zoom_level == key.zoom && t.tile_column == key.x && t.tile_row == key.y)
				.FirstOrDefault();
		}

	}
}
