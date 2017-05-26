//-----------------------------------------------------------------------
// <copyright file="FileSource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Platform
{


	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Security;
#if !NETFX_CORE
	using System.Security.Cryptography.X509Certificates;
#endif
	using System.Threading;


	/// <summary>
	///     Mono implementation of the FileSource class. It will use Mono's
	///     <see href="http://www.mono-project.com/docs/advanced/runtime/">runtime</see> to
	///     asynchronously fetch data from the network via HTTP or HTTPS requests.
	/// </summary>
	/// <remarks>
	///     This implementation requires .NET 4.5 and later. The access token is expected to
	///     be exported to the environment as MAPBOX_ACCESS_TOKEN.
	/// </remarks>
	public sealed class FileSource : IFileSource
	{


		private readonly Dictionary<IAsyncRequest, int> _requests = new Dictionary<IAsyncRequest, int>();
		private readonly string _accessToken;
		private readonly object _lock = new object();

		/// <summary>Length of rate-limiting interval in seconds. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private int? XRateLimitInterval;
		/// <summary>Maximum number of requests you may make in the current interval before reaching the limit. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private long? XRateLimitLimit;
		/// <summary>Timestamp of when the current interval will end and the ratelimit counter is reset. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private DateTime? XRateLimitReset;


		public FileSource(string acessToken = null)
		{
			if (string.IsNullOrEmpty(acessToken))
			{
				_accessToken = Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
			}
			else
			{
				_accessToken = acessToken;
			}
		}

		/// <summary> Performs a request asynchronously. </summary>
		/// <param name="url"> The HTTP/HTTPS url. </param>
		/// <param name="callback"> Callback to be called after the request is completed. </param>
		/// <returns>
		///     Returns a <see cref="IAsyncRequest" /> that can be used for canceling a pending
		///     request. This handle can be completely ignored if there is no intention of ever
		///     canceling the request.
		/// </returns>
		public IAsyncRequest Request(string url, Action<Response> callback, int timeout = 10)
		{
			if (!string.IsNullOrEmpty(_accessToken))
			{
				var uriBuilder = new UriBuilder(url);
				string accessTokenQuery = "access_token=" + _accessToken;
				if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
				{
					uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery;
				}
				else
				{
					uriBuilder.Query = accessTokenQuery;
				}

				url = uriBuilder.ToString();
			}

			// TODO:
			// * add queue for requests
			// * evaluate rate limits (headers and status code)
			// * throttle requests accordingly
			//var request = new HTTPRequest(url, callback);
			//IEnumerator<IAsyncRequest> proxy = proxyResponse(url, callback);
			//proxy.MoveNext();
			//IAsyncRequest request = proxy.Current;

			//return request;

			return proxyResponse(url, callback);
		}


		// TODO: look at requests and implement throttling if needed
		//private IEnumerator<IAsyncRequest> proxyResponse(string url, Action<Response> callback) {
		private IAsyncRequest proxyResponse(string url, Action<Response> callback)
		{

			// TODO: plugin caching somewhere around here

			var request = IAsyncRequestFactory.CreateRequest(url, (Response response) =>
			{
				if (response.XRateLimitInterval.HasValue) { XRateLimitInterval = response.XRateLimitInterval; }
				if (response.XRateLimitLimit.HasValue) { XRateLimitLimit = response.XRateLimitLimit; }
				if (response.XRateLimitReset.HasValue) { XRateLimitReset = response.XRateLimitReset; }
				callback(response);
				lock (_lock)
				{
					//another place to catch if request has been cancelled
					try
					{
						_requests.Remove(response.Request);
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine(ex);
					}
				}
			});
			lock (_lock)
			{
				//sometimes we get here after the request has already finished
				if (!request.IsCompleted)
				{
					_requests.Add(request, 0);
				}
			}
			//yield return request;
			return request;
		}


		/// <summary>
		///     Block until all the requests are processed.
		/// </summary>
		public void WaitForAllRequests()
		{
			int waitTimeMs = 150;
			while (_requests.Count > 0)
			{
				lock (_lock)
				{
					foreach (var req in _requests)
					{
						if (((IAsyncRequest)req.Key).IsCompleted)
						{
							// another place to watch out if request has been cancelled
							try
							{
								_requests.Remove(req.Key);
							}
							catch (Exception ex)
							{
								System.Diagnostics.Debug.WriteLine(ex);
							}
						}
					}
				}

#if !WINDOWS_UWP
				//Thread.Sleep(50);
				// TODO: get rid of DoEvents!!! and find non-blocking wait that works for Net3.5
				//System.Windows.Forms.Application.DoEvents();

				var resetEvent = new ManualResetEvent(false);
				ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
				{
					Thread.Sleep(waitTimeMs);
					resetEvent.Set();
				}), null);
				resetEvent.WaitOne();
				resetEvent.Close();
				resetEvent = null;

#else
				System.Threading.Tasks.Task.Delay(waitTimeMs).Wait();
#endif
			}
		}





	}
}
