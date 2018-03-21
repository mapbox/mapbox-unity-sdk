#if UNITY_ANDROID
namespace Mapbox.Unity.Telemetry
{
	using UnityEngine;

	public class TelemetryAndroid : ITelemetryLibrary
	{
		AndroidJavaObject _activityContext = null;
		AndroidJavaObject _telemInstance = null;

		static ITelemetryLibrary _instance = new TelemetryAndroid();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken)
		{
			if (string.IsNullOrEmpty(accessToken))
			{
				throw new System.ArgumentNullException("accessToken");
			}

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
					, accessToken
					, "MapboxEventsUnityAndroid/" + Constants.SDK_VERSION
				);
			}
		}

		public void SendTurnstile()
		{
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

		public void SetLocationCollectionState(bool enable)
		{
			_telemInstance.Call(
				"setTelemetryEnabled"
				, enable
			);
		}
	}
}
#endif
