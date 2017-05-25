namespace Mapbox.Unity.Telemetry
{
	public interface ITelemetryLibrary
	{
		//ITelemetryLibrary Instance { get; }
		void Initialize(string accessToken);
		void SendTurnstyle();
	}
}
