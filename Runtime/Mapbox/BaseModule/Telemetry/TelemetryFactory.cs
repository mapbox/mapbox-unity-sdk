namespace Mapbox.BaseModule.Telemetry
{
	public static class TelemetryFactory
	{
		public static ITelemetryLibrary GetTelemetryInstance()
		{
#if UNITY_EDITOR
			return TelemetryEditor.Instance;
#elif UNITY_ANDROID
			return TelemetryAndroid.Instance;
#elif UNITY_IOS
			return TelemetryIos.Instance;
#elif UNITY_STANDALONE_WIN
			return TelemetryWindows.Instance;
#else
			return TelemetryFallback.Instance;
#endif
		}
	}
}
