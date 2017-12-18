namespace Mapbox.Unity.Location
{
	using System.Collections;
	using UnityEngine;
	using Mapbox.Utils;

	/// <summary>
	/// The DeviceLocationProvider is responsible for providing real world location and heading data,
	/// served directly from native hardware and OS. 
	/// This relies on Unity's <see href="https://docs.unity3d.com/ScriptReference/LocationService.html">LocationService</see> for location
	/// and <see href="https://docs.unity3d.com/ScriptReference/Compass.html">Compass</see> for heading.
	/// </summary>
	public class DeviceLocationProvider : AbstractLocationProvider
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

		void Awake()
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
#if UNITY_EDITOR
			yield return new WaitWhile(() => !UnityEditor.EditorApplication.isRemoteConnected);
#endif
			if (!Input.location.isEnabledByUser)
			{
				Debug.LogError("DeviceLocationProvider: " + "Location is not enabled by user!");
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
				Debug.LogError("DeviceLocationProvider: " + "Timed out trying to initialize location services!");
				yield break;
			}

			if (Input.location.status == LocationServiceStatus.Failed)
			{
				Debug.LogError("DeviceLocationProvider: " + "Failed to initialize location services!");
				yield break;
			}

#if UNITY_EDITOR
			// HACK: this is to prevent Android devices, connected through Unity Remote, 
			// from reporting a location of (0, 0), initially.
			yield return _wait;
#endif
			while (true)
			{
				_currentLocation.IsHeadingUpdated = false;
				_currentLocation.IsLocationUpdated = false;

				var timestamp = Input.compass.timestamp;
				if (Input.compass.enabled && timestamp > _lastHeadingTimestamp)
				{
					var heading = Input.compass.trueHeading;
					_currentLocation.Heading = heading;
					_lastHeadingTimestamp = timestamp;

					_currentLocation.IsHeadingUpdated = true;
				}

				var lastData = Input.location.lastData;
				timestamp = lastData.timestamp;
				if (Input.location.status == LocationServiceStatus.Running && timestamp > _lastLocationTimestamp)
				{
					_currentLocation.LatitudeLongitude = new Vector2d(lastData.latitude, lastData.longitude);
					_currentLocation.Accuracy = (int)lastData.horizontalAccuracy;
					_currentLocation.Timestamp = timestamp;
					_lastLocationTimestamp = timestamp;

					_currentLocation.IsLocationUpdated = true;
				}

				if (_currentLocation.IsHeadingUpdated || _currentLocation.IsLocationUpdated)
				{
					SendLocation(_currentLocation);
				}

				yield return null;
			}
		}
	}
}