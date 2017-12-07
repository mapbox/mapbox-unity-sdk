namespace Mapbox.Unity.Telemetry
{
	public interface ITelemetryLibrary
	{
		void Initialize(string accessToken);
		void SendTurnstile();
		void SetLocationCollectionState(bool enable);
	}
}