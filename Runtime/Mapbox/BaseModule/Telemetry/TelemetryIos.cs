

using System;
using UnityEngine;

#if UNITY_IOS
namespace Mapbox.BaseModule.Telemetry
{
	using System.Runtime.InteropServices;
	using Mapbox.BaseModule.Utilities;

	public class TelemetryIos : ITelemetryLibrary
	{
		private IntPtr _telemetryService = IntPtr.Zero;
		
		[DllImport("__Internal")] private static extern void setAccessTokenForToken(string accessToken);
		[DllImport("__Internal")] private static extern string getAccessToken();
		
		[DllImport("__Internal")] private static extern IntPtr getOrCreateTelemetryService();
		
		[DllImport("__Internal")] private static extern void setEventsCollectionStateForEnableCollection(bool state);
		
		[DllImport("__Internal")] private static extern void registerSdkInfo(string sdkIdentifier, string version, string packageName);
		
		[DllImport("__Internal")] private static extern void sendTurnstileEvent(string sdkIdentifier, string version, string packageName);
		
		[DllImport("__Internal")] private static extern void sendSdkEvent(string sdkIdentifier, string version, string packageName);
		
		[DllImport("__Internal")] private static extern string getUserSKUToken();
		
		public void Initialize(string accessToken, Func<string> getSkuToken)
		{
			// getSkuToken is unused on iOS — the native BillingService generates the SKU
			// token internally for the billing event and tile requests.
			_telemetryService = getOrCreateTelemetryService();
			registerSdkInfo(Constants.SDK_IDENTIFIER, Constants.SDK_VERSION, Constants.PACKAGE_NAME);
		}

		static readonly ITelemetryLibrary _instance = new TelemetryIos();
		public static ITelemetryLibrary Instance => _instance;

		public void SendTurnstile()
		{
			sendTurnstileEvent(Constants.SDK_IDENTIFIER, Constants.SDK_VERSION, Constants.PACKAGE_NAME);
		}

		public void SendSdkEvent()
		{
			sendSdkEvent(Constants.SDK_IDENTIFIER, Constants.SDK_VERSION, Constants.PACKAGE_NAME);
		}

		public void SetLocationCollectionState(bool enable)
		{
			if (enable)
			{
				Input.location.Start();
			}
			else
			{
				Input.location.Stop();
			}

			setEventsCollectionStateForEnableCollection(enable);
		}
	}
}
#endif