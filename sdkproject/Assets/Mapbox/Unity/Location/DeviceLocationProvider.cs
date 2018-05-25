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
		public float _desiredAccuracyInMeters = 1.0f;


		/// <summary>
		/// The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. 
		/// Higher values like 500 imply less overhead.
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. Higher values like 500 imply less overhead.")]
		public float _updateDistanceInMeters = 0.0f;


		[SerializeField]
		[Tooltip("The minimum time interval between location updates, in milliseconds. It's reasonable to not go below 500ms.")]
		public long _updateTimeInMilliSeconds = 500;


		[SerializeField]
		[Tooltip("Smoothing strategy to be applied to the UserHeading.")]
		public AngleSmoothingAbstractBase _userHeadingSmoothing;


		[SerializeField]
		[Tooltip("Smoothing strategy to applied to the DeviceOrientation.")]
		public AngleSmoothingAbstractBase _deviceOrientationSmoothing;


		[Serializable]
		public struct DebuggingInEditor
		{
			[Header("Set 'EditorLocationProvider' to 'DeviceLocationProvider' and connect device with UnityRemote.")]
			[SerializeField]
			[Tooltip("Mock Unity's 'Input.Location' to route location log files through this class (eg fresh calculation of 'UserHeading') instead of just replaying them. To use set 'Editor Location Provider' in 'Location Factory' to 'Device Location Provider' and select a location log file below.")]
			public bool _mockUnityInputLocation;

			[SerializeField]
			[Tooltip("Also see above. Location log file to mock Unity's 'Input.Location'.")]
			public TextAsset _locationLogFile;
		}

		[Space(20)]
		public DebuggingInEditor _editorDebuggingOnly;


		private IMapboxLocationService _locationService;
		private Coroutine _pollRoutine;
		private double _lastLocationTimestamp;
		private WaitForSeconds _wait1sec;
		private WaitForSeconds _waitUpdateTime;
		/// <summary>list of positions to keep for calculations</summary>
		private CircularBuffer<Vector2d> _lastPositions;
		/// <summary>number of last positons to keep</summary>
		private int _maxLastPositions = 5;
		/// <summary>minimum needed distance between oldest and newest position before UserHeading is calculated</summary>
		private double _minDistanceOldestNewestPosition = 1.5;


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
#if UNITY_EDITOR
			if (_editorDebuggingOnly._mockUnityInputLocation)
			{
				if (null == _editorDebuggingOnly._locationLogFile || null == _editorDebuggingOnly._locationLogFile.bytes)
				{
					throw new ArgumentNullException("Location Log File");
				}

				_locationService = new MapboxLocationServiceMock(_editorDebuggingOnly._locationLogFile.bytes);
			}
			else
			{
#endif
				_locationService = new MapboxLocationServiceUnityWrapper();
#if UNITY_EDITOR
			}
#endif

			_currentLocation.Provider = "unity";
			_wait1sec = new WaitForSeconds(1f);
			_waitUpdateTime = _updateTimeInMilliSeconds < 500 ? new WaitForSeconds(0.5f) : new WaitForSeconds((float)_updateTimeInMilliSeconds / 1000.0f);

			if (null == _userHeadingSmoothing) { _userHeadingSmoothing = transform.gameObject.AddComponent<AngleSmoothingNoOp>(); }
			if (null == _deviceOrientationSmoothing) { _deviceOrientationSmoothing = transform.gameObject.AddComponent<AngleSmoothingNoOp>(); }

			_lastPositions = new CircularBuffer<Vector2d>(_maxLastPositions);

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
				// exit if we are not the selected location provider
				if (null != LocationProviderFactory.Instance && null != LocationProviderFactory.Instance.DefaultLocationProvider)
				{
					if (!this.Equals(LocationProviderFactory.Instance.DefaultLocationProvider))
					{
						yield break;
					}
				}

				Debug.LogWarning("Remote device not connected via 'Unity Remote'. Waiting ..." + Environment.NewLine + "If Unity seems to be stuck here make sure 'Unity Remote' is running and restart Unity with your device already connected.");
				yield return _wait1sec;
			}
#endif


			//request runtime fine location permission on Android if not yet allowed
#if UNITY_ANDROID
			if (!_locationService.isEnabledByUser)
			{
				UniAndroidPermission.RequestPermission(AndroidPermission.ACCESS_FINE_LOCATION);
				//wait for user to allow or deny
				while (!_gotPermissionRequestResponse) { yield return _wait1sec; }
			}
#endif


			if (!_locationService.isEnabledByUser)
			{
				Debug.LogError("DeviceLocationProvider: Location is not enabled by user!");
				_currentLocation.IsLocationServiceEnabled = false;
				SendLocation(_currentLocation);
				yield break;
			}


			_currentLocation.IsLocationServiceInitializing = true;
			_locationService.Start(_desiredAccuracyInMeters, _updateDistanceInMeters);
			Input.compass.enabled = true;

			int maxWait = 20;
			while (_locationService.status == LocationServiceStatus.Initializing && maxWait > 0)
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

			if (_locationService.status == LocationServiceStatus.Failed)
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

				var lastData = _locationService.lastData;
				var timestamp = lastData.timestamp;

				///////////////////////////////
				// oh boy, Unity what are you doing???
				// on some devices it seems that
				// Input.location.status != LocationServiceStatus.Running
				// nevertheless new location is available
				//////////////////////////////
				//Debug.LogFormat("Input.location.status: {0}", Input.location.status);
				_currentLocation.IsLocationServiceEnabled =
					_locationService.status == LocationServiceStatus.Running
					|| timestamp > _lastLocationTimestamp;

				_currentLocation.IsUserHeadingUpdated = false;
				_currentLocation.IsLocationUpdated = false;

				if (!_currentLocation.IsLocationServiceEnabled)
				{
					yield return _waitUpdateTime;
					continue;
				}

				// device orientation, user heading get calculated below
				_deviceOrientationSmoothing.Add(Input.compass.trueHeading);
				_currentLocation.DeviceOrientation = (float)_deviceOrientationSmoothing.Calculate();


				//_currentLocation.LatitudeLongitude = new Vector2d(lastData.latitude, lastData.longitude);
				// HACK to get back to double precision, does this even work?
				// https://forum.unity.com/threads/precision-of-location-longitude-is-worse-when-longitude-is-beyond-100-degrees.133192/#post-1835164
				double latitude = double.Parse(lastData.latitude.ToString("R", invariantCulture), invariantCulture);
				double longitude = double.Parse(lastData.longitude.ToString("R", invariantCulture), invariantCulture);
				Vector2d previousLocation = new Vector2d(_currentLocation.LatitudeLongitude.x, _currentLocation.LatitudeLongitude.y);
				_currentLocation.LatitudeLongitude = new Vector2d(latitude, longitude);

				_currentLocation.Accuracy = (float)Math.Floor(lastData.horizontalAccuracy);
				// sometimes Unity's timestamp doesn't seem to get updated, or even jump back in time
				// do an additional check if location has changed
				_currentLocation.IsLocationUpdated = timestamp > _lastLocationTimestamp || !_currentLocation.LatitudeLongitude.Equals(previousLocation);
				_currentLocation.Timestamp = timestamp;
				_lastLocationTimestamp = timestamp;

				if (_currentLocation.IsLocationUpdated)
				{
					if (_lastPositions.Count > 0)
					{
						// only add position if user has moved +1m since we added the previous position to the list
						CheapRuler cheapRuler = new CheapRuler(_currentLocation.LatitudeLongitude.x, CheapRulerUnits.Meters);
						Vector2d p = _currentLocation.LatitudeLongitude;
						double distance = cheapRuler.Distance(
							new double[] { p.y, p.x },
							new double[] { _lastPositions[0].y, _lastPositions[0].x }
						);
						if (distance > 1.0)
						{
							_lastPositions.Add(_currentLocation.LatitudeLongitude);
						}
					}
					else
					{
						_lastPositions.Add(_currentLocation.LatitudeLongitude);
					}
				}

				// if we have enough positions calculate user heading ourselves.
				// Unity does not provide bearing based on GPS locations, just
				// device orientation based on Compass.Heading.
				// nevertheless, use compass for intial UserHeading till we have
				// enough values to calculate ourselves.
				if (_lastPositions.Count < _maxLastPositions)
				{
					_currentLocation.UserHeading = _currentLocation.DeviceOrientation;
					_currentLocation.IsUserHeadingUpdated = true;
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
						float[] lastHeadings = new float[_maxLastPositions - 1];

						for (int i = 1; i < _maxLastPositions; i++)
						{
							// atan2 increases angle CCW, flip sign of latDiff to get CW
							double latDiff = -(_lastPositions[i].x - _lastPositions[i - 1].x);
							double lngDiff = _lastPositions[i].y - _lastPositions[i - 1].y;
							// +90.0 to make top (north) 0°
							double heading = (Math.Atan2(latDiff, lngDiff) * 180.0 / Math.PI) + 90.0f;
							// stay within [0..360]° range
							if (heading < 0) { heading += 360; }
							if (heading >= 360) { heading -= 360; }
							lastHeadings[i - 1] = (float)heading;
						}

						_userHeadingSmoothing.Add(lastHeadings[0]);
						float finalHeading = (float)_userHeadingSmoothing.Calculate();

						//fix heading to have 0° for north, 90° for east, 180° for south and 270° for west
						finalHeading = finalHeading >= 180.0f ? finalHeading - 180.0f : finalHeading + 180.0f;


						_currentLocation.UserHeading = finalHeading;
						_currentLocation.IsUserHeadingUpdated = true;
					}
				}

				_currentLocation.TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow);
				SendLocation(_currentLocation);

				yield return _waitUpdateTime;
			}
		}
	}
}
