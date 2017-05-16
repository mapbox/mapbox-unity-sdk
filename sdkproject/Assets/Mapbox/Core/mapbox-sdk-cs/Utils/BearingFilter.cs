//-----------------------------------------------------------------------
// <copyright file="BearingFilter.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	using System;

	/// <summary> 
	///     Represents a bearing filter, composed of a bearing in decimal angular degrees, with a +/- range 
	///     also in angular degrees. 
	/// </summary>
	public struct BearingFilter
	{
		/// <summary> A decimal degree between 0 and 360. </summary>
		public double? Bearing;

		/// <summary> 
		///     A decimal degree between 0 and 180. Represents the range  
		///     beyond bearing in both directions. 
		/// </summary>
		public double? Range;

		/// <summary> Initializes a new instance of the <see cref="BearingFilter" /> struct. </summary>
		/// <param name="bearing"> A decimal degree between 0 and 360, or null. </param>
		/// <param name="range"> A decimal degree between 0 and 180, or null. </param>
		public BearingFilter(double? bearing, double? range)
		{
			if (bearing != null && (bearing > 360 || bearing < 0))
			{
				throw new Exception("Bearing must be greater than 0 and less than 360.");
			}

			if (bearing != null && (range > 180 || range < 0))
			{
				throw new Exception("Range must be greater than 0 and less than 180.");
			}

			this.Bearing = bearing;
			this.Range = range;
		}

		/// <summary> Converts bearing to a URL snippet. </summary>
		/// <returns> Returns a string for use in a Mapbox query URL. </returns>
		public override string ToString()
		{
			if (this.Bearing != null && this.Range != null)
			{
				return this.Bearing.ToString() + "," + this.Range.ToString();
			}
			else
			{
				return string.Empty;
			}
		}
	}
}
