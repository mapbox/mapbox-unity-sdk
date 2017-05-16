//-----------------------------------------------------------------------
// <copyright file="Overview.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
	/// <summary> 
	///     Type of returned overview geometry. Can be  full (the most detailed geometry available),  
	///     simplified (a simplified version of the full geometry), or  false (no overview geometry).  
	/// </summary>
	public sealed class Overview
	{
		/// <summary> Use the most detailed geometry available. </summary>
		public static readonly Overview Full = new Overview("full");

		/// <summary> Use simplified geometry. This is the default value. </summary>
		public static readonly Overview Simplified = new Overview("simplified");

		/// <summary> Use no overview geometry. </summary>
		public static readonly Overview False = new Overview("false");

		private readonly string overview;

		private Overview(string overview)
		{
			this.overview = overview;
		}

		/// <summary> Converts the overview type to a string. </summary>
		/// <returns> A string to use as an optional value in the direction query URL. </returns>
		public override string ToString()
		{
			return this.overview;
		}
	}
}
