namespace Mapbox.Platform.Cache
{
	using System;
	using Mapbox.Platform;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;
	using System.Collections;

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


		/// <summary>
		/// Add an ICache instance
		/// </summary>
		/// <param name="cache">Implementation of ICache</param>
		/// <returns></returns>
		public CachingWebFileSource AddCache(ICache cache)
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


		public IAsyncRequest Request(
			string uri
			, Action<Response> callback
			, int timeout = 10
			, CanonicalTileId tileId = new CanonicalTileId()
			, string mapId = null
		)
		{

			if (string.IsNullOrEmpty(mapId))
			{
				throw new Exception("Cannot cache without a map id");
			}

			byte[] data = null;

			// go through existing caches and check if we already have the requested tile available
			foreach (var cache in _caches)
			{
				data = cache.Get(mapId, tileId);
				if (null != data)
				{
					break;
				}
			}

			// if tile was available propagate to all other caches and return
			if (null != data)
			{
				foreach (var cache in _caches)
				{
					cache.Add(mapId, tileId, data);
				}

				callback(Response.FromCache(data));
				return new MemoryCacheAsyncRequest(uri);
			}
			else
			{
				// requested tile is not in any of the caches yet, get it
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

				return IAsyncRequestFactory.CreateRequest(
					uriBuilder.ToString(),
					(Response r) =>
					{
						// if the request was successful add tile to all caches
						if (!r.HasError && null != r.Data)
						{
							foreach (var cache in _caches)
							{
								cache.Add(mapId, tileId, r.Data);
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
