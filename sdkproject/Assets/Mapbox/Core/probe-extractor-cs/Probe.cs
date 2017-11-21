namespace Mapbox.ProbeExtractorCs
{

	public struct TracePoint
	{
		public double Latitude;
		public double Longitude;
		public double Bearing;
		public long Timestamp;

		public static TracePoint FromLocation()
		{
			throw new System.NotImplementedException("TODO: after https://github.com/mapbox/mapbox-unity-sdk/pull/362 has been merged");
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