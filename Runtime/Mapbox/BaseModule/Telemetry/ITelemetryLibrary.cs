using System;

namespace Mapbox.BaseModule.Telemetry
{
	public interface ITelemetryLibrary
	{
		void Initialize(string accessToken, Func<string> getSkuToken);
		void SendTurnstile();
		void SetLocationCollectionState(bool enable);
		void SendSdkEvent();
	}
}
