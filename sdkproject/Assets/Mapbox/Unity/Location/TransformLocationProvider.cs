using Mapbox.Unity.Map;
namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration;

    /// <summary>
    /// The TransformLocationProvider is responsible for providing mock location and heading data
    /// for testing purposes in the Unity editor.
    /// This is achieved by querying a Unity <see href="https://docs.unity3d.com/ScriptReference/Transform.html">Transform</see> every frame.
    /// You might use this to to update location based on a touched position, for example.
    /// </summary>
    public class TransformLocationProvider : MonoBehaviour, ILocationProvider
    {
        [SerializeField]
		private AbstractMap _map;

        /// <summary>
        /// The transform that will be queried for location and heading data.
        /// </summary>
        [SerializeField]
        Transform _targetTransform;

        /// <summary>
        /// Gets the latitude, longitude of the transform.
        /// This is converted from unity world space to real world geocoordinate space.
        /// </summary>
        /// <value>The location.</value>
        public Vector2d Location
        {
            get
            {
                return GetLocation();
            }
        }

        /// <summary>
        /// Sets the target transform.
        /// Use this if you want to switch the transform at runtime.
        /// </summary>
        public Transform TargetTransform
        {
            set
            {
                _targetTransform = value;
            }
        }

        /// <summary>
        /// Occurs every frame.
        /// </summary>
        public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;

        /// <summary>
        /// Occurs every frame.
        /// </summary>
        public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;

        void Update()
        {
            if (OnHeadingUpdated != null)
            {
                OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = _targetTransform.eulerAngles.y });
            }

            if (OnLocationUpdated != null)
            {
                OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = GetLocation() });
            }
        }

        Vector2d GetLocation()
        {
			//if (_map.CenterMercator)
   //         {
   //             return LocationProviderFactory.Instance.DefaultLocationProvider.Location;
   //         }
			return _targetTransform.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
        }
    }
}
