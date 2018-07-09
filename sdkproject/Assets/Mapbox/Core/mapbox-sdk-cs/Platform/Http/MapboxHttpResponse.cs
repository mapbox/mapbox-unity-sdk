//-----------------------------------------------------------------------
// <copyright file="Response.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.Experimental.Platform.Http
{

	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;





	/// <summary> A response from a <see cref="IFileSource" /> request. </summary>
	public class MapboxHttpResponse
	{


		private MapboxHttpResponse() { }


		public IMapboxHttpRequest Request { get; private set; }


		public bool RateLimitHit
		{
			get { return StatusCode.HasValue ? 429 == StatusCode.Value : false; }
		}


		public DateTime? StartedUtc { get; set; }
		public DateTime? EndedUtc { get; set; }
		public TimeSpan? Duration => EndedUtc - StartedUtc;

		/// <summary>Flag to indicate if the request was successful</summary>
		public bool HasError
		{
			get { return _exceptions == null ? false : _exceptions.Count > 0; }
		}


		/// <summary>Flag to indicate if the request was fullfilled from a local cache</summary>
		public bool LoadedFromCache;

		/// <summary>Flag to indicate if the request was issued before but was issued again and updated</summary>
		public bool IsUpdate = false;

		public string RequestUrl;


		public int? StatusCode;


		public string ContentType;


		/// <summary>Length of rate-limiting interval in seconds. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public int? XRateLimitInterval;


		/// <summary>Maximum number of requests you may make in the current interval before reaching the limit. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public long? XRateLimitLimit;


		/// <summary>Timestamp of when the current interval will end and the ratelimit counter is reset. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public DateTime? XRateLimitReset;


		public MapboxWebDataRequestType WebDataRequestType;


		private List<Exception> _exceptions;
		/// <summary> Exceptions that might have occured during the request. </summary>
		public ReadOnlyCollection<Exception> Exceptions
		{
			get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
		}


		/// <summary> Messages of exceptions otherwise empty string. </summary>
		public string ExceptionsAsString
		{
			get
			{
				if (null == _exceptions || _exceptions.Count == 0) { return string.Empty; }
				return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
			}
		}


		/// <summary> Headers of the response. </summary>
		public Dictionary<string, string> Headers;


		/// <summary> Raw data fetched from the request. </summary>
		public byte[] Data;

		public void AddException(Exception ex)
		{
			if (null == _exceptions) { _exceptions = new List<Exception>(); }
			_exceptions.Add(ex);
		}

		// TODO: we should store timestamp of the cache!
		public static MapboxHttpResponse FromCache(byte[] data)
		{
			MapboxHttpResponse response = new MapboxHttpResponse();
			response.Data = data;
			response.LoadedFromCache = true;
			return response;
		}


		public static async Task<MapboxHttpResponse> FromWebResponse(IMapboxHttpRequest request, HttpResponseMessage apiResponse, Exception apiEx)
		{

			MapboxHttpResponse response = new MapboxHttpResponse();
			response.Request = request;
			response.RequestUrl = request.Url;
			response.WebDataRequestType = request.WebDataRequestType;

			if (null != apiEx)
			{
				response.AddException(apiEx);
			}

			// eg timeout: API response is null
			if (null == apiResponse)
			{
				response.AddException(new Exception("No Reponse."));
			}
			else
			{
				// https://www.mapbox.com/api-documentation/#rate-limits
				if (null != apiResponse.Headers)
				{
					response.Headers = new Dictionary<string, string>();
					foreach (var hdr in apiResponse.Headers)
					{
						string key = hdr.Key;
						string val = hdr.Value.FirstOrDefault();
						response.Headers.Add(key, val);
						if (key.Equals("X-Rate-Limit-Interval", StringComparison.OrdinalIgnoreCase))
						{
							int limitInterval;
							if (int.TryParse(val, out limitInterval)) { response.XRateLimitInterval = limitInterval; }
						}
						else if (key.Equals("X-Rate-Limit-Limit", StringComparison.OrdinalIgnoreCase))
						{
							long limitLimit;
							if (long.TryParse(val, out limitLimit)) { response.XRateLimitLimit = limitLimit; }
						}
						else if (key.Equals("X-Rate-Limit-Reset", StringComparison.OrdinalIgnoreCase))
						{
							double unixTimestamp;
							if (double.TryParse(val, out unixTimestamp))
							{
								DateTime beginningOfTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
								response.XRateLimitReset = beginningOfTime.AddSeconds(unixTimestamp).ToLocalTime();
							}
						}
						else if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
						{
							response.ContentType = val;
						}
					}
				}

				if (apiResponse.StatusCode != HttpStatusCode.OK)
				{
					response.AddException(new Exception(string.Format("{0}: {1}", apiResponse.StatusCode, apiResponse.ReasonPhrase)));
				}
				int statusCode = (int)apiResponse.StatusCode;
				response.StatusCode = statusCode;
				if (429 == statusCode)
				{
					response.AddException(new Exception("Rate limit hit"));
				}

				if (null != apiResponse)
				{
					response.Data = await apiResponse.Content.ReadAsByteArrayAsync();
				}
			}

			return response;
		}


	}
}
