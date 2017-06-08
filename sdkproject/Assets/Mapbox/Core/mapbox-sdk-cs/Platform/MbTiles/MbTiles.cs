using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SQLite4Unity3d;
using Mapbox.Utils;

namespace Mapbox.Platform.MbTiles
{
	public class MbTiles : IDisposable
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
		private SQLite.SQLiteDataService _sqlite;

		public MbTiles(string tileset)
		{
			_sqlite = new SQLite.SQLiteDataService(tileset);


			//hrmpf: multiple PKs not supported by sqlite.net
			//https://github.com/praeclarum/sqlite-net/issues/282
			//TODO: do it via plain SQL

			List<SQLite4Unity3d.SQLiteConnection.ColumnInfo> colInfo = _sqlite.GetTableInfo(typeof(Tile).Name);
			if (0 == colInfo.Count)
			{
				UnityEngine.Debug.LogFormat("creating table '{0}'", typeof(Tile).Name);
				_sqlite.CreateTable<Tile>();
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


		~MbTiles()
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
						_sqlite.Dispose();
						_sqlite = null;
					}
				}
				_disposed = true;
			}
		}


		#endregion


		public System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<string, string>> MetaData()
		{
			TableQuery<MetaData> tq = _sqlite.Table<MetaData>();
			if (null == tq) { return null; }
			return tq.Select(r => new KeyValuePair<string, string>(r.name, r.value)).ToList().AsReadOnly();
		}


		public void CreateMetaData(MetaDataRequired md)
		{
			List<SQLite4Unity3d.SQLiteConnection.ColumnInfo> colInfo = _sqlite.GetTableInfo(typeof(MetaData).Name);
			if (0 != colInfo.Count) { return; }

			UnityEngine.Debug.LogFormat("creating table '{0}'", typeof(MetaData).Name);
			_sqlite.CreateTable<MetaData>();
			_sqlite.InsertAll(new[]
			{
				new MetaData{ name="name", value=md.TilesetName},
				new MetaData{name="type", value=md.Type},
				new MetaData{name="version", value=md.Version.ToString()},
				new MetaData{name="description", value=md.Description},
				new MetaData{name="format", value=md.Format}
			});
		}


		public void AddTile(CacheKey key, byte[] data)
		{
			byte[] compressed = Compression.Compress(data);

			UnityEngine.Debug.LogWarningFormat("{0} raw: {1}KB compressed:{2}", key, data.Length / 1024, compressed.Length / 1024);


			_sqlite.Insert(new Tile
			{
				zoom_level = key.zoom,
				tile_column = key.x,
				tile_row = key.y,
				tile_data = compressed
				//tile_data = data
			});
		}


		public byte[] GetTile(CacheKey key)
		{
			Tile tile = _sqlite
				.Table<Tile>()
				.Where(t => t.zoom_level == key.zoom && t.tile_column == key.x && t.tile_row == key.y)
				.FirstOrDefault();

			if (null == tile)
			{
				//UnityEngine.Debug.LogWarningFormat("{0} not yet cached", key);
				return null;
			}
			else
			{
				//UnityEngine.Debug.LogWarningFormat("{0} size: {1}KB", key, tile.tile_data.Length / 1024);
				//return tile.tile_data;
				return Compression.Decompress(tile.tile_data);
			}
		}


		public bool TileExists(CacheKey key)
		{
			return null != _sqlite
				.Table<Tile>()
				.Where(t => t.zoom_level == key.zoom && t.tile_column == key.x && t.tile_row == key.y)
				.FirstOrDefault();
		}

	}
}
