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
		[Tooltip("Using higher value like 500 usually does not require to turn GPS chip on and thus saves battery power. Values like 5-10 could be used for getting best accuracy.")]
		float _desiredAccuracyInMeters = 5f;

		/// <summary>
		/// The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. 
		/// Higher values like 500 imply less overhead.
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. Higher values like 500 imply less overhead.")]
		float _updateDistanceInMeters = 5f;

		Coroutine _pollRoutine;

		double _lastLocationTimestamp;

		double _lastHeadingTimestamp;

		WaitForSeconds _wait1sec;


		// Android 6+ permissions have to be granted during runtime
		// these are the callbacks for requesting location permission
		// TODO: show message to users in case they accidentallly denied permission
#if UNITY_ANDROID
		private bool _gotPermissionRequestResponse = false;

		private void OnAllow() { _gotPermissionRequestResponse = true; }
		private void OnDeny() { _gotPermissionRequestResponse = true; }
		private void OnDenyAndNeverAskAgain() { _gotPermissionRequestResponse = true; }
#endif


		void Awake()
		{
			_wait1sec = new WaitForSeconds(1f);

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
			while (!UnityEditor.EditorApplication.isRemoteConnected)
			{
				yield return null;
			}
#endif


			//request runtime fine location permission on Android if not yet allowed
#if UNITY_ANDROID
			if (!Input.location.isEnabledByUser)
			{
				UniAndroidPermission.RequestPermission(AndroidPermission.ACCESS_FINE_LOCATION);
				//wait for user to allow or deny
				while (!_gotPermissionRequestResponse) { yield return _wait1sec; }
			}
#endif


			if (!Input.location.isEnabledByUser)
			{
				Debug.LogError("DeviceLocationProvider: Location is not enabled by user!");
				_currentLocation.IsLocationServiceEnabled = false;
				SendLocation(_currentLocation);
				yield break;
			}


			_currentLocation.IsLocationServiceInitializing = true;
			Input.location.Start(_desiredAccuracyInMeters, _updateDistanceInMeters);
			Input.compass.enabled = true;

			int maxWait = 20;
			while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
			{
				yield return _wait1sec;
				maxWait--;
			}

			if (maxWait < 1)
			{
				Debug.LogError("DeviceLocationProvider: " + "Timed out trying to initialize location services!");
				_currentLocation.IsLocationServiceInitializing = false;
				_currentLocation.IsLocationServiceEnabled = false;
				SendLocation(_currentLocation);
				yield break;
			}

			if (Input.location.status == LocationServiceStatus.Failed)
			{
				Debug.LogError("DeviceLocationProvider: " + "Failed to initialize location services!");
				_currentLocation.IsLocationServiceInitializing = false;
				_currentLocation.IsLocationServiceEnabled = false;
				SendLocation(_currentLocation);
				yield break;
			}

			_currentLocation.IsLocationServiceInitializing = false;
			_currentLocation.IsLocationServiceEnabled = true;

#if UNITY_EDITOR
			// HACK: this is to prevent Android devices, connected through Unity Remote, 
			// from reporting a location of (0, 0), initially.
			yield return _wait1sec;
#endif


			float gpsInitializedTime = Time.realtimeSinceStartup;
			// initially pass through all locations that come available
			float gpsWarmupTime = 120f; //seconds
			System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

			while (true)
			{
				_currentLocation.IsLocationServiceEnabled = true;

				_currentLocation.IsHeadingUpdated = false;
				_currentLocation.IsLocationUpdated = false;

				var timestamp = Input.compass.timestamp;
				if (
					Input.compass.enabled && timestamp > _lastHeadingTimestamp
					|| Time.realtimeSinceStartup < gpsInitializedTime + gpsWarmupTime
				)
				{
					var heading = Input.compass.trueHeading;
					_currentLocation.Heading = heading;
					_currentLocation.HeadingMagnetic = Input.compass.magneticHeading;
					_currentLocation.HeadingAccuracy = Input.compass.headingAccuracy;
					_lastHeadingTimestamp = timestamp;

					_currentLocation.IsHeadingUpdated = true;
				}

				var lastData = Input.location.lastData;
				timestamp = lastData.timestamp;
				//Debug.LogFormat("{0:yyyyMMdd-HHmmss} acc:{1:0.00} {2} / {3}", UnixTimestampUtils.From(timestamp), lastData.horizontalAccuracy, lastData.latitude, lastData.longitude);

				if (
					(Input.location.status == LocationServiceStatus.Running && timestamp > _lastLocationTimestamp)
					|| Time.realtimeSinceStartup < gpsInitializedTime + gpsWarmupTime
				)
				{
					//_currentLocation.LatitudeLongitude = new Vector2d(lastData.latitude, lastData.longitude);
					// HACK to get back to double precision, does this even work?
					// https://forum.unity.com/threads/precision-of-location-longitude-is-worse-when-longitude-is-beyond-100-degrees.133192/#post-1835164
					double latitude = double.Parse(lastData.latitude.ToString("R", invariantCulture), invariantCulture);
					double longitude = double.Parse(lastData.longitude.ToString("R", invariantCulture), invariantCulture);
					_currentLocation.LatitudeLongitude = new Vector2d(latitude, longitude);

					_currentLocation.Accuracy = (int)System.Math.Floor(lastData.horizontalAccuracy);
					_currentLocation.Timestamp = timestamp;
					_lastLocationTimestamp = timestamp;

					_currentLocation.IsLocationUpdated = true;
				}

				if (_currentLocation.IsHeadingUpdated || _currentLocation.IsLocationUpdated)
				{
					if (_currentLocation.LatitudeLongitude != Vector2d.zero)
					{
						SendLocation(_currentLocation);
					}
				}

				yield return null;
			}
		}
	}
}
