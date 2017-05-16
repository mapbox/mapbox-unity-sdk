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
		/// Gets or sets the steps.
		/// </summary>
		/// <value>The steps.</value>
		[JsonProperty("steps")]
		public List<Step> Steps { get; set; }

		/// <summary>
		/// Gets or sets the summary.
		/// </summary>
		/// <value>The summary.</value>
		[JsonProperty("summary")]
		public string Summary { get; set; }

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
	}
}
