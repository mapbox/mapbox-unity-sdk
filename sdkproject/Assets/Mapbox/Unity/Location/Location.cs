namespace Mapbox.Unity.Location
{
	using Mapbox.Utils;
	
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
		/// Heading represents a facing angle, generally between 0-359. Also need location services enabled via Input.location.Start()
		/// </summary>
		public float Heading;

		/// <summary>
		/// The heading in degrees relative to the magnetic North Pole.
		/// </summary>
		public float HeadingMagnetic;

		/// <summary>
		/// Accuracy of heading reading in degrees.
		/// </summary>
		public float HeadingAccuracy;

		/// <summary>
		/// Timestamp (in seconds since 1970) when location was last updated.
		/// </summary>
		public double Timestamp;

		/// <summary>
		/// Horizontal Accuracy of the location.
		/// </summary>
		public int Accuracy;

		/// <summary>
		/// Has the location changed since last update?
		/// </summary>
		public bool IsLocationUpdated;

		/// <summary>
		/// Has the heading changed since last update?
		/// </summary>
		public bool IsHeadingUpdated;
	}
}
