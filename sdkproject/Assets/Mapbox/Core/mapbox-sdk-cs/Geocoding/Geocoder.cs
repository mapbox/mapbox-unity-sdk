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
#if MAPBOX_EXPERIMENTAL
	using System.Threading.Tasks;
	using Mapbox.Experimental.Platform.Http;
	using Mapbox.Unity;
#endif

	/// <summary>
	///     Wrapper around the <see href="https://www.mapbox.com/api-documentation/#geocoding">
	///     Mapbox Geocoding API</see>. The Geocoder does two things: geocoding and reverse geocoding.
	/// </summary>
	public sealed class Geocoder
	{
#if MAPBOX_EXPERIMENTAL
		private readonly MapboxAccess _mapboxAccess;
#endif
		private readonly IFileSource fileSource;

#if MAPBOX_EXPERIMENTAL
		public Geocoder(MapboxAccess mapboxAccess)
		{
			_mapboxAccess = mapboxAccess;
		}
#endif

		/// <summary> Initializes a new instance of the <see cref="Geocoder" /> class. </summary>
		/// <param name="fileSource"> Network access abstraction. </param>
		public Geocoder(IFileSource fileSource)
		{
			this.fileSource = fileSource;
		}



#if MAPBOX_EXPERIMENTAL
		public async Task<ReverseGeocodeResponse> GeocodeReverse<T>(GeocodeResource<T> geocode)
		{
			MapboxHttpRequest request = await _mapboxAccess.Request(
				MapboxWebDataRequestType.Geocode
				, null
				, MapboxHttpMethod.Get
				, geocode.GetUrl()
			);
			MapboxHttpResponse response = await request.GetResponseAsync();

			if (null == response.Data) { return new ReverseGeocodeResponse(); }

			string jsonTxt = Encoding.UTF8.GetString(response.Data);
			return Deserialize<ReverseGeocodeResponse>(jsonTxt);
		}
#endif


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




#if MAPBOX_EXPERIMENTAL
		public async Task<ForwardGeocodeResponse> GeocodeForward<T>(GeocodeResource<T> geocode)
		{
			MapboxHttpRequest request = await _mapboxAccess.Request(
				MapboxWebDataRequestType.Geocode
				, null
				, MapboxHttpMethod.Get
				, geocode.GetUrl()
			);
			MapboxHttpResponse response = await request.GetResponseAsync();

			if (response.HasError && null == response.Data) { return new ForwardGeocodeResponse(); }

			string jsonTxt = Encoding.UTF8.GetString(response.Data);
			return Deserialize<ForwardGeocodeResponse>(jsonTxt);
		}
#endif


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
		public T Deserialize<T>(string str)
		{
			return JsonConvert.DeserializeObject<T>(str, JsonConverters.Converters);
		}
	}
}
