//-----------------------------------------------------------------------
// <copyright file="CongestionLevel.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.DirectionsApi
{
	/// <summary>
	///     A bucketed traffic congestion level, derived from a <see cref="Response.Annotation.CongestionNumeric"/>
	///     value (0 = no congestion, 100 = maximum; null = unknown).
	/// </summary>
	public enum CongestionLevel
	{
		/// <summary> No numeric value is available for this segment. </summary>
		Unknown,

		/// <summary> Free-flowing traffic. Numeric value 0-39. </summary>
		Low,

		/// <summary> Numeric value 40-59. </summary>
		Moderate,

		/// <summary> Numeric value 60-79. </summary>
		Heavy,

		/// <summary> Near-standstill traffic. Numeric value 80-100. </summary>
		Severe
	}

	/// <summary> Bucketing helpers for turning a numeric congestion annotation into a <see cref="CongestionLevel"/>. </summary>
	public static class CongestionLevelUtility
	{
		/// <summary>
		///     Buckets a <c>congestion_numeric</c> value (0-100, or null for unknown) into a <see cref="CongestionLevel"/>.
		///     Thresholds match the Mapbox Navigation SDK defaults: 0-39 low, 40-59 moderate, 60-79 heavy, 80-100 severe.
		/// </summary>
		public static CongestionLevel LevelForNumeric(int? value)
		{
			if (value == null) return CongestionLevel.Unknown;

			var v = value.Value;
			if (v < 40) return CongestionLevel.Low;
			if (v < 60) return CongestionLevel.Moderate;
			if (v < 80) return CongestionLevel.Heavy;
			return CongestionLevel.Severe;
		}
	}
}
