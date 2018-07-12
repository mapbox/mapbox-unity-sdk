namespace Mapbox.Unity.Telemetry
{
	using Mapbox.Json;
	using Mapbox.Unity.Utilities;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;
	using UnityEngine;

	public class TelemetryWebgl : ITelemetryLibrary
	{
		private string _url;
		private static ITelemetryLibrary _instance = new TelemetryFallback();
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
			long ticks = DateTime.Now.Ticks;

			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, ticks.ToString());
			}
		}

		private string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();

			long unixTimestamp = (long)Mapbox.Utils.UnixTimestampUtils.To(DateTime.UtcNow);

			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", unixTimestamp);
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", false);

			// user-agent cannot be set from web broswer, so we send in payload, instead!
			jsonDict.Add("userAgent", GetUserAgent());

			eventList.Add(jsonDict);

			string jsonString = JsonConvert.SerializeObject(eventList);
			return jsonString;
		}

		private bool ShouldPostTurnstile(long ticks)
		{
			DateTime date = new DateTime(ticks);
			string longAgo = DateTime.Now.AddDays(-100).Ticks.ToString();
			string lastDateString = PlayerPrefs.GetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY, longAgo);
			long lastTicks = 0;
			long.TryParse(lastDateString, out lastTicks);
			DateTime lastDate = new DateTime(lastTicks);
			TimeSpan timeSpan = date - lastDate;
			return timeSpan.Days >= 1;
		}

		private IEnumerator PostWWW(string url, string bodyJsonString)
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("Content-Type", "application/json");

			WWW www = new WWW(url, bodyRaw, headers);
			yield return www;
		}

		private static string GetUserAgent()
		{
			string userAgent = string.Format("{0}/{1} {2} MapboxEventsUnity{3}/{4}",
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
