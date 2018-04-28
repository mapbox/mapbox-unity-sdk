namespace Mapbox.Unity.Map
{
	using System.Collections;
	using Mapbox.Unity.Location;
	using UnityEngine;

	public class UpdateMapWithLocationProvider : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		ILocationProvider _locationProvider;
		Vector3 _targetPosition;
		bool _isMapInitialized = false;
		private void Awake()
		{
			// Prevent double initialization of the map. 
			_map.InitializeOnStart = false;
		}

		IEnumerator Start()
		{
			yield return null;
			_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
			_locationProvider.OnLocationUpdated += LocationProvider_OnFirstLocationUpdate;
		}

		void LocationProvider_OnFirstLocationUpdate(Unity.Location.Location location)
		{
			_locationProvider.OnLocationUpdated -= LocationProvider_OnFirstLocationUpdate;
			_map.OnInitialized += () =>
			{
				_isMapInitialized = true;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			};
			_map.Initialize(location.LatitudeLongitude, _map.AbsoluteZoom);
		}

		void LocationProvider_OnLocationUpdated(Unity.Location.Location location)
		{
			if (_isMapInitialized && location.IsLocationUpdated)
			{
				_targetPosition = _map.GeoToWorldPosition(location.LatitudeLongitude, false);
			}
		}

		private void LateUpdate()
		{
			if (_isMapInitialized)
			{
				var currentPosition = _map.GeoToWorldPosition(_map.CenterLatitudeLongitude, false);
				var position = Vector3.Lerp(currentPosition, _targetPosition, Time.deltaTime);
				var latLong = _map.WorldToGeoPosition(position);
				_map.UpdateMap(latLong, _map.Zoom);
			}
		}
	}
}
