namespace Mapbox.Unity.Location
{
	using UnityEngine;
	using System.Collections;
	using System.Globalization;
	using System;
	using System.IO;
	using System.Text;
	using Mapbox.Utils;

	public class DeviceLocationProviderAndroidNative : AbstractLocationProvider, IDisposable
	{


		/// <summary>
		/// The minimum distance (measured in meters) a device must move laterally before location is updated. 
		/// https://developer.android.com/reference/android/location/LocationManager.html#requestLocationUpdates(java.lang.String,%20long,%20float,%20android.location.LocationListener)
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum distance (measured in meters) a device must move laterally before location is updated. Higher values like 500 imply less overhead.")]
		float _updateDistanceInMeters = 0.0f;


		/// <summary>
		/// The minimum time interval between location updates, in milliseconds.
		/// https://developer.android.com/reference/android/location/LocationManager.html#requestLocationUpdates(java.lang.String,%20long,%20float,%20android.location.LocationListener)
		/// </summary>
		[SerializeField]
		[Tooltip("The minimum time interval between location updates, in milliseconds. It's reasonable to not go below 500ms.")]
		long _updateTimeInMilliSeconds = 1000;


		private WaitForSeconds _wait1sec;
		private WaitForSeconds _wait5sec;
		private WaitForSeconds _wait60sec;
		/// <summary>polls location provider only at the requested update intervall to reduce load</summary>
		private WaitForSeconds _waitUpdateTime;
		private bool _disposed;
		private static object _lock = new object();
		private Coroutine _pollLocation;

		private AndroidJavaObject _activityContext = null;
		private AndroidJavaObject _gpsInstance;
		private AndroidJavaObject _sensorInstance;


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
					if (null != _sensorInstance)
					{
						_sensorInstance.Call("stopSensorListeners");
						_sensorInstance.Dispose();
						_sensorInstance = null;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}


		protected virtual void OnDestroy() { shutdown(); }


		protected virtual void OnDisable() { shutdown(); }

		protected virtual void Awake()
		{
			// safe measures to not run when disabled or not selected as location provider
			if (!enabled) { return; }
			if (!transform.gameObject.activeInHierarchy) { return; }


			_wait1sec = new WaitForSeconds(1);
			_wait5sec = new WaitForSeconds(5);
			_wait60sec = new WaitForSeconds(60);
			// throttle if entered update intervall is unreasonably low
			_waitUpdateTime = _updateTimeInMilliSeconds < 500 ? new WaitForSeconds(0.5f) : new WaitForSeconds((float)_updateTimeInMilliSeconds / 1000.0f);

			_currentLocation.IsLocationServiceEnabled = false;
			_currentLocation.IsLocationServiceInitializing = true;

			if (Application.platform == RuntimePlatform.Android)
			{
				getActivityContext();
				getGpsInstance(true);
				getSensorInstance();

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
					return;
				}

				_gpsInstance = androidGps.CallStatic<AndroidJavaObject>("instance", _activityContext);
				if (null == _gpsInstance)
				{
					Debug.LogError("Could not get 'AndroidGps' instance");
					return;
				}

				_activityContext.Call("runOnUiThread", new AndroidJavaRunnable(() => { _gpsInstance.Call("showMessage", "starting location listeners"); }));

				_gpsInstance.Call("startLocationListeners", _updateDistanceInMeters, _updateTimeInMilliSeconds);
			}
		}


		private void getSensorInstance()
		{
			if (null == _activityContext) { return; }

			using (AndroidJavaClass androidSensors = new AndroidJavaClass("com.mapbox.android.unity.AndroidSensors"))
			{
				if (null == androidSensors)
				{
					Debug.LogError("Could not get class 'AndroidSensors'");
					return;
				}

				_sensorInstance = androidSensors.CallStatic<AndroidJavaObject>("instance", _activityContext);
				if (null == _sensorInstance)
				{
					Debug.LogError("Could not get 'AndroidSensors' instance");
					return;
				}

				_sensorInstance.Call("startSensorListeners");
			}
		}

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

				// update device orientation
				if (null != _sensorInstance)
				{
					_currentLocation.DeviceOrientation = _sensorInstance.Call<float>("getOrientation");
				}

				bool locationServiceAvailable = _gpsInstance.Call<bool>("getIsLocationServiceAvailable");
				_currentLocation.IsLocationServiceEnabled = locationServiceAvailable;

				// app might have been started with location OFF but switched on after start
				// check from time to time
				if (!locationServiceAvailable)
				{
					_currentLocation.IsLocationServiceInitializing = true;
					_currentLocation.Accuracy = 0;
					_currentLocation.HasGpsFix = false;
					_currentLocation.SatellitesInView = 0;
					_currentLocation.SatellitesUsed = 0;

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


					_currentLocation.TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow);
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
				_currentLocation.IsLocationUpdated = false;
				_currentLocation.IsUserHeadingUpdated = false;
				return;
			}

			double lat = location.Call<double>("getLatitude");
			double lng = location.Call<double>("getLongitude");
			Utils.Vector2d newLatLng = new Utils.Vector2d(lat, lng);
			bool coordinatesUpdated = !newLatLng.Equals(_currentLocation.LatitudeLongitude);
			_currentLocation.LatitudeLongitude = newLatLng;

			float newAccuracy = location.Call<float>("getAccuracy");
			bool accuracyUpdated = newAccuracy != _currentLocation.Accuracy;
			_currentLocation.Accuracy = newAccuracy;

			// divide by 1000. Android returns milliseconds, we work with seconds
			long newTimestamp = location.Call<long>("getTime") / 1000;
			bool timestampUpdated = newTimestamp != _currentLocation.Timestamp;
			_currentLocation.Timestamp = newTimestamp;

			string newProvider = location.Call<string>("getProvider");
			bool providerUpdated = newProvider != _currentLocation.Provider;
			_currentLocation.Provider = newProvider;

			bool hasBearing = location.Call<bool>("hasBearing");
			// only evalute bearing when location object actually has a bearing
			// Android populates bearing (which is not equal to device orientation)
			// only when the device is moving.
			// Otherwise it is set to '0.0'
			// https://developer.android.com/reference/android/location/Location.html#getBearing()
			// We don't want that when we rotate a map according to the direction
			// thes user is moving, thus don't update 'heading' with '0.0' 
			if (!hasBearing)
			{
				_currentLocation.IsUserHeadingUpdated = false;
			}
			else
			{
				float newHeading = location.Call<float>("getBearing");
				_currentLocation.IsUserHeadingUpdated = newHeading != _currentLocation.UserHeading;
				_currentLocation.UserHeading = newHeading;
			}

			float? newSpeed = location.Call<float>("getSpeed");
			bool speedUpdated = newSpeed != _currentLocation.SpeedMetersPerSecond;
			_currentLocation.SpeedMetersPerSecond = newSpeed;

			// flag location as updated if any of below conditions evaluates to true
			// Debug.LogFormat("coords:{0} acc:{1} time:{2} speed:{3}", coordinatesUpdated, accuracyUpdated, timestampUpdated, speedUpdated);
			_currentLocation.IsLocationUpdated =
				providerUpdated
				|| coordinatesUpdated
				|| accuracyUpdated
				|| timestampUpdated
				|| speedUpdated;

			// Un-comment if required. Throws a warning right now. 
			//bool networkEnabled = _gpsInstance.Call<bool>("getIsNetworkEnabled");
			bool gpsEnabled = _gpsInstance.Call<bool>("getIsGpsEnabled");
			if (!gpsEnabled)
			{
				_currentLocation.HasGpsFix = null;
				_currentLocation.SatellitesInView = null;
				_currentLocation.SatellitesUsed = null;
			}
			else
			{
				_currentLocation.HasGpsFix = _gpsInstance.Get<bool>("hasGpsFix");
				_currentLocation.SatellitesInView = _gpsInstance.Get<int>("satellitesInView");
				_currentLocation.SatellitesUsed = _gpsInstance.Get<int>("satellitesUsedInFix");
			}

		}


		/// <summary>
		/// If GPS and network location are available use the newer/better one
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

				return string.Format(CultureInfo.InvariantCulture, "{0:0.00000000} / {1:0.00000000}", lat, lng);
			}
			catch (Exception ex)
			{
				return ex.ToString();
			}
		}

#endif



	}
}
