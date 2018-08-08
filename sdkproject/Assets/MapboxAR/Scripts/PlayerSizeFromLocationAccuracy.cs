namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using UnityEngine;

	public class PlayerSizeFromLocationAccuracy : MonoBehaviour
	{

		private ILocationProvider _locationProvider;
		private Vector3 _playerScale = new Vector3(2f, 2f, 2f);


		void Start()
		{
			_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
			if (_locationProvider != null)
			{
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}


		void OnDestroy()
		{
			if (_locationProvider != null)
			{
				_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			}
		}


		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (location.Accuracy != 0)
			{
				float halfAcc = location.Accuracy / 2f;
				_playerScale = new Vector3(halfAcc, halfAcc, halfAcc);
			}
		}


		void Update()
		{
			transform.localScale = _playerScale;
		}
	}
}
