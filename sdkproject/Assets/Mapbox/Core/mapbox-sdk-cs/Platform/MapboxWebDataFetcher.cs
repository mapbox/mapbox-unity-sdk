
namespace Mapbox.Experimental.Platform.Http
{

	using Mapbox.Platform.Cache;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public class MapboxWebDataFetcher : IDisposable
	{

		// trying to automatically disconnect all event handlers
		private List<EventHandler<MapboxHttpResponseReceivedEventArgs>> _handlers = new List<EventHandler<MapboxHttpResponseReceivedEventArgs>>();

		private event EventHandler<MapboxHttpResponseReceivedEventArgs> ResponseReveivedPrivate;
		//public event EventHandler<MapboxHttpResponseReceivedEventArgs> ResponseReveived;
		public event EventHandler<MapboxHttpResponseReceivedEventArgs> ResponseReveived
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


		/// TODO async
		public async Task<MapboxHttpRequest> GetRequest(string url)
		{

			UnityEngine.Debug.LogWarning("TODO EXPERIMENTAL: async and throttle");

			await Task.Delay(0);

			MapboxHttpRequest request = _httpclient.Request(url, _timeoutSeconds, _accessToken);
			_requests.AddOrUpdate(url, request, (key, oldValue) => request);
			request.ResponseReveived += Request_ResponseReveived;
			return request;
		}


		private void Request_ResponseReveived(object sender, MapboxHttpResponseReceivedEventArgs e)
		{
			// TODO: evalute rate limit headers and adjust Delay!!!

			UnityEngine.Debug.Log($"webresponse [{e.Id}], completed:{e.Completed} successed:{e.Succeeded}");
			ResponseReveivedPrivate?.Invoke(sender, e);
		}



	}
}
