//-----------------------------------------------------------------------
// <copyright file="MatchObject.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapMatching
{
	using Mapbox.Directions;
	using Mapbox.Json;

	/// <summary>
	/// A Match object from a Map Matching API call.
	/// </summary>
	public class MatchObject : Route
	{
		/// <summary>
		///  A number between 0 (low) and 1 (high) indicating level of confidence in the returned match
		/// </summary>
		[JsonProperty("confidence")]
		public float Confidence { get; set; }


	}
}
