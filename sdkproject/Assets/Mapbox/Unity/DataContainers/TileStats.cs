namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Json;

	public class TileStats
	{
		[JsonProperty("account")]
		public string account;

		[JsonProperty("tilesetid")]
		public string tilesetid;

		[JsonProperty("layers")]
		public LayerStats[] layers;
	}

	public class LayerStats
	{
		[JsonProperty("account")]
		public string account;

		[JsonProperty("tilesetid")]
		public string tilesetid;

		[JsonProperty("layer")]
		public string layer;

		[JsonProperty("geometry")]
		public string geometry;

		[JsonProperty("count")]
		public string count;

		[JsonProperty("attributes")]
		public AttributeStats[] attributes;
	}

	public class AttributeStats
	{
		[JsonProperty("attribute")]
		public string attribute;

		[JsonProperty("type")]
		public string type;

		[JsonProperty("values")]
		public string[] values;
	}
}
