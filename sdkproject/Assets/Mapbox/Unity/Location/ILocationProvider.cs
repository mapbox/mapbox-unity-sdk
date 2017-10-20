namespace Mapbox.Unity.Location
{
	using System;
	using Mapbox.Utils;

	/// <summary>
	/// Implement ILocationProvider to send Heading and Location updates.
	/// </summary>
	public interface ILocationProvider
	{
		event Action<Location> OnLocationUpdated;
	}

	/// <summary>
	/// Location contains heading, latitude, longitude, accuracy and a timestamp.
	/// </summary>
	public struct Location
	{
		/// <summary>
		/// The location, as descibed by a <see cref="T:Mapbox.Utils.Vector2d"/>. 
		/// Location.x represents Latitude.
		/// Location.y represents Longitude.
		/// </summary>
		public Vector2d LatitudeLongitude;

		/// <summary>
		/// Heading represents a facing angle, generally between 0-359.
		/// </summary>
		public float Heading;

		/// <summary>
		/// Timestamp (in seconds since 1970) when location was last updated.
		/// </summary>
		public double Timestamp;

		/// <summary>
		/// Horizontal Accuracy of the location.
		/// </summary>
		public int Accuracy;
	}
}