namespace Mapbox.Examples.LocationProvider
{
    using Mapbox.Unity.Location;
    using UnityEngine;

    public class RotateWithLocationProvider : MonoBehaviour
    {
        /// <summary>
        /// The rate at which the transform's rotation tries catch up to the provided heading.  
        /// </summary>
        [SerializeField]
        float _rotationFollowFactor;

        /// <summary>
        /// Set this to true if you'd like to adjust the rotation of a RectTransform (in a UI canvas) with the heading.
        /// </summary>
        [SerializeField]
        bool _rotateZ;

        /// <summary>
        /// Use a mock <see cref="T:Mapbox.Unity.Location.TransformLocationProvider"/>,
        /// rather than a <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>.   
        /// </summary>
        [SerializeField]
        bool _useTransformLocationProvider;

        /// <summary>
        /// The location provider.
        /// This is public so you change which concrete <see cref="ILocationProvider"/> to use at runtime.  
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
            var euler = Mapbox.Unity.Constants.Math.Vector3Zero;
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
