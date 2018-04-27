
namespace Mapbox.ProbeExtractorCs
{

	using Mapbox.Unity.Location;


	/// <summary>
	/// Represents a point of a GPS trace
	/// </summary>
	public struct TracePoint
	{
		public long Timestamp;
		public double Latitude;
		public double Longitude;
		public double Bearing;
		public float? Elevation;
		/// <summary> Horizontal dilution of precision </summary>
		public float? HDop;
		/// <summary> Vertical dilution of precision</summary>
		public float? VDop;

		public static TracePoint FromLocation(Location location)
		{
			return new TracePoint()
			{
				Timestamp = (long)location.Timestamp,
				Latitude = location.LatitudeLongitude.x,
				Longitude = location.LatitudeLongitude.y,
				Bearing = location.UserHeading,
				HDop = location.Accuracy
			};
		}
	}


	/// <summary>
	/// Represents a probe extracted by ProbeExtractor
	/// </summary>
	public struct Probe
	{
		public double Latitude;
		public double Longitude;
		public long StartTime;
		public long Duration;
		public double Speed;
		public double Bearing;
		public double Distance;
		public bool IsGood;
	}

}