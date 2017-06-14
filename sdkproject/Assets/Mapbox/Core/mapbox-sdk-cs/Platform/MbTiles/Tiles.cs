using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQLite4Unity3d;

namespace Mapbox.Platform.MbTiles
{

	/// <summary>
	/// https://github.com/mapbox/mbtiles-spec/blob/master/1.1/spec.md#tiles
	/// Don't change the class name: sqlite-net uses it for table creation
	/// </summary>
	public class tiles
	{

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
	}
}