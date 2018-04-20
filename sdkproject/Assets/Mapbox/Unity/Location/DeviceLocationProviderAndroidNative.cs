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
					yield return _wait60sec;
					getActivityContext();
					continue;
				}
				// couldn't get gps plugin instance, wait and retry
				if (null == _gpsInstance)
				{
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
					_gpsInstance.Call("stopLocationListeners");
					yield return _wait5sec;
					_gpsInstance.Call("startLocationListeners", _updateDistanceInMeters, _updateTimeInMilliSeconds);
					yield return _wait1sec;
					continue;
				}


				// if we got till here it means location services are running
				_currentLocation.IsLocationServiceInitializing = false;


				bool networkEnabled = _gpsInstance.Get<bool>("isNetworkEnabled");
				bool gpsEnabled = _gpsInstance.Get<bool>("isGpsEnabled");
				bool hasFix = _gpsInstance.Get<bool>("hasGpsFix");
				int time2firstFix = _gpsInstance.Get<int>("timeToFirstGpsFix");
				int satsInView = _gpsInstance.Get<int>("satellitesInView");
				int satsUsed = _gpsInstance.Get<int>("satellitesUsedInFix");


				try
				{

					AndroidJavaObject locNetwork = _gpsInstance.Get<AndroidJavaObject>("lastKnownLocationNetwork");
					AndroidJavaObject locGps = _gpsInstance.Get<AndroidJavaObject>("lastKnownLocationGps");

					if (null != locGps)
					{
						float acc = locGps.Call<float>("getAccuracy");
						// TODO: getBearingAccuracyDegrees
						float hdg = locGps.Call<float>("getBearing");
					}

					if (null != locNetwork)
					{
						float accNetwork = locNetwork.Call<float>("getAccuracy");
						float hdg = locNetwork.Call<float>("getBearing");
					}

					// TODO evaluate which location to send
					// just use GPS during backporting
					if (null != locGps)
					{
						double lat = locGps.Call<double>("getLatitude");
						double lng = locGps.Call<double>("getLongitude");
						_currentLocation.LatitudeLongitude = new Utils.Vector2d(lat, lng);
						_currentLocation.Accuracy= (int)locGps.Call<float>("getAccuracy");
						SendLocation(_currentLocation);
					}

				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("GPS plugin error: " + ex.ToString());
				}


				yield return _wait1sec;
			}
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
