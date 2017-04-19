namespace Mapbox.Unity.Location
{
    using System.Collections;
    using UnityEngine;
    using System;
    using Mapbox.Utils;

    /// <summary>
    /// The DeviceLocationProvider is responsible for providing real world location and heading data,
    /// served directly from native hardware and OS. 
    /// This relies on Unity's <see href="https://docs.unity3d.com/ScriptReference/LocationService.html">LocationService</see> for location
    /// and <see href="https://docs.unity3d.com/ScriptReference/Compass.html">Compass</see> for heading.
    /// </summary>
    public class DeviceLocationProvider : MonoBehaviour, ILocationProvider
    {
        /// <summary>
        /// Using higher value like 500 usually does not require to turn GPS chip on and thus saves battery power. 
        /// Values like 5-10 could be used for getting best accuracy.
        /// </summary>
        [SerializeField]
        float _desiredAccuracyInMeters = 5f;

        /// <summary>
        /// The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. 
        /// Higher values like 500 imply less overhead.
        /// </summary>
        [SerializeField]
        float _updateDistanceInMeters = 5f;

        Coroutine _pollRoutine;

        double _lastLocationTimestamp;

        double _lastHeadingTimestamp;

        WaitForSeconds _wait;

        Vector2d _location;
        /// <summary>
        /// Gets the current cached location.
        /// </summary>
        /// <value>The location.</value>
        public Vector2d Location
        {
            get
            {
                return _location;
            }
        }

        /// <summary>
        /// Occurs when on location updates.
        /// </summary>
        public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;

        /// <summary>
        /// Occurs when the compass updates.
        /// </summary>
        public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;

        void Start()
        {
            _wait = new WaitForSeconds(1f);
            if (_pollRoutine == null)
            {
                _pollRoutine = StartCoroutine(PollLocationRoutine());
            }
        }

        /// <summary>
        /// Enable location and compass services.
        /// Sends continuous location and heading updates based on 
        /// _desiredAccuracyInMeters and _updateDistanceInMeters.
        /// </summary>
        /// <returns>The location routine.</returns>
        IEnumerator PollLocationRoutine()
        {
            if (!Input.location.isEnabledByUser)
            {
                yield break;
            }

            Input.location.Start(_desiredAccuracyInMeters, _updateDistanceInMeters);
            Input.compass.enabled = true;

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return _wait;
                maxWait--;
            }

            if (maxWait < 1)
            {
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                yield break;
            }

            while (true)
            {
                var timestamp = Input.compass.timestamp;
                if (Input.compass.enabled && timestamp > _lastHeadingTimestamp)
                {
                    var heading = Input.compass.trueHeading;
                    SendHeadingUpdated(heading);
                    _lastHeadingTimestamp = timestamp;
                }

                timestamp = Input.location.lastData.timestamp;
                if (Input.location.status == LocationServiceStatus.Running && timestamp > _lastLocationTimestamp)
                {
                    _location = new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
                    SendLocationUpdated(_location);
                    _lastLocationTimestamp = timestamp;
                }
                yield return null;
            }
        }

        void SendHeadingUpdated(float heading)
        {
            if (OnHeadingUpdated != null)
            {
                OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = heading });
            }
        }

        void SendLocationUpdated(Vector2d location)
        {
            if (OnLocationUpdated != null)
            {
                OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = location });
            }
        }
    }
}