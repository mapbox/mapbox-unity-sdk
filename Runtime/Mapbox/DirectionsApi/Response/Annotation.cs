//-----------------------------------------------------------------------
// <copyright file="Leg.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	/// <summary>
	/// <para>An annotations object contains additional details about each line segment along the route geometry.</para>
	/// <para></para>Each entry in an annotations field corresponds to a coordinate along the route geometry.
	/// </summary>
	[Serializable]
	public class Annotation
	{
		[JsonProperty("distance")] 
		public double[] Distance;
		
		[JsonProperty("duration")] 
		public double[] Duration;
		
		[JsonProperty("speed")] 
		public string[] Speed;
		
		[JsonProperty("congestion")]
		public string[] Congestion;

		/// <summary>
		/// The numeric level of congestion between each pair of coordinates, from 0 (no congestion) to 100
		/// (maximum congestion). Entries are null where congestion is unknown. Only populated for the
		/// driving-traffic profile when <c>congestion_numeric</c> is requested.
		/// </summary>
		[JsonProperty("congestion_numeric")]
		public int?[] CongestionNumeric;

		/// <summary>
		/// The posted speed limit between each pair of coordinates, one <see cref="Response.MaxSpeed"/> per segment:
		/// a known limit with its unit, an unknown limit, or no legal limit. Only populated for the driving and
		/// driving-traffic profiles when <c>maxspeed</c> is requested.
		/// </summary>
		[JsonProperty("maxspeed")]
		public MaxSpeed[] MaxSpeed;
	}
}
