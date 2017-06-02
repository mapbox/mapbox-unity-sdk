using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mapbox.Platform.MbTiles
{

	/// <summary>
	/// https://github.com/mapbox/mbtiles-spec/blob/master/1.1/spec.md#metadata
	/// </summary>
	public class MetaData 
	{

		public string name { get; set; }

		public string value { get; set; }
	}

}