using SQLite4Unity3d;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite4Unity3d;

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
			TableQuery<Tile> tblTiles = _sqlite.Table<Tile>();

			//HACK: commented condition to create table on first run, find a nicer way to do it
			if (null == tblTiles)
			{
				//hrmpf: multiple PKs not supported by sqlite.net
				//https://github.com/praeclarum/sqlite-net/issues/282
				//TODO: do it via plain SQL
				_sqlite.CreateTable<Tile>();
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
			TableQuery<MetaData> tq = _sqlite.Table<MetaData>();
			// already exists -> return
			if (null != tq) { return; }

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
			_sqlite.Insert(new Tile
			{
				zoom_level = key.zoom,
				tile_column = key.x,
				tile_row = key.y,
				tile_data = data
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
				.Table<Tile>()
				.Where(t => t.zoom_level == key.zoom && t.tile_column == key.x && t.tile_row == key.y)
				.FirstOrDefault();
		}

	}
}
