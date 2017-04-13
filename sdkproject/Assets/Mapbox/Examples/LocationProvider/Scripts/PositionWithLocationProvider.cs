namespace Mapbox.Examples.LocationProvider
{
    using Mapbox.Unity.Location;
    using Mapbox.Unity.Utilities;
    using Mapbox.Unity.MeshGeneration;
    using UnityEngine;

    public class PositionWithLocationProvider : MonoBehaviour
	{
		[SerializeField]
		float _positionFollowFactor;

		ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
			set
			{
				if (_locationProvider != null)
				{
					_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;

				}
				_locationProvider = value;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}

		Vector3 _targetPosition;

		void Start()
		{
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}

		void OnDestroy()
		{
			if (LocationProvider != null)
			{
				LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			}
		}

		void LocationProvider_OnLocationUpdated(object sender, LocationUpdatedEventArgs e)
		{
            _targetPosition = Conversions.GeoToWorldPosition(e.Location,
                                                             MapController.ReferenceTileRect.Center, 
                                                             MapController.WorldScaleFactor).ToVector3xz();
		}

		void Update()
		{
			transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _positionFollowFactor);
		}
	}
}
