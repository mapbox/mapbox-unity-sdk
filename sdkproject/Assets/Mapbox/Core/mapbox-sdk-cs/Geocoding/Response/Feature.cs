//-----------------------------------------------------------------------
// <copyright file="Feature.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding {
    using System;
    using System.Collections.Generic;
    using Mapbox.Json;
    using Mapbox.Utils;
    using Mapbox.Utils.JsonConverters;

    /// <summary> A GeoJSON FeatureCollection of points returned by geocoding API.</summary>
#if !WINDOWS_UWP
    //http://stackoverflow.com/a/12903628
    [Serializable]
#endif
	public class Feature {
		/// <summary> Gets or sets the id. Ids are unique in the Mapbox geocoder. </summary>
		/// <value>The id.</value>
		[JsonProperty("id")]
		public string Id { get; set; }

		/// <summary> 
		///     Gets or sets feature type. One of country,  region,  postcode,  place,  locality, neighborhood,  address,  poi.
		/// </summary>
		/// <value>The type.</value>
		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		/// <value>The text.</value>
		[JsonProperty("text")]
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the name of the place.
		/// </summary>
		/// <value>The name of the place.</value>
		[JsonProperty("place_name")]
		public string PlaceName { get; set; }

		/// <summary>
		/// Gets or sets the relevance.
		/// </summary>
		/// <value>The relevance.</value>
		[JsonProperty("relevance")]
		public double Relevance { get; set; }

		/// <summary>
		/// Gets or sets the properties.
		/// </summary>
		/// <value>The properties.</value>
		[JsonProperty("properties")]
		public Dictionary<string, object> Properties { get; set; }

		/// <summary>
		/// Gets or sets the bbox.
		/// </summary>
		/// <value>The bbox.</value>
		[JsonProperty("bbox", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(BboxToVector2dBoundsConverter))]
		public Vector2dBounds? Bbox { get; set; }

		/// <summary>
		/// Gets or sets the center.
		/// </summary>
		/// <value>The center.</value>
		[JsonProperty("center")]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Center { get; set; }

		/// <summary>
		/// Gets or sets the geometry.
		/// </summary>
		/// <value>The geometry.</value>
		[JsonProperty("geometry")]
		public Geometry Geometry { get; set; }

		/// <summary>
		/// Gets or sets the address.
		/// </summary>
		[JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
		public string Address { get; set; }

		/// <summary>
		/// Gets or sets the context.
		/// </summary>
		/// <value>The context.</value>
		[JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
		public List<Dictionary<string, string>> Context { get; set; }
	}
}
