//-----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	/// <summary> Collection of constants used across the project. </summary>
	public static class Constants
	{
		/// <summary> Base URL for all the Mapbox APIs. </summary>
		public const string BaseAPI = "https://api.mapbox.com/";

		public const string EventsAPI = "https://events.mapbox.com/";

		/// <summary> Mercator projection max latitude limit. </summary>
		public const double LatitudeMax = 85.0511;

		/// <summary> Mercator projection max longitude limit. </summary>
		public const double LongitudeMax = 180;

		/// <summary> Mercator projection max meters</summary>
		public const double WebMercMax = 20037508.342789244;

		/// <summary> Epsilon to comapre floating point numbers</summary>
		public const float EpsilonFloatingPoint = 1E-05f;

	}
}