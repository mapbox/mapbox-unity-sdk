namespace Mapbox.Platform.Cache
{
	using System;
	using Mapbox.Platform;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;
	using System.Collections;
	using System.Linq;


	public class CachingWebFileSource : IFileSource, IDisposable
	{


#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private bool _disposed;
		private List<ICache> _caches = new List<ICache>();
		private string _accessToken;
		private Func<string> _getMapsSkuToken;
		private bool _autoRefreshCache;


		public CachingWebFileSource(string accessToken, Func<string> getMapsSkuToken, bool autoRefreshCache)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_accessToken = accessToken;
			_getMapsSkuToken = getMapsSkuToken;
			_autoRefreshCache = autoRefreshCache;
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


		public void ReInit() {
			foreach (var cache in _caches)
			{
				cache.ReInit();
			}
		}


		public IAsyncRequest Request(
			string uri
			, Action<Response> callback
			, int timeout = 10
			, CanonicalTileId tileId = new CanonicalTileId()
			, string tilesetId = null
		)
		{

			if (string.IsNullOrEmpty(tilesetId))
			{
				throw new Exception("Cannot cache without a tileset id");
			}

			CacheItem cachedItem = null;

			// go through existing caches and check if we already have the requested tile available
			foreach (var cache in _caches)
			{
				cachedItem = cache.Get(tilesetId, tileId);
				if (null != cachedItem)
				{
					break;
				}
			}

			var uriBuilder = new UriBuilder(uri);
			if (!string.IsNullOrEmpty(_accessToken))
			{
				string accessTokenQuery = "access_token=" + _accessToken;
				string mapsSkuToken = "sku=" + _getMapsSkuToken();
				if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
				{
					uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery + "&" + mapsSkuToken;
				}
				else
				{
					uriBuilder.Query = accessTokenQuery + "&" + mapsSkuToken;
				}
			}
			string finalUrl = uriBuilder.ToString();

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
#endif

			// if tile was available call callback with it, propagate to all other caches and check if a newer one is available
			if (null != cachedItem)
			{
#if MAPBOX_DEBUG_CACHE
				UnityEngine.Debug.LogFormat("{0} {1} {2} {3}", methodName, tilesetId, tileId, null != cachedItem.Data ? cachedItem.Data.Length.ToString() : "cachedItem.Data is NULL");
#endif
				// immediately return cached tile
				callback(Response.FromCache(cachedItem.Data));

				// check for updated tiles online if this is enabled in the settings
				if (_autoRefreshCache)
				{
					// check if tile on the web is newer than the one we already have locally
					IAsyncRequestFactory.CreateRequest(
						finalUrl,
						(Response headerOnly) =>
						{
							// on error getting information from API just return. tile we have locally has already been returned above
							if (headerOnly.HasError)
							{
								return;
							}

							// TODO: remove Debug.Log before PR
							//UnityEngine.Debug.LogFormat(
							//	"{1}{0}cached:{2}{0}header:{3}"
							//	, Environment.NewLine
							//	, finalUrl
							//	, cachedItem.ETag
							//	, headerOnly.Headers["ETag"]
							//);

							// data from cache is the same as on the web:
							//   * tile has already been returned above
							//   * make sure all all other caches have it too, but don't force insert via cache.add(false)
							// additional ETag empty check: for backwards compability with old caches
							if (!string.IsNullOrEmpty(cachedItem.ETag) && cachedItem.ETag.Equals(headerOnly.Headers["ETag"]))
							{
								foreach (var cache in _caches)
								{
									cache.Add(tilesetId, tileId, cachedItem, false);
								}
							}
							else
							{
								// TODO: remove Debug.Log before PR
								UnityEngine.Debug.LogWarningFormat(
										"updating cached tile {1} tilesetId:{2}{0}cached etag:{3}{0}remote etag:{4}{0}{5}"
										, Environment.NewLine
										, tileId
										, tilesetId
										, cachedItem.ETag
										, headerOnly.Headers["ETag"]
										, finalUrl
									);

								// request updated tile and pass callback to return new data to subscribers
								requestTileAndCache(finalUrl, tilesetId, tileId, timeout, callback);
							}
						}
						, timeout
						, HttpRequestType.Head
					);
				}

				return new MemoryCacheAsyncRequest(uri);
			}
			else
			{
				// requested tile is not in any of the caches yet, get it
#if MAPBOX_DEBUG_CACHE
				UnityEngine.Debug.LogFormat("{0} {1} {2} not cached", methodName, tilesetId, tileId);
#endif
				return requestTileAndCache(finalUrl, tilesetId, tileId, timeout, callback);
			}
		}


		private IAsyncRequest requestTileAndCache(string url, string tilesetId, CanonicalTileId tileId, int timeout, Action<Response> callback)
		{
			return IAsyncRequestFactory.CreateRequest(
				url,
				(Response r) =>
				{
					// if the request was successful add tile to all caches
					if (!r.HasError && null != r.Data)
					{
						string eTag = string.Empty;
						DateTime? lastModified = null;

						if (!r.Headers.ContainsKey("ETag"))
						{
							UnityEngine.Debug.LogWarningFormat("no 'ETag' header present in response for {0}", url);
						}
						else
						{
							eTag = r.Headers["ETag"];
						}

						// not all APIs populate 'Last-Modified' header
						// don't log error if it's missing
						if (r.Headers.ContainsKey("Last-Modified"))
						{
							lastModified = DateTime.ParseExact(r.Headers["Last-Modified"], "r", null);
						}

						// propagate to all caches forcing update
						foreach (var cache in _caches)
						{
							cache.Add(
								tilesetId
								, tileId
								, new CacheItem()
								{
									Data = r.Data,
									ETag = eTag,
									LastModified = lastModified
								}
								, true // force insert/update
							);
						}
					}
					if (null != callback)
					{
						r.IsUpdate = true;
						callback(r);
					}
				}, timeout);
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


			public HttpRequestType RequestType { get { return HttpRequestType.Get; } }


			public void Cancel()
			{
				// Empty. We can't cancel an instantaneous response.
			}
		}
	}
}
