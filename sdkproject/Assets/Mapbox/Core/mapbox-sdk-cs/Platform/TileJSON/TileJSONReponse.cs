namespace Mapbox.Platform.TilesetTileJSON
{

	using Mapbox.Json;



	public class TileJSONResponse
	{


		[JsonProperty("attribution")]
		public string Attribution { get; set; }


		[JsonProperty("autoscale")]
		public bool AutoScale { get; set; }


		[JsonProperty("bounds")]
		public double[] Bounds { get; set; }


		[JsonProperty("center")]
		public double[] Center { get; set; }


		[JsonProperty("created")]
		public long Created { get; set; }


		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }


		[JsonProperty("maxzoom")]
		public int MaxZoom { get; set; }


		[JsonProperty("minzoom")]
		public int MinZoom { get; set; }


		[JsonProperty("modified")]
		public long? Modified { get; set; }


		[JsonProperty("name")]
		public string Name { get; set; }


		[JsonProperty("private")]
		public bool Private { get; set; }


		[JsonProperty("scheme")]
		public string Scheme { get; set; }


		[JsonProperty("tilejson")]
		public string TileJSONVersion { get; set; }


		[JsonProperty("tiles")]
		public string[] Tiles { get; set; }


		[JsonProperty("vector_layers")]
		public TileJSONObjectVectorLayer[] VectorLayers { get; set; }


		[JsonProperty("webpage")]
		public string WebPage { get; set; }


	}
}
