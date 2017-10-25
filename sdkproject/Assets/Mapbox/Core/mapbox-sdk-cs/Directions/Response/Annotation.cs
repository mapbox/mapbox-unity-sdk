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
	/// <para>An annotations object contains additional details about each line segment along the route geometry.</para>
	/// <para></para>Each entry in an annotations field corresponds to a coordinate along the route geometry.
	/// </summary>
	public class Annotation
	{


		[JsonProperty("distance")]
		public double[] Distance { get; set; }


		[JsonProperty("duration")]
		public double[] Duration { get; set; }


		[JsonProperty("speed")]
		public string[] Speed { get; set; }


		[JsonProperty("congestion")]
		public string[] Congestion { get; set; }



	}
}
