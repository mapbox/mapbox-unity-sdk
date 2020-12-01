using System.Diagnostics;
using System.IO;
using Mapbox.Core.Platform.Cache;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

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

		//private MapboxCacheManager _cacheManager;
		
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
			//_cacheManager = cacheManager;
			return this;
		}

		public IAsyncRequest Request(
			string uri
			, Action<Response> callback
			, int timeout = 10
		)
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

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
#endif

			return requestTileAndCache(finalUrl, timeout, callback);
		}

		public void MapboxImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string tilesetId = null, string etag = null)
		{
			var finalUrl = CreateFinalUrl(uri);
			CustomImageRequest(finalUrl, callback, timeout, tileId, tilesetId, etag);
		}

		public void CustomImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string tilesetId = null, string etag = null)
		{
			if (string.IsNullOrEmpty(etag))
			{
				Runnable.Run(FetchTexture(uri, callback));
			}
			else
			{
				Runnable.Run(FetchTextureIfNoneMatch(uri, callback, etag));
			}
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

		private IEnumerator FetchTextureIfNoneMatch(string finalUrl,  Action<TextureResponse> callback, string etag)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(finalUrl))
			{
				if (!string.IsNullOrEmpty(etag))
				{
					uwr.SetRequestHeader("If-None-Match", etag);
				}

				yield return uwr.SendWebRequest();

				if (uwr.responseCode == 304) // 304 NOT MODIFIED
				{
					var response = new TextureResponse();
					response.StatusCode = uwr.responseCode;
					response.ExpirationDate = uwr.GetExpirationDate();
					callback(response);
					// textureCacheItem.ExpirationDate = uwr.GetExpirationDate();
					// _cacheManager.AddTextureItem(tilesetId, tileId, textureCacheItem, true);
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

					response.Texture2D = texture;
					response.ETag = eTag;
					response.ExpirationDate = expirationDate;
					response.Data = uwr.downloadHandler.data;

					callback(response);
				}
			}
		}

		private IEnumerator FetchTexture(string finalUrl, Action<TextureResponse> callback)
		{
			// Stopwatch sw = new Stopwatch();
			// sw.Start();

			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(finalUrl))
			{
				yield return uwr.SendWebRequest();

				// sw.Stop();
				// Debug.Log(sw.ElapsedMilliseconds);

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
					response.ETag = eTag;
					response.ExpirationDate = expirationDate;
					response.Data = uwr.downloadHandler.data;

					callback(response);
				}
			}
		}

		private IAsyncRequest requestTileAndCache(string url, int timeout, Action<Response> callback)
		{
			return IAsyncRequestFactory.CreateRequest(
				url,
				(Response response) =>
				{
					// if the request was successful add tile to all caches
					if (response.HasError)
					{
						response.AddException(new Exception(response.ExceptionsAsString));
					}

					if (null != callback)
					{
						response.IsUpdate = true;
						callback(response);
					}
				}, timeout);
		}
	}
}