//-----------------------------------------------------------------------
// <copyright file="DirectionsResponse.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions {
	using System;
	using System.Collections.Generic;
	using Mapbox.Json;

	/// <summary>
	/// Directions response.
	/// </summary>
#if !WINDOWS_UWP
	// http://stackoverflow.com/a/12903628
	[Serializable]
#endif
	public class DirectionsResponse {
		/// <summary>
		/// Gets or sets the routes.
		/// </summary>
		/// <value>The routes.</value>
		[JsonProperty("routes")]
		public List<Route> Routes { get; set; }

		/// <summary>
		/// Gets or sets the waypoints.
		/// </summary>
		/// <value>The waypoints.</value>
		[JsonProperty("waypoints")]
		public List<Waypoint> Waypoints { get; set; }

		/// <summary>
		/// Gets or sets the code.
		/// </summary>
		/// <value>The code.</value>
		[JsonProperty("code")]
		public string Code { get; set; }
	}
}