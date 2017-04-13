namespace Mapbox.Examples.LocationProvider
{
    using Mapbox.Unity.Location;
    using UnityEngine;

    public class RotateWithLocationProvider : MonoBehaviour
    {
        [SerializeField]
        float _rotationFollowFactor;

        [SerializeField]
        bool _rotateZ;

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
                    _locationProvider.OnHeadingUpdated -= LocationProvider_OnHeadingUpdated;

                }
                _locationProvider = value;
                _locationProvider.OnHeadingUpdated += LocationProvider_OnHeadingUpdated;
            }
        }

        Vector3 _targetPosition;

        void Start()
        {
            LocationProvider.OnHeadingUpdated += LocationProvider_OnHeadingUpdated;
        }

        void OnDestroy()
        {
            if (LocationProvider != null)
            {
                LocationProvider.OnHeadingUpdated -= LocationProvider_OnHeadingUpdated;
            }
        }

        void LocationProvider_OnHeadingUpdated(object sender, HeadingUpdatedEventArgs e)
        {
            var euler = Vector3.zero;
            if (_rotateZ)
            {
                euler.z = -e.Heading;
            }
            else
            {
                euler.y = e.Heading;
            }

            var rotation = Quaternion.Euler(euler);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, rotation, Time.deltaTime * _rotationFollowFactor);
        }
    }
}
