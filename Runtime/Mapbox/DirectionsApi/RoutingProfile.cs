//-----------------------------------------------------------------------
// <copyright file="RoutingProfile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.DirectionsApi
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

		/// <summary> The driving profile with live traffic conditions factored in. </summary>
		public static readonly RoutingProfile DrivingTraffic = new RoutingProfile("mapbox/driving-traffic/");

		private readonly string profile;

		private RoutingProfile(string profile)
		{
			this.profile = profile;
		}

		public static RoutingProfile GetProfile(RoutingProfileOptions option)
		{
			switch (option)
			{
				case RoutingProfileOptions.Driving: return RoutingProfile.Driving; break;
				case RoutingProfileOptions.Walking: return RoutingProfile.Walking; break;
				case RoutingProfileOptions.Cycling: return RoutingProfile.Cycling; break;
				case RoutingProfileOptions.DrivingTraffic: return RoutingProfile.DrivingTraffic; break;
			}

			return RoutingProfile.Driving;
		}

		/// <summary> Converts the profile to a URL snippet. </summary>
		/// <returns> A string to be appened to the direction query URL. </returns>
		public override string ToString()
		{
			return this.profile;
		}
		
		public enum RoutingProfileOptions
		{
			Driving,
			Walking,
			Cycling,
			DrivingTraffic
		}
	}
}
