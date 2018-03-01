//-----------------------------------------------------------------------
// <copyright file="IAsyncRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.Platform {

	using Mapbox.Unity.Utilities;


	/// <summary> A handle to an asynchronous request. </summary>
	public interface IAsyncRequest {

		/// <summary> True after the request has finished. </summary>
		bool IsCompleted { get; }

		/// <summary> Cancel the ongoing request, preventing it from firing a callback. </summary>
		void Cancel();

		/// <summary>Type of request: GET, HEAD, ...</summary>
		HttpRequestType RequestType { get; }
	}
}