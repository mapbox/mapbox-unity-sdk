#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadAndroidAAR : MonoBehaviour
{

	private AndroidJavaObject _activityContext = null;
	private AndroidJavaObject _telemInstance = null;
	private AndroidJavaObject _mapLoadEvent = null;

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
				, "MapboxEventsUnityAndroid"
			);
			Debug.Log("======================= after _telemInstance.Call('initialize' =======================");

			// MapboxTelemetry.getInstance().pushEvent(MapboxEvent.buildMapLoadEvent());
			//_telemInstance.Call

			Debug.Log("======================= get mapbox event call =======================");

			///Hashtable<String, Object> event = MapboxEvent.buildMapLoadEvent();

			using (AndroidJavaClass MapboxAndroidEvent= new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxEvent"))
			{
				if (null == MapboxAndroidEvent)
				{
					Debug.LogError("====== could not get class 'MapboxEvent'");
					return;
				}

				Debug.Log("======================= before MapboxAndroidEvent CallStatic<AndroidJavaObject>('buildMapLoadEvent') =======================");
				_mapLoadEvent = MapboxAndroidEvent.CallStatic<AndroidJavaObject>("buildMapLoadEvent");
				Debug.Log("======================= after MapboxAndroidEvent CallStatic<AndroidJavaObject>('buildMapLoadEvent') =======================");

				_telemInstance.Call ("pushEvent", _mapLoadEvent);

				Debug.Log("======================= after _telemInstance Call<AndroidJavaObject>('pushEvent') =======================");
			}
				
			Debug.Log("======================= after _telemInstance.Call('pushEvent' =======================");
		}

	}

	// Update is called once per frame
	void Update()
	{

	}
}

#endif
