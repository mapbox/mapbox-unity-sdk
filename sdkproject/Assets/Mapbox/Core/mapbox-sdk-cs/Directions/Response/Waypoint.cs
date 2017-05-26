//-----------------------------------------------------------------------
// <copyright file="Waypoint.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
    using Mapbox.Json;
    using Mapbox.Utils;
    using Mapbox.Utils.JsonConverters;

    /// <summary>
    /// A Waypoint from a Directions API call.
    /// </summary>
    public class Waypoint
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the location.
		/// </summary>
		/// <value>The location.</value>
		[JsonProperty("location")]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Location { get; set; }
	}
}
