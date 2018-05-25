namespace Mapbox.Unity.Location
{


	using UnityEngine;


	public interface IMapboxLocationService
	{


		bool isEnabledByUser { get; }

		LocationServiceStatus status { get; }

		IMapboxLocationInfo lastData { get; }

		void Start(float desiredAccuracyInMeters, float updateDistanceInMeters);

		void Stop();
	}
}
