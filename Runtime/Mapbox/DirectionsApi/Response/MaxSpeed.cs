//-----------------------------------------------------------------------
// <copyright file="MaxSpeed.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	/// <summary>
	/// <para>The posted speed limit for one segment of the route, one entry per pair of coordinates in the
	/// <c>maxspeed</c> annotation. Only populated for the driving and driving-traffic profiles when
	/// <c>maxspeed</c> is requested.</para>
	/// <para>Each entry is exactly one of three shapes: a known limit (<see cref="Speed"/> and <see cref="Unit"/>
	/// set), an unknown limit (<see cref="Unknown"/> true), or a segment with no legal limit at all
	/// (<see cref="None"/> true). Check the flags before reading <see cref="Speed"/>, since both "unknown" and
	/// "none" leave it null and they mean different things.</para>
	/// </summary>
	[Serializable]
	public class MaxSpeed
	{
		/// <summary>
		/// The speed limit, in <see cref="Unit"/>. Null when the limit is unknown or when there is no limit.
		/// </summary>
		[JsonProperty("speed")]
		public double? Speed;

		/// <summary>
		/// The unit <see cref="Speed"/> is expressed in, as returned by the API: <c>km/h</c> or <c>mph</c>.
		/// Null when <see cref="Speed"/> is null.
		/// </summary>
		[JsonProperty("unit")]
		public string Unit;

		/// <summary>
		/// True when the speed limit for this segment is not known.
		/// </summary>
		[JsonProperty("unknown")]
		public bool Unknown;

		/// <summary>
		/// True when this segment has no legal speed limit (for example, parts of the German Autobahn).
		/// Distinct from <see cref="Unknown"/>: the absence of a limit is known.
		/// </summary>
		[JsonProperty("none")]
		public bool None;
	}
}
