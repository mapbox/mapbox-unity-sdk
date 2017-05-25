#if UNITY_ANDROID
namespace Mapbox.Unity.Telemetry
{
	using System.Runtime.InteropServices;

	public static class TelemetryAndroid
	{
		[DllImport("__Internal")]
		private static extern void initialize(string accessToken, string userAgentBase);

		[DllImport("__Internal")]
		private static extern void sendTurnstyleEvent();

		public static void SendTurnstyle(string accessToken)
		{
			//initialize(accessToken, "MapboxEventsUnityiOS");
   //         sendTurnstyleEvent();
		}
	}
}
#endif