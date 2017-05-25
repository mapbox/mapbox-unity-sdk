﻿#if UNITY_IOS
namespace Mapbox.Unity.Telemetry
{
	using System.Runtime.InteropServices;

	public class TelemetryIos : ITelemetryLibrary
	{
		[DllImport("__Internal")]
		static extern void initialize(string accessToken, string userAgentBase);

		[DllImport("__Internal")]
		static extern void sendTurnstyleEvent();

		static ITelemetryLibrary _instance = new TelemetryIos();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken)
		{
			initialize(accessToken, "MapboxEventsUnityiOS");
		}

		public void SendTurnstyle()
		{
			sendTurnstyleEvent();
		}
	}
}
#endif