using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQLite4Unity3d;

namespace Mapbox.Platform.MbTiles
{

	/// <summary>
	/// https://github.com/mapbox/mbtiles-spec/blob/master/1.1/spec.md#tiles
	/// </summary>
	public class Tile
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


	}
}