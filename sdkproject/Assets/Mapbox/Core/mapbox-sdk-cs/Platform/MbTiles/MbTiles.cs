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


		private bool _disposed;
		private SQLite.SQLiteDataService _sqlite;

		public MbTiles(string tileset)
		{
			_sqlite = new SQLite.SQLiteDataService(tileset);
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

	}
}
