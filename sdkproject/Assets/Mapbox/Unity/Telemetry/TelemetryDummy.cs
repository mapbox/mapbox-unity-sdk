namespace Mapbox.Unity.Telemetry
{
	public class TelemetryDummy : ITelemetryLibrary
	{
		static ITelemetryLibrary _instance = new TelemetryDummy();
		public static ITelemetryLibrary Instance
		{
			get
			{
				return _instance;
			}
		}

		public void Initialize(string accessToken)
		{
			// empty.
		}

		public void SendTurnstile()
		{
			// empty.
		}

		public void SetLocationCollectionState(bool enable)
		{
			// empty.
		}
	}
}
