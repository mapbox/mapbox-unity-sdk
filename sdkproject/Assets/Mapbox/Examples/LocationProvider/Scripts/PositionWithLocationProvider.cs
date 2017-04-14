namespace Mapbox.Examples.LocationProvider
{
    using Mapbox.Unity.Location;
    using Mapbox.Unity.Utilities;
    using Mapbox.Unity.MeshGeneration;
    using UnityEngine;

    public class PositionWithLocationProvider : MonoBehaviour
	{
        /// <summary>
        /// The rate at which the transform's position tries catch up to the provided location.
        /// </summary>
		[SerializeField]
		float _positionFollowFactor;

        /// <summary>
        /// Use a mock <see cref="T:Mapbox.Unity.Location.TransformLocationProvider"/>,
        /// rather than a <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>. 
        /// </summary>
        [SerializeField]
        bool _useTransformLocationProvider;

        /// <summary>
        /// The location provider.
        /// This is public so you change which concrete <see cref="T:Mapbox.Unity.Location.ILocationProvider"/> to use at runtime.
        /// </summary>
		ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
                    _locationProvider = _useTransformLocationProvider ? 
                        LocationProviderFactory.Instance.TransformLocationProvider : LocationProviderFactory.Instance.DefaultLocationProvider;
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
            if (MapController.ReferenceTileRect == null)
            {
                return;
            }

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
