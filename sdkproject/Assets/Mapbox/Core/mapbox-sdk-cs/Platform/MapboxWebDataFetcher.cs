
namespace Mapbox.Experimental.Platform.Http
{

	using Mapbox.Platform.Cache;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;

	public class MapboxWebDataFetcher : IDisposable
	{

#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private bool _disposed;
		private List<ICache> _caches = new List<ICache>();
		private ConcurrentQueue<MapboxHttpRequest> _requests = new ConcurrentQueue<MapboxHttpRequest>();
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
			MapboxHttpRequest req;
			while (_requests.TryDequeue(out req))
			{
				req.Cancel();
				//req.Dispose();
				req = null;
			}
		}

		public MapboxHttpRequest GetRequest(
			string url
		)
		{

			MapboxHttpRequest request = _httpclient.Request(url, _timeoutSeconds, _accessToken);
			_requests.Enqueue(request);
			request.ResponseReveived += Request_ResponseReveived;
			return request;
		}

		private void Request_ResponseReveived(object sender, MapboxHttpResponseReceivedEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
