namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using UnityEngine;

    /// <summary>
    /// The EditorLocationProvider is responsible for providing mock location and heading data
    /// for testing purposes in the Unity editor.
    /// </summary>
    public class EditorLocationProvider : MonoBehaviour, ILocationProvider
    {
        /// <summary>
        /// The mock "latitude, longitude" location, respresented with a string.
        /// You can search for a place using the embedded "Search" button in the inspector.
        /// This value can be changed at runtime in the inspector.
        /// </summary>
        [SerializeField]
        [Geocode]
        string _latitudeLongitude;

        /// <summary>
        /// The mock heading value.
        /// </summary>
        [SerializeField]
        [Range(0, 359)]
        float _heading;

        /// <summary>
        /// Gets the current location, as specified in the inspector.
        /// </summary>
        /// <value>The location.</value>
        public Vector2d Location
        {
            get
            {
                var split = _latitudeLongitude.Split(',');
                return new Vector2d(double.Parse(split[0]), double.Parse(split[1]));
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

#if UNITY_EDITOR
        void Update()
        {
            SendHeadingUpdated();
            SendLocationUpdated();
        }
#endif

        void SendHeadingUpdated()
        {
            if (OnHeadingUpdated != null)
            {
                OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = _heading });
            }
        }

        void SendLocationUpdated()
        {
            if (OnLocationUpdated != null)
            {
                OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = Location });
            }
        }
    }
}
