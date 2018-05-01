namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Map;
	using UnityEngine;

	public class ImmediatePositionWithLocationProvider : MonoBehaviour
	{
		//[SerializeField]
		//private UnifiedMap _map;

		bool _isInitialized;

		[SerializeField]
		bool _addError = false;

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

		Vector3 _targetPosition;

		void Start()
		{
			LocationProviderFactory.Instance.mapManager.OnInitialized += () =>
			{
				_isInitialized = true;
				LocationProviderFactory.Instance.DeviceLocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			};
		}
		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (_isInitialized && location.IsLocationUpdated)
			{
				var map = LocationProviderFactory.Instance.mapManager;

				_targetPosition = map.GeoToWorldPosition(location.LatitudeLongitude) + ((_addError) ? Random.insideUnitSphere : Vector3.zero);
			}
		}
	}
}