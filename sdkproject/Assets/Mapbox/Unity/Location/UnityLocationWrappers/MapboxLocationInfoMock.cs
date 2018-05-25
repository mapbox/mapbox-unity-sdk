namespace Mapbox.Unity.Location
{


	/// <summary>
	/// Wrapper to mock our 'Location' objects as Unity's 'LocationInfo'
	/// </summary>
	public struct MapboxLocationInfoMock : IMapboxLocationInfo
	{


		public MapboxLocationInfoMock(Location location)
		{
			_location = location;
		}


		private Location _location;

		public float latitude { get { return (float)_location.LatitudeLongitude.x; } }

		public float longitude { get { return (float)_location.LatitudeLongitude.y; } }

		public float altitude { get { return 0f; } }

		public float horizontalAccuracy { get { return _location.Accuracy; } }

		public float verticalAccuracy { get { return 0; } }

		public double timestamp { get { return _location.Timestamp; } }


	}
}
