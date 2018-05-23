namespace Mapbox.Unity.Location
{


	using UnityEngine;


	public class MapboxLocationServiceUnityWrapper : IMapboxLocationService
	{
		public bool isEnabledByUser { get { return Input.location.isEnabledByUser; } }

		public LocationServiceStatus status { get { return Input.location.status; } }

		public IMapboxLocationInfo lastData { get { return new MapboxLocationInfoUnityWrapper(Input.location.lastData); } }

		public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
		{
			Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
		}

		public void Stop()
		{
			Input.location.Stop();
		}
	}
}
