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
	public class EditorLocationProvider : AbstractLocationProvider
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

		[SerializeField]
		int _accuracy;

		Location _currentLocation;

		Vector2d LatitudeLongitude
        {
            get
            {
                var split = _latitudeLongitude.Split(',');
                return new Vector2d(double.Parse(split[0]), double.Parse(split[1]));
            }
        }

#if UNITY_EDITOR
        void Update()
        {
			_currentLocation.Heading = _heading;
			_currentLocation.LatitudeLongitude = LatitudeLongitude;
			_currentLocation.Accuracy = _accuracy;
			_currentLocation.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

			SendLocation(_currentLocation);
        }
#endif
    }
}
