namespace Mapbox.Unity.Location
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public struct MapboxLocationInfoUnityWrapper : IMapboxLocationInfo
	{

		public MapboxLocationInfoUnityWrapper(LocationInfo locationInfo)
		{
			_locationInfo = locationInfo;
		}

		private LocationInfo _locationInfo;


		public float latitude { get { return _locationInfo.latitude; } }

		public float longitude { get { return _locationInfo.longitude; } }

		public float altitude { get { return _locationInfo.altitude; } }

		public float horizontalAccuracy { get { return _locationInfo.horizontalAccuracy; } }

		public float verticalAccuracy { get { return _locationInfo.verticalAccuracy; } }

		public double timestamp { get { return _locationInfo.timestamp; } }


	}
}
