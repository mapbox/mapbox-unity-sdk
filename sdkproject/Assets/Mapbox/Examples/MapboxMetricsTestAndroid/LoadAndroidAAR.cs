#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadAndroidAAR : MonoBehaviour
{

	private AndroidJavaObject _activityContext = null;
	private AndroidJavaObject _telemInstance = null;

	void Start()
	{
		if (null != _telemInstance) { return; }

		using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			_activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
		}

		Debug.Log("======================= BEFORE new AndroidJavaClass =======================");
		using (AndroidJavaClass MapboxAndroidTelem = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxTelemetry"))
		{
			Debug.Log("======================= AFTER new AndroidJavaClass =======================");
			if (null == MapboxAndroidTelem)
			{
				Debug.LogError("====== could not get class 'MapboxTelemetry'");
				return;
			}

			Debug.Log("======================= before CallStatic<AndroidJavaObject>('getInstance') =======================");
			_telemInstance = MapboxAndroidTelem.CallStatic<AndroidJavaObject>("getInstance");
			Debug.Log("======================= after CallStatic<AndroidJavaObject>('getInstance') =======================");

			Debug.Log("null==_telemInstance:" + (null == _telemInstance));

			Debug.Log("======================= before _telemInstance.Call('initialize' =======================");
			_telemInstance.Call(
				"initialize"
				, _activityContext
				, Mapbox.Unity.MapboxAccess.Instance.AccessToken
				, "unity-sdk-android"
			);
			Debug.Log("======================= after _telemInstance.Call('initialize' =======================");


		}

	}

	// Update is called once per frame
	void Update()
	{

	}
}

#endif
