//-----------------------------------------------------------------------
// <copyright file="Step.cs" company="Mapbox">
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
    /// A step from a Directions API call.
    /// </summary>
    public class Step
	{
		/// <summary>
		/// Gets or sets the intersections.
		/// </summary>
		/// <value>The intersections.</value>
		[JsonProperty("intersections")]
		public List<Intersection> Intersections { get; set; }

		/// <summary>
		/// Gets or sets the geometry.
		/// </summary>
		/// <value>The geometry.</value>
		[JsonProperty("geometry")]
		[JsonConverter(typeof(PolylineToVector2dListConverter))]
		public List<Vector2d> Geometry { get; set; }

		/// <summary>
		/// Gets or sets the maneuver.
		/// </summary>
		/// <value>The maneuver.</value>
		[JsonProperty("maneuver")]
		public Maneuver Maneuver { get; set; }

		/// <summary>
		/// Gets or sets the duration.
		/// </summary>
		/// <value>The duration.</value>
		[JsonProperty("duration")]
		public double Duration { get; set; }

		/// <summary>
		/// Gets or sets the distance.
		/// </summary>
		/// <value>The distance.</value>
		[JsonProperty("distance")]
		public double Distance { get; set; }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the mode.
		/// </summary>
		/// <value>The mode.</value>
		[JsonProperty("mode")]
		public string Mode { get; set; }
	}
}
