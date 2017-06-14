using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mapbox.Platform.MbTiles
{

	/// <summary>
	/// https://github.com/mapbox/mbtiles-spec/blob/master/1.1/spec.md#metadata
	/// Don't change the class name: sqlite-net uses it for table creation
	/// </summary>
	public class metadata 
	{

		public string name { get; set; }

		public string value { get; set; }
	}

}