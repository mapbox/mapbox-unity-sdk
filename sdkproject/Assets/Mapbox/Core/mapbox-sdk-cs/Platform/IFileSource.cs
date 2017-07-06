//-----------------------------------------------------------------------
// <copyright file="IFileSource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Platform
{
	using Mapbox.Map;
	using System;

	/// <summary>
	///     A data source abstraction. Used by classes that need to fetch data but really
	///     don't care about from where the data is coming from. An implementation of
	///     IFileSource could fetch the data from the network, disk cache or even generate
	///     the data at runtime.
	/// </summary>
	public interface IFileSource
	{
		/// <summary> Performs a request asynchronously. </summary>
		/// <param name="uri"> The resource description in the URI format. </param>
		/// <param name="callback"> Callback to be called after the request is completed. </param>
		/// <returns>
		///     Returns a <see cref="IAsyncRequest" /> that can be used for canceling a pending
		///     request. This handle can be completely ignored if there is no intention of ever
		///     canceling the request.
		/// </returns>
		IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string mapId = null);
	}
}