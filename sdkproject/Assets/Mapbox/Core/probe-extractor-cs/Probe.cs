
namespace Mapbox.ProbeExtractorCs
{

	using Mapbox.Unity.Location;

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
				Bearing = location.Heading,
				HDop = location.Accuracy
			};
		}
	}


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