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
			_url = string.Format("{0}events/v2?access_token={1}", Mapbox.Utils.Constants.BaseAPI, accessToken);
		}

		public void SendTurnstile()
		{
#if UNITY_EDITOR
			Runnable.EnableRunnableInEditor();
#endif
			Runnable.Run(Post(_url, GetPostBody()));
		}

		string GetPostBody()
		{
			List<Dictionary<string, object>> eventList = new List<Dictionary<string, object>>();
			Dictionary<string, object> jsonDict = new Dictionary<string, object>();
			jsonDict.Add("event", "appUserTurnstile");
			jsonDict.Add("created", DateTime.UtcNow.ToString("o"));
			jsonDict.Add("userId", SystemInfo.deviceUniqueIdentifier);
			jsonDict.Add("enabled.telemetry", false);
			eventList.Add(jsonDict);

			var jsonString = JsonConvert.SerializeObject(eventList);
			return jsonString;
		}

		IEnumerator Post(string url, string bodyJsonString)
		{
			var request = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);

			// FIXME: Why, Unity?!
			//request.SetRequestHeader("user-agent", "MapboxEventsUnityEditor");

			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");

			yield return request.Send();

			Debug.Log("Status Code: " + request.responseCode);
			Debug.Log("Status Code: " + request.downloadHandler.text);
		}
	}
}