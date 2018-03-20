//-----------------------------------------------------------------------
// <copyright file="Geocoder.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
	using System;
	using System.Text;
	using Mapbox.Json;
	using Mapbox.Platform;
	using Mapbox.Utils.JsonConverters;

	/// <summary>
	///     Wrapper around the <see href="https://www.mapbox.com/api-documentation/#geocoding">
	///     Mapbox Geocoding API</see>. The Geocoder does two things: geocoding and reverse geocoding.
	/// </summary>
	public sealed class Geocoder
	{
		private readonly IFileSource fileSource;

		/// <summary> Initializes a new instance of the <see cref="Geocoder" /> class. </summary>
		/// <param name="fileSource"> Network access abstraction. </param>
		public Geocoder(IFileSource fileSource)
		{
			this.fileSource = fileSource;
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
		public IAsyncRequest Geocode<T>(GeocodeResource<T> geocode, Action<ReverseGeocodeResponse> callback)
		{
			return this.fileSource.Request(
				geocode.GetUrl(),
				(Response response) =>
				{
					var str = Encoding.UTF8.GetString(response.Data);

					var data = Deserialize<ReverseGeocodeResponse>(str);

					callback(data);
				});
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
		public IAsyncRequest Geocode<T>(GeocodeResource<T> geocode, Action<ForwardGeocodeResponse> callback)
		{
			return this.fileSource.Request(
				geocode.GetUrl(),
				(Response response) =>
				{
					var str = Encoding.UTF8.GetString(response.Data);

					var data = Deserialize<ForwardGeocodeResponse>(str);

					callback(data);
				});
		}

		/// <summary>
		/// Deserialize the geocode response string into a <see cref="GeocodeResponse"/>.
		/// </summary>
		/// <param name="str">JSON String.</param>
		/// <returns>A <see cref="GeocodeResponse"/>.</returns>
		/// <typeparam name="T">Forward or reverse geocode. </typeparam>
		internal T Deserialize<T>(string str)
		{
			return JsonConvert.DeserializeObject<T>(str, JsonConverters.Converters);
		}
	}
}
