//-----------------------------------------------------------------------
// <copyright file="Route.cs" company="Mapbox">
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
    /// A Route from a Directions API call.
    /// </summary>
    public class Route
	{
		/// <summary>
		/// Gets or sets the legs.
		/// </summary>
		/// <value>The legs.</value>
		[JsonProperty("legs")]
		public List<Leg> Legs { get; set; }

		/// <summary>
		/// Gets or sets the geometry. Polyline is an array of LatLng's.
		/// </summary>
		/// <value>The geometry.</value>
		[JsonProperty("geometry")]
		[JsonConverter(typeof(PolylineToVector2dListConverter))]
		public List<Vector2d> Geometry { get; set; }

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
		/// Float indicating the weight in units described by 'weight_name'.
		/// </summary>
		[JsonProperty("weight")]
		public float Weight { get; set; }

		/// <summary>
		/// String indicating which weight was used. The default is routability which is duration based, with additional penalties for less desirable maneuvers.
		/// </summary>
		[JsonProperty("weight_name")]
		public string WeightName { get; set; }

	}
}