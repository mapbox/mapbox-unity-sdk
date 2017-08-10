//-----------------------------------------------------------------------
// <copyright file="IAsyncRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_WP_8_1 || UNITY_WSA || UNITY_WEBGL || UNITY_IOS || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS
#define UNITY
#endif

namespace Mapbox.Platform {

	using Mapbox.Map;
	using System;

	/// <summary> A handle to an asynchronous request. </summary>
	public static class IAsyncRequestFactory {

		public static IAsyncRequest CreateRequest(
			string url
			, Action<Response> callback
			, int timeout
		) {
#if !UNITY
			if (Environment.ProcessorCount > 2) {
				return new HTTPRequestThreaded(url, callback, timeout);
			} else {
				return new HTTPRequestNonThreaded(url, callback, timeout);
			}
#else
			return new Mapbox.Unity.Utilities.HTTPRequest(url, callback, timeout);
#endif
		}


	}
}