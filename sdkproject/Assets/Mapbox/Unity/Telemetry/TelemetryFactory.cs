namespace Mapbox.Unity.Telemetry
{
	public static class TelemetryFactory
	{
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
		public static readonly string EventQuery = "events=true";
#else
		public static readonly string EventQuery = "events=false";
#endif

		public static ITelemetryLibrary GetTelemetryInstance()
		{
#if UNITY_EDITOR
			return TelemetryEditor.Instance;
#elif UNITY_IOS
			return TelemetryIos.Instance;
#elif UNITY_ANDROID
			return TelemetryAndroid.Instance;
#elif UNITY_WEBGL
			return TelemetryWebgl.Instance;
#else
			return TelemetryFallback.Instance;
#endif
		}
	}
}