//-----------------------------------------------------------------------
// <copyright file="Tracepoint.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapMatching
{
	using Mapbox.Directions;
	using Mapbox.Json;
    using Mapbox.Utils;
    using Mapbox.Utils.JsonConverters;

    /// <summary>
    /// A Waypoint from a Directions API call.
    /// </summary>
    public class Tracepoint: Waypoint
	{
		/// <summary>
		///  Index of the waypoint inside the matched route.
		/// </summary>
		[JsonProperty("waypoint_index")]
		public int WaypointIndex { get; set; }

		/// <summary>
		/// Index to the match object in matchings the sub-trace was matched to.
		/// </summary>
		[JsonProperty("matchings_index")]
		public int MatchingsIndex { get; set; }

		/// <summary>
		/// Number of probable alternative matchings for this trace point. A value of zero indicates that this point was matched unambiguously. Split the trace at these points for incremental map matching.
		/// </summary>
		[JsonProperty("alternatives_count")]
		public int AlternativesCount { get; set; }


	}
}
