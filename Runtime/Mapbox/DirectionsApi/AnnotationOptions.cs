//-----------------------------------------------------------------------
// <copyright file="AnnotationOptions.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mapbox.DirectionsApi
{
	/// <summary>
	///     Additional metadata to return along the route, as per-segment annotation arrays.
	///     Combine values with the bitwise OR operator. Requesting annotations requires the
	///     route to be requested with <see cref="Overview.Full"/>.
	///     <para><see cref="Congestion"/> and <see cref="CongestionNumeric"/> are only
	///     available for the <c>mapbox/driving-traffic</c> profile
	///     (<see cref="RoutingProfile.DrivingTraffic"/>).</para>
	/// </summary>
	[Flags]
	public enum AnnotationOptions
	{
		/// <summary> No annotations. </summary>
		None = 0,

		/// <summary> Distance between each pair of coordinates, in meters. </summary>
		Distance = 1 << 0,

		/// <summary> Duration between each pair of coordinates, in seconds. </summary>
		Duration = 1 << 1,

		/// <summary> Speed between each pair of coordinates, in meters per second. </summary>
		Speed = 1 << 2,

		/// <summary> Level of congestion between each pair of coordinates. Driving-traffic profile only. </summary>
		Congestion = 1 << 3,

		/// <summary> Numeric level of congestion between each pair of coordinates. Driving-traffic profile only. </summary>
		CongestionNumeric = 1 << 4,

		/// <summary> Maximum speed limit between each pair of coordinates. Driving and driving-traffic profiles only. </summary>
		MaxSpeed = 1 << 5
	}

	/// <summary> Helpers for turning <see cref="AnnotationOptions"/> into a query-string value. </summary>
	public static class AnnotationOptionsExtensions
	{
		/// <summary>
		///     Converts the set flags to the comma-separated value expected by the Directions API
		///     (for example <c>congestion,speed</c>). Returns an empty string for <see cref="AnnotationOptions.None"/>.
		/// </summary>
		public static string ToQueryString(this AnnotationOptions options)
		{
			var parts = new List<string>();

			if (options.HasFlag(AnnotationOptions.Distance)) parts.Add("distance");
			if (options.HasFlag(AnnotationOptions.Duration)) parts.Add("duration");
			if (options.HasFlag(AnnotationOptions.Speed)) parts.Add("speed");
			if (options.HasFlag(AnnotationOptions.Congestion)) parts.Add("congestion");
			if (options.HasFlag(AnnotationOptions.CongestionNumeric)) parts.Add("congestion_numeric");
			if (options.HasFlag(AnnotationOptions.MaxSpeed)) parts.Add("maxspeed");

			return string.Join(",", parts);
		}
	}
}
