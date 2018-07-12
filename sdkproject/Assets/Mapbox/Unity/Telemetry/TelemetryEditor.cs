#if UNITY_EDITOR
namespace Mapbox.Unity.Telemetry
{
	using Mapbox.Json;
	using Mapbox.Unity.Utilities;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;
	using UnityEditor;
	using UnityEngine;
#if MAPBOX_EXPERIMENTAL
	using Mapbox.Experimental.Platform.Http;
#endif

	public class TelemetryEditor : ITelemetryLibrary
	{
		private string _url;
		private static ITelemetryLibrary _instance = new TelemetryEditor();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken)
		{
#if MAPBOX_EXPERIMENTAL
			// don't need to add accessToken, gets added automatically in MapboxHttpRequest
			_url = $"{Mapbox.Utils.Constants.EventsAPI}events/v2";
#else
			_url = string.Format("{0}events/v2?access_token={1}", Mapbox.Utils.Constants.EventsAPI, accessToken);
#endif
		}

		public void SendTurnstile()
		{
			// This is only needed for maps at design-time.
			//Runnable.EnableRunnableInEditor();

			long ticks = DateTime.Now.Ticks;
			if (ShouldPostTurnstile(ticks))
			{
				Runnable.Run(PostWWW(_url, GetPostBody()));
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
			eventList.Add(jsonDict);

			string jsonString = JsonConvert.SerializeObject(eventList);
			return jsonString;
		}

		private bool _isPostingTurnstile = false;
		private bool ShouldPostTurnstile(long ticks)
		{
			if (_isPostingTurnstile) { return false; }
			//FOR DEBUGGING
			//return true;

			DateTime date = new DateTime(ticks);
			string longAgo = DateTime.Now.AddDays(-100).Ticks.ToString();
			string lastDateString = PlayerPrefs.GetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, longAgo);
			long lastTicks = 0;
			long.TryParse(lastDateString, out lastTicks);
			DateTime lastDate = new DateTime(lastTicks);
			TimeSpan timeSpan = date - lastDate;
			return timeSpan.Days >= 1;
		}

#if MAPBOX_EXPERIMENTAL
		private IEnumerator PostWWW(string url, string bodyJsonString)
		{
			_isPostingTurnstile = true;

			Action asyncWorkaround = async () =>
			{

				Dictionary<string, string> headers = new Dictionary<string, string>();
				//headers.Add("Content-Type", "application/json");
				headers.Add("User-Agent", GetUserAgent());

				// TODO: verify, Mapbox access might not yet be fully initialised when
				// we get here: verify is there could be a circular race condition
				// currrent workaround via '_isPostingTurnstile'
				MapboxHttpRequest request = await MapboxAccess.Instance.Request(
					MapboxWebDataRequestType.Telemetry
					, null
					, MapboxHttpMethod.Post
					, _url
					, bodyJsonString
					, headers
				);
				MapboxHttpResponse response = await request.GetResponseAsync();

				if (response.HasError)
				{
					PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, "0");
				}
				else
				{
					// FOR DEBUGGING
					//PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, "0");
					PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, DateTime.Now.Ticks.ToString());
				}
				_isPostingTurnstile = false;
			};
			asyncWorkaround();

			while (_isPostingTurnstile) { yield return null; }
		}
#else
		IEnumerator PostWWW(string url, string bodyJsonString)
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			var headers = new Dictionary<string, string>();
			headers.Add("Content-Type", "application/json");
			headers.Add("user-agent", GetUserAgent());

			var www = new WWW(url, bodyRaw, headers);
			yield return www;
			while (!www.isDone) { yield return null; }

			// www doesn't expose HTTP status code, relay on 'error' property
			if (!string.IsNullOrEmpty(www.error))
			{
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, "0");
			}
			else
			{
				PlayerPrefs.SetString(Constants.Path.TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY, DateTime.Now.Ticks.ToString());
			}
		}
#endif

		private static string GetUserAgent()
		{
			string userAgent = string.Format(
				"{0}/{1} {2} MapboxEventsUnityEditor/{3}",
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
