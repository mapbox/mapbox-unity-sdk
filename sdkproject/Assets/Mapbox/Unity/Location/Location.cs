namespace Mapbox.Unity.Location
{
	using Mapbox.Utils;
    using System.Diagnostics;

    /// <summary>
    /// Location contains heading, latitude, longitude, accuracy and a timestamp.
    /// </summary>
    [DebuggerDisplay("{LatitudeLongitude,nq} {Accuracy}m hdg:{UserHeading} orientation:{DeviceOrientation}")]
	public struct Location
	{
		/// <summary>
		/// The location, as descibed by a <see cref="T:Mapbox.Utils.Vector2d"/>. 
		/// Location.x represents Latitude.
		/// Location.y represents Longitude.
		/// </summary>
		public Vector2d LatitudeLongitude;

		/// <summary>
		/// <para>Heading represents a angle of direction during movement, generally between 0-359.</para>
		///<para>Initially 0 this property gets populated after the device has moved far enough to determine a direction</para>
		///<para>If the device stops moving last heading is kept till a new one can be caluculated. Check <see cref="Mapbox.Unity.Location.IsHeadingUpdated"/></para>
		///<para>Also needs location services enabled via Input.location.Start()</para>
		///<para>related <see cref="Mapbox.Unity.Location.DeviceOrientation"/></para>
		/// </summary>
		public float UserHeading;

		/// <summary>
		///<para>Orientation (where the device is looking).</para>
		///<para>Uses device compass</para>
		///<para>related <see cref="Mapbox.Unity.Location.UserHeading"/></para>
		/// </summary>
		public float DeviceOrientation;

		/// <summary>
		/// UTC Timestamp (in seconds since 1970) when location was last updated.
		/// </summary>
		public double Timestamp;

		/// <summary>
		/// UTC Timestamp (in seconds since 1970) of the device when OnLocationUpdated was fired.
		/// </summary>
		public double TimestampDevice;

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
		/// Name of the location provider. GPS or network or 'Null' if not supported by the active location provider.
		/// </summary>
		public string Provider;


		/// <summary>
		/// Name of the location provider script class in Unity
		/// </summary>
		public string ProviderClass;

		/// <summary>
		/// Has the heading changed since last update?
		/// </summary>
		public bool IsUserHeadingUpdated;
	}
}
