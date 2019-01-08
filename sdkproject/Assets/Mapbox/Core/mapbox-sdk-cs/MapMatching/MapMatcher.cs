//-----------------------------------------------------------------------
// <copyright file="MapMatcher.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapMatching
{
	using System;
	using System.Text;
	using Mapbox.Json;
	using Mapbox.Platform;
	using Mapbox.Utils.JsonConverters;

	/// <summary>
	///     Wrapper around the <see href="https://www.mapbox.com/api-documentation/navigation/#map-matching">
	///     Mapbox Map Matching API</see>.
	/// </summary>
	public class MapMatcher
	{
		private readonly IFileSource _fileSource;
		private int _timeout;

		/// <summary> Initializes a new instance of the <see cref="MapMatcher" /> class. </summary>
		/// <param name="fileSource"> Network access abstraction. </param>
		public MapMatcher(IFileSource fileSource, int timeout)
		{
			_fileSource = fileSource;
			_timeout = timeout;
		}

		/// <summary> Performs asynchronously a geocoding lookup. </summary>
		/// <param name="geocode"> Geocode resource. </param>
		/// <param name="callback"> Callback to be called after the request is completed. </param>
		/// <typeparam name="T"> String or LngLat. Should be automatically inferred. </typeparam>
		/// <returns>
		///     Returns a <see cref="IAsyncRequest" /> that can be used for canceling a pending
		///     request. This handle can be completely ignored if there is no intention of ever
		///     canceling the request.
		/// </returns>
		public IAsyncRequest Match(MapMatchingResource match, Action<MapMatchingResponse> callback)
		{
			string url = match.GetUrl();
			return _fileSource.Request(
				url,
				(Response response) =>
				{
					var str = Encoding.UTF8.GetString(response.Data);
					var data = Deserialize<MapMatchingResponse>(str);

					if (response.HasError)
					{
						data.SetRequestExceptions(response.Exceptions);
					}

					callback(data);
				},
				_timeout
				);
		}


		/// <summary>
		/// Deserialize the map match response string into a <see cref="MapMatchingResponse"/>.
		/// </summary>
		/// <param name="str">JSON String.</param>
		/// <returns>A <see cref="MapMatchingResponse"/>.</returns>
		/// <typeparam name="T">Map Matcher. </typeparam>
		internal T Deserialize<T>(string str)
		{
			return JsonConvert.DeserializeObject<T>(str, JsonConverters.Converters);
		}
	}
}
