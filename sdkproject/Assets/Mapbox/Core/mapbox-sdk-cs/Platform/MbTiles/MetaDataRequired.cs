using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mapbox.Platform.MbTiles
{

	/// <summary>
	/// https://github.com/mapbox/mbtiles-spec/blob/master/1.1/spec.md#content
	/// </summary>
	public class MetaDataRequired
	{

		public string TilesetName { get; set; }
		/// <summary>overlay or baselayer</summary>
		public string Type { get; set; }

		public int Version { get; set; }

		public string Description { get; set; }

		public string Format { get; set; }



	}
}
