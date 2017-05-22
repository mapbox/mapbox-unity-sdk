namespace Mapbox.Examples.LocationProvider
{
	using Mapbox.Unity.Map;
	using UnityEngine;
	using Mapbox.Unity.Location;

	/// <summary>
	/// Override the map center (latitude, longitude) for a MapController, based on the DefaultLocationProvider.
	/// This will enable you to generate a map for your current location, for example.
	/// </summary>
	public class BuildMapAtLocation : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		ILocationProvider _locationProvider;
		ILocationProvider LocationProvider
		{
			get
			{
				if (_locationProvider == null)
				{
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
		}

		void Start()
		{
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}

		void LocationProvider_OnLocationUpdated(object sender, Unity.Location.LocationUpdatedEventArgs e)
		{
			LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			_map.CenterLatitudeLongitude = e.Location;
			_map.enabled = true;
		}
	}
}