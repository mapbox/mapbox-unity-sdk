using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mapbox.BaseModule.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
namespace Mapbox.BaseModule.Telemetry
{
	public class TelemetryEditor : ITelemetryLibrary
	{
		
		string _url;
		string _accessToken;
		Func<string> _getSkuToken;

		static ITelemetryLibrary _instance = new TelemetryEditor();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken, Func<string> getSkuToken)
		{
			_accessToken = accessToken;
			_getSkuToken = getSkuToken;
			_url = string.Format("{0}events/v2?access_token={1}", Constants.Map.EventsAPI, accessToken);
		}

		public void SendTurnstile()
		{
			var ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
			}
		}

		public void SendSdkEvent()
		{
			// Billing/session ping — the C# equivalent of the native triggerUserBillingEvent.
			// GET api.mapbox.com/sdk-sessions/v1?access_token=..&sku=<user SKU token>. Fires once
			// per session (each map-context init), NOT throttled like the turnstile.
			if (_getSkuToken == null || string.IsNullOrEmpty(_accessToken))
			{
				return;
			}

			var url = string.Format("{0}sdk-sessions/v1?access_token={1}&sku={2}",
				Constants.Map.BaseAPI, _accessToken, _getSkuToken());
			Runnable.Run(GetSdkSession(url));
		}

		IEnumerator GetSdkSession(string url)
		{
			using (var request = UnityWebRequest.Get(url))
			{
				yield return request.SendWebRequest();
				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning(string.Format("Mapbox SDK event failed: {0} {1}",
						request.responseCode, request.error));
				}
			}
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

			UnityWebRequest postRequest = new UnityWebRequest(url, "POST");
			postRequest.SetRequestHeader("Content-Type", "application/json");
			postRequest.downloadHandler = new DownloadHandlerBuffer();
			postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

			yield return postRequest.SendWebRequest();

			if (postRequest.result == UnityWebRequest.Result.ConnectionError)
			{
				// Couldn't reach the events API — leave the timestamp so the next session retries.
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, "0");
				Debug.LogWarning("Mapbox turnstile telemetry could not reach the events API: " + postRequest.error);
			}
			else
			{
				// Reached the server (204 expected). Record now so the turnstile is sent at most
				// once per day (Mapbox's spec) rather than on every session. NOTE: the previous
				// code had these two branches swapped, which reset the throttle on success and so
				// re-sent the turnstile every session.
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, DateTime.Now.Ticks.ToString());
				if (postRequest.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning(string.Format("Mapbox turnstile telemetry rejected by events API: {0} {1}",
						postRequest.responseCode, postRequest.error));
				}
			}
		}

		public void SetLocationCollectionState(bool enable)
		{
			// Empty.
		}
	}
}
#endif
