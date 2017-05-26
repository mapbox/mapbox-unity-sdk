//-----------------------------------------------------------------------
// <copyright file="Intersection.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
    using System.Collections.Generic;
    using Mapbox.Json;
    using Mapbox.Utils;
    using Mapbox.Utils.JsonConverters;

    /// <summary>
    /// An Intersection from a Directions API call.
    /// </summary>
    public class Intersection
	{
		/// <summary>
		/// Gets or sets the out.
		/// </summary>
		/// <value>The out.</value>
		[JsonProperty("out", Order = 0)]
		public int Out { get; set; }

		/// <summary>
		/// Gets or sets the entry.
		/// </summary>
		/// <value>The entry.</value>
		[JsonProperty("entry", Order = 1)]
		public List<bool> Entry { get; set; }

		/// <summary>
		/// Gets or sets the bearings.
		/// </summary>
		/// <value>The bearings.</value>
		[JsonProperty("bearings", Order = 2)]
		public List<int> Bearings { get; set; }

		/// <summary>
		/// Gets or sets the location.
		/// </summary>
		/// <value>The location.</value>
		[JsonProperty("location", Order = 3)]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Location { get; set; }

		/// <summary>
		/// Gets or sets the in.
		/// </summary>
		/// <value>The in.</value>
		[JsonProperty("in", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
		public int? In { get; set; }
	}
}
