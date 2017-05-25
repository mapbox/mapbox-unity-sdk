
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadAndroidAAR : MonoBehaviour
{

	private AndroidJavaObject _activityContext = null;
	private AndroidJavaObject _telemInstance = null;

	void Start()
	{
		TelemetryInitialize();
		TelemetryPushMapLoadEvent();
	}


	int _previousSecond = -1;
	void Update()
	{
		// JUST FOR TESTING
		// FIRE EVENTS EVERY FEW SECONDS
		int second = System.DateTime.Now.Second;
		if (_previousSecond == second) { return; }
		_previousSecond = second;

		// beware: only postive coordinates (NE hemisphere) are generated
		System.Random random = new System.Random((int)System.DateTime.Now.Ticks);
		double lng = random.NextDouble() * 180d;
		double lat = random.NextDouble() * 90d;
		double zoom = System.Math.Floor(random.NextDouble() * 20d);

		if (0 == second % 7) { TelemetryPushMapCllickEvent(lng, lat, zoom); }
		if (0 == second % 11) { TelemetryPushMapDragEndEvent(lng, lat, zoom); }
	}


	void TelemetryInitialize()
	{
		// Already initialized
		if (null != _telemInstance) { return; }


#if UNITY_ANDROID && !UNITY_EDITOR
		using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			_activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
		}

		if (null == _activityContext)
		{
			Debug.LogError("Could not get current activity");
			return;
		}

		using (AndroidJavaClass MapboxAndroidTelem = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxTelemetry"))
		{
			if (null == MapboxAndroidTelem)
			{
				Debug.LogError("Could not get class 'MapboxTelemetry'");
				return;
			}

			_telemInstance = MapboxAndroidTelem.CallStatic<AndroidJavaObject>("getInstance");
			if (null == _telemInstance)
			{
				Debug.LogError("Could not get MapboxTelemetry instance");
				return;
			}

			_telemInstance.Call(
				"initialize"
				, _activityContext
				, Mapbox.Unity.MapboxAccess.Instance.AccessToken
				, "MapboxEventsUnityAndroid"
			);
		}
#endif
	}



	void TelemetryPushMapLoadEvent()
	{
		if (null == _telemInstance) { return; }

		using (AndroidJavaClass MapboxAndroidEvent = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxEvent"))
		{
			if (null == MapboxAndroidEvent)
			{
				Debug.LogError("Could not get class 'MapboxEvent'");
				return;
			}

			AndroidJavaObject mapLoadEvent = MapboxAndroidEvent.CallStatic<AndroidJavaObject>("buildMapLoadEvent");
			_telemInstance.Call("pushEvent", mapLoadEvent);
		}

	}



	void TelemetryPushMapCllickEvent(double longitude, double latitude, double zoom)
	{
		Debug.Log(string.Format("======== TelemetryPushMapCllickEvent() lng:{0} lat:{1} z:{2} ========", longitude, latitude, zoom));
		if (null == _telemInstance)
		{
			Debug.LogError("Telemetry not initialized");
			return;
		}


		using (AndroidJavaClass MapboxAndroidEvent = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxEvent"))
		{
			if (null == MapboxAndroidEvent)
			{
				Debug.LogError("Could not get class 'MapboxEvent'");
				return;
			}


			// don't know how to do: 'MainActivity.class.getSimpleName()' https://github.com/mapbox/mapbox-telemetry-android/blob/master/telemetry/app/src/main/java/com/mapbox/telemetry/MainActivity.java#L161
			string simpleName = "UnityActivitySimpleName";

			try
			{
				//AndroidJavaObject activityClass = _activityContext.CallStatic("getClass");
				//AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				//AndroidJavaObject ac = activityClass.Get<AndroidJavaObject>("class");
				AndroidJavaObject ac = _activityContext.Get<AndroidJavaObject>("getClass");
				simpleName = ac.Call<string>("getSimpleName");
				Debug.Log("simpleName: " + simpleName);
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex);
			}


			using (AndroidJavaObject androidLocation = new AndroidJavaObject("android.location.Location", simpleName))
			{
				androidLocation.Call("setLongitude", longitude);
				androidLocation.Call("setLatitude", latitude);

				AndroidJavaObject mapClickEvent = MapboxAndroidEvent.CallStatic<AndroidJavaObject>(
					"buildMapClickEvent",
					androidLocation,
					"GESTURE_ID_CLICK",
					zoom
				);

				_telemInstance.Call("pushEvent", mapClickEvent);
			}
		}
	}



	void TelemetryPushMapDragEndEvent(double longitude, double latitude, double zoom)
	{
		Debug.Log(string.Format("======== TelemetryPushMapDragEnd() lng:{0} lat:{1} z:{2} ========", longitude, latitude, zoom));
		if (null == _telemInstance)
		{
			Debug.LogError("Telemetry not initialized");
			return;
		}


		using (AndroidJavaClass MapboxAndroidEvent = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxEvent"))
		{
			if (null == MapboxAndroidEvent)
			{
				Debug.LogError("Could not get class 'MapboxEvent'");
				return;
			}


			// don't know how to do: 'MainActivity.class.getSimpleName()' https://github.com/mapbox/mapbox-telemetry-android/blob/master/telemetry/app/src/main/java/com/mapbox/telemetry/MainActivity.java#L161
			string simpleName = "UnityActivitySimpleName";


			using (AndroidJavaObject androidLocation = new AndroidJavaObject("android.location.Location", simpleName))
			{
				androidLocation.Call("setLongitude", longitude);
				androidLocation.Call("setLatitude", latitude);

				AndroidJavaObject mapDragEndEvent = MapboxAndroidEvent.CallStatic<AndroidJavaObject>(
					"buildMapDragEndEvent",
					androidLocation,
					zoom
				);

				_telemInstance.Call("pushEvent", mapDragEndEvent);
			}
		}
	}


}

