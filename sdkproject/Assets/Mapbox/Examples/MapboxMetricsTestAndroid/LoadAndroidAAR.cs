
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


	int _previousSecond=-1;
	void Update()
	{
		// JUST FOR TESTING
		// FIRE EVENTS EVERY FEW SECONDS
		int second = System.DateTime.Now.Second;
		if (_previousSecond == second) { return; }
		_previousSecond = second;

		if (0 == second % 21) { TelemetryPushMapCllickEvent(); }
		if (0 == second % 27) { TelemetryPushMapDragEndEvent(); }
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


	void TelemetryPushMapCllickEvent()
	{
		Debug.Log("======== TelemetryPushMapCllickEvent() ========");
		if (null == _telemInstance)
		{
			Debug.LogError("Telemetry not initialized");
			return;
		}
	}


	void TelemetryPushMapDragEndEvent()
	{
		Debug.Log("======== TelemetryPushMapDragEnd() ========");
		if (null == _telemInstance)
		{
			Debug.LogError("Telemetry not initialized");
			return;
		}

	}


}

