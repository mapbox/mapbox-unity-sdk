
namespace Mapbox.Experimental.Platform.Http
{

	using Mapbox.Platform.Cache;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;

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



#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif

		private bool _disposed;
		private List<ICache> _caches = new List<ICache>();
		private ConcurrentDictionary<string, MapboxHttpRequest> _requests = new ConcurrentDictionary<string, MapboxHttpRequest>(Environment.ProcessorCount * 2, 49);
		private int _requestsExecuting = 0;
		private int _requestDelay = 0;
		private MapboxHttpClient _httpclient = new MapboxHttpClient();
		private int _timeoutSeconds;
		private string _accessToken;
		private bool _autoRefreshCache;


		public MapboxWebDataFetcher(int timeoutSeconds, string accessToken, bool autoRefreshCache)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
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
					foreach (var handler in _handlers)
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
			foreach (var cache in _caches)
			{
				cache.Clear();
			}
		}


		public void ReInit()
		{
			foreach (var cache in _caches)
			{
				cache.ReInit();
			}
		}


		public void CancelAllRequests()
		{
			MapboxHttpRequest request;
			foreach (var key in _requests.Keys)
			{
				if (_requests.TryRemove(key, out request))
				{
					request.Cancel();
					request.ResponseReveived -= Request_ResponseReveived;
					//request.Dispose();
					request = null;
				}
			}
		}


#pragma warning disable 4014
#pragma warning disable 1998
		public async Task<MapboxHttpRequest> GetRequest(string url)
		{

			UnityEngine.Debug.LogWarning("TODO EXPERIMENTAL: async and throttle");

			MapboxHttpRequest request = _httpclient.Request(url, _timeoutSeconds, _accessToken);
			_requestsExecuting = Interlocked.Increment(ref _requestsExecuting);
			_requests.AddOrUpdate(url, request, (key, oldValue) => request);
			request.ResponseReveived += Request_ResponseReveived;

			// **DON'T(!!!)** use await here, we want to initiate the actual request
			// but continue execution of this method to return the MapboxHttpRequest object
			//[System.Diagnostics.CodeAnalysis.SuppressMessage(
			Task.Run(async () =>
			{
				await Task.Delay(_requestDelay);
				await request.SendAsync(null, HttpMethod.Get);
			});


			return request;
		}
#pragma warning restore 4014
#pragma warning restore 1998

		private void Request_ResponseReveived(object sender, MapboxHttpResponseReceivedEventArgs e)
		{
			//////////////////////
			/////////////TODO: evalute rate limit headers and adjust Delay!!!
			/////////////////////

			if (null != e.Response)
			{
				MapboxHttpResponse r = e.Response;
				if (r.XRateLimitInterval.HasValue && r.XRateLimitLimit.HasValue)
				{
					double limitIntervalSeconds = r.XRateLimitInterval.Value;
					double remainingNrOfRequests = r.XRateLimitLimit.Value;
					double secondsPerRequest = remainingNrOfRequests / limitIntervalSeconds;
					_requestDelay = (int)Math.Ceiling(secondsPerRequest * 1000.0d);
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
			UnityEngine.Debug.Log($"webresponse id:{e.Id}, completed:{e.Completed} successed:{e.Succeeded}");
			ResponseReveivedPrivate?.Invoke(sender, ea);
		}



	}
}
