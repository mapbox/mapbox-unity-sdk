namespace Mapbox.Unity.Location
{
    using System.Collections;
    using UnityEngine;
    using System;
    using Mapbox.Utils;

    public class DeviceLocationProvider : MonoBehaviour, ILocationProvider
    {
        [SerializeField]
        float _desiredAccuracyInMeters = 5f;

        [SerializeField]
        float _updateDistanceInMeters = 5f;

        Coroutine _pollRoutine;

        double _lastLocationTimestamp;

        double _lastHeadingTimestamp;

        WaitForSeconds _wait;

        Vector2d _location;
        public Vector2d Location
        {
            get
            {
                return _location;
            }
        }

        public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;
        public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;

        void Start()
        {
            _wait = new WaitForSeconds(1f);
            if (_pollRoutine == null)
            {
                _pollRoutine = StartCoroutine(PollLocationRoutine());
            }
        }

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