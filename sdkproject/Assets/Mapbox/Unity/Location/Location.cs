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
		public float Accuracy;

		/// <summary>
		/// Is the location service currently initializing?
		/// </summary>
		public bool IsLocationServiceInitializing;

		/// <summary>
		/// Has the location service been enabled by the user?
		/// </summary>
		public bool IsLocationServiceEnabled;

		/// <summary>
		/// Has the location changed since last update?
		/// </summary>
		public bool IsLocationUpdated;

		/// <summary>
		/// Has the location been aquired via a GPS fix. 'Null' if not supported by the active location provider or GPS not enabled.
		/// </summary>
		public bool? HasGpsFix;

		/// <summary>
		/// How many satellites were in view when the location was acquired. 'Null' if not supported by the active location provider or GPS not enabled.
		/// </summary>
		public int? SatellitesInView;

		/// <summary>
		/// How many satellites were used for the location. 'Null' if not supported by the active location provider or GPS not enabled.
		/// </summary>
		public int? SatellitesUsed;

		/// <summary>
		/// Speed in [meters/second]. 'Null' if not supported by the active location provider.
		/// </summary>
		public float? SpeedMetersPerSecond;

		/// <summary>
		/// Speed in [km/h]. 'Null' if not supported by the active location provider.
		/// </summary>
		public float? SpeedKmPerHour
		{
			get
			{
				if (!SpeedMetersPerSecond.HasValue) { return null; }
				return SpeedMetersPerSecond * 3.6f;
			}
		}
		/// <summary>
		/// Name of the location provider. 'Null' if not supported by the active location provider.
		/// </summary>
		public string Provider;

		/// <summary>
		/// Has the heading changed since last update?
		/// </summary>
		public bool IsHeadingUpdated;
	}
}
