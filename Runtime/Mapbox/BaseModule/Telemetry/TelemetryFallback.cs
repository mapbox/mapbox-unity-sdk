using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mapbox.BaseModule.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.BaseModule.Telemetry
{
	public class TelemetryFallback : ITelemetryLibrary
	{
		string _url;

		static ITelemetryLibrary _instance = new TelemetryFallback();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken, Func<string> getSkuToken)
		{
			_url = string.Format("{0}events/v2?access_token={1}", Constants.Map.EventsAPI, accessToken);
		}

		public void SendTurnstile()
		{
			var ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, ticks.ToString());
			}
		}
		
		public void SendSdkEvent()
		{
		}

		string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();

			// ISO-8601 UTC string (e.g. 2026-07-27T12:34:56.789Z) to match the format
			// the native/JS SDKs send; the events API rejects a numeric epoch value.
			var created = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", created);
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", false);
			jsonDict.Add("sdkIdentifier", Constants.SDK_IDENTIFIER);
			jsonDict.Add("skuId", Constants.SDK_SKU_ID);
			jsonDict.Add("sdkVersion", Constants.SDK_VERSION);
			jsonDict.Add("operatingSystem", SystemInfo.operatingSystem);
			eventList.Add(jsonDict);

			var jsonString = JsonConvert.SerializeObject(eventList);
			return jsonString;
		}

		bool ShouldPostTurnstile(long ticks)
		{
			var date = new DateTime(ticks);
			var longAgo = DateTime.Now.AddDays(-100).Ticks.ToString();
			var lastDateString = PlayerPrefs.GetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, longAgo);
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
#else
			var headers = new Dictionary<string, string>();
			headers.Add("Content-Type", "application/json");
			headers.Add("user-agent", GetUserAgent());
			var www = new WWW(url, bodyRaw, headers);
			yield return www;
#endif
		}

		static string GetUserAgent()
		{
			var userAgent = string.Format("{0}/{1}/{2} MapboxEventsUnity{3}/{4}",
										  Application.identifier,
										  Application.version,
										  "0",
										  Application.platform,
										  Constants.SDK_VERSION
										 );
			return userAgent;
		}

		public void SetLocationCollectionState(bool enable)
		{
			// empty.
		}
	}
}