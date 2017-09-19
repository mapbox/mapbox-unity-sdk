﻿#if UNITY_EDITOR
namespace Mapbox.Unity.Telemetry
{
	using System.Collections.Generic;
	using System.Collections;
	using Mapbox.Json;
	using System;
	using Mapbox.Unity.Utilities;
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Text;

	using UnityEditor;

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
			// This is only needed for maps at design-time.
			//Runnable.EnableRunnableInEditor();

			var ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, ticks.ToString());
			}
		}

		string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();

			long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", unixTimestamp);
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", false);
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

		// FIXME: maybe in a future Unity version, "user-agent" will be writable. 
		IEnumerator Post(string url, string bodyJsonString)
		{
			var request = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);

			// FIXME: Why, Unity?! 
			// https://docs.unity3d.com/2017.1/Documentation/ScriptReference/Networking.UnityWebRequest.SetRequestHeader.html
			//request.SetRequestHeader("user-agent", GetUserAgent());

			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");

			yield return request.Send();
		}

		IEnumerator PostWWW(string url, string bodyJsonString)
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			var headers = new Dictionary<string, string>();
			headers.Add("Content-Type", "application/json");
			headers.Add("user-agent", GetUserAgent());

			var www = new WWW(url, bodyRaw, headers);
			yield return www;
		}

		static string GetUserAgent()
		{
			var userAgent = string.Format("{0}/{1}/{2} MapboxEventsUnityEditor/{3}",
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

		public void SetLocationCollectionState(bool enable)
		{
			// Empty.
		}
	}
}
#endif