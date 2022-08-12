using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQLite4Unity3d;

namespace Mapbox.Platform.Cache
{

	/// <summary>
	/// Don't change the class name: sqlite-net uses it for table creation
	/// </summary>
	public class tiles
	{
		[PrimaryKey, AutoIncrement]
		public int id { get; set; }

		public int tile_set { get; set; }

		//hrmpf: multiple PKs not supported by sqlite.net
		//https://github.com/praeclarum/sqlite-net/issues/282
		//TODO: do it via plain SQL
		//[PrimaryKey]
		public int zoom_level { get; set; }

		//[PrimaryKey]
		public long tile_column { get; set; }

		//[PrimaryKey]
		public long tile_row { get; set; }

		public byte[] tile_data { get; set; }

		/// <summary>Unix epoch for simple FIFO pruning </summary>
		public int timestamp { get; set; }

		/// <summary> ETag Header value of the reponse for auto updating cache</summary>
		public string etag { get; set; }

		/// <summary>Last time entry accessed </summary>
		public int accessed { get; set; }

		/// <summary>datetime when tile forced to update </summary>
		public int expirationdate { get; set; }
	}

	public class offlineMaps
	{
		[PrimaryKey, AutoIncrement]
		public int id { get; set; }
		public string name { get; set; }
	}

	public class tile2offline
	{
		public int tileId { get; set; }
		public int mapId { get; set; }
	}
}
