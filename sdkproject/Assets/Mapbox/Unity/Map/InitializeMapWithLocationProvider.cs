namespace Mapbox.Unity.Map
{
	using System.Collections;
	using Mapbox.Unity.Location;
	using UnityEngine;

	public class InitializeMapWithLocationProvider : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		ILocationProvider _locationProvider;

		IEnumerator Start()
		{
			yield return null;
			_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
			_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated; ;
		}

		void LocationProvider_OnLocationUpdated(Unity.Location.Location location)
		{
			_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			_map.Initialize(location.LatitudeLongitude, _map.AbsoluteZoom);
		}
	}
}