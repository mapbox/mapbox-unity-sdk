using System.IO;
using Mapbox.Core.Platform.Cache;
using UnityEngine;
using UnityEngine.Networking;

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

		private MapboxCacheManager _cacheManager;
		
		private bool _disposed;
		
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
//					for (int i = 0; i < _caches.Count; i++)
//					{
//						IDisposable cache = _caches[i] as IDisposable;
//						if (null != cache)
//						{
//							cache.Dispose();
//							cache = null;
//						}
//					}
				}

				_disposed = true;
			}
		}
		#endregion

		public CachingWebFileSource AddCacheManager(MapboxCacheManager cacheManager)
		{
			_cacheManager = cacheManager;
			return this;
		}
		
		/// <summary>
		/// Clear all caches
		/// </summary>
		public void Clear()
		{
			_cacheManager.Clear();
		}

		public void ReInit()
		{
			_cacheManager.ReInit();
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

			CacheItem cachedItem = _cacheManager.GetDataItem(tilesetId, tileId);

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
				if (cachedItem.ExpirationDate < DateTime.Now)
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
								_cacheManager.AddDataItem(tilesetId, tileId, cachedItem, false);
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

		public void UnityImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string tilesetId = null)
		{
			if (string.IsNullOrEmpty(tilesetId))
			{
				throw new Exception("Cannot cache without a tileset id");
			}

			var finalUrl = CreateFinalUrl(uri);

			//go through existing caches and check if we already have the requested tile available
			var textureItem = _cacheManager.GetTextureItem(tilesetId, tileId); //_cacheManager.GetTextureItem(tilesetId, tileId);
			if (textureItem == null)
			{
				if (_cacheManager.TextureExists(tilesetId, tileId))
				{
					_cacheManager.GetTextureItem(tilesetId, tileId, (textureCacheItem) =>
					{
						var textureResponse = new TextureResponse {Texture2D = textureCacheItem.Texture2D};
						callback(textureResponse);

						if (textureCacheItem.ExpirationDate < DateTime.Now)
						{
							Runnable.Run(FetchTextureIfNoneMatch(tileId, tilesetId, finalUrl, textureCacheItem, (response) =>
							{
								callback(response);
							}));
						}
					});

					return;
				}
			}

			Runnable.Run(FetchTexture(finalUrl, callback, tilesetId, tileId));
		}

		private string CreateFinalUrl(string uri)
		{
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
			return finalUrl;
		}

		public Texture2D GetTextureFromMemoryCache(string mapId, CanonicalTileId tileId)
		{
			var cacheItem = _cacheManager.GetTextureItem(mapId, tileId);
			if (cacheItem != null)
			{
				return cacheItem.Texture2D;
			}
			else
			{
				return null;
			}
		}

		private IEnumerator FetchTextureIfNoneMatch(CanonicalTileId tileId, string tilesetId, string finalUrl, TextureCacheItem textureCacheItem, Action<TextureResponse> callback)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(finalUrl))
			{
				if (!string.IsNullOrEmpty(textureCacheItem.ETag))
				{
					uwr.SetRequestHeader("If-None-Match", textureCacheItem.ETag);
				}

				yield return uwr.SendWebRequest();

				if (uwr.responseCode == 304) // 304 NOT MODIFIED
				{
					textureCacheItem.ExpirationDate = uwr.GetExpirationDate();
					_cacheManager.AddTextureItem(tilesetId, tileId, textureCacheItem, true);
				}
				else if (uwr.responseCode == 200) // 200 OK, it means etag&data has changed so need to update cache
				{
					var response = new TextureResponse();
					response.StatusCode = uwr.responseCode;

					string eTag = uwr.GetETag();
					var texture = DownloadHandlerTexture.GetContent(uwr);
					texture.wrapMode = TextureWrapMode.Clamp;
					response.Texture2D = texture;

					var expirationDate = uwr.GetExpirationDate();
					textureCacheItem.Texture2D = texture;
					textureCacheItem.ETag = eTag;
					textureCacheItem.ExpirationDate = expirationDate;
					textureCacheItem.Data = uwr.downloadHandler.data;
					_cacheManager.AddTextureItem(tilesetId, tileId, textureCacheItem, true);

					callback(response);
				}
			}
		}

		private IEnumerator FetchTexture(string finalUrl, Action<TextureResponse> callback, string tilesetId, CanonicalTileId tileId)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(finalUrl))
			{
				yield return uwr.SendWebRequest();

				var response = new TextureResponse();
				response.StatusCode = uwr.responseCode;
				if (uwr.isNetworkError || uwr.isHttpError)
				{
					response.AddException(new Exception(uwr.error));
				}
				else
				{
					string eTag = uwr.GetETag();
					DateTime expirationDate = uwr.GetExpirationDate();

					var texture = DownloadHandlerTexture.GetContent(uwr);
					texture.wrapMode = TextureWrapMode.Clamp;
					response.Texture2D = texture;
					_cacheManager.AddTextureItem(tilesetId, tileId, new TextureCacheItem()
					{
						Texture2D = texture,
						ETag = eTag,
						ExpirationDate = expirationDate,
						Data = uwr.downloadHandler.data
					}, true);

					callback(response);
				}
			}
		}

		private IAsyncRequest requestTileAndCache(string url, string tilesetId, CanonicalTileId tileId, int timeout, Action<Response> callback)
		{
			return IAsyncRequestFactory.CreateRequest(
				url,
				(Response response) =>
				{
					// if the request was successful add tile to all caches
					if (!response.HasError && null != response.Data)
					{
						string eTag = response.GetETag();
						DateTime expirationDate = response.GetExpirationDate();

						// propagate to all caches forcing update
						_cacheManager.AddDataItem(
							tilesetId
							, tileId
							, new CacheItem()
							{
								Data = response.Data,
								ETag = eTag,
								ExpirationDate = expirationDate
							}
							, true // force insert/update
						);
					}

					if (null != callback)
					{
						response.IsUpdate = true;
						callback(response);
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
				get { return true; }
			}


			public HttpRequestType RequestType
			{
				get { return HttpRequestType.Get; }
			}


			public void Cancel()
			{
				// Empty. We can't cancel an instantaneous response.
			}
		}
	}
}