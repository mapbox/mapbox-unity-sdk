//-----------------------------------------------------------------------
// <copyright file="Leg.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
	using System.Collections.Generic;
	using Mapbox.Json;


	/// <summary>
	/// A Leg from a Directions API call.
	/// </summary>
	public class Leg
	{


		/// <summary>
		/// Depending on the steps parameter, either an Array of RouteStep objects (true, default) or an empty array (false)
		/// </summary>
		/// <value>The steps.</value>
		[JsonProperty("steps")]
		public List<Step> Steps { get; set; }


		/// <summary>
		/// Depending on the summary parameter, either a String summarizing the route (true, default) or an empty String (false).
		/// </summary>
		/// <value>The summary.</value>
		[JsonProperty("summary")]
		public string Summary { get; set; }


		/// <summary>
		/// Number indicating the estimated travel time in seconds.
		/// </summary>
		[JsonProperty("duration")]
		public double Duration { get; set; }


		/// <summary>
		/// Number indicating the distance traveled in meters.
		/// </summary>
		[JsonProperty("distance")]
		public double Distance { get; set; }


		/// <summary>
		/// An annotations object that contains additional details about each line segment along the route geometry. Each entry in an annotations field corresponds to a coordinate along the route geometry.
		/// </summary>
		[JsonProperty("annotation")]
		public Annotation Annotation { get; set; }


	}
}
