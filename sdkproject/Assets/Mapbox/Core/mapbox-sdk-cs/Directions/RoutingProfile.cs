//-----------------------------------------------------------------------
// <copyright file="RoutingProfile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
	/// <summary>
	///     Routing profile, affects how the route is calculated, prioritizing routes that fit
	///     the profile the best.
	/// </summary>
	public sealed class RoutingProfile
	{
		/// <summary> The driving profile. </summary>
		public static readonly RoutingProfile Driving = new RoutingProfile("mapbox/driving/");

		/// <summary> The walking profile. </summary>
		public static readonly RoutingProfile Walking = new RoutingProfile("mapbox/walking/");

		/// <summary> The cycling profile. </summary>
		public static readonly RoutingProfile Cycling = new RoutingProfile("mapbox/cycling/");

		private readonly string profile;

		private RoutingProfile(string profile)
		{
			this.profile = profile;
		}

		/// <summary> Converts the profile to a URL snippet. </summary>
		/// <returns> A string to be appened to the direction query URL. </returns>
		public override string ToString()
		{
			return this.profile;
		}
	}
}
