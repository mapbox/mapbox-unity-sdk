//-----------------------------------------------------------------------
// <copyright file="IAsyncRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Mapbox.BaseModule.Data.Platform {
	/// <summary> A handle to an asynchronous request. </summary>
	public static class IAsyncRequestFactory {

		public static IAsyncRequest CreateRequest(
			string url
			, Action<Response> callback
			, int timeout
			, HttpRequestType requestType= HttpRequestType.Get
		) {
			return new HTTPRequest(url, callback, timeout, requestType);
		}


	}
}
