namespace Mapbox.Platform.Cache
{
	using System;
	using Mapbox.Platform;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;

	public class CachingWebFileSource : IFileSource, IDisposable
	{


		private bool _disposed;
		private List<ICache> _caches = new List<ICache>();
		private string _accessToken;


		public CachingWebFileSource(string accessToken)
		{
			_accessToken = accessToken;
		}


		#region idisposable


		~CachingWebFileSource()
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


		public CachingWebFileSource AddCache(ICache cache)
		{
			_caches.Add(cache);
			return this;
		}


		public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
		{

			byte[] data = null;

			foreach (var cache in _caches)
			{
				data = cache.Get(uri);
				if (null != data)
				{
					break;
				}
			}

			if (null != data)
			{
				foreach (var cache in _caches)
				{
					cache.Add(uri, data);
				}

				callback(Response.FromCache(data));
				return new MemoryCacheAsyncRequest(uri);
			}
			else
			{

				string cacheKey = uri;

				var uriBuilder = new UriBuilder(uri);

				if (!string.IsNullOrEmpty(_accessToken))
				{
					string accessTokenQuery = "access_token=" + _accessToken;
					if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
					{
						uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery;
					}
					else
					{
						uriBuilder.Query = accessTokenQuery;
					}
				}

				//UnityEngine.Debug.Log("CachingWebFileSource: sending HTTPRequest " + uri);

				return IAsyncRequestFactory.CreateRequest(
					uriBuilder.ToString(),
					(Response r) =>
					{
						if (!r.HasError)
						{
							foreach (var cache in _caches)
							{
								cache.Add(cacheKey, r.Data);
							}
						}
						callback(r);
					}, timeout);
			}
		}



		class MemoryCacheAsyncRequest : IAsyncRequest
		{


			public string RequestUrl { get; private set; }


			public MemoryCacheAsyncRequest(string requestUrl)
			{
				RequestUrl = requestUrl;
			}


			public bool IsCompleted
			{
				get
				{
					return true;
				}
			}


			public void Cancel()
			{
				// Empty. We can't cancel an instantaneous response.
			}
		}



	}
}
