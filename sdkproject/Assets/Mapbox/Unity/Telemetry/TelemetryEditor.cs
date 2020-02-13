#if UNITY_EDITOR
namespace Mapbox.Unity.Telemetry
{
	using System.Collections.Generic;
	using System.Collections;
	using Mapbox.Json;
	using System;
	using Mapbox.Unity.Utilities;
	using UnityEngine;
	using System.Text;
	using UnityEditor;
	using UnityEngine.Networking;

	public class TelemetryEditor : ITelemetryLibrary
	{
		string _url;

		static ITelemetryLibrary _instance = new TelemetryEditor();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken)
		{
			_url = string.Format("{0}events/v2?access_token={1}", Mapbox.Utils.Constants.EventsAPI, accessToken);
		}

		public void SendTurnstile()
		{
			var ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
			}
		}

		string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();

			long unixTimestamp = (long)Mapbox.Utils.UnixTimestampUtils.To(DateTime.UtcNow);

			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", unixTimestamp);
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", false);
			jsonDict.Add("sdkIdentifier", GetSDKIdentifier());
			jsonDict.Add("skuId", Constants.SDK_SKU_ID);
			jsonDict.Add("sdkVersion", Constants.SDK_VERSION);
			eventList.Add(jsonDict);

			var jsonString = JsonConvert.SerializeObject(eventList);
			return jsonString;
		}

		bool ShouldPostTurnstile(long ticks)
		{
			var date = new DateTime(ticks);
			var longAgo = DateTime.Now.AddDays(-100).Ticks.ToString();
			var lastDateString = PlayerPrefs.GetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, longAgo);
			long lastTicks = 0;
			long.TryParse(lastDateString, out lastTicks);
			var lastDate = new DateTime(lastTicks);
			var timeSpan = date - lastDate;
			return timeSpan.Days >= 1;
		}

		IEnumerator PostWWW(string url, string bodyJsonString)
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);

#if UNITY_2017_1_OR_NEWER
			UnityWebRequest postRequest = new UnityWebRequest(url, "POST");
			postRequest.SetRequestHeader("Content-Type", "application/json");

			postRequest.downloadHandler = new DownloadHandlerBuffer();
			postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

			yield return postRequest.SendWebRequest();

			while (!postRequest.isDone) { yield return null; }

			if (!postRequest.isNetworkError)
			{
#else
				var headers = new Dictionary<string, string>();
				headers.Add("Content-Type", "application/json");
				headers.Add("user-agent", GetUserAgent());
				var www = new WWW(url, bodyRaw, headers);
				yield return www;

				while (!www.isDone) { yield return null; }

				// www doesn't expose HTTP status code, relay on 'error' property
				if (!string.IsNullOrEmpty(www.error))
				{
#endif
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, "0");
			}
			else
			{
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, DateTime.Now.Ticks.ToString());
			}
		}

		static string GetUserAgent()
		{
			var userAgent = string.Format(
				"{0}/{1}/{2} MapboxEventsUnityEditor/{3}",
				PlayerSettings.applicationIdentifier,
				PlayerSettings.bundleVersion,
#if UNITY_IOS
				PlayerSettings.iOS.buildNumber,
#elif UNITY_ANDROID
				PlayerSettings.Android.bundleVersionCode,
#else
				 "0",
#endif
				 Constants.SDK_VERSION
			);
			return userAgent;
		}

		private string GetSDKIdentifier()
		{
			var sdkIdentifier = string.Format("MapboxEventsUnity{0}",
										  Application.platform
										 );
			return sdkIdentifier;
		}

		public void SetLocationCollectionState(bool enable)
		{
			// Empty.
		}
	}
}
#endif
