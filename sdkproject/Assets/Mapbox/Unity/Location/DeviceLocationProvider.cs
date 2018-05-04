namespace Mapbox.Unity.Location
{
	using System.Collections;
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.CheapRulerCs;
	using System;
	using System.Linq;

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


		[SerializeField]
		[Tooltip("The minimum time interval between location updates, in milliseconds.")]
		long _updateTimeInMilliSeconds = 1000;


		private Coroutine _pollRoutine;
		private double _lastLocationTimestamp;
		private double _lastHeadingTimestamp;
		private WaitForSeconds _wait1sec;
		private WaitForSeconds _waitUpdateTime;
		/// <summary>list of positions to keep for calculations</summary>
		private CircularBuffer<Vector2d> _lastPositions;
		/// <summary>number of last positons to keep</summary>
		private int _maxLastPositions = 5;
		/// <summary>minimum needed distance between oldest and newest position before UserHeading is calculated</summary>
		private double _minDistanceOldestNewestPosition = 1.5;
		/// <summary>weights for calculating 'UserHeading'. hardcoded for now. TODO: auto-calc based on time, distance, ...</summary>
		private float[] _headingWeights = new float[]{
			0,
			-0.5f,
			-1.0f,
			-1.5f
		};


		// Android 6+ permissions have to be granted during runtime
		// these are the callbacks for requesting location permission
		// TODO: show message to users in case they accidentallly denied permission
#if UNITY_ANDROID
		private bool _gotPermissionRequestResponse = false;

		private void OnAllow() { _gotPermissionRequestResponse = true; }
		private void OnDeny() { _gotPermissionRequestResponse = true; }
		private void OnDenyAndNeverAskAgain() { _gotPermissionRequestResponse = true; }
#endif


		protected virtual void Awake()
		{
			_currentLocation.Provider = "unity";
			_wait1sec = new WaitForSeconds(1f);
			_waitUpdateTime = _updateTimeInMilliSeconds < 500 ? new WaitForSeconds(500) : new WaitForSeconds(_updateTimeInMilliSeconds / 1000);

			_lastPositions = new CircularBuffer<Vector2d>(_maxLastPositions);
			// safe measure till we have auto calculated weights
			// "_maxLastPositions - 1" because we calculate user heading on the fly: nr of angles = nr of positions - 1
			if (_headingWeights.Length != _maxLastPositions - 1)
			{
				throw new Exception("number of last positions NOT equal number of heading weights");
			}

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

			System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

			while (true)
			{

				_currentLocation.IsLocationServiceEnabled = Input.location.status == LocationServiceStatus.Running;

				_currentLocation.IsUserHeadingUpdated = false;
				_currentLocation.IsLocationUpdated = false;

				if (!_currentLocation.IsLocationServiceEnabled)
				{
					yield return _waitUpdateTime;
					continue;
				}

				// device orientation, user heading get calculated below
				_currentLocation.DeviceOrientation = Input.compass.trueHeading;


				var lastData = Input.location.lastData;
				var timestamp = lastData.timestamp;
				//Debug.LogFormat("{0:yyyyMMdd-HHmmss} acc:{1:0.00} {2} / {3}", UnixTimestampUtils.From(timestamp), lastData.horizontalAccuracy, lastData.latitude, lastData.longitude);


				//_currentLocation.LatitudeLongitude = new Vector2d(lastData.latitude, lastData.longitude);
				// HACK to get back to double precision, does this even work?
				// https://forum.unity.com/threads/precision-of-location-longitude-is-worse-when-longitude-is-beyond-100-degrees.133192/#post-1835164
				double latitude = double.Parse(lastData.latitude.ToString("R", invariantCulture), invariantCulture);
				double longitude = double.Parse(lastData.longitude.ToString("R", invariantCulture), invariantCulture);
				_currentLocation.LatitudeLongitude = new Vector2d(latitude, longitude);

				_currentLocation.Accuracy = (int)System.Math.Floor(lastData.horizontalAccuracy);
				_currentLocation.IsLocationUpdated = timestamp > _lastLocationTimestamp;
				_currentLocation.Timestamp = timestamp;
				_lastLocationTimestamp = timestamp;

				if (_currentLocation.IsLocationUpdated)
				{
					_lastPositions.Add(_currentLocation.LatitudeLongitude);
				}

				// calculate user heading. only if we have enough positions available
				if (_lastPositions.Count < _maxLastPositions)
				{
					_currentLocation.UserHeading = 0;
				}
				else
				{
					Vector2d newestPos = _lastPositions[0];
					Vector2d oldestPos = _lastPositions[_maxLastPositions - 1];
					CheapRuler cheapRuler = new CheapRuler(newestPos.x, CheapRulerUnits.Meters);
					// distance between last and first position in our buffer
					double distance = cheapRuler.Distance(
						new double[] { newestPos.y, newestPos.x },
						new double[] { oldestPos.y, oldestPos.x }
					);
					// positions are minimum required distance apart (user is moving), calculate user heading
					if (distance >= _minDistanceOldestNewestPosition)
					{
						// calculate final heading from last positions but give newest headings more weight:
						// '_lastPositions' contains newest at [0]
						// formula:
						// (heading[0] * e^weight[0] + heading[1] * e^weight[1] + .... ) / weight sum
						float[] lastHeadings = new float[_maxLastPositions - 1];
						float[] actualWeights = new float[_maxLastPositions - 1];
						float finalHeading = 0f;

						for (int i = 1; i < _maxLastPositions; i++)
						{
							lastHeadings[i - 1] = (float)(Math.Atan2(_lastPositions[i].y - _lastPositions[i - 1].y, _lastPositions[i].x - _lastPositions[i - 1].x) * 180 / Math.PI);
							// quick fix to take care of 355° and 5° being apart 10° and not 350°
							if (lastHeadings[i - 1] > 180) { lastHeadings[i - 1] -= 360f; }
						}

						for (int i = 0; i < lastHeadings.Length; i++)
						{
							actualWeights[i] = (float)Math.Exp(_headingWeights[i]);
							finalHeading += lastHeadings[i] * actualWeights[i];
						}

						float weightSum = actualWeights.Sum();
						finalHeading /= weightSum;
						// stay within [0..359] no negative angles
						if (finalHeading < 0) { finalHeading += 360; }

						_currentLocation.UserHeading = finalHeading;
						_currentLocation.IsUserHeadingUpdated = true;
					}
				}

				SendLocation(_currentLocation);

				yield return _waitUpdateTime;
			}
		}
	}
}
