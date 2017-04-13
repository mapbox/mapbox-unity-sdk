namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using UnityEngine;

    public class EditorLocationProvider : MonoBehaviour, ILocationProvider
    {
        [SerializeField]
        [Geocode]
        string _latitudeLongitude;

        [SerializeField]
        [Range(0, 359)]
        float _heading;

        public Vector2d Location
        {
            get
            {
                var split = _latitudeLongitude.Split(',');
                return new Vector2d(double.Parse(split[0]), double.Parse(split[1]));
            }
        }

        public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
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
