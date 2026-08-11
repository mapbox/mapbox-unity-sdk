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
	/// <summary>
	/// Pure-C# telemetry for Windows standalone player builds. Mirrors the Editor
	/// implementation (HTTP POST of the appUserTurnstile event to the Mapbox events API)
	/// but uses only runtime APIs and reports the real telemetry-collection state rather
	/// than the hardcoded value the generic fallback uses. Needed because native
	/// mapbox-common has no Windows binary, so there is no native telemetry path here.
	/// </summary>
	public class TelemetryWindows : ITelemetryLibrary
	{
		string _url;

		// Reflects the user's telemetry opt-in/out (set via SetLocationCollectionState).
		// Defaults to the same value as MapboxConfiguration.TelemetryEnabled.
		bool _telemetryEnabled = true;

		static ITelemetryLibrary _instance = new TelemetryWindows();
		public static ITelemetryLibrary Instance
		{
			get { return _instance; }
		}

		public void Initialize(string accessToken)
		{
			_url = string.Format("{0}events/v2?access_token={1}", Constants.Map.EventsAPI, accessToken);
		}

		public void SetLocationCollectionState(bool enable)
		{
			_telemetryEnabled = enable;
		}

		public void SendTurnstile()
		{
			var ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				// The turnstile timestamp is written inside PostWWW based on the actual
				// request result, so a failed send retries on the next launch.
				Runnable.Run(PostWWW(_url, GetPostBody()));
			}
		}

		public void SendSdkEvent()
		{
			// No SDK/billing event on the C# path (native-only, as on the Editor path).
		}

		string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();

			// ISO-8601 UTC string (e.g. 2026-07-27T12:34:56.789Z) to match the format the
			// native/JS SDKs send; the events API rejects a numeric epoch value.
			var created = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", created);
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", _telemetryEnabled);
			jsonDict.Add("sdkIdentifier", Constants.SDK_IDENTIFIER);
			jsonDict.Add("skuId", Constants.SDK_SKU_ID);
			jsonDict.Add("sdkVersion", Constants.SDK_VERSION);
			jsonDict.Add("operatingSystem", SystemInfo.operatingSystem);
			eventList.Add(jsonDict);

			return JsonConvert.SerializeObject(eventList);
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

			UnityWebRequest postRequest = new UnityWebRequest(url, "POST");
			postRequest.SetRequestHeader("Content-Type", "application/json");
			postRequest.downloadHandler = new DownloadHandlerBuffer();
			postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

			yield return postRequest.SendWebRequest();

			if (postRequest.result == UnityWebRequest.Result.ConnectionError)
			{
				// Couldn't reach the events API — leave the timestamp so the next launch retries.
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, "0");
				Debug.LogWarning("Mapbox turnstile telemetry could not reach the events API: " + postRequest.error);
			}
			else
			{
				// Reached the server (204 expected on success) — mark as sent for today.
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, DateTime.Now.Ticks.ToString());
				if (postRequest.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning(string.Format("Mapbox turnstile telemetry rejected by events API: {0} {1}",
						postRequest.responseCode, postRequest.error));
				}
			}
		}
	}
}
