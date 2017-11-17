namespace Mapbox.ProbeExtractorCs
{

	using System.Collections;
	using System.Collections.Generic;


	public struct TracePoint
	{
		public double Latitude;
		public double Longitude;
		public double Bearing;
		public long Timestamp;
	}


	public struct Probe
	{
		public double Latitude;
		public double Longitude;
		public long StartTime;
		public double Duration;
		public double Speed;
		public double Bearing;
		public double Distance;
		public bool IsGood;
	}

}