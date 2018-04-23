namespace Mapbox.Unity.Location
{
	using UnityEngine;
	using System.Collections;
	using System.Globalization;
	using System;
	using System.IO;
	using System.Text;


	public class DeviceLocationProviderAndroidNative : AbstractLocationProvider, IDisposable
	{


		/// <summary>
		/// The minimum distance (measured in meters) a device must move laterally before location is updated. 
		/// https://developer.android.com/reference/android/location/LocationManager.html#requestLocationUpdates(java.lang.String,%20long,%20float,%20android.location.LocationListener)
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum distance (measured in meters) a device must move laterally before location is updated. Higher values like 500 imply less overhead.")]
		float _updateDistanceInMeters = 0.5f;


		/// <summary>
		/// The minimum time interval between location updates, in milliseconds.
		/// https://developer.android.com/reference/android/location/LocationManager.html#requestLocationUpdates(java.lang.String,%20long,%20float,%20android.location.LocationListener)
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum time interval between location updates, in milliseconds.")]
		long _updateTimeInMilliSeconds = 1000;


		private WaitForSeconds _wait1sec;
		private WaitForSeconds _wait5sec;
		private WaitForSeconds _wait60sec;
		/// <summary>polls location provider only at the requested update intervall to reduce load</summary>
		private WaitForSeconds _waitUpdateTime;
		private bool _disposed;
		private static object _lock = new object();
		private Coroutine _pollLocation;
		private CultureInfo _invariantCulture = CultureInfo.InvariantCulture;
		private AndroidJavaObject _activityContext = null;
		private AndroidJavaObject _gpsInstance;


		~DeviceLocationProviderAndroidNative() { Dispose(false); }
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					shutdown();
				}
				_disposed = true;
			}
		}


		private void shutdown()
		{
			try
			{
				lock (_lock)
				{
					if (null != _gpsInstance)
					{
						_gpsInstance.Call("stopLocationListeners");
						_gpsInstance.Dispose();
						_gpsInstance = null;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}


		private void OnDestroy() { shutdown(); }


		private void OnDisable() { shutdown(); }






		void Awake()
		{

			_wait1sec = new WaitForSeconds(1);
			_wait5sec = new WaitForSeconds(5);
			_wait60sec = new WaitForSeconds(60);
			// throttle if entered update intervall is unreasonably low
			if (_updateTimeInMilliSeconds < 500)
			{
				_waitUpdateTime = new WaitForSeconds(500);
			}
			else
			{
				_waitUpdateTime = new WaitForSeconds(_updateTimeInMilliSeconds / 1000);
			}

			_currentLocation.IsLocationServiceEnabled = false;
			_currentLocation.IsLocationServiceInitializing = true;

			if (Application.platform == RuntimePlatform.Android)
			{
				getActivityContext();
				getGpsInstance(true);

				if (_pollLocation == null)
				{
					_pollLocation = StartCoroutine(locationRoutine());
				}
			}
		}


		private void getActivityContext()
		{
			using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				_activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
			}

			if (null == _activityContext)
			{
				Debug.LogError("Could not get UnityPlayer activity");
				return;
			}
		}


		private void getGpsInstance(bool showToastMessages = false)
		{
			if (null == _activityContext) { return; }

			using (AndroidJavaClass androidGps = new AndroidJavaClass("com.mapbox.android.unity.AndroidGps"))
			{
				if (null == androidGps)
				{
					Debug.LogError("Could not get class 'AndroidGps'");
					_activityContext.Call("runOnUiThread", new AndroidJavaRunnable(() => { _gpsInstance.Call("showMessage", "Could not get class 'AndroidGps'"); }));
					return;
				}

				_gpsInstance = androidGps.CallStatic<AndroidJavaObject>("instance", _activityContext);
				if (null == _gpsInstance)
				{
					Debug.LogError("Could not get 'AndroidGps' instance");
					_activityContext.Call("runOnUiThread", new AndroidJavaRunnable(() => { _gpsInstance.Call("showMessage", "Could not get 'AndroidGps' instance"); }));
					return;
				}

				_activityContext.Call("runOnUiThread", new AndroidJavaRunnable(() => { _gpsInstance.Call("showMessage", "starting location listeners"); }));

				_gpsInstance.Call("startLocationListeners", _updateDistanceInMeters, _updateTimeInMilliSeconds);
			}
		}


		//private void Update() {
		private IEnumerator locationRoutine()
		{

			while (true)
			{
				// couldn't get player activity, wait and retry
				if (null == _activityContext)
				{
					SendLocation(_currentLocation);
					yield return _wait60sec;
					getActivityContext();
					continue;
				}
				// couldn't get gps plugin instance, wait and retry
				if (null == _gpsInstance)
				{
					SendLocation(_currentLocation);
					yield return _wait60sec;
					getGpsInstance();
					continue;
				}

				bool locationServiceAvailable = _gpsInstance.Call<bool>("getIsLocationServiceAvailable");
				_currentLocation.IsLocationServiceEnabled = locationServiceAvailable;

				// app might have been started with location OFF but switched on after start
				// check from time to time
				if (!locationServiceAvailable)
				{
					SendLocation(_currentLocation);
					_gpsInstance.Call("stopLocationListeners");
					yield return _wait5sec;
					_gpsInstance.Call("startLocationListeners", _updateDistanceInMeters, _updateTimeInMilliSeconds);
					yield return _wait1sec;
					continue;
				}


				// if we got till here it means location services are running
				_currentLocation.IsLocationServiceInitializing = false;

				try
				{
					AndroidJavaObject locNetwork = _gpsInstance.Get<AndroidJavaObject>("lastKnownLocationNetwork");
					AndroidJavaObject locGps = _gpsInstance.Get<AndroidJavaObject>("lastKnownLocationGps");

					// easy cases: neither or either gps location or network location available
					if (null == locGps & null == locNetwork) { populateCurrentLocation(null); }
					if (null != locGps && null == locNetwork) { populateCurrentLocation(locGps); }
					if (null == locGps && null != locNetwork) { populateCurrentLocation(locNetwork); }

					// gps- and network location available: figure out which one to use
					if (null != locGps && null != locNetwork) { populateWithBetterLocation(locGps, locNetwork); }


					SendLocation(_currentLocation);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("GPS plugin error: " + ex.ToString());
				}


				yield return _waitUpdateTime;
			}
		}


		private void populateCurrentLocation(AndroidJavaObject location)
		{
			if (null == location)
			{
				_currentLocation.IsHeadingUpdated = false;
				_currentLocation.IsHeadingUpdated = false;
				return;
			}

			double lat = location.Call<double>("getLatitude");
			double lng = location.Call<double>("getLongitude");
			Utils.Vector2d newLatLng = new Utils.Vector2d(lat, lng);
			_currentLocation.IsLocationUpdated = !newLatLng.Equals(_currentLocation.LatitudeLongitude);
			_currentLocation.LatitudeLongitude = newLatLng;
			_currentLocation.Accuracy = location.Call<float>("getAccuracy");
			// divide by 1000. Android uses milliseconds
			_currentLocation.Timestamp = location.Call<long>("getTime") / 1000;
			_currentLocation.Provider = location.Call<string>("getProvider");
			float newHeading = location.Call<float>("getBearing");
			_currentLocation.IsHeadingUpdated = newHeading != _currentLocation.Heading;
			_currentLocation.Heading = newHeading;
			_currentLocation.HeadingAccuracy = location.Call<float>("getBearingAccuracyDegrees");
			_currentLocation.SpeedMetersPerSecond = location.Call<float>("getSpeed");

			bool networkEnabled = _gpsInstance.Get<bool>("isNetworkEnabled");
			bool gpsEnabled = _gpsInstance.Get<bool>("isGpsEnabled");
			if (!gpsEnabled)
			{
				_currentLocation.HasGpsFix = null;
				_currentLocation.SatellitesInView = null;
				_currentLocation.SatellitesUsed = null;
			}
			else
			{
				_currentLocation.HasGpsFix = _gpsInstance.Get<bool>("hasGpsFix");
				//int time2firstFix = _gpsInstance.Get<int>("timeToFirstGpsFix");
				_currentLocation.SatellitesInView = _gpsInstance.Get<int>("satellitesInView");
				_currentLocation.SatellitesUsed = _gpsInstance.Get<int>("satellitesUsedInFix");
			}

		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="locGps"></param>
		/// <param name="locNetwork"></param>
		private void populateWithBetterLocation(AndroidJavaObject locGps, AndroidJavaObject locNetwork)
		{

			// check which location is fresher
			long timestampGps = locGps.Call<long>("getTime");
			long timestampNet = locNetwork.Call<long>("getTime");
			if (timestampGps > timestampNet)
			{
				populateCurrentLocation(locGps);
				return;
			}

			// check which location has better accuracy
			float accuracyGps = locGps.Call<float>("getAccuracy");
			float accuracyNet = locNetwork.Call<float>("getAccuracy");
			if (accuracyGps < accuracyNet)
			{
				populateCurrentLocation(locGps);
				return;
			}

			// default to network
			populateCurrentLocation(locNetwork);
		}



		private void Update()
		{

			/*
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Debug.LogWarning("EXIT");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#else
			if (Application.platform == RuntimePlatform.Android) {
				AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
				//activity.Call<bool>("moveTaskToBack", true);
				activity.Call("finishAndRemoveTask");
				//activity.Call("finish");
			} else {
				Application.Quit();
			}
#endif
			}
			*/
		}

#if UNITY_ANDROID

		private string time2str(AndroidJavaObject loc)
		{
			long time = loc.Call<long>("getTime");
			DateTime dtPlugin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromMilliseconds(time));
			return dtPlugin.ToString("yyyyMMdd HHmmss");
		}



		private string loc2str(AndroidJavaObject loc)
		{

			if (null == loc) { return "loc: NULL"; }

			try
			{

				double lat = loc.Call<double>("getLatitude");
				double lng = loc.Call<double>("getLongitude");

				return string.Format(_invariantCulture, "{0:0.00000000} / {1:0.00000000}", lat, lng);
			}
			catch (Exception ex)
			{
				return ex.ToString();
			}
		}

#endif



	}
}
