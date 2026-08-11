using System;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using MapboxAccountsUnity;
#endif
using UnityEngine;
using System.Runtime.InteropServices;

namespace Mapbox.BaseModule.Utilities
{
	public class MapboxConfiguration
	{
		public string AccessToken;
		public bool TelemetryEnabled = true;
		
		#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			[NonSerialized] private MapboxAccounts mapboxAccounts = new MapboxAccounts();

			public void Initialize()
			{
			
			}
			
			public string GetMapsSkuToken()
			{
				return mapboxAccounts.ObtainMapsSkuUserToken(Application.persistentDataPath);
			}
			
		#elif UNITY_IOS
			[DllImport("__Internal")] private static extern void setAccessTokenForToken(string accessToken);
			[DllImport("__Internal")] private static extern string getUserSKUToken();

			public void Initialize()
			{
				setAccessTokenForToken(AccessToken);
			}
		
			public string GetMapsSkuToken()
			{
				return getUserSKUToken();
			}
		#elif UNITY_ANDROID
			private string _mapboxBillingServiceClassName = "com.mapbox.common.BillingService";
			private string _mapboxBillingServiceFactoryClassName = "com.mapbox.common.BillingServiceFactory";
			private string _mapboxBillingFactoryGetMethodName = "getInstance";
			private string _mapboxUserSkuIdentifierClassName = "com.mapbox.common.UserSKUIdentifier";
			private string _unityMausEnumName = "UNITY_MAUS";
			private string _mapboxSkuTokenMethodName = "getUserSKUToken";
		
			//Mapbox Options
			private string _mapboxoptionsClassName = "com.mapbox.common.MapboxOptions";
			private string _mapboxOptionsSetAccessTokenMethodName = "setAccessToken";
			private string _couldNotGetMapboxOptionsMessage = "Couldn't get Mapbox Options";
			private string _unityMausEnumString = "UnityMAUS";
			private string _valuesMethodNameForEnums = "values";
			private string _toStringMethodName = "toString";
			private AndroidJavaObject _unityEnum;

			public void Initialize()
			{
				SetAccessToken(AccessToken);

				var skuidClass = new AndroidJavaClass(_mapboxUserSkuIdentifierClassName);
				var skuValuesArray = skuidClass.CallStatic<AndroidJavaObject>(_valuesMethodNameForEnums);
				var convertedArray = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(skuValuesArray.GetRawObject());
				foreach (var javaEnumObject in convertedArray)
				{
					var enumString = javaEnumObject.Call<string>(_toStringMethodName);
					if (enumString == _unityMausEnumString)
					{
						_unityEnum = javaEnumObject;
						break;
					}
				}
			}

			private bool SetAccessToken(string accessToken)
			{
				var mapboxOptionsClass = new AndroidJavaClass(_mapboxoptionsClassName);
				if (mapboxOptionsClass == null)
				{
					Debug.LogError(_couldNotGetMapboxOptionsMessage);
					return true;
				}

				mapboxOptionsClass.CallStatic(_mapboxOptionsSetAccessTokenMethodName, accessToken);
				return false;
			}

			public string GetMapsSkuToken()
			{
				var billingServiceFactory = new AndroidJavaClass(_mapboxBillingServiceFactoryClassName);
				var billingService = billingServiceFactory.CallStatic<AndroidJavaObject>(_mapboxBillingFactoryGetMethodName);
				return billingService.Call<string>(_mapboxSkuTokenMethodName, _unityEnum);
			}
		#endif
		
	}
}
