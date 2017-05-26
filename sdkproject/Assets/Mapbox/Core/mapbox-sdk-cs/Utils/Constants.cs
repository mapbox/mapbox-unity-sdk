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

		/// <summary> Mercator projection max latitude limit. </summary>
		public const double LatitudeMax = 85.0511;

		/// <summary> Mercator projection max longitude limit. </summary>
		public const double LongitudeMax = 180;
	}
}