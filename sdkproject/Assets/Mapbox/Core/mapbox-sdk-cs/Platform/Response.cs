//-----------------------------------------------------------------------
// <copyright file="Response.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_WP_8_1 || UNITY_WSA || UNITY_WEBGL || UNITY_IOS || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS
#define UNITY
#endif

namespace Mapbox.Platform {

	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Linq;
	using System.Net;
	using Utils;

#if NETFX_CORE
	using System.Net.Http;
	using System.Threading.Tasks;
#endif
#if UNITY
	using UnityEngine.Networking;
#endif

	/// <summary> A response from a <see cref="IFileSource" /> request. </summary>
	public class Response {


		private Response() { }


		public IAsyncRequest Request { get; private set; }


		public bool RateLimitHit {
			get { return StatusCode.HasValue ? 429 == StatusCode.Value : false; }
		}


		/// <summary>Flag to indicate if the request was successful</summary>
		public bool HasError {
			get { return _exceptions == null ? false : _exceptions.Count > 0; }
		}


		public int? StatusCode;


		public string ContentType;


		/// <summary>Length of rate-limiting interval in seconds. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public int? XRateLimitInterval;


		/// <summary>Maximum number of requests you may make in the current interval before reaching the limit. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public long? XRateLimitLimit;


		/// <summary>Timestamp of when the current interval will end and the ratelimit counter is reset. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		public DateTime? XRateLimitReset;


		private List<Exception> _exceptions;
		/// <summary> Exceptions that might have occured during the request. </summary>
		public ReadOnlyCollection<Exception> Exceptions {
			get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
		}


		/// <summary> Messages of exceptions otherwise empty string. </summary>
		public string ExceptionsAsString {
			get {
				if (null == _exceptions || _exceptions.Count == 0) { return string.Empty; }
				return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
			}
		}


		/// <summary> Headers of the response. </summary>
		public Dictionary<string, string> Headers;


		/// <summary> Raw data fetched from the request. </summary>
		public byte[] Data;

		public void AddException(Exception ex) {
			if (null == _exceptions) { _exceptions = new List<Exception>(); }
			_exceptions.Add(ex);
		}


#if !NETFX_CORE && !UNITY // full .NET Framework
		public static Response FromWebResponse(IAsyncRequest request, HttpWebResponse apiResponse, Exception apiEx) {

			Response response = new Response();
			response.Request = request;

			if (null != apiEx) {
				response.AddException(apiEx);
			}

			// timeout: API response is null
			if (null == apiResponse) {
				response.AddException(new Exception("No Reponse."));
			} else {
				// https://www.mapbox.com/api-documentation/#rate-limits
				if (null != apiResponse.Headers) {
					response.Headers = new Dictionary<string, string>();
					for (int i = 0; i < apiResponse.Headers.Count; i++) {
						// TODO: implement .Net Core / UWP implementation
						string key = apiResponse.Headers.Keys[i];
						string val = apiResponse.Headers[i];
						response.Headers.Add(key, val);
						if (key.Equals("X-Rate-Limit-Interval", StringComparison.InvariantCultureIgnoreCase)) {
							int limitInterval;
							if (int.TryParse(val, out limitInterval)) { response.XRateLimitInterval = limitInterval; }
						} else if (key.Equals("X-Rate-Limit-Limit", StringComparison.InvariantCultureIgnoreCase)) {
							long limitLimit;
							if (long.TryParse(val, out limitLimit)) { response.XRateLimitLimit = limitLimit; }
						} else if (key.Equals("X-Rate-Limit-Reset", StringComparison.InvariantCultureIgnoreCase)) {
							double unixTimestamp;
							if (double.TryParse(val, out unixTimestamp)) {
								response.XRateLimitReset = UnixTimestampUtils.From(unixTimestamp);
							}
						} else if (key.Equals("Content-Type", StringComparison.InvariantCultureIgnoreCase)) {
							response.ContentType = val;
						}
					}
				}

				if (apiResponse.StatusCode != HttpStatusCode.OK) {
					response.AddException(new Exception(string.Format("{0}: {1}", apiResponse.StatusCode, apiResponse.StatusDescription)));
				}
				int statusCode = (int)apiResponse.StatusCode;
				response.StatusCode = statusCode;
				if (429 == statusCode) {
					response.AddException(new Exception("Rate limit hit"));
				}

				if (null != apiResponse) {
					using (Stream responseStream = apiResponse.GetResponseStream()) {
						byte[] buffer = new byte[0x1000];
						int bytesRead;
						using (MemoryStream ms = new MemoryStream()) {
							while (0 != (bytesRead = responseStream.Read(buffer, 0, buffer.Length))) {
								ms.Write(buffer, 0, bytesRead);
							}
							response.Data = ms.ToArray();
						}
					}
					apiResponse.Close();
				}
			}

			return response;
		}
#endif

#if NETFX_CORE && !UNITY //UWP but not Unity
		public static async Task<Response> FromWebResponse(IAsyncRequest request, HttpResponseMessage apiResponse, Exception apiEx) {

			Response response = new Response();
			response.Request = request;

			if (null != apiEx) {
				response.AddException(apiEx);
			}

			// timeout: API response is null
			if (null == apiResponse) {
				response.AddException(new Exception("No Reponse."));
			} else {
				// https://www.mapbox.com/api-documentation/#rate-limits
				if (null != apiResponse.Headers) {
					response.Headers = new Dictionary<string, string>();
					foreach (var hdr in apiResponse.Headers) {
						string key = hdr.Key;
						string val = hdr.Value.FirstOrDefault();
						response.Headers.Add(key, val);
						if (key.Equals("X-Rate-Limit-Interval", StringComparison.OrdinalIgnoreCase)) {
							int limitInterval;
							if (int.TryParse(val, out limitInterval)) { response.XRateLimitInterval = limitInterval; }
						} else if (key.Equals("X-Rate-Limit-Limit", StringComparison.OrdinalIgnoreCase)) {
							long limitLimit;
							if (long.TryParse(val, out limitLimit)) { response.XRateLimitLimit = limitLimit; }
						} else if (key.Equals("X-Rate-Limit-Reset", StringComparison.OrdinalIgnoreCase)) {
							double unixTimestamp;
							if (double.TryParse(val, out unixTimestamp)) {
								DateTime beginningOfTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
								response.XRateLimitReset = beginningOfTime.AddSeconds(unixTimestamp).ToLocalTime();
							}
						} else if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) {
							response.ContentType = val;
						}
					}
				}

				if (apiResponse.StatusCode != HttpStatusCode.OK) {
					response.AddException(new Exception(string.Format("{0}: {1}", apiResponse.StatusCode, apiResponse.ReasonPhrase)));
				}
				int statusCode = (int)apiResponse.StatusCode;
				response.StatusCode = statusCode;
				if (429 == statusCode) {
					response.AddException(new Exception("Rate limit hit"));
				}

				if (null != apiResponse) {
					response.Data = await apiResponse.Content.ReadAsByteArrayAsync();
				}
			}

			return response;
		}
#endif

#if UNITY // within Unity or UWP build from Unity
		public static Response FromWebResponse(IAsyncRequest request, UnityWebRequest apiResponse, Exception apiEx) {

			Response response = new Response();
			response.Request = request;

			if (null != apiEx) {
				response.AddException(apiEx);
			}

			if (!string.IsNullOrEmpty(apiResponse.error)) {
				response.AddException(new Exception(apiResponse.error));
			}

			if (null == apiResponse.downloadHandler.data) {
				response.AddException(new Exception("Response has no data."));
			}

#if NETFX_CORE
			StringComparison stringComp = StringComparison.OrdinalIgnoreCase;
#else
			StringComparison stringComp = StringComparison.InvariantCultureIgnoreCase;
#endif

			Dictionary<string, string> apiHeaders = apiResponse.GetResponseHeaders();
			if (null != apiHeaders) {
				response.Headers = new Dictionary<string, string>();
				foreach (var apiHdr in apiHeaders) {
					string key = apiHdr.Key;
					string val = apiHdr.Value;
					response.Headers.Add(key, val);
					if (key.Equals("X-Rate-Limit-Interval", stringComp)) {
						int limitInterval;
						if (int.TryParse(val, out limitInterval)) { response.XRateLimitInterval = limitInterval; }
					} else if (key.Equals("X-Rate-Limit-Limit", stringComp)) {
						long limitLimit;
						if (long.TryParse(val, out limitLimit)) { response.XRateLimitLimit = limitLimit; }
					} else if (key.Equals("X-Rate-Limit-Reset", stringComp)) {
						double unixTimestamp;
						if (double.TryParse(val, out unixTimestamp)) {
							response.XRateLimitReset = UnixTimestampUtils.From(unixTimestamp);
						}
					} else if (key.Equals("Content-Type", stringComp)) {
						response.ContentType = val;
					}
				}
			}

			int statusCode = (int)apiResponse.responseCode;
			response.StatusCode = statusCode;

			if (statusCode != 200) {
				response.AddException(new Exception(string.Format("Status Code {0}", apiResponse.responseCode)));
			}
			if (429 == statusCode) {
				response.AddException(new Exception("Rate limit hit"));
			}

			response.Data = apiResponse.downloadHandler.data;

			return response;
		}
#endif



	}
}