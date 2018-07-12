
namespace Mapbox.Experimental.Platform.Http
{


	using Mapbox.Platform.Cache;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using static System.FormattableString;


	public enum MapboxWebDataRequestType
	{
		Generic,
		Tile,
		Geocode,
		Direction,
		MapMatching,
		TileJson,
		Token,
		Telemetry
	}


	public class MapboxWebDataFetcher : IDisposable
	{

		// trying to automatically disconnect all event handlers
		private List<EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs>> _handlers = new List<EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs>>();

		private event EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs> ResponseReveivedPrivate;
		//public event EventHandler<MapboxHttpResponseReceivedEventArgs> ResponseReveived;
		public event EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs> ResponseReveived
		{
			add { ResponseReveivedPrivate += value; _handlers.Add(value); }
			remove { ResponseReveivedPrivate -= value; _handlers.Remove(value); }
		}



#if MAPBOX_DEBUG_HTTP
		private string _className;
#endif

		private bool _disposed;
		private static readonly object _lock = new object();
		private List<ICache> _caches = new List<ICache>();
		private ConcurrentDictionary<string, MapboxHttpRequest> _requests = new ConcurrentDictionary<string, MapboxHttpRequest>(Environment.ProcessorCount * 2, 49);
		private int _requestsExecuting = 0;
		private ConcurrentDictionary<MapboxWebDataRequestType, int> _requestDelays = new ConcurrentDictionary<MapboxWebDataRequestType, int>();
		private MapboxHttpClient _httpclient = new MapboxHttpClient();
		private int _timeoutSeconds;
		private string _accessToken;
		private bool _autoRefreshCache;


		public MapboxWebDataFetcher(int timeoutSeconds, string accessToken, bool autoRefreshCache)
		{
#if MAPBOX_DEBUG_HTTP
			_className = GetType().Name;
#endif
			_timeoutSeconds = timeoutSeconds;
			_accessToken = accessToken;
			_autoRefreshCache = autoRefreshCache;
		}


		#region idisposable


		~MapboxWebDataFetcher()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					foreach (EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs> handler in _handlers)
					{
						ResponseReveivedPrivate -= handler;
					}
					_handlers.Clear();

					for (int i = 0; i < _caches.Count; i++)
					{
						IDisposable cache = _caches[i] as IDisposable;
						if (null != cache)
						{
							cache.Dispose();
							cache = null;
						}
					}
				}

				_disposed = true;
			}
		}


		#endregion


		/// <summary>
		/// Add an ICache instance
		/// </summary>
		/// <param name="cache">Implementation of ICache</param>
		/// <returns></returns>
		public MapboxWebDataFetcher AddCache(ICache cache)
		{
			// don't add cache when cache size is 0
			if (0 == cache.MaxCacheSize)
			{
				return this;
			}

			_caches.Add(cache);
			return this;
		}


		/// <summary>
		/// Clear all caches
		/// </summary>
		public void Clear()
		{
			foreach (ICache cache in _caches)
			{
				cache.Clear();
			}
		}


		public void ReInit()
		{
			foreach (ICache cache in _caches)
			{
				cache.ReInit();
			}
		}


		public void CancelAllRequests()
		{
			MapboxHttpRequest request;
			foreach (string key in _requests.Keys)
			{
				if (_requests.TryRemove(key, out request))
				{
					request.Cancel();
					request.ResponseReveived -= Request_ResponseReveived;
					//request.Dispose();
					request = null;
				}
				else
				{
					UnityEngine.Debug.LogError($"CancelAllRequests: Unexpected error: could not remove request for [{key}] from internal queue");
				}

			}
		}


#pragma warning disable 4014
#pragma warning disable 1998
		/// <summary>
		/// /
		/// </summary>
		/// <param name="webDataRequestType"></param>
		/// <param name="id"></param>
		/// <param name="verb"></param>
		/// <param name="url"></param>
		/// <param name="contentAsString">Some parts of Unity cannot access System.Net.Http: limit to StringContent for now</param>
		/// <param name="headers"></param>
		/// <returns></returns>
		public async Task<MapboxHttpRequest> GetRequestAsync(
			MapboxWebDataRequestType webDataRequestType
			, object id
			, MapboxHttpMethod verb
			, string url
			/*
			 * System.Net.Http not available from Unity unit tests
			 * HACK: just use string for now
			, HttpContent content = null
			*/
			, string contentAsString = null
			, Dictionary<string, string> headers = null
		)
		{

			HttpContent content = null;
			if (!string.IsNullOrWhiteSpace(contentAsString))
			{
				content = new StringContent(contentAsString, System.Text.Encoding.UTF8, "application/json");
			}

			UnityEngine.Debug.LogWarning("TODO EXPERIMENTAL: async and throttle");

			MapboxHttpRequest request = _httpclient.Request(url, _timeoutSeconds, _accessToken);
			_requestsExecuting = Interlocked.Increment(ref _requestsExecuting);
			_requests.AddOrUpdate(url, request, (key, oldValue) => request);
			request.ResponseReveived += Request_ResponseReveived;

			// **DON'T(!!!)** use await for 'Task.Run()', we want to initiate the actual request
			// but continue execution of this method to return the MapboxHttpRequest object
			//[System.Diagnostics.CodeAnalysis.SuppressMessage(
			Task.Run(async () =>
			{
				////////
				//TODO: create dedicated queues (with separte delays) for each requestType!!!
				///////
				// set a default delay of 30ms: will be autoadjusted according to
				// response header X-Rate headers as soon as we get the first response
				await Task.Delay(_requestDelays.GetOrAdd(webDataRequestType, 30));
				await request.SendAsync(webDataRequestType, id, verb, content, headers);
			});


			return request;
		}
#pragma warning restore 4014
#pragma warning restore 1998

		private void Request_ResponseReveived(object sender, MapboxHttpResponseReceivedEventArgs e)
		{
			//////////////////////
			/////////////TODO:
			///////////// evalute rate limit headers and adjust Delay!!!
			///////////// according to request type: different API calls have different rate limits!!!
			/////////////////////

			if (null != e.Response)
			{
				MapboxHttpResponse r = e.Response;
				if (r.XRateLimitInterval.HasValue && r.XRateLimitLimit.HasValue)
				{
					double limitIntervalSeconds = r.XRateLimitInterval.Value;
					double remainingNrOfRequests = r.XRateLimitLimit.Value;
					double requestsPerSecond = remainingNrOfRequests / limitIntervalSeconds;
					double milliSecondsPerRequest = 1000.0d / requestsPerSecond;
					lock (_lock)
					{
						int delay = (int)Math.Ceiling(milliSecondsPerRequest);
						_requestDelays.AddOrUpdate(e.Response.WebDataRequestType, delay, (key, oldValue) => delay);
#if MAPBOX_DEBUG_HTTP
						UnityEngine.Debug.LogWarning(Invariant($"new request delay set[{e.Response.WebDataRequestType}]: {delay} (remaining requests:{remainingNrOfRequests} time interval:{limitIntervalSeconds}s)"));
#endif
					}
				}
			}

			MapboxHttpRequest request;
			if (!_requests.TryRemove(e.Response.RequestUrl, out request))
			{
				UnityEngine.Debug.LogError($"Unexpected error: could not remove request for [{e.Response.RequestUrl}] from internal queue");
			}
			_requestsExecuting = Interlocked.Decrement(ref _requestsExecuting);

			MapboxWebDataFetcherResponseReceivedEventArgs ea = new MapboxWebDataFetcherResponseReceivedEventArgs(
				e
				, _requests.Count
				, _requestsExecuting
			);
#if MAPBOX_DEBUG_HTTP
			string methodName = new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.Log($"{_className}.{methodName} webresponse id:{e.Id}, completed:{e.Completed} successed:{e.Succeeded}");
			if (!e.Completed || e.Succeeded)
			{
				MapboxHttpResponse failed = e.Response;
				UnityEngine.Debug.LogError($"{failed.RequestUrl} statusCode:{failed.StatusCode} errors:{failed.ExceptionsAsString}");
			}
#endif
			ResponseReveivedPrivate?.Invoke(sender, ea);

			cache(e.Response);
		}



		private void cache(MapboxHttpResponse response)
		{
			UnityEngine.Debug.Log("TODO!!!! settings: implement caching strategy based on WebDataRequestType");

			if (null == response) { return; }

			bool doCache = false;
			switch (response.Request.WebDataRequestType)
			{
				case MapboxWebDataRequestType.Tile:
					doCache = true;
					break;
				default:
					doCache = false;
					break;
			}

			if (doCache)
			{
				foreach (ICache cache in _caches)
				{
					// TODO caching
				}
			}
		}



	}
}
